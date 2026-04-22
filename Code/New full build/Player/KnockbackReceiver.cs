using Sandbox;
using System;

public sealed class KnockbackReceiver : Component
{
	[Property] public float FrictionPerSecond { get; set; } = 6f;

	[Sync] public Vector3 KnockbackVelocity { get; set; }
	[Sync] public float StunTimeRemaining { get; set; }
	[Sync] public float TotalTimeRemaining { get; set; }

	PlayerController _cachedController;

	public bool IsStunned => StunTimeRemaining > 0f;
	public bool IsKnockedBack => TotalTimeRemaining > 0f || KnockbackVelocity.LengthSquared > 0.01f;

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

		KnockbackVelocity = direction.Normal * force;
		StunTimeRemaining = MathF.Max( StunTimeRemaining, stunDuration );
		TotalTimeRemaining = MathF.Max( TotalTimeRemaining, totalDuration );
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
			TotalTimeRemaining -= Time.Delta;

		if ( KnockbackVelocity.LengthSquared > 0.01f )
		{
			GameObject.WorldPosition += KnockbackVelocity * Time.Delta;

			float drop = FrictionPerSecond * Time.Delta;
			float newLen = MathF.Max( 0f, KnockbackVelocity.Length - KnockbackVelocity.Length * drop );
			KnockbackVelocity = KnockbackVelocity.Normal * newLen;

			if ( KnockbackVelocity.Length < 5f )
				KnockbackVelocity = Vector3.Zero;
		}
	}
}