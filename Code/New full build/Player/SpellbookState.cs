using Sandbox;
using System.Collections.Generic;
using System.Text.Json;

public static class SpellbookState
{
	const string LocalBindingsFile = "spellbook_bindings.json";

	static HashSet<SpellId> _unlockedSpells = new();
	static Dictionary<int, SpellId> _slotBindings = new();

	public static bool IsUnlocked( SpellId spellId )
	{
		var def = SpellDatabase.Get( spellId );
		if ( def == null )
			return false;

		var skills = PlayerHelper.GetSkills( PlayerHelper.GetLocalPlayer() );
		int magicLevel = skills != null ? skills.GetLevel( SkillType.Magic ) : 1;

		return magicLevel >= def.RequiredLevel;
	}

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
		SaveBindingsLocal();
	}

	public static void UnbindSlot( int slotIndex )
	{
		if ( _slotBindings.Remove( slotIndex ) )
			SaveBindingsLocal();
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
			SaveBindingsLocal();
	}

	public static IEnumerable<SpellId> GetUnlocked()
	{
		foreach ( var def in SpellDatabase.GetAll() )
		{
			if ( IsUnlocked( def.Id ) )
				yield return def.Id;
		}
	}

	public static Dictionary<int, SpellId> GetSlotBindings() => _slotBindings;

	public static void ApplySaveData( List<string> unlocked )
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

		LoadBindingsLocal();

		if ( _slotBindings.Count == 0 )
		{
			_slotBindings[1] = SpellId.Fireball;
			if ( IsUnlocked( SpellId.IceShard ) )
				_slotBindings[2] = SpellId.IceShard;
			SaveBindingsLocal();
		}
	}

	public static List<string> ToSaveData()
	{
		var unlocked = new List<string>();
		foreach ( var id in _unlockedSpells )
			unlocked.Add( id.ToString() );

		return unlocked;
	}

	static void SaveBindingsLocal()
	{
		try
		{
			var data = new Dictionary<string, string>();
			foreach ( var kv in _slotBindings )
				data[kv.Key.ToString()] = kv.Value.ToString();

			string json = JsonSerializer.Serialize( data );
			FileSystem.Data.WriteAllText( LocalBindingsFile, json );
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[SpellbookState] Failed to save local bindings: {ex.Message}" );
		}
	}

	static void LoadBindingsLocal()
	{
		try
		{
			if ( !FileSystem.Data.FileExists( LocalBindingsFile ) )
				return;

			string json = FileSystem.Data.ReadAllText( LocalBindingsFile );
			if ( string.IsNullOrEmpty( json ) )
				return;

			var data = JsonSerializer.Deserialize<Dictionary<string, string>>( json );
			if ( data == null )
				return;

			foreach ( var kv in data )
			{
				if ( !int.TryParse( kv.Key, out var slotIdx ) )
					continue;

				if ( slotIdx != 1 && slotIdx != 2 )
					continue;

				if ( !System.Enum.TryParse<SpellId>( kv.Value, out var id ) )
					continue;

				if ( !IsUnlocked( id ) )
					continue;

				_slotBindings[slotIdx] = id;
			}
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[SpellbookState] Failed to load local bindings: {ex.Message}" );
		}
	}
}