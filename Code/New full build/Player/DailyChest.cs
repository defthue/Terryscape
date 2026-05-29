using Sandbox;
using System;
using System.Collections.Generic;

public sealed class DailyChest : Component
{
	[Property] public string ChestId { get; set; } = "";
	[Property] public string ChestName { get; set; } = "Loot Chest";
	[Property] public float InteractDistance { get; set; } = 150f;
	[Property] public float CooldownHours { get; set; } = 24f;

	[Property, Group( "Loot" )] public ChestLootTable LootTable { get; set; }

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

			var def = ItemDatabase.Get( reward.Item );
			string name = def != null ? def.Name : reward.Item.ToString();

			if ( reward.Item == ItemId.GoldCoin )
			{
				if ( placed > 0 )
					GameLog.Add( $"You received {placed} gold.", "#f0c040" );
				if ( banked > 0 )
					GameLog.Add( $"Inventory full — {banked} gold sent to your bank.", "#c9a84c" );
			}
			else
			{
				if ( placed > 0 )
					GameLog.Add( $"You received {placed}x {name}.", "#6db8f0" );
				if ( banked > 0 )
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
		if ( LootTable == null )
			return rewards;

		var rng = new Random();
		var combined = new Dictionary<ItemId, int>();

		int gold = RollGold( rng );
		if ( gold > 0 )
			combined[ItemId.GoldCoin] = gold;

		int pulls = LootTable.Pulls < 1 ? 1 : LootTable.Pulls;
		for ( int i = 0; i < pulls; i++ )
		{
			var tier = PickTier( rng );
			if ( tier == null )
				continue;

			var item = PickItem( rng, tier );
			if ( item == null || item.Item == ItemId.None )
				continue;

			int amount = RollAmount( rng, item );
			if ( amount <= 0 )
				continue;

			if ( combined.TryGetValue( item.Item, out var existing ) )
				combined[item.Item] = existing + amount;
			else
				combined[item.Item] = amount;
		}

		foreach ( var kv in combined )
			rewards.Add( new DailyChestRewardEntry { Item = kv.Key, Amount = kv.Value } );

		return rewards;
	}

	int RollGold( Random rng )
	{
		int lo = Math.Min( LootTable.GoldMin, LootTable.GoldMax );
		int hi = Math.Max( LootTable.GoldMin, LootTable.GoldMax );
		if ( hi <= 0 ) return 0;
		if ( lo < 0 ) lo = 0;
		if ( hi <= lo ) return lo;
		return rng.Next( lo, hi + 1 );
	}

	bool TierHasValidItem( ChestLootTier tier )
	{
		if ( tier == null || tier.Items == null )
			return false;
		foreach ( var e in tier.Items )
			if ( e != null && e.Item != ItemId.None )
				return true;
		return false;
	}

	ChestLootTier PickTier( Random rng )
	{
		if ( LootTable.Tiers == null )
			return null;

		float total = 0f;
		foreach ( var t in LootTable.Tiers )
		{
			if ( t == null || t.Weight <= 0f || !TierHasValidItem( t ) ) continue;
			total += t.Weight;
		}
		if ( total <= 0f ) return null;

		float roll = (float)( rng.NextDouble() * total );
		float cursor = 0f;
		foreach ( var t in LootTable.Tiers )
		{
			if ( t == null || t.Weight <= 0f || !TierHasValidItem( t ) ) continue;
			cursor += t.Weight;
			if ( roll <= cursor ) return t;
		}
		return null;
	}

	ChestLootItem PickItem( Random rng, ChestLootTier tier )
	{
		float total = 0f;
		foreach ( var e in tier.Items )
		{
			if ( e == null || e.Item == ItemId.None ) continue;
			total += e.Weight <= 0f ? 1f : e.Weight;
		}
		if ( total <= 0f ) return null;

		float roll = (float)( rng.NextDouble() * total );
		float cursor = 0f;
		foreach ( var e in tier.Items )
		{
			if ( e == null || e.Item == ItemId.None ) continue;
			cursor += e.Weight <= 0f ? 1f : e.Weight;
			if ( roll <= cursor ) return e;
		}
		return null;
	}

	int RollAmount( Random rng, ChestLootItem item )
	{
		int lo = Math.Min( item.MinAmount, item.MaxAmount );
		int hi = Math.Max( item.MinAmount, item.MaxAmount );
		if ( lo < 1 ) lo = 1;
		if ( hi < lo ) hi = lo;
		return rng.Next( lo, hi + 1 );
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
