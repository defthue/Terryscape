using Sandbox;
using System;

public static class SpellGizmo
{
	public static void SoftSphere( Vector3 worldPos, float size, Color color )
	{
		Gizmo.Draw.Color = color.WithAlpha( color.a * 0.30f );
		Gizmo.Draw.SolidSphere( worldPos, size * 1.4f );

		Gizmo.Draw.Color = color.WithAlpha( color.a * 0.55f );
		Gizmo.Draw.SolidSphere( worldPos, size );

		Gizmo.Draw.Color = color.WithAlpha( color.a * 0.85f );
		Gizmo.Draw.SolidSphere( worldPos, size * 0.55f );
	}

	public static void SoftLine( Vector3 from, Vector3 to, float size, Color color, int steps )
	{
		if ( steps < 1 ) steps = 1;
		for ( int i = 0; i <= steps; i++ )
			SoftSphere( Vector3.Lerp( from, to, (float)i / steps ), size, color );
	}

	public static void SoftRing( Vector3 center, float radius, float size, Color color, int count )
	{
		if ( count < 1 ) count = 1;
		for ( int i = 0; i < count; i++ )
		{
			float ang = ( (float)i / count ) * MathF.PI * 2f;
			SoftSphere( center + new Vector3( MathF.Cos( ang ) * radius, MathF.Sin( ang ) * radius, 0f ), size, color );
		}
	}
}
