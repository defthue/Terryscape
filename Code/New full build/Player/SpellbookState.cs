using Sandbox;
using System.Collections.Generic;

public static class SpellbookState
{
	static HashSet<SpellId> _unlockedSpells = new();
	static Dictionary<int, SpellId> _slotBindings = new();

	public static bool IsUnlocked( SpellId spellId ) => _unlockedSpells.Contains( spellId );

	public static void Unlock( SpellId spellId )
	{
		if ( _unlockedSpells.Add( spellId ) )
		{
			var def = SpellDatabase.Get( spellId );
			GameLog.Add( $"Spell unlocked: {def?.Name ?? spellId.ToString()}!", "#a080d0" );
			PlayerPersistence.Local?.RequestSaveNow();
		}
	}

	public static bool TryGetSlot( int slotIndex, out SpellId spellId )
	{
		return _slotBindings.TryGetValue( slotIndex, out spellId );
	}

	public static SpellId GetSlot( int slotIndex )
	{
		return _slotBindings.TryGetValue( slotIndex, out var id ) ? id : SpellId.Fireball;
	}

	public static bool IsSlotBound( int slotIndex )
	{
		return _slotBindings.ContainsKey( slotIndex );
	}

	public static void BindSlot( int slotIndex, SpellId spellId )
	{
		if ( !IsUnlocked( spellId ) )
			return;

		foreach ( var kv in new List<KeyValuePair<int, SpellId>>( _slotBindings ) )
		{
			if ( kv.Value == spellId && kv.Key != slotIndex )
				_slotBindings.Remove( kv.Key );
		}

		_slotBindings[slotIndex] = spellId;
		PlayerPersistence.Local?.RequestSaveNow();
	}

	public static void UnbindSlot( int slotIndex )
	{
		if ( _slotBindings.Remove( slotIndex ) )
			PlayerPersistence.Local?.RequestSaveNow();
	}

	public static void UnbindSpell( SpellId spellId )
	{
		bool changed = false;
		foreach ( var kv in new List<KeyValuePair<int, SpellId>>( _slotBindings ) )
		{
			if ( kv.Value == spellId )
			{
				_slotBindings.Remove( kv.Key );
				changed = true;
			}
		}

		if ( changed )
			PlayerPersistence.Local?.RequestSaveNow();
	}

	public static IEnumerable<SpellId> GetUnlocked() => _unlockedSpells;

	public static Dictionary<int, SpellId> GetSlotBindings() => _slotBindings;

	public static void ApplySaveData( List<string> unlocked, Dictionary<string, string> slots )
	{
		_unlockedSpells.Clear();
		_slotBindings.Clear();

		if ( unlocked != null )
		{
			foreach ( var name in unlocked )
			{
				if ( System.Enum.TryParse<SpellId>( name, out var id ) )
					_unlockedSpells.Add( id );
			}
		}

		if ( _unlockedSpells.Count == 0 )
			_unlockedSpells.Add( SpellId.Fireball );

		if ( slots != null )
		{
			foreach ( var kv in slots )
			{
				if ( !int.TryParse( kv.Key, out var slotIdx ) )
					continue;

				if ( slotIdx != 1 && slotIdx != 2 )
					continue;

				if ( System.Enum.TryParse<SpellId>( kv.Value, out var id ) )
					_slotBindings[slotIdx] = id;
			}
		}

		if ( _slotBindings.Count == 0 )
		{
			_slotBindings[1] = SpellId.Fireball;
			if ( _unlockedSpells.Contains( SpellId.IceShard ) )
				_slotBindings[2] = SpellId.IceShard;
		}
	}

	public static (List<string> unlocked, Dictionary<string, string> slots) ToSaveData()
	{
		var unlocked = new List<string>();
		foreach ( var id in _unlockedSpells )
			unlocked.Add( id.ToString() );

		var slots = new Dictionary<string, string>();
		foreach ( var kv in _slotBindings )
			slots[kv.Key.ToString()] = kv.Value.ToString();

		return ( unlocked, slots );
	}
}