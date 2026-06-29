using Sandbox;
using System;
using System.Collections.Generic;

public sealed class SpellCaster : Component
{
	[Property, Group( "Spell Prefabs" )] public GameObject FireballPrefab { get; set; }
	[Property, Group( "Spell Prefabs" )] public GameObject IceShardPrefab { get; set; }
	[Property, Group( "Spell Prefabs" )] public GameObject DarkBlastPrefab { get; set; }
	[Property, Group( "Spell Prefabs" )] public GameObject MagicMissilePrefab { get; set; }
	[Property, Group( "Spell Prefabs" )] public GameObject BarrierPrefab { get; set; }
	[Property, Group( "Spell Prefabs" )] public GameObject StoneskinAuraPrefab { get; set; }
	[Property, Group( "Spell Prefabs" )] public GameObject AcidSpitPrefab { get; set; }

	[Property, Group( "Spell VFX" )] public Sprite GlowSprite { get; set; }

	[Property] public GameObject AimSource { get; set; }
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }

	[Property, Group( "First Person Offsets" )] public float FpForwardOffset { get; set; } = 60f;
	[Property, Group( "First Person Offsets" )] public float FpHeightOffset { get; set; } = 30f;
	[Property, Group( "First Person Offsets" )] public float FpLateralOffset { get; set; } = 0f;

	[Property, Group( "Third Person Offsets" )] public float TpForwardOffset { get; set; } = 60f;
	[Property, Group( "Third Person Offsets" )] public float TpHeightOffset { get; set; } = 30f;
	[Property, Group( "Third Person Offsets" )] public float TpLateralOffset { get; set; } = 15f;

	[Property, Group( "Barrier" )] public float BarrierForwardOffset { get; set; } = 100f;
	[Property, Group( "Barrier" )] public float BarrierHeightOffset { get; set; } = 0f;
	[Property, Group( "Barrier" )] public float BarrierLateralOffset { get; set; } = 0f;
	[Property, Group( "Barrier" )] public Color BarrierTint { get; set; } = new Color( 0.2f, 0.4f, 1f, 0.4f );
	[Property, Group( "Barrier" )] public float BarrierYawOffset { get; set; } = 0f;

	[Property, Group( "Aim Trace" )] public float AimTraceDistance { get; set; } = 5000f;

	[Property, Group( "Homing" )] public float HomingAcquireConeDegrees { get; set; } = 25f;
	[Property, Group( "Homing" )] public float HomingMaxAcquireRange { get; set; } = 2500f;
	[Property, Group( "Homing" )] public float HomingTurnSpeed { get; set; } = 8f;

	[Property, Group( "Heal Pulse" )] public Color HealPulseColor { get; set; } = new Color( 0.4f, 1f, 0.6f, 0.55f );

	[Property, Group( "Lightning" )] public Color LightningColor { get; set; } = new Color( 0.78f, 0.88f, 1f, 1f );
	[Property, Group( "Lightning" )] public Color LightningHaloColor { get; set; } = new Color( 0.4f, 0.6f, 1f, 1f );
	[Property, Group( "Lightning" )] public float LightningForwardOffset { get; set; } = 60f;
	[Property, Group( "Lightning" )] public float LightningHeightOffset { get; set; } = 40f;
	[Property, Group( "Lightning" )] public float LightningLateralOffset { get; set; } = 0f;

	[Property, Group( "Acid Spit" )] public float AcidGravity { get; set; } = 800f;
	[Property, Group( "Acid Spit" )] public float AcidLaunchPitchDegrees { get; set; } = 35f;
	[Property, Group( "Acid Spit" )] public Color AcidPoolColor { get; set; } = new Color( 0.5f, 0.95f, 0.2f, 0.55f );

	[Property, Group( "Inferno" )] public Color InfernoReticleColor { get; set; } = new Color( 1f, 0.5f, 0.15f, 1f );

	[Property, Group( "Singularity" )] public Color SingularityReticleColor { get; set; } = new Color( 0.7f, 0.4f, 1f, 1f );

	const float BarrierWidth = 4f;
	const float BarrierHeight = 3f;
	const float BarrierDepth = 0.2f;
	const float BarrierDuration = 5f;

	public bool IsCasting { get; private set; }
	public bool IsCastReady { get; private set; }
	public float CastProgress => _activeSpell != null && _activeSpell.MinCastTime > 0f ?
		MathF.Min( _castTimer / _activeSpell.MinCastTime, 1f ) : 1f;
	public SpellDefinition ActiveSpell => _activeSpell;

	public bool IsChannelling => _activeChannel != null && _activeChannel.IsActive;

	SpellDefinition _activeSpell;
	float _castTimer;
	string _castAction;

	LightningBoltChannel _activeChannel;
	string _channelAction;

	GameObject _activeReticle;

	Dictionary<SpellId, float> _cooldownEndTimes = new();

	public float GetCooldownRemaining( SpellId id )
	{
		if ( _cooldownEndTimes.TryGetValue( id, out var endTime ) )
		{
			float remaining = endTime - Time.Now;
			return remaining > 0f ? remaining : 0f;
		}
		return 0f;
	}

	bool IsThirdPerson()
	{
		var pc = GameObject.Components.Get<PlayerController>();
		if ( pc == null )
			return true;

		return pc.ThirdPerson;
	}

	GameObject GetPrefabForSpell( SpellId id )
	{
		switch ( id )
		{
			case SpellId.Fireball: return FireballPrefab;
			case SpellId.IceShard: return IceShardPrefab;
			case SpellId.DarkBlast: return DarkBlastPrefab;
			case SpellId.MagicMissile: return MagicMissilePrefab != null ? MagicMissilePrefab : FireballPrefab;
			case SpellId.AcidSpit: return AcidSpitPrefab;
			default: return null;
		}
	}

	protected override void OnUpdate()
	{
		if ( GlowSprite != null )
			SpellVfx.GlowSprite = GlowSprite;

		if ( IsProxy )
			return;

		var inventory = GameObject.Components.Get<Inventory>();
		bool hasMagicWeapon = inventory != null && inventory.IsWeaponMagic();

		if ( !hasMagicWeapon || PlayerGatherResource.UIOpen || SpellbookStation.IsOpen )
		{
			if ( IsCasting )
				CancelCast();
			if ( IsChannelling )
				StopChannel();
			return;
		}

		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null && potionSystem.IsDrinking )
		{
			if ( IsCasting )
				CancelCast();
			if ( IsChannelling )
				StopChannel();
			return;
		}

		if ( IsChannelling )
		{
			ArcherAimCamera.NotifyAimActivity();
			if ( !Input.Down( _channelAction ) || !_activeChannel.IsActive )
				StopChannel();
			return;
		}

		if ( !IsCasting )
		{
			if ( Input.Pressed( "attack1" ) )
				TryStartCastForSlot( 1, "attack1" );
			else if ( Input.Pressed( "attack2" ) )
				TryStartCastForSlot( 2, "attack2" );

			return;
		}

		_castTimer += Time.Delta;

		if ( CastProgress >= 1f )
			IsCastReady = true;

		if ( _activeSpell != null && _activeReticle != null && _activeReticle.IsValid() )
		{
			if ( _activeSpell.Type == SpellType.Lobbed )
			{
				Vector3 landing = PredictLobbedLanding( _activeSpell );
				_activeReticle.WorldPosition = landing;
			}
			else if ( _activeSpell.Type == SpellType.GroundEffect )
			{
				Vector3 landing = GetCursorGroundPoint( _activeSpell.MaxRange );
				_activeReticle.WorldPosition = landing;
			}
		}

		bool released = !string.IsNullOrEmpty( _castAction ) && !Input.Down( _castAction );
		if ( released )
		{
			if ( CastProgress >= 1f )
				ReleaseCast();
			else
				CancelCast();
		}
	}

	void TryStartCastForSlot( int slotIndex, string action )
	{
		if ( !SpellbookState.IsSlotBound( slotIndex ) )
			return;

		var spellId = SpellbookState.GetSlot( slotIndex );
		if ( !SpellbookState.IsUnlocked( spellId ) )
			return;

		var spell = SpellDatabase.Get( spellId );
		if ( spell == null )
			return;

		if ( spell.Type == SpellType.Channelled )
		{
			StartChannel( spell, action );
			return;
		}

		StartCast( spellId, action );
	}

	void StartCast( SpellId spellId, string action )
	{
		var spell = SpellDatabase.Get( spellId );
		if ( spell == null )
			return;

		float cdRemaining = GetCooldownRemaining( spellId );
		if ( cdRemaining > 0f )
		{
			GameLog.Add( $"{spell.Name} is on cooldown. ({(int)MathF.Ceiling( cdRemaining )}s left)", "#c86464" );
			return;
		}

		var mana = GameObject.Components.Get<ManaSystem>();
		if ( mana == null || !mana.HasMana( spell.ManaCost ) )
		{
			GameLog.Add( $"Not enough mana to cast {spell.Name}. ({( mana != null ? mana.CurrentMana : 0 )}/{spell.ManaCost})", "#c86464" );
			return;
		}

		if ( spell.Type == SpellType.Projectile || spell.Type == SpellType.Homing || spell.Type == SpellType.Lobbed )
		{
			var prefab = GetPrefabForSpell( spellId );
			if ( prefab == null )
			{
				GameLog.Add( $"No prefab assigned for {spell.Name}.", "#c86464" );
				return;
			}
		}

		_activeSpell = spell;
		_castAction = action;
		_castTimer = 0f;
		IsCasting = true;
		IsCastReady = false;

		Log.Info( $"[SpellCaster] StartCast {spell.Name} — MinCastTime={spell.MinCastTime}s, ManaCost={spell.ManaCost}, Dmg×{spell.DamageMultiplier}" );

		if ( spell.Type == SpellType.Lobbed )
		{
			_activeReticle = SpawnLobbedReticle( spell.SplashRadius, AcidPoolColor );
		}
		else if ( spell.Type == SpellType.GroundEffect )
		{
			float radius = GetGroundEffectRadiusForSpell( spell.Id );
			Color color = GetGroundEffectReticleColorForSpell( spell.Id );
			_activeReticle = SpawnLobbedReticle( radius, color );
		}

		ArcherAimCamera.NotifyAimActivity();
	}

	void StartChannel( SpellDefinition spell, string action )
	{
		float cdRemaining = GetCooldownRemaining( spell.Id );
		if ( cdRemaining > 0f )
		{
			GameLog.Add( $"{spell.Name} is on cooldown. ({(int)MathF.Ceiling( cdRemaining )}s left)", "#c86464" );
			return;
		}

		var mana = GameObject.Components.Get<ManaSystem>();
		if ( mana == null || !mana.HasMana( spell.ManaCost ) )
		{
			GameLog.Add( $"Not enough mana to cast {spell.Name}. ({( mana != null ? mana.CurrentMana : 0 )}/{spell.ManaCost})", "#c86464" );
			return;
		}

		var existing = GameObject.Components.Get<LightningBoltChannel>();
		if ( existing != null )
			existing.Destroy();

		var channel = CreateLightningChannelLocal( spell.Id, false );
		if ( channel == null )
			return;

		_activeChannel = channel;
		_channelAction = action;

		if ( BodyRenderer != null )
		{
			BodyRenderer.Set( "holdtype", 6 );
			BodyRenderer.Set( "b_attack", true );
		}
		BroadcastCastAnim();
		BroadcastLightningStart( (int)spell.Id );
		ArcherAimCamera.NotifyAimActivity();

		GameLog.Add( $"You channel {spell.Name}!", "#c8d0ff" );
	}

	LightningBoltChannel CreateLightningChannelLocal( SpellId spellId, bool visualOnly )
	{
		var spell = SpellDatabase.Get( spellId );
		if ( spell == null )
			return null;

		var channel = GameObject.Components.Create<LightningBoltChannel>();
		channel.BoltColor = LightningColor;
		channel.HaloColor = LightningHaloColor;
		channel.ConeRange = spell.MaxRange;
		channel.ForwardOffset = LightningForwardOffset;
		channel.HeightOffset = LightningHeightOffset;
		channel.LateralOffset = LightningLateralOffset;
		channel.VisualOnly = visualOnly;

		channel.Begin( GameObject, AimSource != null ? AimSource : GameObject, spell );

		return channel;
	}

	void StopChannel()
	{
		if ( _activeChannel != null && _activeChannel.IsValid() )
		{
			_activeChannel.End();
			_activeChannel.Destroy();
		}
		_activeChannel = null;
		_channelAction = null;

		BroadcastLightningStop();
	}

	[Rpc.Broadcast]
	void BroadcastLightningStart( int spellIdRaw )
	{
		if ( !IsProxy )
			return;

		var existing = GameObject.Components.Get<LightningBoltChannel>();
		if ( existing != null )
			existing.Destroy();

		CreateLightningChannelLocal( (SpellId)spellIdRaw, true );
	}

	[Rpc.Broadcast]
	void BroadcastLightningStop()
	{
		if ( !IsProxy )
			return;

		var existing = GameObject.Components.Get<LightningBoltChannel>();
		if ( existing != null )
		{
			existing.End();
			existing.Destroy();
		}
	}

	[Rpc.Broadcast]
	void BroadcastCastAnim()
	{
		if ( BodyRenderer != null )
		{
			BodyRenderer.Set( "holdtype", 6 );
			BodyRenderer.Set( "b_attack", true );
		}
	}

	void CancelCast()
	{
		IsCasting = false;
		IsCastReady = false;
		_activeSpell = null;
		_castAction = null;
		_castTimer = 0f;

		if ( _activeReticle != null && _activeReticle.IsValid() )
			_activeReticle.Destroy();
		_activeReticle = null;
	}

	void ReleaseCast()
	{
		var spell = _activeSpell;

		IsCasting = false;
		IsCastReady = false;
		_activeSpell = null;
		_castAction = null;
		_castTimer = 0f;

		if ( spell == null )
			return;

		var mana = GameObject.Components.Get<ManaSystem>();
		if ( mana == null || !mana.ConsumeMana( spell.ManaCost ) )
		{
			GameLog.Add( "Not enough mana!", "#c86464" );
			return;
		}

		if ( mana != null )
			mana.MarkCombat();

		if ( BodyRenderer != null )
		{
			BodyRenderer.Set( "holdtype", 6 );
			BodyRenderer.Set( "holdtype_attack", 0 );
			BodyRenderer.Set( "b_attack", true );
		}

		BroadcastCastAnim();
		ArcherAimCamera.NotifyAimActivity();

		if ( spell.Cooldown > 0f )
			_cooldownEndTimes[spell.Id] = Time.Now + spell.Cooldown;

		if ( spell.Type == SpellType.Barrier )
		{
			SpawnBarrier( spell );
			return;
		}

		if ( spell.Type == SpellType.SelfBuff )
		{
			ApplySelfBuff( spell );
			return;
		}

		if ( spell.Type == SpellType.SelfAoE )
		{
			ApplySelfAoE( spell );
			return;
		}

		if ( spell.Type == SpellType.Lobbed )
		{
			SpawnLobbedProjectile( spell );
			if ( _activeReticle != null && _activeReticle.IsValid() )
				_activeReticle.Destroy();
			_activeReticle = null;
			return;
		}

		if ( spell.Type == SpellType.GroundEffect )
		{
			SpawnGroundEffect( spell );
			if ( _activeReticle != null && _activeReticle.IsValid() )
				_activeReticle.Destroy();
			_activeReticle = null;
			return;
		}

		SpawnProjectile( spell );
	}

	void ApplySelfBuff( SpellDefinition spell )
	{
		var skills = GameObject.Components.Get<Skills>();
		if ( skills != null )
			skills.AddXp( SkillType.Magic, 2 );

		int manaLeft = 0;
		var manaCheck = GameObject.Components.Get<ManaSystem>();
		if ( manaCheck != null )
			manaLeft = manaCheck.CurrentMana;

		switch ( spell.Id )
		{
			case SpellId.Stoneskin:
				ApplyStoneskinLocal( spell.BuffDuration, false );
				BroadcastStoneskinBegin( spell.BuffDuration );
				GameLog.Add( $"You cast Stoneskin! Heavy armor for {(int)spell.BuffDuration}s. ({manaLeft} mana left)", "#a0a0a8" );
				break;

			default:
				GameLog.Add( $"You cast {spell.Name}! ({manaLeft} mana left)", "#7a5aaa" );
				break;
		}
	}

	void ApplyStoneskinLocal( float duration, bool visualOnly )
	{
		var existing = GameObject.Components.Get<StoneskinBuff>();
		if ( existing != null && existing.IsValid() )
		{
			existing.VisualOnly = visualOnly;
			existing.Begin( duration );
			return;
		}

		var buff = GameObject.Components.Create<StoneskinBuff>();
		buff.VisualOnly = visualOnly;
		buff.Begin( duration );
	}

	[Rpc.Broadcast]
	void BroadcastStoneskinBegin( float duration )
	{
		if ( !IsProxy )
			return;

		ApplyStoneskinLocal( duration, true );
	}

	void ApplySelfAoE( SpellDefinition spell )
	{
		var skills = GameObject.Components.Get<Skills>();
		if ( skills != null )
			skills.AddXp( SkillType.Magic, 4 );

		int manaLeft = 0;
		var manaCheck = GameObject.Components.Get<ManaSystem>();
		if ( manaCheck != null )
			manaLeft = manaCheck.CurrentMana;

		switch ( spell.Id )
		{
			case SpellId.HealPulse:
				CastHealPulse( spell, manaLeft );
				break;

			default:
				GameLog.Add( $"You cast {spell.Name}! ({manaLeft} mana left)", "#7a5aaa" );
				break;
		}
	}

	void CastHealPulse( SpellDefinition spell, int manaLeft )
	{
		var skills = GameObject.Components.Get<Skills>();
		int magicLevel = skills != null ? skills.GetLevel( SkillType.Magic ) : 1;
		int healAmount = 10 + magicLevel;

		int targets = 0;
		Vector3 origin = WorldPosition;
		float range = spell.MaxRange;
		float rangeSqr = range * range;

		foreach ( var player in PlayerHelper.GetAllPlayers() )
		{
			if ( player == null )
				continue;

			if ( ( player.WorldPosition - origin ).LengthSquared > rangeSqr )
				continue;

			var health = player.Components.Get<PlayerHealth>();
			if ( health == null || health.IsDead )
				continue;

			if ( PvpCombat.CanDamage( GameObject, player ) )
				continue;

			HealPlayer( player, healAmount );
			targets++;
		}

		HealPulseRing.Spawn( Scene, origin, range, HealPulseColor );
		BroadcastHealPulseRing( origin, range );

		GameLog.Add( $"You cast Heal Pulse! ({healAmount} HP restored to {targets} {( targets == 1 ? "ally" : "allies" )}, {manaLeft} mana left)", "#7ad29e" );
	}

	[Rpc.Broadcast]
	void HealPlayer( GameObject playerObj, int amount )
	{
		if ( playerObj == null )
			return;

		var health = playerObj.Components.Get<PlayerHealth>();
		if ( health == null )
			return;

		health.Heal( amount );
	}

	[Rpc.Broadcast]
	void BroadcastHealPulseRing( Vector3 origin, float range )
	{
		if ( !IsProxy )
			return;

		HealPulseRing.Spawn( Scene, origin, range, HealPulseColor );
	}

	void SpawnProjectile( SpellDefinition spell )
	{
		if ( AimSource == null )
			return;

		var prefab = GetPrefabForSpell( spell.Id );
		if ( prefab == null )
			return;

		var inventory = GameObject.Components.Get<Inventory>();
		var skills = GameObject.Components.Get<Skills>();
		if ( inventory == null || skills == null )
			return;

		var weaponDef = inventory.GetEquippedWeaponDef();
		float staffPower = weaponDef != null ? weaponDef.WeaponPower : 1f;
		float skillBonus = skills.GetCombatPower( SkillType.Magic );

		float buffMult = 1f;
		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null )
			buffMult = potionSystem.GetBuffMultiplier( BuffType.Magic );

		float sicknessMult = 1f;
		var mana = GameObject.Components.Get<ManaSystem>();
		if ( mana != null )
			sicknessMult = mana.GetManaDamageMultiplier();

		float totalPower = staffPower * spell.DamageMultiplier * skillBonus * buffMult * sicknessMult;
		int damage = (int)totalPower;
		if ( damage < 1 ) damage = 1;

		bool isCrit = CombatConstants.RollCrit();
		if ( isCrit )
			damage = (int)( damage * CombatConstants.CritMultiplier );

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
		GameObject homingTarget = null;

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

			if ( spell.Type == SpellType.Homing )
				homingTarget = FindHomingTarget( camPos, camForward );
		}

		var projectile = prefab.Clone( spawnPos );
		if ( projectile == null )
			return;

		float yaw = MathF.Atan2( launchDir.y, launchDir.x ) * ( 180f / MathF.PI );
		float pitch = MathF.Asin( -launchDir.z ) * ( 180f / MathF.PI );
		projectile.WorldRotation = Rotation.From( pitch, yaw, 0f );

		projectile.NetworkSpawn();

		var spellProj = projectile.Components.Get<SpellProjectile>();
		if ( spellProj != null )
		{
			spellProj.Velocity = launchDir * spell.ProjectileSpeed;
			spellProj.Damage = damage;
			spellProj.Shooter = GameObject;
			spellProj.SpellId = spell.Id;
			spellProj.MaxRange = spell.MaxRange;
			spellProj.MaxLifetime = spell.MaxLifetime;
			spellProj.TraceRadius = spell.TraceRadius;
			spellProj.FreezeDuration = spell.FreezeDuration;
			spellProj.SlowDuration = spell.SlowDuration;
			spellProj.SlowMultiplier = spell.SlowMultiplier;
			spellProj.FrozenBonusDamage = spell.FrozenBonusDamage > 0f ? spell.FrozenBonusDamage : 1f;
			spellProj.IsCrit = isCrit;
		}

		if ( spell.Type == SpellType.Homing && homingTarget != null )
		{
			var homing = projectile.Components.Get<HomingProjectile>();
			if ( homing == null )
				homing = projectile.Components.Create<HomingProjectile>();
			homing.Target = homingTarget;
			homing.TurnSpeed = HomingTurnSpeed;
		}

		skills.AddXp( SkillType.Magic, 2 );

		switch ( spell.Id )
		{
			case SpellId.Fireball: SoundLibrary.PlayFireball( spawnPos ); break;
			case SpellId.IceShard: SoundLibrary.PlayIceShard( spawnPos ); break;
			case SpellId.DarkBlast: SoundLibrary.PlayDarkBlast( spawnPos ); break;
			case SpellId.MagicMissile: SoundLibrary.PlayMagicMissile( spawnPos ); break;
		}

		int manaLeft = 0;
		var manaCheck = GameObject.Components.Get<ManaSystem>();
		if ( manaCheck != null )
			manaLeft = manaCheck.CurrentMana;

		GameLog.Add( $"You cast {spell.Name}! ({damage} power{( isCrit ? ", CRIT!" : "" )}, {manaLeft} mana left)", "#7a5aaa" );
	}

	GameObject FindHomingTarget( Vector3 originPos, Vector3 aimDir )
	{
		float cosThreshold = MathF.Cos( HomingAcquireConeDegrees * MathF.PI / 180f );
		float bestDot = cosThreshold;
		GameObject best = null;

		foreach ( var monster in Scene.GetAllComponents<Monster>() )
		{
			if ( monster == null || !monster.IsValid() || monster.IsDead )
				continue;

			Vector3 toMonster = monster.WorldPosition - originPos;
			float dist = toMonster.Length;
			if ( dist > HomingMaxAcquireRange || dist < 1f )
				continue;

			Vector3 toMonsterDir = toMonster / dist;
			float dot = aimDir.Dot( toMonsterDir );
			if ( dot > bestDot )
			{
				bestDot = dot;
				best = monster.GameObject;
			}
		}

		foreach ( var boss in Scene.GetAllComponents<Boss>() )
		{
			if ( boss == null || !boss.IsValid() || boss.IsDead )
				continue;

			Vector3 toBoss = boss.WorldPosition - originPos;
			float dist = toBoss.Length;
			if ( dist > HomingMaxAcquireRange || dist < 1f )
				continue;

			Vector3 toBossDir = toBoss / dist;
			float dot = aimDir.Dot( toBossDir );
			if ( dot > bestDot )
			{
				bestDot = dot;
				best = boss.GameObject;
			}
		}

		return best;
	}

	void SpawnBarrier( SpellDefinition spell )
	{
		if ( AimSource == null )
			return;

		var aimForward = AimSource.WorldRotation.Forward;
		var aimRight = AimSource.WorldRotation.Right;
		var flatForward = new Vector3( aimForward.x, aimForward.y, 0f ).Normal;

		var spawnPos = GameObject.WorldPosition
			+ flatForward * BarrierForwardOffset
			+ Vector3.Up * BarrierHeightOffset
			+ aimRight * BarrierLateralOffset;

		var barrierRotation = Rotation.LookAt( flatForward, Vector3.Up ) * Rotation.FromYaw( BarrierYawOffset );

		CreateBarrierLocal( spawnPos, barrierRotation );

		var barrierSpell = SpellDatabase.Get( SpellId.ArcaneBarrier );
		float pushWidth = barrierSpell != null ? barrierSpell.BarrierWidth : BarrierWidth;
		PushOverlapping( spawnPos, flatForward, pushWidth * 25f, 30f );
		BroadcastBarrier( spawnPos, barrierRotation );

		var mana = GameObject.Components.Get<ManaSystem>();
		if ( mana != null )
			mana.MarkCombat();

		int manaLeft = 0;
		if ( mana != null )
			manaLeft = mana.CurrentMana;

		GameLog.Add( $"You conjure an Arcane Barrier! ({manaLeft} mana left)", "#7a5aaa" );
	}

	void CreateBarrierLocal( Vector3 pos, Rotation rot )
	{
		var spell = SpellDatabase.Get( SpellId.ArcaneBarrier );
		float barrierWidth = spell != null ? spell.BarrierWidth : BarrierWidth;
		float barrierHeight = spell != null ? spell.BarrierHeight : BarrierHeight;
		float barrierDepth = spell != null ? spell.BarrierDepth : BarrierDepth;
		float barrierDuration = spell != null ? spell.BarrierDuration : BarrierDuration;

		if ( BarrierPrefab != null )
		{
			var barrier = BarrierPrefab.Clone( pos );
			barrier.WorldRotation = rot;
			barrier.Tags.Add( "solid" );

			var barrierComp = barrier.Components.Get<ArcaneBarrier>();
			if ( barrierComp == null )
				barrierComp = barrier.Components.Create<ArcaneBarrier>();
			barrierComp.Duration = barrierDuration;

			var renderer = barrier.Components.Get<ModelRenderer>();
			if ( renderer != null )
				renderer.Tint = BarrierTint;

			return;
		}

		var fallback = new GameObject( true, "ArcaneBarrier" );
		fallback.WorldPosition = pos;
		fallback.WorldRotation = rot;
		fallback.WorldScale = new Vector3( barrierWidth, barrierDepth, barrierHeight );
		fallback.Tags.Add( "solid" );

		var fbBarrier = fallback.Components.Create<ArcaneBarrier>();
		fbBarrier.Duration = barrierDuration;

		var fbCollider = fallback.Components.Create<BoxCollider>();
		fbCollider.Scale = new Vector3( 50f, 50f, 50f );
		fbCollider.Static = true;

		var fbRenderer = fallback.Components.Create<ModelRenderer>();
		fbRenderer.Model = Model.Load( "models/dev/box.vmdl" );
		fbRenderer.Tint = BarrierTint;
	}

	[Rpc.Broadcast]
	void BroadcastBarrier( Vector3 pos, Rotation rot )
	{
		if ( !IsProxy )
			return;

		CreateBarrierLocal( pos, rot );
	}

	void PushOverlapping( Vector3 barrierPos, Vector3 barrierForward, float halfWidth, float pushDist )
	{
		var monsters = Scene.GetAllComponents<Monster>();

		foreach ( var monster in monsters )
		{
			if ( monster.IsDead )
				continue;

			Vector3 toMonster = monster.WorldPosition - barrierPos;
			Vector3 flatToMonster = new Vector3( toMonster.x, toMonster.y, 0f );

			float forwardDot = flatToMonster.Dot( barrierForward );
			Vector3 barrierRight = new Vector3( -barrierForward.y, barrierForward.x, 0f );
			float sidewaysDot = flatToMonster.Dot( barrierRight );

			if ( MathF.Abs( forwardDot ) > pushDist || MathF.Abs( sidewaysDot ) > halfWidth )
				continue;

			float pushDirection = forwardDot >= 0f ? 1f : -1f;
			Vector3 pushTarget = barrierPos + barrierForward * ( pushDist + 20f ) * pushDirection;
			pushTarget = new Vector3( pushTarget.x, pushTarget.y, monster.WorldPosition.z );

			monster.GameObject.WorldPosition = pushTarget;
		}
	}

	GameObject SpawnLobbedReticle( float radius, Color color )
	{
		var go = Scene.CreateObject();
		go.Name = "LobbedAimReticle";

		int segments = 32;
		float thickness = 4f;
		float boxUnit = 50f;
		float angleStep = MathF.PI * 2f / segments;

		for ( int i = 0; i < segments; i++ )
		{
			float a = i * angleStep;
			float b = ( i + 1 ) * angleStep;

			Vector3 p1 = new Vector3( MathF.Cos( a ) * radius, MathF.Sin( a ) * radius, 2f );
			Vector3 p2 = new Vector3( MathF.Cos( b ) * radius, MathF.Sin( b ) * radius, 2f );

			var segGo = new GameObject( true, $"ReticleSeg{i}" );
			segGo.SetParent( go );

			Vector3 mid = ( p1 + p2 ) * 0.5f;
			segGo.LocalPosition = mid;

			Vector3 diff = p2 - p1;
			float length = diff.Length;
			if ( length < 0.01f ) continue;

			Vector3 dir = diff / length;
			segGo.LocalRotation = Rotation.LookAt( dir );
			segGo.LocalScale = new Vector3( length / boxUnit, thickness / boxUnit, thickness / boxUnit );

			var seg = segGo.Components.Create<ModelRenderer>();
			seg.Model = Model.Load( "models/dev/box.vmdl" );
			seg.Tint = color;
		}

		return go;
	}

	Vector3 GetLobbedSpawnPos()
	{
		bool tp = IsThirdPerson();
		float forwardOff = tp ? TpForwardOffset : FpForwardOffset;
		float heightOff = tp ? TpHeightOffset : FpHeightOffset;
		float lateralOff = tp ? TpLateralOffset : FpLateralOffset;

		var aimForward = AimSource != null ? AimSource.WorldRotation.Forward : GameObject.WorldRotation.Forward;
		var aimRight = AimSource != null ? AimSource.WorldRotation.Right : GameObject.WorldRotation.Right;

		return GameObject.WorldPosition
			+ Vector3.Up * heightOff
			+ aimForward * forwardOff
			+ aimRight * lateralOff;
	}

	Vector3 GetCursorWorldPoint( float maxDistance )
	{
		var camera = Scene.Camera;
		if ( camera == null )
			return GameObject.WorldPosition + GameObject.WorldRotation.Forward * 500f;

		Vector3 camPos = camera.WorldPosition;
		Vector3 camForward = camera.WorldRotation.Forward;
		Vector3 camEnd = camPos + camForward * maxDistance;

		var aimTrace = Scene.Trace
			.Ray( camPos, camEnd )
			.UseHitboxes( true )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "monster", "boss", "player", "pickup" )
			.Run();

		return aimTrace.Hit ? aimTrace.HitPosition : camEnd;
	}

	Vector3 PredictLobbedLanding( SpellDefinition spell )
	{
		Vector3 spawnPos = GetLobbedSpawnPos();
		Vector3 cursorPoint = GetCursorWorldPoint( spell.MaxRange );

		Vector3 launchVel = ComputeLobbedVelocity( spawnPos, cursorPoint, spell.ProjectileSpeed, AcidGravity, spell.MaxRange );

		Vector3 pos = spawnPos;
		Vector3 vel = launchVel;
		float dt = 0.03f;
		float maxTime = 5f;
		float elapsed = 0f;

		while ( elapsed < maxTime )
		{
			Vector3 nextPos = pos + vel * dt;
			vel = new Vector3( vel.x, vel.y, vel.z - AcidGravity * dt );

			var trace = Scene.Trace
				.Ray( pos, nextPos )
				.IgnoreGameObjectHierarchy( GameObject )
				.WithoutTags( "monster", "boss", "player", "pickup" )
				.Run();

			if ( trace.Hit )
				return trace.HitPosition;

			pos = nextPos;
			elapsed += dt;
		}

		return pos;
	}

	Vector3 ComputeLobbedVelocity( Vector3 from, Vector3 to, float speed, float gravity, float maxRange )
	{
		Vector3 flat = new Vector3( to.x - from.x, to.y - from.y, 0f );
		float flatDist = flat.Length;
		float verticalDelta = to.z - from.z;

		float clampedFlat = MathF.Min( flatDist, maxRange );
		Vector3 flatDir = flatDist > 0.01f ? flat / flatDist : new Vector3( 1f, 0f, 0f );

		float speedSqr = speed * speed;
		float discriminant = speedSqr * speedSqr - gravity * ( gravity * clampedFlat * clampedFlat + 2f * verticalDelta * speedSqr );

		float vx, vz;

		if ( discriminant < 0f )
		{
			float pitchRad = AcidLaunchPitchDegrees * MathF.PI / 180f;
			vx = speed * MathF.Cos( pitchRad );
			vz = speed * MathF.Sin( pitchRad );
		}
		else
		{
			float root = MathF.Sqrt( discriminant );
			float lowAngle = MathF.Atan( ( speedSqr - root ) / ( gravity * clampedFlat ) );
			float pitchRad = lowAngle;
			vx = speed * MathF.Cos( pitchRad );
			vz = speed * MathF.Sin( pitchRad );
		}

		return flatDir * vx + Vector3.Up * vz;
	}

	void SpawnLobbedProjectile( SpellDefinition spell )
	{
		var prefab = GetPrefabForSpell( spell.Id );
		if ( prefab == null )
			return;

		var inventory = GameObject.Components.Get<Inventory>();
		var skills = GameObject.Components.Get<Skills>();
		if ( inventory == null || skills == null )
			return;

		Vector3 spawnPos = GetLobbedSpawnPos();
		Vector3 cursorPoint = GetCursorWorldPoint( spell.MaxRange );
		Vector3 launchVel = ComputeLobbedVelocity( spawnPos, cursorPoint, spell.ProjectileSpeed, AcidGravity, spell.MaxRange );

		var projectile = prefab.Clone( spawnPos );
		if ( projectile == null )
			return;

		Vector3 dir = launchVel.Normal;
		float yaw = MathF.Atan2( dir.y, dir.x ) * ( 180f / MathF.PI );
		float pitch = MathF.Asin( -dir.z ) * ( 180f / MathF.PI );
		projectile.WorldRotation = Rotation.From( pitch, yaw, 0f );

		projectile.NetworkSpawn();

		var acid = projectile.Components.Get<AcidSpitProjectile>();
		if ( acid == null )
			acid = projectile.Components.Create<AcidSpitProjectile>();

		acid.Velocity = launchVel;
		acid.Shooter = GameObject;
		acid.Gravity = AcidGravity;
		acid.MaxLifetime = spell.MaxLifetime;
		acid.TraceRadius = spell.TraceRadius;
		acid.SplashRadius = spell.SplashRadius;
		acid.SplashVisualDuration = spell.SplashVisualDuration;
		acid.PoisonDamagePerTick = spell.PoisonDamagePerTick;
		acid.PoisonTickInterval = spell.PoisonTickInterval;
		acid.PoisonDuration = spell.PoisonDuration;
		acid.SplashColor = AcidPoolColor;

		skills.AddXp( SkillType.Magic, 2 );

		var manaCheck = GameObject.Components.Get<ManaSystem>();
		int manaLeft = manaCheck != null ? manaCheck.CurrentMana : 0;

		GameLog.Add( $"You cast {spell.Name}! ({manaLeft} mana left)", "#7a5aaa" );
	}

	Vector3 GetCursorGroundPoint( float maxDistance )
	{
		Vector3 cursorPoint = GetCursorWorldPoint( maxDistance );
		Vector3 origin = GameObject.WorldPosition;

		Vector3 flat = new Vector3( cursorPoint.x - origin.x, cursorPoint.y - origin.y, 0f );
		float flatDist = flat.Length;
		if ( flatDist > maxDistance )
		{
			Vector3 capped = origin + flat.Normal * maxDistance;
			cursorPoint = new Vector3( capped.x, capped.y, cursorPoint.z );
		}

		Vector3 traceStart = new Vector3( cursorPoint.x, cursorPoint.y, cursorPoint.z + 500f );
		Vector3 traceEnd = new Vector3( cursorPoint.x, cursorPoint.y, cursorPoint.z - 500f );

		var trace = Scene.Trace
			.Ray( traceStart, traceEnd )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "monster", "boss", "player", "pickup" )
			.Run();

		return trace.Hit ? trace.HitPosition : cursorPoint;
	}

	float GetGroundEffectRadiusForSpell( SpellId id )
	{
		var def = SpellDatabase.Get( id );
		if ( def == null )
			return 100f;

		switch ( id )
		{
			case SpellId.Inferno: return def.AoeRadius;
			case SpellId.Singularity: return def.PullRadius;
			default: return 100f;
		}
	}

	Color GetGroundEffectReticleColorForSpell( SpellId id )
	{
		switch ( id )
		{
			case SpellId.Inferno: return InfernoReticleColor;
			case SpellId.Singularity: return SingularityReticleColor;
			default: return new Color( 1f, 1f, 1f, 1f );
		}
	}

	void SpawnGroundEffect( SpellDefinition spell )
	{
		Vector3 ground = GetCursorGroundPoint( spell.MaxRange );

		var skills = GameObject.Components.Get<Skills>();
		if ( skills != null )
			skills.AddXp( SkillType.Magic, 2 );

		switch ( spell.Id )
		{
			case SpellId.Inferno:
				FireTornado.Spawn( Scene, ground, GameObject, spell.AoeRadius, spell.AoeHeight, spell.AoeDuration, spell.AoeDamagePerTick, spell.AoeTickInterval, false );
				BroadcastFireTornado( ground, spell.AoeRadius, spell.AoeHeight, spell.AoeDuration );
				break;

			case SpellId.Singularity:
				int damage = ComputeSpellDamage( spell );
				Singularity.Spawn( Scene, ground, GameObject, spell.PullRadius, spell.CollapseRadius, spell.PullDuration, damage, false );
				BroadcastSingularity( ground, spell.PullRadius, spell.CollapseRadius, spell.PullDuration );
				break;
		}

		var manaCheck = GameObject.Components.Get<ManaSystem>();
		int manaLeft = manaCheck != null ? manaCheck.CurrentMana : 0;

		GameLog.Add( $"You cast {spell.Name}! ({manaLeft} mana left)", "#7a5aaa" );
	}

	[Rpc.Broadcast]
	void BroadcastFireTornado( Vector3 position, float radius, float height, float duration )
	{
		if ( !IsProxy )
			return;

		FireTornado.Spawn( Scene, position, GameObject, radius, height, duration, 0f, 1f, true );
	}

	[Rpc.Broadcast]
	void BroadcastSingularity( Vector3 position, float pullRadius, float collapseRadius, float pullDuration )
	{
		if ( !IsProxy )
			return;

		Singularity.Spawn( Scene, position, GameObject, pullRadius, collapseRadius, pullDuration, 0, true );
	}

	int ComputeSpellDamage( SpellDefinition spell )
	{
		var inventory = GameObject.Components.Get<Inventory>();
		var skills = GameObject.Components.Get<Skills>();
		if ( inventory == null || skills == null )
			return 1;

		var weaponDef = inventory.GetEquippedWeaponDef();
		float staffPower = weaponDef != null ? weaponDef.WeaponPower : 1f;
		float skillBonus = skills.GetCombatPower( SkillType.Magic );

		float buffMult = 1f;
		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null )
			buffMult = potionSystem.GetBuffMultiplier( BuffType.Magic );

		float sicknessMult = 1f;
		var mana = GameObject.Components.Get<ManaSystem>();
		if ( mana != null )
			sicknessMult = mana.GetManaDamageMultiplier();

		float totalPower = staffPower * spell.DamageMultiplier * skillBonus * buffMult * sicknessMult;
		int damage = (int)totalPower;
		if ( damage < 1 ) damage = 1;
		return damage;
	}
}