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

	public const int BaseSlots = 30;
	public const int SlotsPerExpansion = 5;
	public const int HotbarSize = 5;

	int _expansions = 0;

	public int MaxSlots => BaseSlots + _expansions * SlotsPerExpansion;
	public int ExpansionCount => _expansions;

	List<InventorySlot> _slots = new();

	Dictionary<EquipSlot, int> _equippedSlotIndex = new();

	int _equippedAmmoSlotIndex = -1;

	HashSet<string> _unlockedRecipes = new();
	HashSet<string> _discoveredStones = new();
	HashSet<string> _completedQuests = new();
	HashSet<string> _discoveredQuests = new();
	Dictionary<string, int> _killCounts = new();
	Dictionary<string, string> _chestClaims = new();

	int _nodesMined = 0;

	bool _suppressUnequipSound = false;

	public class InventorySlot
	{
		public ItemId ItemId = ItemId.None;
		public int Count = 0;
		public ItemInstance Unique;

		public bool IsEmpty => ItemId == ItemId.None && Unique == null;
		public bool IsStack => Unique == null && ItemId != ItemId.None && Count > 0;
		public bool IsUnique => Unique != null;

		public void Clear()
		{
			ItemId = ItemId.None;
			Count = 0;
			Unique = null;
		}

		public InventorySlot Clone()
		{
			return new InventorySlot
			{
				ItemId = ItemId,
				Count = Count,
				Unique = Unique
			};
		}
	}

	protected override void OnStart()
	{
		InitializeDefaults();
	}

	void InitializeDefaults()
	{
		_slots.Clear();
		EnsureSlotCapacity();

		_equippedSlotIndex.Clear();
		_equippedAmmoSlotIndex = -1;

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
	}

	void EnsureSlotCapacity()
	{
		while ( _slots.Count < MaxSlots )
			_slots.Add( new InventorySlot() );
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

	public List<InventorySlot> GetSlots()
	{
		return _slots;
	}

	public InventorySlot GetSlot( int index )
	{
		if ( index < 0 || index >= _slots.Count )
			return null;

		return _slots[index];
	}

	public int CountEmptySlots()
	{
		int count = 0;
		for ( int i = 0; i < MaxSlots; i++ )
		{
			if ( _slots[i].IsEmpty )
				count++;
		}
		return count;
	}

	public bool HasEmptySlot()
	{
		return FindFirstEmptySlot() >= 0;
	}

	public bool CanFitStackable( ItemId id, int amount )
	{
		if ( id == ItemId.None || amount <= 0 )
			return true;

		var def = ItemDatabase.Get( id );
		int maxStack = def != null ? def.MaxStack : 999;
		if ( maxStack < 1 ) maxStack = 1;

		int remaining = amount;

		for ( int i = 0; i < MaxSlots && remaining > 0; i++ )
		{
			var slot = _slots[i];
			if ( slot.IsStack && slot.ItemId == id )
				remaining -= ( maxStack - slot.Count );
		}

		if ( remaining <= 0 )
			return true;

		int empties = CountEmptySlots();
		remaining -= empties * maxStack;

		return remaining <= 0;
	}

	public int GetItemCount( ItemId id )
	{
		if ( id == ItemId.None )
			return 0;

		int total = 0;

		if ( IsEquipmentItem( id ) )
		{
			for ( int i = 0; i < MaxSlots; i++ )
			{
				var slot = _slots[i];
				if ( slot.IsUnique && slot.Unique.ItemId == id && !slot.Unique.IsEnchanted )
					total++;
			}
			return total;
		}

		for ( int i = 0; i < MaxSlots; i++ )
		{
			var slot = _slots[i];
			if ( slot.IsStack && slot.ItemId == id )
				total += slot.Count;
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

		return TryPlaceItem( id, amount );
	}

	public (int placed, int banked) AddItemOrBank( ItemId id, int amount = 1 )
	{
		if ( id == ItemId.None || amount <= 0 )
			return (0, 0);

		int placed = TryPlaceItem( id, amount );
		int remaining = amount - placed;

		int banked = 0;
		if ( remaining > 0 )
		{
			var bank = Components.Get<BankStorage>();
			if ( bank != null )
			{
				if ( IsEquipmentItem( id ) )
				{
					for ( int i = 0; i < remaining; i++ )
						bank.DepositUnique( new ItemInstance( id ) );
				}
				else
				{
					bank.Deposit( id, remaining );
				}
				banked = remaining;
			}
		}

		return (placed, banked);
	}

	public bool AddUniqueItemOrBank( ItemInstance instance )
	{
		if ( instance == null )
			return false;

		int slotIndex = FindFirstEmptySlot();
		if ( slotIndex >= 0 )
		{
			_slots[slotIndex].Unique = instance;
			return true;
		}

		var bank = Components.Get<BankStorage>();
		if ( bank != null )
		{
			bank.DepositUnique( instance );
			return false;
		}

		return false;
	}

	int TryPlaceItem( ItemId id, int amount )
	{
		if ( IsEquipmentItem( id ) )
		{
			int placed = 0;
			while ( placed < amount )
			{
				int slotIndex = FindFirstEmptySlot();
				if ( slotIndex < 0 )
					break;

				_slots[slotIndex].Unique = new ItemInstance( id );
				placed++;
			}
			return placed;
		}

		var def = ItemDatabase.Get( id );
		int maxStack = def != null ? def.MaxStack : 999;
		if ( maxStack < 1 ) maxStack = 1;

		int remaining = amount;

		for ( int i = 0; i < MaxSlots && remaining > 0; i++ )
		{
			var slot = _slots[i];
			if ( !slot.IsStack || slot.ItemId != id )
				continue;
			if ( slot.Count >= maxStack )
				continue;

			int room = maxStack - slot.Count;
			int put = remaining < room ? remaining : room;
			slot.Count += put;
			remaining -= put;
		}

		while ( remaining > 0 )
		{
			int slotIndex = FindFirstEmptySlot();
			if ( slotIndex < 0 )
				break;

			int put = remaining < maxStack ? remaining : maxStack;
			_slots[slotIndex].ItemId = id;
			_slots[slotIndex].Count = put;
			remaining -= put;
		}

		return amount - remaining;
	}

	int FindFirstEmptySlot()
	{
		for ( int i = 0; i < MaxSlots; i++ )
		{
			if ( _slots[i].IsEmpty )
				return i;
		}
		return -1;
	}

	public bool RemoveItem( ItemId id, int amount = 1 )
	{
		if ( !HasItem( id, amount ) )
			return false;

		if ( IsEquipmentItem( id ) )
		{
			int removed = 0;
			for ( int i = MaxSlots - 1; i >= 0 && removed < amount; i-- )
			{
				var slot = _slots[i];
				if ( slot.IsUnique && slot.Unique.ItemId == id && !slot.Unique.IsEnchanted )
				{
					ClearSlotAndUpdateEquipped( i );
					removed++;
				}
			}
			return removed >= amount;
		}

		int remaining = amount;
		for ( int i = MaxSlots - 1; i >= 0 && remaining > 0; i-- )
		{
			var slot = _slots[i];
			if ( !slot.IsStack || slot.ItemId != id )
				continue;

			if ( slot.Count <= remaining )
			{
				remaining -= slot.Count;
				ClearSlotAndUpdateEquipped( i );
			}
			else
			{
				slot.Count -= remaining;
				remaining = 0;
			}
		}

		return true;
	}

	public bool RemoveFromSlot( int slotIndex, int amount )
	{
		if ( slotIndex < 0 || slotIndex >= MaxSlots || amount <= 0 )
			return false;

		var slot = _slots[slotIndex];

		if ( slot.IsUnique )
		{
			ClearSlotAndUpdateEquipped( slotIndex );
			return true;
		}

		if ( !slot.IsStack )
			return false;

		if ( slot.Count <= amount )
		{
			ClearSlotAndUpdateEquipped( slotIndex );
			return true;
		}

		slot.Count -= amount;
		return true;
	}

	void ClearSlotAndUpdateEquipped( int slotIndex )
	{
		var keysToRemove = new List<EquipSlot>();
		foreach ( var kv in _equippedSlotIndex )
		{
			if ( kv.Value == slotIndex )
				keysToRemove.Add( kv.Key );
		}

		foreach ( var key in keysToRemove )
		{
			_equippedSlotIndex.Remove( key );
			SyncEquippedSlot( key, ItemId.None );
		}

		if ( _equippedAmmoSlotIndex == slotIndex )
			_equippedAmmoSlotIndex = -1;

		_slots[slotIndex].Clear();
	}

	public bool SwapSlots( int indexA, int indexB )
	{
		if ( indexA == indexB )
			return false;
		if ( indexA < 0 || indexA >= MaxSlots )
			return false;
		if ( indexB < 0 || indexB >= MaxSlots )
			return false;

		var a = _slots[indexA];
		var b = _slots[indexB];

		if ( a.IsStack && b.IsStack && a.ItemId == b.ItemId )
		{
			var def = ItemDatabase.Get( a.ItemId );
			int maxStack = def != null ? def.MaxStack : 999;
			if ( maxStack < 1 ) maxStack = 1;

			int room = maxStack - b.Count;
			if ( room > 0 )
			{
				int move = a.Count < room ? a.Count : room;
				b.Count += move;
				a.Count -= move;

				if ( a.Count <= 0 )
					ClearSlotAndUpdateEquipped( indexA );

				return true;
			}
		}

		var temp = a.Clone();
		a.ItemId = b.ItemId;
		a.Count = b.Count;
		a.Unique = b.Unique;
		b.ItemId = temp.ItemId;
		b.Count = temp.Count;
		b.Unique = temp.Unique;

		UpdateEquippedIndexOnSwap( indexA, indexB );

		return true;
	}

	void UpdateEquippedIndexOnSwap( int indexA, int indexB )
	{
		var updates = new List<(EquipSlot key, int newValue)>();
		foreach ( var kv in _equippedSlotIndex )
		{
			if ( kv.Value == indexA )
				updates.Add( (kv.Key, indexB) );
			else if ( kv.Value == indexB )
				updates.Add( (kv.Key, indexA) );
		}

		foreach ( var u in updates )
			_equippedSlotIndex[u.key] = u.newValue;

		if ( _equippedAmmoSlotIndex == indexA )
			_equippedAmmoSlotIndex = indexB;
		else if ( _equippedAmmoSlotIndex == indexB )
			_equippedAmmoSlotIndex = indexA;
	}

	public bool ExpandInventory()
	{
		_expansions++;
		EnsureSlotCapacity();
		PlayerPersistence.Local?.RequestSaveNow();
		return true;
	}

	public IReadOnlyList<ItemStack> GetItemStacks()
	{
		var list = new List<ItemStack>();
		for ( int i = 0; i < MaxSlots; i++ )
		{
			var slot = _slots[i];
			if ( slot.IsStack )
				list.Add( new ItemStack { ItemId = slot.ItemId, Count = slot.Count } );
		}
		return list;
	}

	public void AddUniqueItem( ItemInstance instance )
	{
		AddUniqueItemOrBank( instance );
	}

	public void RemoveUniqueItem( int index )
	{
		var slotIndex = GetSlotIndexForUniqueByListIndex( index );
		if ( slotIndex < 0 )
			return;

		ClearSlotAndUpdateEquipped( slotIndex );
	}

	int GetSlotIndexForUniqueByListIndex( int listIndex )
	{
		int counter = 0;
		for ( int i = 0; i < MaxSlots; i++ )
		{
			if ( _slots[i].IsUnique )
			{
				if ( counter == listIndex )
					return i;
				counter++;
			}
		}
		return -1;
	}

	public List<ItemInstance> GetUniqueItems()
	{
		var list = new List<ItemInstance>();
		for ( int i = 0; i < MaxSlots; i++ )
		{
			if ( _slots[i].IsUnique )
				list.Add( _slots[i].Unique );
		}
		return list;
	}

	public int GetUniqueItemCount()
	{
		int count = 0;
		for ( int i = 0; i < MaxSlots; i++ )
		{
			if ( _slots[i].IsUnique )
				count++;
		}
		return count;
	}

	public int GetSlotIndexForUnique( ItemInstance instance )
	{
		if ( instance == null )
			return -1;

		for ( int i = 0; i < MaxSlots; i++ )
		{
			if ( _slots[i].Unique == instance )
				return i;
		}
		return -1;
	}

	public bool IsSlotEquipped( int slotIndex )
	{
		foreach ( var kv in _equippedSlotIndex )
		{
			if ( kv.Value == slotIndex )
				return true;
		}

		if ( _equippedAmmoSlotIndex == slotIndex )
			return true;

		return false;
	}

	public EquipSlot GetEquippedSlotTypeAt( int slotIndex )
	{
		foreach ( var kv in _equippedSlotIndex )
		{
			if ( kv.Value == slotIndex )
				return kv.Key;
		}

		if ( _equippedAmmoSlotIndex == slotIndex )
			return EquipSlot.Ammo;

		return EquipSlot.None;
	}

	public bool EquipAmmo( ItemId ammoId )
	{
		if ( ammoId == ItemId.None )
			return false;

		for ( int i = 0; i < MaxSlots; i++ )
		{
			var slot = _slots[i];
			if ( slot.IsStack && slot.ItemId == ammoId )
				return EquipAmmoFromSlot( i );
		}

		return false;
	}

	public bool EquipAmmoFromSlot( int slotIndex )
	{
		if ( slotIndex < 0 || slotIndex >= MaxSlots )
			return false;

		var slot = _slots[slotIndex];
		if ( !slot.IsStack )
			return false;

		var def = ItemDatabase.Get( slot.ItemId );
		if ( def == null || def.Type != ItemType.Arrow )
			return false;

		var skills = Components.Get<Skills>();
		if ( skills != null && !skills.CanEquip( def ) )
		{
			GameLog.Add( $"You need {def.SkillRequired} level {def.LevelRequired} to equip {def.Name}.", "#c86464" );
			return false;
		}

		_equippedAmmoSlotIndex = slotIndex;

		GameLog.Add( $"Equipped {slot.Count}x {def.Name}.", "#c9a84c" );
		SoundLibrary.PlayEquip();
		return true;
	}

	public bool UnequipAmmo()
	{
		if ( _equippedAmmoSlotIndex < 0 )
			return true;

		var slot = _slots[_equippedAmmoSlotIndex];
		var def = slot.IsStack ? ItemDatabase.Get( slot.ItemId ) : null;
		string name = def != null ? def.Name : "ammo";

		_equippedAmmoSlotIndex = -1;

		GameLog.Add( $"Unequipped {name}.", "#c9a84c" );

		if ( !_suppressUnequipSound )
			SoundLibrary.PlayEquip();

		return true;
	}

	public bool ConsumeAmmo( int amount = 1 )
	{
		if ( _equippedAmmoSlotIndex < 0 )
			return false;

		var slot = _slots[_equippedAmmoSlotIndex];
		if ( !slot.IsStack || slot.Count < amount )
			return false;

		slot.Count -= amount;

		if ( slot.Count <= 0 )
		{
			GameLog.Add( "You've run out of arrows!", "#c86464" );
			ClearSlotAndUpdateEquipped( _equippedAmmoSlotIndex );
		}

		return true;
	}

	public ItemId GetEquippedAmmoId()
	{
		if ( _equippedAmmoSlotIndex < 0 )
			return ItemId.None;

		var slot = _slots[_equippedAmmoSlotIndex];
		if ( !slot.IsStack )
			return ItemId.None;

		return slot.ItemId;
	}

	public int GetEquippedAmmoCount()
	{
		if ( _equippedAmmoSlotIndex < 0 )
			return 0;

		var slot = _slots[_equippedAmmoSlotIndex];
		if ( !slot.IsStack )
			return 0;

		return slot.Count;
	}

	public int GetEquippedAmmoSlotIndex()
	{
		return _equippedAmmoSlotIndex;
	}

	public bool EquipUnique( int index )
	{
		int slotIndex = GetSlotIndexForUniqueByListIndex( index );
		return EquipUniqueAtSlot( slotIndex );
	}

	public bool EquipUniqueAtSlot( int slotIndex )
	{
		if ( slotIndex < 0 || slotIndex >= MaxSlots )
			return false;

		var slot = _slots[slotIndex];
		if ( !slot.IsUnique )
			return false;

		var instance = slot.Unique;
		var def = ItemDatabase.Get( instance.ItemId );
		if ( def == null || def.Slot == EquipSlot.None )
			return false;

		var skills = Components.Get<Skills>();
		if ( skills != null && !skills.CanEquip( def ) )
		{
			GameLog.Add( $"You need {def.SkillRequired} level {def.LevelRequired} to equip {def.Name}.", "#c86464" );
			return false;
		}

		if ( _equippedSlotIndex.TryGetValue( def.Slot, out var previousSlotIndex ) && previousSlotIndex == slotIndex )
		{
			UnequipUnique( def.Slot );
			return true;
		}

		_equippedSlotIndex[def.Slot] = slotIndex;
		SyncEquippedSlot( def.Slot, instance.ItemId );

		GameLog.Add( $"Equipped {instance.GetDisplayName()}.", "#c9a84c" );
		SoundLibrary.PlayEquip();
		return true;
	}

	public bool UnequipUnique( EquipSlot equipSlot )
	{
		if ( !_equippedSlotIndex.ContainsKey( equipSlot ) )
			return true;

		int slotIndex = _equippedSlotIndex[equipSlot];
		_equippedSlotIndex.Remove( equipSlot );
		SyncEquippedSlot( equipSlot, ItemId.None );

		string name = "item";
		if ( slotIndex >= 0 && slotIndex < MaxSlots )
		{
			var slot = _slots[slotIndex];
			if ( slot.IsUnique )
				name = slot.Unique.GetDisplayName();
		}

		GameLog.Add( $"Unequipped {name}.", "#c9a84c" );

		if ( !_suppressUnequipSound )
			SoundLibrary.PlayEquip();

		return true;
	}

	public ItemInstance GetEquippedUnique( EquipSlot equipSlot )
	{
		if ( _equippedSlotIndex.TryGetValue( equipSlot, out var slotIndex ) )
		{
			if ( slotIndex >= 0 && slotIndex < MaxSlots )
			{
				var slot = _slots[slotIndex];
				if ( slot.IsUnique )
					return slot.Unique;
			}
		}

		var syncedId = GetEquipped( equipSlot );
		if ( syncedId != ItemId.None && equipSlot != EquipSlot.Ammo )
			return new ItemInstance( syncedId );

		return null;
	}

	public int GetEquippedSlotIndex( EquipSlot equipSlot )
	{
		if ( _equippedSlotIndex.TryGetValue( equipSlot, out var slotIndex ) )
			return slotIndex;

		return -1;
	}

	public void Unequip( EquipSlot equipSlot )
	{
		if ( equipSlot == EquipSlot.Ammo )
		{
			UnequipAmmo();
			return;
		}

		if ( _equippedSlotIndex.ContainsKey( equipSlot ) )
		{
			UnequipUnique( equipSlot );
			return;
		}
	}

	public void UnequipAll()
	{
		var equipSlots = new List<EquipSlot>( _equippedSlotIndex.Keys );
		foreach ( var slot in equipSlots )
			UnequipUnique( slot );

		UnequipAmmo();
	}

	public ItemId GetEquipped( EquipSlot equipSlot )
	{
		switch ( equipSlot )
		{
			case EquipSlot.Ammo: return GetEquippedAmmoId();
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

	void SyncEquippedSlot( EquipSlot equipSlot, ItemId id )
	{
		switch ( equipSlot )
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

		foreach ( var es in armorSlots )
		{
			var id = GetEquipped( es );
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
		var ammoId = GetEquippedAmmoId();
		if ( ammoId == ItemId.None )
			return 0f;

		var def = ItemDatabase.Get( ammoId );
		if ( def == null )
			return 0f;

		return def.WeaponPower;
	}

	public float GetEnchantmentBonus( EnchantmentType type )
	{
		float total = 0f;

		foreach ( var kv in _equippedSlotIndex )
		{
			int idx = kv.Value;
			if ( idx < 0 || idx >= MaxSlots )
				continue;

			var slot = _slots[idx];
			if ( !slot.IsUnique )
				continue;

			if ( slot.Unique.Enchantment == type )
				total += slot.Unique.EnchantmentPercent;
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
		for ( int i = 0; i < MaxSlots; i++ )
		{
			var slot = _slots[i];
			if ( !slot.IsStack )
				continue;

			if ( totals.TryGetValue( slot.ItemId, out var existing ) )
				totals[slot.ItemId] = existing + slot.Count;
			else
				totals[slot.ItemId] = slot.Count;
		}
		return totals;
	}

	public Dictionary<EquipSlot, ItemInstance> GetAllEquippedUnique()
	{
		var result = new Dictionary<EquipSlot, ItemInstance>();
		foreach ( var kv in _equippedSlotIndex )
		{
			int idx = kv.Value;
			if ( idx < 0 || idx >= MaxSlots )
				continue;

			var slot = _slots[idx];
			if ( slot.IsUnique )
				result[kv.Key] = slot.Unique;
		}
		return result;
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
		for ( int i = 0; i < MaxSlots; i++ )
		{
			var slot = _slots[i];
			if ( !slot.IsStack )
				continue;

			string key = slot.ItemId.ToString();
			if ( data.Stackables.TryGetValue( key, out var existing ) )
				data.Stackables[key] = existing + slot.Count;
			else
				data.Stackables[key] = slot.Count;
		}

		data.UniqueItems = new List<PlayerSaveData.UniqueItemEntry>();
		for ( int i = 0; i < MaxSlots; i++ )
		{
			var slot = _slots[i];
			if ( !slot.IsUnique )
				continue;

			data.UniqueItems.Add( new PlayerSaveData.UniqueItemEntry
			{
				ItemId = slot.Unique.ItemId.ToString(),
				Enchantment = slot.Unique.Enchantment.ToString(),
				EnchantmentPercent = slot.Unique.EnchantmentPercent
			} );
		}

		data.Equipped = new Dictionary<string, PlayerSaveData.UniqueItemEntry>();
		data.EquippedSlotIndices = new Dictionary<string, int>();
		foreach ( var kv in _equippedSlotIndex )
		{
			int idx = kv.Value;
			if ( idx < 0 || idx >= MaxSlots )
				continue;

			var slot = _slots[idx];
			if ( !slot.IsUnique )
				continue;

			data.Equipped[kv.Key.ToString()] = new PlayerSaveData.UniqueItemEntry
			{
				ItemId = slot.Unique.ItemId.ToString(),
				Enchantment = slot.Unique.Enchantment.ToString(),
				EnchantmentPercent = slot.Unique.EnchantmentPercent
			};

			data.EquippedSlotIndices[kv.Key.ToString()] = idx + 1;
		}

		var ammoId = GetEquippedAmmoId();
		var ammoCount = GetEquippedAmmoCount();
		data.EquippedAmmoId = ammoId.ToString();
		data.EquippedAmmoQty = ammoCount;
		data.EquippedAmmoSlotIndex = _equippedAmmoSlotIndex >= 0 ? _equippedAmmoSlotIndex + 1 : 0;

		data.Recipes = new List<string>( _unlockedRecipes );
		data.Stones = new List<string>( _discoveredStones );
		data.Quests = new List<string>( _completedQuests );
		data.DiscoveredQuests = new List<string>( _discoveredQuests );
		data.Kills = new Dictionary<string, int>( _killCounts );
		data.ChestClaims = new Dictionary<string, string>( _chestClaims );

		data.NodesMined = _nodesMined;

		data.Slots = new List<PlayerSaveData.InventorySlotEntry>();
		for ( int i = 0; i < MaxSlots; i++ )
		{
			var slot = _slots[i];
			if ( slot.IsEmpty )
				continue;

			var entry = new PlayerSaveData.InventorySlotEntry
			{
				Slot = i + 1
			};

			if ( slot.IsUnique )
			{
				entry.IsUnique = true;
				entry.ItemId = slot.Unique.ItemId.ToString();
				entry.Enchantment = slot.Unique.Enchantment.ToString();
				entry.EnchantmentPercent = slot.Unique.EnchantmentPercent;
				entry.Count = 1;
			}
			else
			{
				entry.IsUnique = false;
				entry.ItemId = slot.ItemId.ToString();
				entry.Count = slot.Count;
				entry.Enchantment = "None";
				entry.EnchantmentPercent = 0f;
			}

			data.Slots.Add( entry );
		}

		data.InventoryExpansions = _expansions;

		return data;
	}

	public void ApplySaveData( PlayerSaveData data )
	{
		InitializeDefaults();

		if ( data == null )
			return;

		_expansions = data.InventoryExpansions;
		EnsureSlotCapacity();

		bool hasSlotData = data.Slots != null && data.Slots.Count > 0;

		if ( hasSlotData )
		{
			foreach ( var entry in data.Slots )
			{
				int idx = entry.Slot - 1;
				if ( idx < 0 || idx >= MaxSlots )
					continue;

				if ( !System.Enum.TryParse<ItemId>( entry.ItemId, out var id ) )
					continue;
				if ( id == ItemId.None )
					continue;

				if ( entry.IsUnique )
				{
					var enchant = EnchantmentType.None;
					System.Enum.TryParse<EnchantmentType>( entry.Enchantment, out enchant );

					_slots[idx].Unique = new ItemInstance( id, enchant, entry.EnchantmentPercent );
				}
				else
				{
					int count = entry.Count > 0 ? entry.Count : 1;
					_slots[idx].ItemId = id;
					_slots[idx].Count = count;
				}
			}
		}
		else
		{
			foreach ( var kv in data.Stackables )
			{
				if ( !System.Enum.TryParse<ItemId>( kv.Key, out var id ) )
					continue;
				if ( id == ItemId.None )
					continue;

				TryPlaceItem( id, kv.Value );
			}

			foreach ( var entry in data.UniqueItems )
			{
				if ( !System.Enum.TryParse<ItemId>( entry.ItemId, out var id ) )
					continue;
				if ( id == ItemId.None )
					continue;

				var enchant = EnchantmentType.None;
				System.Enum.TryParse<EnchantmentType>( entry.Enchantment, out enchant );

				int slotIndex = FindFirstEmptySlot();
				if ( slotIndex < 0 )
					break;

				_slots[slotIndex].Unique = new ItemInstance( id, enchant, entry.EnchantmentPercent );
			}

			if ( !string.IsNullOrEmpty( data.EquippedAmmoId ) && data.EquippedAmmoQty > 0 )
			{
				if ( System.Enum.TryParse<ItemId>( data.EquippedAmmoId, out var ammoId ) && ammoId != ItemId.None )
				{
					int ammoSlot = FindFirstEmptySlot();
					if ( ammoSlot >= 0 )
					{
						_slots[ammoSlot].ItemId = ammoId;
						_slots[ammoSlot].Count = data.EquippedAmmoQty;
					}
				}
			}
		}

		bool hasNewEquipped = data.EquippedSlotIndices != null && data.EquippedSlotIndices.Count > 0;

		if ( hasNewEquipped )
		{
			foreach ( var kv in data.EquippedSlotIndices )
			{
				if ( !System.Enum.TryParse<EquipSlot>( kv.Key, out var equipSlot ) )
					continue;

				int idx = kv.Value - 1;
				if ( idx < 0 || idx >= MaxSlots )
					continue;

				var slot = _slots[idx];
				if ( !slot.IsUnique )
					continue;

				_equippedSlotIndex[equipSlot] = idx;
				SyncEquippedSlot( equipSlot, slot.Unique.ItemId );
			}
		}
		else if ( data.Equipped != null )
		{
			foreach ( var kv in data.Equipped )
			{
				if ( !System.Enum.TryParse<EquipSlot>( kv.Key, out var equipSlot ) )
					continue;
				if ( !System.Enum.TryParse<ItemId>( kv.Value.ItemId, out var id ) )
					continue;
				if ( id == ItemId.None )
					continue;

				var enchant = EnchantmentType.None;
				System.Enum.TryParse<EnchantmentType>( kv.Value.Enchantment, out enchant );

				int foundSlot = -1;
				for ( int i = 0; i < MaxSlots; i++ )
				{
					var s = _slots[i];
					if ( !s.IsUnique )
						continue;
					if ( s.Unique.ItemId != id )
						continue;
					if ( s.Unique.Enchantment != enchant )
						continue;
					if ( System.MathF.Abs( s.Unique.EnchantmentPercent - kv.Value.EnchantmentPercent ) > 0.01f )
						continue;

					foundSlot = i;
					break;
				}

				if ( foundSlot < 0 )
				{
					int empty = FindFirstEmptySlot();
					if ( empty >= 0 )
					{
						_slots[empty].Unique = new ItemInstance( id, enchant, kv.Value.EnchantmentPercent );
						foundSlot = empty;
					}
				}

				if ( foundSlot >= 0 )
				{
					_equippedSlotIndex[equipSlot] = foundSlot;
					SyncEquippedSlot( equipSlot, id );
				}
			}
		}

		if ( data.EquippedAmmoSlotIndex > 0 )
		{
			int idx = data.EquippedAmmoSlotIndex - 1;
			if ( idx >= 0 && idx < MaxSlots && _slots[idx].IsStack )
				_equippedAmmoSlotIndex = idx;
		}
		else if ( !string.IsNullOrEmpty( data.EquippedAmmoId ) && data.EquippedAmmoQty > 0 )
		{
			if ( System.Enum.TryParse<ItemId>( data.EquippedAmmoId, out var ammoId ) && ammoId != ItemId.None )
			{
				for ( int i = 0; i < MaxSlots; i++ )
				{
					var s = _slots[i];
					if ( s.IsStack && s.ItemId == ammoId )
					{
						_equippedAmmoSlotIndex = i;
						break;
					}
				}
			}
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