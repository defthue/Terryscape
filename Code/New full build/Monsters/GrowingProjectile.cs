using Sandbox;
using System;

public sealed class GrowingProjectile : Component
{
	[Property] public float StartScale { get; set; } = 0.3f;
	[Property] public float EndScale { get; set; } = 1.5f;
	[Property] public float GrowDuration { get; set; } = 1.0f;

	float _timer = 0f;

	protected override void OnStart()
	{
		GameObject.WorldScale = Vector3.One * StartScale;
	}

	protected override void OnUpdate()
	{
		_timer += Time.Delta;
		float t = MathF.Min( _timer / GrowDuration, 1f );
		float scale = StartScale + ( EndScale - StartScale ) * t;
		GameObject.WorldScale = Vector3.One * scale;
	}
}