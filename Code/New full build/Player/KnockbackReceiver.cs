using Sandbox;
using System;

public sealed class KnockbackReceiver : Component
{
	[Property] public float VerticalImpulse { get; set; } = 180f;

	[Sync] public float StunTimeRemaining { get; set; }
	[Sync] public float TotalTimeRemaining { get; set; }

	PlayerController _cachedController;

	public bool IsStunned => StunTimeRemaining > 0f;
	public bool IsKnockedBack => TotalTimeRemaining > 0f;

	PlayerController GetPlayerController()
	{
		if ( _cachedController == null || !_cachedController.IsValid() )
			_cachedController = Components.Get<PlayerController>();
		return _cachedController;
	}

	[Rpc.Host]
	public void ApplyKnockback( Vector3 direction, float force, float stunDuration, float totalDuration )
	{
		if ( direction.LengthSquared < 0.0001f )
			return;

		StunTimeRemaining = MathF.Max( StunTimeRemaining, stunDuration );
		TotalTimeRemaining = MathF.Max( TotalTimeRemaining, totalDuration );

		var horizontal = direction.WithZ( 0f ).Normal * force;
		var impulse = horizontal + Vector3.Up * VerticalImpulse;

		ulong ownerSteamId = Network.Owner?.SteamId ?? 0ul;
		ApplyKnockbackOnOwner( ownerSteamId, impulse );
	}

	Vector3 _pendingImpulse;
	float _pendingTimeout;

	[Rpc.Broadcast]
	void ApplyKnockbackOnOwner( ulong targetSteamId, Vector3 impulse )
	{
		if ( Connection.Local == null || Connection.Local.SteamId != targetSteamId )
			return;

		var pc = GetPlayerController();
		if ( pc == null )
			return;

		if ( IsSeated( pc ) )
		{
			LeaveChairs( pc );
			_pendingImpulse = impulse;
			_pendingTimeout = 0.6f;
			return;
		}

		pc.Jump( impulse );
	}

	bool IsSeated( PlayerController pc )
	{
		foreach ( var chair in Scene.GetAllComponents<BaseChair>() )
		{
			if ( chair != null && chair.IsOccupied && chair.GetOccupant() == pc )
				return true;
		}

		return false;
	}

	void LeaveChairs( PlayerController pc )
	{
		foreach ( var chair in Scene.GetAllComponents<BaseChair>() )
		{
			if ( chair == null || !chair.IsOccupied )
				continue;

			if ( chair.GetOccupant() == pc )
			{
				if ( chair is SlimeChair sc )
					sc.RequestDismount();
				else
					chair.AskToLeave( pc );
			}
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy || _pendingTimeout <= 0f )
			return;

		_pendingTimeout -= Time.Delta;

		var pc = GetPlayerController();
		if ( pc == null )
		{
			_pendingTimeout = 0f;
			return;
		}

		if ( !IsSeated( pc ) )
		{
			pc.Jump( _pendingImpulse );
			_pendingTimeout = 0f;
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost )
			return;

		if ( StunTimeRemaining > 0f )
		{
			StunTimeRemaining -= Time.Delta;
			if ( StunTimeRemaining < 0f )
				StunTimeRemaining = 0f;
		}

		if ( TotalTimeRemaining > 0f )
		{
			TotalTimeRemaining -= Time.Delta;
			if ( TotalTimeRemaining < 0f )
				TotalTimeRemaining = 0f;
		}
	}
}
