using Sandbox;

public sealed class ManaSystem : Component
{
	[Property] public int DefaultMaxMana { get; set; } = 0;

	public int CurrentMana { get; private set; }
	public int MaxMana { get; private set; }

	protected override void OnStart()
	{
		MaxMana = DefaultMaxMana;
		CurrentMana = 0;
	}

	public bool HasMana( int amount )
	{
		return CurrentMana >= amount;
	}

	public bool ConsumeMana( int amount )
	{
		if ( CurrentMana < amount )
			return false;

		CurrentMana -= amount;
		return true;
	}

	public void RestoreMana( int amount )
	{
		CurrentMana += amount;
		if ( CurrentMana > MaxMana )
			CurrentMana = MaxMana;
	}

	public bool TryDrinkManaPotion( ItemId potionId )
	{
		var inventory = Components.Get<Inventory>();
		if ( inventory == null )
			return false;

		var slots = inventory.GetSlots();
		for ( int i = 0; i < inventory.MaxSlots; i++ )
		{
			var slot = slots[i];
			if ( slot.IsStack && slot.ItemId == potionId )
				return TryDrinkManaPotionFromSlot( i );
		}

		return false;
	}

	public bool TryDrinkManaPotionFromSlot( int slotIndex )
	{
		if ( IsProxy )
			return false;

		var inventory = Components.Get<Inventory>();
		if ( inventory == null )
			return false;

		var slot = inventory.GetSlot( slotIndex );
		if ( slot == null || !slot.IsStack )
			return false;

		var potionId = slot.ItemId;
		int newMax = 0;

		switch ( potionId )
		{
			case ItemId.LesserManaPotion:
				newMax = 5;
				break;
			case ItemId.ManaPotion:
				newMax = 10;
				break;
			case ItemId.GreaterManaPotion:
				newMax = 20;
				break;
			default:
				return false;
		}

		var potionSystem = Components.Get<PotionSystem>();
		if ( potionSystem != null && !potionSystem.CanDrink() )
		{
			GameLog.Add( "You can't drink another potion yet.", "#c86464" );
			return false;
		}

		inventory.RemoveFromSlot( slotIndex, 1 );

		if ( newMax > MaxMana )
		{
			MaxMana = newMax;
			GameLog.Add( $"Mana capacity upgraded to {MaxMana}!", "#4a8ac8" );
		}

		CurrentMana = MaxMana;

		if ( potionSystem != null )
		{
			potionSystem.IsDrinking = true;
			potionSystem.DrinkTimer = potionSystem.DrinkDuration;
		}

		var def = ItemDatabase.Get( potionId );
		string name = def != null ? def.Name : "Mana Potion";
		GameLog.Add( $"You drink a {name}. Mana restored to {CurrentMana}/{MaxMana}.", "#4a8ac8" );

		return true;
	}
}