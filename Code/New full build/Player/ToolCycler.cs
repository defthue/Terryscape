using Sandbox;
using System.Collections.Generic;

public sealed class ToolCycler : Component
{
	enum Category
	{
		Hatchet,
		Pickaxe,
		Melee,
		Ranged,
		Magic,
		EmptyHands
	}

	static readonly Category[] CycleOrder = new[]
	{
		Category.Hatchet,
		Category.Pickaxe,
		Category.Melee,
		Category.Ranged,
		Category.Magic,
		Category.EmptyHands
	};

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( !Input.Pressed( "CycleTool" ) )
			return;

		if ( PlayerGatherResource.UIOpen )
			return;

		var gm = GameManager.Instance;
		if ( gm != null && gm.ChatOpen )
			return;

		CycleNext();
	}

	void CycleNext()
	{
		var inventory = GameObject.Components.Get<Inventory>();
		var skills = GameObject.Components.Get<Skills>();
		if ( inventory == null )
			return;

		var available = BuildAvailableCategories( inventory, skills );
		if ( available.Count == 0 )
			return;

		var current = GetCurrentCategory( inventory );
		var next = GetNextCategory( current, available );

		EquipCategory( next, inventory, skills );
	}

	List<Category> BuildAvailableCategories( Inventory inventory, Skills skills )
	{
		var list = new List<Category>();
		foreach ( var category in CycleOrder )
		{
			if ( category == Category.EmptyHands )
			{
				list.Add( category );
				continue;
			}

			if ( FindBestItemIndex( category, inventory, skills ) >= 0 || IsEquippedInCategory( category, inventory ) )
				list.Add( category );
		}
		return list;
	}

	Category GetCurrentCategory( Inventory inventory )
	{
		var equippedId = inventory.GetEquipped( EquipSlot.Weapon );
		if ( equippedId == ItemId.None )
			return Category.EmptyHands;

		return GetCategoryForItem( equippedId );
	}

	Category GetNextCategory( Category current, List<Category> available )
	{
		int currentIndex = available.IndexOf( current );
		if ( currentIndex < 0 )
			return available[0];

		int nextIndex = ( currentIndex + 1 ) % available.Count;
		return available[nextIndex];
	}

	void EquipCategory( Category category, Inventory inventory, Skills skills )
	{
		if ( category == Category.EmptyHands )
		{
			inventory.UnequipUnique( EquipSlot.Weapon );
			return;
		}

		int index = FindBestItemIndex( category, inventory, skills );
		if ( index < 0 )
			return;

		inventory.EquipUnique( index );
	}

	int FindBestItemIndex( Category category, Inventory inventory, Skills skills )
	{
		var items = inventory.GetUniqueItems();
		int bestIndex = -1;
		int bestTier = -1;

		for ( int i = 0; i < items.Count; i++ )
		{
			var instance = items[i];
			if ( GetCategoryForItem( instance.ItemId ) != category )
				continue;

			var def = ItemDatabase.Get( instance.ItemId );
			if ( def == null )
				continue;

			if ( skills != null && !skills.CanEquip( def ) )
				continue;

			if ( def.Tier > bestTier )
			{
				bestTier = def.Tier;
				bestIndex = i;
			}
		}

		return bestIndex;
	}

	bool IsEquippedInCategory( Category category, Inventory inventory )
	{
		var equippedId = inventory.GetEquipped( EquipSlot.Weapon );
		if ( equippedId == ItemId.None )
			return false;

		return GetCategoryForItem( equippedId ) == category;
	}

	Category GetCategoryForItem( ItemId id )
	{
		var def = ItemDatabase.Get( id );
		if ( def == null )
			return Category.EmptyHands;

		switch ( def.Type )
		{
			case ItemType.Tool:
				if ( def.Name.Contains( "Hatchet" ) )
					return Category.Hatchet;
				if ( def.Name.Contains( "Pickaxe" ) )
					return Category.Pickaxe;
				return Category.Melee;

			case ItemType.MeleeWeapon:
				return Category.Melee;

			case ItemType.RangedWeapon:
				return Category.Ranged;

			case ItemType.MagicWeapon:
				return Category.Magic;

			default:
				return Category.EmptyHands;
		}
	}
}
