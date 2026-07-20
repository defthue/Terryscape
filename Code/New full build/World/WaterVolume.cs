using Sandbox;
using System;
using System.Collections.Generic;

public class WaterVolume : Component, Component.ExecuteInEditor
{
	Vector2 _size = new Vector2( 1024f, 1024f );
	float _depth = 200f;
	int _resolution = 32;
	Material _waterMaterial;

	[Property, Group( "Dimensions" )]
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

	[Property, Group( "Dimensions" )]
	public float Depth
	{
		get => _depth;
		set
		{
			if ( _depth == value )
				return;
			_depth = value;
			Rebuild();
		}
	}

	[Property, Group( "Dimensions" ), Range( 2, 128 )]
	public int Resolution
	{
		get => _resolution;
		set
		{
			int clamped = Math.Clamp( value, 2, 128 );
			if ( _resolution == clamped )
				return;
			_resolution = clamped;
			Rebuild();
		}
	}

	[Property, Group( "Dimensions" )]
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

	[Property, Group( "Colors" )] public Color ShallowColor { get; set; } = new Color( 0.25f, 0.85f, 0.95f );
	[Property, Group( "Colors" )] public Color DeepColor { get; set; } = new Color( 0.10f, 0.45f, 0.75f );
	[Property, Group( "Colors" )] public Color FoamColor { get; set; } = new Color( 0.97f, 0.99f, 1.00f );

	[Property, Group( "Depth Fade" )] public float DepthFadeDistance { get; set; } = 140f;
	[Property, Group( "Depth Fade" )] public float ShallowAlpha { get; set; } = 0.60f;
	[Property, Group( "Depth Fade" )] public float DeepAlpha { get; set; } = 0.90f;

	[Property, Group( "Foam" )] public float FoamDistance { get; set; } = 26f;
	[Property, Group( "Foam" )] public float FoamNoiseScale { get; set; } = 0.03f;
	[Property, Group( "Foam" )] public float FoamScrollSpeed { get; set; } = 18f;

	[Property, Group( "Surface" )] public float SurfaceNoiseScale { get; set; } = 0.008f;
	[Property, Group( "Surface" )] public float SurfaceScrollSpeed { get; set; } = 10f;
	[Property, Group( "Surface" )] public float SurfaceHighlight { get; set; } = 0.12f;

	[Property, Group( "Waves" )] public float WaveAmplitude { get; set; } = 6f;
	[Property, Group( "Waves" )] public float WaveLength { get; set; } = 220f;
	[Property, Group( "Waves" )] public float WaveSpeed { get; set; } = 1f;
	[Property, Group( "Waves" )] public Vector2 WaveDirection { get; set; } = new Vector2( 1f, 0.3f );
	[Property, Group( "Waves" )] public float WaveSecondOctave { get; set; } = 0.5f;
	[Property, Group( "Waves" )] public float WaveCrestTint { get; set; } = 0.10f;

	[Property, Group( "Patches" )] public float PatchScale { get; set; } = 0.01f;
	[Property, Group( "Patches" )] public float PatchDetailScale { get; set; } = 0.05f;
	[Property, Group( "Patches" )] public float PatchDetailStrength { get; set; } = 0.6f;
	[Property, Group( "Patches" )] public float PatchCoverage { get; set; } = 0.22f;
	[Property, Group( "Patches" )] public float PatchDriftSpeed { get; set; } = 0.02f;
	[Property, Group( "Patches" )] public float PatchHaloStrength { get; set; } = 0.45f;

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

	protected virtual void BuildMesh()
	{
		if ( _waterMaterial == null )
			return;

		int res = Math.Clamp( _resolution, 2, 128 );

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

		float zExtent = MathF.Max( 64f, WaveAmplitude * 2f );
		var bounds = new BBox(
			new Vector3( -_size.x * 0.5f, -_size.y * 0.5f, -zExtent ),
			new Vector3( _size.x * 0.5f, _size.y * 0.5f, zExtent ) );

		BuildModel( verts, indices, bounds );
	}

	protected void BuildModel( List<Vertex> verts, List<int> indices, BBox bounds )
	{
		if ( _waterMaterial == null )
			return;
		if ( _renderer == null || !_renderer.IsValid() )
			return;

		try
		{
			var mesh = new Mesh( _waterMaterial );
			mesh.CreateVertexBuffer( verts.Count, verts );
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

	protected virtual void BuildCollision()
	{
		if ( _collider == null || !_collider.IsValid() )
			_collider = Components.Get<BoxCollider>();
		if ( _collider == null || !_collider.IsValid() )
			_collider = Components.Create<BoxCollider>();

		_collider.IsTrigger = true;
		_collider.Center = new Vector3( 0f, 0f, -_depth * 0.5f );
		_collider.Scale = new Vector3( _size.x, _size.y, _depth );
	}

	void EnsureTag()
	{
		if ( !GameObject.Tags.Has( "water" ) )
			GameObject.Tags.Add( "water" );
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
		attributes.Set( "DepthFadeDistance", DepthFadeDistance );
		attributes.Set( "ShallowAlpha", ShallowAlpha );
		attributes.Set( "DeepAlpha", DeepAlpha );

		attributes.Set( "FoamDistance", FoamDistance );
		attributes.Set( "FoamNoiseScale", FoamNoiseScale );
		attributes.Set( "FoamScrollSpeed", FoamScrollSpeed );

		attributes.Set( "SurfaceNoiseScale", SurfaceNoiseScale );
		attributes.Set( "SurfaceScrollSpeed", SurfaceScrollSpeed );
		attributes.Set( "SurfaceHighlight", SurfaceHighlight );

		attributes.Set( "WaveAmplitude", WaveAmplitude );
		attributes.Set( "WaveLength", WaveLength );
		attributes.Set( "WaveSpeed", WaveSpeed );
		attributes.Set( "WaveDirection", WaveDirection );
		attributes.Set( "WaveSecondOctave", WaveSecondOctave );
		attributes.Set( "WaveCrestTint", WaveCrestTint );

		attributes.Set( "PatchScale", PatchScale );
		attributes.Set( "PatchDetailScale", PatchDetailScale );
		attributes.Set( "PatchDetailStrength", PatchDetailStrength );
		attributes.Set( "PatchCoverage", PatchCoverage );
		attributes.Set( "PatchDriftSpeed", PatchDriftSpeed );
		attributes.Set( "PatchHaloStrength", PatchHaloStrength );

		PushExtraAttributes( attributes );
	}

	protected virtual void PushExtraAttributes( RenderAttributes attributes )
	{
		attributes.Set( "RibbonMode", 0f );
		attributes.Set( "FlowDirection", new Vector2( 0f, -1f ) );
		attributes.Set( "FlowSpeed", 0f );
		attributes.Set( "StreakStrength", 0f );
		attributes.Set( "StreakLaneSpacing", 30f );
		attributes.Set( "StreakWidth", 4f );
		attributes.Set( "StreakLengthFrequency", 0.004f );
		attributes.Set( "StreakCoverage", 0.35f );
		attributes.Set( "StreakWobbleFrequency", 0.003f );
		attributes.Set( "StreakWobbleAmount", 8f );
		attributes.Set( "StreakCrestBias", 0.5f );
	}
}
