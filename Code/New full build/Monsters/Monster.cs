using Sandbox;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public enum MonsterMovementMode
{
	Roam,
	Patrol,
	Sentinel
}

public enum MonsterSoundProfile
{
	Small,
	Large
}

public enum MonsterRoamShape
{
	Circle,
	Rectangle,
	Ellipse
}

public sealed class Monster : Component
{
	[Property, Group( "Identity" )] public string MonsterName { get; set; } = "Goblin";
	[Property, Group( "Identity" )] public string MonsterType { get; set; } = "Goblin";
	[Property, Group( "Identity" )] public CombatStyle CombatStyle { get; set; } = CombatStyle.Melee;
	[Property, Group( "Identity" )] public MonsterSoundProfile SoundProfile { get; set; } = MonsterSoundProfile.Small;

	[Property, Group( "Stats" )] public int MaxHealth { get; set; } = 100;
	[Property, Group( "Stats" )] public int Damage { get; set; } = 10;
	[Property, Group( "Stats" )] public float AttackCooldown { get; set; } = 2f;
	[Property, Group( "Stats" )] public int CombatXpReward { get; set; } = 50;

	[Property, Group( "Movement" )] public MonsterMovementMode MovementMode { get; set; } = MonsterMovementMode.Roam;
	[Property, Group( "Movement" )] public MonsterRoamShape RoamShape { get; set; } = MonsterRoamShape.Circle;
	[Property, Group( "Movement" )] public float PatrolSpeed { get; set; } = 100f;
	[Property, Group( "Movement" )] public float ChaseSpeed { get; set; } = 150f;
	[Property, Group( "Movement" )] public float SmoothTurnSpeed { get; set; } = 360f;
	[Property, Group( "Movement" )] public float RoamRadius { get; set; } = 300f;
	[Property, Group( "Movement" )] public float RoamExtentX { get; set; } = 300f;
	[Property, Group( "Movement" )] public float RoamExtentY { get; set; } = 300f;
	[Property, Group( "Movement" )] public float RoamIdleMin { get; set; } = 2f;
	[Property, Group( "Movement" )] public float RoamIdleMax { get; set; } = 6f;
	[Property, Group( "Movement" )] public List<GameObject> PatrolPoints { get; set; } = new();

	[Property, Group( "Combat Ranges" )] public float AggroRange { get; set; } = 400f;
	[Property, Group( "Combat Ranges" )] public float LeashRange { get; set; } = 800f;
	[Property, Group( "Combat Ranges" )] public float AttackRange { get; set; } = 80f;
	[Property, Group( "Combat Ranges" )] public float VerticalHitTolerance { get; set; } = 80f;
	[Property, Group( "Combat Ranges" )] public float LeashNoExchangeTime { get; set; } = 8f;
	[Property, Group( "Combat Ranges" )] public float LeashNoHitChaseTime { get; set; } = 30f;

	[Property, Group( "Evade" )] public float EvadeGraceDuration { get; set; } = 4f;
	[Property, Group( "Evade" )] public float EvadeHealPercentPerSecond { get; set; } = 9f;

	[Property, Group( "Ranged" )] public bool IsRanged { get; set; } = false;
	[Property, Group( "Ranged" )] public GameObject ProjectilePrefab { get; set; }
	[Property, Group( "Ranged" )] public float ProjectileRange { get; set; } = 300f;
	[Property, Group( "Ranged" )] public float ProjectileSpeed { get; set; } = 400f;
	[Property, Group( "Ranged" )] public float ProjectileSpawnHeight { get; set; } = 50f;
	[Property, Group( "Ranged" )] public float ProjectileCastDelay { get; set; } = 0.3f;
	[Property, Group( "Ranged" )] public float ProjectileDamageDelay { get; set; } = 0.5f;
	[Property, Group( "Ranged" )] public string ProjectileSpawnBone { get; set; } = "RightHand";
	[Property, Group( "Ranged" )] public float ProjectileForwardOffset { get; set; } = 20f;

	[Property, Group( "Loot" )] public LootTable LootTable { get; set; }

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

	[Property, Group( "Performance" )] public float ActivationRange { get; set; } = 2500f;
	[Property, Group( "Performance" )] public float DrawDistanceMax { get; set; } = 5000f;

	[Sync] public int CurrentHealth { get; set; }
	[Sync] public bool IsDead { get; set; }
	[Sync] public bool IsAggro { get; set; }
	[Sync] public bool IsEvading { get; set; }
	[Sync] public bool IsFrozen { get; set; }
	[Sync] public float FreezeTimeRemaining { get; set; }
	[Sync] public float SlowTimeRemaining { get; set; }
	[Sync] public float SlowMultiplier { get; set; } = 1f;
	[Sync] public GameObject FirstAttacker { get; set; }

	public SkinnedModelRenderer ModelRenderer { get; private set; }
	public Collider MonsterCollider { get; private set; }

	enum MonsterState { Dormant, Idle, Roaming, Patrolling, Chasing, Attacking, Evading, Victory, Dead }

	MonsterState _state = MonsterState.Dormant;
	MonsterMover _mover;
	Vector3 _spawnPosition;
	float _spawnYaw;
	GameObject _target;
	int _currentPatrolPoint;
	List<Vector3> _patrolPositions = new();
	float _idleTimer;
	float _legDeadline;
	float _attackCooldownRemaining;
	float _attackAnimTimer;
	float _victoryTimer;
	float _healAccum;
	float _evadeGraceRemaining;
	int _respawnGeneration;
	float _nextThinkGateTime;
	float _thinkGateOffset;

	float _lastDamageExchangeTime = -100f;
	float _chaseStartTime = -100f;
	bool _hasLandedHitDuringChase;

	bool _lastBroadcastMoving;
	bool _lastBroadcastRunning;

	bool _localCulled;
	float _nextCullCheckTime;
	bool _navTilesRequested;

	enum VisualPhase { Solid, Dying, DespawnBlink, Hidden, MaterializeBlink }
	VisualPhase _visualPhase = VisualPhase.Solid;
	float _phaseTimer;
	float _blinkAccum;
	bool _blinkVisible = true;

	const float GlobalSpeedScale = 1.4f;
	const float MovingSpeedThreshold = 5f;

	protected override void OnStart()
	{
		GameObject.Tags.Add( "monster" );

		ModelRenderer = Components.GetInDescendantsOrSelf<SkinnedModelRenderer>();
		MonsterCollider = Components.GetInDescendantsOrSelf<Collider>();

		_spawnPosition = GameObject.WorldPosition;
		_spawnYaw = GameObject.WorldRotation.Yaw();

		if ( PatrolPoints != null )
		{
			foreach ( var point in PatrolPoints )
			{
				if ( point != null && point.IsValid() )
					_patrolPositions.Add( point.WorldPosition );
			}
		}
		_thinkGateOffset = Random.Shared.NextSingle() * 0.5f;
		_nextCullCheckTime = Time.Now + Random.Shared.NextSingle() * 0.5f;

		if ( Networking.IsHost )
		{
			CurrentHealth = MaxHealth;
			_mover = Components.GetOrCreate<MonsterMover>();
		}

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

		TickStatusTimers();

		if ( IsFrozen )
		{
			_mover?.Stop();
			SetMoving( false, false );
			return;
		}

		switch ( _state )
		{
			case MonsterState.Dormant: UpdateDormant(); break;
			case MonsterState.Idle: UpdateIdle(); break;
			case MonsterState.Roaming: UpdateRoaming(); break;
			case MonsterState.Patrolling: UpdatePatrolling(); break;
			case MonsterState.Chasing: UpdateChasing(); break;
			case MonsterState.Attacking: UpdateAttacking(); break;
			case MonsterState.Evading: UpdateEvading(); break;
			case MonsterState.Victory: UpdateVictory(); break;
		}

		UpdateLocomotionVisuals();
	}

	void TickStatusTimers()
	{
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
		}
	}

	bool ThinkGateReady()
	{
		if ( Time.Now < _nextThinkGateTime )
			return false;

		_nextThinkGateTime = Time.Now + 0.5f + _thinkGateOffset * 0.2f;
		return true;
	}

	bool AnyPlayerWithinActivation()
	{
		float rangeSqr = ActivationRange * ActivationRange;

		foreach ( var player in Scene.GetAllComponents<PlayerController>() )
		{
			if ( player == null || !player.IsValid() )
				continue;

			if ( ( player.WorldPosition - WorldPosition ).LengthSquared <= rangeSqr )
				return true;
		}

		return false;
	}

	void UpdateDormant()
	{
		SetMoving( false, false );

		if ( !ThinkGateReady() )
			return;

		if ( AnyPlayerWithinActivation() )
		{
			EnsureNavTiles();
			_mover?.Teleport( WorldPosition );
			_state = MonsterState.Idle;
		}
	}

	void EnsureNavTiles()
	{
		if ( _navTilesRequested )
			return;

		_navTilesRequested = true;

		float areaReach = 0f;

		if ( MovementMode == MonsterMovementMode.Roam )
		{
			areaReach = RoamShape == MonsterRoamShape.Circle
				? RoamRadius
				: MathF.Max( RoamExtentX, RoamExtentY );
		}

		float margin = areaReach + LeashBeyond() + 512f;

		Vector3 mins = _spawnPosition;
		Vector3 maxs = _spawnPosition;

		if ( MovementMode == MonsterMovementMode.Patrol )
		{
			foreach ( var p in _patrolPositions )
			{
				mins = Vector3.Min( mins, p );
				maxs = Vector3.Max( maxs, p );
			}
		}

		var bounds = new BBox( mins - new Vector3( margin, margin, margin ), maxs + new Vector3( margin, margin, margin ) );
		Scene.NavMesh.GenerateTiles( Scene.PhysicsWorld, bounds );
	}

	bool TrySleep()
	{
		if ( !ThinkGateReady() )
			return false;

		if ( AnyPlayerWithinActivation() )
			return false;

		_mover?.Stop();
		SetMoving( false, false );
		_state = MonsterState.Dormant;
		return true;
	}

	void UpdateIdle()
	{
		SetMoving( false, false );

		if ( CheckAggro() )
			return;

		if ( TrySleep() )
			return;

		if ( MovementMode == MonsterMovementMode.Sentinel )
			return;

		_idleTimer -= Time.Delta;
		if ( _idleTimer > 0f )
			return;

		if ( MovementMode == MonsterMovementMode.Patrol && _patrolPositions.Count > 0 )
		{
			_legDeadline = 0f;
			_state = MonsterState.Patrolling;
			return;
		}

		StartRoamLeg();
	}

	void StartRoamLeg()
	{
		Vector3 destination = PickRoamPoint();
		float speed = PatrolSpeed * GetSpeedMultiplier();
		_mover.MoveTo( destination, speed );
		_legDeadline = Time.Now + LegTravelTime( destination, speed );
		_state = MonsterState.Roaming;
	}

	float LegTravelTime( Vector3 destination, float speed )
	{
		float dist = FlatDistance( WorldPosition, destination );
		return dist / MathF.Max( 10f, speed ) + 4f;
	}

	Vector3 PickRoamPoint()
	{
		for ( int i = 0; i < 6; i++ )
		{
			Vector3 candidate = RandomPointInRoamArea();
			var snapped = Scene.NavMesh.GetClosestPoint( candidate );
			if ( !snapped.HasValue )
				continue;

			if ( DistanceOutsideRoamArea( snapped.Value ) <= 0f )
				return snapped.Value;
		}

		return _spawnPosition;
	}

	Vector3 RandomPointInRoamArea()
	{
		switch ( RoamShape )
		{
			case MonsterRoamShape.Rectangle:
				return FromShapeLocal( new Vector2(
					Game.Random.Float( -RoamExtentX, RoamExtentX ),
					Game.Random.Float( -RoamExtentY, RoamExtentY ) ) );

			case MonsterRoamShape.Ellipse:
			{
				float a = Game.Random.Float( 0f, MathF.PI * 2f );
				float r = MathF.Sqrt( Game.Random.Float( 0f, 1f ) );
				return FromShapeLocal( new Vector2( MathF.Cos( a ) * r * RoamExtentX, MathF.Sin( a ) * r * RoamExtentY ) );
			}

			default:
			{
				float a = Game.Random.Float( 0f, MathF.PI * 2f );
				float r = RoamRadius * MathF.Sqrt( Game.Random.Float( 0f, 1f ) );
				return _spawnPosition + new Vector3( MathF.Cos( a ) * r, MathF.Sin( a ) * r, 0f );
			}
		}
	}

	Vector2 ToShapeLocal( Vector3 worldPos )
	{
		float rad = -_spawnYaw * ( MathF.PI / 180f );
		float dx = worldPos.x - _spawnPosition.x;
		float dy = worldPos.y - _spawnPosition.y;
		float c = MathF.Cos( rad );
		float s = MathF.Sin( rad );
		return new Vector2( dx * c - dy * s, dx * s + dy * c );
	}

	Vector3 FromShapeLocal( Vector2 local )
	{
		float rad = _spawnYaw * ( MathF.PI / 180f );
		float c = MathF.Cos( rad );
		float s = MathF.Sin( rad );
		return _spawnPosition + new Vector3( local.x * c - local.y * s, local.x * s + local.y * c, 0f );
	}

	float DistanceOutsideRoamArea( Vector3 worldPos )
	{
		if ( RoamShape == MonsterRoamShape.Circle )
			return FlatDistance( worldPos, _spawnPosition ) - RoamRadius;

		Vector2 p = ToShapeLocal( worldPos );

		if ( RoamShape == MonsterRoamShape.Rectangle )
		{
			float ox = MathF.Abs( p.x ) - RoamExtentX;
			float oy = MathF.Abs( p.y ) - RoamExtentY;
			float cx = MathF.Max( ox, 0f );
			float cy = MathF.Max( oy, 0f );
			float outside = MathF.Sqrt( cx * cx + cy * cy );
			return outside > 0f ? outside : MathF.Max( ox, oy );
		}

		float ex = MathF.Max( 1f, RoamExtentX );
		float ey = MathF.Max( 1f, RoamExtentY );
		float nx = p.x / ex;
		float ny = p.y / ey;
		float k = MathF.Sqrt( nx * nx + ny * ny );

		if ( k <= 0.0001f )
			return -MathF.Min( ex, ey );

		float len = MathF.Sqrt( p.x * p.x + p.y * p.y );
		return len * ( 1f - 1f / k );
	}

	void UpdateRoaming()
	{
		if ( CheckAggro() )
			return;

		RotateTowardVelocity();

		if ( _mover.HasArrived() || Time.Now >= _legDeadline )
		{
			_mover.Stop();
			_idleTimer = Game.Random.Float( RoamIdleMin, RoamIdleMax );
			_state = MonsterState.Idle;
		}
	}

	void UpdatePatrolling()
	{
		if ( CheckAggro() )
			return;

		if ( _patrolPositions.Count == 0 )
		{
			_state = MonsterState.Idle;
			return;
		}

		if ( _currentPatrolPoint >= _patrolPositions.Count )
			_currentPatrolPoint = 0;

		Vector3 target = _patrolPositions[_currentPatrolPoint];
		float speed = PatrolSpeed * GetSpeedMultiplier();

		if ( _legDeadline <= 0f )
			_legDeadline = Time.Now + LegTravelTime( target, speed );

		_mover.MoveTo( target, speed );
		RotateTowardVelocity();

		if ( _mover.HasArrived() || Time.Now >= _legDeadline )
		{
			AdvancePatrolPoint();
			_idleTimer = Game.Random.Float( RoamIdleMin, RoamIdleMax ) * 0.5f;
			_state = MonsterState.Idle;
		}
	}

	void AdvancePatrolPoint()
	{
		_currentPatrolPoint = ( _currentPatrolPoint + 1 ) % Math.Max( 1, _patrolPositions.Count );
		_legDeadline = 0f;
	}

	float LeashBeyond()
	{
		return MathF.Max( LeashRange, 32f );
	}

	bool WithinLeashBoundary( Vector3 pos )
	{
		if ( MovementMode == MonsterMovementMode.Roam )
			return DistanceOutsideRoamArea( pos ) <= LeashBeyond();

		if ( MovementMode == MonsterMovementMode.Patrol && _patrolPositions.Count > 0 )
			return DistanceToPatrolRoute( pos ) <= LeashBeyond();

		return FlatDistance( pos, _spawnPosition ) <= LeashBeyond();
	}

	float DistanceToPatrolRoute( Vector3 pos )
	{
		if ( _patrolPositions.Count == 1 )
			return FlatDistance( pos, _patrolPositions[0] );

		float best = float.MaxValue;

		for ( int i = 0; i < _patrolPositions.Count; i++ )
		{
			Vector3 a = _patrolPositions[i];
			Vector3 b = _patrolPositions[( i + 1 ) % _patrolPositions.Count];
			float d = DistancePointToSegment2D( pos, a, b );
			if ( d < best )
				best = d;
		}

		return best;
	}

	float DistancePointToSegment2D( Vector3 p, Vector3 a, Vector3 b )
	{
		float abx = b.x - a.x;
		float aby = b.y - a.y;
		float apx = p.x - a.x;
		float apy = p.y - a.y;
		float lenSqr = abx * abx + aby * aby;
		float t = lenSqr < 0.0001f ? 0f : Math.Clamp( ( apx * abx + apy * aby ) / lenSqr, 0f, 1f );
		float cx = a.x + abx * t - p.x;
		float cy = a.y + aby * t - p.y;
		return MathF.Sqrt( cx * cx + cy * cy );
	}

	bool ShouldLeash()
	{
		if ( _target == null || !_target.IsValid() )
			return true;

		if ( !WithinLeashBoundary( WorldPosition ) )
			return true;

		float sinceExchange = Time.Now - _lastDamageExchangeTime;
		if ( sinceExchange > LeashNoExchangeTime && !HasLineOfSight( _target ) )
			return true;

		float sinceChaseStart = Time.Now - _chaseStartTime;
		if ( sinceChaseStart > LeashNoHitChaseTime && !_hasLandedHitDuringChase )
			return true;

		return false;
	}

	bool TargetInvalidOrDead()
	{
		if ( _target == null || !_target.IsValid() )
			return true;

		var health = _target.Components.Get<PlayerHealth>();
		return health == null || health.IsDead;
	}

	void UpdateChasing()
	{
		if ( TargetInvalidOrDead() || ShouldLeash() )
		{
			StartEvade();
			return;
		}

		if ( CanAttackTarget() )
		{
			_mover.Stop();
			_state = MonsterState.Attacking;
			return;
		}

		_mover.MoveTo( _target.WorldPosition, ChaseSpeed * GetSpeedMultiplier() );
		RotateTowardVelocity();
	}

	bool CanAttackTarget()
	{
		if ( _target == null || !_target.IsValid() )
			return false;

		float flatDist = FlatDistance( WorldPosition, _target.WorldPosition );
		float verticalDist = MathF.Abs( _target.WorldPosition.z - WorldPosition.z );
		float effectiveRange = IsRanged ? ProjectileRange : AttackRange;

		if ( flatDist > effectiveRange )
			return false;

		if ( verticalDist > VerticalHitTolerance )
			return false;

		return HasLineOfSight( _target );
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

		if ( TargetInvalidOrDead() || ShouldLeash() )
		{
			StartEvade();
			return;
		}

		float flatDist = FlatDistance( WorldPosition, _target.WorldPosition );
		float effectiveRange = IsRanged ? ProjectileRange : AttackRange;

		if ( flatDist > effectiveRange * 1.3f || !CanAttackTargetLenient() )
		{
			_attackCooldownRemaining = 0f;
			_state = MonsterState.Chasing;
			return;
		}

		FaceTarget( _target.WorldPosition );

		if ( _attackCooldownRemaining > 0f )
		{
			_attackCooldownRemaining -= Time.Delta;
			return;
		}

		PerformAttack();
	}

	bool CanAttackTargetLenient()
	{
		if ( _target == null || !_target.IsValid() )
			return false;

		float verticalDist = MathF.Abs( _target.WorldPosition.z - WorldPosition.z );
		if ( verticalDist > VerticalHitTolerance * 1.3f )
			return false;

		return HasLineOfSight( _target );
	}

	void StartEvade()
	{
		_target = null;
		IsAggro = false;
		IsEvading = true;
		_evadeGraceRemaining = EvadeGraceDuration;
		_healAccum = 0f;
		float speed = ChaseSpeed * GetSpeedMultiplier();
		_legDeadline = Time.Now + LegTravelTime( _spawnPosition, speed ) * 1.5f;
		_state = MonsterState.Evading;
	}

	void UpdateEvading()
	{
		if ( _evadeGraceRemaining > 0f )
		{
			_evadeGraceRemaining -= Time.Delta;
			HealPercentPerSecond( EvadeHealPercentPerSecond );

			if ( _evadeGraceRemaining <= 0f )
				CurrentHealth = MaxHealth;
		}

		if ( Time.Now >= _legDeadline )
		{
			_mover.Teleport( _spawnPosition );
			FinishEvade();
			return;
		}

		_mover.MoveTo( _spawnPosition, ChaseSpeed * GetSpeedMultiplier() );
		RotateTowardVelocity();

		bool arrived = _mover.HasArrived( 64f );
		bool closeAndStopped = FlatDistance( WorldPosition, _spawnPosition ) <= 128f && _mover.Speed < MovingSpeedThreshold;

		if ( arrived || closeAndStopped )
			FinishEvade();
	}

	void FinishEvade()
	{
		_mover.Stop();
		IsEvading = false;
		CurrentHealth = MaxHealth;
		_healAccum = 0f;
		_evadeGraceRemaining = 0f;
		_idleTimer = Game.Random.Float( RoamIdleMin, RoamIdleMax );
		_state = MonsterState.Idle;
	}

	void HealPercentPerSecond( float percent )
	{
		if ( CurrentHealth >= MaxHealth )
			return;

		_healAccum += MaxHealth * ( percent / 100f ) * Time.Delta;
		int whole = (int)_healAccum;

		if ( whole > 0 )
		{
			CurrentHealth = Math.Min( CurrentHealth + whole, MaxHealth );
			_healAccum -= whole;
		}
	}

	void UpdateVictory()
	{
		SetMoving( false, false );
		_victoryTimer -= Time.Delta;

		if ( _victoryTimer <= 0f )
		{
			ModelRenderer?.Set( "b_victory", false );
			StartEvade();
		}
	}

	bool CheckAggro()
	{
		float rangeSqr = AggroRange * AggroRange;

		foreach ( var player in Scene.GetAllComponents<PlayerController>() )
		{
			if ( player == null || !player.IsValid() )
				continue;

			var playerHealth = player.Components.Get<PlayerHealth>();
			if ( playerHealth != null && playerHealth.IsDead )
				continue;

			if ( ( player.WorldPosition - WorldPosition ).LengthSquared > rangeSqr )
				continue;

			if ( !HasLineOfSight( player.GameObject ) )
				continue;

			EnterChase( player.GameObject );
			return true;
		}

		return false;
	}

	void EnterChase( GameObject target )
	{
		bool isNewEngagement = _state != MonsterState.Chasing && _state != MonsterState.Attacking;

		_target = target;
		IsAggro = true;
		IsEvading = false;
		_state = MonsterState.Chasing;

		if ( isNewEngagement )
		{
			_chaseStartTime = Time.Now;
			_hasLandedHitDuringChase = false;
			_lastDamageExchangeTime = Time.Now;
		}
	}

	bool HasLineOfSight( GameObject target )
	{
		if ( target == null || !target.IsValid() )
			return false;

		Vector3 eyePos = WorldPosition + Vector3.Up * MathF.Max( ProjectileSpawnHeight, 48f );
		Vector3 targetPos = target.WorldPosition + Vector3.Up * 40f;

		var trace = Scene.Trace
			.Ray( eyePos, targetPos )
			.WithoutTags( "monster", "boss", "player", "pickup" )
			.IgnoreGameObjectHierarchy( GameObject )
			.IgnoreGameObjectHierarchy( target )
			.Run();

		return !trace.Hit;
	}

	void RotateTowardVelocity()
	{
		Vector3 vel = _mover.Velocity.WithZ( 0f );
		if ( vel.Length < MovingSpeedThreshold )
			return;

		RotateTowardYaw( MathF.Atan2( vel.y, vel.x ) * ( 180f / MathF.PI ) );
	}

	void RotateTowardYaw( float targetYaw )
	{
		float currentYaw = GameObject.WorldRotation.Yaw();
		float delta = NormalizeAngle( targetYaw - currentYaw );
		float step = SmoothTurnSpeed * Time.Delta;
		float move = MathF.Min( MathF.Abs( delta ), step ) * MathF.Sign( delta );
		GameObject.WorldRotation = Rotation.FromYaw( currentYaw + move );
	}

	void FaceTarget( Vector3 target )
	{
		float dx = target.x - WorldPosition.x;
		float dy = target.y - WorldPosition.y;
		GameObject.WorldRotation = Rotation.FromYaw( MathF.Atan2( dy, dx ) * ( 180f / MathF.PI ) );
	}

	float NormalizeAngle( float angle )
	{
		while ( angle > 180f ) angle -= 360f;
		while ( angle < -180f ) angle += 360f;
		return angle;
	}

	float FlatDistance( Vector3 a, Vector3 b )
	{
		float dx = a.x - b.x;
		float dy = a.y - b.y;
		return MathF.Sqrt( dx * dx + dy * dy );
	}

	public float GetSpeedMultiplier()
	{
		if ( SlowTimeRemaining > 0f )
			return SlowMultiplier * GlobalSpeedScale;
		return GlobalSpeedScale;
	}

	void UpdateLocomotionVisuals()
	{
		bool moving = _mover != null && _mover.Speed > MovingSpeedThreshold;
		bool running = moving && ( _state == MonsterState.Chasing || _state == MonsterState.Evading );

		if ( _state == MonsterState.Attacking || _state == MonsterState.Victory || _state == MonsterState.Dormant )
			return;

		SetMoving( moving, running );
	}

	void SetMoving( bool moving, bool running )
	{
		ModelRenderer?.Set( "is_moving", moving );
		ModelRenderer?.Set( "is_running", running );
		ApplyMovePlaybackRate( moving );

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
		ApplyMovePlaybackRate( moving );
	}

	void ApplyMovePlaybackRate( bool moving )
	{
		if ( ModelRenderer != null )
			ModelRenderer.PlaybackRate = moving ? GlobalSpeedScale : 1f;
	}

	[Rpc.Host]
	public void TakeDamage( int damage, GameObject attacker )
	{
		if ( IsDead )
			return;

		if ( IsEvading )
		{
			if ( _evadeGraceRemaining <= 0f )
				return;

			if ( attacker == null || !attacker.IsValid() )
				return;

			if ( !WithinLeashBoundary( attacker.WorldPosition ) )
				return;

			ApplyDamageInternal( damage, attacker );

			if ( !IsDead )
				EnterChase( attacker );

			return;
		}

		if ( attacker != null && _state != MonsterState.Evading )
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

		ApplyDamageInternal( damage, attacker );
	}

	void ApplyDamageInternal( int damage, GameObject attacker )
	{
		if ( FirstAttacker == null && attacker != null )
			FirstAttacker = attacker;

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

		float armorValue = playerInventory != null ? CombatTriangle.GetEffectiveArmorValue( playerInventory ) : 0f;
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
			SpawnProjectileVisual( _target );
			PlayRangedAttackSoundDelayed();
			DealRangedDamageDelayed( playerHealth, finalDamage );
			return;
		}

		PlayMeleeAttackSoundDelayed();
		DealDamageDelayed( playerHealth, finalDamage );
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

		if ( !HasLineOfSight( playerHealth.GameObject ) )
			return;

		bool willKill = playerHealth.CurrentHealth - damage <= 0;
		playerHealth.TakeDamage( damage );

		_lastDamageExchangeTime = Time.Now;
		_hasLandedHitDuringChase = true;

		DamagePopupBroadcaster.Broadcast( playerHealth.WorldPosition + Vector3.Up * 60f, damage, playerHealth.MaxHealth, false );

		if ( willKill )
			StartVictory();
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
			StartVictory();
	}

	void StartVictory()
	{
		ModelRenderer?.Set( "b_victory", true );
		_victoryTimer = VictoryAnimLength;
		_state = MonsterState.Victory;
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

	async void PlayMeleeAttackSoundDelayed()
	{
		bool isLarge = SoundProfile == MonsterSoundProfile.Large;
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

	void Die()
	{
		IsDead = true;
		IsAggro = false;
		IsEvading = false;
		_target = null;
		_state = MonsterState.Dead;
		_mover?.Stop();

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
		int gold = 0;
		var rolledItems = new List<ItemId>();
		var rolledAmounts = new List<int>();

		if ( LootTable != null )
		{
			gold = LootTable.RollGoldPool( rng );

			var entries = LootTable.Entries ?? new List<LootEntry>();
			foreach ( var entry in entries )
			{
				if ( entry == null || entry.Item == ItemId.None || entry.ChancePercent <= 0f )
					continue;

				if ( (float)( rng.NextDouble() * 100.0 ) >= entry.ChancePercent )
					continue;

				int amount = LootTable.RollEntryAmount( rng, entry );
				if ( amount <= 0 )
					continue;

				rolledItems.Add( entry.Item );
				rolledAmounts.Add( amount );
			}
		}

		BroadcastReward( killerSteamId, MonsterType, killerSkill, CombatXpReward, gold, rolledItems.ToArray(), rolledAmounts.ToArray() );
	}

	[Rpc.Broadcast]
	void BroadcastReward( ulong killerSteamId, string monsterType, SkillType killerSkill, int xpReward, int gold, ItemId[] items, int[] amounts )
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

			if ( gold > 0 )
			{
				var (placedGold, bankedGold) = inventory.AddItemOrBank( ItemId.GoldCoin, gold );
				if ( placedGold > 0 )
					GameLog.Add( $"You looted {placedGold} gold.", "#f0c040" );
				if ( bankedGold > 0 )
					GameLog.Add( $"Inventory full — {bankedGold} gold sent to your bank.", "#c9a84c" );
				if ( placedGold > 0 || bankedGold > 0 )
					gainedAnyItem = true;
			}

			int len = Math.Min( items.Length, amounts.Length );
			for ( int i = 0; i < len; i++ )
			{
				var id = items[i];
				int amt = amounts[i];
				if ( id == ItemId.None || amt <= 0 )
					continue;

				var (placed, banked) = inventory.AddItemOrBank( id, amt );
				if ( placed > 0 || banked > 0 )
				{
					ItemPickupEffect.Trigger( id );
					LogLootSplit( id, placed, banked );
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
		foreach ( var player in Scene.GetAllComponents<PlayerController>() )
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
		ApplyMovePlaybackRate( false );

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
			IsEvading = false;
			IsFrozen = false;
			FreezeTimeRemaining = 0f;
			SlowTimeRemaining = 0f;
			SlowMultiplier = 1f;
			FirstAttacker = null;
			_target = null;
			_attackCooldownRemaining = 0f;
			_attackAnimTimer = 0f;
			_healAccum = 0f;
			_evadeGraceRemaining = 0f;
			_state = MonsterState.Dead;

			if ( _mover != null )
				_mover.Teleport( _spawnPosition );
			else
				GameObject.WorldPosition = _spawnPosition;

			_lastBroadcastMoving = false;
			_lastBroadcastRunning = false;
		}

		ModelRenderer?.Set( "b_death", false );
		ModelRenderer?.Set( "b_victory", false );
		ModelRenderer?.Set( "b_attack", false );
		ModelRenderer?.Set( "is_moving", false );
		ModelRenderer?.Set( "is_running", false );
		ApplyMovePlaybackRate( false );

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
			_state = MonsterState.Dormant;
		}

		_localCulled = ShouldCullForDistance();
		EnterVisualPhase( VisualPhase.Solid );

		if ( MonsterCollider != null )
			MonsterCollider.Enabled = !_localCulled;
	}

	bool ShouldCullForDistance()
	{
		if ( DrawDistanceMax <= 0f )
			return false;

		var camera = Scene.Camera;
		if ( camera == null )
			return false;

		float sqrDist = ( WorldPosition - camera.WorldPosition ).LengthSquared;
		return sqrDist > DrawDistanceMax * DrawDistanceMax;
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

	static void LogLootSplit( ItemId item, int placed, int banked )
	{
		var def = ItemDatabase.Get( item );
		string name = def != null ? def.Name : item.ToString();

		if ( placed > 0 )
			GameLog.Add( $"You looted {placed}x {name}.", "#6db8f0" );

		if ( banked > 0 )
			GameLog.Add( $"Inventory full — {banked}x {name} sent to your bank.", "#c9a84c" );
	}

	protected override void DrawGizmos()
	{
		float scale = MathF.Max( 0.0001f, WorldScale.x );

		bool playing = _spawnPosition != Vector3.Zero;
		Vector3 spawnWorld = playing ? _spawnPosition : WorldPosition;
		float spawnYaw = playing ? _spawnYaw : GameObject.WorldRotation.Yaw();
		Vector3 spawnLocal = WorldTransform.PointToLocal( spawnWorld );

		Gizmo.Draw.Color = Color.Yellow.WithAlpha( 0.3f );
		Gizmo.Draw.LineSphere( Vector3.Zero, AggroRange / scale );

		Gizmo.Draw.Color = Color.Orange.WithAlpha( 0.3f );
		Gizmo.Draw.LineSphere( Vector3.Zero, ( IsRanged ? ProjectileRange : AttackRange ) / scale );

		float beyond = LeashBeyond();

		if ( MovementMode == MonsterMovementMode.Patrol )
		{
			var routePoints = new List<Vector3>();

			if ( _patrolPositions.Count > 0 )
			{
				routePoints.AddRange( _patrolPositions );
			}
			else if ( PatrolPoints != null )
			{
				foreach ( var point in PatrolPoints )
				{
					if ( point != null && point.IsValid() )
						routePoints.Add( point.WorldPosition );
				}
			}

			if ( routePoints.Count == 0 )
			{
				Gizmo.Draw.Color = Color.Red.WithAlpha( 0.3f );
				Gizmo.Draw.LineSphere( spawnLocal, beyond / scale );
				return;
			}

			Gizmo.Draw.LineThickness = 2f;
			Gizmo.Draw.Color = Color.Green.WithAlpha( 0.6f );

			for ( int i = 0; i < routePoints.Count; i++ )
			{
				Vector3 a = WorldTransform.PointToLocal( routePoints[i] );
				Vector3 b = WorldTransform.PointToLocal( routePoints[( i + 1 ) % routePoints.Count] );
				Gizmo.Draw.Line( a, b );
			}

			Gizmo.Draw.Color = Color.Red.WithAlpha( 0.3f );
			foreach ( var p in routePoints )
				Gizmo.Draw.LineSphere( WorldTransform.PointToLocal( p ), beyond / scale );

			return;
		}

		if ( MovementMode != MonsterMovementMode.Roam )
		{
			Gizmo.Draw.Color = Color.Red.WithAlpha( 0.3f );
			Gizmo.Draw.LineSphere( spawnLocal, beyond / scale );
			return;
		}

		Gizmo.Draw.Color = Color.Red.WithAlpha( 0.6f );
		DrawRoamOutline( spawnWorld, spawnYaw, spawnLocal, scale, beyond );

		Gizmo.Draw.Color = Color.Green.WithAlpha( 0.5f );
		DrawRoamOutline( spawnWorld, spawnYaw, spawnLocal, scale, 0f );
	}

	void DrawRoamOutline( Vector3 center, float yaw, Vector3 centerLocal, float scale, float inflate )
	{
		if ( RoamShape == MonsterRoamShape.Circle )
		{
			Gizmo.Draw.LineSphere( centerLocal, ( RoamRadius + inflate ) / scale );
			return;
		}

		Gizmo.Draw.LineThickness = 2f;

		float ex = RoamExtentX + inflate;
		float ey = RoamExtentY + inflate;
		float rad = yaw * ( MathF.PI / 180f );
		float c = MathF.Cos( rad );
		float s = MathF.Sin( rad );
		const float wallBottom = -100f;
		const float wallTop = 220f;
		const int rails = 5;

		Vector3 ToLocal( float lx, float ly, float lz )
		{
			Vector3 world = center + new Vector3( lx * c - ly * s, lx * s + ly * c, lz );
			return WorldTransform.PointToLocal( world );
		}

		float RailZ( int rail )
		{
			return wallBottom + ( wallTop - wallBottom ) * ( rail / (float)( rails - 1 ) );
		}

		if ( RoamShape == MonsterRoamShape.Rectangle )
		{
			Vector2[] corners = new Vector2[]
			{
				new Vector2( -ex, -ey ),
				new Vector2( ex, -ey ),
				new Vector2( ex, ey ),
				new Vector2( -ex, ey )
			};

			for ( int i = 0; i < 4; i++ )
			{
				var a = corners[i];
				var b = corners[( i + 1 ) % 4];

				for ( int r = 0; r < rails; r++ )
				{
					float z = RailZ( r );
					Gizmo.Draw.Line( ToLocal( a.x, a.y, z ), ToLocal( b.x, b.y, z ) );
				}

				Gizmo.Draw.Line( ToLocal( a.x, a.y, wallBottom ), ToLocal( a.x, a.y, wallTop ) );
			}
			return;
		}

		const int segments = 20;

		for ( int r = 0; r < rails; r++ )
		{
			float z = RailZ( r );
			Vector3 prev = ToLocal( ex, 0f, z );

			for ( int i = 1; i <= segments; i++ )
			{
				float t = ( i / (float)segments ) * MathF.PI * 2f;
				Vector3 next = ToLocal( MathF.Cos( t ) * ex, MathF.Sin( t ) * ey, z );
				Gizmo.Draw.Line( prev, next );
				prev = next;
			}
		}

		for ( int i = 0; i < segments; i += 5 )
		{
			float t = ( i / (float)segments ) * MathF.PI * 2f;
			float lx = MathF.Cos( t ) * ex;
			float ly = MathF.Sin( t ) * ey;
			Gizmo.Draw.Line( ToLocal( lx, ly, wallBottom ), ToLocal( lx, ly, wallTop ) );
		}
	}
}