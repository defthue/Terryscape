using Sandbox;
using System;

[Title( "Magma Worm Head Vfx" ), Group( "Vfx" ), Icon( "local_fire_department" )]
public sealed class MagmaWormHeadVfx : Component
{
	[Property, Group( "Setup" )] public float Scale { get; set; } = 1f;
	[Property, Group( "Setup" )] public bool AutoTurn { get; set; } = true;
	[Property, Group( "Setup" )] public float TurnSpeed { get; set; } = 16f;
	[Property, Group( "Setup" )] public float ChompSpeed { get; set; } = 0.9f;
	[Property, Group( "Setup" )] public bool ShowNeck { get; set; } = true;

	[Property, Group( "Colors" )] public Color BodyColor { get; set; } = new Color( 0.42f, 0.13f, 0.11f );
	[Property, Group( "Colors" )] public Color PlateColor { get; set; } = new Color( 0.72f, 0.22f, 0.14f );
	[Property, Group( "Colors" )] public Color HornColor { get; set; } = new Color( 0.88f, 0.45f, 0.16f );
	[Property, Group( "Colors" )] public Color GlowColor { get; set; } = new Color( 1f, 0.55f, 0.12f );
	[Property, Group( "Colors" )] public Color ToothColor { get; set; } = new Color( 1f, 0.78f, 0.45f );

	struct Ember
	{
		public Vector3 Position;
		public Vector3 Velocity;
		public float Life;
		public float MaxLife;
		public float Size;
	}

	Ember[] _embers = new Ember[24];
	int _emberCount;
	float _emberTimer;

	readonly GizmoPaint _paint = new GizmoPaint();

	protected override void OnUpdate()
	{
		float t = Time.Now;
		float s = MathF.Max( Scale, 0.05f );

		Rotation rot = WorldRotation;
		if ( AutoTurn )
			rot = Rotation.FromYaw( t * TurnSpeed );
		rot *= Rotation.FromPitch( MathF.Sin( t * 0.8f ) * 3f );

		Vector3 center = WorldPosition + Vector3.Up * ( 60f * s + MathF.Sin( t * 1.1f ) * 3f * s );

		float chomp = MathF.Pow( MathF.Max( 0f, MathF.Sin( t * ChompSpeed ) ), 4f );
		float jawDeg = chomp * 26f;
		Rotation jawRot = Rotation.FromAxis( Vector3.Left, jawDeg );
		Vector3 hinge = new Vector3( -10f, 0f, -6f );

		DrawSkull( center, rot, s );
		DrawArmor( center, rot, s );
		DrawJaw( center, rot, jawRot, hinge, s );
		DrawTeeth( center, rot, jawRot, hinge, s );
		DrawHorns( center, rot, s );
		DrawCrest( center, rot, s, t );
		DrawCheekSpikes( center, rot, s );
		DrawEyes( center, rot, s, t );
		if ( ShowNeck )
			DrawNeck( center, rot, s, t );
		DrawMouthFire( center, rot, s, chomp );
		UpdateEmbers( center, rot, s, chomp );
		QueueEmbers();

		_paint.Flush( Scene );
	}

	void DrawSkull( Vector3 center, Rotation rot, float s )
	{
		Color body = BodyColor.WithAlpha( 1f );

		_paint.Sphere( center, 24f * s, body );
		_paint.Cylinder( center + rot.Forward * 21f * s - rot.Up * 3f * s, center + rot.Forward * 36f * s - rot.Up * 5f * s, 12f * s, body );
		_paint.Sphere( center + rot.Forward * 39f * s - rot.Up * 5f * s, 11f * s, body );

		Color dark = new Color( 0.08f, 0.02f, 0.02f, 1f );
		_paint.Sphere( center + rot * ( new Vector3( 48f, 4f, -1f ) * s ), 1.5f * s, dark );
		_paint.Sphere( center + rot * ( new Vector3( 48f, -4f, -1f ) * s ), 1.5f * s, dark );
	}

	void DrawArmor( Vector3 center, Rotation rot, float s )
	{
		Color plate = PlateColor.WithAlpha( 1f );

		for ( int side = -1; side <= 1; side += 2 )
		{
			for ( int i = 0; i < 2; i++ )
			{
				Vector3 stud = center + rot * ( new Vector3( 4f - i * 14f, side * 21f, 10f ) * s );
				Vector3 dir = ( rot * new Vector3( 0f, side * 0.75f, 0.65f ) ).Normal;
				_paint.ShadedCone( stud, dir, 7f * s, 2.6f * s, plate );
			}
		}
	}

	void DrawJaw( Vector3 center, Rotation rot, Rotation jawRot, Vector3 hinge, float s )
	{
		Color body = BodyColor.WithAlpha( 1f );

		Vector3 backLocal = hinge + jawRot * ( new Vector3( 2f, 0f, -16f ) - hinge );
		Vector3 chinLocal = hinge + jawRot * ( new Vector3( 36f, 0f, -15f ) - hinge );

		Vector3 back = center + rot * ( backLocal * s );
		Vector3 chin = center + rot * ( chinLocal * s );

		_paint.Cylinder( back, chin, 8.5f * s, body );
		_paint.Sphere( chin, 8f * s, body );
	}

	void DrawTeeth( Vector3 center, Rotation rot, Rotation jawRot, Vector3 hinge, float s )
	{
		Color tooth = ToothColor.WithAlpha( 1f );

		for ( int i = 0; i < 4; i++ )
		{
			float x = 16f + i * 8f;
			float len = ( i == 3 ? 9f : 6.5f ) * s;

			for ( int side = -1; side <= 1; side += 2 )
			{
				Vector3 upperBase = center + rot * ( new Vector3( x, side * 8f, -8f ) * s );
				Vector3 upperDir = ( rot * new Vector3( 0.12f, 0f, -1f ) ).Normal;
				_paint.Cone( upperBase, upperDir, len, 2.2f * s, tooth );
			}
		}

		for ( int i = 0; i < 3; i++ )
		{
			float x = 18f + i * 8f;
			float len = ( i == 2 ? 8f : 5.5f ) * s;

			for ( int side = -1; side <= 1; side += 2 )
			{
				Vector3 baseLocal = hinge + jawRot * ( new Vector3( x, side * 6f, -9f ) - hinge );
				Vector3 basePos = center + rot * ( baseLocal * s );
				Vector3 dir = ( rot * jawRot * new Vector3( 0.1f, 0f, 1f ) ).Normal;
				_paint.Cone( basePos, dir, len, 2f * s, tooth );
			}
		}
	}

	void DrawHorns( Vector3 center, Rotation rot, float s )
	{
		Color horn = HornColor.WithAlpha( 1f );
		_paint.ShadedCone( center + rot * ( new Vector3( -12f, 8f, 20f ) * s ), ( rot * new Vector3( -0.75f, 0.18f, 0.64f ) ).Normal, 38f * s, 7f * s, horn );
		_paint.ShadedCone( center + rot * ( new Vector3( -12f, -8f, 20f ) * s ), ( rot * new Vector3( -0.75f, -0.18f, 0.64f ) ).Normal, 38f * s, 7f * s, horn );
		_paint.ShadedCone( center + rot * ( new Vector3( -18f, 15f, 12f ) * s ), ( rot * new Vector3( -0.6f, 0.55f, 0.58f ) ).Normal, 24f * s, 5f * s, horn );
		_paint.ShadedCone( center + rot * ( new Vector3( -18f, -15f, 12f ) * s ), ( rot * new Vector3( -0.6f, -0.55f, 0.58f ) ).Normal, 24f * s, 5f * s, horn );
	}

	void DrawCrest( Vector3 center, Rotation rot, float s, float t )
	{
		float[] xs = new float[] { 4f, -8f, -20f };
		float[] heights = new float[] { 14f, 18f, 15f };

		for ( int i = 0; i < xs.Length; i++ )
		{
			float sway = MathF.Sin( t * 1.5f + i * 0.7f ) * 1.5f;
			Vector3 baseFront = center + rot * ( new Vector3( xs[i] + 5f, 0f, 23f ) * s );
			Vector3 baseBack = center + rot * ( new Vector3( xs[i] - 5f, 0f, 23f ) * s );
			Vector3 tip = center + rot * ( new Vector3( xs[i] - 9f + sway, 0f, 23f + heights[i] ) * s );

			_paint.DoubleTri( baseFront, baseBack, tip, PlateColor.WithAlpha( 1f ) );
		}
	}

	void DrawCheekSpikes( Vector3 center, Rotation rot, float s )
	{
		Color plate = PlateColor.WithAlpha( 1f );

		for ( int side = -1; side <= 1; side += 2 )
		{
			_paint.ShadedCone( center + rot * ( new Vector3( -4f, side * 22f, 2f ) * s ), ( rot * new Vector3( 0.05f, side * 0.95f, 0.15f ) ).Normal, 15f * s, 4.5f * s, plate );
		}
	}

	void DrawEyes( Vector3 center, Rotation rot, float s, float t )
	{
		float pulse = 0.9f + MathF.Sin( t * 5f ) * 0.1f;
		float flare = 0.85f + MathF.Sin( t * 2.3f ) * 0.15f;

		for ( int side = -1; side <= 1; side += 2 )
		{
			Vector3 eye = center + rot * ( new Vector3( 18f, side * 15f, 10f ) * s );
			Vector3 fwd = ( rot * new Vector3( 0.6f, side * 0.4f, 0.15f ) ).Normal;

			_paint.Sphere( eye, 6.5f * s * flare, new Color( 0.35f, 0.05f, 0.03f, 0.45f ) );
			_paint.Sphere( eye + fwd * 1f * s, 4.6f * s * pulse, GlowColor.WithAlpha( 0.55f ) );
			_paint.Sphere( eye + fwd * 2f * s, 3f * s * pulse, new Color( 1f, 0.7f, 0.2f, 0.85f ) );
			_paint.Sphere( eye + fwd * 2.8f * s, 1.5f * s, new Color( 1f, 0.95f, 0.6f, 0.95f ) );
		}
	}

	void DrawNeck( Vector3 center, Rotation rot, float s, float t )
	{
		for ( int i = 0; i < 3; i++ )
		{
			float x0 = -24f - i * 17f;
			float droop = i * 2.5f;
			Vector3 a = center + rot * ( new Vector3( x0, 0f, 2f - droop ) * s );
			Vector3 b = center + rot * ( new Vector3( x0 - 15f, 0f, 0f - droop ) * s );

			float shade = 1f - i * 0.12f;
			Color seg = new Color( BodyColor.r * shade, BodyColor.g * shade, BodyColor.b * shade, 1f );
			_paint.Cylinder( a, b, ( 15f - i * 1.5f ) * s, seg );

			if ( i == 2 )
				_paint.Sphere( b, 13f * s, seg );

			float sway = MathF.Sin( t * 1.5f + i ) * 1.5f;
			Vector3 finBaseF = center + rot * ( new Vector3( x0 - 2f, 0f, 14f - droop ) * s );
			Vector3 finBaseB = center + rot * ( new Vector3( x0 - 12f, 0f, 14f - droop ) * s );
			Vector3 finTip = center + rot * ( new Vector3( x0 - 14f + sway, 0f, 28f - droop ) * s );

			_paint.DoubleTri( finBaseF, finBaseB, finTip, PlateColor.WithAlpha( 1f ) );
		}
	}

	void DrawMouthFire( Vector3 center, Rotation rot, float s, float chomp )
	{
		if ( chomp < 0.1f )
			return;

		Vector3 mouth = center + rot * ( new Vector3( 22f, 0f, -10f ) * s );

		_paint.Sphere( mouth, 7f * s * chomp, GlowColor.WithAlpha( 0.5f * chomp ) );
		_paint.Sphere( mouth + rot.Forward * 6f * s, 4.5f * s * chomp, new Color( 1f, 0.85f, 0.35f, 0.8f * chomp ) );
	}

	void UpdateEmbers( Vector3 center, Rotation rot, float s, float chomp )
	{
		float dt = Time.Delta;

		_emberTimer -= dt;
		float interval = chomp > 0.4f ? 0.05f : 0.12f;
		while ( _emberTimer <= 0f )
		{
			_emberTimer += interval;
			SpawnEmber( center, rot, s, chomp );
		}

		for ( int i = _emberCount - 1; i >= 0; i-- )
		{
			_embers[i].Life -= dt;
			if ( _embers[i].Life <= 0f )
			{
				_embers[i] = _embers[--_emberCount];
				continue;
			}

			_embers[i].Position += _embers[i].Velocity * dt;
			_embers[i].Velocity += Vector3.Up * 55f * s * dt;
			_embers[i].Velocity *= ( 1f - dt * 1.4f );
		}
	}

	void SpawnEmber( Vector3 center, Rotation rot, float s, float chomp )
	{
		if ( _emberCount >= _embers.Length )
			return;

		bool fromMouth = chomp > 0.4f && Game.Random.Float( 0f, 1f ) < 0.7f;

		Vector3 pos;
		Vector3 vel;

		if ( fromMouth )
		{
			pos = center + rot * ( new Vector3( 28f, Game.Random.Float( -5f, 5f ), -10f ) * s );
			vel = rot.Forward * Game.Random.Float( 35f, 80f ) * s + Vector3.Up * Game.Random.Float( 10f, 35f ) * s;
		}
		else
		{
			pos = center + rot * ( new Vector3( Game.Random.Float( -20f, 8f ), Game.Random.Float( -12f, 12f ), 26f ) * s );
			vel = Vector3.Up * Game.Random.Float( 15f, 40f ) * s + Vector3.Random.WithZ( 0f ) * 8f * s;
		}

		float life = Game.Random.Float( 0.4f, 0.9f );
		_embers[_emberCount++] = new Ember
		{
			Position = pos,
			Velocity = vel,
			Life = life,
			MaxLife = life,
			Size = Game.Random.Float( 1f, 2f ) * s
		};
	}

	void QueueEmbers()
	{
		for ( int i = 0; i < _emberCount; i++ )
		{
			float f = _embers[i].Life / _embers[i].MaxLife;
			Color col = Color.Lerp( GlowColor, new Color( 1f, 0.9f, 0.4f ), f ).WithAlpha( f * 0.85f );
			_paint.Sphere( _embers[i].Position, _embers[i].Size * ( 0.4f + f * 0.6f ), col );
		}
	}
}