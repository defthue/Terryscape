using Sandbox;

public static class AchievementTracker
{
	public static void OnMonsterKilled()
	{
		Sandbox.Services.Stats.Increment( "monsters_killed", 1 );
	}

	public static void OnDuelWon()
	{
		Sandbox.Services.Stats.Increment( "duels_won", 1 );
	}

	public static void OnNodeGathered()
	{
		Sandbox.Services.Stats.Increment( "nodes_mined", 1 );
	}

	public static void OnWarp()
	{
		Sandbox.Services.Achievements.Unlock( "warp" );
	}

	public static void OnBlackjackPlayed()
	{
		Sandbox.Services.Achievements.Unlock( "high_roller" );
	}

	public static void OnQuestCompleted()
	{
		Sandbox.Services.Achievements.Unlock( "adventurer" );
	}

	public static void OnPetMounted()
	{
		Sandbox.Services.Achievements.Unlock( "mounted" );
	}

	public static void OnBossKilled()
	{
		Sandbox.Services.Achievements.Unlock( "boss_slayer" );
	}

	public static void OnSkillFirstTrained( SkillType skill )
	{
		var ident = SkillIdent( skill );
		if ( string.IsNullOrEmpty( ident ) )
			return;

		Sandbox.Services.Achievements.Unlock( ident );
	}

	public static void SyncSkillFirsts( Skills skills )
	{
		if ( skills == null )
			return;

		foreach ( SkillType skill in System.Enum.GetValues( typeof( SkillType ) ) )
		{
			if ( skill == SkillType.None )
				continue;

			if ( skills.GetLevel( skill ) > 1 || skills.GetXp( skill ) > 0 )
				OnSkillFirstTrained( skill );
		}
	}

	static string SkillIdent( SkillType skill )
	{
		return skill switch
		{
			SkillType.Woodcutting => "lumberjack",
			SkillType.Mining => "prospector",
			SkillType.Enchanting => "enchanter",
			SkillType.Smithing => "blacksmith",
			SkillType.Crafting => "apprentice",
			SkillType.Attack => "warrior",
			SkillType.Defence => "ironhide",
			SkillType.Archery => "marksman",
			SkillType.Magic => "spellcaster",
			_ => null
		};
	}
}
