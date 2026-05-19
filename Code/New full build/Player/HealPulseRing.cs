using Sandbox;
using System;
using System.Collections.Generic;

public sealed class HealPulseRing : Component
{
	[Property] public float Duration { get; set; } = 1.2f;
	[Property] public float Radius { get; set; } = 100f;
	[Property] public int Segments { get; set; } = 32;
	[Property] public float SegmentThickness { get; set; } = 8f;
	[Property] public float SegmentHeight { get; set; } = 40f;
	[Property] public Color RingColor { get; set; } = new Color( 0.4f, 1f, 0.6f, .55f );
	[Property] public float RiseDuration { get; set; } = 0.25f;

	float _timer;
	List<ModelRenderer> _renderers = new();

	public static GameObject Spawn( Scene scene, Vector3 position, float radius, Color color )
	{
		var go = scene.CreateObject();
		go.Name = "HealPulseRing";
		go.WorldPosition = position;

		var ring = go.Components.Create<HealPulseRing>();
		ring.Radius = radius;
		ring.RingColor = color;

		return go;
	}

	protected override void OnStart()
	{
		BuildRing();
	}

	void BuildRing()
	{
		float angleStep = 360f / Segments;
		float segmentArc = ( 2f * MathF.PI * Radius ) / Segments;

		for ( int i = 0; i < Segments; i++ )
		{
			float angleRad = ( angleStep * i ) * ( MathF.PI / 180f );
			float x = MathF.Cos( angleRad ) * Radius;
			float y = MathF.Sin( angleRad ) * Radius;

			var box = new GameObject( true, $"Segment{i}" );
			box.SetParent( GameObject );
			box.LocalPosition = new Vector3( x, y, SegmentHeight * 0.5f );
			box.LocalRotation = Rotation.FromYaw( angleStep * i + 90f );
			box.LocalScale = new Vector3( segmentArc / 50f, SegmentThickness / 50f, SegmentHeight / 50f );

			var renderer = box.Components.Create<ModelRenderer>();
			renderer.Model = Model.Load( "models/dev/box.vmdl" );

			var color = RingColor;
			color.a = 0f;
			renderer.Tint = color;

			_renderers.Add( renderer );
		}
	}

	protected override void OnUpdate()
	{
		_timer += Time.Delta;

		if ( _timer >= Duration )
		{
			GameObject.Destroy();
			return;
		}

		float alpha;
		float t = _timer / Duration;

		if ( _timer < RiseDuration )
		{
			alpha = _timer / RiseDuration;
		}
		else
		{
			float fadeT = ( _timer - RiseDuration ) / ( Duration - RiseDuration );
			alpha = 1f - fadeT;
		}

		alpha = MathF.Max( 0f, MathF.Min( 1f, alpha ) );

		var color = RingColor;
		color.a = alpha;

		foreach ( var r in _renderers )
		{
			if ( r != null && r.IsValid() )
				r.Tint = color;
		}

		float riseT = MathF.Min( _timer / RiseDuration, 1f );
		float eased = 1f - ( 1f - riseT ) * ( 1f - riseT );

		foreach ( var r in _renderers )
		{
			if ( r == null || !r.IsValid() )
				continue;

			var seg = r.GameObject;
			var pos = seg.LocalPosition;
			pos.z = ( SegmentHeight * 0.5f ) * eased;
			seg.LocalPosition = pos;

			var scl = seg.LocalScale;
			scl.z = ( SegmentHeight / 50f ) * eased;
			seg.LocalScale = scl;
		}
	}
}
