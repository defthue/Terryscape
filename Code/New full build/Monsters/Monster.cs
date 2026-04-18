using Sandbox;
using System;
using System.Collections.Generic;

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

	[Property, Group( "Ranged" )] public bool IsRanged { get; set; } = false;
	[Property, Group( "Ranged" )] public GameObject ProjectilePrefab { get; set; }
	[Property, Group( "Ranged" )] public float ProjectileRange { get; set; } = 300f;
	[Property, Group( "Ranged" )] public float ProjectileSpeed { get; set; } = 400f;
	[Property, Group( "Ranged" )] public float ProjectileSpawnHeight { get; set; } = 50f;
	[Property, Group( "Ranged" )] public float ProjectileCastDelay { get; set; } = 0.3f;
	[Property, Group( "Ranged" )] public float ProjectileDamageDelay { get; set; } = 0.5f;

	[Property, Group( "Respawn" )] public float RespawnMin { get; set; } = 5f;
	[Property, Group( "Respawn" )] public float RespawnMax { get; set; } = 20f;

	[Property, Group( "Animations" )] public float AttackAnimLength { get; set; } = 1.0f;
	[Property, Group( "Animations" )] public float DamageDelay { get; set; } = 0.6f;
	[Property, Group( "Animations" )] public float DeathAnimLength { get; set; } = 2.0f;
	[Property, Group( "Animations" )] public float DeathLingerTime { get; set; } = 2.0f;
	[Property, Group( "Animations" )] public float VictoryAnimLength { get; set; } = 3.0f;

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
	bool _pendingDeath = false;
	bool _leashed = false;
	int _strafeDirection = 1;
	float _repositionExtra = 0f;

	protected override void OnStart()
	{
		_spawnPosition = GameObject.WorldPosition;
		CurrentHealth = MaxHealth;
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost )
			return;

		if ( IsDead )
			return;

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
		MoveForward( PatrolSpeed );
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

	void UpdateChasing()
	{
		if ( _target == null || !_target.IsValid() )
		{
			_target = null;
			IsAggro = false;
			StartReturning();
			return;
		}

		float dist = FlatDistance( WorldPosition, _target.WorldPosition );
		float distFromWaypoint = GetDistFromNearestWaypoint();

		if ( distFromWaypoint > LeashRange )
		{
			_target = null;
			IsAggro = false;
			_leashed = true;
			StartReturning();
			return;
		}

		if ( dist > AggroRange )
		{
			_target = null;
			IsAggro = false;
			StartReturning();
			return;
		}

		float effectiveAttackRange = IsRanged ? ProjectileRange : AttackRange;

		if ( dist <= effectiveAttackRange )
		{
			_state = MonsterState.Attacking;
			return;
		}

		FaceTarget( _target.WorldPosition );
		SetMoving( true, true );
		MoveTowards( _target.WorldPosition, ChaseSpeed );
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

		if ( _pendingDeath )
		{
			ExecuteDeath();
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

		float dist = FlatDistance( WorldPosition, _target.WorldPosition );

		if ( dist > AggroRange )
		{
			_target = null;
			IsAggro = false;
			StartReturning();
			return;
		}

		float distFromWaypoint = GetDistFromNearestWaypoint();
		if ( distFromWaypoint > LeashRange )
		{
			_target = null;
			IsAggro = false;
			_leashed = true;
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

		float moveDist = ChaseSpeed * Time.Delta;

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
		if ( CheckAggro() )
			return;

		RegenerateHealth();

		if ( Waypoints == null || Waypoints.Count == 0 )
		{
			_state = MonsterState.Idle;
			SetMoving( false, false );
			return;
		}

		var waypoint = Waypoints[_currentWaypoint];
		if ( waypoint == null )
		{
			_state = MonsterState.Idle;
			SetMoving( false, false );
			return;
		}

		float dist = FlatDistance( WorldPosition, waypoint.WorldPosition );

		if ( dist < 20f )
		{
			_healthRegenAccum = 0f;
			_leashed = false;
			_state = MonsterState.Patrolling;
			return;
		}

		FaceTarget( waypoint.WorldPosition );
		SetMoving( true, false );
		MoveTowards( waypoint.WorldPosition, PatrolSpeed );
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
		SetNearestWaypointAsCurrent();
		_state = MonsterState.Returning;
	}

	void SetMoving( bool moving, bool running )
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

		if ( _target == null && attacker != null )
		{
			_target = attacker;
			IsAggro = true;

			if ( _state != MonsterState.Attacking && _state != MonsterState.Chasing && _state != MonsterState.Repositioning )
				_state = MonsterState.Chasing;
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

		float armorValue = playerInventory != null ? CombatTriangle.GetEffectiveArmorValue( CombatStyle, playerInventory ) : 0f;
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
			DealRangedDamageDelayed( playerHealth, finalDamage );
			return;
		}

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

		if ( ModelRenderer != null && ModelRenderer.SceneModel != null )
		{
			var handTransform = ModelRenderer.SceneModel.GetBoneWorldTransform( "RightHand" );
			if ( handTransform.Position.Length > 0.01f )
				spawnPos = handTransform.Position;
		}

		Vector3 targetPos = targetObj.WorldPosition + Vector3.Up * 40f;
		Vector3 direction = ( targetPos - spawnPos ).Normal;
		spawnPos += direction * 20f;

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

	async void ResetAttackBool()
	{
		await Task.DelaySeconds( 0.1f );
		if ( IsValid )
			ModelRenderer?.Set( "b_attack", false );
	}

	void Die()
	{
		if ( _attackAnimTimer > 0f && _state == MonsterState.Attacking )
		{
			_pendingDeath = true;
			return;
		}

		ExecuteDeath();
	}

	void ExecuteDeath()
	{
		_pendingDeath = false;
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

		var inventory = FirstAttacker.Components.Get<Inventory>();
		var skills = FirstAttacker.Components.Get<Skills>();

		inventory?.AddKill( MonsterType );
		skills?.AddCombatXp( GetKillerSkill(), CombatXpReward );

		if ( inventory == null )
			return;

		var rng = new Random();
		TryDropLoot( inventory, rng, LootItem1, LootAmount1, LootChance1 );
		TryDropLoot( inventory, rng, LootItem2, LootAmount2, LootChance2 );
		TryDropLoot( inventory, rng, LootItem3, LootAmount3, LootChance3 );
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

	void TryDropLoot( Inventory inventory, Random rng, ItemId itemId, int amount, float chance )
	{
		if ( itemId == ItemId.None || chance <= 0f )
			return;

		if ( (float)( rng.NextDouble() * 100.0 ) < chance )
			inventory.AddItem( itemId, amount );
	}

	[Rpc.Broadcast]
	void BroadcastDeath()
	{
		if ( MonsterCollider != null )
			MonsterCollider.Enabled = false;

		ModelRenderer?.Set( "b_death", true );
		ModelRenderer?.Set( "is_moving", false );
		ModelRenderer?.Set( "is_running", false );
		HideAfterDeath();
	}

	async void HideAfterDeath()
	{
		int gen = _respawnGeneration;
		await Task.DelaySeconds( DeathAnimLength + DeathLingerTime );

		if ( !IsValid || _respawnGeneration != gen )
			return;

		if ( ModelRenderer != null )
			ModelRenderer.Enabled = false;

		if ( MonsterCollider != null )
			MonsterCollider.Enabled = false;
	}

	async void StartRespawnTimer( int generation )
	{
		float delay = RespawnMin + (float)( new Random().NextDouble() * ( RespawnMax - RespawnMin ) );
		await Task.DelaySeconds( DeathAnimLength + DeathLingerTime + delay );

		if ( !IsValid || _respawnGeneration != generation )
			return;

		BroadcastRespawn();
	}

	[Rpc.Broadcast]
	void BroadcastRespawn()
	{
		if ( Networking.IsHost )
		{
			CurrentHealth = MaxHealth;
			IsDead = false;
			IsAggro = false;
			IsFrozen = false;
			FreezeTimeRemaining = 0f;
			FirstAttacker = null;
			_target = null;
			_attackCooldownRemaining = 0f;
			_attackAnimTimer = 0f;
			_healthRegenAccum = 0f;
			_pendingDeath = false;
			_leashed = false;
			_repositionExtra = 0f;
			_state = MonsterState.Idle;
			GameObject.WorldPosition = _spawnPosition;
		}

		ModelRenderer?.Set( "b_death", false );
		ModelRenderer?.Set( "b_victory", false );
		ModelRenderer?.Set( "b_attack", false );
		ModelRenderer?.Set( "is_moving", false );
		ModelRenderer?.Set( "is_running", false );

		if ( ModelRenderer != null )
			ModelRenderer.Enabled = true;

		if ( MonsterCollider != null )
			MonsterCollider.Enabled = true;
	}

	bool CheckAggro()
	{
		if ( _leashed )
			return false;

		var players = Scene.GetAllComponents<PlayerController>();

		foreach ( var player in players )
		{
			var playerHealth = player.Components.Get<PlayerHealth>();
			if ( playerHealth != null && playerHealth.IsDead )
				continue;

			float dist = FlatDistance( WorldPosition, player.WorldPosition );
			if ( dist <= AggroRange )
			{
				_target = player.GameObject;
				IsAggro = true;
				_state = MonsterState.Chasing;
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

	float GetDistFromNearestWaypoint()
	{
		if ( Waypoints == null || Waypoints.Count == 0 )
			return 0f;

		float nearestDist = float.MaxValue;

		foreach ( var wp in Waypoints )
		{
			if ( wp == null )
				continue;

			float dist = FlatDistance( WorldPosition, wp.WorldPosition );
			if ( dist < nearestDist )
				nearestDist = dist;
		}

		return nearestDist;
	}
}