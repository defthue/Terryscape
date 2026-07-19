using Sandbox;
using System;
using System.Collections.Generic;

public sealed class WaterfallSplash : Component, Component.ExecuteInEditor
{
	Material _foamMaterial;
	float _length = 400f;
	float _width = 220f;
	float _height = 90f;

	[Property, Group( "Shape" )]
	public Material FoamMaterial
	{
		get => _foamMaterial;
		set
		{
			if ( _foamMaterial == value )
				return;
			_foamMaterial = value;
			MarkDirty();
		}
	}

	[Property, Group( "Shape" ), Range( 100f, 2000f )]
	public float Length
	{
		get => _length;
		set
		{
			float clamped = Math.Clamp( value, 100f, 2000f );
			if ( _length == clamped )
				return;
			_length = clamped;
			MarkDirty();
		}
	}

	[Property, Group( "Shape" ), Range( 50f, 1000f )]
	public float Width
	{
		get => _width;
		set
		{
			float clamped = Math.Clamp( value, 50f, 1000f );
			if ( _width == clamped )
				return;
			_width = clamped;
			MarkDirty();
		}
	}

	[Property, Group( "Shape" ), Range( 20f, 300f )]
	public float Height
	{
		get => _height;
		set
		{
			float clamped = Math.Clamp( value, 20f, 300f );
			if ( _height == clamped )
				return;
			_height = clamped;
			MarkDirty();
		}
	}

	[Property, Group( "Foam" ), Range( 0f, 1f )] public float Rumble { get; set; } = 1f;
	[Property, Group( "Foam" ), Range( 0.2f, 3f )] public float RumbleSpeed { get; set; } = 2f;
	[Property, Group( "Foam" ), Range( 0f, 1f )] public float CreviceShading { get; set; } = 1f;
	[Property, Group( "Foam" ), Range( 0f, 1f )] public float MistyTop { get; set; } = 0.45f;
	[Property, Group( "Foam" )] public Color FoamColor { get; set; } = new Color( 0.99f, 1.00f, 1.00f );
	[Property, Group( "Foam" )] public Color ShadowColor { get; set; } = new Color( 0.72f, 0.82f, 0.92f );


	ModelRenderer _renderer;
	bool _dirty;

	protected override void OnEnabled()
	{
		MarkDirty();
	}

	protected override void OnUpdate()
	{
		if ( _dirty )
		{
			_dirty = false;
			RebuildMound();
		}

		ApplyAttributes();
	}

	void MarkDirty()
	{
		_dirty = true;
	}

	[Button]
	public void Rebuild()
	{
		MarkDirty();
	}

	void EnsureRenderer()
	{
		if ( _renderer == null || !_renderer.IsValid() )
			_renderer = Components.Get<ModelRenderer>();
		if ( _renderer == null || !_renderer.IsValid() )
			_renderer = Components.Create<ModelRenderer>();
	}

	void RebuildMound()
	{
		if ( GameObject == null || !GameObject.IsValid() || Scene == null )
			return;

		EnsureRenderer();

		if ( _foamMaterial == null )
		{
			_renderer.Model = null;
			return;
		}

		float halfAlong = _length * 0.5f;
		float halfOut = _width * 0.5f;
		float segHalf = MathF.Max( halfAlong - halfOut, 0f );

		int nAlong = Math.Clamp( (int)( _length / 18f ), 12, 96 );
		int nAcross = Math.Clamp( (int)( _width / 14f ), 8, 40 );

		var verts = new List<Vertex>( ( nAlong + 1 ) * ( nAcross + 1 ) );
		var indices = new List<int>( nAlong * nAcross * 6 );

		for ( int iy = 0; iy <= nAlong; iy++ )
		{
			float y = ( (float)iy / nAlong - 0.5f ) * 2f * halfAlong;

			for ( int ix = 0; ix <= nAcross; ix++ )
			{
				float x = ( (float)ix / nAcross - 0.5f ) * 2f * halfOut;

				float segY = Math.Clamp( y, -segHalf, segHalf );
				float capLen = MathF.Max( halfAlong - segHalf, 1f );
				float dx = x;
				float dy = y - segY;

				float nx = dx / halfOut;
				float ny = dy / capLen;
				float norm = MathF.Sqrt( nx * nx + ny * ny );

				if ( norm > 1f )
				{
					dx /= norm;
					dy /= norm;
					x = dx;
					y = segY + dy;
					nx = dx / halfOut;
					ny = dy / capLen;
					norm = 1f;
				}

				float heightNorm = MathF.Sqrt( MathF.Max( 1f - norm * norm, 0f ) );
				float z = _height * heightNorm;

				float gx = nx / halfOut;
				float gy = ny / capLen;
				float gLen = MathF.Sqrt( gx * gx + gy * gy );
				Vector3 radial = gLen > 0.00001f
					? new Vector3( gx / gLen, gy / gLen, 0f )
					: new Vector3( 0f, 0f, 0f );

				Vector3 normal = ( radial * norm + new Vector3( 0f, 0f, MathF.Max( heightNorm, 0.05f ) ) ).Normal;

				var vert = new Vertex( new Vector3( x, y, z ), normal, Vector3.Forward, new Vector4( x, y, 0f, 0f ) );
				vert.TexCoord1 = new Vector4( heightNorm, norm, 0f, 0f );
				verts.Add( vert );
			}
		}

		for ( int iy = 0; iy < nAlong; iy++ )
		{
			for ( int ix = 0; ix < nAcross; ix++ )
			{
				int i0 = iy * ( nAcross + 1 ) + ix;
				int i1 = i0 + 1;
				int i2 = i0 + nAcross + 1;
				int i3 = i2 + 1;

				indices.Add( i0 );
				indices.Add( i2 );
				indices.Add( i1 );
				indices.Add( i1 );
				indices.Add( i2 );
				indices.Add( i3 );
			}
		}

		float pad = _height * 1.2f + 16f;
		var bounds = new BBox(
			new Vector3( -halfOut - 16f, -halfAlong - 16f, -8f ),
			new Vector3( halfOut + 16f, halfAlong + 16f, pad ) );

		try
		{
			var mesh = new Mesh( _foamMaterial );
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

	void ApplyAttributes()
	{
		if ( _renderer == null || !_renderer.IsValid() )
			return;

		var attributes = _renderer.Attributes;
		attributes.Set( "FoamColor", new Vector3( FoamColor.r, FoamColor.g, FoamColor.b ) );
		attributes.Set( "ShadowColor", new Vector3( ShadowColor.r, ShadowColor.g, ShadowColor.b ) );
		attributes.Set( "Rumble", Rumble );
		attributes.Set( "RumbleSpeed", RumbleSpeed );
		attributes.Set( "Crevice", CreviceShading );
		attributes.Set( "MistyTop", MistyTop );
		attributes.Set( "MoundHeight", _height );
	}
}