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

		var horizontal = direction.WithZ( 0f ).Normal * force;
		var impulse = horizontal + Vector3.Up * VerticalImpulse;

		StunTimeRemaining = MathF.Max( StunTimeRemaining, stunDuration );
		TotalTimeRemaining = MathF.Max( TotalTimeRemaining, totalDuration );

		var pc = GetPlayerController();
		if ( pc != null )
			pc.Jump( impulse );
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

			var pc = GetPlayerController();
			if ( pc != null )
				pc.WishVelocity = Vector3.Zero;
		}

		if ( TotalTimeRemaining > 0f )
		{
			TotalTimeRemaining -= Time.Delta;
			if ( TotalTimeRemaining < 0f )
				TotalTimeRemaining = 0f;
		}
	}
}