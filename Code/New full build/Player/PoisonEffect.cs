using Sandbox;
using System;
using System.Collections.Generic;

public sealed class PoisonEffect : Component
{
	[Property] public float DamagePerTick { get; set; } = 2f;
	[Property] public float TickInterval { get; set; } = 1f;
	[Property] public float Duration { get; set; } = 5f;

	[Property] public int IndicatorParticleCount { get; set; } = 5;
	[Property] public float IndicatorOrbitRadius { get; set; } = 30f;
	[Property] public float IndicatorOrbitSpeed { get; set; } = 1.5f;
	[Property] public float IndicatorHeight { get; set; } = 50f;
	[Property] public float IndicatorParticleSize { get; set; } = 25f;
	[Property] public Color IndicatorColor { get; set; } = new Color( 0.5f, 0.95f, 0.2f, 0.85f );
	[Property] public string SpritePath { get; set; } = "particle_glow.sprite";

	GameObject _source;
	float _remaining;
	float _tickTimer;
	float _orbitTime;

	List<IndicatorParticle> _indicatorParticles = new();
	GameObject _indicatorRoot;

	class IndicatorParticle
	{
		public GameObject Go;
		public SpriteRenderer Renderer;
		public float OrbitOffset;
		public float HeightOffset;
		public float PulsePhase;
	}

	public static void Apply( GameObject target, GameObject source, float damagePerTick, float tickInterval, float duration )
	{
		if ( target == null || !target.IsValid() )
			return;

		var existing = target.Components.Get<PoisonEffect>();
		if ( existing != null )
		{
			existing.DamagePerTick = damagePerTick;
			existing.TickInterval = tickInterval;
			existing.Duration = duration;
			existing._remaining = duration;
			existing._source = source;
			return;
		}

		var effect = target.Components.Create<PoisonEffect>();
		effect.DamagePerTick = damagePerTick;
		effect.TickInterval = tickInterval;
		effect.Duration = duration;
		effect._remaining = duration;
		effect._source = source;
	}

	protected override void OnStart()
	{
		BuildIndicator();
	}

	void BuildIndicator()
	{
		Sprite spriteAsset = null;
		try { spriteAsset = ResourceLibrary.Get<Sprite>( SpritePath ); }
		catch ( System.Exception ) { spriteAsset = null; }

		_indicatorRoot = new GameObject( true, "PoisonIndicator" );
		_indicatorRoot.SetParent( GameObject );
		_indicatorRoot.LocalPosition = Vector3.Zero;

		for ( int i = 0; i < IndicatorParticleCount; i++ )
		{
			var go = new GameObject( true, $"PoisonParticle{i}" );
			go.SetParent( _indicatorRoot );

			var sr = go.Components.Create<SpriteRenderer>();
			if ( spriteAsset != null )
				sr.Sprite = spriteAsset;
			sr.Color = IndicatorColor;
			sr.Size = new Vector2( IndicatorParticleSize, IndicatorParticleSize );

			_indicatorParticles.Add( new IndicatorParticle
			{
				Go = go,
				Renderer = sr,
				OrbitOffset = ( (float)i / IndicatorParticleCount ) * MathF.PI * 2f,
				HeightOffset = Game.Random.Float( 0f, IndicatorHeight ),
				PulsePhase = Game.Random.Float( 0f, MathF.PI * 2f )
			} );
		}
	}

	protected override void OnUpdate()
	{
		_remaining -= Time.Delta;
		_tickTimer -= Time.Delta;
		_orbitTime += Time.Delta;

		UpdateIndicator();

		if ( _tickTimer <= 0f )
		{
			_tickTimer = TickInterval;
			ApplyTick();
		}

		if ( _remaining <= 0f )
			Destroy();
	}

	void UpdateIndicator()
	{
		float fadeIn = MathF.Min( 1f, _remaining / 0.5f );
		float globalFade = _remaining <= 0.5f ? fadeIn : 1f;

		foreach ( var p in _indicatorParticles )
		{
			if ( p.Go == null || !p.Go.IsValid() ) continue;

			float angle = p.OrbitOffset + _orbitTime * IndicatorOrbitSpeed;
			float bob = MathF.Sin( _orbitTime * 2f + p.PulsePhase ) * 8f;

			p.Go.LocalPosition = new Vector3(
				MathF.Cos( angle ) * IndicatorOrbitRadius,
				MathF.Sin( angle ) * IndicatorOrbitRadius,
				p.HeightOffset + bob
			);

			float pulse = 1f + 0.2f * MathF.Sin( _orbitTime * 3f + p.PulsePhase );
			float size = IndicatorParticleSize * pulse;
			p.Renderer.Size = new Vector2( size, size );

			var c = IndicatorColor;
			c.a = IndicatorColor.a * globalFade;
			p.Renderer.Color = c;
		}
	}

	protected override void OnDestroy()
	{
		if ( _indicatorRoot != null && _indicatorRoot.IsValid() )
			_indicatorRoot.Destroy();
	}

	void ApplyTick()
	{
		var monster = GameObject.Components.Get<Monster>();
		if ( monster != null && !monster.IsDead )
		{
			int dmg = (int)DamagePerTick;
			if ( dmg < 1 ) dmg = 1;
			monster.TakeDamage( dmg, _source );
			DamagePopupBroadcaster.BroadcastPoison( monster.WorldPosition + Vector3.Up * 60f, dmg );
			return;
		}

		var boss = GameObject.Components.Get<Boss>();
		if ( boss != null && !boss.IsDead )
		{
			int dmg = (int)DamagePerTick;
			if ( dmg < 1 ) dmg = 1;
			boss.TakeDamage( dmg, _source );
			DamagePopupBroadcaster.BroadcastPoison( boss.WorldPosition + Vector3.Up * 60f, dmg );
		}
	}
}