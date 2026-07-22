using Sandbox;
using System;

public sealed class PlayerCombat : Component
{
	[Property] public float MeleeRange { get; set; } = 130f;
	[Property] public float ArcHalfAngleDegrees { get; set; } = 60f;
	[Property] public float SwingArcHeight { get; set; } = 45f;
	[Property] public float SwingArcForward { get; set; } = 55f;

	public GameObject FindArcPvpTarget()
	{
		var cam = Scene.Camera;
		if ( cam == null )
			return null;

		Vector3 origin = GameObject.WorldPosition;
		Vector3 aim = cam.WorldRotation.Forward.WithZ( 0f );
		if ( aim.LengthSquared < 0.0001f )
			return null;
		aim = aim.Normal;

		float rangeSqr = MeleeRange * MeleeRange;
		float cosHalfAngle = MathF.Cos( ArcHalfAngleDegrees * ( MathF.PI / 180f ) );

		float bestDistSqr = float.MaxValue;
		GameObject best = null;

		foreach ( var p in PlayerHelper.GetAllPlayers() )
		{
			if ( p == null || p == GameObject )
				continue;

			if ( !PvpCombat.CanDamage( GameObject, p ) )
				continue;

			Vector3 toFlat = ( p.WorldPosition - origin ).WithZ( 0f );
			float distSqr = toFlat.LengthSquared;
			if ( distSqr > rangeSqr )
				continue;

			if ( distSqr > 0.0001f )
			{
				float dot = Vector3.Dot( aim, toFlat.Normal );
				if ( dot < cosHalfAngle )
					continue;
			}

			if ( distSqr < bestDistSqr )
			{
				bestDistSqr = distSqr;
				best = p;
			}
		}

		return best;
	}

	public void TrySwingArc( Vector3 forward )
	{
		var inventory = GameObject.Components.Get<Inventory>();
		var weaponDef = inventory?.GetEquippedWeaponDef();
		if ( weaponDef == null || weaponDef.Type != ItemType.MeleeWeapon )
			return;

		Vector3 fwd = forward.WithZ( 0f );
		if ( fwd.LengthSquared < 0.0001f )
			fwd = WorldRotation.Forward.WithZ( 0f );
		fwd = fwd.Normal;

		Vector3 origin = GameObject.WorldPosition + Vector3.Up * SwingArcHeight + fwd * SwingArcForward;

		MeleeSwingArc.Spawn( Scene, origin, fwd );
		BroadcastSwingArc( origin, fwd );
	}

	[Rpc.Broadcast]
	void BroadcastSwingArc( Vector3 origin, Vector3 forward )
	{
		if ( !IsProxy )
			return;

		MeleeSwingArc.Spawn( Scene, origin, forward );
	}

	public void DoPvpHit( GameObject target, Inventory inventory, Skills skills )
	{
		var weaponDef = inventory.GetEquippedWeaponDef();
		CombatStyle playerStyle = CombatTriangle.GetStyleFromWeapon( weaponDef );

		float weaponPower = 1f;

		if ( weaponDef != null )
		{
			weaponPower = weaponDef.WeaponPower;
		}

		float skillBonus = skills.GetCombatPower( SkillType.Attack );

		float buffMult = 1f;
		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null )
			buffMult = potionSystem.GetBuffMultiplier( BuffType.Attack );

		float enchantMult = 1f;
		if ( weaponDef != null && ( weaponDef.Type == ItemType.MeleeWeapon || weaponDef.Type == ItemType.Tool ) )
			enchantMult = 1f + inventory.GetEnchantmentBonus( EnchantmentType.Sharpness ) / 100f;

		float rawOffence = weaponPower * skillBonus * buffMult * enchantMult;

		bool isCrit = CombatConstants.RollCrit();
		if ( isCrit )
			rawOffence *= CombatConstants.CritMultiplier;

		var manaCombat = GameObject.Components.Get<ManaSystem>();
		if ( manaCombat != null )
			manaCombat.MarkCombat();

		int finalDamage = PvpCombat.ResolveDamage( rawOffence, playerStyle, target, isCrit );

		var targetHealth = target.Components.Get<PlayerHealth>();
		if ( targetHealth == null )
			return;

		int applied = targetHealth.TakeDamage( finalDamage, triggerHitFeedback: false );

		NotifyPvpHit( target, applied, isCrit, true );
	}

	public void NotifyPvpHit( GameObject target, int dealt, bool isCrit = false, bool playSound = true )
	{
		if ( target == null )
			return;

		ulong targetSteamId = target.Network?.Owner?.SteamId ?? 0ul;
		BroadcastDuelHit( targetSteamId, target, dealt, isCrit, playSound );
	}

	[Rpc.Broadcast]
	void BroadcastDuelHit( ulong targetSteamId, GameObject target, int dealt, bool isCrit, bool playSound )
	{
		DuelHealthPrediction.NotifyHit( targetSteamId, dealt );
		HitFlash.Trigger( target );

		if ( target == null )
			return;

		if ( playSound )
			SoundLibrary.PlayPvpHitLocal( target.WorldPosition );

		int maxHp = target.Components.Get<PlayerHealth>()?.MaxHealth ?? 0;
		DamagePopupBroadcaster.ShowLocal( target.WorldPosition + Vector3.Up * 60f, dealt, maxHp, isCrit, DamagePopupBroadcaster.SteamIdOf( GameObject ), targetSteamId );
	}

	public void DoMonsterHit( Monster monster, Inventory inventory, Skills skills )
	{
		var weaponDef = inventory.GetEquippedWeaponDef();
		CombatStyle playerStyle = CombatTriangle.GetStyleFromWeapon( weaponDef );

		float weaponPower = 1f;

		if ( weaponDef != null )
		{
			weaponPower = weaponDef.WeaponPower;
		}

		float skillBonus = skills.GetCombatPower( SkillType.Attack );
		float triangleMult = CombatTriangle.GetDealMultiplier( playerStyle, monster.CombatStyle );

		float buffMult = 1f;
		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null )
			buffMult = potionSystem.GetBuffMultiplier( BuffType.Attack );

		float enchantMult = 1f;
		if ( weaponDef != null && ( weaponDef.Type == ItemType.MeleeWeapon || weaponDef.Type == ItemType.Tool ) )
			enchantMult = 1f + inventory.GetEnchantmentBonus( EnchantmentType.Sharpness ) / 100f;

		int damage = (int)( weaponPower * skillBonus * triangleMult * buffMult * enchantMult );
		if ( damage < 1 ) damage = 1;

		bool isCrit = CombatConstants.RollCrit();
		if ( isCrit )
			damage = (int)( damage * CombatConstants.CritMultiplier );

		var manaCombat = GameObject.Components.Get<ManaSystem>();
		if ( manaCombat != null )
			manaCombat.MarkCombat();

		int monsterHpLeft = System.Math.Max( 0, monster.CurrentHealth - damage );
		GameLog.Add( $"You hit {monster.MonsterName} for {damage} damage{( isCrit ? " (CRIT!)" : "" )}. ({monsterHpLeft}/{monster.MaxHealth} HP left)", "#a8c8a8" );

		SoundLibrary.PlayMonsterHit( monster.WorldPosition );

		monster.TakeDamage( damage, GameObject );

		DamagePopupBroadcaster.Broadcast( monster.WorldPosition + Vector3.Up * 50f, damage, monster.MaxHealth, isCrit, DamagePopupBroadcaster.SteamIdOf( GameObject ), 0 );
	}

	public void DoBossHit( Boss boss, Inventory inventory, Skills skills )
	{
		var weaponDef = inventory.GetEquippedWeaponDef();
		CombatStyle playerStyle = CombatTriangle.GetStyleFromWeapon( weaponDef );

		float weaponPower = 1f;

		if ( weaponDef != null )
		{
			weaponPower = weaponDef.WeaponPower;
		}

		float skillBonus = skills.GetCombatPower( SkillType.Attack );
		float triangleMult = CombatTriangle.GetDealMultiplier( playerStyle, boss.CombatStyle );

		float buffMult = 1f;
		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null )
			buffMult = potionSystem.GetBuffMultiplier( BuffType.Attack );

		float enchantMult = 1f;
		if ( weaponDef != null && ( weaponDef.Type == ItemType.MeleeWeapon || weaponDef.Type == ItemType.Tool ) )
			enchantMult = 1f + inventory.GetEnchantmentBonus( EnchantmentType.Sharpness ) / 100f;

		int damage = (int)( weaponPower * skillBonus * triangleMult * buffMult * enchantMult );
		if ( damage < 1 ) damage = 1;

		bool isCrit = CombatConstants.RollCrit();
		if ( isCrit )
			damage = (int)( damage * CombatConstants.CritMultiplier );

		var manaCombat = GameObject.Components.Get<ManaSystem>();
		if ( manaCombat != null )
			manaCombat.MarkCombat();

		int bossHpLeft = System.Math.Max( 0, boss.CurrentHealth - damage );
		GameLog.Add( $"You hit {boss.BossName} for {damage} damage{( isCrit ? " (CRIT!)" : "" )}. ({bossHpLeft}/{boss.MaxHealth} HP left)", "#a8c8a8" );

		SoundLibrary.PlayMonsterHit( boss.WorldPosition );

		boss.TakeDamage( damage, GameObject );

		DamagePopupBroadcaster.Broadcast( boss.WorldPosition + Vector3.Up * 50f, damage, boss.MaxHealth, isCrit, DamagePopupBroadcaster.SteamIdOf( GameObject ), 0 );
	}

	public void DoSlimeKingHit( SlimeKing slimeKing, Inventory inventory, Skills skills )
	{
		var weaponDef = inventory.GetEquippedWeaponDef();
		CombatStyle playerStyle = CombatTriangle.GetStyleFromWeapon( weaponDef );

		float weaponPower = 1f;

		if ( weaponDef != null )
		{
			weaponPower = weaponDef.WeaponPower;
		}

		float skillBonus = skills.GetCombatPower( SkillType.Attack );
		float triangleMult = CombatTriangle.GetDealMultiplier( playerStyle, slimeKing.CombatStyle );

		float buffMult = 1f;
		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null )
			buffMult = potionSystem.GetBuffMultiplier( BuffType.Attack );

		float enchantMult = 1f;
		if ( weaponDef != null && ( weaponDef.Type == ItemType.MeleeWeapon || weaponDef.Type == ItemType.Tool ) )
			enchantMult = 1f + inventory.GetEnchantmentBonus( EnchantmentType.Sharpness ) / 100f;

		int damage = (int)( weaponPower * skillBonus * triangleMult * buffMult * enchantMult );
		if ( damage < 1 ) damage = 1;

		bool isCrit = CombatConstants.RollCrit();
		if ( isCrit )
			damage = (int)( damage * CombatConstants.CritMultiplier );

		var manaCombat = GameObject.Components.Get<ManaSystem>();
		if ( manaCombat != null )
			manaCombat.MarkCombat();

		int slimeHpLeft = System.Math.Max( 0, slimeKing.CurrentHealth - damage );
		GameLog.Add( $"You hit {slimeKing.DisplayName} for {damage} damage{( isCrit ? " (CRIT!)" : "" )}. ({slimeHpLeft}/{slimeKing.MaxHealth} HP left)", "#a8c8a8" );

		SoundLibrary.PlayMonsterHit( slimeKing.WorldPosition );

		slimeKing.TakeDamage( damage, GameObject );

		DamagePopupBroadcaster.Broadcast( slimeKing.WorldPosition + Vector3.Up * 50f, damage, slimeKing.MaxHealth, isCrit, DamagePopupBroadcaster.SteamIdOf( GameObject ), 0 );
	}
}
