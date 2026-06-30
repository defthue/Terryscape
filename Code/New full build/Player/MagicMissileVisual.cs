using Sandbox;
using System;

public sealed class MagicMissileVisual : Component
{
	[Property] public float Scale { get; set; } = 0.6f;
	[Property] public Color LightColor { get; set; } = new Color( 0.62f, 0.30f, 1f );
	[Property] public float LightRadius { get; set; } = 200f;
	[Property] public float SpawnInterval { get; set; } = 0.01f;
	[Property] public int CoreFlashes { get; set; } = 2;

	struct Mote
	{
		public Vector3 Position;
		public Vector3 Velocity;
		public float Life;
		public float MaxLife;
		public float Size;
	}

	Mote[] _trail = new Mote[256];
	int _count;
	float _timer;
	PointLight _light;

	protected override void OnStart()
	{
		_light = Components.Get<PointLight>();
		if ( _light == null )
			_light = Components.Create<PointLight>();

		_light.LightColor = LightColor;
		_light.Radius = LightRadius;
	}

	protected override void OnUpdate()
	{
		float dt = Time.Delta;

		if ( _light != null && _light.IsValid() )
		{
			float pulse = 1f + MathF.Sin( Time.Now * 10f ) * 0.25f;
			_light.Radius = LightRadius * pulse;
		}

		_timer -= dt;
		while ( _timer <= 0f )
		{
			_timer += SpawnInterval;
			SpawnMote();
		}

		UpdateTrail( dt );
		RenderTrail();
	}

	void SpawnMote()
	{
		if ( _count >= _trail.Length )
			return;

		float spread = 8f * Scale;
		float life = 0.5f;
		float size = 3.5f * Scale;
		Vector3 back = -WorldRotation.Forward;
		Vector3 swirl = WorldRotation.Right * MathF.Sin( Time.Now * 30f ) * 18f
			+ WorldRotation.Up * MathF.Cos( Time.Now * 30f ) * 18f;

		_trail[_count++] = new Mote
		{
			Position = WorldPosition + Vector3.Random.Normal * spread,
			Velocity = back * 22f + swirl + Vector3.Random.Normal * 10f,
			Life = life + Game.Random.Float( -0.08f, 0.08f ),
			MaxLife = life,
			Size = size + Game.Random.Float( -0.8f, 0.8f ) * Scale
		};
	}

	void UpdateTrail( float dt )
	{
		for ( int i = _count - 1; i >= 0; i-- )
		{
			_trail[i].Life -= dt;
			if ( _trail[i].Life <= 0f )
			{
				_trail[i] = _trail[--_count];
				continue;
			}

			_trail[i].Position += _trail[i].Velocity * dt;
			_trail[i].Velocity *= ( 1f - dt * 3.5f );
			_trail[i].Size += dt * 2f * Scale;
		}
	}

	void RenderTrail()
	{
		for ( int c = 0; c < CoreFlashes; c++ )
		{
			Vector3 p = WorldPosition + Vector3.Random.Normal * ( 3f * Scale );
			SpellGizmo.SoftSphere( p, 4.5f * Scale, new Color( 0.85f, 0.7f, 1f, 0.55f ) );
		}

		for ( int i = 0; i < _count; i++ )
		{
			float t = 1f - ( _trail[i].Life / _trail[i].MaxLife );
			float alpha = ( 1f - t ) * 0.7f;
			var col = new Color( 0.55f + t * 0.25f, 0.2f + t * 0.2f, 0.95f, alpha );
			SpellGizmo.SoftSphere( _trail[i].Position, _trail[i].Size * ( 1f - t * 0.4f ), col );
		}
	}
}
