using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class TeleportStone : Component
{
	[Property] public string StoneId { get; set; } = "";
	[Property] public string StoneName { get; set; } = "Teleport Stone";
	[Property] public float InteractDistance { get; set; } = 150f;
	[Property] public int TeleportCost { get; set; } = 1;
	[Property] public float CooldownDuration { get; set; } = 10f;

	public static TeleportStone ActiveStone { get; private set; }
	public static float CooldownRemaining { get; private set; }

	protected override void OnUpdate()
	{
		if ( CooldownRemaining > 0f )
			CooldownRemaining -= Time.Delta;

		if ( ActiveStone == this )
		{
			if ( !IsPlayerInRange() )
			{
				Close();
				return;
			}
		}

		if ( ActiveStone != null )
			return;

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

		if ( !IsPlayerInRange() )
			return;

		if ( !Input.Pressed( "use" ) )
			return;

		Open();
	}

	void Open()
	{
		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return;

		if ( !inventory.IsStoneDiscovered( StoneId ) )
		{
			inventory.DiscoverStone( StoneId );
			GameLog.Add( $"Discovered teleport stone: {StoneName}!", "#a080d0" );
		}

		ActiveStone = this;
		Mouse.Visibility = MouseVisibility.Visible;
	}

	public static void Close()
	{
		ActiveStone = null;
		Mouse.Visibility = MouseVisibility.Hidden;
	}

	public bool TryTeleport( string targetStoneId )
	{
		if ( targetStoneId == StoneId )
			return false;

		if ( CooldownRemaining > 0f )
		{
			GameLog.Add( $"Teleport on cooldown. Wait {CooldownRemaining:F0} seconds.", "#c86464" );
			return false;
		}

		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		if ( !inventory.IsStoneDiscovered( targetStoneId ) )
			return false;

		if ( !inventory.HasItem( ItemId.GoldCoin, TeleportCost ) )
		{
			GameLog.Add( "You don't have enough gold to teleport.", "#c86464" );
			return false;
		}

		var target = FindStone( targetStoneId );
		if ( target == null )
		{
			GameLog.Add( "That teleport stone could not be found.", "#c86464" );
			return false;
		}

		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
			return false;

		inventory.RemoveItem( ItemId.GoldCoin, TeleportCost );
		player.WorldPosition = target.WorldPosition;
		CooldownRemaining = CooldownDuration;

		GameLog.Add( $"Teleported to {target.StoneName} for {TeleportCost} gold.", "#a080d0" );

		Close();
		return true;
	}

	public bool IsPlayerInRange()
	{
		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
			return false;

		return Vector3.DistanceBetween( WorldPosition, player.WorldPosition ) <= InteractDistance;
	}

	public List<TeleportStone> GetDiscoveredStones()
	{
		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return new List<TeleportStone>();

		var discovered = inventory.GetDiscoveredStones();
		var stones = new List<TeleportStone>();

		foreach ( var stone in Game.ActiveScene.GetAllComponents<TeleportStone>() )
		{
			if ( discovered.Contains( stone.StoneId ) )
				stones.Add( stone );
		}

		return stones;
	}

	TeleportStone FindStone( string stoneId )
	{
		foreach ( var stone in Game.ActiveScene.GetAllComponents<TeleportStone>() )
		{
			if ( stone.StoneId == stoneId )
				return stone;
		}

		return null;
	}

	Inventory GetPlayerInventory()
	{
		return PlayerHelper.GetLocalInventory();
	}
}