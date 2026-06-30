using Sandbox;
using System;

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

		var ringColor = RingColor;
		ringColor.a = alpha;

		float riseT = MathF.Min( _timer / RiseDuration, 1f );
		float eased = 1f - ( 1f - riseT ) * ( 1f - riseT );

		float currentHeight = ( SegmentHeight * 0.5f ) * eased;

		SpellGizmo.SoftRing( WorldPosition + Vector3.Up * currentHeight, Radius, SegmentThickness, ringColor, Segments );
	}
}
