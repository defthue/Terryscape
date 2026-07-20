using Sandbox;

public sealed class SpawnFacing : Component
{
	bool _applied;

	protected override void OnUpdate()
	{
		if ( _applied )
			return;

		if ( IsProxy )
		{
			_applied = true;
			Enabled = false;
			return;
		}

		var pc = Components.Get<PlayerController>();
		if ( pc == null )
			return;

		float yaw = GameManager.Instance != null ? GameManager.Instance.SpawnYawDegrees : WorldRotation.Yaw();
		float pitch = GameManager.Instance != null ? GameManager.Instance.SpawnPitchDegrees : 0f;

		pc.EyeAngles = new Angles( pitch, yaw, 0f );
		WorldRotation = Rotation.FromYaw( yaw );

		_applied = true;
		Enabled = false;
	}
}
