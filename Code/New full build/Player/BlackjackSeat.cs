using Sandbox;

public sealed class BlackjackSeat : Component
{
	[Property] public int SeatIndex { get; set; } = 0;
	[Property] public GameObject HandAnchor { get; set; }
	[Property] public BlackjackTable Table { get; set; }
	[Property] public float ProximityThreshold { get; set; } = 120f;

	[Sync] public GameObject OccupantPlayer { get; set; }

	public static BlackjackSeat LocalSeat { get; private set; }

	protected override void OnEnabled()
	{
		LocalSeat = null;
	}

	bool _wasAtSeat;

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		var localPlayer = PlayerHelper.GetLocalPlayer();
		if ( localPlayer == null )
			return;

		bool isOurOccupant = OccupantPlayer == localPlayer;

		if ( isOurOccupant )
			LocalSeat = this;

		float distance = Vector3.DistanceBetween( WorldPosition, localPlayer.WorldPosition );
		bool atSeatNow = distance < ProximityThreshold;

		if ( atSeatNow && !_wasAtSeat )
		{
			if ( !isOurOccupant && !OccupantPlayer.IsValid() && LocalSeat == null )
			{
				if ( Table != null && Table.TryClaimSeat( this, localPlayer ) )
				{
					LocalSeat = this;
					Mouse.Visibility = MouseVisibility.Visible;
				}
			}
		}
		else if ( !atSeatNow && _wasAtSeat )
		{
			if ( isOurOccupant )
			{
				if ( Table != null )
					Table.ReleaseSeat( this );
				if ( LocalSeat == this )
					LocalSeat = null;
				Mouse.Visibility = MouseVisibility.Hidden;
			}
		}

		_wasAtSeat = atSeatNow;
	}
}