using Sandbox;
using System;

public sealed class SpellTrailPuff : Component
{
	public Sprite Sprite { get; set; }
	public Vector3 Velocity { get; set; }
	public float Gravity { get; set; }
	public float Drag { get; set; }
	public Color StartColor { get; set; } = Color.White;
	public Color MidColor { get; set; } = Color.White;
	public Color EndColor { get; set; } = Color.Black;
	public float StartSize { get; set; } = 20f;
	public float PeakSize { get; set; } = 50f;
	public float EndSize { get; set; } = 8f;
	public float PeakAt { get; set; } = 0.25f;
	public float Lifetime { get; set; } = 0.4f;
	public bool Additive { get; set; } = true;
	public bool Lit { get; set; } = false;

	SpriteRenderer _renderer;
	Vector3 _vel;
	float _age;

	public static SpellTrailPuff Spawn( Scene scene, Vector3 pos, Sprite sprite, Vector3 velocity, float gravity, float drag,
		Color startColor, Color midColor, Color endColor,
		float startSize, float peakSize, float endSize, float peakAt, float lifetime,
		bool additive = true, bool lit = false )
	{
		if ( scene == null )
			return null;

		var go = scene.CreateObject();
		go.Name = "SpellParticle";
		go.WorldPosition = pos;

		var p = go.Components.Create<SpellTrailPuff>();
		p.Sprite = sprite;
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
		p.Additive = additive;
		p.Lit = lit;
		p._vel = velocity;

		var sr = go.Components.Create<SpriteRenderer>();
		if ( sprite != null )
			sr.Sprite = sprite;
		sr.Color = startColor;
		sr.Size = new Vector2( startSize, startSize );
		sr.Additive = additive;
		p._renderer = sr;

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

		Color col;
		if ( t < 0.5f )
			col = Color.Lerp( StartColor, MidColor, t / 0.5f );
		else
			col = Color.Lerp( MidColor, EndColor, ( t - 0.5f ) / 0.5f );
		_renderer.Color = col;

		float size;
		if ( t < PeakAt )
			size = StartSize + ( PeakSize - StartSize ) * ( t / PeakAt );
		else
			size = PeakSize + ( EndSize - PeakSize ) * ( ( t - PeakAt ) / ( 1f - PeakAt ) );
		_renderer.Size = new Vector2( size, size );
	}
}
