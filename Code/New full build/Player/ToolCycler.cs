using Sandbox;

/// <summary>
/// Cycles through hotbar slots 1-5 on F key press.
/// Starts from currently equipped weapon's slot index (or 0 if nothing equipped).
/// Skips slots containing armor (helm/chest/legs/shield).
/// Unique non-armor items → equip via standard hotbar path.
/// Stackable items (potions, arrows, resources) or empty slots → unequip weapon (empty hands).
/// </summary>
public sealed class ToolCycler : Component
{
	[Property] public string CycleAction { get; set; } = "CycleAction";

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( !Input.Pressed( CycleAction ) )
			return;

		Log.Info( $"[ToolCycler] {CycleAction} pressed" );

		if ( IsAnyUIBlocking() )
		{
			Log.Info( "[ToolCycler] UI blocking, skipping" );
			return;
		}

		var inventory = GameObject.Components.Get<Inventory>();
		if ( inventory == null )
		{
			Log.Info( "[ToolCycler] No Inventory on GameObject" );
			return;
		}

		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null && potionSystem.IsDrinking )
			return;

		var shooter = GameObject.Components.Get<ProjectileShooter>();
		if ( shooter != null && shooter.IsDrawing )
			return;

		Log.Info( "[ToolCycler] Cycling..." );
		CycleToNext( inventory );
	}

	void CycleToNext( Inventory inventory )
	{
		int hotbarSize = Inventory.HotbarSize;
		int emptyHandsPos = hotbarSize;
		int totalPositions = hotbarSize + 1;

		int equippedSlot = inventory.GetEquippedSlotIndex( EquipSlot.Weapon );
		int currentPos;
		if ( equippedSlot >= 0 && equippedSlot < hotbarSize )
			currentPos = equippedSlot;
		else
			currentPos = emptyHandsPos;

		for ( int i = 1; i <= totalPositions; i++ )
		{
			int candidate = ( currentPos + i ) % totalPositions;

			if ( candidate == emptyHandsPos )
			{
				if ( inventory.GetEquipped( EquipSlot.Weapon ) != ItemId.None )
					inventory.UnequipUnique( EquipSlot.Weapon );
				return;
			}

			var slot = inventory.GetSlot( candidate );

			if ( slot != null && slot.IsUnique && IsArmor( slot.Unique.ItemId ) )
				continue;

			if ( slot == null || slot.IsEmpty || slot.IsStack )
				continue;

			if ( slot.IsUnique )
			{
				inventory.EquipUniqueAtSlot( candidate );
				return;
			}
		}
	}

	static bool IsArmor( ItemId id )
	{
		var def = ItemDatabase.Get( id );
		if ( def == null )
			return false;

		return def.Type == ItemType.HeavyArmor
			|| def.Type == ItemType.MediumArmor
			|| def.Type == ItemType.LightArmor
			|| def.Type == ItemType.Shield;
	}

	static bool IsAnyUIBlocking()
	{
		if ( PlayerGatherResource.UIOpen ) return true;
		if ( ShopStation.ActiveShop != null || ShopStation.ShowingChoice ) return true;
		if ( BankStation.ActiveBank != null ) return true;
		if ( CraftingStation.ActiveStation != null ) return true;
		if ( EnchantingStation.ActiveStation != null ) return true;
		if ( TeleportStone.ActiveStone != null ) return true;
		if ( NpcInteract.ActiveNpc != null ) return true;
		if ( JournalStation.IsOpen ) return true;
		if ( LeaderboardStation.IsOpen ) return true;
		if ( SpellbookStation.IsOpen ) return true;
		if ( MinimapState.IsFullMapOpen ) return true;
		if ( WelcomeHudState.IsOpen ) return true;
		if ( BlackjackSeat.LocalSeat != null ) return true;
		return false;
	}
}