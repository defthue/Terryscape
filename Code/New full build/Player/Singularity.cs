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

	public GameObject Source { get; set; }
	public bool VisualOnly { get; set; }

	float _elapsed;
	float _inwardAccum;
	bool _collapsed;
	bool _destroying;
	float _destroyTimer;

	List<HaloParticle> _haloParticles = new();
	List<InwardParticle> _inwardParticles = new();
	List<BeamParticle> _beamParticles = new();
	List<ShockwaveParticle> _shockwaveParticles = new();
	HashSet<GameObject> _suppressedMonsters = new();

	class HaloParticle
	{
		public float AngleOffset;
		public float TiltOffset;
	}

	class InwardParticle
	{
		public Vector3 StartLocalPos;
		public float SpawnTime;
		public float Lifetime;
		public float BaseSize;
	}

	class BeamParticle
	{
		public Vector3 BaseOffset;
	}

	class ShockwaveParticle
	{
		public Vector3 Direction;
		public float SpawnTime;
	}

	public static Singularity Spawn( Scene scene, Vector3 position, GameObject source, float pullRadius, float collapseRadius, float pullDuration, int collapseDamage, bool visualOnly = false )
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
		sing.VisualOnly = visualOnly;

		return sing;
	}

	protected override void OnStart()
	{
		BuildHalo();
		BuildBeams();

		if ( !VisualOnly )
			SoundLibrary.PlaySingularity( WorldPosition );
	}

	void BuildHalo()
	{
		for ( int i = 0; i < HaloParticleCount; i++ )
		{
			_haloParticles.Add( new HaloParticle
			{
				AngleOffset = ( (float)i / HaloParticleCount ) * MathF.PI * 2f,
				TiltOffset = Game.Random.Float( -8f, 8f )
			} );
		}
	}

	void BuildBeams()
	{
		for ( int i = 0; i < BeamCount; i++ )
		{
			float angle = ( (float)i / BeamCount ) * MathF.PI * 2f;
			float lateralOffset = Game.Random.Float( 15f, 35f );

			_beamParticles.Add( new BeamParticle
			{
				BaseOffset = new Vector3( MathF.Cos( angle ) * lateralOffset, MathF.Sin( angle ) * lateralOffset, 0f )
			} );
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

		PullLocalPlayer( t );

		if ( !VisualOnly )
		{
			PullMonsters( t );
			SuppressMonsterAttacks();
		}
	}

	void PullLocalPlayer( float t )
	{
		var localPlayer = PlayerHelper.GetLocalPlayer();
		if ( localPlayer == null || !localPlayer.IsValid() )
			return;

		if ( !PvpCombat.CanDamage( Source, localPlayer ) )
			return;

		Vector3 center = WorldPosition;
		Vector3 to = center - localPlayer.WorldPosition;
		to.z = 0f;
		float distSqr = to.LengthSquared;
		if ( distSqr > PullRadius * PullRadius || distSqr < 1f )
			return;

		float dist = MathF.Sqrt( distSqr );
		Vector3 dir = to / dist;

		float pullStrength = 200f + t * 600f;
		float distFactor = 1f - ( dist / PullRadius );
		float moveAmount = pullStrength * ( 0.3f + distFactor ) * Time.Delta;
		if ( moveAmount > dist - 10f )
			moveAmount = MathF.Max( 0f, dist - 10f );

		localPlayer.WorldPosition += dir * moveAmount;
	}

	void UpdateCore( float t )
	{
		float size = CoreMaxSize * t;
		Vector3 coreWorldPos = WorldPosition + new Vector3( 0f, 0f, CoreMaxSize * 0.5f );
		SpellGizmo.SoftSphere( coreWorldPos, size, CoreColor.WithAlpha( 1f ) );
	}

	void UpdateHalo( float t )
	{
		float currentRadius = HaloRadius * t;

		foreach ( var p in _haloParticles )
		{
			float angle = p.AngleOffset + _elapsed * HaloSpinSpeed;
			Vector3 localPos = new Vector3(
				MathF.Cos( angle ) * currentRadius,
				MathF.Sin( angle ) * currentRadius,
				CoreMaxSize * 0.5f + p.TiltOffset
			);

			float pulse = 1f + 0.2f * MathF.Sin( _elapsed * 5f + p.AngleOffset );
			float size = HaloParticleSize * pulse * t;

			var c = HaloColor;
			c.a = HaloColor.a * t;

			SpellGizmo.SoftSphere( WorldPosition + localPos, size, c );
		}
	}

	void UpdateBeams( float t )
	{
		for ( int i = 0; i < _beamParticles.Count; i++ )
		{
			var beam = _beamParticles[i];

			float thickness = BeamMaxThickness * t * ( 0.7f + 0.3f * MathF.Sin( _elapsed * 12f + i ) );

			var c = BeamColor;
			c.a = BeamColor.a * t;

			Vector3 from = WorldPosition + beam.BaseOffset;
			Vector3 to = from + new Vector3( 0f, 0f, BeamHeight );
			SpellGizmo.SoftLine( from, to, thickness, c, 12 );
		}
	}

	void UpdateGroundRing( float t )
	{
		var c = GroundRingColor;
		c.a = GroundRingColor.a * t;
		SpellGizmo.SoftRing( WorldPosition, PullRadius, GroundRingThickness, c, GroundRingSegments );
	}

	void SpawnInwardParticles()
	{
		_inwardAccum += InwardParticleRate * Time.Delta;
		while ( _inwardAccum >= 1f )
		{
			float angle = Game.Random.Float( 0f, MathF.PI * 2f );
			float startRadius = Game.Random.Float( PullRadius * 0.7f, PullRadius );
			float startHeight = Game.Random.Float( 5f, 80f );

			Vector3 start = new Vector3( MathF.Cos( angle ) * startRadius, MathF.Sin( angle ) * startRadius, startHeight );

			float size = InwardParticleSize * Game.Random.Float( 0.6f, 1.3f );

			_inwardParticles.Add( new InwardParticle
			{
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

			if ( age >= p.Lifetime )
			{
				_inwardParticles.RemoveAt( i );
				continue;
			}

			float u = age / p.Lifetime;
			float ease = u * u;

			Vector3 target = new Vector3( 0f, 0f, CoreMaxSize * 0.5f );
			Vector3 localPos = Vector3.Lerp( p.StartLocalPos, target, ease );

			float sizeShrink = 1f - 0.5f * u;
			float size = p.BaseSize * sizeShrink;

			float fade = 1f - u;
			var c = InwardColor;
			c.a = InwardColor.a * fade;

			SpellGizmo.SoftSphere( WorldPosition + localPos, size, c );
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

		if ( !VisualOnly )
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
			DamagePopupBroadcaster.Broadcast( monster.WorldPosition + Vector3.Up * 60f, dmg, monster.MaxHealth, true, DamagePopupBroadcaster.SteamIdOf( Source ), 0 );
		}

		foreach ( var boss in Scene.GetAllComponents<Boss>() )
		{
			if ( boss == null || !boss.IsValid() || boss.IsDead )
				continue;
			if ( ( boss.WorldPosition - center ).LengthSquared > radiusSqr )
				continue;

			boss.TakeDamage( dmg, Source );
			DamagePopupBroadcaster.Broadcast( boss.WorldPosition + Vector3.Up * 60f, dmg, boss.MaxHealth, true, DamagePopupBroadcaster.SteamIdOf( Source ), 0 );
		}

		foreach ( var slimeKing in Scene.GetAllComponents<SlimeKing>() )
		{
			if ( slimeKing == null || !slimeKing.IsValid() || slimeKing.IsDead )
				continue;
			if ( ( slimeKing.WorldPosition - center ).LengthSquared > radiusSqr )
				continue;

			slimeKing.TakeDamage( dmg, Source );
			DamagePopupBroadcaster.Broadcast( slimeKing.WorldPosition + Vector3.Up * 60f, dmg, slimeKing.MaxHealth, true, DamagePopupBroadcaster.SteamIdOf( Source ), 0 );
		}

		foreach ( var player in PlayerHelper.GetAllPlayers() )
		{
			if ( player == null || !player.IsValid() )
				continue;
			if ( ( player.WorldPosition - center ).LengthSquared > radiusSqr )
				continue;
			if ( !PvpCombat.CanDamage( Source, player ) )
				continue;

			int dealt = PvpCombat.ResolveDamage( dmg, CombatStyle.Magic, player );
			var health = player.Components.Get<PlayerHealth>();
			if ( health == null )
				continue;

			int applied = health.TakeDamage( dealt );
			Source?.Components.Get<PlayerCombat>()?.NotifyPvpHit( player, applied, true, true );
		}
	}

	void HideCorePhase()
	{
		_inwardParticles.Clear();
	}

	void SpawnShockwave()
	{
		for ( int i = 0; i < ShockwaveParticleCount; i++ )
		{
			float angle = ( (float)i / ShockwaveParticleCount ) * MathF.PI * 2f + Game.Random.Float( -0.05f, 0.05f );
			Vector3 dir = new Vector3( MathF.Cos( angle ), MathF.Sin( angle ), 0f );

			_shockwaveParticles.Add( new ShockwaveParticle
			{
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
			float dist = ShockwaveExpandSpeed * _destroyTimer;
			Vector3 localPos = new Vector3( s.Direction.x * dist, s.Direction.y * dist, 30f );

			float growth = 1f + t * 1.5f;
			float size = ShockwaveParticleSize * growth * fade;

			var c = ShockwaveColor;
			c.a = fade;

			SpellGizmo.SoftSphere( WorldPosition + localPos, size, c );
		}
	}
}
