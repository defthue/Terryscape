using Sandbox;
using Sandbox.Movement;

[Title( "Slime Chair" ), Group( "Pets" ), Icon( "pets" )]
public sealed class SlimeChair : BaseChair
{
	[Sync] public ulong OccupantSteamId { get; set; }

	PetSlime _pet;

	PetSlime Pet
	{
		get
		{
			if ( _pet == null || !_pet.IsValid() )
				_pet = Components.Get<PetSlime>();
			return _pet;
		}
	}

	public bool HasRider => OccupantSteamId != 0ul || IsOccupied;

	public PlayerController ResolveRider()
	{
		var occ = GetOccupant();
		if ( occ.IsValid() )
			return occ;

		if ( OccupantSteamId == 0ul )
			return null;

		foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
		{
			var conn = pc.Network.Owner;
			if ( conn != null && conn.SteamId == OccupantSteamId )
				return pc;
		}
		return null;
	}

	public static bool IsSeatedInAnyChair( PlayerController player )
	{
		if ( player == null || !player.IsValid() )
			return false;
		return player.GetComponentInParent<ISitTarget>() != null;
	}

	public static bool RiderCanMount( PlayerController player )
	{
		if ( player == null || !player.IsValid() )
			return false;

		if ( player.IsSwimming )
			return false;

		if ( !player.IsProxy && BlackjackSeat.LocalSeat != null )
			return false;

		if ( IsSeatedInAnyChair( player ) )
			return false;

		return true;
	}

	public override bool CanEnter( PlayerController player )
	{
		if ( !base.CanEnter( player ) )
			return false;

		if ( HasRider )
			return false;

		var pet = Pet;
		if ( pet == null )
			return false;

		ulong id = player?.Network?.Owner?.SteamId ?? 0ul;
		if ( id == 0ul || id != pet.OwnerSteamId )
			return false;

		var state = player.Components.Get<PvpState>();
		if ( state != null && state.InArena )
			return false;

		if ( !player.IsProxy && InteractPriority.StationWantsUse() )
			return false;

		if ( !player.IsProxy && !RiderCanMount( player ) )
			return false;

		return true;
	}

	public bool TryMountLocal( PlayerController player )
	{
		if ( IsProxy )
			return false;

		if ( player == null || !player.IsValid() || player.IsProxy )
			return false;

		if ( !CanEnter( player ) )
			return false;

		var seat = SeatPosition ?? GameObject;

		player.Body.Enabled = false;
		player.ColliderObject.Enabled = false;
		player.GameObject.SetParent( seat, false );
		player.GameObject.LocalTransform = global::Transform.Zero;

		OccupantSteamId = player.Network?.Owner?.SteamId ?? 0ul;
		return true;
	}

	public void RequestDismount()
	{
		if ( IsProxy )
			return;

		var rider = ResolveRider();

		if ( rider != null && rider.IsValid() && !rider.IsProxy && GetOccupant() == rider )
		{
			var exit = FindBestExitPoint();
			rider.GameObject.SetParent( null, true );
			rider.WorldPosition = exit;

			var cam = Scene?.Camera;
			if ( cam != null )
			{
				var ang = cam.WorldRotation.Angles();
				rider.EyeAngles = new Angles( ang.pitch, ang.yaw, 0f );
			}
		}
		else if ( rider != null && rider.IsValid() )
		{
			AskToLeave( rider );
		}

		OccupantSteamId = 0ul;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		var occ = GetOccupant();

		if ( occ.IsValid() )
		{
			if ( !occ.IsProxy && occ.GameObject.LocalPosition.Length > 100f )
			{
				occ.GameObject.SetParent( null, true );
				OccupantSteamId = 0ul;
				return;
			}

			ulong id = occ.Network?.Owner?.SteamId ?? 0ul;
			if ( id != 0ul && OccupantSteamId != id )
				OccupantSteamId = id;
		}
		else if ( OccupantSteamId != 0ul )
		{
			OccupantSteamId = 0ul;
		}
	}
}
