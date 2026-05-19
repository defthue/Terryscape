using Sandbox;
using System;

public sealed class AcidSpitProjectile : Component
{
	public Vector3 Velocity { get; set; }
	public GameObject Shooter { get; set; }
	public bool IsCrit { get; set; }

	[Property] public float Gravity { get; set; } = 800f;
	[Property] public float MaxLifetime { get; set; } = 5f;
	[Property] public float TraceRadius { get; set; } = 6f;

	[Property] public float SplashRadius { get; set; } = 150f;
	[Property] public float SplashVisualDuration { get; set; } = 1.5f;
	[Property] public float PoisonDamagePerTick { get; set; } = 2f;
	[Property] public float PoisonTickInterval { get; set; } = 1f;
	[Property] public float PoisonDuration { get; set; } = 5f;
	[Property] public Color SplashColor { get; set; } = new Color( 0.5f, 0.95f, 0.2f, 0.55f );

	float _lifetime;

	protected override void OnUpdate()
	{
		_lifetime += Time.Delta;
		if ( _lifetime >= MaxLifetime )
		{
			Impact( WorldPosition );
			return;
		}

		Vector3 previousPos = WorldPosition;

		Velocity = new Vector3( Velocity.x, Velocity.y, Velocity.z - Gravity * Time.Delta );
		GameObject.WorldPosition += Velocity * Time.Delta;

		Vector3 currentPos = WorldPosition;
		Vector3 moveDir = currentPos - previousPos;
		float moveLen = moveDir.Length;

		if ( moveLen > 0.01f )
		{
			Vector3 forward = moveDir.Normal;
			float yaw = MathF.Atan2( forward.y, forward.x ) * ( 180f / MathF.PI );
			float pitch = MathF.Asin( -forward.z ) * ( 180f / MathF.PI );
			GameObject.WorldRotation = Rotation.From( pitch, yaw, 0f );
		}

		var trace = Scene.Trace
			.Ray( previousPos, currentPos )
			.Radius( TraceRadius )
			.UseHitboxes( true )
			.IgnoreGameObject( Shooter )
			.Run();

		if ( trace.Hit )
			Impact( trace.HitPosition );
	}

	void Impact( Vector3 hitPos )
	{
		SpawnPool( Scene, hitPos, Shooter, SplashRadius, SplashVisualDuration,
			PoisonDamagePerTick, PoisonTickInterval, PoisonDuration, SplashColor );

		SoundLibrary.PlayAcidSpitImpact( hitPos );

		GameObject.Destroy();
	}

	static void SpawnPool( Scene scene, Vector3 position, GameObject source, float radius, float visualDuration,
		float dmgPerTick, float tickInterval, float duration, Color color )
	{
		if ( scene == null )
			return;

		float radiusSqr = radius * radius;

		foreach ( var monster in scene.GetAllComponents<Monster>() )
		{
			if ( monster == null || !monster.IsValid() || monster.IsDead )
				continue;
			if ( ( monster.WorldPosition - position ).LengthSquared > radiusSqr )
				continue;

			PoisonEffect.Apply( monster.GameObject, source, dmgPerTick, tickInterval, duration );
		}

		foreach ( var boss in scene.GetAllComponents<Boss>() )
		{
			if ( boss == null || !boss.IsValid() || boss.IsDead )
				continue;
			if ( ( boss.WorldPosition - position ).LengthSquared > radiusSqr )
				continue;

			PoisonEffect.Apply( boss.GameObject, source, dmgPerTick, tickInterval, duration );
		}

		var poolGo = scene.CreateObject();
		poolGo.Name = "AcidPoolVisual";
		poolGo.WorldPosition = position;

		var visual = poolGo.Components.Create<AcidPoolVisual>();
		visual.Radius = radius;
		visual.LifeDuration = visualDuration;
		visual.PoolColor = color;
	}
}

public sealed class AcidPoolVisual : Component
{
	public float Radius { get; set; } = 150f;
	public float LifeDuration { get; set; } = 3f;
	public Color PoolColor { get; set; } = new Color( 0.5f, 0.95f, 0.2f, 0.55f );
	public int ParticleCount { get; set; } = 50;
	public float ParticleSizeMin { get; set; } = 40f;
	public float ParticleSizeMax { get; set; } = 90f;
	public float RiseSpeedMin { get; set; } = 8f;
	public float RiseSpeedMax { get; set; } = 22f;
	public float SpawnSpread { get; set; } = 0.6f;
	public string SpritePath { get; set; } = "particle_glow.sprite";

	float _elapsed;
	int _spawnedSoFar;
	Sprite _spriteAsset;
	System.Collections.Generic.List<Particle> _particles = new();

	class Particle
	{
		public SpriteRenderer Renderer;
		public Vector3 Velocity;
		public float BaseSize;
		public Color BaseColor;
		public float Phase;
		public float PulseSpeed;
		public float SpawnTime;
	}

	protected override void OnStart()
	{
		try { _spriteAsset = ResourceLibrary.Get<Sprite>( SpritePath ); }
		catch ( System.Exception ) { _spriteAsset = null; }
	}

	void SpawnParticle()
	{
		float angle = Game.Random.Float( 0f, MathF.PI * 2f );
		float dist = MathF.Sqrt( Game.Random.Float( 0f, 1f ) ) * Radius;
		Vector3 offset = new Vector3( MathF.Cos( angle ) * dist, MathF.Sin( angle ) * dist, Game.Random.Float( 0f, 15f ) );

		var go = new GameObject( true, $"PoisonPuff{_spawnedSoFar}" );
		go.SetParent( GameObject );
		go.LocalPosition = offset;

		var sr = go.Components.Create<SpriteRenderer>();
		if ( _spriteAsset != null )
			sr.Sprite = _spriteAsset;
		sr.Color = PoolColor;

		float size = Game.Random.Float( ParticleSizeMin, ParticleSizeMax );
		sr.Size = new Vector2( size, size );

		float rise = Game.Random.Float( RiseSpeedMin, RiseSpeedMax );
		float drift = Game.Random.Float( 2f, 8f );
		float driftAngle = Game.Random.Float( 0f, MathF.PI * 2f );

		_particles.Add( new Particle
		{
			Renderer = sr,
			Velocity = new Vector3( MathF.Cos( driftAngle ) * drift, MathF.Sin( driftAngle ) * drift, rise ),
			BaseSize = size,
			BaseColor = PoolColor,
			Phase = Game.Random.Float( 0f, MathF.PI * 2f ),
			PulseSpeed = Game.Random.Float( 2f, 4f ),
			SpawnTime = _elapsed
		} );
	}

	protected override void OnUpdate()
	{
		_elapsed += Time.Delta;

		if ( _elapsed >= LifeDuration )
		{
			GameObject.Destroy();
			return;
		}

		float spawnEnd = SpawnSpread;
		float targetSpawned = MathF.Min( 1f, _elapsed / spawnEnd ) * ParticleCount;
		while ( _spawnedSoFar < (int)targetSpawned )
		{
			SpawnParticle();
			_spawnedSoFar++;
		}

		for ( int i = _particles.Count - 1; i >= 0; i-- )
		{
			var p = _particles[i];
			if ( p.Renderer == null || !p.Renderer.IsValid() ) continue;

			float age = _elapsed - p.SpawnTime;
			float ageNorm = MathF.Min( 1f, age / ( LifeDuration - p.SpawnTime ) );

			p.Renderer.GameObject.LocalPosition += p.Velocity * Time.Delta;

			float pulse = 1f + 0.25f * MathF.Sin( p.Phase + _elapsed * p.PulseSpeed );
			float growth = 1f + ageNorm * 0.4f;
			float size = p.BaseSize * pulse * growth;
			p.Renderer.Size = new Vector2( size, size );

			float fade = 1f - ageNorm;
			var c = p.BaseColor;
			c.a = p.BaseColor.a * fade;
			p.Renderer.Color = c;
		}
	}
}