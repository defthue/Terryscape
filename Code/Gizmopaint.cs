using Sandbox;
using System;
using System.Collections.Generic;

public sealed class GizmoPaint
{
	struct Item
	{
		public int Kind;
		public Vector3 A;
		public Vector3 B;
		public Vector3 C;
		public float R;
		public float Thickness;
		public Color Col;
		public Rotation Rot;
		public Vector3 Size;
		public float SortDist;
		public int Seq;
	}

	static readonly Vector3 DefaultLight = new Vector3( 0.35f, 0.25f, 0.9f ).Normal;

	public static bool LogCounts;
	static float _lastLog;

	readonly List<Item> _opaque = new List<Item>();
	readonly List<Item> _glow = new List<Item>();
	int _seq;

	public Vector3 LightDir { get; set; } = DefaultLight;

	List<Item> Bucket( Color c ) => c.a >= 0.999f ? _opaque : _glow;

	void Add( List<Item> list, Item it )
	{
		it.Seq = _seq++;
		list.Add( it );
	}

	public void Sphere( Vector3 pos, float radius, Color color )
	{
		Add( Bucket( color ), new Item { Kind = 0, A = pos, R = radius, Col = color } );
	}

	public void Cone( Vector3 basePos, Vector3 dir, float length, float radius, Color color )
	{
		Vector3 nd = dir.Normal;
		Add( Bucket( color ), new Item { Kind = 1, A = basePos, B = nd * length, R = radius, Col = color } );
	}

	public void ShadedCone( Vector3 basePos, Vector3 dir, float length, float radius, Color color )
	{
		Vector3 nd = dir.Normal;
		float bright = 0.65f + 0.35f * MathF.Max( 0f, Vector3.Dot( nd, LightDir ) );
		Cone( basePos, nd, length, radius, new Color( color.r * bright, color.g * bright, color.b * bright, color.a ) );
	}

	public void Tri( Vector3 a, Vector3 b, Vector3 c, Color color )
	{
		Add( Bucket( color ), new Item { Kind = 2, A = a, B = b, C = c, Col = color } );
	}

	public void DoubleTri( Vector3 a, Vector3 b, Vector3 c, Color color )
	{
		Tri( a, b, c, color );
		Tri( c, b, a, color );
	}

	public void Line( Vector3 a, Vector3 b, float thickness, Color color )
	{
		Add( Bucket( color ), new Item { Kind = 3, A = a, B = b, Thickness = thickness, Col = color } );
	}

	public void Cylinder( Vector3 a, Vector3 b, float radius, Color color )
	{
		Add( Bucket( color ), new Item { Kind = 4, A = a, B = b, R = radius, Col = color } );
	}

	public void ShadedBox( Vector3 center, Rotation rot, Vector3 size, Color color )
	{
		Add( Bucket( color ), new Item { Kind = 5, A = center, Rot = rot, Size = size, Col = color } );
	}

	public void Flush( Scene scene )
	{
		Vector3 camPos = scene != null && scene.Camera != null
			? scene.Camera.WorldPosition
			: Vector3.Up * 10000f;

		Gizmo.Draw.IgnoreDepth = false;

		if ( LogCounts && Time.Now - _lastLog > 1f )
		{
			_lastLog = Time.Now;
			Log.Info( $"[GizmoPaint] opaque={_opaque.Count} glow={_glow.Count}" );
		}

		SortAndDraw( _opaque, camPos );
		SortAndDraw( _glow, camPos );

		_opaque.Clear();
		_glow.Clear();
		_seq = 0;
	}

	static float BoundingRadius( in Item it )
	{
		if ( it.Kind == 0 )
			return it.R;
		if ( it.Kind == 1 )
			return it.B.Length * 0.5f + it.R;
		if ( it.Kind == 2 )
		{
			Vector3 centroid = ( it.A + it.B + it.C ) / 3f;
			float d = ( it.A - centroid ).Length;
			d = MathF.Max( d, ( it.B - centroid ).Length );
			d = MathF.Max( d, ( it.C - centroid ).Length );
			return d;
		}
		if ( it.Kind == 3 || it.Kind == 4 )
			return ( it.B - it.A ).Length * 0.5f + it.R + it.Thickness;
		return it.Size.Length * 0.5f;
	}

	void SortAndDraw( List<Item> items, Vector3 camPos )
	{
		for ( int i = 0; i < items.Count; i++ )
		{
			var it = items[i];
			Vector3 rep;
			if ( it.Kind == 2 )
				rep = ( it.A + it.B + it.C ) / 3f;
			else if ( it.Kind == 1 )
				rep = it.A + it.B * 0.5f;
			else if ( it.Kind == 3 || it.Kind == 4 )
				rep = ( it.A + it.B ) * 0.5f;
			else
				rep = it.A;

			it.SortDist = ( rep - camPos ).Length - BoundingRadius( it );
			items[i] = it;
		}

		items.Sort( ( x, y ) =>
		{
			int byDist = y.SortDist.CompareTo( x.SortDist );
			if ( byDist != 0 )
				return byDist;
			return x.Seq.CompareTo( y.Seq );
		} );

		for ( int i = 0; i < items.Count; i++ )
		{
			var it = items[i];

			if ( it.Kind == 5 )
			{
				DrawBoxFaces( it, camPos );
				continue;
			}

			Gizmo.Draw.Color = it.Col;

			if ( it.Kind == 0 )
				Gizmo.Draw.SolidSphere( it.A, it.R );
			else if ( it.Kind == 1 )
				Gizmo.Draw.SolidCone( it.A, it.B, it.R );
			else if ( it.Kind == 2 )
				Gizmo.Draw.SolidTriangle( new Triangle( it.A, it.B, it.C ) );
			else if ( it.Kind == 3 )
			{
				Gizmo.Draw.LineThickness = it.Thickness;
				Gizmo.Draw.Line( it.A, it.B );
			}
			else
				Gizmo.Draw.SolidCylinder( it.A, it.B, it.R );
		}
	}

	struct Face
	{
		public Vector3 C0;
		public Vector3 C1;
		public Vector3 C2;
		public Vector3 C3;
		public Color Col;
		public float Dist;
	}

	static readonly Face[] FaceBuffer = new Face[6];

	void DrawBoxFaces( Item box, Vector3 camPos )
	{
		Vector3 h = box.Size * 0.5f;
		Rotation rot = box.Rot;
		Vector3 center = box.A;
		Color color = box.Col;

		Vector3[] normals = new Vector3[] { Vector3.Forward, Vector3.Backward, Vector3.Left, Vector3.Right, Vector3.Up, Vector3.Down };
		Vector3[] tangents = new Vector3[] { Vector3.Left, Vector3.Right, Vector3.Backward, Vector3.Forward, Vector3.Forward, Vector3.Forward };
		float[] extents = new float[] { h.x, h.x, h.y, h.y, h.z, h.z };

		for ( int f = 0; f < 6; f++ )
		{
			Vector3 n = normals[f];
			Vector3 u = tangents[f];
			Vector3 v = Vector3.Cross( n, u );

			float hu = MathF.Abs( Vector3.Dot( u, h ) );
			float hv = MathF.Abs( Vector3.Dot( v, h ) );

			Vector3 fc = center + rot * ( n * extents[f] );
			Vector3 wu = rot * u * hu;
			Vector3 wv = rot * v * hv;

			float bright = 0.5f + 0.5f * MathF.Max( 0f, Vector3.Dot( rot * n, LightDir ) );

			FaceBuffer[f] = new Face
			{
				C0 = fc - wu - wv,
				C1 = fc + wu - wv,
				C2 = fc + wu + wv,
				C3 = fc - wu + wv,
				Col = new Color( color.r * bright, color.g * bright, color.b * bright, color.a ),
				Dist = ( fc - camPos ).LengthSquared
			};
		}

		Array.Sort( FaceBuffer, ( x, y ) => y.Dist.CompareTo( x.Dist ) );

		for ( int f = 0; f < 6; f++ )
		{
			var face = FaceBuffer[f];
			Gizmo.Draw.Color = face.Col;
			Gizmo.Draw.SolidTriangle( new Triangle( face.C0, face.C1, face.C2 ) );
			Gizmo.Draw.SolidTriangle( new Triangle( face.C0, face.C2, face.C3 ) );
			Gizmo.Draw.SolidTriangle( new Triangle( face.C2, face.C1, face.C0 ) );
			Gizmo.Draw.SolidTriangle( new Triangle( face.C3, face.C2, face.C0 ) );
		}
	}
}