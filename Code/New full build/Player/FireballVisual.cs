using Sandbox;
using System;

public sealed class FireballVisual : Component
{
	[Property] public float Scale { get; set; } = 0.7f;

	[Property] public Color LightColor { get; set; } = new Color( 1f, 0.45f, 0.12f );
	[Property] public float LightRadius { get; set; } = 220f;

	[Property] public float SpawnInterval { get; set; } = 0.012f;
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
			float pulse = 1f + MathF.Sin( Time.Now * 8f ) * 0.3f;
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

		float spread = 13f * Scale;
		float life = 0.6f;
		float size = 4.5f * Scale;
		Vector3 back = -WorldRotation.Forward;

		_trail[_count++] = new Mote
		{
			Position = WorldPosition + Vector3.Random.Normal * spread,
			Velocity = back * 30f + Vector3.Random.Normal * 20f + Vector3.Up * 10f,
			Life = life + Game.Random.Float( -0.1f, 0.1f ),
			MaxLife = life,
			Size = size + Game.Random.Float( -1f, 1f ) * Scale
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
			_trail[i].Velocity *= ( 1f - dt * 3f );
			_trail[i].Position += Vector3.Up * 20f * dt * Scale;
			_trail[i].Size += dt * 3f * Scale;
		}
	}

	void RenderTrail()
	{
		for ( int c = 0; c < CoreFlashes; c++ )
		{
			Vector3 p = WorldPosition + Vector3.Random.Normal * ( 4f * Scale );
			Gizmo.Draw.Color = new Color( 1f, 0.92f, 0.55f, 0.5f );
			Gizmo.Draw.SolidSphere( p, 5.5f * Scale );
		}

		for ( int i = 0; i < _count; i++ )
		{
			float t = 1f - ( _trail[i].Life / _trail[i].MaxLife );
			float alpha = ( 1f - t ) * 0.7f;

			Gizmo.Draw.Color = new Color( 1f, 0.3f + t * 0.6f, 0.05f + t * 0.1f, alpha );
			Gizmo.Draw.SolidSphere( _trail[i].Position, _trail[i].Size * ( 1f - t * 0.5f ) );
		}
	}
}
