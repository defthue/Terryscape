using Sandbox;
using System;

public sealed class ArrowProjectile : Component
{
	[Property] public float Gravity { get; set; } = 600f;
	[Property] public float MaxLifetime { get; set; } = 6f;
	[Property] public float StickDuration { get; set; } = 2f;
	[Property] public float TraceRadius { get; set; } = 3f;

	public Vector3 Velocity { get; set; }
	public int Damage { get; set; }
	public GameObject Shooter { get; set; }
	public CombatStyle Style { get; set; }

	bool _stuck;
	float _lifetime;
	float _stickTimer;

	protected override void OnUpdate()
	{
		if ( _stuck )
		{
			_stickTimer -= Time.Delta;
			if ( _stickTimer <= 0f )
				GameObject.Destroy();
			return;
		}

		_lifetime += Time.Delta;
		if ( _lifetime >= MaxLifetime )
		{
			GameObject.Destroy();
			return;
		}

		Vector3 previousPos = WorldPosition;

		Velocity += Vector3.Down * Gravity * Time.Delta;
		GameObject.WorldPosition += Velocity * Time.Delta;

		Vector3 currentPos = WorldPosition;
		Vector3 moveDir = ( currentPos - previousPos );
		float moveLen = moveDir.Length;

		if ( moveLen > 0.01f )
		{
			Vector3 forward = moveDir.Normal;
			float yaw = MathF.Atan2( forward.y, forward.x ) * ( 180f / MathF.PI );
			float pitch = MathF.Asin( -forward.z ) * ( 180f / MathF.PI );
			GameObject.WorldRotation = Rotation.From( pitch, yaw, 0f );
		}

		var trace = Scene.Trace
			.Ray( previousPos, currentPos )
			.Radius( TraceRadius )
			.UseHitboxes( true )
			.IgnoreGameObject( Shooter )
			.Run();

		if ( !trace.Hit )
			return;

		var monster = trace.GameObject.Components.Get<Monster>();
		if ( monster != null )
		{
			float triangleMult = CombatTriangle.GetDealMultiplier( Style, monster.CombatStyle );
			int finalDamage = (int)( Damage * triangleMult );
			if ( finalDamage < 1 ) finalDamage = 1;

			monster.TakeDamage( finalDamage, Shooter );
			GameObject.Destroy();
			return;
		}

		var boss = trace.GameObject.Components.Get<Boss>();
		if ( boss != null )
		{
			float triangleMult = CombatTriangle.GetDealMultiplier( Style, boss.CombatStyle );
			int finalDamage = (int)( Damage * triangleMult );
			if ( finalDamage < 1 ) finalDamage = 1;

			boss.TakeDamage( finalDamage, Shooter );
			GameObject.Destroy();
			return;
		}

		Stick( trace.HitPosition );
	}

	void Stick( Vector3 hitPos )
	{
		_stuck = true;
		_stickTimer = StickDuration;
		GameObject.WorldPosition = hitPos;
		Velocity = Vector3.Zero;
	}
}