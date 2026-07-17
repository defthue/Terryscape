using Sandbox;

[Title( "Slime Chair" ), Group( "Pets" ), Icon( "pets" )]
public sealed class SlimeChair : BaseChair
{
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

	public override bool CanEnter( PlayerController player )
	{
		if ( !base.CanEnter( player ) )
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

		return true;
	}
}
