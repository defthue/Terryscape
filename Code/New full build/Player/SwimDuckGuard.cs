using Sandbox;

public sealed class SwimDuckGuard : Component
{
	PlayerController _pc;

	protected override void OnStart()
	{
		_pc = Components.Get<PlayerController>();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( _pc == null || !_pc.IsValid() )
			return;

		if ( !_pc.IsSwimming )
			return;

		Input.Clear( "Duck" );
	}
}
