using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class Boss : Component
{
	public enum BossAttackType
	{
		Downward,
		ThreeSixtyLow,
		Combo,
		Kick,
		Horizontal
	}

	public class AttackHitFrame
	{
		[Property] public int Frame { get; set; } = 18;
		[Property] public Vector3 HitboxLocalOffset { get; set; } = new Vector3( 100f, 0f, 50f );
		[Property] public Vector3 HitboxSize { get; set; } = new Vector3( 160f, 160f, 100f );
	}

	public class BossAttackDefinition
	{
		[Property] public BossAttackType Type { get; set; } = BossAttackType.Downward;
		[Property] public string AnimParam { get; set; } = "b_attack_downward";

		[Property, Group( "Timing" )] public int AnimLengthFrames { get; set; } = 42;
		[Property, Group( "Timing" )] public float Cooldown { get; set; } = 3f;

		[Property, Group( "Damage" )] public float DamageMultiplier { get; set; } = 1f;

		[Property, Group( "Hits" )] public List<AttackHitFrame> Hits { get; set; } = new();

		[Property, Group( "Knockback" )] public float KnockbackForce { get; set; } = 0f;
		[Property, Group( "Knockback" )] public float KnockbackStunDuration { get; set; } = 0.4f;
		[Property, Group( "Knockback" )] public float KnockbackTotalDuration { get; set; } = 0.8f;

		[Property, Group( "Debug" )] public bool ShowGizmo { get; set; } = false;
	}

	enum BossState
	{
		Idle,
		Patrolling,
		Chasing,
		Battlecry,
		Attacking,
		Returning,
		Dead
	}

	class ActiveBeam
	{
		public Vector3 Position;
		public Vector3 Direction;
		public float DistanceTraveled;
		public float Lifetime;
		public HashSet<GameObject> AlreadyHit = new();
		public int Damage;
	}

	class ActiveBeamVisual
	{
		public GameObject VisualObject;
		public Vector3 Position;
		public Vector3 Direction;
		public float DistanceTraveled;
		public float Lifetime;
	}

	const int AnimFrameRate = 30;

	[Property, Group( "Identity" )] public string BossName { get; set; } = "Boss";
	[Property, Group( "Identity" )] public CombatStyle CombatStyle { get; set; } = CombatStyle.Melee;

	[Property, Group( "Stats" )] public int MaxHealth { get; set; } = 5000;
	[Property, Group( "Stats" )] public int BaseDamage { get; set; } = 25;
	[Property, Group( "Stats" )] public float ArmorValue { get; set; } = 40f;
	[Property, Group( "Stats" )] public int DefenceLevel { get; set; } = 60;
	[Property, Group( "Stats" )] public float LeashHealPercentPerSecond { get; set; } = 10f;

	[Property, Group( "Ranges" )] public float AggroRange { get; set; } = 800f;
	[Property, Group( "Ranges" )] public float DeaggroRange { get; set; } = 1600f;
	[Property, Group( "Ranges" )] public float MeleeRange { get; set; } = 150f;
	[Property, Group( "Ranges" )] public float KickRange { get; set; } = 60f;
	[Property, Group( "Ranges" )] public float BeamMinRange { get; set; } = 250f;

	[Property, Group( "Movement" )] public float WalkSpeed { get; set; } = 80f;
	[Property, Group( "Movement" )] public float RunSpeed { get; set; } = 180f;
	[Property, Group( "Movement" )] public float TurnSpeedDegrees { get; set; } = 360f;
	[Property, Group( "Movement" )] public float MoveStartDelay { get; set; } = 0.3f;

	[Property, Group( "Target Switching" )] public float TargetSwitchMinInterval { get; set; } = 15f;
	[Property, Group( "Target Switching" )] public float TargetSwitchMaxInterval { get; set; } = 30f;

	[Property, Group( "Attack Weights" )] public int DownwardWeight { get; set; } = 70;
	[Property, Group( "Attack Weights" )] public int ThreeSixtyWeight { get; set; } = 20;
	[Property, Group( "Attack Weights" )] public int ComboWeight { get; set; } = 10;
	[Property, Group( "Attack Weights" )] public float GlobalAttackDelay { get; set; } = 2.5f;
	[Property, Group( "Attack Weights" )] public float PostAttackBuffer { get; set; } = 0.5f;

	[Property, Group( "Attacks" )] public List<BossAttackDefinition> Attacks { get; set; } = new()
	{
		new BossAttackDefinition
		{
			Type = BossAttackType.Downward,
			AnimParam = "b_attack_downward",
			AnimLengthFrames = 50,
			Cooldown = 3f,
			DamageMultiplier = 1f,
			KnockbackForce = 0f,
			Hits = new List<AttackHitFrame>
			{
				new AttackHitFrame { Frame = 26 }
			}
		},
		new BossAttackDefinition
		{
			Type = BossAttackType.ThreeSixtyLow,
			AnimParam = "b_attack_360",
			AnimLengthFrames = 68,
			Cooldown = 5f,
			DamageMultiplier = 1.1f,
			KnockbackForce = 0f,
			Hits = new List<AttackHitFrame>
			{
				new AttackHitFrame { Frame = 29 }
			}
		},
		new BossAttackDefinition
		{
			Type = BossAttackType.Combo,
			AnimParam = "b_attack_combo",
			AnimLengthFrames = 100,
			Cooldown = 8f,
			DamageMultiplier = 0.75f,
			KnockbackForce = 0f,
			Hits = new List<AttackHitFrame>
			{
				new AttackHitFrame { Frame = 29 },
				new AttackHitFrame { Frame = 49 },
				new AttackHitFrame { Frame = 80 }
			}
		},
		new BossAttackDefinition
		{
			Type = BossAttackType.Kick,
			AnimParam = "b_attack_kick",
			AnimLengthFrames = 44,
			Cooldown = 6f,
			DamageMultiplier = 0.6f,
			KnockbackForce = 400f,
			KnockbackStunDuration = 0.4f,
			KnockbackTotalDuration = 0.8f,
			Hits = new List<AttackHitFrame>
			{
				new AttackHitFrame { Frame = 18 }
			}
		},
		new BossAttackDefinition
		{
			Type = BossAttackType.Horizontal,
			AnimParam = "b_attack_horizontal",
			AnimLengthFrames = 56,
			Cooldown = 8f,
			DamageMultiplier = 1f,
			KnockbackForce = 0f,
			Hits = new List<AttackHitFrame>
			{
				new AttackHitFrame { Frame = 28 }
			}
		}
	};

	[Property, Group( "Battlecry" )] public string BattlecryParam { get; set; } = "b_battlecry";
	[Property, Group( "Battlecry" )] public float BattlecryDuration { get; set; } = 3f;

	[Property, Group( "Pillars" )] public List<BossPillar> Pillars { get; set; } = new();
	[Property, Group( "Pillars" )] public float PillarHealInterval { get; set; } = 5f;
	[Property, Group( "Pillars" )] public float PillarHealPercent { get; set; } = 10f;
	[Property, Group( "Pillars" )] public Color ProtectedTint { get; set; } = new Color( 0.4f, 1f, 0.4f );

	[Property, Group( "Beam" )] public float BeamSpeed { get; set; } = 1200f;
	[Property, Group( "Beam" )] public float BeamRange { get; set; } = 800f;
	[Property, Group( "Beam" )] public float BeamRadius { get; set; } = 40f;
	[Property, Group( "Beam" )] public float BeamVerticalHitTolerance { get; set; } = 80f;
	[Property, Group( "Beam" )] public float BeamVisualLength { get; set; } = 200f;
	[Property, Group( "Beam" )] public Color BeamColor { get; set; } = new Color( 1f, 0.3f, 0.8f );
	[Property, Group( "Beam" )] public float BeamDamageMultiplier { get; set; } = 1.5f;
	[Property, Group( "Beam" )] public string BeamSpawnBone { get; set; } = "RightHand";
	[Property, Group( "Beam" )] public float BeamSpawnForwardOffset { get; set; } = 40f;
	[Property, Group( "Beam" )] public float BeamSpawnVerticalOffset { get; set; } = -30f;
	[Property, Group( "Beam" )] public float BeamPlayerCenterHeight { get; set; } = 40f;

	[Property, Group( "Weapon" )] public GameObject WeaponPrefab { get; set; }
	[Property, Group( "Weapon" )] public string WeaponBone { get; set; } = "RightHand";
	[Property, Group( "Weapon" )] public Vector3 WeaponLocalOffset { get; set; } = Vector3.Zero;
	[Property, Group( "Weapon" )] public Angles WeaponLocalRotation { get; set; } = Angles.Zero;

	GameObject _weaponInstance;

	[Property, Group( "Respawn" )] public float RespawnDelayMin { get; set; } = 300f;
	[Property, Group( "Respawn" )] public float RespawnDelayMax { get; set; } = 600f;
	[Property, Group( "Respawn" )] public Vector3 RespawnOffset { get; set; } = Vector3.Zero;
	[Property, Group( "Respawn" )] public float DeathAnimDuration { get; set; } = 3.9f;
	[Property, Group( "Respawn" )] public float DeathHoldDuration { get; set; } = 3f;

	[Property, Group( "Victory" )] public string VictoryParam { get; set; } = "b_victory";
	[Property, Group( "Victory" )] public float VictoryDuration { get; set; } = 3f;

	[Property, Group( "Loot" )] public LootTable LootTable { get; set; }
	[Property, Group( "Loot" ), Range( 0f, 1f )] public float GroupLootRetention { get; set; } = 0.5f;

	[Property, Group( "References" )] public ModelRenderer ModelRenderer { get; set; }
	[Property, Group( "References" )] public SkinnedModelRenderer SkinnedRenderer { get; set; }
	[Property, Group( "References" )] public Collider BossCollider { get; set; }

	[Property, Group( "Debug" )] public bool ShowHitboxDebug { get; set; } = false;
	[Property, Group( "Debug" )] public float DebugHitboxFlashDuration { get; set; } = 0.3f;

	[Sync] public int CurrentHealth { get; set; }
	[Sync] public bool IsDead { get; set; }
	[Sync] public GameObject PrimaryTarget { get; set; }

	BossState _state = BossState.Idle;
	Vector3 _spawnPosition;
	Rotation _spawnRotation;
	Color _baseRendererTint;
	bool _baseRendererTintCaptured;

	float _globalAttackTimer;
	float _animationLockRemaining;
	float _targetSwitchTimer;
	float _pillarHealTimer;
	float _battlecryRemaining;
	float _respawnTimer;
	bool _respawnRequested;
	float _leashHealAccum;

	Dictionary<BossAttackType, float> _attackCooldowns = new();
	BossAttackDefinition _currentAttack;
	int _currentAttackHitsFired;
	float _currentAttackTime;
	bool _forceBeamNext;
	float _moveStartTimer;
	bool _wasMovingLastFrame;
	float _deathAnimTimer;
	bool _deathAnimFinished;
	int _deathGeneration;

	HashSet<ulong> _contributorSteamIds = new();

	List<ActiveBeam> _activeBeams = new();
	List<ActiveBeamVisual> _activeBeamVisuals = new();

	struct DebugHitboxDraw
	{
		public Vector3 LocalOffset;
		public Vector3 Size;
		public float TimeRemaining;
	}
	List<DebugHitboxDraw> _debugHitboxes = new();

	protected override void OnStart()
	{
		_spawnPosition = WorldPosition;
		_spawnRotation = WorldRotation;
		CurrentHealth = MaxHealth;

		if ( ModelRenderer != null )
		{
			_baseRendererTint = ModelRenderer.Tint;
			_baseRendererTintCaptured = true;
		}

		_targetSwitchTimer = RollTargetSwitchInterval();

		SpawnWeapon();
	}

	void SpawnWeapon()
	{
		if ( WeaponPrefab == null )
			return;

		_weaponInstance = WeaponPrefab.Clone();
		_weaponInstance.SetParent( GameObject );
		_weaponInstance.Name = "BossWeapon";
	}

	protected override void OnUpdate()
	{
		UpdatePillarTint();
		UpdateDebugHitboxes();
		UpdateActiveBeams();

		if ( !Networking.IsHost )
			return;

		if ( IsDead )
		{
			UpdateRespawn();
			return;
		}

		if ( _state != BossState.Battlecry && _state != BossState.Attacking && _state != BossState.Dead )
			UpdatePrimaryTarget();

		UpdatePillarHealing();

		if ( _globalAttackTimer > 0f )
			_globalAttackTimer -= Time.Delta;

		foreach ( var key in _attackCooldowns.Keys.ToList() )
			_attackCooldowns[key] = MathF.Max( 0f, _attackCooldowns[key] - Time.Delta );

		switch ( _state )
		{
			case BossState.Idle:
				UpdateIdle();
				break;
			case BossState.Patrolling:
				UpdatePatrolling();
				break;
			case BossState.Battlecry:
				UpdateBattlecry();
				break;
			case BossState.Chasing:
				UpdateChasing();
				break;
			case BossState.Attacking:
				UpdateAttacking();
				break;
			case BossState.Returning:
				UpdateReturning();
				break;
		}
	}

	void UpdateIdle()
	{
		SetMoving( false, false );
		_leashHealAccum = 0f;

		var player = FindNearestPlayerInAggroRange();
		if ( player == null )
			return;

		PrimaryTarget = player;
		StartBattlecry();
	}

	void UpdatePatrolling()
	{
		SetMoving( false, false );
		_state = BossState.Idle;
	}

	void StartBattlecry()
	{
		_state = BossState.Battlecry;
		_battlecryRemaining = BattlecryDuration;
		BroadcastAnimBool( BattlecryParam, true );
		SetMoving( false, false );
		SoundLibrary.PlayBossRoar( WorldPosition );
	}

	void UpdateBattlecry()
	{
		SetMoving( false, false );

		if ( PrimaryTarget != null && PrimaryTarget.IsValid() )
			FaceTarget( PrimaryTarget.WorldPosition );

		_battlecryRemaining -= Time.Delta;
		if ( _battlecryRemaining <= 0f )
		{
			BroadcastAnimBool( BattlecryParam, false );
			_state = BossState.Chasing;
		}
	}

	void UpdateChasing()
	{
		if ( !EnsureValidTarget() )
		{
			_state = BossState.Returning;
			return;
		}

		float distanceToSpawn = FlatDistance( WorldPosition, _spawnPosition );
		if ( distanceToSpawn > DeaggroRange )
		{
			_state = BossState.Returning;
			return;
		}

		var targetPos = PrimaryTarget.WorldPosition;
		float d = FlatDistance( WorldPosition, targetPos );

		if ( _globalAttackTimer <= 0f && TryStartAttack( d ) )
		{
			_wasMovingLastFrame = false;
			return;
		}

		if ( _globalAttackTimer > 0f )
		{
			SetMoving( false, false );
			FaceTarget( targetPos );
			_wasMovingLastFrame = false;
			return;
		}

		FaceTarget( targetPos );

		if ( d <= MeleeRange )
		{
			SetMoving( false, false );
			_wasMovingLastFrame = false;
			return;
		}

		if ( !_wasMovingLastFrame )
		{
			_moveStartTimer = MoveStartDelay;
			_wasMovingLastFrame = true;
		}

		SetMoving( true, true );

		if ( _moveStartTimer > 0f )
		{
			_moveStartTimer -= Time.Delta;
			return;
		}

		MoveTowards( targetPos, RunSpeed );
	}

	bool TryStartAttack( float distanceToTarget )
	{
		if ( _forceBeamNext && GetAttackDef( BossAttackType.Horizontal ) != null )
		{
			_forceBeamNext = false;
			return StartAttackByType( BossAttackType.Horizontal );
		}

		if ( distanceToTarget < KickRange && IsAttackReady( BossAttackType.Kick ) )
			return StartAttackByType( BossAttackType.Kick );

		if ( distanceToTarget > BeamMinRange && IsAttackReady( BossAttackType.Horizontal ) && HasLineOfSight() )
			return StartAttackByType( BossAttackType.Horizontal );

		if ( distanceToTarget <= MeleeRange )
		{
			var chosen = RollMeleeAttack();
			if ( IsAttackReady( chosen ) )
				return StartAttackByType( chosen );

			if ( chosen != BossAttackType.Downward && IsAttackReady( BossAttackType.Downward ) )
				return StartAttackByType( BossAttackType.Downward );
		}

		return false;
	}

	BossAttackType RollMeleeAttack()
	{
		int total = Math.Max( 1, DownwardWeight + ThreeSixtyWeight + ComboWeight );
		int roll = Game.Random.Int( 0, total - 1 );

		if ( roll < ComboWeight )
			return BossAttackType.Combo;

		if ( roll < ComboWeight + ThreeSixtyWeight )
			return BossAttackType.ThreeSixtyLow;

		return BossAttackType.Downward;
	}

	bool IsAttackReady( BossAttackType type )
	{
		if ( GetAttackDef( type ) == null )
			return false;

		if ( _attackCooldowns.TryGetValue( type, out var remaining ) && remaining > 0f )
			return false;

		return true;
	}

	BossAttackDefinition GetAttackDef( BossAttackType type )
	{
		foreach ( var a in Attacks )
		{
			if ( a.Type == type )
				return a;
		}
		return null;
	}

	bool StartAttackByType( BossAttackType type )
	{
		var def = GetAttackDef( type );
		if ( def == null )
			return false;

		_currentAttack = def;
		_currentAttackTime = 0f;
		_currentAttackHitsFired = 0;
		_animationLockRemaining = def.AnimLengthFrames / (float)AnimFrameRate;
		_state = BossState.Attacking;

		BroadcastAnimBool( "b_is_attacking", true );
		BroadcastAnimBool( def.AnimParam, true );
		SetMoving( false, false );

		_attackCooldowns[type] = def.Cooldown;
		_globalAttackTimer = GlobalAttackDelay;

		return true;
	}

	void UpdateAttacking()
	{
		if ( _currentAttack == null )
		{
			_state = BossState.Chasing;
			return;
		}

		if ( PrimaryTarget != null && PrimaryTarget.IsValid() )
			FaceTarget( PrimaryTarget.WorldPosition );

		_currentAttackTime += Time.Delta;
		_animationLockRemaining -= Time.Delta;

		while ( _currentAttackHitsFired < _currentAttack.Hits.Count )
		{
			var hit = _currentAttack.Hits[_currentAttackHitsFired];
			float hitTime = hit.Frame / (float)AnimFrameRate;

			if ( _currentAttackTime < hitTime )
				break;

			ResolveHit( _currentAttack, hit );
			_currentAttackHitsFired++;
		}

		if ( _animationLockRemaining <= -PostAttackBuffer )
		{
			BroadcastAnimBool( _currentAttack.AnimParam, false );
			BroadcastAnimBool( "b_is_attacking", false );

			if ( _currentAttack.Type == BossAttackType.Kick )
				_forceBeamNext = true;

			_currentAttack = null;
			_state = BossState.Chasing;
		}
	}

	void ResolveHit( BossAttackDefinition attack, AttackHitFrame hit )
	{
		if ( attack.Type == BossAttackType.Horizontal )
		{
			SpawnBeam( attack );
			return;
		}

		if ( attack.Type == BossAttackType.Kick )
		{
			SoundLibrary.PlayBossKick( WorldPosition );
		}
		else
		{
			SoundLibrary.PlaySwordBoss( WorldPosition );
		}

		var hitPlayers = ScanHitbox( hit );

		if ( PrimaryTarget != null && PrimaryTarget.IsValid() && !hitPlayers.Contains( PrimaryTarget ) )
			hitPlayers.Add( PrimaryTarget );

		foreach ( var playerObj in hitPlayers )
			ApplyDamageToPlayer( playerObj, attack );

		if ( attack.KnockbackForce > 0f )
		{
			foreach ( var playerObj in hitPlayers )
				ApplyKnockbackToPlayer( playerObj, attack );
		}

		if ( ShowHitboxDebug )
			_debugHitboxes.Add( new DebugHitboxDraw { LocalOffset = hit.HitboxLocalOffset, Size = hit.HitboxSize, TimeRemaining = DebugHitboxFlashDuration } );
	}

	List<GameObject> ScanHitbox( AttackHitFrame hit )
	{
		var hits = new List<GameObject>();

		var halfSize = hit.HitboxSize * 0.5f;
		var rot = WorldRotation;
		var origin = WorldPosition;
		var inverseRot = rot.Inverse;

		foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
		{
			if ( pc == null || !pc.IsValid() )
				continue;

			var playerObj = pc.GameObject;
			var health = playerObj.Components.Get<PlayerHealth>();
			if ( health == null || health.IsDead )
				continue;

			var localPos = inverseRot * ( playerObj.WorldPosition - origin );
			var delta = localPos - hit.HitboxLocalOffset;

			if ( MathF.Abs( delta.x ) <= halfSize.x && MathF.Abs( delta.y ) <= halfSize.y && MathF.Abs( delta.z ) <= halfSize.z )
				hits.Add( playerObj );
		}

		return hits;
	}

	void ApplyDamageToPlayer( GameObject playerObj, BossAttackDefinition attack )
	{
		if ( playerObj == null || !playerObj.IsValid() )
			return;

		var playerHealth = playerObj.Components.Get<PlayerHealth>();
		if ( playerHealth == null || playerHealth.IsDead )
			return;

		var playerInventory = playerObj.Components.Get<Inventory>();
		var playerSkills = playerObj.Components.Get<Skills>();
		var potionSystem = playerObj.Components.Get<PotionSystem>();

		var playerWeaponDef = playerInventory?.GetEquippedWeaponDef();
		CombatStyle playerStyle = CombatTriangle.GetStyleFromWeapon( playerWeaponDef );
		float triangleMult = CombatTriangle.GetDealMultiplier( CombatStyle, playerStyle );

		float armorValue = playerInventory != null ? CombatTriangle.GetEffectiveArmorValue( playerInventory ) : 0f;
		float armorReduction = CombatTriangle.GetArmorReduction( armorValue );

		float defenceMult = playerSkills != null ? playerSkills.GetDefenceMultiplier() : 1f;

		float defenceBuffMult = 1f;
		if ( potionSystem != null )
			defenceBuffMult = potionSystem.GetBuffMultiplier( BuffType.Defence );

		float attackMult = attack != null ? attack.DamageMultiplier : 1f;

		int finalDamage = (int)( BaseDamage * attackMult * triangleMult * ( 1f - armorReduction ) / defenceMult / defenceBuffMult );
		if ( finalDamage < 1 )
			finalDamage = 1;

		playerHealth.TakeDamage( finalDamage );
		DamagePopupBroadcaster.Broadcast( playerObj.WorldPosition + Vector3.Up * 60f, finalDamage, playerHealth.MaxHealth, false );
	}

	void ApplyKnockbackToPlayer( GameObject playerObj, BossAttackDefinition attack )
	{
		if ( playerObj == null || !playerObj.IsValid() )
			return;

		var receiver = playerObj.Components.Get<KnockbackReceiver>();
		if ( receiver == null )
			return;

		var direction = ( playerObj.WorldPosition - WorldPosition ).WithZ( 0f );
		if ( direction.LengthSquared < 0.0001f )
			direction = WorldRotation.Forward.WithZ( 0f );

		receiver.ApplyKnockback( direction.Normal, attack.KnockbackForce, attack.KnockbackStunDuration, attack.KnockbackTotalDuration );
	}

	void SpawnBeam( BossAttackDefinition attack )
	{
		Vector3 origin = GetBeamSpawnPosition();
		Vector3 dir = WorldRotation.Forward;

		if ( PrimaryTarget != null && PrimaryTarget.IsValid() )
		{
			var aim = ( PrimaryTarget.WorldPosition - origin ).WithZ( 0f );
			if ( aim.LengthSquared > 0.0001f )
				dir = aim.Normal;
		}

		int damage = (int)( BaseDamage * attack.DamageMultiplier * BeamDamageMultiplier );
		if ( damage < 1 )
			damage = 1;

		var beam = new ActiveBeam
		{
			Position = origin,
			Direction = dir,
			DistanceTraveled = 0f,
			Lifetime = BeamRange / MathF.Max( 1f, BeamSpeed ) + 0.1f,
			Damage = damage
		};

		_activeBeams.Add( beam );

		BroadcastSpawnBeamVisual( origin, dir );
	}

	[Rpc.Broadcast]
	void BroadcastSpawnBeamVisual( Vector3 origin, Vector3 direction )
	{
		_activeBeamVisuals.Add( new ActiveBeamVisual
		{
			VisualObject = CreateBeamVisual( origin, direction ),
			Position = origin,
			Direction = direction,
			DistanceTraveled = 0f,
			Lifetime = BeamRange / MathF.Max( 1f, BeamSpeed ) + 0.1f
		} );

		Sound.Play( "Sounds/MagicMissile.sound", origin );
	}

	Vector3 GetBeamSpawnPosition()
	{
		if ( SkinnedRenderer != null && SkinnedRenderer.SceneModel != null && !string.IsNullOrEmpty( BeamSpawnBone ) )
		{
			var boneTx = SkinnedRenderer.SceneModel.GetBoneWorldTransform( BeamSpawnBone );
			return boneTx.Position + WorldRotation.Forward * BeamSpawnForwardOffset + Vector3.Up * BeamSpawnVerticalOffset;
		}

		return WorldPosition + WorldRotation.Forward * ( BeamSpawnForwardOffset + 40f ) + Vector3.Up * ( 80f + BeamSpawnVerticalOffset );
	}

	GameObject CreateBeamVisual( Vector3 origin, Vector3 direction )
	{
		var go = new GameObject( true, "BossBeam" );
		go.WorldPosition = origin;
		go.WorldRotation = Rotation.LookAt( direction, Vector3.Up );

		var devBox = Model.Load( "models/dev/box.vmdl" );

		float coreLength = BeamVisualLength;
		float coreWidth = BeamRadius * 1.6f;
		float coreHeight = BeamRadius * 0.5f;

		var core = new GameObject( true, "Core" );
		core.SetParent( go );
		core.LocalPosition = Vector3.Zero;
		core.LocalRotation = Rotation.Identity;
		core.LocalScale = new Vector3( coreLength / 50f, coreWidth / 50f, coreHeight / 50f );
		var coreRenderer = core.Components.Create<ModelRenderer>();
		coreRenderer.Model = devBox;
		coreRenderer.Tint = BeamColor;

		var inner = new GameObject( true, "Inner" );
		inner.SetParent( go );
		inner.LocalPosition = Vector3.Zero;
		inner.LocalRotation = Rotation.Identity;
		inner.LocalScale = new Vector3( ( coreLength * 0.85f ) / 50f, ( coreWidth * 0.4f ) / 50f, ( coreHeight * 0.6f ) / 50f );
		var innerRenderer = inner.Components.Create<ModelRenderer>();
		innerRenderer.Model = devBox;
		innerRenderer.Tint = Color.Lerp( BeamColor, Color.White, 0.7f );

		var halo = new GameObject( true, "Halo" );
		halo.SetParent( go );
		halo.LocalPosition = Vector3.Zero;
		halo.LocalRotation = Rotation.Identity;
		halo.LocalScale = new Vector3( ( coreLength * 1.1f ) / 50f, ( coreWidth * 1.8f ) / 50f, ( coreHeight * 1.6f ) / 50f );
		var haloRenderer = halo.Components.Create<ModelRenderer>();
		haloRenderer.Model = devBox;
		haloRenderer.Tint = BeamColor.WithAlpha( 0.25f );

		return go;
	}

	void UpdateActiveBeams()
	{
		UpdateBeamVisuals();

		if ( !Networking.IsHost )
			return;

		if ( _activeBeams.Count == 0 )
			return;

		for ( int i = _activeBeams.Count - 1; i >= 0; i-- )
		{
			var beam = _activeBeams[i];

			float step = BeamSpeed * Time.Delta;
			var prevPos = beam.Position;
			var nextPos = beam.Position + beam.Direction * step;

			var wallTrace = Scene.Trace
				.Ray( prevPos, nextPos )
				.WithoutTags( "player", "boss", "monster", "pickup" )
				.Run();

			if ( wallTrace.Hit )
			{
				_activeBeams.RemoveAt( i );
				continue;
			}

			foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
			{
				if ( pc == null || !pc.IsValid() )
					continue;

				var playerObj = pc.GameObject;
				if ( beam.AlreadyHit.Contains( playerObj ) )
					continue;

				var health = playerObj.Components.Get<PlayerHealth>();
				if ( health == null || health.IsDead )
					continue;

				var playerCenter = playerObj.WorldPosition + Vector3.Up * BeamPlayerCenterHeight;

				var segDir = nextPos - prevPos;
				float segLen = segDir.Length;
				if ( segLen < 0.0001f )
					continue;

				var segNormal = segDir / segLen;
				var toPlayer = playerCenter - prevPos;
				float along = Vector3.Dot( toPlayer, segNormal );
				along = MathF.Max( 0f, MathF.Min( segLen, along ) );

				var closestPoint = prevPos + segNormal * along;
				var diff = playerCenter - closestPoint;

				float horiz = MathF.Sqrt( diff.x * diff.x + diff.y * diff.y );
				float vert = MathF.Abs( diff.z );

				if ( horiz > BeamRadius )
					continue;
				if ( vert > BeamVerticalHitTolerance )
					continue;

				ApplyBeamDamageToPlayer( playerObj, beam.Damage );
				beam.AlreadyHit.Add( playerObj );
			}

			beam.Position = nextPos;
			beam.DistanceTraveled += step;
			beam.Lifetime -= Time.Delta;

			if ( beam.DistanceTraveled >= BeamRange || beam.Lifetime <= 0f )
			{
				_activeBeams.RemoveAt( i );
			}
		}
	}

	void UpdateBeamVisuals()
	{
		if ( _activeBeamVisuals.Count == 0 )
			return;

		for ( int i = _activeBeamVisuals.Count - 1; i >= 0; i-- )
		{
			var visual = _activeBeamVisuals[i];

			float step = BeamSpeed * Time.Delta;
			visual.Position += visual.Direction * step;
			visual.DistanceTraveled += step;
			visual.Lifetime -= Time.Delta;

			if ( visual.VisualObject != null && visual.VisualObject.IsValid() )
			{
				visual.VisualObject.WorldPosition = visual.Position;
				visual.VisualObject.WorldRotation = Rotation.LookAt( visual.Direction, Vector3.Up );
			}

			if ( visual.DistanceTraveled >= BeamRange || visual.Lifetime <= 0f )
			{
				if ( visual.VisualObject != null && visual.VisualObject.IsValid() )
					visual.VisualObject.Destroy();

				_activeBeamVisuals.RemoveAt( i );
			}
		}
	}

	void ApplyBeamDamageToPlayer( GameObject playerObj, int rawDamage )
	{
		if ( playerObj == null || !playerObj.IsValid() )
			return;

		var playerHealth = playerObj.Components.Get<PlayerHealth>();
		if ( playerHealth == null || playerHealth.IsDead )
			return;

		var playerInventory = playerObj.Components.Get<Inventory>();
		var playerSkills = playerObj.Components.Get<Skills>();
		var potionSystem = playerObj.Components.Get<PotionSystem>();

		var playerWeaponDef = playerInventory?.GetEquippedWeaponDef();
		CombatStyle playerStyle = CombatTriangle.GetStyleFromWeapon( playerWeaponDef );
		float triangleMult = CombatTriangle.GetDealMultiplier( CombatStyle.Magic, playerStyle );

		float armorValue = playerInventory != null ? CombatTriangle.GetEffectiveArmorValue( playerInventory ) : 0f;
		float armorReduction = CombatTriangle.GetArmorReduction( armorValue );

		float defenceMult = playerSkills != null ? playerSkills.GetDefenceMultiplier() : 1f;

		float defenceBuffMult = 1f;
		if ( potionSystem != null )
			defenceBuffMult = potionSystem.GetBuffMultiplier( BuffType.Defence );

		int finalDamage = (int)( rawDamage * triangleMult * ( 1f - armorReduction ) / defenceMult / defenceBuffMult );
		if ( finalDamage < 1 )
			finalDamage = 1;

		playerHealth.TakeDamage( finalDamage );
		DamagePopupBroadcaster.Broadcast( playerObj.WorldPosition + Vector3.Up * 60f, finalDamage, playerHealth.MaxHealth, false );
	}

	void UpdateReturning()
	{
		PrimaryTarget = null;

		ApplyLeashHeal();

		float d = FlatDistance( WorldPosition, _spawnPosition );
		if ( d < 20f )
		{
			WorldPosition = _spawnPosition;
			WorldRotation = _spawnRotation;
			SetMoving( false, false );
			CurrentHealth = MaxHealth;
			_leashHealAccum = 0f;
			_state = BossState.Idle;
			return;
		}

		FaceTarget( _spawnPosition );
		SetMoving( true, true );
		MoveTowards( _spawnPosition, RunSpeed );
	}

	void ApplyLeashHeal()
	{
		if ( CurrentHealth >= MaxHealth )
			return;

		float perSecond = MaxHealth * ( LeashHealPercentPerSecond / 100f );
		_leashHealAccum += perSecond * Time.Delta;

		int whole = (int)_leashHealAccum;
		if ( whole <= 0 )
			return;

		_leashHealAccum -= whole;
		CurrentHealth = Math.Min( CurrentHealth + whole, MaxHealth );
	}

	void UpdatePrimaryTarget()
	{
		if ( !EnsureValidTarget() )
		{
			var next = FindNearestPlayerInAggroRange();
			if ( next != null )
			{
				PrimaryTarget = next;
				_targetSwitchTimer = RollTargetSwitchInterval();
			}
		}

		_targetSwitchTimer -= Time.Delta;
		if ( _targetSwitchTimer > 0f )
			return;

		var candidates = FindPlayersInAggroRange();
		if ( candidates.Count > 0 )
		{
			var pick = candidates[Game.Random.Int( 0, candidates.Count - 1 )];
			PrimaryTarget = pick;
		}
		_targetSwitchTimer = RollTargetSwitchInterval();
	}

	float RollTargetSwitchInterval()
	{
		return (float)( TargetSwitchMinInterval + Game.Random.NextDouble() * ( TargetSwitchMaxInterval - TargetSwitchMinInterval ) );
	}

	bool EnsureValidTarget()
	{
		if ( PrimaryTarget == null || !PrimaryTarget.IsValid() )
			return false;

		var health = PrimaryTarget.Components.Get<PlayerHealth>();
		if ( health == null || health.IsDead )
			return false;

		float d = FlatDistance( WorldPosition, PrimaryTarget.WorldPosition );
		if ( d > DeaggroRange )
			return false;

		return true;
	}

	GameObject FindNearestPlayerInAggroRange()
	{
		GameObject best = null;
		float bestDistSq = AggroRange * AggroRange;

		foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
		{
			if ( pc == null || !pc.IsValid() )
				continue;

			var obj = pc.GameObject;
			var health = obj.Components.Get<PlayerHealth>();
			if ( health == null || health.IsDead )
				continue;

			float distSq = ( obj.WorldPosition - WorldPosition ).LengthSquared;
			if ( distSq <= bestDistSq )
			{
				bestDistSq = distSq;
				best = obj;
			}
		}

		return best;
	}

	List<GameObject> FindPlayersInAggroRange()
	{
		var list = new List<GameObject>();
		float rangeSq = AggroRange * AggroRange;

		foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
		{
			if ( pc == null || !pc.IsValid() )
				continue;

			var obj = pc.GameObject;
			var health = obj.Components.Get<PlayerHealth>();
			if ( health == null || health.IsDead )
				continue;

			float distSq = ( obj.WorldPosition - WorldPosition ).LengthSquared;
			if ( distSq <= rangeSq )
				list.Add( obj );
		}

		return list;
	}

	bool HasLineOfSight()
	{
		if ( PrimaryTarget == null || !PrimaryTarget.IsValid() )
			return false;

		var origin = WorldPosition + Vector3.Up * 80f;
		var target = PrimaryTarget.WorldPosition + Vector3.Up * 40f;

		var trace = Scene.Trace
			.Ray( origin, target )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player", "boss", "monster", "pickup" )
			.Run();

		return !trace.Hit;
	}

	void UpdatePillarHealing()
	{
		bool anyAlive = HasAlivePillar();
		if ( !anyAlive )
		{
			_pillarHealTimer = 0f;
			return;
		}

		_pillarHealTimer += Time.Delta;
		if ( _pillarHealTimer >= PillarHealInterval )
		{
			_pillarHealTimer = 0f;
			int healAmount = (int)( MaxHealth * ( PillarHealPercent / 100f ) );
			CurrentHealth = Math.Min( MaxHealth, CurrentHealth + healAmount );
		}
	}

	bool HasAlivePillar()
	{
		foreach ( var p in Pillars )
		{
			if ( p != null && p.IsAlive )
				return true;
		}
		return false;
	}

	protected override void OnPreRender()
	{
		UpdateWeaponAttachment();
	}

	void UpdateWeaponAttachment()
	{
		if ( _weaponInstance == null || !_weaponInstance.IsValid() )
			return;

		if ( SkinnedRenderer == null || SkinnedRenderer.SceneModel == null )
			return;

		if ( string.IsNullOrEmpty( WeaponBone ) )
			return;

		var boneTx = SkinnedRenderer.SceneModel.GetBoneWorldTransform( WeaponBone );

		_weaponInstance.WorldPosition = boneTx.Position + boneTx.Rotation * WeaponLocalOffset;
		_weaponInstance.WorldRotation = boneTx.Rotation * WeaponLocalRotation.ToRotation();
	}

	void UpdatePillarTint()
	{
		if ( ModelRenderer == null )
			return;

		if ( !_baseRendererTintCaptured )
		{
			_baseRendererTint = ModelRenderer.Tint;
			_baseRendererTintCaptured = true;
		}

		ModelRenderer.Tint = HasAlivePillar() && !IsDead ? ProtectedTint : _baseRendererTint;
	}

	void UpdateRespawn()
	{
		if ( !_deathAnimFinished )
		{
			_deathAnimTimer += Time.Delta;
			if ( _deathAnimTimer >= DeathAnimDuration + DeathHoldDuration )
			{
				_deathAnimFinished = true;
				BroadcastDeathHide();
			}
			return;
		}

		if ( !_respawnRequested )
		{
			float delay = (float)( RespawnDelayMin + Game.Random.NextDouble() * ( RespawnDelayMax - RespawnDelayMin ) );
			_respawnTimer = delay;
			_respawnRequested = true;
		}

		_respawnTimer -= Time.Delta;
		if ( _respawnTimer > 0f )
			return;

		bool allPillarsAlive = true;
		foreach ( var p in Pillars )
		{
			if ( p == null )
				continue;
			if ( !p.IsAlive )
			{
				allPillarsAlive = false;
				break;
			}
		}

		if ( !allPillarsAlive )
		{
			_respawnTimer = 10f;
			return;
		}

		Respawn();
	}

	void Respawn()
	{
		IsDead = false;
		CurrentHealth = MaxHealth;
		PrimaryTarget = null;
		_respawnRequested = false;
		_deathAnimFinished = false;
		_deathAnimTimer = 0f;
		_state = BossState.Idle;
		_contributorSteamIds.Clear();
		BroadcastAnimBool( "b_death", false );
		BroadcastRespawn();
	}

	[Rpc.Broadcast]
	void BroadcastRespawn()
	{
		WorldPosition = _spawnPosition + RespawnOffset;
		WorldRotation = _spawnRotation;

		if ( BossCollider != null )
			BossCollider.Enabled = true;

		if ( ModelRenderer != null )
			ModelRenderer.Enabled = true;

		if ( _weaponInstance != null && _weaponInstance.IsValid() )
			_weaponInstance.Enabled = true;
	}

	[Rpc.Host]
	public void TakeDamage( int damage, GameObject attacker )
	{
		if ( IsDead )
			return;

		if ( attacker != null && attacker.IsValid() )
		{
			var ownerConnection = attacker.Network.Owner;
			if ( ownerConnection != null && ownerConnection.SteamId != 0L )
				_contributorSteamIds.Add( ownerConnection.SteamId );
		}

		CurrentHealth -= damage;

		if ( CurrentHealth <= 0 )
		{
			CurrentHealth = 0;
			Die( attacker );
		}
		else
		{
			if ( _state == BossState.Idle || _state == BossState.Patrolling )
			{
				if ( attacker != null && attacker.IsValid() )
				{
					PrimaryTarget = attacker;
					StartBattlecry();
				}
			}
		}
	}

	void Die( GameObject killer )
	{
		IsDead = true;
		_state = BossState.Dead;
		_deathAnimTimer = 0f;
		_deathAnimFinished = false;
		SetMoving( false, false );
		BroadcastAnimBool( "b_death", true );
		AwardLootToContributors();
		BroadcastDeathStart();

		_deathGeneration++;
		PlayDeathSoundSequence( _deathGeneration );
	}

	async void PlayDeathSoundSequence( int generation )
	{
		await Task.Yield();
		if ( !IsValid || !IsDead || generation != _deathGeneration )
			return;
		SoundLibrary.PlayBossDeathGrasp( WorldPosition );

		await Task.DelaySeconds( 49f / AnimFrameRate );
		if ( !IsValid || !IsDead || generation != _deathGeneration )
			return;
		SoundLibrary.PlayBossDeathKnees( WorldPosition );

		await Task.DelaySeconds( ( 93f - 49f ) / AnimFrameRate );
		if ( !IsValid || !IsDead || generation != _deathGeneration )
			return;
		SoundLibrary.PlayBossDeathFall( WorldPosition );
	}

	[Rpc.Broadcast]
	void BroadcastDeathStart()
	{
		if ( BossCollider != null )
			BossCollider.Enabled = false;
	}

	[Rpc.Broadcast]
	void BroadcastDeathHide()
	{
		if ( ModelRenderer != null )
			ModelRenderer.Enabled = false;

		if ( _weaponInstance != null && _weaponInstance.IsValid() )
			_weaponInstance.Enabled = false;
	}

	void AwardLootToContributors()
	{
		if ( _contributorSteamIds.Count == 0 )
			return;

		if ( LootTable == null )
		{
			_contributorSteamIds.Clear();
			return;
		}

		var rng = new Random();

		int contributorCount = _contributorSteamIds.Count;

		int goldPool = LootTable.RollGoldPool( rng );
		int goldPerPlayer = contributorCount > 0
			? (int)Math.Ceiling( goldPool / (double)contributorCount )
			: 0;

		float oddsScale = GroupLootRetention + ( 1f - GroupLootRetention ) / contributorCount;

		var entries = LootTable.Entries ?? new List<LootEntry>();

		foreach ( var steamId in _contributorSteamIds )
		{
			var rolledItems = new List<ItemId>();
			var rolledAmounts = new List<int>();

			foreach ( var entry in entries )
			{
				if ( entry == null || entry.Item == ItemId.None || entry.ChancePercent <= 0f )
					continue;

				float scaledChance = entry.ChancePercent * oddsScale;
				if ( (float)( rng.NextDouble() * 100.0 ) >= scaledChance )
					continue;

				int amount = LootTable.RollEntryAmount( rng, entry );
				if ( amount <= 0 )
					continue;

				rolledItems.Add( entry.Item );
				rolledAmounts.Add( amount );
			}

			if ( goldPerPlayer > 0 || rolledItems.Count > 0 )
				BroadcastLootReward( steamId, goldPerPlayer, rolledItems.ToArray(), rolledAmounts.ToArray() );
		}

		_contributorSteamIds.Clear();
	}

	[Rpc.Broadcast]
	void BroadcastLootReward( ulong recipientSteamId, int gold, ItemId[] items, int[] amounts )
	{
		if ( Connection.Local == null || Connection.Local.SteamId != recipientSteamId )
			return;

		var localPlayer = FindLocalPlayerForLoot();
		if ( localPlayer == null )
			return;

		var inventory = localPlayer.Components.Get<Inventory>();
		if ( inventory == null )
			return;

		bool gainedAny = false;

		if ( gold > 0 )
		{
			var (placed, banked) = inventory.AddItemOrBank( ItemId.GoldCoin, gold );
			if ( placed > 0 || banked > 0 )
			{
				if ( placed > 0 )
					GameLog.Add( $"You looted {placed} gold.", "#f0c040" );
				if ( banked > 0 )
					GameLog.Add( $"Inventory full — {banked} gold sent to your bank.", "#c9a84c" );
				gainedAny = true;
			}
		}

		int len = Math.Min( items.Length, amounts.Length );
		for ( int i = 0; i < len; i++ )
		{
			var id = items[i];
			int amt = amounts[i];
			if ( id == ItemId.None || amt <= 0 )
				continue;

			var (placed, banked) = inventory.AddItemOrBank( id, amt );
			if ( placed <= 0 && banked <= 0 )
				continue;

			ItemPickupEffect.Trigger( id );

			var def = ItemDatabase.Get( id );
			string name = def != null ? def.Name : id.ToString();

			if ( placed > 0 )
				GameLog.Add( $"You looted {placed}x {name}.", "#6db8f0" );
			if ( banked > 0 )
				GameLog.Add( $"Inventory full — {banked}x {name} sent to your bank.", "#c9a84c" );

			gainedAny = true;
		}

		if ( gainedAny )
			SoundLibrary.PlayReceiveItem();
	}

	GameObject FindLocalPlayerForLoot()
	{
		foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
		{
			var owner = pc.Network.Owner;
			if ( owner != null && Connection.Local != null && owner.SteamId == Connection.Local.SteamId )
				return pc.GameObject;
		}
		return null;
	}

	static void LogLoot( ItemId item, int amount )
	{
		var def = ItemDatabase.Get( item );
		string name = def != null ? def.Name : item.ToString();
		GameLog.Add( $"You looted {amount}x {name}.", "#6db8f0" );
	}

	void FaceTarget( Vector3 targetPos )
	{
		var to = ( targetPos - WorldPosition ).WithZ( 0f );
		if ( to.LengthSquared < 0.0001f )
			return;

		var desired = Rotation.LookAt( to.Normal, Vector3.Up );
		float maxStep = TurnSpeedDegrees * Time.Delta;
		float t = Math.Clamp( maxStep / 180f, 0f, 1f );
		WorldRotation = Rotation.Slerp( WorldRotation, desired, t );
	}

	void MoveTowards( Vector3 targetPos, float speed )
	{
		if ( _state == BossState.Attacking || _state == BossState.Battlecry || _state == BossState.Dead )
			return;

		var to = ( targetPos - WorldPosition ).WithZ( 0f );
		if ( to.LengthSquared < 0.0001f )
			return;

		var step = to.Normal * speed * Time.Delta;
		WorldPosition += step;
		SnapToGround();
	}

	void SnapToGround()
	{
		var trace = Scene.Trace
			.Ray( WorldPosition + Vector3.Up * 50f, WorldPosition + Vector3.Down * 200f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player", "boss", "monster", "pickup" )
			.Run();

		if ( trace.Hit )
			WorldPosition = trace.HitPosition;
	}

	float FlatDistance( Vector3 a, Vector3 b )
	{
		a.z = 0;
		b.z = 0;
		return Vector3.DistanceBetween( a, b );
	}

	void SetMoving( bool moving, bool running )
	{
		BroadcastAnimBool( "is_moving", moving );
		BroadcastAnimBool( "is_running", running );
	}

	[Rpc.Broadcast]
	void BroadcastAnimBool( string param, bool value )
	{
		if ( string.IsNullOrEmpty( param ) )
			return;

		if ( SkinnedRenderer != null )
			SkinnedRenderer.Set( param, value );
	}

	void UpdateDebugHitboxes()
	{
		if ( _debugHitboxes.Count == 0 )
			return;

		for ( int i = _debugHitboxes.Count - 1; i >= 0; i-- )
		{
			var d = _debugHitboxes[i];
			d.TimeRemaining -= Time.Delta;
			_debugHitboxes[i] = d;
			if ( d.TimeRemaining <= 0f )
				_debugHitboxes.RemoveAt( i );
		}
	}

	protected override void DrawGizmos()
	{
		if ( Attacks != null )
		{
			foreach ( var attack in Attacks )
			{
				if ( !attack.ShowGizmo || attack.Hits == null )
					continue;

				Gizmo.Draw.Color = GizmoColorForAttack( attack.Type );
				foreach ( var hit in attack.Hits )
				{
					var halfSize = hit.HitboxSize * 0.5f;
					var bbox = new BBox( hit.HitboxLocalOffset - halfSize, hit.HitboxLocalOffset + halfSize );
					Gizmo.Draw.LineBBox( bbox );
				}
			}
		}

		if ( ShowHitboxDebug && _debugHitboxes.Count > 0 )
		{
			Gizmo.Draw.Color = Color.Red;
			foreach ( var d in _debugHitboxes )
			{
				var halfSize = d.Size * 0.5f;
				var bbox = new BBox( d.LocalOffset - halfSize, d.LocalOffset + halfSize );
				Gizmo.Draw.LineBBox( bbox );
			}
		}

		Gizmo.Draw.Color = Color.Yellow.WithAlpha( 0.3f );
		Gizmo.Draw.LineSphere( Vector3.Zero, AggroRange );

		Gizmo.Draw.Color = Color.Orange.WithAlpha( 0.3f );
		Gizmo.Draw.LineSphere( Vector3.Zero, MeleeRange );

		Gizmo.Draw.Color = Color.Red.WithAlpha( 0.3f );
		Gizmo.Draw.LineSphere( Vector3.Zero, KickRange );
	}

	Color GizmoColorForAttack( BossAttackType type )
	{
		switch ( type )
		{
			case BossAttackType.Downward: return Color.White;
			case BossAttackType.ThreeSixtyLow: return Color.Cyan;
			case BossAttackType.Combo: return Color.Magenta;
			case BossAttackType.Kick: return Color.Red;
			case BossAttackType.Horizontal: return Color.Yellow;
			default: return Color.White;
		}
	}
}