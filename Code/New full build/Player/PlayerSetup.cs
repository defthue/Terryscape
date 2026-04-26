using Sandbox;

public sealed class PlayerSetup : Component
{
	[Property] public CameraComponent PlayerCamera { get; set; }
	[Property] public Dresser Dresser { get; set; }

	protected override void OnStart()
	{
		if ( !PlayerHelper.IsLocalPlayer( GameObject ) )
		{
			if ( PlayerCamera != null )
				PlayerCamera.Enabled = false;
		}

		_ = ApplyClothingWhenReady();
	}

	async System.Threading.Tasks.Task ApplyClothingWhenReady()
	{
		if ( Dresser == null )
		{
			Log.Warning( "[PlayerSetup] No Dresser assigned — clothing won't be applied." );
			return;
		}

		// Wait until network ownership is established. The Dresser needs a real owner
		// connection to know whose Steam clothing to load.
		var startTime = RealTime.Now;
		while ( Network.Owner == null )
		{
			if ( RealTime.Now - startTime > 5f )
			{
				Log.Warning( "[PlayerSetup] Timed out waiting for network owner; applying clothing anyway." );
				break;
			}
			await GameTask.DelayRealtime( 16 );
		}

		Dresser.Apply();
	}
}