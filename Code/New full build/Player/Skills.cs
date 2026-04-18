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
		foreach ( SkillType skill in System.Enum.GetValues( typeof( SkillType ) ) )
		{
			if ( skill == SkillType.None )
				continue;

			_skills[skill] = new SkillData();
		}

		var skills = GameObject.Components.Get<Skills>();
		if ( skills != null )
		{
			skills.AddXp( SkillType.Woodcutting, 200000 );
			skills.AddXp( SkillType.Mining, 200000 );
			skills.AddXp( SkillType.Enchanting, 200000 );
			skills.AddXp( SkillType.Smithing, 200000 );
			skills.AddXp( SkillType.Crafting, 200000 );
			skills.AddXp( SkillType.Attack, 200000 );
			skills.AddXp( SkillType.Defence, 200000 );
			skills.AddXp( SkillType.Archery, 200000 );
			skills.AddXp( SkillType.Magic, 200000 );
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

		while ( data.Xp >= required && required > 0 )
		{
			data.Xp -= required;
			data.Level++;

			string skillName = skill.ToString();
			GameLog.Add( $"{skillName} leveled up to {data.Level}!", "#f0c040" );

			required = GetXpRequired( data.Level );
		}
	}

	public void AddCombatXp( SkillType combatStyle, int amount )
	{
		int styleXp = (int)(amount * 0.7f);
		int defenceXp = amount - styleXp;

		AddXp( combatStyle, styleXp );
		AddXp( SkillType.Defence, defenceXp );
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
}