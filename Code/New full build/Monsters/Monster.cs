using Sandbox;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class Monster : Component
{
	[Property, Group( "Identity" )] public string MonsterName { get; set; } = "Goblin";
	[Property, Group( "Identity" )] public string MonsterType { get; set; } = "Goblin";
	[Property, Group( "Identity" )] public CombatStyle CombatStyle { get; set; } = CombatStyle.Melee;

	[Property, Group( "Stats" )] public int MaxHealth { get; set; } = 100;
	[Property, Group( "Stats" )] public int Damage { get; set; } = 10;
	[Property, Group( "Stats" )] public float AttackCooldown { get; set; } = 2f;
	[Property, Group( "Stats" )] public int CombatXpReward { get; set; } = 50;

	[Property, Group( "Movement" )] public float PatrolSpeed { get; set; } = 100f;
	[Property, Group( "Movement" )] public float ChaseSpeed { get; set; } = 150f;
	[Property, Group( "Movement" )] public float SmoothTurnSpeed { get; set; } = 180f;
	[Property, Group( "Movement" )] public float StopAndTurnThreshold { get; set; } = 60f;

	[Property, Group( "Aggro" )] public float AggroRange { get; set; } = 400f;
	[Property, Group( "Aggro" )] public float LeashRange { get; set; } = 800f;
	[Property, Group( "Aggro" )] public float AttackRange { get; set; } = 80f;
	[Property, Group( "Aggro" )] public float LeashNoExchangeTime { get; set; } = 8f;
	[Property, Group( "Aggro" )] public float LeashNoHitChaseTime { get; set; } = 30f;

	[Property, Group( "Ranged" )] public bool IsRanged { get; set; } = false;
	[Property, Group( "Ranged" )] public GameObject ProjectilePrefab { get; set; }
	[Property, Group( "Ranged" )] public float ProjectileRange { get; set; } = 300f;
	[Property, Group( "Ranged" )] public float ProjectileSpeed { get; set; } = 400f;
	[Property, Group( "Ranged" )] public float ProjectileSpawnHeight { get; set; } = 50f;
	[Property, Group( "Ranged" )] public float ProjectileCastDelay { get; set; } = 0.3f;
	[Property, Group( "Ranged" )] public float ProjectileDamageDelay { get; set; } = 0.5f;
	[Property, Group( "Ranged" )] public string ProjectileSpawnBone { get; set; } = "RightHand";
	[Property, Group( "Ranged" )] public float ProjectileForwardOffset { get; set; } = 20f;

	[Property, Group( "Respawn" )] public float RespawnMin { get; set; } = 5f;
	[Property, Group( "Respawn" )] public float RespawnMax { get; set; } = 20f;
	[Property, Group( "Respawn" )] public float DespawnBlinkDuration { get; set; } = 0.6f;
	[Property, Group( "Respawn" )] public float MaterializeDuration { get; set; } = 1.5f;
	[Property, Group( "Respawn" )] public float BlinkIntervalSlow { get; set; } = 0.28f;
	[Property, Group( "Respawn" )] public float BlinkIntervalFast { get; set; } = 0.05f;

	[Property, Group( "Animations" )] public float AttackAnimLength { get; set; } = 1.0f;
	[Property, Group( "Animations" )] public float DamageDelay { get; set; } = 0.6f;
	[Property, Group( "Animations" )] public float DeathAnimLength { get; set; } = 2.0f;
	[Property, Group( "Animations" )] public float DeathLingerTime { get; set; } = 2.0f;
	[Property, Group( "Animations" )] public float VictoryAnimLength { get; set; } = 3.0f;

	[Property, Group( "Culling" )] public float DrawDistanceMax { get; set; } = 5000f;

	[Property, Group( "References" )] public SkinnedModelRenderer ModelRenderer { get; set; }
	[Property, Group( "References" )] public Collider MonsterCollider { get; set; }
	[Property, Group( "References" )] public List<GameObject> Waypoints { get; set; } = new();

	[Property, Group( "Loot" )] public ItemId LootItem1 { get; set; } = ItemId.None;
	[Property, Group( "Loot" )] public int LootAmount1 { get; set; } = 1;
	[Property, Group( "Loot" )] public float LootChance1 { get; set; } = 50f;
	[Property, Group( "Loot" )] public ItemId LootItem2 { get; set; } = ItemId.None;
	[Property, Group( "Loot" )] public int LootAmount2 { get; set; } = 1;
	[Property, Group( "Loot" )] public float LootChance2 { get; set; } = 25f;
	[Property, Group( "Loot" )] public ItemId LootItem3 { get; set; } = ItemId.None;
	[Property, Group( "Loot" )] public int LootAmount3 { get; set; } = 1;
	[Property, Group( "Loot" )] public float LootChance3 { get; set; } = 10f;

	[Sync] public int CurrentHealth { get; set; }
	[Sync] public bool IsDead { get; set; }
	[Sync] public bool IsAggro { get; set; }
	[Sync] public bool IsFrozen { get; set; }
	[Sync] public float FreezeTimeRemaining { get; set; }
	[Sync] public float SlowTimeRemaining { get; set; }
	[Sync] public float SlowMultiplier { get; set; } = 1f;
	[Sync] public GameObject FirstAttacker { get; set; }

	enum MonsterState { Idle, Patrolling, TurningInPlace, Chasing, Attacking, Repositioning, Returning, Dead, Victory }

	MonsterState _state = MonsterState.Idle;
	Vector3 _spawnPosition;
	int _currentWaypoint = 0;
	GameObject _target;
	float _attackCooldownRemaining = 0f;
	float _attackAnimTimer = 0f;
	float _victoryTimer = 0f;
	float _targetYaw;
	float _healthRegenAccum = 0f;
	int _respawnGeneration = 0;
	int _strafeDirection = 1;
	float _repositionExtra = 0f;

	float _lastDamageExchangeTime = -100f;
	float _chaseStartTime = -100f;
	bool _hasLandedHitDuringChase = false;

	bool _lastBroadcastMoving = false;
	bool _lastBroadcastRunning = false;

	bool _localCulled = false;
	float _nextCullCheckTime = 0f;

	enum VisualPhase { Solid, Dying, DespawnBlink, Hidden, MaterializeBlink }
	VisualPhase _visualPhase = VisualPhase.Solid;
	float _phaseTimer = 0f;
	float _blinkAccum = 0f;
	bool _blinkVisible = true;

	protected override void OnStart()
	{
		_spawnPosition = GameObject.WorldPosition;
		CurrentHealth = MaxHealth;

		_nextCullCheckTime = Time.Now + Random.Shared.NextSingle() * 0.5f;

		if ( !IsDead )
		{
			ApplyCulling( ShouldCullForDistance() );
		}
		else
		{
			_visualPhase = VisualPhase.Hidden;
			ApplyVisibility();
			if ( MonsterCollider != null )
				MonsterCollider.Enabled = false;
		}
	}

	protected override void OnUpdate()
	{
		UpdateVisualPhase();

		if ( !Networking.IsHost )
			return;

		if ( IsDead )
			return;

		if ( SlowTimeRemaining > 0f )
		{
			SlowTimeRemaining -= Time.Delta;
			if ( SlowTimeRemaining <= 0f )
			{
				SlowTimeRemaining = 0f;
				SlowMultiplier = 1f;
			}
		}

		if ( IsFrozen )
		{
			FreezeTimeRemaining -= Time.Delta;
			if ( FreezeTimeRemaining <= 0f )
			{
				IsFrozen = false;
				FreezeTimeRemaining = 0f;
			}
			SetMoving( false, false );
			return;
		}

		switch ( _state )
		{
			case MonsterState.Idle: UpdateIdle(); break;
			case MonsterState.Patrolling: UpdatePatrolling(); break;
			case MonsterState.TurningInPlace: UpdateTurningInPlace(); break;
			case MonsterState.Chasing: UpdateChasing(); break;
			case MonsterState.Attacking: UpdateAttacking(); break;
			case MonsterState.Repositioning: UpdateRepositioning(); break;
			case MonsterState.Returning: UpdateReturning(); break;
			case MonsterState.Victory: UpdateVictory(); break;
		}
	}

	bool ShouldCullForDistance()
	{
		if ( DrawDistanceMax <= 0f )
			return false;

		var camera = Scene.Camera;
		if ( camera == null )
			return false;

		float sqrDist = ( WorldPosition - camera.WorldPosition ).LengthSquared;
		float maxSqr = DrawDistanceMax * DrawDistanceMax;

		return sqrDist > maxSqr;
	}

	void ApplyCulling( bool culled )
	{
		_localCulled = culled;
		ApplyVisibility();

		if ( MonsterCollider != null )
			MonsterCollider.Enabled = !culled && _visualPhase == VisualPhase.Solid;
	}

	void ApplyVisibility()
	{
		if ( ModelRenderer == null )
			return;

		switch ( _visualPhase )
		{
			case VisualPhase.Hidden:
				ModelRenderer.Enabled = false;
				break;

			case VisualPhase.DespawnBlink:
			case VisualPhase.MaterializeBlink:
				ModelRenderer.Enabled = !_localCulled;
				SetRendering( !_localCulled && _blinkVisible );
				break;

			default:
				ModelRenderer.Enabled = !_localCulled;
				SetRendering( true );
				break;
		}
	}

	void SetRendering( bool on )
	{
		if ( ModelRenderer != null && ModelRenderer.SceneModel != null )
			ModelRenderer.SceneModel.RenderingEnabled = on;
	}

	void EnterVisualPhase( VisualPhase phase )
	{
		_visualPhase = phase;
		_phaseTimer = 0f;
		_blinkAccum = 0f;
		_blinkVisible = phase != VisualPhase.Hidden;
		ApplyVisibility();
	}

	void UpdateVisualPhase()
	{
		if ( _visualPhase == VisualPhase.Solid )
		{
			if ( !IsDead && Time.Now >= _nextCullCheckTime )
			{
				_nextCullCheckTime = Time.Now + 0.5f;
				bool shouldCull = ShouldCullForDistance();
				if ( shouldCull != _localCulled )
					ApplyCulling( shouldCull );
			}
			return;
		}

		_phaseTimer += Time.Delta;

		switch ( _visualPhase )
		{
			case VisualPhase.Dying:
				if ( _phaseTimer >= MathF.Max( 0f, DeathAnimLength - DespawnBlinkDuration ) )
					EnterVisualPhase( VisualPhase.DespawnBlink );
				break;

			case VisualPhase.DespawnBlink:
				TickBlink( DespawnBlinkDuration );
				if ( _phaseTimer >= DespawnBlinkDuration )
				{
					_blinkVisible = false;
					EnterVisualPhase( VisualPhase.Hidden );
				}
				break;

			case VisualPhase.MaterializeBlink:
				TickBlink( MaterializeDuration );
				if ( _phaseTimer >= MaterializeDuration )
					EnterVisualPhase( VisualPhase.Solid );
				break;
		}
	}

	void TickBlink( float duration )
	{
		float t = duration > 0f ? Math.Clamp( _phaseTimer / duration, 0f, 1f ) : 1f;
		t *= t;
		float interval = BlinkIntervalSlow + ( BlinkIntervalFast - BlinkIntervalSlow ) * t;

		_blinkAccum += Time.Delta;
		if ( _blinkAccum >= interval )
		{
			_blinkAccum = 0f;
			_blinkVisible = !_blinkVisible;
			ApplyVisibility();
		}
	}

	void UpdateIdle()
	{
		SetMoving( false, false );

		if ( CheckAggro() )
			return;

		if ( Waypoints != null && Waypoints.Count > 0 )
			_state = MonsterState.Patrolling;
	}

	void UpdatePatrolling()
	{
		if ( CheckAggro() )
			return;

		if ( Waypoints == null || Waypoints.Count == 0 )
		{
			_state = MonsterState.Idle;
			return;
		}

		var waypoint = Waypoints[_currentWaypoint];
		if ( waypoint == null )
		{
			_currentWaypoint = ( _currentWaypoint + 1 ) % Waypoints.Count;
			return;
		}

		float dist = FlatDistance( WorldPosition, waypoint.WorldPosition );

		if ( dist < 20f )
		{
			_currentWaypoint = ( _currentWaypoint + 1 ) % Waypoints.Count;

			var next = Waypoints[_currentWaypoint];
			if ( next == null )
				return;

			float angle = GetAngleTo( next.WorldPosition );

			if ( angle > StopAndTurnThreshold )
			{
				_targetYaw = YawToFace( next.WorldPosition );
				SetMoving( false, false );
				_state = MonsterState.TurningInPlace;
			}

			return;
		}

		RotateToward( waypoint.WorldPosition );
		SetMoving( true, false );
		MoveForward( PatrolSpeed * GetSpeedMultiplier() );
		SnapToGround();
	}

	void UpdateTurningInPlace()
	{
		if ( CheckAggro() )
			return;

		SetMoving( false, false );

		float currentYaw = GameObject.WorldRotation.Yaw();
		float delta = NormalizeAngle( _targetYaw - currentYaw );

		if ( MathF.Abs( delta ) < 2f )
		{
			GameObject.WorldRotation = Rotation.FromYaw( _targetYaw );
			_state = MonsterState.Patrolling;
			return;
		}

		float step = SmoothTurnSpeed * Time.Delta;
		float move = MathF.Min( MathF.Abs( delta ), step ) * MathF.Sign( delta );
		GameObject.WorldRotation = Rotation.FromYaw( currentYaw + move );
	}

	bool ShouldLeash()
	{
		if ( _target == null || !_target.IsValid() )
			return true;

		float sinceExchange = Time.Now - _lastDamageExchangeTime;
		if ( sinceExchange > LeashNoExchangeTime && !HasLineOfSightRanged( _target ) )
			return true;

		float sinceChaseStart = Time.Now - _chaseStartTime;
		if ( sinceChaseStart > LeashNoHitChaseTime && !_hasLandedHitDuringChase )
			return true;

		return false;
	}

	void UpdateChasing()
	{
		if ( _target == null || !_target.IsValid() )
		{
			_target = null;
			IsAggro = false;
			StartReturning();
			return;
		}

		if ( ShouldLeash() )
		{
			_target = null;
			IsAggro = false;
			StartReturning();
			return;
		}

		float dist = FlatDistance( WorldPosition, _target.WorldPosition );

		float effectiveAttackRange = IsRanged ? ProjectileRange : AttackRange;

		if ( dist <= effectiveAttackRange )
		{
			_state = MonsterState.Attacking;
			return;
		}

		FaceTarget( _target.WorldPosition );
		SetMoving( true, true );
		MoveTowards( _target.WorldPosition, ChaseSpeed * GetSpeedMultiplier() );
		SnapToGround();
	}

	void UpdateAttacking()
	{
		SetMoving( false, false );

		if ( _attackAnimTimer > 0f )
		{
			_attackAnimTimer -= Time.Delta;
			if ( _target != null && _target.IsValid() )
				FaceTarget( _target.WorldPosition );
			return;
		}

		if ( _target == null || !_target.IsValid() )
		{
			_target = null;
			IsAggro = false;
			StartReturning();
			return;
		}

		float dist = FlatDistance( WorldPosition, _target.WorldPosition );
		float effectiveAttackRange = IsRanged ? ProjectileRange : AttackRange;

		if ( dist > effectiveAttackRange * 1.3f )
		{
			_attackCooldownRemaining = 0f;
			_state = MonsterState.Chasing;
			return;
		}

		FaceTarget( _target.WorldPosition );

		if ( IsRanged && !HasLineOfSightRanged( _target ) )
		{
			_strafeDirection = Game.Random.Float( 0f, 1f ) > 0.5f ? 1 : -1;
			_repositionExtra = 0f;
			_state = MonsterState.Repositioning;
			return;
		}

		if ( _attackCooldownRemaining > 0f )
		{
			_attackCooldownRemaining -= Time.Delta;
			return;
		}

		PerformAttack();
	}

	void UpdateRepositioning()
	{
		if ( _target == null || !_target.IsValid() )
		{
			_target = null;
			IsAggro = false;
			StartReturning();
			return;
		}

		if ( ShouldLeash() )
		{
			_target = null;
			IsAggro = false;
			StartReturning();
			return;
		}

		bool hasLos = HasLineOfSightRanged( _target );

		if ( hasLos )
		{
			_repositionExtra += Time.Delta;

			if ( _repositionExtra > 0.4f )
			{
				SetMoving( false, false );
				_state = MonsterState.Attacking;
				return;
			}
		}

		Vector3 toTarget = _target.WorldPosition - WorldPosition;
		Vector3 flatToTarget = new Vector3( toTarget.x, toTarget.y, 0f ).Normal;
		Vector3 strafeDir = new Vector3( -flatToTarget.y, flatToTarget.x, 0f ) * _strafeDirection;

		Vector3 moveDir = ( flatToTarget * 0.4f + strafeDir * 0.6f ).Normal;

		FaceTarget( _target.WorldPosition );
		SetMoving( true, true );

		float moveDist = ChaseSpeed * GetSpeedMultiplier() * Time.Delta;

		var trace = Scene.Trace
			.Ray( WorldPosition + Vector3.Up * 30f, WorldPosition + Vector3.Up * 30f + moveDir * moveDist )
			.Radius( 16f )
			.WithoutTags( "monster" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( trace.Hit )
		{
			_strafeDirection = -_strafeDirection;
		}
		else
		{
			GameObject.WorldPosition = new Vector3(
				WorldPosition.x + moveDir.x * moveDist,
				WorldPosition.y + moveDir.y * moveDist,
				WorldPosition.z );
		}

		SnapToGround();
	}

	void UpdateReturning()
	{
		RegenerateHealth();

		float dist = FlatDistance( WorldPosition, _spawnPosition );

		if ( dist < 20f )
		{
			_healthRegenAccum = 0f;

			if ( Waypoints != null && Waypoints.Count > 0 )
			{
				SetNearestWaypointAsCurrent();
				_state = MonsterState.Patrolling;
			}
			else
			{
				_state = MonsterState.Idle;
				SetMoving( false, false );
			}
			return;
		}

		FaceTarget( _spawnPosition );
		SetMoving( true, false );
		MoveTowards( _spawnPosition, PatrolSpeed * GetSpeedMultiplier() );
		SnapToGround();
	}

	void UpdateVictory()
	{
		SetMoving( false, false );
		_victoryTimer -= Time.Delta;

		if ( _victoryTimer <= 0f )
		{
			ModelRenderer?.Set( "b_victory", false );
			_state = Waypoints != null && Waypoints.Count > 0 ? MonsterState.Patrolling : MonsterState.Idle;
		}
	}

	void StartReturning()
	{
		_state = MonsterState.Returning;
	}

	void EnterChase( GameObject target )
	{
		bool isNewEngagement = _state != MonsterState.Chasing && _state != MonsterState.Attacking && _state != MonsterState.Repositioning;

		_target = target;
		IsAggro = true;
		_state = MonsterState.Chasing;

		if ( isNewEngagement )
		{
			_chaseStartTime = Time.Now;
			_hasLandedHitDuringChase = false;
			_lastDamageExchangeTime = Time.Now;
		}
	}

	void SetMoving( bool moving, bool running )
	{
		ModelRenderer?.Set( "is_moving", moving );
		ModelRenderer?.Set( "is_running", running );

		if ( moving != _lastBroadcastMoving || running != _lastBroadcastRunning )
		{
			_lastBroadcastMoving = moving;
			_lastBroadcastRunning = running;
			BroadcastMovingState( moving, running );
		}
	}

	[Rpc.Broadcast]
	void BroadcastMovingState( bool moving, bool running )
	{
		ModelRenderer?.Set( "is_moving", moving );
		ModelRenderer?.Set( "is_running", running );
	}

	[Rpc.Host]
	public void TakeDamage( int damage, GameObject attacker )
	{
		if ( IsDead )
			return;

		if ( FirstAttacker == null && attacker != null )
			FirstAttacker = attacker;

		if ( attacker != null && _state != MonsterState.Returning )
		{
			if ( _target == attacker )
			{
				_lastDamageExchangeTime = Time.Now;
				IsAggro = true;
			}
			else
			{
				EnterChase( attacker );
			}
		}

		CurrentHealth -= damage;

		if ( CurrentHealth <= 0 )
		{
			CurrentHealth = 0;
			Die();
		}
	}

	[Rpc.Host]
	public void ApplyFreeze( float duration )
	{
		if ( IsDead )
			return;

		IsFrozen = true;
		FreezeTimeRemaining = MathF.Max( FreezeTimeRemaining, duration );
	}

	[Rpc.Host]
	public void ApplySlow( float duration, float multiplier )
	{
		if ( IsDead )
			return;

		if ( duration > SlowTimeRemaining )
		{
			SlowTimeRemaining = duration;
			SlowMultiplier = multiplier;
		}
	}

	public float GetSpeedMultiplier()
	{
		if ( SlowTimeRemaining > 0f )
			return SlowMultiplier;
		return 1f;
	}

	bool HasLineOfSightRanged( GameObject target )
	{
		if ( target == null || !target.IsValid() )
			return false;

		Vector3 eyePos = WorldPosition + Vector3.Up * ProjectileSpawnHeight;
		Vector3 targetPos = target.WorldPosition + Vector3.Up * 40f;

		var trace = Scene.Trace
			.Ray( eyePos, targetPos )
			.IgnoreGameObjectHierarchy( GameObject )
			.IgnoreGameObjectHierarchy( target )
			.Run();

		return !trace.Hit;
	}

	void PerformAttack()
	{
		if ( _target == null || !_target.IsValid() )
			return;

		var playerHealth = _target.Components.Get<PlayerHealth>();
		if ( playerHealth == null || playerHealth.IsDead )
			return;

		BroadcastAttackAnim();
		_attackAnimTimer = AttackAnimLength;
		_attackCooldownRemaining = AttackCooldown;

		var playerInventory = _target.Components.Get<Inventory>();
		var playerSkills = _target.Components.Get<Skills>();

		var playerWeaponDef = playerInventory?.GetEquippedWeaponDef();
		CombatStyle playerStyle = CombatTriangle.GetStyleFromWeapon( playerWeaponDef );
		float triangleMult = CombatTriangle.GetDealMultiplier( CombatStyle, playerStyle );

		float armorValue = playerInventory != null ?
			CombatTriangle.GetEffectiveArmorValue( playerInventory ) : 0f;
		float armorReduction = CombatTriangle.GetArmorReduction( armorValue );

		float defenceMult = playerSkills != null ? playerSkills.GetDefenceMultiplier() : 1f;

		float defenceBuffMult = 1f;
		var potionSystem = _target.Components.Get<PotionSystem>();
		if ( potionSystem != null )
			defenceBuffMult = potionSystem.GetBuffMultiplier( BuffType.Defence );

		int finalDamage = (int)( Damage * triangleMult * ( 1f - armorReduction ) / defenceMult / defenceBuffMult );
		if ( finalDamage < 1 ) finalDamage = 1;

		if ( IsRanged )
		{
			SpawnProjectile();
			PlayRangedAttackSoundDelayed();
			DealRangedDamageDelayed( playerHealth, finalDamage );
			return;
		}

		PlayMeleeAttackSoundDelayed();
		DealDamageDelayed( playerHealth, finalDamage );
	}

	void SpawnProjectile()
	{
		if ( ProjectilePrefab == null || _target == null || !_target.IsValid() )
			return;

		var targetObj = _target;
		SpawnProjectileVisual( targetObj );
	}

	async void SpawnProjectileVisual( GameObject targetObj )
	{
		await Task.DelaySeconds( ProjectileCastDelay );

		if ( !IsValid || IsDead )
			return;

		if ( targetObj == null || !targetObj.IsValid() )
			return;

		if ( ProjectilePrefab == null )
			return;

		Vector3 spawnPos = WorldPosition + Vector3.Up * ProjectileSpawnHeight;

		if ( !string.IsNullOrEmpty( ProjectileSpawnBone ) && ModelRenderer != null && ModelRenderer.SceneModel != null )
		{
			var boneTransform = ModelRenderer.SceneModel.GetBoneWorldTransform( ProjectileSpawnBone );
			if ( boneTransform.Position.Length > 0.01f )
				spawnPos = boneTransform.Position;
		}

		Vector3 targetPos = targetObj.WorldPosition + Vector3.Up * 40f;
		Vector3 direction = ( targetPos - spawnPos ).Normal;
		spawnPos += direction * ProjectileForwardOffset;

		var projectile = ProjectilePrefab.Clone( spawnPos );
		if ( projectile == null )
			return;

		float yaw = MathF.Atan2( direction.y, direction.x ) * ( 180f / MathF.PI );
		float pitch = MathF.Asin( -direction.z ) * ( 180f / MathF.PI );
		projectile.WorldRotation = Rotation.From( pitch, yaw, 0f );
		projectile.NetworkSpawn();

		var spellProj = projectile.Components.Get<SpellProjectile>();
		if ( spellProj != null )
		{
			spellProj.Velocity = direction * ProjectileSpeed;
			spellProj.Damage = 0;
			spellProj.Shooter = GameObject;
			spellProj.SpellId = SpellId.Fireball;
			spellProj.MaxRange = ProjectileRange * 2f;
			spellProj.MaxLifetime = 4f;
			spellProj.TraceRadius = 5f;
			spellProj.FreezeDuration = 0f;
			spellProj.FrozenBonusDamage = 1f;
		}

		var homing = projectile.Components.Get<HomingProjectile>();
		if ( homing != null )
			homing.Target = targetObj;
	}

	async void DealRangedDamageDelayed( PlayerHealth playerHealth, int damage )
	{
		await Task.DelaySeconds( ProjectileCastDelay + ProjectileDamageDelay );

		if ( !IsValid || IsDead )
			return;

		if ( !playerHealth.IsValid || playerHealth.IsDead )
			return;

		if ( !HasLineOfSightRanged( playerHealth.GameObject ) )
			return;

		bool willKill = playerHealth.CurrentHealth - damage <= 0;
		playerHealth.TakeDamage( damage );

		_lastDamageExchangeTime = Time.Now;
		_hasLandedHitDuringChase = true;

		DamagePopupBroadcaster.Broadcast( playerHealth.WorldPosition + Vector3.Up * 60f, damage, playerHealth.MaxHealth, false );

		if ( willKill )
		{
			ModelRenderer?.Set( "b_victory", true );
			_victoryTimer = VictoryAnimLength;
			_state = MonsterState.Victory;
		}
	}

	async void DealDamageDelayed( PlayerHealth playerHealth, int damage )
	{
		await Task.DelaySeconds( DamageDelay );

		if ( !IsValid || IsDead )
			return;

		if ( !playerHealth.IsValid || playerHealth.IsDead )
			return;

		bool willKill = playerHealth.CurrentHealth - damage <= 0;
		playerHealth.TakeDamage( damage );

		_lastDamageExchangeTime = Time.Now;
		_hasLandedHitDuringChase = true;

		DamagePopupBroadcaster.Broadcast( playerHealth.WorldPosition + Vector3.Up * 60f, damage, playerHealth.MaxHealth, false );

		if ( willKill )
		{
			ModelRenderer?.Set( "b_victory", true );
			_victoryTimer = VictoryAnimLength;
			_state = MonsterState.Victory;
		}
	}

	[Rpc.Broadcast]
	void BroadcastAttackAnim()
	{
		ModelRenderer?.Set( "b_attack", true );
		ResetAttackBool();
	}

	void PlayAttackSound()
	{
		var pos = WorldPosition;

		if ( CombatStyle == CombatStyle.Ranged )
		{
			SoundLibrary.PlayBowRelease( pos );
			return;
		}

		if ( CombatStyle == CombatStyle.Magic )
		{
			SoundLibrary.PlayFireball( pos );
			return;
		}

		if ( MonsterType == "Troll" )
			SoundLibrary.PlayLargeMonsterAttack( pos );
		else
			SoundLibrary.PlaySmallMonsterAttack( pos );
	}

	async void PlayMeleeAttackSoundDelayed()
	{
		bool isLarge = MonsterType == "Troll";
		float lead = isLarge ? 0.4f : 0.2f;

		float delay = DamageDelay - lead;
		if ( delay < 0f ) delay = 0f;

		await Task.DelaySeconds( delay );

		if ( !IsValid || IsDead )
			return;

		var pos = WorldPosition;
		if ( isLarge )
			SoundLibrary.PlayLargeMonsterAttack( pos );
		else
			SoundLibrary.PlaySmallMonsterAttack( pos );
	}

	async void PlayRangedAttackSoundDelayed()
	{
		await Task.DelaySeconds( ProjectileCastDelay );

		if ( !IsValid || IsDead )
			return;

		var pos = WorldPosition;
		if ( CombatStyle == CombatStyle.Magic )
			SoundLibrary.PlayFireball( pos );
		else
			SoundLibrary.PlayBowRelease( pos );
	}

	async void ResetAttackBool()
	{
		await Task.DelaySeconds( 0.1f );
		if ( IsValid )
			ModelRenderer?.Set( "b_attack", false );
	}

	void Die()
	{
		ExecuteDeath();
	}

	void ExecuteDeath()
	{
		IsDead = true;
		IsAggro = false;
		_target = null;
		_state = MonsterState.Dead;

		int generation = ++_respawnGeneration;
		AwardLootAndXp();
		BroadcastDeath();
		StartRespawnTimer( generation );
	}

	void AwardLootAndXp()
	{
		if ( FirstAttacker == null || !FirstAttacker.IsValid() )
			return;

		ulong killerSteamId = 0;
		var ownerConnection = FirstAttacker.Network.Owner;
		if ( ownerConnection != null )
			killerSteamId = ownerConnection.SteamId;

		if ( killerSteamId == 0 )
			return;

		SkillType killerSkill = GetKillerSkill();

		var rng = new Random();
		int rolledAmount1 = RollLootAmount( rng, LootItem1, LootAmount1, LootChance1 );
		int rolledAmount2 = RollLootAmount( rng, LootItem2, LootAmount2, LootChance2 );
		int rolledAmount3 = RollLootAmount( rng, LootItem3, LootAmount3, LootChance3 );

		BroadcastReward(
			killerSteamId,
			MonsterType,
			killerSkill,
			CombatXpReward,
			LootItem1, rolledAmount1,
			LootItem2, rolledAmount2,
			LootItem3, rolledAmount3 );
	}

	int RollLootAmount( Random rng, ItemId itemId, int amount, float chance )
	{
		if ( itemId == ItemId.None || chance <= 0f )
			return 0;

		if ( (float)( rng.NextDouble() * 100.0 ) < chance )
			return amount;

		return 0;
	}

	[Rpc.Broadcast]
	void BroadcastReward(
		ulong killerSteamId,
		string monsterType,
		SkillType killerSkill,
		int xpReward,
		ItemId loot1, int amount1,
		ItemId loot2, int amount2,
		ItemId loot3, int amount3 )
	{
		if ( Connection.Local == null || Connection.Local.SteamId != killerSteamId )
			return;

		var localPlayer = FindLocalPlayer();
		if ( localPlayer == null )
			return;

		var inventory = localPlayer.Components.Get<Inventory>();
		var skills = localPlayer.Components.Get<Skills>();

		bool gainedAnyItem = false;

		if ( inventory != null )
		{
			inventory.AddKill( monsterType );
			AchievementTracker.OnMonsterKilled();

			if ( loot1 != ItemId.None && amount1 > 0 )
			{
				var (placed, banked) = inventory.AddItemOrBank( loot1, amount1 );
				if ( placed > 0 || banked > 0 )
				{
					ItemPickupEffect.Trigger( loot1 );
					LogLootSplit( loot1, placed, banked );
					gainedAnyItem = true;
				}
			}

			if ( loot2 != ItemId.None && amount2 > 0 )
			{
				var (placed, banked) = inventory.AddItemOrBank( loot2, amount2 );
				if ( placed > 0 || banked > 0 )
				{
					ItemPickupEffect.Trigger( loot2 );
					LogLootSplit( loot2, placed, banked );
					gainedAnyItem = true;
				}
			}

			if ( loot3 != ItemId.None && amount3 > 0 )
			{
				var (placed, banked) = inventory.AddItemOrBank( loot3, amount3 );
				if ( placed > 0 || banked > 0 )
				{
					ItemPickupEffect.Trigger( loot3 );
					LogLootSplit( loot3, placed, banked );
					gainedAnyItem = true;
				}
			}
		}

		if ( skills != null && xpReward > 0 )
			skills.AddCombatXp( killerSkill, xpReward );

		if ( gainedAnyItem )
			SoundLibrary.PlayReceiveItem();
	}

	GameObject FindLocalPlayer()
	{
		var players = Scene.GetAllComponents<PlayerController>();
		foreach ( var player in players )
		{
			var owner = player.Network.Owner;
			if ( owner != null && Connection.Local != null && owner.SteamId == Connection.Local.SteamId )
				return player.GameObject;
		}
		return null;
	}

	SkillType GetKillerSkill()
	{
		if ( FirstAttacker == null || !FirstAttacker.IsValid() )
			return SkillType.Attack;

		var inventory = FirstAttacker.Components.Get<Inventory>();
		var weaponDef = inventory?.GetEquippedWeaponDef();

		if ( weaponDef == null )
			return SkillType.Attack;

		if ( weaponDef.Type == ItemType.RangedWeapon )
			return SkillType.Archery;

		if ( weaponDef.Type == ItemType.MagicWeapon )
			return SkillType.Magic;

		return SkillType.Attack;
	}

	[Rpc.Broadcast]
	void BroadcastDeath()
	{
		if ( MonsterCollider != null )
			MonsterCollider.Enabled = false;

		ModelRenderer?.Set( "b_attack", false );
		ModelRenderer?.Set( "b_victory", false );
		ModelRenderer?.Set( "is_moving", false );
		ModelRenderer?.Set( "is_running", false );
		ModelRenderer?.Set( "b_death", true );

		SoundLibrary.PlayMonsterDeath( WorldPosition );

		EnterVisualPhase( VisualPhase.Dying );
	}

	async void StartRespawnTimer( int generation )
	{
		float delay = RespawnMin + (float)( new Random().NextDouble() * ( RespawnMax - RespawnMin ) );
		await Task.DelaySeconds( DeathAnimLength + DeathLingerTime + delay );

		if ( !IsValid || _respawnGeneration != generation )
			return;

		BroadcastRespawnTelegraph();

		await Task.DelaySeconds( MaterializeDuration );

		if ( !IsValid || _respawnGeneration != generation )
			return;

		BroadcastRespawnComplete();
	}

	[Rpc.Broadcast]
	void BroadcastRespawnTelegraph()
	{
		if ( Networking.IsHost )
		{
			CurrentHealth = MaxHealth;
			IsAggro = false;
			IsFrozen = false;
			FreezeTimeRemaining = 0f;
			SlowTimeRemaining = 0f;
			SlowMultiplier = 1f;
			FirstAttacker = null;
			_target = null;
			_attackCooldownRemaining = 0f;
			_attackAnimTimer = 0f;
			_healthRegenAccum = 0f;
			_repositionExtra = 0f;
			_state = MonsterState.Dead;
			GameObject.WorldPosition = _spawnPosition;

			_lastBroadcastMoving = false;
			_lastBroadcastRunning = false;
		}

		ModelRenderer?.Set( "b_death", false );
		ModelRenderer?.Set( "b_victory", false );
		ModelRenderer?.Set( "b_attack", false );
		ModelRenderer?.Set( "is_moving", false );
		ModelRenderer?.Set( "is_running", false );

		if ( MonsterCollider != null )
			MonsterCollider.Enabled = false;

		_localCulled = ShouldCullForDistance();
		EnterVisualPhase( VisualPhase.MaterializeBlink );
	}

	[Rpc.Broadcast]
	void BroadcastRespawnComplete()
	{
		if ( Networking.IsHost )
		{
			IsDead = false;
			IsAggro = false;
			_target = null;
			_state = MonsterState.Idle;
		}

		_localCulled = ShouldCullForDistance();
		EnterVisualPhase( VisualPhase.Solid );

		if ( MonsterCollider != null )
			MonsterCollider.Enabled = !_localCulled;
	}

	bool CheckAggro()
	{
		var players = Scene.GetAllComponents<PlayerController>();

		foreach ( var player in players )
		{
			var playerHealth = player.Components.Get<PlayerHealth>();
			if ( playerHealth != null && playerHealth.IsDead )
				continue;

			float dist = FlatDistance( WorldPosition, player.WorldPosition );
			if ( dist <= AggroRange )
			{
				EnterChase( player.GameObject );
				return true;
			}
		}

		return false;
	}

	void FaceTarget( Vector3 target )
	{
		GameObject.WorldRotation = Rotation.FromYaw( YawToFace( target ) );
	}

	void RotateToward( Vector3 target )
	{
		float targetYaw = YawToFace( target );
		float currentYaw = GameObject.WorldRotation.Yaw();
		float delta = NormalizeAngle( targetYaw - currentYaw );
		float step = SmoothTurnSpeed * Time.Delta;
		float move = MathF.Min( MathF.Abs( delta ), step ) * MathF.Sign( delta );
		GameObject.WorldRotation = Rotation.FromYaw( currentYaw + move );
	}

	float YawToFace( Vector3 target )
	{
		float dx = target.x - WorldPosition.x;
		float dy = target.y - WorldPosition.y;
		return MathF.Atan2( dy, dx ) * ( 180f / MathF.PI );
	}

	float GetAngleTo( Vector3 target )
	{
		float targetYaw = YawToFace( target );
		return MathF.Abs( NormalizeAngle( targetYaw - GameObject.WorldRotation.Yaw() ) );
	}

	float NormalizeAngle( float angle )
	{
		while ( angle > 180f ) angle -= 360f;
		while ( angle < -180f ) angle += 360f;
		return angle;
	}

	void MoveTowards( Vector3 target, float speed )
	{
		float dx = target.x - WorldPosition.x;
		float dy = target.y - WorldPosition.y;
		float len = MathF.Sqrt( dx * dx + dy * dy );

		if ( len < 0.01f )
			return;

		Vector3 moveDir = new Vector3( dx / len, dy / len, 0f );
		float moveDist = speed * Time.Delta;

		var trace = Scene.Trace
			.Ray( WorldPosition + Vector3.Up * 30f, WorldPosition + Vector3.Up * 30f + moveDir * moveDist )
			.Radius( 16f )
			.WithoutTags( "monster" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		Vector3 finalDir = moveDir;
		if ( trace.Hit )
		{
			Vector3 slideDir = Vector3.Cross( trace.Normal, Vector3.Up ).Normal;
			if ( Vector3.Dot( slideDir, moveDir ) < 0f )
				slideDir = -slideDir;
			if ( slideDir.Length > 0.01f )
				finalDir = slideDir;
		}

		GameObject.WorldPosition = new Vector3(
			WorldPosition.x + finalDir.x * moveDist,
			WorldPosition.y + finalDir.y * moveDist,
			WorldPosition.z );
	}

	void MoveForward( float speed )
	{
		Vector3 fwd = GameObject.WorldRotation.Forward;
		Vector3 moveDir = new Vector3( fwd.x, fwd.y, 0f );
		float moveLen = moveDir.Length;

		if ( moveLen < 0.01f )
			return;

		moveDir /= moveLen;
		float moveDist = speed * Time.Delta;

		var trace = Scene.Trace
			.Ray( WorldPosition + Vector3.Up * 30f, WorldPosition + Vector3.Up * 30f + moveDir * moveDist )
			.Radius( 16f )
			.WithoutTags( "monster" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		Vector3 finalDir = moveDir;
		if ( trace.Hit )
		{
			Vector3 slideDir = Vector3.Cross( trace.Normal, Vector3.Up ).Normal;
			if ( Vector3.Dot( slideDir, moveDir ) < 0f )
				slideDir = -slideDir;
			if ( slideDir.Length > 0.01f )
				finalDir = slideDir;
		}

		GameObject.WorldPosition = new Vector3(
			WorldPosition.x + finalDir.x * moveDist,
			WorldPosition.y + finalDir.y * moveDist,
			WorldPosition.z );
	}

	void SnapToGround()
	{
		var trace = Scene.Trace
			.Ray( WorldPosition + Vector3.Up * 100f, WorldPosition + Vector3.Down * 500f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "monster" )
			.Run();

		if ( trace.Hit )
			GameObject.WorldPosition = new Vector3( WorldPosition.x, WorldPosition.y, trace.HitPosition.z );
	}

	float FlatDistance( Vector3 a, Vector3 b )
	{
		float dx = a.x - b.x;
		float dy = a.y - b.y;
		return MathF.Sqrt( dx * dx + dy * dy );
	}

	void RegenerateHealth()
	{
		if ( CurrentHealth >= MaxHealth )
			return;

		_healthRegenAccum += ( MaxHealth / 2f ) * Time.Delta;
		int wholeRegen = (int)_healthRegenAccum;

		if ( wholeRegen > 0 )
		{
			CurrentHealth = Math.Min( CurrentHealth + wholeRegen, MaxHealth );
			_healthRegenAccum -= wholeRegen;
		}
	}

	void SetNearestWaypointAsCurrent()
	{
		if ( Waypoints == null || Waypoints.Count == 0 )
			return;

		float nearestDist = float.MaxValue;

		for ( int i = 0; i < Waypoints.Count; i++ )
		{
			if ( Waypoints[i] == null )
				continue;

			float dist = FlatDistance( WorldPosition, Waypoints[i].WorldPosition );
			if ( dist < nearestDist )
			{
				nearestDist = dist;
				_currentWaypoint = i;
			}
		}
	}

	static void LogLoot( ItemId item, int amount )
	{
		var def = ItemDatabase.Get( item );
		string name = def != null ? def.Name : item.ToString();
		GameLog.Add( $"You looted {amount}x {name}.", "#6db8f0" );
	}

	static void LogLootSplit( ItemId item, int placed, int banked )
	{
		var def = ItemDatabase.Get( item );
		string name = def != null ? def.Name : item.ToString();

		if ( placed > 0 )
			GameLog.Add( $"You looted {placed}x {name}.", "#6db8f0" );

		if ( banked > 0 )
			GameLog.Add( $"Inventory full — {banked}x {name} sent to your bank.", "#c9a84c" );
	}
}