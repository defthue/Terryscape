using System.Collections.Generic;

public enum SpellId
{
	Fireball,
	IceShard,
	DarkBlast,
	ArcaneBarrier
}

public enum SpellType
{
	Projectile,
	Barrier
}

public class SpellDefinition
{
	public SpellId Id;
	public string Name;
	public SpellType Type;
	public int ManaCost;
	public float MinCastTime;
	public float DamageMultiplier;
	public float MaxRange;
	public float ProjectileSpeed;
	public float FreezeDuration;
	public float TraceRadius;
	public float MaxLifetime;
	public int RequiredLevel;
	public string Description;
	public string UnlockHint;
}

public static class SpellDatabase
{
	static Dictionary<SpellId, SpellDefinition> _spells;

	static void Build()
	{
		_spells = new Dictionary<SpellId, SpellDefinition>();

		_spells[SpellId.Fireball] = new SpellDefinition
		{
			Id = SpellId.Fireball,
			Name = "Fireball",
			Type = SpellType.Projectile,
			ManaCost = 1,
			MinCastTime = 0.5f,
			DamageMultiplier = 1.25f,
			MaxRange = 3000f,
			ProjectileSpeed = 2000f,
			FreezeDuration = 0f,
			TraceRadius = 6f,
			MaxLifetime = 5f,
			RequiredLevel = 1,
			Description = "A balanced fire projectile.",
			UnlockHint = "Starter spell"
		};

		_spells[SpellId.IceShard] = new SpellDefinition
		{
			Id = SpellId.IceShard,
			Name = "Ice Shard",
			Type = SpellType.Projectile,
			ManaCost = 1,
			MinCastTime = 0.5f,
			DamageMultiplier = 0.875f,
			MaxRange = 3000f,
			ProjectileSpeed = 1500f,
			FreezeDuration = 2f,
			TraceRadius = 5f,
			MaxLifetime = 5f,
			RequiredLevel = 10,
			Description = "Low damage, but freezes enemies briefly.",
			UnlockHint = "Found in the world"
		};

		_spells[SpellId.DarkBlast] = new SpellDefinition
		{
			Id = SpellId.DarkBlast,
			Name = "Dark Blast",
			Type = SpellType.Projectile,
			ManaCost = 2,
			MinCastTime = 1.0f,
			DamageMultiplier = 2.5f,
			MaxRange = 3000f,
			ProjectileSpeed = 1800f,
			FreezeDuration = 0f,
			TraceRadius = 8f,
			MaxLifetime = 5f,
			RequiredLevel = 25,
			Description = "Heavy single-target burst.",
			UnlockHint = "Quest reward"
		};

		_spells[SpellId.ArcaneBarrier] = new SpellDefinition
		{
			Id = SpellId.ArcaneBarrier,
			Name = "Arcane Barrier",
			Type = SpellType.Barrier,
			ManaCost = 1,
			MinCastTime = 0.3f,
			DamageMultiplier = 0f,
			MaxRange = 0f,
			ProjectileSpeed = 0f,
			FreezeDuration = 0f,
			TraceRadius = 0f,
			MaxLifetime = 0f,
			RequiredLevel = 15,
			Description = "Conjure a wall that blocks enemies.",
			UnlockHint = "Found in the world"
		};
	}

	public static SpellDefinition Get( SpellId id )
	{
		if ( _spells == null )
			Build();

		if ( _spells.TryGetValue( id, out var def ) )
			return def;

		return null;
	}

	public static IEnumerable<SpellDefinition> GetAll()
	{
		if ( _spells == null )
			Build();

		return _spells.Values;
	}
}
