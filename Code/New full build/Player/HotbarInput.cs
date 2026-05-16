using Sandbox;

public sealed class HotbarInput : Component
{
	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( PlayerGatherResource.UIOpen )
			return;
		if ( ShopStation.ActiveShop != null || ShopStation.ShowingChoice )
			return;
		if ( BankStation.ActiveBank != null )
			return;
		if ( CraftingStation.ActiveStation != null )
			return;
		if ( EnchantingStation.ActiveStation != null )
			return;
		if ( TeleportStone.ActiveStone != null )
			return;
		if ( NpcInteract.ActiveNpc != null )
			return;
		if ( JournalStation.IsOpen )
			return;
		if ( LeaderboardStation.IsOpen )
			return;
		if ( SpellbookStation.IsOpen )
			return;
		if ( MinimapState.IsFullMapOpen )
			return;
		if ( WelcomeHudState.IsOpen )
			return;
		if ( BlackjackSeat.LocalSeat != null )
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

		if ( Input.Pressed( "Slot1" ) )
			UseHotbarSlot( inventory, 0 );
		else if ( Input.Pressed( "Slot2" ) )
			UseHotbarSlot( inventory, 1 );
		else if ( Input.Pressed( "Slot3" ) )
			UseHotbarSlot( inventory, 2 );
		else if ( Input.Pressed( "Slot4" ) )
			UseHotbarSlot( inventory, 3 );
		else if ( Input.Pressed( "Slot5" ) )
			UseHotbarSlot( inventory, 4 );
	}

	void UseHotbarSlot( Inventory inventory, int slotIndex )
	{
		var slot = inventory.GetSlot( slotIndex );

		if ( slot == null || slot.IsEmpty )
		{
			if ( inventory.GetEquipped( EquipSlot.Weapon ) != ItemId.None )
				inventory.UnequipUnique( EquipSlot.Weapon );
			return;
		}

		if ( slot.IsUnique )
		{
			inventory.EquipUniqueAtSlot( slotIndex );
			return;
		}

		if ( !slot.IsStack )
			return;

		var def = ItemDatabase.Get( slot.ItemId );
		if ( def == null )
			return;

		if ( def.Type == ItemType.Potion )
		{
			var potionSystem = GameObject.Components.Get<PotionSystem>();
			if ( potionSystem != null )
				potionSystem.TryDrinkPotionFromSlot( slotIndex );
			return;
		}

		if ( def.Type == ItemType.Arrow )
		{
			inventory.EquipAmmoFromSlot( slotIndex );
			return;
		}
	}
}