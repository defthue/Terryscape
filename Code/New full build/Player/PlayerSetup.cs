using Sandbox;

public sealed class PlayerSetup : Component
{
	[Property] public CameraComponent PlayerCamera { get; set; }

	protected override void OnStart()
	{
		if ( !PlayerHelper.IsLocalPlayer( GameObject ) )
		{
			if ( PlayerCamera != null )
				PlayerCamera.Enabled = false;
		}
	}
}