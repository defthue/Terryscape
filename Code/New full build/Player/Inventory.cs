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

	Dictionary<EquipSlot, ItemInstance> _equipped = new();
	Dictionary<EquipSlot, int> _equippedOrigin = new();

	ItemId _equippedAmmoId = ItemId.None;
	int _equippedAmmoCount = 0;

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

		_equipped.Clear();
		_equippedOrigin.Clear();
		_equippedAmmoId = ItemId.None;
		_equippedAmmoCount = 0;

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

	public void RepairEquipmentStacks()
	{
		for ( int i = 0; i < MaxSlots; i++ )
		{
			var slot = _slots[i];
			if ( slot.IsStack && IsEquipmentItem( slot.ItemId ) )
			{
				var id = slot.ItemId;
				slot.Clear();
				slot.Unique = new ItemInstance( id );
			}
		}
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

	int CountEmptySlotsFrom( int startIndex )
	{
		int count = 0;
		for ( int i = startIndex; i < MaxSlots; i++ )
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

	public bool HasNonHotbarEmptySlot()
	{
		return FindFirstEmptySlotFrom( HotbarSize ) >= 0;
	}

	public bool CanFitStackable( ItemId id, int amount, bool hotbarProtected = true )
	{
		if ( id == ItemId.None || amount <= 0 )
			return true;

		var def = ItemDatabase.Get( id );
		int maxStack = def != null ? def.MaxStack : 1000;
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

		int empties = hotbarProtected ? CountEmptySlotsFrom( HotbarSize ) : CountEmptySlots();
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

		int placed = TryPlaceItem( id, amount );
		if ( placed > 0 )
			PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory | SaveSection.Stats );
		return placed;
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
				SoundLibrary.PlaySendToBank();
			}
		}

		if ( placed > 0 )
			PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory | SaveSection.Stats );

		return (placed, banked);
	}

	public bool AddUniqueItemOrBank( ItemInstance instance )
	{
		if ( instance == null )
			return false;

		int slotIndex = FindFirstEmptySlotFrom( HotbarSize );
		if ( slotIndex >= 0 )
		{
			_slots[slotIndex].Unique = instance;
			PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory | SaveSection.Stats );
			return true;
		}

		var bank = Components.Get<BankStorage>();
		if ( bank != null )
		{
			bank.DepositUnique( instance );
			SoundLibrary.PlaySendToBank();
			return false;
		}

		return false;
	}

	int TryPlaceItem( ItemId id, int amount, bool hotbarProtected = true )
	{
		if ( IsEquipmentItem( id ) )
		{
			int placed = 0;
			while ( placed < amount )
			{
				int slotIndex = hotbarProtected ? FindFirstEmptySlotFrom( HotbarSize ) : FindFirstEmptySlot();
				if ( slotIndex < 0 )
					break;

				_slots[slotIndex].Unique = new ItemInstance( id );
				placed++;
			}
			return placed;
		}

		var def = ItemDatabase.Get( id );
		int maxStack = def != null ? def.MaxStack : 1000;
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
			int slotIndex = hotbarProtected ? FindFirstEmptySlotFrom( HotbarSize ) : FindFirstEmptySlot();
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

	int FindFirstEmptySlotFrom( int startIndex )
	{
		for ( int i = startIndex; i < MaxSlots; i++ )
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
					ClearSlot( i );
					removed++;
				}
			}
			if ( removed > 0 )
				PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory | SaveSection.Stats );
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
				ClearSlot( i );
			}
			else
			{
				slot.Count -= remaining;
				remaining = 0;
			}
		}

		PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory | SaveSection.Stats );
		return true;
	}

	public bool RemoveFromSlot( int slotIndex, int amount )
	{
		if ( slotIndex < 0 || slotIndex >= MaxSlots || amount <= 0 )
			return false;

		var slot = _slots[slotIndex];

		if ( slot.IsUnique )
		{
			ClearSlot( slotIndex );
			PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory | SaveSection.Stats );
			return true;
		}

		if ( !slot.IsStack )
			return false;

		if ( slot.Count <= amount )
		{
			ClearSlot( slotIndex );
			PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory | SaveSection.Stats );
			return true;
		}

		slot.Count -= amount;
		PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory | SaveSection.Stats );
		return true;
	}

	void ClearSlot( int slotIndex )
	{
		if ( slotIndex < 0 || slotIndex >= _slots.Count )
			return;

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
			int maxStack = def != null ? def.MaxStack : 1000;
			if ( maxStack < 1 ) maxStack = 1;

			int room = maxStack - b.Count;
			if ( room > 0 )
			{
				int move = a.Count < room ? a.Count : room;
				b.Count += move;
				a.Count -= move;

				if ( a.Count <= 0 )
					ClearSlot( indexA );

				PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory );
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

		UpdateOriginOnSwap( indexA, indexB );

		PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory );
		return true;
	}

	void UpdateOriginOnSwap( int indexA, int indexB )
	{
		var updates = new List<(EquipSlot key, int newValue)>();
		foreach ( var kv in _equippedOrigin )
		{
			if ( kv.Value == indexA )
				updates.Add( (kv.Key, indexB) );
			else if ( kv.Value == indexB )
				updates.Add( (kv.Key, indexA) );
		}

		foreach ( var u in updates )
			_equippedOrigin[u.key] = u.newValue;
	}

	public bool ExpandInventory()
	{
		_expansions++;
		EnsureSlotCapacity();
		PlayerPersistence.Local?.MarkDirty( SaveSection.Stats );
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

		ClearSlot( slotIndex );
		PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory | SaveSection.Stats );
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
		return false;
	}

	public EquipSlot GetEquippedSlotTypeAt( int slotIndex )
	{
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
			GameLog.RequestFocusAllTab();
			SoundLibrary.PlayCantUse();
			return false;
		}

		var newAmmoId = slot.ItemId;
		var newAmmoCount = slot.Count;

		var oldAmmoId = _equippedAmmoId;
		var oldAmmoCount = _equippedAmmoCount;

		ClearSlot( slotIndex );

		_equippedAmmoId = newAmmoId;
		_equippedAmmoCount = newAmmoCount;

		if ( oldAmmoId != ItemId.None && oldAmmoCount > 0 )
			TryPlaceItem( oldAmmoId, oldAmmoCount, false );

		GameLog.Add( $"Equipped {newAmmoCount}x {def.Name}.", "#c9a84c" );
		SoundLibrary.PlayEquip();
		PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory | SaveSection.Stats );
		return true;
	}

	public bool CanUnequipAmmoToInventory()
	{
		if ( _equippedAmmoId == ItemId.None )
			return true;

		return CanFitStackable( _equippedAmmoId, _equippedAmmoCount, false );
	}

	public bool UnequipAmmo()
	{
		if ( _equippedAmmoId == ItemId.None )
			return true;

		if ( !CanFitStackable( _equippedAmmoId, _equippedAmmoCount, false ) )
			return false;

		var def = ItemDatabase.Get( _equippedAmmoId );
		string name = def != null ? def.Name : "ammo";

		TryPlaceItem( _equippedAmmoId, _equippedAmmoCount, false );

		_equippedAmmoId = ItemId.None;
		_equippedAmmoCount = 0;

		GameLog.Add( $"Unequipped {name}.", "#c9a84c" );

		if ( !_suppressUnequipSound )
			SoundLibrary.PlayEquip();

		PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory | SaveSection.Stats );
		return true;
	}

	public bool UnequipAmmoToBank()
	{
		if ( _equippedAmmoId == ItemId.None )
			return true;

		var bank = Components.Get<BankStorage>();
		if ( bank == null )
			return false;

		var def = ItemDatabase.Get( _equippedAmmoId );
		string name = def != null ? def.Name : "ammo";

		bank.Deposit( _equippedAmmoId, _equippedAmmoCount );
		SoundLibrary.PlaySendToBank();

		_equippedAmmoId = ItemId.None;
		_equippedAmmoCount = 0;

		GameLog.Add( $"Sent {name} to your bank.", "#c9a84c" );
		PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory | SaveSection.Stats );
		return true;
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

		PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory | SaveSection.Stats );
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

	public int GetEquippedAmmoSlotIndex()
	{
		return -1;
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
			GameLog.RequestFocusAllTab();
			SoundLibrary.PlayCantUse();
			return false;
		}

		ItemInstance previous = null;
		_equipped.TryGetValue( def.Slot, out previous );

		int previousOrigin = -1;
		_equippedOrigin.TryGetValue( def.Slot, out previousOrigin );

		ClearSlot( slotIndex );

		_equipped[def.Slot] = instance;
		_equippedOrigin[def.Slot] = slotIndex;
		SyncEquippedSlot( def.Slot, instance.ItemId );

		if ( previous != null )
		{
			int dest = ( previousOrigin >= 0 && previousOrigin < MaxSlots && _slots[previousOrigin].IsEmpty )
				? previousOrigin
				: FindFirstEmptySlot();

			if ( dest >= 0 )
				_slots[dest].Unique = previous;
		}

		GameLog.Add( $"Equipped {instance.GetDisplayName()}.", "#c9a84c" );
		SoundLibrary.PlayEquip();
		PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory );
		return true;
	}

	public bool UnequipUnique( EquipSlot equipSlot )
	{
		if ( !_equipped.TryGetValue( equipSlot, out var instance ) || instance == null )
			return true;

		int origin = -1;
		_equippedOrigin.TryGetValue( equipSlot, out origin );

		int dest = ( origin >= 0 && origin < MaxSlots && _slots[origin].IsEmpty )
			? origin
			: FindFirstEmptySlot();

		if ( dest < 0 )
			return false;

		_slots[dest].Unique = instance;

		_equipped.Remove( equipSlot );
		_equippedOrigin.Remove( equipSlot );
		SyncEquippedSlot( equipSlot, ItemId.None );

		GameLog.Add( $"Unequipped {instance.GetDisplayName()}.", "#c9a84c" );

		if ( !_suppressUnequipSound )
			SoundLibrary.PlayEquip();

		PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory );
		return true;
	}

	public bool UnequipUniqueToBank( EquipSlot equipSlot )
	{
		if ( !_equipped.TryGetValue( equipSlot, out var instance ) || instance == null )
			return true;

		var bank = Components.Get<BankStorage>();
		if ( bank == null )
			return false;

		bank.DepositUnique( instance );
		SoundLibrary.PlaySendToBank();

		_equipped.Remove( equipSlot );
		_equippedOrigin.Remove( equipSlot );
		SyncEquippedSlot( equipSlot, ItemId.None );

		GameLog.Add( $"Sent {instance.GetDisplayName()} to your bank.", "#c9a84c" );
		PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory );
		return true;
	}

	public ItemInstance GetEquippedUnique( EquipSlot equipSlot )
	{
		if ( _equipped.TryGetValue( equipSlot, out var instance ) && instance != null )
			return instance;

		var syncedId = GetEquipped( equipSlot );
		if ( syncedId != ItemId.None && equipSlot != EquipSlot.Ammo )
			return new ItemInstance( syncedId );

		return null;
	}

	public int GetEquippedSlotIndex( EquipSlot equipSlot )
	{
		if ( _equippedOrigin.TryGetValue( equipSlot, out var origin ) )
			return origin;

		return -1;
	}

	public bool IsEquipped( EquipSlot equipSlot )
	{
		if ( equipSlot == EquipSlot.Ammo )
			return _equippedAmmoId != ItemId.None;

		return _equipped.ContainsKey( equipSlot );
	}

	public bool CanUnequipToInventory( EquipSlot equipSlot )
	{
		if ( equipSlot == EquipSlot.Ammo )
			return CanUnequipAmmoToInventory();

		if ( !_equipped.ContainsKey( equipSlot ) )
			return true;

		if ( _equippedOrigin.TryGetValue( equipSlot, out var origin ) && origin >= 0 && origin < MaxSlots && _slots[origin].IsEmpty )
			return true;

		return HasEmptySlot();
	}

	public bool Unequip( EquipSlot equipSlot )
	{
		if ( equipSlot == EquipSlot.Ammo )
			return UnequipAmmo();

		return UnequipUnique( equipSlot );
	}

	public bool UnequipToBank( EquipSlot equipSlot )
	{
		if ( equipSlot == EquipSlot.Ammo )
			return UnequipAmmoToBank();

		return UnequipUniqueToBank( equipSlot );
	}

	public void UnequipAll()
	{
		var slots = new List<EquipSlot>( _equipped.Keys );
		foreach ( var slot in slots )
		{
			if ( !UnequipUnique( slot ) )
				UnequipUniqueToBank( slot );
		}

		if ( _equippedAmmoId != ItemId.None )
		{
			if ( !UnequipAmmo() )
				UnequipAmmoToBank();
		}
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
			PlayerPersistence.Local?.MarkDirty( SaveSection.Progress );
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
		if ( type == EnchantmentType.None )
			return 0f;

		float total = 0f;

		EquipSlot[] socketSlots = { EquipSlot.Ring, EquipSlot.Amulet };
		foreach ( var equipSlot in socketSlots )
		{
			if ( !_equipped.TryGetValue( equipSlot, out var instance ) || instance == null )
				continue;

			if ( instance.Socket1 != null && instance.Socket1.Enchantment == type )
				total += instance.Socket1.EnchantmentPercent;
			if ( instance.Socket2 != null && instance.Socket2.Enchantment == type )
				total += instance.Socket2.EnchantmentPercent;
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
		foreach ( var kv in _equipped )
		{
			if ( kv.Value != null )
				result[kv.Key] = kv.Value;
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
		if ( _discoveredStones.Add( stoneId ) )
			PlayerPersistence.Local?.MarkDirty( SaveSection.Progress );
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
			PlayerPersistence.Local?.SaveNow( SaveSection.Progress | SaveSection.Stats );
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
			PlayerPersistence.Local?.SaveNow( SaveSection.Progress );
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

		PlayerPersistence.Local?.MarkDirty( SaveSection.Kills | SaveSection.Stats );
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
		PlayerPersistence.Local?.MarkDirty( SaveSection.Stats );
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
		PlayerPersistence.Local?.MarkDirty( SaveSection.Progress | SaveSection.Inventory | SaveSection.Stats );
	}

	public Dictionary<string, string> GetChestClaims()
	{
		return _chestClaims;
	}

	public string GetProgressValue( string key )
	{
		if ( string.IsNullOrEmpty( key ) )
			return null;
		return _chestClaims.TryGetValue( key, out var v ) ? v : null;
	}

	public void SetProgressValue( string key, string value )
	{
		if ( string.IsNullOrEmpty( key ) )
			return;
		_chestClaims[key] = value ?? "";
		PlayerPersistence.Local?.MarkDirty( SaveSection.Progress | SaveSection.Inventory | SaveSection.Stats );
	}

	public static PlayerSaveData.UniqueItemEntry BuildUniqueEntry( ItemInstance instance )
	{
		var entry = new PlayerSaveData.UniqueItemEntry
		{
			ItemId = instance.ItemId.ToString(),
			Enchantment = instance.Enchantment.ToString(),
			EnchantmentPercent = instance.EnchantmentPercent
		};

		if ( instance.Socket1 != null )
		{
			entry.Socket1ItemId = instance.Socket1.ItemId.ToString();
			entry.Socket1Enchantment = instance.Socket1.Enchantment.ToString();
			entry.Socket1Percent = instance.Socket1.EnchantmentPercent;
		}
		if ( instance.Socket2 != null )
		{
			entry.Socket2ItemId = instance.Socket2.ItemId.ToString();
			entry.Socket2Enchantment = instance.Socket2.Enchantment.ToString();
			entry.Socket2Percent = instance.Socket2.EnchantmentPercent;
		}
		return entry;
	}

	public static ItemInstance BuildInstanceFromEntry( PlayerSaveData.UniqueItemEntry entry )
	{
		if ( entry == null )
			return null;
		if ( !System.Enum.TryParse<ItemId>( entry.ItemId, out var id ) || id == ItemId.None )
			return null;

		var enchant = EnchantmentType.None;
		System.Enum.TryParse<EnchantmentType>( entry.Enchantment, out enchant );

		var instance = new ItemInstance( id, enchant, entry.EnchantmentPercent );
		WipeLegacyEnchantIfJewelry( instance );

		instance.Socket1 = BuildSocketRune( entry.Socket1ItemId, entry.Socket1Enchantment, entry.Socket1Percent );
		instance.Socket2 = BuildSocketRune( entry.Socket2ItemId, entry.Socket2Enchantment, entry.Socket2Percent );

		return instance;
	}

	static ItemInstance BuildSocketRune( string itemIdStr, string enchantStr, float percent )
	{
		if ( string.IsNullOrEmpty( itemIdStr ) || itemIdStr == "None" )
			return null;
		if ( !System.Enum.TryParse<ItemId>( itemIdStr, out var id ) || id == ItemId.None )
			return null;

		var enchant = EnchantmentType.None;
		System.Enum.TryParse<EnchantmentType>( enchantStr, out enchant );
		if ( enchant == EnchantmentType.None || percent <= 0f )
			return null;

		return new ItemInstance( id, enchant, percent );
	}

	static void WipeLegacyEnchantIfJewelry( ItemInstance instance )
	{
		if ( instance == null )
			return;
		if ( !instance.IsSocketable )
			return;
		if ( instance.Enchantment == EnchantmentType.None )
			return;

		instance.Enchantment = EnchantmentType.None;
		instance.EnchantmentPercent = 0f;
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

			data.UniqueItems.Add( BuildUniqueEntry( slot.Unique ) );
		}

		data.Equipped = new Dictionary<string, PlayerSaveData.UniqueItemEntry>();
		foreach ( var kv in _equipped )
		{
			if ( kv.Value == null )
				continue;

			data.Equipped[kv.Key.ToString()] = BuildUniqueEntry( kv.Value );
		}

		data.EquippedSlotIndices = new Dictionary<string, int>();

		data.EquippedAmmoId = _equippedAmmoId.ToString();
		data.EquippedAmmoQty = _equippedAmmoCount;
		data.EquippedAmmoSlotIndex = 0;

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

				if ( slot.Unique.Socket1 != null )
				{
					entry.Socket1ItemId = slot.Unique.Socket1.ItemId.ToString();
					entry.Socket1Enchantment = slot.Unique.Socket1.Enchantment.ToString();
					entry.Socket1Percent = slot.Unique.Socket1.EnchantmentPercent;
				}
				if ( slot.Unique.Socket2 != null )
				{
					entry.Socket2ItemId = slot.Unique.Socket2.ItemId.ToString();
					entry.Socket2Enchantment = slot.Unique.Socket2.Enchantment.ToString();
					entry.Socket2Percent = slot.Unique.Socket2.EnchantmentPercent;
				}
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

					var instance = new ItemInstance( id, enchant, entry.EnchantmentPercent );
					WipeLegacyEnchantIfJewelry( instance );

					instance.Socket1 = BuildSocketRune( entry.Socket1ItemId, entry.Socket1Enchantment, entry.Socket1Percent );
					instance.Socket2 = BuildSocketRune( entry.Socket2ItemId, entry.Socket2Enchantment, entry.Socket2Percent );

					_slots[idx].Unique = instance;
				}
				else if ( IsEquipmentItem( id ) )
				{
					_slots[idx].Unique = new ItemInstance( id );
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

				TryPlaceItem( id, kv.Value, false );
			}

			foreach ( var entry in data.UniqueItems )
			{
				var instance = BuildInstanceFromEntry( entry );
				if ( instance == null )
					continue;

				int slotIndex = FindFirstEmptySlot();
				if ( slotIndex < 0 )
					break;

				_slots[slotIndex].Unique = instance;
			}

			if ( !string.IsNullOrEmpty( data.EquippedAmmoId ) && data.EquippedAmmoQty > 0 && data.EquippedAmmoSlotIndex > 0 )
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

		bool legacyEquipped = data.EquippedSlotIndices != null && data.EquippedSlotIndices.Count > 0;

		if ( legacyEquipped )
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

				_equipped[equipSlot] = slot.Unique;
				_equippedOrigin[equipSlot] = idx;
				SyncEquippedSlot( equipSlot, slot.Unique.ItemId );
				slot.Clear();
			}
		}
		else if ( data.Equipped != null )
		{
			foreach ( var kv in data.Equipped )
			{
				if ( !System.Enum.TryParse<EquipSlot>( kv.Key, out var equipSlot ) )
					continue;

				var instance = BuildInstanceFromEntry( kv.Value );
				if ( instance == null )
					continue;

				_equipped[equipSlot] = instance;
				SyncEquippedSlot( equipSlot, instance.ItemId );
			}
		}

		if ( data.EquippedAmmoSlotIndex > 0 )
		{
			int idx = data.EquippedAmmoSlotIndex - 1;
			if ( idx >= 0 && idx < MaxSlots && _slots[idx].IsStack )
			{
				_equippedAmmoId = _slots[idx].ItemId;
				_equippedAmmoCount = _slots[idx].Count;
				_slots[idx].Clear();
			}
		}
		else if ( !string.IsNullOrEmpty( data.EquippedAmmoId ) && data.EquippedAmmoQty > 0 )
		{
			if ( System.Enum.TryParse<ItemId>( data.EquippedAmmoId, out var ammoId ) && ammoId != ItemId.None )
			{
				_equippedAmmoId = ammoId;
				_equippedAmmoCount = data.EquippedAmmoQty;

				if ( !hasSlotData )
					RemoveItem( ammoId, data.EquippedAmmoQty );
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

		RepairEquipmentStacks();
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