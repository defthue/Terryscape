using Sandbox;
using System.Collections.Generic;

public sealed class BankStorage : Component
{
	Dictionary<ItemId, int> _banked = new();
	List<ItemInstance> _bankedUnique = new();

	protected override void OnStart()
	{
		_banked.Clear();
		_bankedUnique.Clear();
	}

	public int GetItemCount( ItemId id )
	{
		if ( _banked.TryGetValue( id, out var count ) )
			return count;

		return 0;
	}

	public bool HasItem( ItemId id, int amount = 1 )
	{
		return GetItemCount( id ) >= amount;
	}

	public bool Deposit( ItemId id, int amount = 1 )
	{
		if ( id == ItemId.None || amount <= 0 )
			return false;

		int current = GetItemCount( id );
		_banked[id] = current + amount;
		PlayerPersistence.Local?.MarkDirty( SaveSection.Bank | SaveSection.Stats );
		return true;
	}

	public bool Withdraw( ItemId id, int amount = 1 )
	{
		if ( !HasItem( id, amount ) )
			return false;

		int current = GetItemCount( id );
		int newAmount = current - amount;

		if ( newAmount <= 0 )
			_banked.Remove( id );
		else
			_banked[id] = newAmount;

		PlayerPersistence.Local?.MarkDirty( SaveSection.Bank | SaveSection.Stats );
		return true;
	}

	public void DepositUnique( ItemInstance instance )
	{
		_bankedUnique.Add( instance );
		PlayerPersistence.Local?.MarkDirty( SaveSection.Bank | SaveSection.Stats );
	}

	public ItemInstance WithdrawUnique( int index )
	{
		if ( index < 0 || index >= _bankedUnique.Count )
			return null;

		var instance = _bankedUnique[index];
		_bankedUnique.RemoveAt( index );
		PlayerPersistence.Local?.MarkDirty( SaveSection.Bank | SaveSection.Stats );
		return instance;
	}

	public List<ItemInstance> GetBankedUnique()
	{
		return _bankedUnique;
	}

	public int GetBankedUniqueCount()
	{
		return _bankedUnique.Count;
	}

	public Dictionary<ItemId, int> GetAllItems()
	{
		return _banked;
	}

	public PlayerSaveData ToSaveData( PlayerSaveData data )
	{
		data.Bank = new Dictionary<string, int>();
		foreach ( var kv in _banked )
			data.Bank[kv.Key.ToString()] = kv.Value;

		data.BankUnique = new List<PlayerSaveData.UniqueItemEntry>();
		foreach ( var item in _bankedUnique )
			data.BankUnique.Add( Inventory.BuildUniqueEntry( item ) );

		return data;
	}

	public void ApplySaveData( PlayerSaveData data )
	{
		_banked.Clear();
		_bankedUnique.Clear();

		if ( data == null )
			return;

		if ( data.Bank != null )
		{
			foreach ( var kv in data.Bank )
			{
				if ( !System.Enum.TryParse<ItemId>( kv.Key, out var id ) )
					continue;
				if ( id == ItemId.None )
					continue;

				_banked[id] = kv.Value;
			}
		}

		if ( data.BankUnique != null )
		{
			foreach ( var entry in data.BankUnique )
			{
				var instance = Inventory.BuildInstanceFromEntry( entry );
				if ( instance == null )
					continue;
				_bankedUnique.Add( instance );
			}
		}
	}
}