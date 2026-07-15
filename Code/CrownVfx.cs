using Sandbox;
using System;

[Title( "Crown Vfx" ), Group( "Vfx" ), Icon( "workspace_premium" )]
public sealed class CrownVfx : Component
{
	[Property, Group( "Setup" )] public float Scale { get; set; } = 1f;
	[Property, Group( "Setup" )] public bool AutoTurn { get; set; } = true;
	[Property, Group( "Setup" )] public float TurnSpeed { get; set; } = 20f;

	[Property, Group( "Design" )] public int PointCount { get; set; } = 7;
	[Property, Group( "Design" )] public float BandRadius { get; set; } = 16f;
	[Property, Group( "Design" )] public float PointHeight { get; set; } = 18f;

	[Property, Group( "Colors" )] public Color GoldColor { get; set; } = new Color( 0.88f, 0.75f, 0.38f );
	[Property, Group( "Colors" )] public Color GoldDark { get; set; } = new Color( 0.79f, 0.66f, 0.30f );
	[Property, Group( "Colors" )] public Color GemColor { get; set; } = new Color( 0.95f, 0.15f, 0.5f );

	readonly GizmoPaint _paint = new GizmoPaint();

	protected override void OnUpdate()
	{
		float t = Time.Now;
		float s = MathF.Max( Scale, 0.05f );

		Rotation rot = WorldRotation;
		if ( AutoTurn )
			rot = Rotation.FromYaw( t * TurnSpeed );

		Vector3 center = WorldPosition + Vector3.Up * ( 40f * s + MathF.Sin( t * 1.2f ) * 2f * s );

		Draw( _paint, center, rot, s, PointCount, BandRadius, PointHeight, GoldColor, GoldDark, GemColor, t );

		_paint.Flush( Scene );
	}

	public static void Draw( GizmoPaint paint, Vector3 center, Rotation rot, float s,
		int pointCount, float bandRadius, float pointHeight,
		Color gold, Color goldDark, Color gem, float t )
	{
		float r = bandRadius * s;

		Color goldO = gold.WithAlpha( 1f );
		Color darkO = goldDark.WithAlpha( 1f );

		paint.Cylinder( center - rot.Up * 1.5f * s, center + rot.Up * 6.5f * s, r, goldO );
		paint.Cylinder( center - rot.Up * 4f * s, center - rot.Up * 1f * s, r * 1.09f, darkO );

		int n = Math.Max( pointCount, 3 );
		float h = pointHeight * s;

		for ( int i = 0; i < n; i++ )
		{
			float ang = i * ( 360f / n );
			Rotation around = rot * Rotation.FromYaw( ang );
			Vector3 outward = around.Forward;

			Vector3 basePos = center + outward * ( r * 0.82f ) + rot.Up * 3f * s;
			Vector3 dir = ( rot.Up * 1f + outward * 0.1f ).Normal;

			paint.ShadedCone( basePos, dir, h, 4.6f * s, goldO );
			paint.Sphere( basePos + dir * ( h + 1.2f * s ), 3f * s, goldO );
		}

		float glintCycle = ( t * 0.8f ) % 1f;
		if ( glintCycle < 0.12f )
		{
			int glintPoint = (int)( t * 0.8f ) % n;
			float ang = glintPoint * ( 360f / n );
			Rotation around = rot * Rotation.FromYaw( ang );
			Vector3 outward = around.Forward;
			Vector3 basePos = center + outward * ( r * 0.82f ) + rot.Up * 3f * s;
			Vector3 dir = ( rot.Up * 1f + outward * 0.1f ).Normal;

			float f = MathF.Sin( ( glintCycle / 0.12f ) * MathF.PI );
			paint.Sphere( basePos + dir * ( h + 1.2f * s ) + rot.Up * 1f * s, 1.1f * s * f, new Color( 1f, 1f, 0.92f, 1f ) );
		}
	}
}
