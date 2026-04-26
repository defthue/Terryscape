using Sandbox;

public sealed class PlayerSetup : Component
{
	[Property] public CameraComponent PlayerCamera { get; set; }
	[Property] public Dresser Dresser { get; set; }

	bool _clothingApplied = false;
	float _waitTimer = 0f;
	const float MaxWaitSeconds = 5f;

	protected override void OnStart()
	{
		// Disable the camera on remote players — each client only renders their own view.
		if ( !PlayerHelper.IsLocalPlayer( GameObject ) )
		{
			if ( PlayerCamera != null )
				PlayerCamera.Enabled = false;
		}
	}

	protected override void OnUpdate()
	{
		// Apply clothing once, as soon as the network owner is set on this GameObject.
		// This runs in the normal update tick (which keeps ticking even during loading)
		// instead of an async loop that might deadlock during scene transitions.
		if ( _clothingApplied )
			return;

		if ( Dresser == null )
		{
			_clothingApplied = true; // give up cleanly, no warning spam
			return;
		}

		_waitTimer += Time.Delta;

		if ( Network.Owner != null )
		{
			Dresser.Apply();
			_clothingApplied = true;
			return;
		}

		// Failsafe: if owner never gets set, apply anyway after the timeout so we don't loop forever.
		if ( _waitTimer >= MaxWaitSeconds )
		{
			Log.Warning( "[PlayerSetup] Timed out waiting for network owner; applying Dresser anyway." );
			Dresser.Apply();
			_clothingApplied = true;
		}
	}
}