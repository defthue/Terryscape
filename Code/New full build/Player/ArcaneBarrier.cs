using Sandbox;

public sealed class ArcaneBarrier : Component
{
	[Property] public float Duration { get; set; } = 5f;

	float _timer;

	protected override void OnUpdate()
	{
		_timer += Time.Delta;

		if ( _timer >= Duration )
			GameObject.Destroy();
	}
}
