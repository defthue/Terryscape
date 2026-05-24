using Sandbox;
using System;
using System.Collections.Generic;

[System.Serializable]
public class ChestLootEntry
{
	[Property] public ItemId Item { get; set; } = ItemId.None;
	[Property] public int MinAmount { get; set; } = 1;
	[Property] public int MaxAmount { get; set; } = 1;
	[Property] public float Weight { get; set; } = 1f;
}

public sealed class DailyChest : Component
{
	[Property] public string ChestId { get; set; } = "";
	[Property] public string ChestName { get; set; } = "Loot Chest";
	[Property] public float InteractDistance { get; set; } = 150f;
	[Property] public float CooldownHours { get; set; } = 24f;

	[Property, Group( "Loot" )] public int RollCount { get; set; } = 3;
	[Property, Group( "Loot" )] public List<ChestLootEntry> LootTable { get; set; } = new();

	public static List<DailyChestRewardEntry> LastRewards { get; private set; } = new();
	public static string LastRewardChestName { get; private set; } = "";
	public static bool RewardHudOpen { get; private set; } = false;

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( RewardHudOpen )
		{
			if ( Input.Pressed( "use" ) || Input.Pressed( "menu" ) )
				CloseRewardHud();
			return;
		}

		if ( NpcInteract.ActiveNpc != null )
			return;

		if ( CraftingStation.ActiveStation != null )
			return;

		if ( ShopStation.ActiveShop != null || ShopStation.ShowingChoice )
			return;

		if ( BankStation.ActiveBank != null )
			return;

		if ( EnchantingStation.ActiveStation != null )
			return;

		if ( TeleportStone.ActiveStone != null )
			return;

		if ( !IsPlayerInRange() )
			return;

		if ( !Input.Pressed( "use" ) )
			return;

		TryClaim();
	}

	void TryClaim()
	{
		var inventory = PlayerHelper.GetLocalInventory();
		if ( inventory == null )
			return;

		if ( string.IsNullOrEmpty( ChestId ) )
		{
			GameLog.Add( "This chest has no ID set.", "#c86464" );
			return;
		}

		float remainingHours = inventory.GetChestCooldownHoursRemaining( ChestId, CooldownHours );
		if ( remainingHours > 0f )
		{
			string when = FormatRemaining( remainingHours );
			GameLog.Add( $"This chest will be ready in {when}.", "#c86464" );
			return;
		}

		var rewards = RollLoot();
		if ( rewards.Count == 0 )
		{
			GameLog.Add( "The chest was empty.", "#c86464" );
			return;
		}

		foreach ( var reward in rewards )
		{
			var (placed, banked) = inventory.AddItemOrBank( reward.Item, reward.Amount );

			if ( banked > 0 )
			{
				var def = ItemDatabase.Get( reward.Item );
				string name = def != null ? def.Name : reward.Item.ToString();
				GameLog.Add( $"Inventory full — {banked}x {name} sent to your bank.", "#c9a84c" );
			}
		}

		inventory.MarkChestClaimed( ChestId );

		OpenRewardHud( rewards );

		SoundLibrary.PlaySellBuy();

		PlayerPersistence.Local?.SaveNow( SaveSection.Progress | SaveSection.Inventory | SaveSection.Stats );
	}

	List<DailyChestRewardEntry> RollLoot()
	{
		var rewards = new List<DailyChestRewardEntry>();
		if ( LootTable == null || LootTable.Count == 0 )
			return rewards;

		float totalWeight = 0f;
		foreach ( var entry in LootTable )
		{
			if ( entry == null || entry.Item == ItemId.None || entry.Weight <= 0f )
				continue;
			totalWeight += entry.Weight;
		}

		if ( totalWeight <= 0f )
			return rewards;

		var rng = new Random();
		var combined = new Dictionary<ItemId, int>();

		for ( int i = 0; i < RollCount; i++ )
		{
			float roll = (float)rng.NextDouble() * totalWeight;
			float cursor = 0f;
			ChestLootEntry chosen = null;

			foreach ( var entry in LootTable )
			{
				if ( entry == null || entry.Item == ItemId.None || entry.Weight <= 0f )
					continue;

				cursor += entry.Weight;
				if ( roll <= cursor )
				{
					chosen = entry;
					break;
				}
			}

			if ( chosen == null )
				continue;

			int min = chosen.MinAmount < 1 ? 1 : chosen.MinAmount;
			int max = chosen.MaxAmount < min ? min : chosen.MaxAmount;
			int amount = rng.Next( min, max + 1 );
			if ( amount <= 0 )
				continue;

			if ( combined.TryGetValue( chosen.Item, out var existing ) )
				combined[chosen.Item] = existing + amount;
			else
				combined[chosen.Item] = amount;
		}

		foreach ( var kv in combined )
		{
			rewards.Add( new DailyChestRewardEntry
			{
				Item = kv.Key,
				Amount = kv.Value
			} );
		}

		return rewards;
	}

	void OpenRewardHud( List<DailyChestRewardEntry> rewards )
	{
		LastRewards = rewards;
		LastRewardChestName = ChestName;
		RewardHudOpen = true;
		Mouse.Visibility = MouseVisibility.Visible;
	}

	public static void CloseRewardHud()
	{
		RewardHudOpen = false;
		Mouse.Visibility = MouseVisibility.Hidden;
	}

	public bool IsPlayerInRange()
	{
		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
			return false;

		return Vector3.DistanceBetween( WorldPosition, player.WorldPosition ) <= InteractDistance;
	}

	static string FormatRemaining( float hours )
	{
		if ( hours >= 1f )
		{
			int rounded = (int)Math.Ceiling( hours );
			return rounded == 1 ? "1 hour" : $"{rounded} hours";
		}

		int minutes = (int)Math.Ceiling( hours * 60f );
		if ( minutes < 1 )
			minutes = 1;
		return minutes == 1 ? "1 minute" : $"{minutes} minutes";
	}
}

public class DailyChestRewardEntry
{
	public ItemId Item { get; set; }
	public int Amount { get; set; }
}
