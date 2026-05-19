using Sandbox;
using System;

public sealed class ProjectileShooter : Component
{
	[Property] public GameObject ArrowPrefab { get; set; }
	[Property] public GameObject AimSource { get; set; }
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }
	[Property] public float MinDrawDuration { get; set; } = 0.6f;
	[Property] public float ArrowSpeed { get; set; } = 1200f;

	[Property, Group( "First Person Offsets" )] public float FpForwardOffset { get; set; } = 60f;
	[Property, Group( "First Person Offsets" )] public float FpHeightOffset { get; set; } = 30f;
	[Property, Group( "First Person Offsets" )] public float FpLateralOffset { get; set; } = 0f;

	[Property, Group( "Third Person Offsets" )] public float TpForwardOffset { get; set; } = 60f;
	[Property, Group( "Third Person Offsets" )] public float TpHeightOffset { get; set; } = 30f;
	[Property, Group( "Third Person Offsets" )] public float TpLateralOffset { get; set; } = 15f;

	[Property, Group( "Draw Animation" )] public int DrawHoldType { get; set; } = 1;
	[Property, Group( "Draw Animation" )] public int DrawHoldTypeAttack { get; set; } = 0;

	[Property, Group( "Aim Trace" )] public float AimTraceDistance { get; set; } = 5000f;

	public bool IsDrawing { get; private set; }
	public bool IsDrawReady { get; private set; }
	public float DrawProgress
	{
		get
		{
			float effective = EffectiveDrawDuration;
			return effective > 0f ? MathF.Min( _drawTimer / effective, 1f ) : 1f;
		}
	}

	public float EffectiveDrawDuration
	{
		get
		{
			var inventory = GameObject.Components.Get<Inventory>();
			var penalties = CombatTriangle.GetEquippedArmorPenalties( inventory );
			return MinDrawDuration + penalties.RangedDrawPenalty;
		}
	}

	float _drawTimer;

	bool IsThirdPerson()
	{
		var pc = GameObject.Components.Get<PlayerController>();
		if ( pc == null )
			return true;

		return pc.ThirdPerson;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( !IsDrawing )
			return;

		_drawTimer += Time.Delta;

		if ( DrawProgress >= 1f )
			IsDrawReady = true;

		if ( !Input.Down( "attack1" ) )
		{
			if ( DrawProgress >= 1f )
				ReleaseShot();
			else
				CancelDraw();
		}
	}

	public bool StartDraw()
	{
		if ( IsDrawing )
			return false;

		if ( ArrowPrefab == null )
			return false;

		var inventory = GameObject.Components.Get<Inventory>();
		if ( inventory == null )
			return false;

		var bowDef = inventory.GetEquippedWeaponDef();
		if ( bowDef == null || bowDef.Type != ItemType.RangedWeapon )
			return false;

		var ammoId = inventory.GetEquippedAmmoId();
		if ( ammoId == ItemId.None )
		{
			GameLog.Add( "You have no arrows equipped.", "#c86464" );
			return false;
		}

		if ( inventory.GetEquippedAmmoCount() <= 0 )
		{
			GameLog.Add( "You're out of arrows.", "#c86464" );
			return false;
		}

		IsDrawing = true;
		IsDrawReady = false;
		_drawTimer = 0f;

		if ( BodyRenderer != null )
		{
			BodyRenderer.Set( "holdtype", DrawHoldType );
			BodyRenderer.Set( "holdtype_attack", DrawHoldTypeAttack );
			BodyRenderer.Set( "b_attack", true );
		}

		BroadcastDrawAnim();

		SoundLibrary.PlayBowPull( WorldPosition );

		ArcherAimCamera.NotifyAimActivity();

		return true;
	}

	[Rpc.Broadcast]
	void BroadcastDrawAnim()
	{
		if ( BodyRenderer != null )
		{
			BodyRenderer.Set( "holdtype", DrawHoldType );
			BodyRenderer.Set( "b_attack", true );
		}
	}

	public void CancelDraw()
	{
		if ( !IsDrawing )
			return;

		IsDrawing = false;
		IsDrawReady = false;
		_drawTimer = 0f;
		GameLog.Add( "Shot cancelled.", "#6a6a6a" );
	}

	void ReleaseShot()
	{
		IsDrawing = false;
		IsDrawReady = false;
		_drawTimer = 0f;

		ArcherAimCamera.NotifyAimActivity();

		var inventory = GameObject.Components.Get<Inventory>();
		var skills = GameObject.Components.Get<Skills>();
		if ( inventory == null || skills == null )
			return;

		var bowDef = inventory.GetEquippedWeaponDef();
		if ( bowDef == null )
			return;

		var ammoId = inventory.GetEquippedAmmoId();
		if ( ammoId == ItemId.None || inventory.GetEquippedAmmoCount() <= 0 )
		{
			GameLog.Add( "You're out of arrows.", "#c86464" );
			return;
		}

		var ammoDef = ItemDatabase.Get( ammoId );
		float arrowPower = ammoDef != null ? ammoDef.WeaponPower : 0f;

		if ( !inventory.ConsumeAmmo( 1 ) )
			return;

		float skillBonus = skills.GetCombatPower( SkillType.Archery );
		float buffMult = 1f;

		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null )
			buffMult = potionSystem.GetBuffMultiplier( BuffType.Archery );

		float piercingBonus = inventory.GetEnchantmentBonus( EnchantmentType.Piercing );
		float enchantMult = 1f + piercingBonus / 100f;

		float totalPower = ( bowDef.WeaponPower + arrowPower ) * skillBonus * buffMult * enchantMult;
		int damage = (int)totalPower;
		if ( damage < 1 ) damage = 1;

		if ( AimSource == null )
			return;

		bool tp = IsThirdPerson();
		float forwardOff = tp ? TpForwardOffset : FpForwardOffset;
		float heightOff = tp ? TpHeightOffset : FpHeightOffset;
		float lateralOff = tp ? TpLateralOffset : FpLateralOffset;

		var aimForward = AimSource.WorldRotation.Forward;
		var aimRight = AimSource.WorldRotation.Right;

		var spawnPos =
			GameObject.WorldPosition +
			Vector3.Up * heightOff +
			aimForward * forwardOff +
			aimRight * lateralOff;

		Vector3 launchDir = aimForward;

		var camera = Scene.Camera;
		if ( camera != null )
		{
			var camPos = camera.WorldPosition;
			var camForward = camera.WorldRotation.Forward;
			var camEnd = camPos + camForward * AimTraceDistance;

			var aimTrace = Scene.Trace
				.Ray( camPos, camEnd )
				.UseHitboxes( true )
				.IgnoreGameObjectHierarchy( GameObject )
				.Run();

			var aimTarget = aimTrace.Hit ? aimTrace.HitPosition : camEnd;
			var toTarget = aimTarget - spawnPos;
			if ( toTarget.LengthSquared > 0.01f )
				launchDir = toTarget.Normal;
		}

		var arrow = ArrowPrefab.Clone( spawnPos );

		float yaw = MathF.Atan2( launchDir.y, launchDir.x ) * ( 180f / MathF.PI );
		float pitch = MathF.Asin( -launchDir.z ) * ( 180f / MathF.PI );
		arrow.WorldRotation = Rotation.From( pitch, yaw, 0f );

		arrow.NetworkSpawn();

		var projectile = arrow.Components.Get<ArrowProjectile>();
		if ( projectile != null )
		{
			projectile.Velocity = launchDir * ArrowSpeed;
			projectile.Damage = damage;
			projectile.Shooter = GameObject;
			projectile.Style = CombatStyle.Ranged;
		}

		GameLog.Add( $"You fire an arrow! ({damage} power)", "#a8c8a8" );

		skills.AddXp( SkillType.Archery, 2 );

		SoundLibrary.PlayBowRelease( WorldPosition );

		if ( BodyRenderer != null )
		{
			BodyRenderer.Set( "holdtype", DrawHoldType );
			BodyRenderer.Set( "holdtype_attack", 1 );
			BodyRenderer.Set( "b_attack", true );
		}

		BroadcastReleaseAnim();
	}

	[Rpc.Broadcast]
	void BroadcastReleaseAnim()
	{
		if ( BodyRenderer != null )
		{
			BodyRenderer.Set( "holdtype", DrawHoldType );
			BodyRenderer.Set( "holdtype_attack", 1 );
			BodyRenderer.Set( "b_attack", true );
		}
	}
}
