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
	public float SlowDuration { get; set; }
	public float SlowMultiplier { get; set; } = 1f;
	public float FrozenBonusDamage { get; set; } = 1.5f;
	public bool IsCrit { get; set; }

	const float HomingImpactRadius = 35f;

	float _distanceTraveled;
	float _lifetime;

	protected override void OnStart()
	{
		if ( Components.Get<HomingProjectile>() == null )
			return;

		bool hasLegacyVfx = false;
		foreach ( var effect in Components.GetAll<ParticleEffect>( FindMode.EverythingInSelfAndDescendants ) )
		{
			effect.Enabled = false;
			hasLegacyVfx = true;
		}

		if ( !hasLegacyVfx )
			return;

		foreach ( var emitter in Components.GetAll<ParticleSphereEmitter>( FindMode.EverythingInSelfAndDescendants ) )
			emitter.Enabled = false;
		foreach ( var sprite in Components.GetAll<ParticleSpriteRenderer>( FindMode.EverythingInSelfAndDescendants ) )
			sprite.Enabled = false;
		foreach ( var trail in Components.GetAll<TrailRenderer>( FindMode.EverythingInSelfAndDescendants ) )
			trail.Enabled = false;

		var visual = Components.GetOrCreate<FireballVisual>();
		visual.Tint = new Color( 1f, 0.55f, 0.45f );
		visual.LightColor = new Color( 1f, 0.35f, 0.15f );
	}

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

		var homing = Components.Get<HomingProjectile>();
		if ( homing != null && homing.Target != null && homing.Target.IsValid() )
		{
			Vector3 targetPoint = homing.Target.WorldPosition + Vector3.Up * homing.TargetHeightOffset;
			if ( ( targetPoint - currentPos ).Length <= HomingImpactRadius )
			{
				ApplyImpact( homing.Target, currentPos );
				return;
			}
		}

		var trace = Scene.Trace
			.Ray( previousPos, currentPos )
			.Radius( TraceRadius )
			.UseHitboxes( true )
			.IgnoreGameObject( Shooter )
			.Run();

		if ( !trace.Hit )
		{
			var slimeNear = SlimeKing.FindAlongPath( Scene, previousPos, currentPos, TraceRadius );
			if ( slimeNear != null )
				ApplyImpact( slimeNear.GameObject, currentPos );
			return;
		}

		ApplyImpact( trace.GameObject, trace.HitPosition );
	}

	void ApplyImpact( GameObject hitObject, Vector3 hitPos )
	{
		var pvpTarget = PvpCombat.ResolveTarget( hitObject, Shooter );
		if ( pvpTarget != null )
		{
			int finalDamage = PvpCombat.ResolveDamage( Damage, CombatStyle.Magic, pvpTarget, IsCrit );
			var targetHealth = pvpTarget.Components.Get<PlayerHealth>();
			if ( targetHealth != null )
			{
				int applied = targetHealth.TakeDamage( finalDamage, triggerHitFeedback: false );
				Shooter?.Components.Get<PlayerCombat>()?.NotifyPvpHit( pvpTarget, applied, IsCrit, true );
			}
			PlayImpactSound( hitPos );
			GameObject.Destroy();
			return;
		}

		var monster = hitObject.Components.Get<Monster>();

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

			if ( SlowDuration > 0f )
				monster.ApplySlow( SlowDuration, SlowMultiplier );

			DamagePopupBroadcaster.Broadcast( hitPos, finalDamage, monster.MaxHealth, IsCrit, DamagePopupBroadcaster.SteamIdOf( Shooter ), 0 );

			PlayImpactSound( hitPos );

			GameObject.Destroy();
			return;
		}

		var boss = hitObject.Components.Get<Boss>();
		if ( boss != null )
		{
			float triangleMult = CombatTriangle.GetDealMultiplier( CombatStyle.Magic, boss.CombatStyle );
			int finalDamage = (int)( Damage * triangleMult );
			if ( finalDamage < 1 ) finalDamage = 1;

			boss.TakeDamage( finalDamage, Shooter );
			DamagePopupBroadcaster.Broadcast( hitPos, finalDamage, boss.MaxHealth, IsCrit, DamagePopupBroadcaster.SteamIdOf( Shooter ), 0 );
			PlayImpactSound( hitPos );
			GameObject.Destroy();
			return;
		}

		var slimeKing = hitObject.Components.Get<SlimeKing>();
		if ( slimeKing != null )
		{
			float triangleMult = CombatTriangle.GetDealMultiplier( CombatStyle.Magic, slimeKing.CombatStyle );
			int finalDamage = (int)( Damage * triangleMult );
			if ( finalDamage < 1 ) finalDamage = 1;

			slimeKing.TakeDamage( finalDamage, Shooter );
			DamagePopupBroadcaster.Broadcast( hitPos, finalDamage, slimeKing.MaxHealth, IsCrit, DamagePopupBroadcaster.SteamIdOf( Shooter ), 0 );
			PlayImpactSound( hitPos );
			GameObject.Destroy();
			return;
		}

		PlayImpactSound( hitPos );
		GameObject.Destroy();
	}

	void PlayImpactSound( Vector3 pos )
	{
		switch ( SpellId )
		{
			case SpellId.IceShard:
				SoundLibrary.PlayIceShardImpact( pos );
				break;
		}
	}
}