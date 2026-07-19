using Sandbox;
using System;
using System.Collections.Generic;

public sealed class PondWater : Component, Component.ExecuteInEditor
{
	public enum DetailLevel
	{
		Low,
		Medium,
		High,
		Ultra
	}

	Material _waterMaterial;
	Vector2 _size = new Vector2( 1024f, 1024f );
	DetailLevel _meshDetail = DetailLevel.Medium;
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
			Rebuild();
		}
	}

	[Property, Group( "Shape" )]
	public Vector2 Size
	{
		get => _size;
		set
		{
			if ( _size == value )
				return;
			_size = value;
			Rebuild();
		}
	}

	[Property, Group( "Shape" )]
	public DetailLevel MeshDetail
	{
		get => _meshDetail;
		set
		{
			if ( _meshDetail == value )
				return;
			_meshDetail = value;
			Rebuild();
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
			Rebuild();
		}
	}

	[Property, Group( "Colors" )] public Color ShallowColor { get; set; } = new Color( 0.25f, 0.85f, 0.95f );
	[Property, Group( "Colors" )] public Color DeepColor { get; set; } = new Color( 0.10f, 0.45f, 0.75f );
	[Property, Group( "Colors" )] public Color FoamColor { get; set; } = new Color( 0.97f, 0.99f, 1.00f );
	[Property, Group( "Colors" ), Range( 0f, 1f )] public float ShallowOpacity { get; set; } = 0.60f;
	[Property, Group( "Colors" ), Range( 0f, 1f )] public float DeepOpacity { get; set; } = 0.90f;

	[Property, Group( "Depth" ), Range( 10f, 500f )] public float DepthFade { get; set; } = 140f;

	[Property, Group( "Waves" ), Range( 0f, 30f )] public float WaveHeight { get; set; } = 6f;
	[Property, Group( "Waves" ), Range( 50f, 1000f )] public float WaveLength { get; set; } = 220f;
	[Property, Group( "Waves" ), Range( 0f, 4f )] public float WaveSpeed { get; set; } = 1f;
	[Property, Group( "Waves" ), Range( 0f, 360f )] public float WaveDirection { get; set; } = 15f;
	[Property, Group( "Waves" ), Range( 0f, 1f )] public float WaveIrregularity { get; set; } = 0.5f;
	[Property, Group( "Waves" ), Range( 0f, 1f )] public float CrestHighlight { get; set; } = 0.1f;

	[Property, Group( "Shore Foam" ), Range( 0f, 100f )] public float FoamSize { get; set; } = 26f;
	[Property, Group( "Shore Foam" ), Range( 0f, 1f )] public float FoamWobble { get; set; } = 0.5f;
	[Property, Group( "Shore Foam" ), Range( 0f, 3f )] public float FoamSpeed { get; set; } = 1f;

	[Property, Group( "Surface" ), Range( 0f, 1f )] public float SparkleStrength { get; set; } = 0.12f;
	[Property, Group( "Surface" ), Range( 20f, 400f )] public float SparkleSize { get; set; } = 125f;
	[Property, Group( "Surface" ), Range( 0f, 3f )] public float SparkleSpeed { get; set; } = 1f;
	[Property, Group( "Surface" ), Range( 0f, 1f )] public float SparkleDensity { get; set; } = 1f;
	[Property, Group( "Surface" ), Range( 0f, 1f )] public float SparkleParallax { get; set; } = 0.35f;

	ModelRenderer _renderer;
	BoxCollider _collider;

	protected override void OnEnabled()
	{
		Rebuild();
	}

	protected override void OnUpdate()
	{
		ApplyAttributes();
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

	int GridResolution()
	{
		switch ( _meshDetail )
		{
			case DetailLevel.Low: return 16;
			case DetailLevel.High: return 64;
			case DetailLevel.Ultra: return 96;
			default: return 32;
		}
	}

	void BuildMesh()
	{
		if ( _waterMaterial == null )
		{
			if ( _renderer != null && _renderer.IsValid() )
				_renderer.Model = null;
			return;
		}

		int res = GridResolution();

		var verts = new List<Vertex>( ( res + 1 ) * ( res + 1 ) );
		var indices = new List<int>( res * res * 6 );

		for ( int y = 0; y <= res; y++ )
		{
			for ( int x = 0; x <= res; x++ )
			{
				float u = (float)x / res;
				float w = (float)y / res;
				var pos = new Vector3( ( u - 0.5f ) * _size.x, ( w - 0.5f ) * _size.y, 0f );
				verts.Add( new Vertex( pos, Vector3.Up, Vector3.Forward, new Vector4( u, w, 0f, 0f ) ) );
			}
		}

		for ( int y = 0; y < res; y++ )
		{
			for ( int x = 0; x < res; x++ )
			{
				int i0 = y * ( res + 1 ) + x;
				int i1 = i0 + 1;
				int i2 = i0 + res + 1;
				int i3 = i2 + 1;

				indices.Add( i0 );
				indices.Add( i1 );
				indices.Add( i2 );
				indices.Add( i1 );
				indices.Add( i3 );
				indices.Add( i2 );
			}
		}

		float zExtent = MathF.Max( 64f, WaveHeight * 2f );
		var bounds = new BBox(
			new Vector3( -_size.x * 0.5f, -_size.y * 0.5f, -zExtent ),
			new Vector3( _size.x * 0.5f, _size.y * 0.5f, zExtent ) );

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
		if ( _collider == null || !_collider.IsValid() )
			_collider = Components.Get<BoxCollider>();
		if ( _collider == null || !_collider.IsValid() )
			_collider = Components.Create<BoxCollider>();

		_collider.IsTrigger = true;
		_collider.Center = new Vector3( 0f, 0f, -_swimDepth * 0.5f );
		_collider.Scale = new Vector3( _size.x, _size.y, _swimDepth );
	}

	void ApplyAttributes()
	{
		if ( _renderer == null || !_renderer.IsValid() )
			_renderer = Components.Get<ModelRenderer>();

		if ( _renderer == null )
			return;

		var attributes = _renderer.Attributes;

		attributes.Set( "SurfaceSize", _size );
		attributes.Set( "ShallowColor", new Vector3( ShallowColor.r, ShallowColor.g, ShallowColor.b ) );
		attributes.Set( "DeepColor", new Vector3( DeepColor.r, DeepColor.g, DeepColor.b ) );
		attributes.Set( "FoamColor", new Vector3( FoamColor.r, FoamColor.g, FoamColor.b ) );
		attributes.Set( "ShallowOpacity", ShallowOpacity );
		attributes.Set( "DeepOpacity", DeepOpacity );
		attributes.Set( "DepthFade", DepthFade );
		attributes.Set( "WaveHeight", WaveHeight );
		attributes.Set( "WaveLength", WaveLength );
		attributes.Set( "WaveSpeed", WaveSpeed );
		attributes.Set( "WaveDirectionDegrees", WaveDirection );
		attributes.Set( "WaveIrregularity", WaveIrregularity );
		attributes.Set( "CrestHighlight", CrestHighlight );
		attributes.Set( "FoamSize", FoamSize );
		attributes.Set( "FoamWobble", FoamWobble );
		attributes.Set( "FoamSpeed", FoamSpeed );
		attributes.Set( "SparkleStrength", SparkleStrength );
		attributes.Set( "SparkleSize", SparkleSize );
		attributes.Set( "SparkleSpeed", SparkleSpeed );
		attributes.Set( "SparkleDensity", SparkleDensity );
		attributes.Set( "SparkleParallax", SparkleParallax );
	}
}
