using Sandbox;
using Sandbox.Movement;

[Icon( "scuba_diving" ), Group( "Movement" ), Title( "MoveMode - Swim (Validated)" )]
public sealed class SwimModeValidated : MoveModeSwim
{
	public override void OnModeBegin()
	{
		base.OnModeBegin();

		if ( IsProxy )
			return;

		var inventory = Components.Get<Inventory>();
		if ( inventory == null )
			return;

		if ( inventory.GetEquipped( EquipSlot.Weapon ) == ItemId.None )
			return;

		if ( !inventory.Unequip( EquipSlot.Weapon ) )
			inventory.UnequipToBank( EquipSlot.Weapon );
	}

	public override int Score( PlayerController controller )
	{
		if ( base.Score( controller ) < 0 )
			return -100;

		if ( controller == null || controller.Body == null || !controller.Body.IsValid() )
			return -100;

		var probe = WorldTransform.PointToWorld( new Vector3( 0f, 0f, controller.CurrentHeight * SwimLevel ) );

		foreach ( var touch in controller.Body.Touching )
		{
			if ( touch == null || !touch.IsValid() )
				continue;

			if ( !touch.Tags.Contains( "water" ) )
				continue;

			if ( Vector3.DistanceBetween( touch.FindClosestPoint( probe ), probe ) < 1f )
				return Priority;
		}

		return -100;
	}
}
