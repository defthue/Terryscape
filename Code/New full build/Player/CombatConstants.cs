using Sandbox;

public static class CombatConstants
{
	public const float CritChance = 0.05f;
	public const float CritMultiplier = 1.5f;

	public static bool RollCrit()
	{
		return Game.Random.Float( 0f, 1f ) < CritChance;
	}
}
