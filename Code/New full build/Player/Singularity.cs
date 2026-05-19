using Sandbox;
using System;
using System.Collections.Generic;

public sealed class Singularity : Component
{
	[Property] public float PullRadius { get; set; } = 220f;
	[Property] public float CollapseRadius { get; set; } = 110f;
	[Property] public float PullDuration { get; set; } = 1.5f;
	[Property] public float CollapseFlashDuration { get; set; } = 0.4f;
	[Property] public int CollapseDamage { get; set; } = 30;

	[Property] public float CoreMaxSize { get; set; } = 70f;
	[Property] public Color CoreColor { get; set; } = new Color( 0.02f, 0.02f, 0.05f, 1f );

	[Property] public int HaloParticleCount { get; set; } = 24;
	[Property] public float HaloRadius { get; set; } = 90f;
	[Property] public float HaloSpinSpeed { get; set; } = 8f;
	[Property] public float HaloParticleSize { get; set; } = 30f;
	[Property] public Color HaloColor { get; set; } = new Color( 0.7f, 0.4f, 1f, 0.9f );

	[Property] public int InwardParticleRate { get; set; } = 120;
	[Property] public float InwardParticleSize { get; set; } = 25f;
	[Property] public Color InwardColor { get; set; } = new Color( 0.85f, 0.6f, 1f, 0.85f );

	[Property] public int BeamCount { get; set; } = 4;
	[Property] public float BeamHeight { get; set; } = 600f;
	[Property] public float BeamMaxThickness { get; set; } = 18f;
	[Property] public Color BeamColor { get; set; } = new Color( 0.85f, 0.65f, 1f, 0.75f );

	[Property] public float GroundRingThickness { get; set; } = 8f;
	[Property] public Color GroundRingColor { get; set; } = new Color( 0.7f, 0.4f, 1f, 1f );
	[Property] public int GroundRingSegments { get; set; } = 48;

	[Property] public int ShockwaveParticleCount { get; set; } = 60;
	[Property] public float ShockwaveExpandSpeed { get; set; } = 800f;
	[Property] public float ShockwaveParticleSize { get; set; } = 50f;
	[Property] public Color ShockwaveColor { get; set; } = new Color( 1f, 1f, 1f, 1f );

	[Property] public string SpritePath { get; set; } = "particle_glow.sprite";

	public GameObject Source { get; set; }

	float _elapsed;
	float _inwardAccum;
	bool _collapsed;
	bool _destroying;
	float _destroyTimer;
	Sprite _spriteAsset;

	ModelRenderer _coreRenderer;
	GameObject _coreGo;
	List<HaloParticle> _haloParticles = new();
	List<InwardParticle> _inwardParticles = new();
	List<ModelRenderer> _beamRenderers = new();
	List<GameObject> _beamGos = new();
	List<ModelRenderer> _groundRingSegs = new();
	List<ShockwaveParticle> _shockwaveParticles = new();
	HashSet<GameObject> _suppressedMonsters = new();

	class HaloParticle
	{
		public GameObject Go;
		public SpriteRenderer Renderer;
		public float AngleOffset;
		public float TiltOffset;
	}

	class InwardParticle
	{
		public GameObject Go;
		public SpriteRenderer Renderer;
		public Vector3 StartLocalPos;
		public float SpawnTime;
		public float Lifetime;
		public float BaseSize;
	}

	class ShockwaveParticle
	{
		public GameObject Go;
		public SpriteRenderer Renderer;
		public Vector3 Direction;
		public float SpawnTime;
	}

	public static Singularity Spawn( Scene scene, Vector3 position, GameObject source, float pullRadius, float collapseRadius, float pullDuration, int collapseDamage )
	{
		if ( scene == null )
			return null;

		var go = scene.CreateObject();
		go.Name = "Singularity";
		go.WorldPosition = position;

		var sing = go.Components.Create<Singularity>();
		sing.PullRadius = pullRadius;
		sing.CollapseRadius = collapseRadius;
		sing.PullDuration = pullDuration;
		sing.CollapseDamage = collapseDamage;
		sing.Source = source;

		return sing;
	}

	protected override void OnStart()
	{
		try { _spriteAsset = ResourceLibrary.Get<Sprite>( SpritePath ); }
		catch ( System.Exception ) { _spriteAsset = null; }

		BuildCore();
		BuildHalo();
		BuildBeams();
		BuildGroundRing();

		SoundLibrary.PlaySingularity( WorldPosition );
	}

	void BuildCore()
	{
		_coreGo = new GameObject( true, "Core" );
		_coreGo.SetParent( GameObject );
		_coreGo.LocalPosition = new Vector3( 0f, 0f, CoreMaxSize * 0.5f );
		_coreGo.LocalScale = new Vector3( 0.01f );

		_coreRenderer = _coreGo.Components.Create<ModelRenderer>();
		_coreRenderer.Model = Model.Load( "models/dev/sphere.vmdl" );
		_coreRenderer.Tint = CoreColor;
	}

	void BuildHalo()
	{
		for ( int i = 0; i < HaloParticleCount; i++ )
		{
			var go = new GameObject( true, $"Halo{i}" );
			go.SetParent( GameObject );

			var sr = go.Components.Create<SpriteRenderer>();
			if ( _spriteAsset != null )
				sr.Sprite = _spriteAsset;
			sr.Color = HaloColor;
			sr.Size = new Vector2( HaloParticleSize, HaloParticleSize );

			_haloParticles.Add( new HaloParticle
			{
				Go = go,
				Renderer = sr,
				AngleOffset = ( (float)i / HaloParticleCount ) * MathF.PI * 2f,
				TiltOffset = Game.Random.Float( -8f, 8f )
			} );
		}
	}

	void BuildBeams()
	{
		for ( int i = 0; i < BeamCount; i++ )
		{
			var go = new GameObject( true, $"Beam{i}" );
			go.SetParent( GameObject );

			float angle = ( (float)i / BeamCount ) * MathF.PI * 2f;
			float lateralOffset = Game.Random.Float( 15f, 35f );
			go.LocalPosition = new Vector3( MathF.Cos( angle ) * lateralOffset, MathF.Sin( angle ) * lateralOffset, BeamHeight * 0.5f );
			go.LocalScale = new Vector3( BeamHeight / 50f, 0.01f, 0.01f );
			go.LocalRotation = Rotation.LookAt( Vector3.Up );

			var renderer = go.Components.Create<ModelRenderer>();
			renderer.Model = Model.Load( "models/dev/box.vmdl" );
			renderer.Tint = BeamColor;

			_beamGos.Add( go );
			_beamRenderers.Add( renderer );
		}
	}

	void BuildGroundRing()
	{
		float angleStep = MathF.PI * 2f / GroundRingSegments;
		float boxUnit = 50f;

		for ( int i = 0; i < GroundRingSegments; i++ )
		{
			float a = i * angleStep;
			float b = ( i + 1 ) * angleStep;

			Vector3 p1 = new Vector3( MathF.Cos( a ) * PullRadius, MathF.Sin( a ) * PullRadius, 2f );
			Vector3 p2 = new Vector3( MathF.Cos( b ) * PullRadius, MathF.Sin( b ) * PullRadius, 2f );

			var segGo = new GameObject( true, $"GroundRing{i}" );
			segGo.SetParent( GameObject );

			Vector3 mid = ( p1 + p2 ) * 0.5f;
			segGo.LocalPosition = mid;

			Vector3 diff = p2 - p1;
			float length = diff.Length;
			if ( length < 0.01f ) continue;

			segGo.LocalRotation = Rotation.LookAt( diff / length );
			segGo.LocalScale = new Vector3( length / boxUnit, GroundRingThickness / boxUnit, GroundRingThickness / boxUnit );

			var seg = segGo.Components.Create<ModelRenderer>();
			seg.Model = Model.Load( "models/dev/box.vmdl" );
			seg.Tint = GroundRingColor;
			_groundRingSegs.Add( seg );
		}
	}

	protected override void OnUpdate()
	{
		_elapsed += Time.Delta;

		if ( _destroying )
		{
			_destroyTimer += Time.Delta;
			UpdateShockwave();

			if ( _destroyTimer >= CollapseFlashDuration )
			{
				ReleaseSuppressedMonsters();
				GameObject.Destroy();
			}
			return;
		}

		if ( !_collapsed && _elapsed >= PullDuration )
		{
			TriggerCollapse();
			return;
		}

		float t = MathF.Min( 1f, _elapsed / PullDuration );
		UpdateCore( t );
		UpdateHalo( t );
		UpdateBeams( t );
		UpdateGroundRing( t );
		SpawnInwardParticles();
		UpdateInwardParticles();
		PullMonsters( t );
		SuppressMonsterAttacks();
	}

	void UpdateCore( float t )
	{
		float size = CoreMaxSize * t;
		float scale = size / 50f;
		_coreGo.LocalScale = new Vector3( scale, scale, scale );
		_coreGo.LocalRotation = Rotation.FromAxis( Vector3.Up, _elapsed * 90f );
	}

	void UpdateHalo( float t )
	{
		float currentRadius = HaloRadius * t;

		foreach ( var p in _haloParticles )
		{
			if ( p.Go == null || !p.Go.IsValid() ) continue;

			float angle = p.AngleOffset + _elapsed * HaloSpinSpeed;
			p.Go.LocalPosition = new Vector3(
				MathF.Cos( angle ) * currentRadius,
				MathF.Sin( angle ) * currentRadius,
				CoreMaxSize * 0.5f + p.TiltOffset
			);

			float pulse = 1f + 0.2f * MathF.Sin( _elapsed * 5f + p.AngleOffset );
			p.Renderer.Size = new Vector2( HaloParticleSize * pulse * t, HaloParticleSize * pulse * t );

			var c = HaloColor;
			c.a = HaloColor.a * t;
			p.Renderer.Color = c;
		}
	}

	void UpdateBeams( float t )
	{
		for ( int i = 0; i < _beamGos.Count; i++ )
		{
			var go = _beamGos[i];
			var renderer = _beamRenderers[i];
			if ( go == null || !go.IsValid() ) continue;

			float thickness = BeamMaxThickness * t * ( 0.7f + 0.3f * MathF.Sin( _elapsed * 12f + i ) );
			float scaleY = thickness / 50f;
			go.LocalScale = new Vector3( BeamHeight / 50f, scaleY, scaleY );

			var c = BeamColor;
			c.a = BeamColor.a * t;
			renderer.Tint = c;
		}
	}

	void UpdateGroundRing( float t )
	{
		foreach ( var seg in _groundRingSegs )
		{
			if ( seg == null || !seg.IsValid() ) continue;
			var c = GroundRingColor;
			c.a = GroundRingColor.a * t;
			seg.Tint = c;
		}
	}

	void SpawnInwardParticles()
	{
		_inwardAccum += InwardParticleRate * Time.Delta;
		while ( _inwardAccum >= 1f )
		{
			float angle = Game.Random.Float( 0f, MathF.PI * 2f );
			float startRadius = Game.Random.Float( PullRadius * 0.7f, PullRadius );
			float startHeight = Game.Random.Float( 5f, 80f );

			var go = new GameObject( true, "InwardParticle" );
			go.SetParent( GameObject );
			Vector3 start = new Vector3( MathF.Cos( angle ) * startRadius, MathF.Sin( angle ) * startRadius, startHeight );
			go.LocalPosition = start;

			var sr = go.Components.Create<SpriteRenderer>();
			if ( _spriteAsset != null )
				sr.Sprite = _spriteAsset;
			sr.Color = InwardColor;
			float size = InwardParticleSize * Game.Random.Float( 0.6f, 1.3f );
			sr.Size = new Vector2( size, size );

			_inwardParticles.Add( new InwardParticle
			{
				Go = go,
				Renderer = sr,
				StartLocalPos = start,
				SpawnTime = _elapsed,
				Lifetime = Game.Random.Float( 0.5f, 0.9f ),
				BaseSize = size
			} );

			_inwardAccum -= 1f;
		}
	}

	void UpdateInwardParticles()
	{
		for ( int i = _inwardParticles.Count - 1; i >= 0; i-- )
		{
			var p = _inwardParticles[i];
			float age = _elapsed - p.SpawnTime;

			if ( age >= p.Lifetime || p.Go == null || !p.Go.IsValid() )
			{
				if ( p.Go != null && p.Go.IsValid() )
					p.Go.Destroy();
				_inwardParticles.RemoveAt( i );
				continue;
			}

			float u = age / p.Lifetime;
			float ease = u * u;

			Vector3 target = new Vector3( 0f, 0f, CoreMaxSize * 0.5f );
			p.Go.LocalPosition = Vector3.Lerp( p.StartLocalPos, target, ease );

			float sizeShrink = 1f - 0.5f * u;
			float size = p.BaseSize * sizeShrink;
			p.Renderer.Size = new Vector2( size, size );

			float fade = 1f - u;
			var c = InwardColor;
			c.a = InwardColor.a * fade;
			p.Renderer.Color = c;
		}
	}

	void PullMonsters( float t )
	{
		Vector3 center = WorldPosition;
		float radiusSqr = PullRadius * PullRadius;
		float pullStrength = 200f + t * 600f;

		foreach ( var monster in Scene.GetAllComponents<Monster>() )
		{
			if ( monster == null || !monster.IsValid() || monster.IsDead )
				continue;

			Vector3 to = center - monster.WorldPosition;
			to.z = 0f;
			float distSqr = to.LengthSquared;
			if ( distSqr > radiusSqr || distSqr < 1f )
				continue;

			float dist = MathF.Sqrt( distSqr );
			Vector3 dir = to / dist;

			float distFactor = 1f - ( dist / PullRadius );
			float moveAmount = pullStrength * ( 0.3f + distFactor ) * Time.Delta;
			if ( moveAmount > dist - 10f )
				moveAmount = MathF.Max( 0f, dist - 10f );

			monster.GameObject.WorldPosition += dir * moveAmount;
			_suppressedMonsters.Add( monster.GameObject );
		}
	}

	void SuppressMonsterAttacks()
	{
		foreach ( var go in _suppressedMonsters )
		{
			if ( go == null || !go.IsValid() ) continue;
			var monster = go.Components.Get<Monster>();
			if ( monster != null && !monster.IsDead )
				monster.ApplyFreeze( 0.15f );
		}
	}

	void ReleaseSuppressedMonsters()
	{
		_suppressedMonsters.Clear();
	}

	void TriggerCollapse()
	{
		_collapsed = true;
		_destroying = true;
		_destroyTimer = 0f;

		ApplyCollapseDamage();
		HideCorePhase();
		SpawnShockwave();
	}

	void ApplyCollapseDamage()
	{
		Vector3 center = WorldPosition;
		float radiusSqr = CollapseRadius * CollapseRadius;
		int dmg = CollapseDamage;
		if ( dmg < 1 ) dmg = 1;

		foreach ( var monster in Scene.GetAllComponents<Monster>() )
		{
			if ( monster == null || !monster.IsValid() || monster.IsDead )
				continue;
			if ( ( monster.WorldPosition - center ).LengthSquared > radiusSqr )
				continue;

			monster.TakeDamage( dmg, Source );
			DamagePopupBroadcaster.Broadcast( monster.WorldPosition + Vector3.Up * 60f, dmg, monster.MaxHealth, true );
		}

		foreach ( var boss in Scene.GetAllComponents<Boss>() )
		{
			if ( boss == null || !boss.IsValid() || boss.IsDead )
				continue;
			if ( ( boss.WorldPosition - center ).LengthSquared > radiusSqr )
				continue;

			boss.TakeDamage( dmg, Source );
			DamagePopupBroadcaster.Broadcast( boss.WorldPosition + Vector3.Up * 60f, dmg, boss.MaxHealth, true );
		}
	}

	void HideCorePhase()
	{
		if ( _coreGo != null && _coreGo.IsValid() )
			_coreGo.Enabled = false;

		foreach ( var p in _haloParticles )
		{
			if ( p.Go != null && p.Go.IsValid() )
				p.Go.Enabled = false;
		}

		for ( int i = _inwardParticles.Count - 1; i >= 0; i-- )
		{
			var p = _inwardParticles[i];
			if ( p.Go != null && p.Go.IsValid() )
				p.Go.Destroy();
		}
		_inwardParticles.Clear();

		foreach ( var seg in _groundRingSegs )
		{
			if ( seg != null && seg.IsValid() )
				seg.GameObject.Enabled = false;
		}
	}

	void SpawnShockwave()
	{
		for ( int i = 0; i < ShockwaveParticleCount; i++ )
		{
			float angle = ( (float)i / ShockwaveParticleCount ) * MathF.PI * 2f + Game.Random.Float( -0.05f, 0.05f );
			Vector3 dir = new Vector3( MathF.Cos( angle ), MathF.Sin( angle ), 0f );

			var go = new GameObject( true, $"Shock{i}" );
			go.SetParent( GameObject );
			go.LocalPosition = new Vector3( 0f, 0f, 30f );

			var sr = go.Components.Create<SpriteRenderer>();
			if ( _spriteAsset != null )
				sr.Sprite = _spriteAsset;
			sr.Color = ShockwaveColor;
			sr.Size = new Vector2( ShockwaveParticleSize, ShockwaveParticleSize );

			_shockwaveParticles.Add( new ShockwaveParticle
			{
				Go = go,
				Renderer = sr,
				Direction = dir,
				SpawnTime = _elapsed
			} );
		}
	}

	void UpdateShockwave()
	{
		float t = _destroyTimer / CollapseFlashDuration;
		float fade = 1f - t;

		foreach ( var s in _shockwaveParticles )
		{
			if ( s.Go == null || !s.Go.IsValid() ) continue;

			float dist = ShockwaveExpandSpeed * _destroyTimer;
			s.Go.LocalPosition = new Vector3( s.Direction.x * dist, s.Direction.y * dist, 30f );

			float growth = 1f + t * 1.5f;
			float size = ShockwaveParticleSize * growth * fade;
			s.Renderer.Size = new Vector2( size, size );

			var c = ShockwaveColor;
			c.a = fade;
			s.Renderer.Color = c;
		}

		float beamFade = 1f - t * 1.5f;
		if ( beamFade < 0f ) beamFade = 0f;
		for ( int i = 0; i < _beamRenderers.Count; i++ )
		{
			var r = _beamRenderers[i];
			if ( r == null || !r.IsValid() ) continue;
			var c = BeamColor;
			c.a = c.a * beamFade;
			r.Tint = c;
		}
	}
}