using Sandbox;
using System;

public sealed class FlameEmber : Component
{
	public Vector3 Velocity { get; set; }
	public float Gravity { get; set; }
	public float Drag { get; set; }
	public Color StartColor { get; set; } = Color.White;
	public Color MidColor { get; set; } = Color.White;
	public Color EndColor { get; set; } = Color.Black;
	public float StartSize { get; set; } = 14f;
	public float PeakSize { get; set; } = 34f;
	public float EndSize { get; set; } = 1.5f;
	public float PeakAt { get; set; } = 0.2f;
	public float Lifetime { get; set; } = 0.45f;
	public float Stretch { get; set; } = 1f;

	const float SphereUnit = 50f;

	ModelRenderer _renderer;
	Vector3 _vel;
	float _age;

	public static FlameEmber Spawn( Scene scene, Vector3 pos, Vector3 velocity, float gravity, float drag,
		Color startColor, Color midColor, Color endColor,
		float startSize, float peakSize, float endSize, float peakAt, float lifetime, float stretch = 1f )
	{
		if ( scene == null )
			return null;

		var go = scene.CreateObject();
		go.Name = "FlameEmber";
		go.WorldPosition = pos;

		if ( velocity.Length > 1f )
			go.WorldRotation = Rotation.LookAt( velocity.Normal );

		float w0 = startSize / SphereUnit;
		float l0 = ( startSize * stretch ) / SphereUnit;
		go.LocalScale = new Vector3( l0, w0, w0 );

		var p = go.Components.Create<FlameEmber>();
		p.Velocity = velocity;
		p.Gravity = gravity;
		p.Drag = drag;
		p.StartColor = startColor;
		p.MidColor = midColor;
		p.EndColor = endColor;
		p.StartSize = startSize;
		p.PeakSize = peakSize;
		p.EndSize = endSize;
		p.PeakAt = peakAt;
		p.Lifetime = lifetime;
		p.Stretch = stretch;
		p._vel = velocity;

		var r = go.Components.Create<ModelRenderer>();
		r.Model = Model.Load( "models/dev/sphere.vmdl" );
		r.Tint = startColor;
		p._renderer = r;

		return p;
	}

	protected override void OnUpdate()
	{
		_age += Time.Delta;
		float t = Lifetime > 0f ? _age / Lifetime : 1f;

		if ( t >= 1f )
		{
			GameObject.Destroy();
			return;
		}

		if ( Drag > 0f )
			_vel *= MathF.Max( 0f, 1f - Drag * Time.Delta );
		_vel += Vector3.Up * Gravity * Time.Delta;
		GameObject.WorldPosition += _vel * Time.Delta;

		if ( _renderer == null || !_renderer.IsValid() )
			return;

		float dirLen = _vel.Length;
		if ( dirLen > 1f )
			GameObject.WorldRotation = Rotation.LookAt( _vel / dirLen );

		Color col;
		if ( t < 0.5f )
			col = Color.Lerp( StartColor, MidColor, t / 0.5f );
		else
			col = Color.Lerp( MidColor, EndColor, ( t - 0.5f ) / 0.5f );
		_renderer.Tint = col;

		float size;
		if ( t < PeakAt )
			size = StartSize + ( PeakSize - StartSize ) * ( t / PeakAt );
		else
			size = PeakSize + ( EndSize - PeakSize ) * ( ( t - PeakAt ) / ( 1f - PeakAt ) );

		float w = size / SphereUnit;
		float l = ( size * Stretch ) / SphereUnit;
		GameObject.LocalScale = new Vector3( l, w, w );
	}
}
