using Sandbox;
using System;

public sealed class AimReticleVisual : Component
{
	[Property] public float Radius { get; set; } = 100f;
	[Property] public Color Color { get; set; } = Color.White;
	[Property] public float SphereSize { get; set; } = 6f;
	[Property] public int Count { get; set; } = 40;
	[Property] public float HeightOffset { get; set; } = 3f;

	protected override void OnUpdate()
	{
		float pulse = 0.75f + 0.25f * MathF.Sin( Time.Now * 5f );
		var c = Color.WithAlpha( Color.a <= 0f ? 0.8f * pulse : Color.a * pulse );
		SpellGizmo.SoftRing( WorldPosition + Vector3.Up * HeightOffset, Radius, SphereSize, c, Count );
	}
}
