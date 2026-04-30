using Sandbox;

public sealed class PlayerMovementTuner : Component
{
	[Property] public float WalkSpeedMultiplier { get; set; } = 1.15f;
	[Property] public float RunSpeedMultiplier { get; set; } = 1.15f;

	protected override void OnStart()
	{
		var pc = Components.Get<PlayerController>();
		if ( pc == null )
			return;

		pc.WalkSpeed *= WalkSpeedMultiplier;
		pc.RunSpeed *= RunSpeedMultiplier;
	}
}
