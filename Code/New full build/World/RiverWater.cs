using Sandbox;
using System;
using System.Collections.Generic;

public sealed class RiverWater : Component, Component.ExecuteInEditor
{
	public enum DetailLevel
	{
		Low,
		Medium,
		High,
		Ultra
	}

	[RequireComponent] public SplineComponent Spline { get; set; }

	Material _waterMaterial;
	float _width = 200f;
	DetailLevel _curveDetail = DetailLevel.Medium;
	float _swimDepth = 200f;

	[Property, Group( "Shape" )]
	public Material WaterMaterial
	{
		get => _waterMaterial;
		set
		{
			if ( _waterMaterial == value )
				return;
			_waterMaterial = value;
			MarkDirty();
		}
	}

	[Property, Group( "Shape" ), Range( 25f, 1000f )]
	public float Width
	{
		get => _width;
		set
		{
			float clamped = Math.Clamp( value, 25f, 1000f );
			if ( _width == clamped )
				return;
			_width = clamped;
			MarkDirty();
		}
	}

	[Property, Group( "Shape" )]
	public DetailLevel CurveDetail
	{
		get => _curveDetail;
		set
		{
			if ( _curveDetail == value )
				return;
			_curveDetail = value;
			MarkDirty();
		}
	}

	[Property, Group( "Shape" ), Range( 25f, 500f )]
	public float SwimDepth
	{
		get => _swimDepth;
		set
		{
			float clamped = Math.Clamp( value, 25f, 500f );
			if ( _swimDepth == clamped )
				return;
			_swimDepth = clamped;
			MarkDirty();
		}
	}

	[Property, Group( "Colors" )] public Color ShallowColor { get; set; } = new Color( 0.25f, 0.85f, 0.95f );
	[Property, Group( "Colors" )] public Color DeepColor { get; set; } = new Color( 0.10f, 0.45f, 0.75f );
	[Property, Group( "Colors" )] public Color FoamColor { get; set; } = new Color( 0.97f, 0.99f, 1.00f );
	[Property, Group( "Colors" ), Range( 0f, 1f )] public float ShallowOpacity { get; set; } = 0.60f;
	[Property, Group( "Colors" ), Range( 0f, 1f )] public float DeepOpacity { get; set; } = 0.90f;

	[Property, Group( "Depth" ), Range( 10f, 500f )] public float DepthFade { get; set; } = 140f;

	[Property, Group( "Flow" ), Range( 0f, 200f )] public float FlowSpeed { get; set; } = 40f;

	[Property, Group( "Waves" ), Range( 0f, 30f )] public float WaveHeight { get; set; } = 6f;
	[Property, Group( "Waves" ), Range( 50f, 1000f )] public float WaveLength { get; set; } = 220f;
	[Property, Group( "Waves" ), Range( 0f, 4f )] public float WaveSpeed { get; set; } = 1f;
	[Property, Group( "Waves" ), Range( 0f, 1f )] public float WaveIrregularity { get; set; } = 0.5f;

	[Property, Group( "Shore Foam" ), Range( 0f, 100f )] public float FoamSize { get; set; } = 26f;
	[Property, Group( "Shore Foam" ), Range( 0f, 1f )] public float FoamWobble { get; set; } = 0.5f;
	[Property, Group( "Shore Foam" ), Range( 0f, 3f )] public float FoamSpeed { get; set; } = 1f;
	[Property, Group( "Shore Foam" ), Range( 10f, 100f )] public float FoamDetail { get; set; } = 33f;

	[Property, Group( "Surface" ), Range( 0f, 1f )] public float SparkleStrength { get; set; } = 0.12f;
	[Property, Group( "Surface" ), Range( 20f, 400f )] public float SparkleSize { get; set; } = 125f;
	[Property, Group( "Surface" ), Range( 0f, 3f )] public float SparkleSpeed { get; set; } = 1f;
	[Property, Group( "Surface" ), Range( 0f, 1f )] public float SparkleDensity { get; set; } = 0.5f;
	[Property, Group( "Surface" ), Range( 0f, 0.3f )] public float SparkleParallax { get; set; } = 0.12f;

	[Property, Group( "Streaks" ), Range( 0f, 1f )] public float StreakStrength { get; set; } = 0.7f;
	[Property, Group( "Streaks" ), Range( 15f, 80f )] public float StreakSpacing { get; set; } = 35f;
	[Property, Group( "Streaks" ), Range( 1f, 10f )] public float StreakWidth { get; set; } = 4f;
	[Property, Group( "Streaks" ), Range( 0f, 1f )] public float StreakCoverage { get; set; } = 0.5f;
	[Property, Group( "Streaks" ), Range( 100f, 800f )] public float StreakLength { get; set; } = 300f;
	[Property, Group( "Streaks" ), Range( 0f, 40f )] public float StreakWarp { get; set; } = 20f;
	[Property, Group( "Streaks" ), Range( 0f, 1f )] public float StreakSpeedVariation { get; set; } = 0.5f;

	ModelRenderer _renderer;
	Spline _subscribedSpline;
	bool _dirty;
	float _fingerprint;
	bool _hasFingerprint;

	struct CurveSample
	{
		public Vector3 Position;
		public Vector3 Tangent;
		public float Arc;
		public float Width;
	}

	protected override void OnEnabled()
	{
		RevalidateSubscription();
		MarkDirty();
	}

	protected override void OnDisabled()
	{
		Unsubscribe();
	}

	protected override void OnUpdate()
	{
		Maintain();
		ApplyAttributes();
	}

	void MarkDirty()
	{
		_dirty = true;
	}

	void RevalidateSubscription()
	{
		var current = Spline.IsValid() ? Spline.Spline : null;
		if ( ReferenceEquals( current, _subscribedSpline ) )
			return;

		Unsubscribe();

		if ( current != null )
		{
			current.SplineChanged += MarkDirty;
			_subscribedSpline = current;
		}

		_dirty = true;
	}

	void Unsubscribe()
	{
		if ( _subscribedSpline == null )
			return;

		_subscribedSpline.SplineChanged -= MarkDirty;
		_subscribedSpline = null;
	}

	void Maintain()
	{
		RevalidateSubscription();

		try
		{
			if ( Scene != null && Scene.IsEditor )
			{
				float fp = ComputeFingerprint();
				if ( !_hasFingerprint || fp != _fingerprint )
					_dirty = true;
			}

			if ( !_dirty )
				return;

			_dirty = false;
			Rebuild();
			_fingerprint = ComputeFingerprint();
			_hasFingerprint = true;
		}
		catch
		{
			_dirty = false;
		}
	}

	float ComputeFingerprint()
	{
		if ( !Spline.IsValid() )
			return -1f;

		var spline = Spline.Spline;
		if ( spline == null || spline.PointCount < 2 )
			return -1f;

		float fp = spline.PointCount * 13.13f + spline.Length * 0.61803f;
		for ( int i = 0; i < spline.PointCount; i++ )
		{
			var p = spline.GetPoint( i );
			float k = i + 1f;
			fp += ( p.Position.x * 1.0001f + p.Position.y * 1.0093f + p.Position.z * 1.0217f ) * k;
			fp += ( p.In.x + p.In.y + p.In.z ) * 0.517f * k;
			fp += ( p.Out.x + p.Out.y + p.Out.z ) * 0.731f * k;
			fp += p.Roll * 0.291f + p.Scale.y * 3.7f;
		}

		return fp;
	}

	[Button]
	public void Rebuild()
	{
		if ( GameObject == null || !GameObject.IsValid() )
			return;
		if ( Scene == null )
			return;

		EnsureRenderer();
		BuildMesh();
		BuildCollision();
		EnsureTag();
	}

	void EnsureRenderer()
	{
		if ( _renderer == null || !_renderer.IsValid() )
			_renderer = Components.Get<ModelRenderer>();
		if ( _renderer == null || !_renderer.IsValid() )
			_renderer = Components.Create<ModelRenderer>();
	}

	void EnsureTag()
	{
		if ( !GameObject.Tags.Has( "water" ) )
			GameObject.Tags.Add( "water" );
	}

	float SliceSpacing()
	{
		switch ( _curveDetail )
		{
			case DetailLevel.Low: return 60f;
			case DetailLevel.High: return 15f;
			case DetailLevel.Ultra: return 8f;
			default: return 30f;
		}
	}

	static float SignedTurn( Vector3 a, Vector3 b )
	{
		Vector2 fa = new Vector2( a.x, a.y );
		Vector2 fb = new Vector2( b.x, b.y );
		if ( fa.Length < 0.001f || fb.Length < 0.001f )
			return 0f;

		fa = fa.Normal;
		fb = fb.Normal;
		float crossZ = fa.x * fb.y - fa.y * fb.x;
		float dot = Math.Clamp( fa.x * fb.x + fa.y * fb.y, -1f, 1f );
		return MathF.Atan2( crossZ, dot );
	}

	List<CurveSample> SampleCurve()
	{
		var result = new List<CurveSample>();

		if ( !Spline.IsValid() )
			return result;

		var spline = Spline.Spline;
		if ( spline == null || spline.PointCount < 2 )
			return result;

		float total = spline.Length;
		if ( total < 1f )
			return result;

		float step = SliceSpacing();
		int sampleCount = Math.Max( (int)MathF.Ceiling( total / step ), 1 );

		for ( int i = 0; i <= sampleCount; i++ )
		{
			float distance = MathF.Min( total * i / sampleCount, total );
			var sample = spline.SampleAtDistance( distance );

			Vector3 tangent = sample.Tangent;
			if ( tangent.Length < 0.0001f )
				tangent = Vector3.Forward;

			result.Add( new CurveSample
			{
				Position = sample.Position,
				Tangent = tangent.Normal,
				Arc = distance,
				Width = MathF.Max( _width * sample.Scale.y, 1f )
			} );
		}

		return result;
	}

	void BuildMesh()
	{
		var samples = SampleCurve();
		if ( samples.Count < 2 || _waterMaterial == null )
		{
			if ( _renderer != null && _renderer.IsValid() )
				_renderer.Model = null;
			return;
		}

		var spline = Spline.Spline;
		float total = spline.Length;
		float blockSize = 128f;

		var arcs = new List<float>();
		foreach ( var s in samples )
			arcs.Add( s.Arc );

		var slices = new List<CurveSample>();
		var coarse = new List<int>();
		var quadBreak = new List<bool>();

		int block = 0;
		int cursor = 0;
		while ( block * blockSize < total || block == 0 )
		{
			float blockStart = block * blockSize;
			float blockEnd = MathF.Min( ( block + 1 ) * blockSize, total );

			AddSlice( slices, coarse, quadBreak, blockStart, block, slices.Count > 0 );

			while ( cursor < arcs.Count && arcs[cursor] <= blockStart + 0.5f )
				cursor++;
			while ( cursor < arcs.Count && arcs[cursor] < blockEnd - 0.5f )
			{
				AddSlice( slices, coarse, quadBreak, arcs[cursor], block, false );
				cursor++;
			}

			AddSlice( slices, coarse, quadBreak, blockEnd, block, false );

			if ( blockEnd >= total )
				break;
			block++;
		}

		var verts = new List<Vertex>( slices.Count * 2 );
		var indices = new List<int>( slices.Count * 6 );

		Vector3 across = new Vector3( 1f, 0f, 0f );
		Vector3 min = slices[0].Position;
		Vector3 max = slices[0].Position;

		for ( int i = 0; i < slices.Count; i++ )
		{
			var sample = slices[i];
			float half = sample.Width * 0.5f;

			Vector2 flat = new Vector2( sample.Tangent.x, sample.Tangent.y );
			if ( flat.Length > 0.001f )
			{
				flat = flat.Normal;
				across = new Vector3( -flat.y, flat.x, 0f );
			}

			Vector3 normal = Vector3.Cross( sample.Tangent, across ).Normal;
			if ( normal.Length < 0.5f )
				normal = Vector3.Up;

			int prev = i;
			while ( prev > 0 && slices[i].Arc - slices[prev].Arc < 0.5f )
				prev--;
			int next = i;
			while ( next < slices.Count - 1 && slices[next].Arc - slices[i].Arc < 0.5f )
				next++;

			float turn = SignedTurn( slices[prev].Tangent, slices[next].Tangent );
			float arcSpan = slices[next].Arc - slices[prev].Arc;

			float negOffset = half;
			float posOffset = half;
			if ( MathF.Abs( turn ) > 0.0001f && arcSpan > 0.0001f )
			{
				float radius = arcSpan / MathF.Abs( turn );
				float maxInner = radius * 0.95f;
				if ( half > maxInner )
				{
					if ( turn > 0f )
						posOffset = maxInner;
					else
						negOffset = maxInner;
				}
			}

			Vector3 left = sample.Position - across * negOffset;
			Vector3 right = sample.Position + across * posOffset;

			float fine = sample.Arc - coarse[i] * blockSize;

			var leftVert = new Vertex( left, normal, Vector3.Forward, new Vector4( fine, -negOffset, 0f, 0f ) );
			leftVert.TexCoord1 = new Vector4( coarse[i], 0f, 0f, 0f );
			verts.Add( leftVert );
			var rightVert = new Vertex( right, normal, Vector3.Forward, new Vector4( fine, posOffset, 0f, 0f ) );
			rightVert.TexCoord1 = new Vector4( coarse[i], 0f, 0f, 0f );
			verts.Add( rightVert );

			min = new Vector3(
				MathF.Min( min.x, MathF.Min( left.x, right.x ) ),
				MathF.Min( min.y, MathF.Min( left.y, right.y ) ),
				MathF.Min( min.z, MathF.Min( left.z, right.z ) ) );
			max = new Vector3(
				MathF.Max( max.x, MathF.Max( left.x, right.x ) ),
				MathF.Max( max.y, MathF.Max( left.y, right.y ) ),
				MathF.Max( max.z, MathF.Max( left.z, right.z ) ) );
		}

		for ( int i = 0; i < slices.Count - 1; i++ )
		{
			if ( quadBreak[i + 1] )
				continue;

			int i0 = i * 2;
			int i1 = i0 + 1;
			int i2 = i0 + 2;
			int i3 = i0 + 3;

			indices.Add( i0 );
			indices.Add( i2 );
			indices.Add( i1 );
			indices.Add( i1 );
			indices.Add( i2 );
			indices.Add( i3 );
		}

		float zExtent = MathF.Max( 64f, WaveHeight * 2f );
		var bounds = new BBox(
			min - new Vector3( 16f, 16f, zExtent ),
			max + new Vector3( 16f, 16f, zExtent ) );

		try
		{
			var mesh = new Mesh( _waterMaterial );
			mesh.CreateVertexBuffer( verts.Count, Vertex.Layout, verts );
			mesh.CreateIndexBuffer( indices.Count, indices );
			mesh.Bounds = bounds;

			var model = Model.Builder.AddMesh( mesh ).Create();
			if ( model == null )
				return;

			_renderer.Model = model;
		}
		catch
		{
		}
	}

	void AddSlice( List<CurveSample> slices, List<int> coarse, List<bool> quadBreak, float arc, int block, bool isBreak )
	{
		var spline = Spline.Spline;
		var sample = spline.SampleAtDistance( MathF.Min( arc, spline.Length ) );

		Vector3 tangent = sample.Tangent;
		if ( tangent.Length < 0.0001f )
			tangent = Vector3.Forward;

		slices.Add( new CurveSample
		{
			Position = sample.Position,
			Tangent = tangent.Normal,
			Arc = arc,
			Width = MathF.Max( _width * sample.Scale.y, 1f )
		} );
		coarse.Add( block );
		quadBreak.Add( isBreak );
	}

	void BuildCollision()
	{
		ClearColliders();

		var samples = SampleCurve();
		if ( samples.Count < 2 )
			return;

		int chunk = 4;
		for ( int i = 0; i < samples.Count - 1; i += chunk )
		{
			int end = Math.Min( i + chunk, samples.Count - 1 );
			Vector3 a = samples[i].Position;
			Vector3 b = samples[end].Position;
			Vector3 mid = ( a + b ) * 0.5f;
			Vector3 dir = b - a;
			float length = dir.Length;
			if ( length < 1f )
				continue;

			dir /= length;
			Vector3 up = MathF.Abs( dir.z ) > 0.99f ? Vector3.Forward : Vector3.Up;
			float chunkWidth = samples[( i + end ) / 2].Width;

			var child = Scene.CreateObject();
			child.Name = "RiverWaterCollider";
			child.Parent = GameObject;
			child.LocalPosition = mid;
			child.LocalRotation = Rotation.LookAt( dir, up );
			child.Tags.Add( "water" );
			child.Flags |= GameObjectFlags.NotSaved | GameObjectFlags.Hidden;

			var box = child.Components.Create<BoxCollider>();
			box.IsTrigger = true;
			box.Center = new Vector3( 0f, 0f, -_swimDepth * 0.5f );
			box.Scale = new Vector3( length, chunkWidth, _swimDepth );
		}
	}

	void ClearColliders()
	{
		var stale = new List<GameObject>();
		foreach ( var child in GameObject.Children )
		{
			if ( child != null && child.IsValid() && child.Name == "RiverWaterCollider" )
				stale.Add( child );
		}
		foreach ( var child in stale )
			child.Destroy();
	}

	void ApplyAttributes()
	{
		if ( _renderer == null || !_renderer.IsValid() )
			_renderer = Components.Get<ModelRenderer>();

		if ( _renderer == null )
			return;

		var attributes = _renderer.Attributes;

		attributes.Set( "ShallowColor", new Vector3( ShallowColor.r, ShallowColor.g, ShallowColor.b ) );
		attributes.Set( "DeepColor", new Vector3( DeepColor.r, DeepColor.g, DeepColor.b ) );
		attributes.Set( "FoamColor", new Vector3( FoamColor.r, FoamColor.g, FoamColor.b ) );
		attributes.Set( "ShallowOpacity", ShallowOpacity );
		attributes.Set( "DeepOpacity", DeepOpacity );
		attributes.Set( "DepthFade", DepthFade );
		attributes.Set( "FlowSpeed", FlowSpeed );
		attributes.Set( "WaveHeight", WaveHeight );
		attributes.Set( "WaveLength", WaveLength );
		attributes.Set( "WaveSpeed", WaveSpeed );
		attributes.Set( "WaveIrregularity", WaveIrregularity );
		attributes.Set( "FoamSize", FoamSize );
		attributes.Set( "FoamWobble", FoamWobble );
		attributes.Set( "FoamSpeed", FoamSpeed );
		attributes.Set( "FoamDetail", FoamDetail );
		attributes.Set( "SparkleStrength", SparkleStrength );
		attributes.Set( "SparkleSize", SparkleSize );
		attributes.Set( "SparkleSpeed", SparkleSpeed );
		attributes.Set( "SparkleDensity", SparkleDensity );
		attributes.Set( "SparkleParallax", SparkleParallax );
		attributes.Set( "StreakStrength", StreakStrength );
		attributes.Set( "StreakSpacing", StreakSpacing );
		attributes.Set( "StreakWidth", StreakWidth );
		attributes.Set( "StreakCoverage", StreakCoverage );
		attributes.Set( "StreakLength", StreakLength );
		attributes.Set( "StreakWarp", StreakWarp );
		attributes.Set( "StreakSpeedVariation", StreakSpeedVariation );
	}
}