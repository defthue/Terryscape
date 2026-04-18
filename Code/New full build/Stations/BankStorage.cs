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

		return true;
	}

	public void DepositUnique( ItemInstance instance )
	{
		_bankedUnique.Add( instance );
	}

	public ItemInstance WithdrawUnique( int index )
	{
		if ( index < 0 || index >= _bankedUnique.Count )
			return null;

		var instance = _bankedUnique[index];
		_bankedUnique.RemoveAt( index );
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
}