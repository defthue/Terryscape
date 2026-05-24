using Sandbox;
using System.Collections.Generic;

public class SkillData
{
	public int Level = 1;
	public int Xp;
}

public sealed class Skills : Component
{
	Dictionary<SkillType, SkillData> _skills = new();

	static readonly int[] XpTable = new int[]
	{
		50, 70, 90, 110, 130,
		150, 180, 200, 220, 250,
		300, 350, 400, 450, 500,
		550, 600, 650, 700, 750,
		800, 850, 850, 900, 850,
		1000, 1200, 1300, 1500, 1800,
		2000, 2200, 2500, 3000, 3500,
		4000, 4500, 5000, 6000, 7000,
		8000, 9000, 10000, 12000, 14000,
		16000, 19000, 22000, 25000
	};

	protected override void OnStart()
	{
		InitializeDefaults();
	}

	void InitializeDefaults()
	{
		_skills.Clear();
		foreach ( SkillType skill in System.Enum.GetValues( typeof( SkillType ) ) )
		{
			if ( skill == SkillType.None )
				continue;

			_skills[skill] = new SkillData();
		}
	}

	public int GetLevel( SkillType skill )
	{
		if ( _skills.TryGetValue( skill, out var data ) )
			return data.Level;

		return 1;
	}

	public int GetXp( SkillType skill )
	{
		if ( _skills.TryGetValue( skill, out var data ) )
			return data.Xp;

		return 0;
	}

	public int GetXpRequired( int level )
	{
		if ( level < 1 || level > XpTable.Length )
			return 99999;

		return XpTable[level - 1];
	}

	public int GetXpRequired( SkillType skill )
	{
		return GetXpRequired( GetLevel( skill ) );
	}

	public void AddXp( SkillType skill, int amount )
	{
		if ( skill == SkillType.None )
			return;

		if ( !_skills.TryGetValue( skill, out var data ) )
			return;

		data.Xp += amount;

		int required = GetXpRequired( data.Level );

		bool leveledUp = false;

		while ( data.Xp >= required && required > 0 )
		{
			data.Xp -= required;
			data.Level++;
			leveledUp = true;

			string skillName = skill.ToString();
			string text = $"{skillName} leveled up to {data.Level}!";
			GameLog.Add( text, "#f0c040" );

			GameManager.Instance?.AddLocalChatMessage( text );

			required = GetXpRequired( data.Level );
		}

		if ( leveledUp )
			PlayerPersistence.Local?.SaveNow( SaveSection.Skills | SaveSection.Stats );
		else
			PlayerPersistence.Local?.MarkDirty( SaveSection.Skills | SaveSection.Stats );
	}

	public void AddCombatXp( SkillType combatStyle, int amount )
	{
		AddXp( combatStyle, amount );
	}

	public bool MeetsRequirement( SkillType skill, int level )
	{
		if ( skill == SkillType.None )
			return true;

		return GetLevel( skill ) >= level;
	}

	public bool CanEquip( ItemDefinition item )
	{
		if ( item == null )
			return false;

		return MeetsRequirement( item.SkillRequired, item.LevelRequired );
	}

	public float GetToolPower( SkillType gatherSkill )
	{
		int level = GetLevel( gatherSkill );
		return 1.0f + (level - 1) * 0.05f;
	}

	public float GetCombatPower( SkillType combatStyle )
	{
		int level = GetLevel( combatStyle );
		return 1.0f + (level - 1) * 0.03f;
	}

	public float GetDefenceMultiplier()
	{
		int level = GetLevel( SkillType.Defence );
		return 1.0f + (level - 1) * 0.02f;
	}

	public Dictionary<string, PlayerSaveData.SkillEntry> ToSaveData()
	{
		var result = new Dictionary<string, PlayerSaveData.SkillEntry>();
		foreach ( var kv in _skills )
		{
			result[kv.Key.ToString()] = new PlayerSaveData.SkillEntry
			{
				Level = kv.Value.Level,
				Xp = kv.Value.Xp
			};
		}
		return result;
	}

	public void ApplySaveData( Dictionary<string, PlayerSaveData.SkillEntry> data )
	{
		InitializeDefaults();

		if ( data == null )
			return;

		foreach ( var kv in data )
		{
			if ( !System.Enum.TryParse<SkillType>( kv.Key, out var skill ) )
				continue;
			if ( skill == SkillType.None )
				continue;
			if ( !_skills.ContainsKey( skill ) )
				_skills[skill] = new SkillData();

			_skills[skill].Level = kv.Value.Level;
			_skills[skill].Xp = kv.Value.Xp;
		}
	}
}