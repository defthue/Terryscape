using Sandbox;
using System;

public sealed class FireballVisual : Component
{
	[Property] public Sprite FlameSprite { get; set; }

	[Property] public Color CoreColor { get; set; } = new Color( 1f, 0.8f, 0.45f, 1f );
	[Property] public Color HeadColor { get; set; } = new Color( 1f, 0.32f, 0.08f, 1f );
	[Property] public Color LightColor { get; set; } = new Color( 1f, 0.38f, 0.12f );

	[Property] public float CoreSize { get; set; } = 30f;
	[Property] public float HeadSize { get; set; } = 52f;
	[Property] public float LightRadius { get; set; } = 320f;

	[Property] public float EmitRate { get; set; } = 75f;
	[Property] public float ParticleLifetime { get; set; } = 0.45f;
	[Property] public float SpawnRadius { get; set; } = 7f;
	[Property] public float RiseSpeed { get; set; } = 45f;
	[Property] public float TrailSpeed { get; set; } = 60f;
	[Property] public float Jitter { get; set; } = 35f;

	[Property] public Color FlameStart { get; set; } = new Color( 1f, 0.72f, 0.3f, 1f );
	[Property] public Color FlameMid { get; set; } = new Color( 1f, 0.22f, 0.04f, 0.9f );
	[Property] public Color FlameEnd { get; set; } = new Color( 0.16f, 0.015f, 0f, 0f );

	[Property] public float FlameStartSize { get; set; } = 14f;
	[Property] public float FlamePeakSize { get; set; } = 38f;
	[Property] public float FlameEndSize { get; set; } = 10f;

	SpriteRenderer _core;
	SpriteRenderer _head;
	PointLight _light;
	float _t;
	float _emitAccum;

	protected override void OnStart()
	{
		_head = SpellVfx.CreateSprite( GameObject, FlameSprite, HeadColor, HeadSize, true, false );
		_core = SpellVfx.CreateSprite( GameObject, FlameSprite, CoreColor, CoreSize, true, false );
		_light = SpellVfx.CreateLight( GameObject, LightColor, LightRadius );
	}

	protected override void OnUpdate()
	{
		_t += Time.Delta;

		float flick = 0.85f + 0.15f * MathF.Sin( _t * 32f );
		float flick2 = 0.9f + 0.12f * MathF.Sin( _t * 49f + 1.1f );

		if ( _core != null && _core.IsValid() )
			_core.Size = new Vector2( CoreSize * flick, CoreSize * flick );

		if ( _head != null && _head.IsValid() )
			_head.Size = new Vector2( HeadSize * flick2, HeadSize * flick2 );

		if ( _light != null && _light.IsValid() )
			_light.Radius = LightRadius * ( 0.85f + 0.2f * MathF.Sin( _t * 26f ) );

		Vector3 back = -GameObject.WorldRotation.Forward;

		_emitAccum += EmitRate * Time.Delta;
		while ( _emitAccum >= 1f )
		{
			_emitAccum -= 1f;
			EmitFlame( back );
		}
	}

	void EmitFlame( Vector3 back )
	{
		Vector3 offset = new Vector3(
			Game.Random.Float( -SpawnRadius, SpawnRadius ),
			Game.Random.Float( -SpawnRadius, SpawnRadius ),
			Game.Random.Float( -SpawnRadius, SpawnRadius ) );

		Vector3 vel = back * ( TrailSpeed * Game.Random.Float( 0.6f, 1.1f ) )
			+ Vector3.Up * ( RiseSpeed * Game.Random.Float( 0.6f, 1.2f ) )
			+ new Vector3(
				Game.Random.Float( -Jitter, Jitter ),
				Game.Random.Float( -Jitter, Jitter ),
				Game.Random.Float( -Jitter * 0.4f, Jitter ) );

		float startSize = FlameStartSize * Game.Random.Float( 0.8f, 1.2f );
		float peakSize = FlamePeakSize * Game.Random.Float( 0.8f, 1.2f );
		float life = ParticleLifetime * Game.Random.Float( 0.8f, 1.2f );

		SpellTrailPuff.Spawn( Scene, WorldPosition + offset, FlameSprite, vel, 0f, 1.5f,
			FlameStart, FlameMid, FlameEnd,
			startSize, peakSize, FlameEndSize, 0.22f, life,
			true, false );
	}
}
