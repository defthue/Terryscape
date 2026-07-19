using Sandbox;
using System;
using System.Collections.Generic;

public sealed class WaterfallWater : Component, Component.ExecuteInEditor
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
	float _width = 300f;
	float _thickness = 60f;
	DetailLevel _detail = DetailLevel.Medium;

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

	[Property, Group( "Shape" ), Range( 50f, 1500f )]
	public float Width
	{
		get => _width;
		set
		{
			float clamped = Math.Clamp( value, 50f, 1500f );
			if ( _width == clamped )
				return;
			_width = clamped;
			MarkDirty();
		}
	}

	[Property, Group( "Shape" ), Range( 10f, 200f )]
	public float Thickness
	{
		get => _thickness;
		set
		{
			float clamped = Math.Clamp( value, 10f, 200f );
			if ( _thickness == clamped )
				return;
			_thickness = clamped;
			MarkDirty();
		}
	}

	[Property, Group( "Shape" )]
	public DetailLevel Detail
	{
		get => _detail;
		set
		{
			if ( _detail == value )
				return;
			_detail = value;
			MarkDirty();
		}
	}

	[Property, Group( "Colors" )] public Color BrightColor { get; set; } = new Color( 0.35f, 0.80f, 0.97f );
	[Property, Group( "Colors" )] public Color BaseColor { get; set; } = new Color( 0.16f, 0.55f, 0.85f );
	[Property, Group( "Colors" )] public Color FoamColor { get; set; } = new Color( 0.97f, 0.99f, 1.00f );
	[Property, Group( "Colors" ), Range( 0f, 1f )] public float Opacity { get; set; } = 0.85f;

	[Property, Group( "Flow" ), Range( 0f, 200f )] public float FlowSpeed { get; set; } = 40f;
	[Property, Group( "Flow" ), Range( 0f, 1f )] public float Aggressiveness { get; set; } = 0.5f;

	[Property, Group( "Foam" ), Range( 0f, 1f )] public float Foaminess { get; set; } = 0.5f;
	[Property, Group( "Foam" ), Range( 0f, 100f )] public float LipFoam { get; set; } = 30f;
	[Property, Group( "Foam" ), Range( 0f, 1f )] public float EdgeFoam { get; set; } = 0.5f;
	[Property, Group( "Foam" ), Range( 0f, 1f )] public float BottomFroth { get; set; } = 0.5f;

	[Property, Group( "Waves" ), Range( 0f, 20f )] public float WaveHeight { get; set; } = 5f;
	[Property, Group( "Waves" ), Range( 50f, 500f )] public float WaveLength { get; set; } = 150f;

	float _rippleReach = 1.8f;

	[Property, Group( "Ripples" ), Range( 0f, 1f )] public float Ripples { get; set; } = 0.6f;
	[Property, Group( "Ripples" ), Range( 0.2f, 2f )] public float RippleSpeed { get; set; } = 1f;
	[Property, Group( "Ripples" ), Range( 20f, 200f )] public float RippleSpacing { get; set; } = 60f;
	[Property, Group( "Ripples" ), Range( 0f, 1f )] public float RippleNoise { get; set; } = 0.5f;
	[Property, Group( "Ripples" )] public float RippleThickness { get; set; } = 9f;

	[Property, Group( "Foam" )] public float ContactFoam { get; set; } = 35f;

	float _rippleWidth = 1f;

	[Property, Group( "Ripples" )]
	public float RippleWidth
	{
		get => _rippleWidth;
		set
		{
			float clamped = MathF.Max( value, 0.1f );
			if ( _rippleWidth == clamped )
				return;
			_rippleWidth = clamped;
			MarkDirty();
		}
	}

	[Property, Group( "Ripples" )]
	public float RippleReach
	{
		get => _rippleReach;
		set
		{
			float clamped = MathF.Max( value, 1.05f );
			if ( _rippleReach == clamped )
				return;
			_rippleReach = clamped;
			MarkDirty();
		}
	}

	ModelRenderer _renderer;
	GameObject _rippleGo;
	ModelRenderer _rippleRenderer;
	Spline _subscribedSpline;
	bool _dirty;
	float _fingerprint;
	bool _hasFingerprint;
	float _splineLength;

	struct FallSample
	{
		public Vector3 Position;
		public Vector3 Tangent;
		public Vector3 Side;
		public float Arc;
		public float FallTime;
		public float Width;
	}

	List<FallSample> _cachedSamples = new();

	protected override void OnEnabled()
	{
		RevalidateSubscription();
		MarkDirty();
	}

	protected override void OnDisabled()
	{
		Unsubscribe();
		if ( _rippleGo != null && _rippleGo.IsValid() )
			_rippleGo.Destroy();
		_rippleGo = null;
		_rippleRenderer = null;
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
		BuildRipples();
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
		switch ( _detail )
		{
			case DetailLevel.Low: return 48f;
			case DetailLevel.High: return 20f;
			case DetailLevel.Ultra: return 12f;
			default: return 32f;
		}
	}

	float MaxSliceAngle()
	{
		switch ( _detail )
		{
			case DetailLevel.Low: return 10f;
			case DetailLevel.High: return 4f;
			case DetailLevel.Ultra: return 2.5f;
			default: return 6f;
		}
	}

	float MaxSpeedRatio()
	{
		switch ( _detail )
		{
			case DetailLevel.Low: return 1.04f;
			case DetailLevel.High: return 1.015f;
			case DetailLevel.Ultra: return 1.01f;
			default: return 1.02f;
		}
	}

	List<FallSample> SampleCurve()
	{
		var samples = new List<FallSample>();

		if ( !Spline.IsValid() )
			return samples;

		var spline = Spline.Spline;
		if ( spline == null )
			return samples;

		float total = spline.Length;
		_splineLength = total;
		if ( total < 10f )
			return samples;

		float step = SliceSpacing();
		float maxAngle = MaxSliceAngle();
		float maxSpeedRatio = MaxSpeedRatio();
		float vFlow = MathF.Max( FlowSpeed, 5f );
		float gEff = 800f * ( 0.5f + Aggressiveness );
		float vMax = vFlow * ( 3f + Aggressiveness * 7f );

		Vector3 TangentAt( float a )
		{
			var t = spline.SampleAtDistance( MathF.Min( a, total ) ).Tangent;
			if ( t.Length < 0.0001f )
				t = Vector3.Forward;
			return t.Normal;
		}

		float fallTime = 0f;
		float cumulativeDrop = 0f;
		float baseZ = spline.SampleAtDistance( 0f ).Position.z;
		float prevZ = baseZ;
		float prevSpeed = vFlow;
		float arc = 0f;
		Vector3 sideCarry = new Vector3( 0f, 1f, 0f );

		Vector3 SideFor( Vector3 tangent )
		{
			Vector2 flat = new Vector2( tangent.x, tangent.y );
			if ( flat.Length > 0.001f )
			{
				var f = flat.Normal;
				sideCarry = new Vector3( -f.y, f.x, 0f );
			}
			return sideCarry;
		}

		var firstSample = spline.SampleAtDistance( 0f );
		var firstTangent = TangentAt( 0f );
		samples.Add( new FallSample
		{
			Position = firstSample.Position,
			Tangent = firstTangent,
			Side = SideFor( firstTangent ),
			Arc = 0f,
			FallTime = 0f,
			Width = MathF.Max( _width * firstSample.Scale.y, 1f )
		} );

		while ( arc < total )
		{
			float stepSize = step;
			float nextArc = 0f;
			Vector3 nextTangent = Vector3.Forward;

			while ( true )
			{
				nextArc = MathF.Min( arc + stepSize, total );
				nextTangent = TangentAt( nextArc );

				float dot = Math.Clamp( Vector3.Dot( samples[samples.Count - 1].Tangent, nextTangent ), -1f, 1f );
				float angle = MathF.Acos( dot ) * ( 180f / MathF.PI );

				var probe = spline.SampleAtDistance( nextArc );
				float probeDrop = cumulativeDrop + MathF.Max( prevZ - probe.Position.z, 0f );
				float probeSpeed = MathF.Min( MathF.Sqrt( vFlow * vFlow + 2f * gEff * probeDrop ), vMax );
				float speedRatio = probeSpeed / MathF.Max( prevSpeed, 1f );

				if ( ( angle <= maxAngle && speedRatio <= maxSpeedRatio ) || stepSize <= 4f )
					break;

				stepSize *= 0.5f;
			}

			var sample = spline.SampleAtDistance( nextArc );

			float drop = prevZ - sample.Position.z;
			if ( drop > 0f )
				cumulativeDrop += drop;

			float v = MathF.Min( MathF.Sqrt( vFlow * vFlow + 2f * gEff * cumulativeDrop ), vMax );
			float ds = nextArc - arc;
			if ( ds > 0.001f )
				fallTime += ds / v;

			prevZ = sample.Position.z;
			prevSpeed = v;
			arc = nextArc;

			samples.Add( new FallSample
			{
				Position = sample.Position,
				Tangent = nextTangent,
				Side = SideFor( nextTangent ),
				Arc = nextArc,
				FallTime = fallTime,
				Width = MathF.Max( _width * sample.Scale.y, 1f )
			} );
		}

		return samples;
	}

	struct RingPoint
	{
		public float Out;
		public float Y;
		public Vector2 Normal;
		public float Perimeter;
		public float Edge;
	}

	List<RingPoint> BuildRing()
	{
		var ring = new List<RingPoint>();
		float hw = _width * 0.5f;
		float ht = _thickness * 0.5f;
		float flat = MathF.Max( hw - ht, 1f );
		int faceSegs = Math.Max( (int)( _width / 30f ), 2 );
		int capSegs = 4;

		float EdgeAt( float y )
		{
			float band = MathF.Min( 60f, flat * 0.5f );
			return Math.Clamp( ( MathF.Abs( y ) - ( flat - band ) ) / band, 0f, 1f );
		}

		for ( int i = 0; i <= faceSegs; i++ )
		{
			float y = flat - 2f * flat * i / faceSegs;
			ring.Add( new RingPoint { Out = -ht, Y = y, Normal = new Vector2( -1f, 0f ), Edge = EdgeAt( y ) } );
		}
		for ( int i = 1; i < capSegs; i++ )
		{
			float a = MathF.PI * i / capSegs;
			ring.Add( new RingPoint { Out = -ht * MathF.Cos( a ), Y = -flat - ht * MathF.Sin( a ), Normal = new Vector2( -MathF.Cos( a ), -MathF.Sin( a ) ), Edge = 1f } );
		}
		for ( int i = 0; i <= faceSegs; i++ )
		{
			float y = -flat + 2f * flat * i / faceSegs;
			ring.Add( new RingPoint { Out = ht, Y = y, Normal = new Vector2( 1f, 0f ), Edge = EdgeAt( y ) } );
		}
		for ( int i = 1; i < capSegs; i++ )
		{
			float a = MathF.PI * i / capSegs;
			ring.Add( new RingPoint { Out = ht * MathF.Cos( a ), Y = flat + ht * MathF.Sin( a ), Normal = new Vector2( MathF.Cos( a ), MathF.Sin( a ) ), Edge = 1f } );
		}

		float per = 0f;
		for ( int i = 0; i < ring.Count; i++ )
		{
			if ( i > 0 )
			{
				var a = ring[i - 1];
				var b = ring[i];
				per += MathF.Sqrt( ( b.Out - a.Out ) * ( b.Out - a.Out ) + ( b.Y - a.Y ) * ( b.Y - a.Y ) );
			}
			var rp = ring[i];
			rp.Perimeter = per;
			ring[i] = rp;
		}

		return ring;
	}

	void BuildMesh()
	{
		var samples = SampleCurve();
		_cachedSamples = samples;
		if ( samples.Count < 2 || _waterMaterial == null )
		{
			if ( _renderer != null && _renderer.IsValid() )
				_renderer.Model = null;
			return;
		}

		float totalArc = samples[samples.Count - 1].Arc;
		var ring = BuildRing();
		int K = ring.Count;

		var verts = new List<Vertex>( samples.Count * K );
		var indices = new List<int>( samples.Count * K * 6 );

		Vector3 side = new Vector3( 1f, 0f, 0f );
		Vector3 min = new Vector3( float.MaxValue );
		Vector3 max = new Vector3( float.MinValue );

		for ( int r = 0; r < samples.Count; r++ )
		{
			var sample = samples[r];

			Vector2 flatTangent = new Vector2( sample.Tangent.x, sample.Tangent.y );
			if ( flatTangent.Length > 0.001f )
			{
				var f = flatTangent.Normal;
				side = new Vector3( -f.y, f.x, 0f );
			}

			Vector3 outward = Vector3.Cross( sample.Tangent, side ).Normal;
			if ( outward.Length < 0.5f )
				outward = Vector3.Up;

			float widthScale = sample.Width / _width;
			float alongNorm = totalArc > 0.001f ? sample.Arc / totalArc : 0f;

			for ( int k = 0; k < K; k++ )
			{
				var rp = ring[k];
				var pos = sample.Position + outward * rp.Out + side * ( rp.Y * widthScale );

				var vert = new Vertex( pos, outward, Vector3.Forward, new Vector4( sample.FallTime, rp.Perimeter, 0f, 0f ) );
				vert.TexCoord1 = new Vector4( alongNorm, rp.Edge, 0f, 0f );
				verts.Add( vert );

				min = new Vector3( MathF.Min( min.x, pos.x ), MathF.Min( min.y, pos.y ), MathF.Min( min.z, pos.z ) );
				max = new Vector3( MathF.Max( max.x, pos.x ), MathF.Max( max.y, pos.y ), MathF.Max( max.z, pos.z ) );
			}
		}

		for ( int r = 0; r < samples.Count - 1; r++ )
		{
			for ( int k = 0; k < K; k++ )
			{
				int k2 = ( k + 1 ) % K;
				int a0 = r * K + k;
				int a1 = r * K + k2;
				int b0 = ( r + 1 ) * K + k;
				int b1 = ( r + 1 ) * K + k2;

				indices.Add( a0 );
				indices.Add( b0 );
				indices.Add( a1 );
				indices.Add( a1 );
				indices.Add( b0 );
				indices.Add( b1 );
			}
		}

		float pad = 32f;
		var bounds = new BBox( min - new Vector3( pad ), max + new Vector3( pad ) );

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

	void BuildCollision()
	{
		ClearColliders();

		var samples = SampleCurve();
		if ( samples.Count < 2 )
			return;

		float chunkLen = 100f;
		int start = 0;

		for ( int i = 1; i < samples.Count; i++ )
		{
			bool last = i == samples.Count - 1;
			if ( samples[i].Arc - samples[start].Arc < chunkLen && !last )
				continue;

			var p0 = samples[start].Position;
			var p1 = samples[i].Position;
			var mid = ( p0 + p1 ) * 0.5f;
			var dir = p1 - p0;
			float len = dir.Length;
			start = i;
			if ( len < 1f )
				continue;
			dir /= len;

			var up = MathF.Abs( dir.z ) > 0.99f ? Vector3.Forward : Vector3.Up;

			var child = Scene.CreateObject();
			child.Name = "WaterfallCollider";
			child.Parent = GameObject;
			child.LocalPosition = mid;
			child.LocalRotation = Rotation.LookAt( dir, up );
			child.Tags.Add( "water" );
			child.Flags |= GameObjectFlags.NotSaved | GameObjectFlags.Hidden;

			var box = child.Components.Create<BoxCollider>();
			box.IsTrigger = true;
			box.Center = Vector3.Zero;
			box.Scale = new Vector3( len, _width, _thickness );
		}
	}

	void ClearColliders()
	{
		var stale = new List<GameObject>();
		foreach ( var child in GameObject.Children )
		{
			if ( child != null && child.IsValid() && child.Name == "WaterfallCollider" )
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

		float lipFrac = _splineLength > 1f ? Math.Clamp( LipFoam / _splineLength, 0f, 1f ) : 0f;

		attributes.Set( "BrightColor", new Vector3( BrightColor.r, BrightColor.g, BrightColor.b ) );
		attributes.Set( "BaseColor", new Vector3( BaseColor.r, BaseColor.g, BaseColor.b ) );
		attributes.Set( "FoamColor", new Vector3( FoamColor.r, FoamColor.g, FoamColor.b ) );
		attributes.Set( "Opacity", Opacity );
		attributes.Set( "PatternSpeed", 180f + Aggressiveness * 220f );
		attributes.Set( "Foaminess", Foaminess );
		attributes.Set( "LipFoamFrac", lipFrac );
		attributes.Set( "EdgeFoam", EdgeFoam );
		attributes.Set( "BottomFroth", BottomFroth );
		attributes.Set( "WaveHeight", WaveHeight );
		attributes.Set( "WaveLength", WaveLength );
		attributes.Set( "ContactFoam", MathF.Max( ContactFoam, 0f ) );

		if ( _rippleRenderer != null && _rippleRenderer.IsValid() )
		{
			var ra = _rippleRenderer.Attributes;
			ra.Set( "FoamColor", new Vector3( FoamColor.r, FoamColor.g, FoamColor.b ) );
			ra.Set( "Ripples", Ripples );
			ra.Set( "RippleSpeed", RippleSpeed );
			ra.Set( "RippleReach", _rippleReach );
			ra.Set( "RippleSpacing", RippleSpacing );
			ra.Set( "RippleNoise", RippleNoise );
			ra.Set( "RippleThickness", MathF.Max( RippleThickness, 1f ) );
		}
	}


	void BuildRipples()
	{
		if ( _waterMaterial == null || Ripples <= 0.01f || !Spline.IsValid() )
		{
			if ( _rippleGo != null && _rippleGo.IsValid() )
				_rippleGo.Destroy();
			_rippleGo = null;
			_rippleRenderer = null;
			return;
		}

		var spline = Spline.Spline;
		if ( spline == null || spline.Length < 10f )
			return;

		var end = spline.SampleAtDistance( spline.Length );
		float impactWidth = MathF.Max( _width * end.Scale.y, 50f ) * _rippleWidth;

		Vector3 side = new Vector3( 0f, 1f, 0f );
		Vector3 tangent = end.Tangent;
		Vector2 flat = new Vector2( tangent.x, tangent.y );
		if ( flat.Length > 0.001f )
		{
			var f = flat.Normal;
			side = new Vector3( -f.y, f.x, 0f );
		}

		float yaw = MathF.Atan2( side.y, side.x ) * ( 180f / MathF.PI ) - 90f;

		if ( _rippleGo == null || !_rippleGo.IsValid() )
		{
			_rippleGo = Scene.CreateObject();
			_rippleGo.Name = "WaterfallRipples";
			_rippleGo.Parent = GameObject;
			_rippleGo.Flags |= GameObjectFlags.NotSaved | GameObjectFlags.Hidden;
			_rippleRenderer = _rippleGo.Components.Create<ModelRenderer>();
		}

		_rippleGo.LocalPosition = end.Position + new Vector3( 0f, 0f, 1f );
		_rippleGo.LocalRotation = Rotation.FromYaw( yaw );

		float halfOut = impactWidth * 0.35f;
		float segHalf = MathF.Max( impactWidth * 0.5f - halfOut, 0f );
		float reachOut = halfOut * _rippleReach + 24f;
		float reachAlong = segHalf + reachOut;

		var verts = new List<Vertex>( 4 );
		var indices = new List<int> { 0, 2, 1, 1, 2, 3 };

		for ( int iy = 0; iy <= 1; iy++ )
		{
			for ( int ix = 0; ix <= 1; ix++ )
			{
				float x = ( ix - 0.5f ) * 2f * reachOut;
				float y = ( iy - 0.5f ) * 2f * reachAlong;
				var vert = new Vertex( new Vector3( x, y, 0f ), Vector3.Up, Vector3.Forward, new Vector4( x, y, 0f, 0f ) );
				vert.TexCoord1 = new Vector4( -( segHalf + 1f ), halfOut, 0f, 0f );
				verts.Add( vert );
			}
		}

		var bounds = new BBox(
			new Vector3( -reachOut, -reachAlong, -4f ),
			new Vector3( reachOut, reachAlong, 4f ) );

		try
		{
			var mesh = new Mesh( _waterMaterial );
			mesh.CreateVertexBuffer( verts.Count, Vertex.Layout, verts );
			mesh.CreateIndexBuffer( indices.Count, indices );
			mesh.Bounds = bounds;

			var model = Model.Builder.AddMesh( mesh ).Create();
			if ( model == null )
				return;

			_rippleRenderer.Model = model;
		}
		catch
		{
		}
	}
}