using System.Collections.Generic;

public enum SpellId
{
	Fireball,
	IceShard,
	MagicMissile,
	ArcaneBarrier,
	AcidSpit,
	HealPulse,
	DarkBlast,
	Stoneskin,
	LightningBolt,
	Inferno,
	Singularity
}

public enum SpellType
{
	Projectile,
	Barrier,
	Homing,
	Lobbed,
	SelfAoE,
	SelfBuff,
	Channelled,
	GroundEffect
}

public class SpellDefinition
{
	public SpellId Id;
	public string Name;
	public SpellType Type;
	public int ManaCost;
	public float MinCastTime;
	public float Cooldown;
	public float DamageMultiplier;
	public float MaxRange;
	public float ProjectileSpeed;
	public float FreezeDuration;
	public float SlowDuration;
	public float SlowMultiplier;
	public float TraceRadius;
	public float MaxLifetime;
	public int RequiredLevel;
	public string Description;

	public float FrozenBonusDamage;
	public float BuffDuration;
	public float BarrierWidth;
	public float BarrierHeight;
	public float BarrierDepth;
	public float BarrierDuration;
	public float PoisonDamagePerTick;
	public float PoisonTickInterval;
	public float PoisonDuration;
	public float SplashRadius;
	public float SplashVisualDuration;
	public float AoeRadius;
	public float AoeHeight;
	public float AoeDuration;
	public float AoeDamagePerTick;
	public float AoeTickInterval;
	public float PullRadius;
	public float CollapseRadius;
	public float PullDuration;
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
			ManaCost = 2,
			MinCastTime = 0.5f,
			Cooldown = 0f,
			DamageMultiplier = 1.0f,
			MaxRange = 3000f,
			ProjectileSpeed = 2000f,
			FreezeDuration = 0f,
			SlowDuration = 0f,
			SlowMultiplier = 1f,
			TraceRadius = 6f,
			MaxLifetime = 5f,
			RequiredLevel = 1,
			Description = "A balanced fire projectile.",
		};

		_spells[SpellId.IceShard] = new SpellDefinition
		{
			Id = SpellId.IceShard,
			Name = "Ice Shard",
			Type = SpellType.Projectile,
			ManaCost = 2,
			MinCastTime = 2.0f,
			Cooldown = 0f,
			DamageMultiplier = 0.875f,
			MaxRange = 3000f,
			ProjectileSpeed = 1500f,
			FreezeDuration = 0f,
			SlowDuration = 1f,
			SlowMultiplier = 0.5f,
			TraceRadius = 5f,
			MaxLifetime = 5f,
			RequiredLevel = 1,
			Description = "Slow-cast shard of ice that slows enemies for 1 second on hit.",
			FrozenBonusDamage = 1.5f
		};

		_spells[SpellId.MagicMissile] = new SpellDefinition
		{
			Id = SpellId.MagicMissile,
			Name = "Magic Missile",
			Type = SpellType.Homing,
			ManaCost = 2,
			MinCastTime = 0.5f,
			Cooldown = 0f,
			DamageMultiplier = 0.75f,
			MaxRange = 2500f,
			ProjectileSpeed = 1600f,
			FreezeDuration = 0f,
			SlowDuration = 0f,
			SlowMultiplier = 1f,
			TraceRadius = 5f,
			MaxLifetime = 5f,
			RequiredLevel = 1,
			Description = "A lower-damage missile that locks onto the nearest target near your cursor and never misses.",
		};

		_spells[SpellId.ArcaneBarrier] = new SpellDefinition
		{
			Id = SpellId.ArcaneBarrier,
			Name = "Arcane Barrier",
			Type = SpellType.Barrier,
			ManaCost = 2,
			MinCastTime = 0.3f,
			Cooldown = 0f,
			DamageMultiplier = 0f,
			MaxRange = 0f,
			ProjectileSpeed = 0f,
			FreezeDuration = 0f,
			SlowDuration = 0f,
			SlowMultiplier = 1f,
			TraceRadius = 0f,
			MaxLifetime = 0f,
			RequiredLevel = 1,
			Description = "Conjure a shield that blocks enemies and projectiles.",
			BarrierWidth = 4f,
			BarrierHeight = 3f,
			BarrierDepth = 0.2f,
			BarrierDuration = 5f
		};

		_spells[SpellId.AcidSpit] = new SpellDefinition
		{
			Id = SpellId.AcidSpit,
			Name = "Acid Spit",
			Type = SpellType.Lobbed,
			ManaCost = 5,
			MinCastTime = 1.0f,
			Cooldown = 0f,
			DamageMultiplier = 0.6f,
			MaxRange = 1500f,
			ProjectileSpeed = 1200f,
			FreezeDuration = 0f,
			SlowDuration = 0f,
			SlowMultiplier = 1f,
			TraceRadius = 6f,
			MaxLifetime = 5f,
			RequiredLevel = 1,
			Description = "Lobs a glob of acid that arcs through the air and poisons whatever it hits.",
			PoisonDamagePerTick = 2f,
			PoisonTickInterval = 1f,
			PoisonDuration = 5f,
			SplashRadius = 150f,
			SplashVisualDuration = 3f
		};

		_spells[SpellId.HealPulse] = new SpellDefinition
		{
			Id = SpellId.HealPulse,
			Name = "Heal Pulse",
			Type = SpellType.SelfAoE,
			ManaCost = 5,
			MinCastTime = 0.6f,
			Cooldown = 30f,
			DamageMultiplier = 0f,
			MaxRange = 400f,
			ProjectileSpeed = 0f,
			FreezeDuration = 0f,
			SlowDuration = 0f,
			SlowMultiplier = 1f,
			TraceRadius = 0f,
			MaxLifetime = 0f,
			RequiredLevel = 1,
			Description = "Releases a healing pulse that restores HP to you and any allies nearby.",
		};

		_spells[SpellId.DarkBlast] = new SpellDefinition
		{
			Id = SpellId.DarkBlast,
			Name = "Dark Blast",
			Type = SpellType.Projectile,
			ManaCost = 4,
			MinCastTime = 1.0f,
			Cooldown = 0f,
			DamageMultiplier = 2.5f,
			MaxRange = 3000f,
			ProjectileSpeed = 1800f,
			FreezeDuration = 0f,
			SlowDuration = 0f,
			SlowMultiplier = 1f,
			TraceRadius = 8f,
			MaxLifetime = 5f,
			RequiredLevel = 1,
			Description = "Heavy single-target burst.",
		};

		_spells[SpellId.Stoneskin] = new SpellDefinition
		{
			Id = SpellId.Stoneskin,
			Name = "Stoneskin",
			Type = SpellType.SelfBuff,
			ManaCost = 4,
			MinCastTime = 0.5f,
			Cooldown = 0f,
			DamageMultiplier = 0f,
			MaxRange = 0f,
			ProjectileSpeed = 0f,
			FreezeDuration = 0f,
			SlowDuration = 0f,
			SlowMultiplier = 1f,
			TraceRadius = 0f,
			MaxLifetime = 3f,
			RequiredLevel = 1,
			Description = "Coats your body in stone — heavy defense, but heavily slowed.",
			BuffDuration = 4f
		};

		_spells[SpellId.LightningBolt] = new SpellDefinition
		{
			Id = SpellId.LightningBolt,
			Name = "Lightning Bolt",
			Type = SpellType.Channelled,
			ManaCost = 4,
			MinCastTime = 0.0f,
			Cooldown = 0f,
			DamageMultiplier = 0.4f,
			MaxRange = 500f,
			ProjectileSpeed = 0f,
			FreezeDuration = 0f,
			SlowDuration = 0f,
			SlowMultiplier = 1f,
			TraceRadius = 0f,
			MaxLifetime = 4f,
			RequiredLevel = 1,
			Description = "Hold to channel lightning from your staff — short range, continuous damage.",
		};

		_spells[SpellId.Inferno] = new SpellDefinition
		{
			Id = SpellId.Inferno,
			Name = "Inferno",
			Type = SpellType.GroundEffect,
			ManaCost = 10,
			MinCastTime = 1.0f,
			Cooldown = 0f,
			DamageMultiplier = 1.0f,
			MaxRange = 1200f,
			ProjectileSpeed = 0f,
			FreezeDuration = 0f,
			SlowDuration = 0f,
			SlowMultiplier = 1f,
			TraceRadius = 200f,
			MaxLifetime = 5f,
			RequiredLevel = 1,
			Description = "Conjures a swirling pillar of fire at your cursor that burns enemies inside it.",
			AoeRadius = 100f,
			AoeHeight = 180f,
			AoeDuration = 5f,
			AoeDamagePerTick = 2f,
			AoeTickInterval = 0.5f
		};

		_spells[SpellId.Singularity] = new SpellDefinition
		{
			Id = SpellId.Singularity,
			Name = "Singularity",
			Type = SpellType.GroundEffect,
			ManaCost = 15,
			MinCastTime = 2.0f,
			Cooldown = 0f,
			DamageMultiplier = 3.0f,
			MaxRange = 1500f,
			ProjectileSpeed = 0f,
			FreezeDuration = 0f,
			SlowDuration = 0f,
			SlowMultiplier = 1f,
			TraceRadius = 300f,
			MaxLifetime = 1f,
			RequiredLevel = 1,
			Description = "Tear open a small singularity that pulls enemies inward, then collapses for massive damage.",
			PullRadius = 220f,
			CollapseRadius = 110f,
			PullDuration = 1.5f
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