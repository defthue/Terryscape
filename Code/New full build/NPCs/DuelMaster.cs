using Sandbox;

public sealed class DuelMaster : Component
{
	public static DuelMaster ActiveStation { get; private set; }

	[Property] public float InteractDistance { get; set; } = 120f;

	public static bool IsOpen => ActiveStation != null;

	protected override void OnUpdate()
	{
		if ( ActiveStation == this )
		{
			var owner = PlayerHelper.GetLocalPlayer();
			if ( owner == null )
			{
				Close();
				return;
			}

			var ownerState = owner.Components.Get<PvpState>();
			if ( ownerState == null || !ownerState.InArena )
			{
				Close();
				return;
			}

			if ( Input.Pressed( "use" ) )
				Close();

			return;
		}

		var local = PlayerHelper.GetLocalPlayer();
		if ( local == null )
			return;

		float dist = Vector3.DistanceBetween( WorldPosition, local.WorldPosition );
		if ( dist > InteractDistance )
			return;

		if ( !Input.Pressed( "use" ) )
			return;

		var localState = local.Components.Get<PvpState>();
		if ( localState == null || !localState.InArena )
		{
			GameLog.Add( "You must be in the arena to challenge someone.", "#c86464" );
			return;
		}

		Open();
	}

	void Open()
	{
		ActiveStation = this;
	}

	public static void Close()
	{
		ActiveStation = null;
	}
}
