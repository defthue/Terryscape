using Sandbox;
using System;
using System.Collections.Generic;

public sealed class FireTornado : Component
{
	[Property] public float Radius { get; set; } = 100f;
	[Property] public float Height { get; set; } = 180f;
	[Property] public float Duration { get; set; } = 5f;
	[Property] public float DamagePerTick { get; set; } = 1f;
	[Property] public float TickInterval { get; set; } = 0.5f;

	[Property] public int OuterParticleRate { get; set; } = 80;
	[Property] public int InnerParticleRate { get; set; } = 40;
	[Property] public int EmberRate { get; set; } = 20;

	[Property] public float RiseSpeedMin { get; set; } = 80f;
	[Property] public float RiseSpeedMax { get; set; } = 140f;
	[Property] public float SwirlSpeedMin { get; set; } = 3f;
	[Property] public float SwirlSpeedMax { get; set; } = 6f;

	[Property] public float OuterParticleSize { get; set; } = 50f;
	[Property] public float InnerParticleSize { get; set; } = 35f;
	[Property] public float EmberSize { get; set; } = 18f;

	[Property] public Color OuterColor { get; set; } = new Color( 1f, 0.4f, 0.1f, 0.7f );
	[Property] public Color InnerColor { get; set; } = new Color( 1f, 0.85f, 0.3f, 0.9f );
	[Property] public Color EmberColor { get; set; } = new Color( 1f, 0.55f, 0.15f, 1f );

	public GameObject Source { get; set; }
	public bool VisualOnly { get; set; }

	float _elapsed;
	float _tickTimer;
	float _outerAccum;
	float _innerAccum;
	float _emberAccum;
	Sprite _spriteAsset;

	List<TornadoParticle> _particles = new();

	enum ParticleKind { Outer, Inner, Ember }

	class TornadoParticle
	{
		public GameObject Go;
		public SpriteRenderer Renderer;
		public ParticleKind Kind;
		public float SpawnTime;
		public float Lifetime;
		public float OrbitAngle;
		public float OrbitRadius;
		public float SwirlSpeed;
		public float RiseSpeed;
		public float BaseSize;
		public Color BaseColor;
	}

	public static FireTornado Spawn( Scene scene, Vector3 position, GameObject source, float radius, float height, float duration, float damage, float tickInterval, bool visualOnly = false )
	{
		if ( scene == null )
			return null;

		var go = scene.CreateObject();
		go.Name = "FireTornado";
		go.WorldPosition = position;

		var tornado = go.Components.Create<FireTornado>();
		tornado.Radius = radius;
		tornado.Height = height;
		tornado.Duration = duration;
		tornado.DamagePerTick = damage;
		tornado.TickInterval = tickInterval;
		tornado.Source = source;
		tornado.VisualOnly = visualOnly;

		return tornado;
	}

	protected override void OnStart()
	{
		_spriteAsset = SpellVfx.GlowSprite;
	}

	protected override void OnUpdate()
	{
		_elapsed += Time.Delta;
		_tickTimer -= Time.Delta;

		if ( _elapsed >= Duration && _particles.Count == 0 )
		{
			GameObject.Destroy();
			return;
		}

		if ( _elapsed < Duration )
		{
			SpawnParticles();

			if ( !VisualOnly && _tickTimer <= 0f )
			{
				_tickTimer = TickInterval;
				ApplyDamageTick();
			}
		}

		UpdateParticles();
	}

	void SpawnParticles()
	{
		_outerAccum += OuterParticleRate * Time.Delta;
		while ( _outerAccum >= 1f )
		{
			SpawnTornadoParticle( ParticleKind.Outer );
			_outerAccum -= 1f;
		}

		_innerAccum += InnerParticleRate * Time.Delta;
		while ( _innerAccum >= 1f )
		{
			SpawnTornadoParticle( ParticleKind.Inner );
			_innerAccum -= 1f;
		}

		_emberAccum += EmberRate * Time.Delta;
		while ( _emberAccum >= 1f )
		{
			SpawnTornadoParticle( ParticleKind.Ember );
			_emberAccum -= 1f;
		}
	}

	void SpawnTornadoParticle( ParticleKind kind )
	{
		float orbitRadius;
		float lifetime;
		float baseSize;
		Color baseColor;
		float rise;
		float swirl;
		float startHeight;

		switch ( kind )
		{
			case ParticleKind.Inner:
				orbitRadius = Radius * Game.Random.Float( 0.15f, 0.5f );
				lifetime = Game.Random.Float( 1.2f, 1.8f );
				baseSize = InnerParticleSize * Game.Random.Float( 0.8f, 1.2f );
				baseColor = InnerColor;
				rise = Game.Random.Float( RiseSpeedMin * 1.2f, RiseSpeedMax * 1.2f );
				swirl = Game.Random.Float( SwirlSpeedMin * 1.5f, SwirlSpeedMax * 1.5f );
				startHeight = Game.Random.Float( 0f, 20f );
				break;

			case ParticleKind.Ember:
				orbitRadius = Radius * Game.Random.Float( 0.3f, 1.1f );
				lifetime = Game.Random.Float( 0.6f, 1.2f );
				baseSize = EmberSize * Game.Random.Float( 0.7f, 1.3f );
				baseColor = EmberColor;
				rise = Game.Random.Float( 5f, 25f );
				swirl = Game.Random.Float( 0.5f, 1.5f );
				startHeight = Game.Random.Float( 0f, 8f );
				break;

			default:
				orbitRadius = Radius * Game.Random.Float( 0.55f, 1f );
				lifetime = Game.Random.Float( 1.5f, 2.2f );
				baseSize = OuterParticleSize * Game.Random.Float( 0.7f, 1.3f );
				baseColor = OuterColor;
				rise = Game.Random.Float( RiseSpeedMin, RiseSpeedMax );
				swirl = Game.Random.Float( SwirlSpeedMin, SwirlSpeedMax );
				startHeight = Game.Random.Float( 0f, 15f );
				break;
		}

		float angle = Game.Random.Float( 0f, MathF.PI * 2f );

		var go = new GameObject( true, $"FireParticle{_particles.Count}" );
		go.SetParent( GameObject );
		go.LocalPosition = new Vector3( MathF.Cos( angle ) * orbitRadius, MathF.Sin( angle ) * orbitRadius, startHeight );

		var sr = go.Components.Create<SpriteRenderer>();
		if ( _spriteAsset != null )
			sr.Sprite = _spriteAsset;
		sr.Color = baseColor;
		sr.Size = new Vector2( baseSize, baseSize );

		_particles.Add( new TornadoParticle
		{
			Go = go,
			Renderer = sr,
			Kind = kind,
			SpawnTime = _elapsed,
			Lifetime = lifetime,
			OrbitAngle = angle,
			OrbitRadius = orbitRadius,
			SwirlSpeed = swirl,
			RiseSpeed = rise,
			BaseSize = baseSize,
			BaseColor = baseColor
		} );
	}

	void UpdateParticles()
	{
		for ( int i = _particles.Count - 1; i >= 0; i-- )
		{
			var p = _particles[i];
			float age = _elapsed - p.SpawnTime;

			if ( age >= p.Lifetime || p.Go == null || !p.Go.IsValid() )
			{
				if ( p.Go != null && p.Go.IsValid() )
					p.Go.Destroy();
				_particles.RemoveAt( i );
				continue;
			}

			float ageNorm = age / p.Lifetime;

			p.OrbitAngle += p.SwirlSpeed * Time.Delta;

			float currentHeight = p.Go.LocalPosition.z + p.RiseSpeed * Time.Delta;

			float radiusScale = p.Kind == ParticleKind.Ember ? 1f : ( 1f - 0.4f * ageNorm );
			float currentRadius = p.OrbitRadius * radiusScale;

			p.Go.LocalPosition = new Vector3(
				MathF.Cos( p.OrbitAngle ) * currentRadius,
				MathF.Sin( p.OrbitAngle ) * currentRadius,
				currentHeight
			);

			float sizePulse = 1f + 0.15f * MathF.Sin( age * 8f );
			float sizeShrink = p.Kind == ParticleKind.Ember ? ( 1f - ageNorm ) : ( 1f - 0.3f * ageNorm );
			float size = p.BaseSize * sizePulse * sizeShrink;
			p.Renderer.Size = new Vector2( size, size );

			float fade;
			if ( ageNorm < 0.2f )
				fade = ageNorm / 0.2f;
			else
				fade = 1f - ( ( ageNorm - 0.2f ) / 0.8f );

			var c = p.BaseColor;
			c.a = p.BaseColor.a * fade;
			p.Renderer.Color = c;
		}
	}

	void ApplyDamageTick()
	{
		Vector3 pos = WorldPosition;
		float radiusSqr = Radius * Radius;
		int dmg = (int)DamagePerTick;
		if ( dmg < 1 ) dmg = 1;

		foreach ( var monster in Scene.GetAllComponents<Monster>() )
		{
			if ( monster == null || !monster.IsValid() || monster.IsDead )
				continue;
			if ( ( monster.WorldPosition - pos ).LengthSquared > radiusSqr )
				continue;

			monster.TakeDamage( dmg, Source );
			DamagePopupBroadcaster.Broadcast( monster.WorldPosition + Vector3.Up * 60f, dmg, monster.MaxHealth, false );
		}

		foreach ( var boss in Scene.GetAllComponents<Boss>() )
		{
			if ( boss == null || !boss.IsValid() || boss.IsDead )
				continue;
			if ( ( boss.WorldPosition - pos ).LengthSquared > radiusSqr )
				continue;

			boss.TakeDamage( dmg, Source );
			DamagePopupBroadcaster.Broadcast( boss.WorldPosition + Vector3.Up * 60f, dmg, boss.MaxHealth, false );
		}
	}
}