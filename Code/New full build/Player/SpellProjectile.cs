using Sandbox;
using System;

public sealed class SpellProjectile : Component
{
	public Vector3 Velocity { get; set; }
	public int Damage { get; set; }
	public GameObject Shooter { get; set; }
	public SpellId SpellId { get; set; }
	public float MaxRange { get; set; } = 800f;
	public float MaxLifetime { get; set; } = 4f;
	public float TraceRadius { get; set; } = 5f;
	public float FreezeDuration { get; set; }
	public float FrozenBonusDamage { get; set; } = 1.5f;

	float _distanceTraveled;
	float _lifetime;

	protected override void OnUpdate()
	{
		_lifetime += Time.Delta;
		if ( _lifetime >= MaxLifetime )
		{
			GameObject.Destroy();
			return;
		}

		Vector3 previousPos = WorldPosition;

		GameObject.WorldPosition += Velocity * Time.Delta;

		Vector3 currentPos = WorldPosition;
		Vector3 moveDir = currentPos - previousPos;
		float moveLen = moveDir.Length;

		_distanceTraveled += moveLen;

		if ( _distanceTraveled >= MaxRange )
		{
			GameObject.Destroy();
			return;
		}

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
			float triangleMult = CombatTriangle.GetDealMultiplier( CombatStyle.Magic, monster.CombatStyle );

			float frozenMult = 1f;
			if ( monster.IsFrozen )
				frozenMult = FrozenBonusDamage;

			int finalDamage = (int)( Damage * triangleMult * frozenMult );
			if ( finalDamage < 1 ) finalDamage = 1;

			monster.TakeDamage( finalDamage, Shooter );

			if ( FreezeDuration > 0f )
				monster.ApplyFreeze( FreezeDuration );

			GameObject.Destroy();
			return;
		}

		var boss = trace.GameObject.Components.Get<Boss>();
		if ( boss != null )
		{
			float triangleMult = CombatTriangle.GetDealMultiplier( CombatStyle.Magic, boss.CombatStyle );
			int finalDamage = (int)( Damage * triangleMult );
			if ( finalDamage < 1 ) finalDamage = 1;

			boss.TakeDamage( finalDamage, Shooter );
			GameObject.Destroy();
			return;
		}

		GameObject.Destroy();
	}
}