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
	bool _wasOccupant;
	bool _localClaimed;

	protected override void OnUpdate()
	{
		var localPlayer = PlayerHelper.GetLocalPlayer();
		if ( localPlayer == null )
			return;

		bool isOurOccupant = OccupantPlayer == localPlayer;

		if ( _localClaimed && isOurOccupant )
			LocalSeat = this;

		if ( _localClaimed && isOurOccupant && !_wasOccupant )
		{
			Mouse.Visibility = MouseVisibility.Visible;
		}
		_wasOccupant = isOurOccupant;

		float distance = Vector3.DistanceBetween( WorldPosition, localPlayer.WorldPosition );
		bool atSeatNow = distance < ProximityThreshold;

		if ( atSeatNow && !_wasAtSeat )
		{
			var localPc = localPlayer.Components.Get<PlayerController>();
			bool seatedElsewhere = localPc != null && SlimeChair.IsSeatedInAnyChair( localPc );

			if ( !_localClaimed && !OccupantPlayer.IsValid() && LocalSeat == null && !seatedElsewhere )
			{
				if ( Table != null )
				{
					_localClaimed = true;
					Table.RpcRequestClaimSeat( SeatIndex, localPlayer );
				}
			}
		}
		else if ( !atSeatNow && _wasAtSeat )
		{
			if ( _localClaimed )
			{
				_localClaimed = false;
				if ( Table != null )
					Table.RpcRequestReleaseSeat( SeatIndex );
				if ( LocalSeat == this )
					LocalSeat = null;
				Mouse.Visibility = MouseVisibility.Hidden;
			}
		}

		_wasAtSeat = atSeatNow;
	}
}