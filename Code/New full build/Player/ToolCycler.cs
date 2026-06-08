using Sandbox;

public sealed class ToolCycler : Component
{
	[Property] public string CycleAction { get; set; } = "CycleAction";

	int _lastBeltPos = -1;

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( !Input.Pressed( CycleAction ) )
			return;

		if ( IsAnyUIBlocking() )
			return;

		var inventory = GameObject.Components.Get<Inventory>();
		if ( inventory == null )
			return;

		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null && potionSystem.IsDrinking )
			return;

		var shooter = GameObject.Components.Get<ProjectileShooter>();
		if ( shooter != null && shooter.IsDrawing )
			return;

		CycleToNext( inventory );
	}

	void CycleToNext( Inventory inventory )
	{
		int hotbarSize = Inventory.HotbarSize;
		int emptyHandsPos = hotbarSize;
		int totalPositions = hotbarSize + 1;

		int equippedSlot = inventory.GetEquippedSlotIndex( EquipSlot.Weapon );

		int startPos;
		if ( equippedSlot >= 0 && equippedSlot < hotbarSize )
			startPos = equippedSlot;
		else if ( _lastBeltPos >= 0 && _lastBeltPos <= hotbarSize )
			startPos = _lastBeltPos;
		else
			startPos = emptyHandsPos;

		for ( int i = 1; i <= totalPositions; i++ )
		{
			int candidate = ( startPos + i ) % totalPositions;

			if ( candidate == emptyHandsPos )
			{
				if ( inventory.GetEquipped( EquipSlot.Weapon ) != ItemId.None )
					inventory.UnequipUnique( EquipSlot.Weapon );

				_lastBeltPos = emptyHandsPos;
				return;
			}

			if ( !IsCyclable( inventory, candidate ) )
				continue;

			if ( inventory.GetEquipped( EquipSlot.Weapon ) != ItemId.None )
				inventory.UnequipUnique( EquipSlot.Weapon );

			inventory.EquipUniqueAtSlot( candidate );
			_lastBeltPos = candidate;
			return;
		}
	}

	bool IsCyclable( Inventory inventory, int slotIndex )
	{
		var slot = inventory.GetSlot( slotIndex );
		if ( slot == null || !slot.IsUnique )
			return false;

		var def = ItemDatabase.Get( slot.Unique.ItemId );
		if ( def == null || def.Slot != EquipSlot.Weapon )
			return false;

		var skills = GameObject.Components.Get<Skills>();
		if ( skills != null && !skills.CanEquip( def ) )
			return false;

		return true;
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