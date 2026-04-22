using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class Inventory : Component
{
	Dictionary<ItemId, int> _items = new();
	List<ItemInstance> _uniqueItems = new();
	Dictionary<EquipSlot, ItemInstance> _equippedUnique = new();
	HashSet<string> _unlockedRecipes = new();
	HashSet<string> _discoveredStones = new();
	HashSet<string> _completedQuests = new();
	Dictionary<string, int> _killCounts = new();

	ItemId _equippedAmmoId = ItemId.None;
	int _equippedAmmoCount = 0;

	protected override void OnStart()
	{
		_items.Clear();
		_uniqueItems.Clear();
		_equippedUnique.Clear();
		_unlockedRecipes.Clear();
		_discoveredStones.Clear();
		_completedQuests.Clear();
		_killCounts.Clear();
		_equippedAmmoId = ItemId.None;
		_equippedAmmoCount = 0;
		AddItem( ItemId.AbyssiumHeavyHelm, 1 );
		AddItem( ItemId.AbyssiumHeavyChestplate, 1 );
		AddItem( ItemId.AbyssiumHeavyLegs, 1 );
		AddItem( ItemId.AbyssiumHatchet, 1 );
		AddItem( ItemId.AbyssiumPickaxe, 1 );
		AddItem( ItemId.WorldrootStaff, 1 );
		AddItem( ItemId.WorldrootBow, 1 );
		AddItem( ItemId.CoppiteArrow, 50 );
		AddItem( ItemId.ManaPotion, 10 );
		AddItem( ItemId.AbyssiumSword, 1 );
		}

	public static bool IsEquipmentItem( ItemId id )
	{
		var def = ItemDatabase.Get( id );
		if ( def == null )
			return false;

		if ( def.Type == ItemType.Arrow )
			return false;

		return def.Slot != EquipSlot.None;
	}

	public int GetItemCount( ItemId id )
	{
		if ( IsEquipmentItem( id ) )
		{
			int count = 0;
			foreach ( var item in _uniqueItems )
			{
				if ( item.ItemId == id && !item.IsEnchanted )
					count++;
			}
			return count;
		}

		if ( _items.TryGetValue( id, out var stackCount ) )
			return stackCount;

		return 0;
	}

	public bool HasItem( ItemId id, int amount = 1 )
	{
		return GetItemCount( id ) >= amount;
	}

	public bool AddItem( ItemId id, int amount = 1 )
	{
		if ( id == ItemId.None || amount <= 0 )
			return false;

		if ( IsEquipmentItem( id ) )
		{
			for ( int i = 0; i < amount; i++ )
				_uniqueItems.Add( new ItemInstance( id ) );
			return true;
		}

		var def = ItemDatabase.Get( id );
		int maxStack = def != null ? def.MaxStack : 999;

		int current = 0;
		if ( _items.TryGetValue( id, out var existing ) )
			current = existing;

		int newAmount = current + amount;
		if ( newAmount > maxStack )
			newAmount = maxStack;

		_items[id] = newAmount;
		return true;
	}

	public bool RemoveItem( ItemId id, int amount = 1 )
	{
		if ( !HasItem( id, amount ) )
			return false;

		if ( IsEquipmentItem( id ) )
		{
			int removed = 0;
			for ( int i = _uniqueItems.Count - 1; i >= 0 && removed < amount; i-- )
			{
				if ( _uniqueItems[i].ItemId == id && !_uniqueItems[i].IsEnchanted )
				{
					_uniqueItems.RemoveAt( i );
					removed++;
				}
			}
			return removed >= amount;
		}

		int current = 0;
		if ( _items.TryGetValue( id, out var existing ) )
			current = existing;

		int newAmount = current - amount;
		if ( newAmount <= 0 )
			_items.Remove( id );
		else
			_items[id] = newAmount;

		return true;
	}

	public void AddUniqueItem( ItemInstance instance )
	{
		_uniqueItems.Add( instance );
	}

	public void RemoveUniqueItem( int index )
	{
		if ( index >= 0 && index < _uniqueItems.Count )
			_uniqueItems.RemoveAt( index );
	}

	public List<ItemInstance> GetUniqueItems()
	{
		return _uniqueItems;
	}

	public int GetUniqueItemCount()
	{
		return _uniqueItems.Count;
	}

	public bool EquipAmmo( ItemId ammoId )
	{
		if ( ammoId == ItemId.None )
			return false;

		var def = ItemDatabase.Get( ammoId );
		if ( def == null || def.Type != ItemType.Arrow )
			return false;

		if ( !_items.TryGetValue( ammoId, out var count ) || count <= 0 )
			return false;

		var skills = Components.Get<Skills>();
		if ( skills != null && !skills.CanEquip( def ) )
		{
			GameLog.Add( $"You need {def.SkillRequired} level {def.LevelRequired} to equip {def.Name}.", "#c86464" );
			return false;
		}

		if ( _equippedAmmoId != ItemId.None )
			UnequipAmmo();

		_equippedAmmoId = ammoId;
		_equippedAmmoCount = count;
		_items.Remove( ammoId );

		GameLog.Add( $"Equipped {count}x {def.Name}.", "#c9a84c" );
		return true;
	}

	public void UnequipAmmo()
	{
		if ( _equippedAmmoId == ItemId.None )
			return;

		if ( _equippedAmmoCount > 0 )
		{
			int current = 0;
			if ( _items.TryGetValue( _equippedAmmoId, out var existing ) )
				current = existing;

			_items[_equippedAmmoId] = current + _equippedAmmoCount;
		}

		var def = ItemDatabase.Get( _equippedAmmoId );
		string name = def != null ? def.Name : _equippedAmmoId.ToString();
		GameLog.Add( $"Unequipped {name}.", "#c9a84c" );

		_equippedAmmoId = ItemId.None;
		_equippedAmmoCount = 0;
	}

	public bool ConsumeAmmo( int amount = 1 )
	{
		if ( _equippedAmmoId == ItemId.None || _equippedAmmoCount < amount )
			return false;

		_equippedAmmoCount -= amount;

		if ( _equippedAmmoCount <= 0 )
		{
			GameLog.Add( "You've run out of arrows!", "#c86464" );
			_equippedAmmoId = ItemId.None;
			_equippedAmmoCount = 0;
		}

		return true;
	}

	public ItemId GetEquippedAmmoId()
	{
		return _equippedAmmoId;
	}

	public int GetEquippedAmmoCount()
	{
		return _equippedAmmoCount;
	}

	public bool EquipUnique( int index )
	{
		if ( index < 0 || index >= _uniqueItems.Count )
			return false;

		var instance = _uniqueItems[index];
		var def = ItemDatabase.Get( instance.ItemId );
		if ( def == null || def.Slot == EquipSlot.None )
			return false;

		var skills = Components.Get<Skills>();
		if ( skills != null && !skills.CanEquip( def ) )
		{
			GameLog.Add( $"You need {def.SkillRequired} level {def.LevelRequired} to equip {def.Name}.", "#c86464" );
			return false;
		}

		if ( _equippedUnique.TryGetValue( def.Slot, out var previous ) )
		{
			_uniqueItems.Add( previous );
			_equippedUnique.Remove( def.Slot );
		}

		_uniqueItems.RemoveAt( index );
		_equippedUnique[def.Slot] = instance;

		GameLog.Add( $"Equipped {instance.GetDisplayName()}.", "#c9a84c" );
		return true;
	}

	public void UnequipUnique( EquipSlot slot )
	{
		if ( !_equippedUnique.ContainsKey( slot ) )
			return;

		var instance = _equippedUnique[slot];
		_equippedUnique.Remove( slot );
		_uniqueItems.Add( instance );

		GameLog.Add( $"Unequipped {instance.GetDisplayName()}.", "#c9a84c" );
	}

	public ItemInstance GetEquippedUnique( EquipSlot slot )
	{
		if ( _equippedUnique.TryGetValue( slot, out var instance ) )
			return instance;

		return null;
	}

	public void Unequip( EquipSlot slot )
	{
		if ( slot == EquipSlot.Ammo )
		{
			UnequipAmmo();
			return;
		}

		if ( _equippedUnique.ContainsKey( slot ) )
		{
			UnequipUnique( slot );
			return;
		}
	}

	public void UnequipAll()
	{
		var uniqueSlots = new List<EquipSlot>( _equippedUnique.Keys );
		foreach ( var slot in uniqueSlots )
			UnequipUnique( slot );

		UnequipAmmo();
	}

	public ItemId GetEquipped( EquipSlot slot )
	{
		if ( slot == EquipSlot.Ammo )
			return _equippedAmmoId;

		if ( _equippedUnique.TryGetValue( slot, out var instance ) )
			return instance.ItemId;

		return ItemId.None;
	}

	public bool IsEquippable( ItemId id )
	{
		var def = ItemDatabase.Get( id );
		if ( def == null )
			return false;

		return def.Slot != EquipSlot.None;
	}

	public bool HasIngredients( RecipeDefinition recipe )
	{
		foreach ( var ingredient in recipe.Ingredients )
		{
			if ( !HasItem( ingredient.Item, ingredient.Amount ) )
				return false;
		}

		return true;
	}

	public bool RemoveIngredients( RecipeDefinition recipe )
	{
		if ( !HasIngredients( recipe ) )
			return false;

		foreach ( var ingredient in recipe.Ingredients )
			RemoveItem( ingredient.Item, ingredient.Amount );

		return true;
	}

	public bool IsRecipeUnlocked( string recipeId )
	{
		return _unlockedRecipes.Contains( recipeId );
	}

	public void UnlockRecipe( string recipeId )
	{
		_unlockedRecipes.Add( recipeId );
	}

	public ItemDefinition GetEquippedWeaponDef()
	{
		var weaponId = GetEquipped( EquipSlot.Weapon );
		if ( weaponId == ItemId.None )
			return null;

		return ItemDatabase.Get( weaponId );
	}

	public float GetToolPower()
	{
		var def = GetEquippedWeaponDef();
		if ( def == null || def.Type != ItemType.Tool )
			return 0f;

		return def.ToolPower;
	}

	public float GetWeaponPower()
	{
		var def = GetEquippedWeaponDef();
		if ( def == null )
			return 1f;

		return def.WeaponPower;
	}

	public float GetTotalArmorValue()
	{
		float total = 0f;
		EquipSlot[] armorSlots = { EquipSlot.Head, EquipSlot.Chest, EquipSlot.Legs, EquipSlot.Shield };

		foreach ( var slot in armorSlots )
		{
			var id = GetEquipped( slot );
			if ( id == ItemId.None )
				continue;

			var def = ItemDatabase.Get( id );
			if ( def != null )
				total += def.ArmorValue;
		}

		return total;
	}

	public float GetArrowPower()
	{
		if ( _equippedAmmoId == ItemId.None )
			return 0f;

		var def = ItemDatabase.Get( _equippedAmmoId );
		if ( def == null )
			return 0f;

		return def.WeaponPower;
	}

	public float GetEnchantmentBonus( EnchantmentType type )
	{
		float total = 0f;

		foreach ( var kv in _equippedUnique )
		{
			if ( kv.Value.Enchantment == type )
				total += kv.Value.EnchantmentPercent;
		}

		return total;
	}

	public bool IsWeaponTool()
	{
		var def = GetEquippedWeaponDef();
		if ( def == null )
			return false;

		return def.Type == ItemType.Tool;
	}

	public bool IsWeaponHatchet()
	{
		var def = GetEquippedWeaponDef();
		if ( def == null )
			return false;

		return def.Type == ItemType.Tool && def.Name.Contains( "Hatchet" );
	}

	public bool IsWeaponPickaxe()
	{
		var def = GetEquippedWeaponDef();
		if ( def == null )
			return false;

		return def.Type == ItemType.Tool && def.Name.Contains( "Pickaxe" );
	}

	public bool IsWeaponMelee()
	{
		var def = GetEquippedWeaponDef();
		if ( def == null )
			return false;

		return def.Type == ItemType.MeleeWeapon || def.Type == ItemType.Tool;
	}

	public bool IsWeaponRanged()
	{
		var def = GetEquippedWeaponDef();
		if ( def == null )
			return false;

		return def.Type == ItemType.RangedWeapon;
	}

	public bool IsWeaponMagic()
	{
		var def = GetEquippedWeaponDef();
		if ( def == null )
			return false;

		return def.Type == ItemType.MagicWeapon;
	}

	public Dictionary<ItemId, int> GetAllItems()
	{
		return _items;
	}

	public Dictionary<EquipSlot, ItemInstance> GetAllEquippedUnique()
	{
		return _equippedUnique;
	}

	public HashSet<string> GetUnlockedRecipes()
	{
		return _unlockedRecipes;
	}

	public bool IsStoneDiscovered( string stoneId )
	{
		return _discoveredStones.Contains( stoneId );
	}

	public void DiscoverStone( string stoneId )
	{
		_discoveredStones.Add( stoneId );
	}

	public HashSet<string> GetDiscoveredStones()
	{
		return _discoveredStones;
	}

	public bool IsQuestCompleted( string questId )
	{
		return _completedQuests.Contains( questId );
	}

	public void CompleteQuest( string questId )
	{
		_completedQuests.Add( questId );
	}

	public HashSet<string> GetCompletedQuests()
	{
		return _completedQuests;
	}

	public int GetKillCount( string monsterType )
	{
		if ( _killCounts.TryGetValue( monsterType, out var count ) )
			return count;

		return 0;
	}

	public void AddKill( string monsterType )
	{
		if ( _killCounts.ContainsKey( monsterType ) )
			_killCounts[monsterType]++;
		else
			_killCounts[monsterType] = 1;
	}

	public Dictionary<string, int> GetAllKillCounts()
	{
		return _killCounts;
	}
}