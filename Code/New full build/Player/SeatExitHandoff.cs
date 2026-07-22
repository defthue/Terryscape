using Sandbox;
using Sandbox.Movement;

public sealed class SeatExitHandoff : Component
{
	PlayerController _pc;
	bool _wasSeated;

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

		bool seated = _pc.GetComponentInParent<ISitTarget>() != null;

		if ( !seated && ( _wasSeated || BodySuspended() ) )
			RestoreController();

		_wasSeated = seated;
	}

	bool BodySuspended()
	{
		var body = _pc.Body;
		return body != null && body.IsValid() && !body.Enabled;
	}

	void RestoreController()
	{
		var body = _pc.Body;
		if ( body != null && body.IsValid() && !body.Enabled )
			body.Enabled = true;

		var colliders = _pc.ColliderObject;
		if ( colliders != null && colliders.IsValid() && !colliders.Enabled )
			colliders.Enabled = true;
	}
}
