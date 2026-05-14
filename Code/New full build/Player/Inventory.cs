using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class Inventory : Component
{
	[Sync] public ItemId EquippedWeapon { get; set; } = ItemId.None;
	[Sync] public ItemId EquippedShield { get; set; } = ItemId.None;
	[Sync] public ItemId EquippedHead { get; set; } = ItemId.None;
	[Sync] public ItemId EquippedChest { get; set; } = ItemId.None;
	[Sync] public ItemId EquippedLegs { get; set; } = ItemId.None;
	[Sync] public ItemId EquippedRing { get; set; } = ItemId.None;
	[Sync] public ItemId EquippedAmulet { get; set; } = ItemId.None;

	List<ItemStack> _itemStacks = new();
	List<ItemInstance> _uniqueItems = new();
	Dictionary<EquipSlot, ItemInstance> _equippedUnique = new();
	HashSet<string> _unlockedRecipes = new();
	HashSet<string> _discoveredStones = new();
	HashSet<string> _completedQuests = new();
	HashSet<string> _discoveredQuests = new();
	Dictionary<string, int> _killCounts = new();
	Dictionary<string, string> _chestClaims = new();

	int _nodesMined = 0;

	ItemId _equippedAmmoId = ItemId.None;
	int _equippedAmmoCount = 0;

	bool _suppressUnequipSound = false;

	protected override void OnStart()
	{
		InitializeDefaults();
	}

	void InitializeDefaults()
	{
		_itemStacks.Clear();
		_uniqueItems.Clear();
		_equippedUnique.Clear();

		if ( !IsProxy )
		{
			EquippedWeapon = ItemId.None;
			EquippedShield = ItemId.None;
			EquippedHead = ItemId.None;
			EquippedChest = ItemId.None;
			EquippedLegs = ItemId.None;
			EquippedRing = ItemId.None;
			EquippedAmulet = ItemId.None;
		}

		_unlockedRecipes.Clear();
		_discoveredStones.Clear();
		_completedQuests.Clear();
		_discoveredQuests.Clear();
		_killCounts.Clear();
		_chestClaims.Clear();
		_nodesMined = 0;
		_equippedAmmoId = ItemId.None;
		_equippedAmmoCount = 0;
	}

	public void GrantStarterKit()
	{
		AddItem( ItemId.PrimitiveHatchet, 1 );
		AddItem( ItemId.PrimitivePickaxe, 1 );
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

		int total = 0;
		foreach ( var stack in _itemStacks )
		{
			if ( stack.ItemId == id )
				total += stack.Count;
		}
		return total;
	}

	public bool HasItem( ItemId id, int amount = 1 )
	{
		return GetItemCount( id ) >= amount;
	}

	public int AddItem( ItemId id, int amount = 1 )
	{
		if ( id == ItemId.None || amount <= 0 )
			return 0;

		if ( IsEquipmentItem( id ) )
		{
			for ( int i = 0; i < amount; i++ )
				_uniqueItems.Add( new ItemInstance( id ) );

			PlayerPersistence.Local?.RequestSaveNow();
			return amount;
		}

		var def = ItemDatabase.Get( id );
		int maxStack = def != null ? def.MaxStack : 999;
		if ( maxStack < 1 ) maxStack = 1;

		int remaining = amount;

		for ( int i = 0; i < _itemStacks.Count && remaining > 0; i++ )
		{
			var stack = _itemStacks[i];
			if ( stack.ItemId != id )
				continue;
			if ( stack.Count >= maxStack )
				continue;

			int room = maxStack - stack.Count;
			int put = remaining < room ? remaining : room;
			stack.Count += put;
			remaining -= put;
		}

		while ( remaining > 0 )
		{
			int put = remaining < maxStack ? remaining : maxStack;
			_itemStacks.Add( new ItemStack { ItemId = id, Count = put } );
			remaining -= put;
		}

		return amount;
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

		int remaining = amount;

		for ( int i = _itemStacks.Count - 1; i >= 0 && remaining > 0; i-- )
		{
			var stack = _itemStacks[i];
			if ( stack.ItemId != id )
				continue;

			if ( stack.Count <= remaining )
			{
				remaining -= stack.Count;
				_itemStacks.RemoveAt( i );
			}
			else
			{
				stack.Count -= remaining;
				remaining = 0;
			}
		}

		return true;
	}

	public IReadOnlyList<ItemStack> GetItemStacks()
	{
		return _itemStacks;
	}

	public void AddUniqueItem( ItemInstance instance )
	{
		_uniqueItems.Add( instance );
		PlayerPersistence.Local?.RequestSaveNow();
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

		int count = GetItemCount( ammoId );
		if ( count <= 0 )
			return false;

		var skills = Components.Get<Skills>();
		if ( skills != null && !skills.CanEquip( def ) )
		{
			GameLog.Add( $"You need {def.SkillRequired} level {def.LevelRequired} to equip {def.Name}.", "#c86464" );
			return false;
		}

		if ( _equippedAmmoId == ammoId )
		{
			_equippedAmmoCount += count;
			RemoveItem( ammoId, count );

			GameLog.Add( $"Equipped {count}x {def.Name}.", "#c9a84c" );
			SoundLibrary.PlayEquip();
			return true;
		}

		if ( _equippedAmmoId != ItemId.None )
		{
			_suppressUnequipSound = true;
			UnequipAmmo();
			_suppressUnequipSound = false;
		}

		count = GetItemCount( ammoId );
		if ( count <= 0 )
			return false;

		_equippedAmmoId = ammoId;
		_equippedAmmoCount = count;
		RemoveItem( ammoId, count );

		GameLog.Add( $"Equipped {count}x {def.Name}.", "#c9a84c" );
		SoundLibrary.PlayEquip();
		return true;
	}

	public void UnequipAmmo()
	{
		if ( _equippedAmmoId == ItemId.None )
			return;

		if ( _equippedAmmoCount > 0 )
		{
			AddItem( _equippedAmmoId, _equippedAmmoCount );
		}

		var def = ItemDatabase.Get( _equippedAmmoId );
		string name = def != null ? def.Name : _equippedAmmoId.ToString();
		GameLog.Add( $"Unequipped {name}.", "#c9a84c" );

		_equippedAmmoId = ItemId.None;
		_equippedAmmoCount = 0;

		if ( !_suppressUnequipSound )
			SoundLibrary.PlayEquip();
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
		SyncEquippedSlot( def.Slot, instance.ItemId );

		GameLog.Add( $"Equipped {instance.GetDisplayName()}.", "#c9a84c" );
		SoundLibrary.PlayEquip();
		return true;
	}

	public void UnequipUnique( EquipSlot slot )
	{
		if ( !_equippedUnique.ContainsKey( slot ) )
			return;

		var instance = _equippedUnique[slot];
		_equippedUnique.Remove( slot );
		SyncEquippedSlot( slot, ItemId.None );
		_uniqueItems.Add( instance );

		GameLog.Add( $"Unequipped {instance.GetDisplayName()}.", "#c9a84c" );

		if ( !_suppressUnequipSound )
			SoundLibrary.PlayEquip();
	}

	public ItemInstance GetEquippedUnique( EquipSlot slot )
	{
		if ( _equippedUnique.TryGetValue( slot, out var instance ) )
			return instance;

		var syncedId = GetEquipped( slot );
		if ( syncedId != ItemId.None && slot != EquipSlot.Ammo )
			return new ItemInstance( syncedId );

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
		switch ( slot )
		{
			case EquipSlot.Ammo: return _equippedAmmoId;
			case EquipSlot.Weapon: return EquippedWeapon;
			case EquipSlot.Shield: return EquippedShield;
			case EquipSlot.Head: return EquippedHead;
			case EquipSlot.Chest: return EquippedChest;
			case EquipSlot.Legs: return EquippedLegs;
			case EquipSlot.Ring: return EquippedRing;
			case EquipSlot.Amulet: return EquippedAmulet;
			default: return ItemId.None;
		}
	}

	void SyncEquippedSlot( EquipSlot slot, ItemId id )
	{
		switch ( slot )
		{
			case EquipSlot.Weapon: EquippedWeapon = id; break;
			case EquipSlot.Shield: EquippedShield = id; break;
			case EquipSlot.Head: EquippedHead = id; break;
			case EquipSlot.Chest: EquippedChest = id; break;
			case EquipSlot.Legs: EquippedLegs = id; break;
			case EquipSlot.Ring: EquippedRing = id; break;
			case EquipSlot.Amulet: EquippedAmulet = id; break;
		}
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
		if ( _unlockedRecipes.Add( recipeId ) )
		{
			Log.Info( $"[Inventory] Recipe unlocked: {recipeId}" );
			PlayerPersistence.Local?.RequestSaveNow();
		}
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
		var totals = new Dictionary<ItemId, int>();
		foreach ( var stack in _itemStacks )
		{
			if ( totals.TryGetValue( stack.ItemId, out var existing ) )
				totals[stack.ItemId] = existing + stack.Count;
			else
				totals[stack.ItemId] = stack.Count;
		}
		return totals;
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
		if ( _completedQuests.Add( questId ) )
		{
			Log.Info( $"[Inventory] Quest completed: {questId}" );
			PlayerPersistence.Local?.RequestSaveNow();
		}
	}

	public HashSet<string> GetCompletedQuests()
	{
		return _completedQuests;
	}

	public void DiscoverQuest( string questId )
	{
		if ( string.IsNullOrEmpty( questId ) )
			return;

		if ( _discoveredQuests.Add( questId ) )
		{
			Log.Info( $"[Inventory] Quest discovered: {questId}" );
			PlayerPersistence.Local?.RequestSaveNow();
		}
	}

	public bool IsQuestDiscovered( string questId )
	{
		return _discoveredQuests.Contains( questId );
	}

	public HashSet<string> GetDiscoveredQuests()
	{
		return _discoveredQuests;
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

	public int GetTotalKills()
	{
		int total = 0;
		foreach ( var kv in _killCounts )
			total += kv.Value;
		return total;
	}

	public int GetNodesMined()
	{
		return _nodesMined;
	}

	public void AddNodeMined()
	{
		_nodesMined++;
	}

	public bool IsChestOnCooldown( string chestId, float cooldownHours )
	{
		return GetChestCooldownHoursRemaining( chestId, cooldownHours ) > 0f;
	}

	public float GetChestCooldownHoursRemaining( string chestId, float cooldownHours )
	{
		if ( string.IsNullOrEmpty( chestId ) )
			return 0f;

		if ( !_chestClaims.TryGetValue( chestId, out var claimedAt ) )
			return 0f;

		if ( !System.DateTime.TryParse( claimedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var claimedTime ) )
			return 0f;

		var elapsed = System.DateTime.UtcNow - claimedTime.ToUniversalTime();
		float elapsedHours = (float)elapsed.TotalHours;
		float remaining = cooldownHours - elapsedHours;

		return remaining > 0f ? remaining : 0f;
	}

	public void MarkChestClaimed( string chestId )
	{
		if ( string.IsNullOrEmpty( chestId ) )
			return;

		_chestClaims[chestId] = System.DateTime.UtcNow.ToString( "o" );
	}

	public Dictionary<string, string> GetChestClaims()
	{
		return _chestClaims;
	}

	public PlayerSaveData ToSaveData( PlayerSaveData data )
	{
		data.Stackables = new Dictionary<string, int>();
		foreach ( var stack in _itemStacks )
		{
			string key = stack.ItemId.ToString();
			if ( data.Stackables.TryGetValue( key, out var existing ) )
				data.Stackables[key] = existing + stack.Count;
			else
				data.Stackables[key] = stack.Count;
		}

		data.UniqueItems = new List<PlayerSaveData.UniqueItemEntry>();
		foreach ( var item in _uniqueItems )
		{
			data.UniqueItems.Add( new PlayerSaveData.UniqueItemEntry
			{
				ItemId = item.ItemId.ToString(),
				Enchantment = item.Enchantment.ToString(),
				EnchantmentPercent = item.EnchantmentPercent
			} );
		}

		data.Equipped = new Dictionary<string, PlayerSaveData.UniqueItemEntry>();
		foreach ( var kv in _equippedUnique )
		{
			data.Equipped[kv.Key.ToString()] = new PlayerSaveData.UniqueItemEntry
			{
				ItemId = kv.Value.ItemId.ToString(),
				Enchantment = kv.Value.Enchantment.ToString(),
				EnchantmentPercent = kv.Value.EnchantmentPercent
			};
		}

		data.EquippedAmmoId = _equippedAmmoId.ToString();
		data.EquippedAmmoQty = _equippedAmmoCount;

		data.Recipes = new List<string>( _unlockedRecipes );
		data.Stones = new List<string>( _discoveredStones );
		data.Quests = new List<string>( _completedQuests );
		data.DiscoveredQuests = new List<string>( _discoveredQuests );
		data.Kills = new Dictionary<string, int>( _killCounts );
		data.ChestClaims = new Dictionary<string, string>( _chestClaims );

		data.NodesMined = _nodesMined;

		return data;
	}

	public void ApplySaveData( PlayerSaveData data )
	{
		InitializeDefaults();

		if ( data == null )
			return;

		foreach ( var kv in data.Stackables )
		{
			if ( !System.Enum.TryParse<ItemId>( kv.Key, out var id ) )
				continue;
			if ( id == ItemId.None )
				continue;

			AddItem( id, kv.Value );
		}

		foreach ( var entry in data.UniqueItems )
		{
			if ( !System.Enum.TryParse<ItemId>( entry.ItemId, out var id ) )
				continue;
			if ( id == ItemId.None )
				continue;

			var enchant = EnchantmentType.None;
			System.Enum.TryParse<EnchantmentType>( entry.Enchantment, out enchant );

			_uniqueItems.Add( new ItemInstance( id, enchant, entry.EnchantmentPercent ) );
		}

		foreach ( var kv in data.Equipped )
		{
			if ( !System.Enum.TryParse<EquipSlot>( kv.Key, out var slot ) )
				continue;
			if ( !System.Enum.TryParse<ItemId>( kv.Value.ItemId, out var id ) )
				continue;
			if ( id == ItemId.None )
				continue;

			var enchant = EnchantmentType.None;
			System.Enum.TryParse<EnchantmentType>( kv.Value.Enchantment, out enchant );

			_equippedUnique[slot] = new ItemInstance( id, enchant, kv.Value.EnchantmentPercent );
			SyncEquippedSlot( slot, id );
		}

		if ( System.Enum.TryParse<ItemId>( data.EquippedAmmoId, out var ammoId ) )
		{
			_equippedAmmoId = ammoId;
			_equippedAmmoCount = data.EquippedAmmoQty;
		}

		foreach ( var r in data.Recipes )
			_unlockedRecipes.Add( r );

		foreach ( var s in data.Stones )
			_discoveredStones.Add( s );

		foreach ( var q in data.Quests )
			_completedQuests.Add( q );

		if ( data.DiscoveredQuests != null )
		{
			foreach ( var q in data.DiscoveredQuests )
				_discoveredQuests.Add( q );
		}

		foreach ( var kv in data.Kills )
			_killCounts[kv.Key] = kv.Value;

		if ( data.ChestClaims != null )
		{
			foreach ( var kv in data.ChestClaims )
				_chestClaims[kv.Key] = kv.Value;
		}

		_nodesMined = data.NodesMined;
	}
}

public class ItemStack
{
	public ItemId ItemId;
	public int Count;
}

public enum SortMode
{
	ByType,
	ByName,
	ByTier,
	ByAmount
}