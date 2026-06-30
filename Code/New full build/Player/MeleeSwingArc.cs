using Sandbox;
using System;
using System.Collections.Generic;

public sealed class MeleeSwingArc : Component
{
	public float Lifetime { get; set; } = 0.3f;
	public float SweepDuration { get; set; } = 0.1f;
	public float FadeInTime { get; set; } = 0.04f;
	public float FadeOutTime { get; set; } = 0.14f;
	public float Radius { get; set; } = 50f;
	public float ArcSpanDegrees { get; set; } = 150f;
	public float RollDegrees { get; set; } = -15f;
	public int Segments { get; set; } = 16;
	public float MidThickness { get; set; } = 10f;
	public float EndThickness { get; set; } = 2f;
	public float PeakAlpha { get; set; } = 1f;
	public Color SlashColor { get; set; } = new Color( 0.95f, 0.97f, 1f );

	Vector3 _forwardFlat;
	float _age;

	class Seg
	{
		public ModelRenderer Renderer;
		public GameObject Go;
		public float BaseThickness;
		public float BaseLength;
		public float Delay;
	}

	List<Seg> _segs = new();

	public static MeleeSwingArc Spawn( Scene scene, Vector3 origin, Vector3 forward )
	{
		if ( scene == null )
			return null;

		var fwd = forward.WithZ( 0f );
		if ( fwd.LengthSquared < 0.0001f )
			fwd = Vector3.Forward;
		fwd = fwd.Normal;

		var go = scene.CreateObject();
		go.Name = "MeleeSwingArc";
		go.WorldPosition = origin;

		var arc = go.Components.Create<MeleeSwingArc>();
		arc._forwardFlat = fwd;
		return arc;
	}

	protected override void OnStart()
	{
		BuildArc();
	}

	void BuildArc()
	{
		Vector3 fwd = _forwardFlat;
		Vector3 right = Vector3.Cross( Vector3.Up, fwd ).Normal;
		Vector3 up = Vector3.Up;
		float roll = RollDegrees * ( MathF.PI / 180f );
		Vector3 wing = right * MathF.Cos( roll ) + up * MathF.Sin( roll );
		float half = ArcSpanDegrees * 0.5f;
		int n = Math.Max( 2, Segments );

		var pts = new Vector3[n + 1];
		for ( int i = 0; i <= n; i++ )
		{
			float f = (float)i / n;
			float phi = ( -half + ArcSpanDegrees * f ) * ( MathF.PI / 180f );
			Vector3 dir = fwd * MathF.Cos( phi ) + wing * MathF.Sin( phi );
			pts[i] = dir * Radius;
		}

		float boxUnit = 50f;

		for ( int i = 0; i < n; i++ )
		{
			Vector3 p1 = pts[i];
			Vector3 p2 = pts[i + 1];
			Vector3 diff = p2 - p1;
			float length = diff.Length;
			if ( length < 0.01f )
				continue;

			float f = ( i + 0.5f ) / n;
			float tEnd = MathF.Abs( f - 0.5f ) * 2f;
			float thickness = EndThickness + ( MidThickness - EndThickness ) * ( 1f - tEnd );

			var segGo = new GameObject( true, $"Slash{i}" );
			segGo.SetParent( GameObject );
			segGo.LocalPosition = ( p1 + p2 ) * 0.5f;
			segGo.LocalRotation = Rotation.LookAt( diff / length );
			segGo.LocalScale = new Vector3( length / boxUnit, thickness / boxUnit, thickness / boxUnit );

			var r = segGo.Components.Create<ModelRenderer>();
			r.Model = Model.Load( "models/dev/box.vmdl" );
			r.Tint = WithAlpha( SlashColor, 0f );

			_segs.Add( new Seg
			{
				Renderer = r,
				Go = segGo,
				BaseThickness = thickness,
				BaseLength = length,
				Delay = f * SweepDuration
			} );
		}
	}

	protected override void OnUpdate()
	{
		_age += Time.Delta;

		if ( _age >= Lifetime )
		{
			GameObject.Destroy();
			return;
		}

		float globalFade = FadeOutTime > 0f ? MathF.Min( 1f, ( Lifetime - _age ) / FadeOutTime ) : 1f;

		foreach ( var s in _segs )
		{
			if ( s.Renderer == null || !s.Renderer.IsValid() )
				continue;

			float fadeIn = FadeInTime > 0f ? ( _age - s.Delay ) / FadeInTime : 1f;
			if ( fadeIn < 0f ) fadeIn = 0f;
			if ( fadeIn > 1f ) fadeIn = 1f;

			float vis = fadeIn * globalFade;
			float alpha = vis * PeakAlpha;
			s.Renderer.Tint = WithAlpha( SlashColor, alpha );

			float thick = ( s.BaseThickness / 50f ) * vis;
			s.Go.LocalScale = new Vector3( s.BaseLength / 50f, thick, thick );
		}
	}

	static Color WithAlpha( Color c, float a )
	{
		return new Color( c.r, c.g, c.b, a );
	}
}
