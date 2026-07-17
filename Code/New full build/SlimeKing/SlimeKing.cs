using Sandbox;
using System;
using System.Collections.Generic;

public sealed class SlimeKing : Component
{
	enum SlimeState
	{
		Dormant,
		Hopping,
		Telegraphing,
		Dead
	}

	[Property, Group( "Identity" )] public CombatStyle CombatStyle { get; set; } = CombatStyle.Melee;
	[Property, Group( "Identity" )] public Color BodyColor { get; set; } = new Color( 0.14f, 0.07f, 0.23f );

	[Property, Group( "Stats" )] public int BaseMaxHealth { get; set; } = 10000;
	[Property, Group( "Stats" )] public int CombatXpReward { get; set; } = 1000;

	[Property, Group( "Ranges" )] public float AggroRadius { get; set; } = 900f;
	[Property, Group( "Ranges" )] public float LeashRadius { get; set; } = 2500f;
	[Property, Group( "Ranges" )] public float SpikeTriggerRange { get; set; } = 300f;

	[Property, Group( "Hopping" )] public float HopSpeed { get; set; } = 420f;
	[Property, Group( "Hopping" )] public float HopHeight { get; set; } = 600f;
	[Property, Group( "Hopping" )] public float PounceRange { get; set; } = 600f;
	[Property, Group( "Hopping" )] public float HopPauseMin { get; set; } = 0.4f;
	[Property, Group( "Hopping" )] public float HopPauseMax { get; set; } = 1.1f;
	[Property, Group( "Hopping" )] public float Gravity { get; set; } = 800f;

	[Property, Group( "Attacks" )] public int LandDamage { get; set; } = 70;
	[Property, Group( "Attacks" )] public float LandRadiusBase { get; set; } = 130f;
	[Property, Group( "Attacks" )] public float LandKnockback { get; set; } = 500f;
	[Property, Group( "Attacks" )] public int SpikeDamage { get; set; } = 50;
	[Property, Group( "Attacks" )] public float SpikeRadiusBase { get; set; } = 500f;
	[Property, Group( "Attacks" )] public float SpikeTelegraphDuration { get; set; } = 1.2f;
	[Property, Group( "Attacks" )] public float SpikeCooldown { get; set; } = 6f;

	[Property, Group( "Respawn" )] public float RespawnDelayMin { get; set; } = 180f;
	[Property, Group( "Respawn" )] public float RespawnDelayMax { get; set; } = 300f;

	[Property, Group( "Loot" )] public GameObject SplitPrefab { get; set; }
	[Property, Group( "Loot" )] public LootTable LootTable { get; set; }
	[Property, Group( "Loot" ), Range( 0f, 1f )] public float GroupLootRetention { get; set; } = 0.9f;

	[Property, Group( "References" )] public ModelRenderer ModelRenderer { get; set; }
	[Property, Group( "References" )] public Collider BodyCollider { get; set; }

	[Property, Group( "Debug" )] public bool ShowRangeRings { get; set; }

	[Sync] public int CurrentHealth { get; set; }
	[Sync] public int MaxHealth { get; set; }
	[Sync] public bool IsDead { get; set; }
	[Sync] public int Generation { get; set; }
	[Sync] public Color SlimeColor { get; set; }
	[Sync] public float TelegraphFraction { get; set; }
	[Sync] public float VisualSquash { get; set; }
	[Sync] public int BurstCounter { get; set; }
	[Sync] public bool IsHiddenRoot { get; set; }

	public string DisplayName => Generation == 0 ? "The Slime King" : Generation == 1 ? "Slime Prince" : "Slimeling";

	public float HealthBarHeight
	{
		get
		{
			if ( _visualRenderer != null && _visualRenderer.IsValid() )
				return _visualRenderer.Bounds.Maxs.z - WorldPosition.z + 30f;
			return 120f;
		}
	}

	public float BodyWorldRadius => _modelHalf * WorldScale.x;

	public Vector3 BodyWorldCenter => WorldPosition + Vector3.Up * ( 0.7f * _modelHalf * WorldScale.x );

	public static SlimeKing FindAlongPath( Scene scene, Vector3 from, Vector3 to, float extraRadius )
	{
		if ( scene == null )
			return null;

		var seg = to - from;
		float len = seg.Length;
		var dir = len > 0.01f ? seg / len : Vector3.Zero;

		foreach ( var slime in scene.GetAllComponents<SlimeKing>() )
		{
			if ( slime == null || !slime.IsValid() || slime.IsDead || slime.IsHiddenRoot )
				continue;

			var center = slime.BodyWorldCenter;
			float range = slime.BodyWorldRadius + extraRadius;

			Vector3 closest = from;
			if ( len > 0.01f )
			{
				float along = MathX.Clamp( Vector3.Dot( center - from, dir ), 0f, len );
				closest = from + dir * along;
			}

			if ( ( center - closest ).LengthSquared <= range * range )
				return slime;
		}

		return null;
	}

	float ScaleFactor => Generation == 0 ? 1f : Generation == 1 ? 0.55f : 0.3f;

	float GenerationMultiplier => Generation == 0 ? 1f : Generation == 1 ? 0.5f : 0.25f;

	int ScaledLandDamage => Math.Max( 1, (int)( LandDamage * GenerationMultiplier ) );
	int ScaledSpikeDamage => Math.Max( 1, (int)( SpikeDamage * GenerationMultiplier ) );
	float ScaledLandKnockback => LandKnockback * GenerationMultiplier;

	SlimeState _state = SlimeState.Dormant;
	Vector3 _spawnPosition;
	Vector3 _baseWorldScale = Vector3.One;
	GameObject _target;
	Vector3 _velocity;
	bool _airborne;
	bool _landDamageApplied;
	float _pauseTimer;
	float _spikeCooldownRemaining;
	float _telegraphTimer;
	float _respawnTimer;
	bool _respawnRequested;

	SlimeKing _familyRoot;
	Dictionary<ulong, SkillType> _familyContributors = new();
	int _familyAlive;

	GameObject _visual;
	ModelRenderer _visualRenderer;
	float _modelHalf = 14f;
	bool _visualsSetUp;
	bool _metricsCaptured;
	float _proxyDelay;
	Vector3 _lastPos;
	Vector3 _velSmooth;
	int _lastBurstCounter;
	float _burstFxRemaining;
	float _landFxRemaining;
	float _landFxDuration;
	Vector3 _landFxCenter;
	float _landFxRadius;
	int _landFxSeed;

	readonly GizmoPaint _paint = new GizmoPaint();

	const float GroundSink = 0.3f;

	SlimeKing Root => _familyRoot != null && _familyRoot.IsValid() ? _familyRoot : this;

	protected override void DrawGizmos()
	{
		using ( Gizmo.Scope( "slimeking-ranges", global::Transform.Zero ) )
		{
			Gizmo.Draw.Color = Color.Yellow.WithAlpha( 0.35f );
			Gizmo.Draw.LineSphere( WorldPosition, AggroRadius );

			Gizmo.Draw.Color = Color.Red.WithAlpha( 0.35f );
			Gizmo.Draw.LineSphere( WorldPosition, LeashRadius );
		}
	}

	void DrawRangeRings()
	{
		if ( !ShowRangeRings )
			return;

		SpellGizmo.SoftRing( WorldPosition + Vector3.Up * 12f, AggroRadius, 6f, new Color( 1f, 0.85f, 0.2f, 0.6f ), 48 );

		Vector3 leashCenter = _spawnPosition == Vector3.Zero ? WorldPosition : _spawnPosition;
		SpellGizmo.SoftRing( leashCenter + Vector3.Up * 12f, LeashRadius, 6f, new Color( 1f, 0.25f, 0.2f, 0.6f ), 48 );
	}

	protected override void OnStart()
	{
		GameObject.Tags.Add( "boss" );
		GameObject.Tags.Add( "slimeking" );

		_spawnPosition = WorldPosition;
		_baseWorldScale = WorldScale;
		_lastBurstCounter = BurstCounter;

		if ( Networking.IsHost && MaxHealth <= 0 )
		{
			Generation = 0;
			MaxHealth = BaseMaxHealth;
			CurrentHealth = MaxHealth;
			ApplyBodyColor();
			_familyAlive = 1;
		}

		if ( !IsProxy )
		{
			_visualsSetUp = true;
			SetupVisuals();
		}
	}

	void ApplyBodyColor()
	{
		SlimeColor = BodyColor;
	}

	int MaxHealthForGeneration( int generation )
	{
		float mult = generation == 0 ? 1f : generation == 1 ? 0.5f : 0.25f;
		return Math.Max( 1, (int)( BaseMaxHealth * mult ) );
	}

	float GenerationScale( int generation )
	{
		return generation == 0 ? 1f : generation == 1 ? 0.55f : 0.3f;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy && !_visualsSetUp )
		{
			_proxyDelay += Time.Delta;
			if ( _proxyDelay >= 0.5f )
			{
				_visualsSetUp = true;
				SetupVisuals();
			}
		}

		if ( _visualsSetUp && !_metricsCaptured )
		{
			if ( _visualRenderer == null || !_visualRenderer.IsValid() )
				SetupVisuals();
			else
				_metricsCaptured = TryCaptureModelMetrics();
		}

		TrackVelocity();
		ApplyTint();
		ApplyVisual();
		DrawBubbles();
		DrawCrown();
		DrawTelegraph();
		UpdateBurstFx();
		UpdateLandFx();
		DrawRangeRings();
		_paint.Flush( Scene );

		if ( !Networking.IsHost )
			return;

		if ( IsDead )
		{
			UpdateRespawn();
			return;
		}

		if ( _spikeCooldownRemaining > 0f )
			_spikeCooldownRemaining -= Time.Delta;

		switch ( _state )
		{
			case SlimeState.Dormant:
				UpdateDormant();
				break;
			case SlimeState.Hopping:
				UpdateHopping();
				break;
			case SlimeState.Telegraphing:
				UpdateTelegraphing();
				break;
		}
	}

	void UpdateDormant()
	{
		VisualSquash = -0.15f + MathF.Sin( Time.Now * 2.5f ) * 0.05f;

		var player = FindNearestPlayerInAggroRange();
		if ( player == null )
			return;

		_target = player;
		_pauseTimer = 0.2f;
		_state = SlimeState.Hopping;
	}

	void UpdateHopping()
	{
		if ( _airborne )
		{
			UpdateAirborne();
			return;
		}

		VisualSquash = -0.25f + MathF.Sin( Time.Now * 2.5f ) * 0.05f;

		if ( !EnsureValidTarget() )
			_target = FindNearestPlayerInAggroRange();

		if ( _target != null && FlatDistance( _target.WorldPosition, _spawnPosition ) > LeashRadius )
			_target = null;

		if ( _target == null && FlatDistance( WorldPosition, _spawnPosition ) < 50f )
		{
			CurrentHealth = MaxHealth;
			_state = SlimeState.Dormant;
			return;
		}

		Vector3 destination = _target != null && _target.IsValid() ? _target.WorldPosition : _spawnPosition;

		if ( _target != null && Generation < 2 && _spikeCooldownRemaining <= 0f
			&& FlatDistance( WorldPosition, destination ) <= SpikeTriggerRange )
		{
			_telegraphTimer = 0f;
			_state = SlimeState.Telegraphing;
			return;
		}

		_pauseTimer -= Time.Delta;
		if ( _pauseTimer > 0f )
			return;

		LaunchHop( destination );
	}

	void LaunchHop( Vector3 destination )
	{
		bool pounce = _target != null && _target.IsValid()
			&& FlatDistance( WorldPosition, _target.WorldPosition ) <= PounceRange;
		if ( pounce )
			destination = _target.WorldPosition;

		Vector3 to = ( destination - WorldPosition ).WithZ( 0f );
		float dist = to.Length;
		if ( dist < 10f )
		{
			_pauseTimer = Game.Random.Float( HopPauseMin, HopPauseMax );
			return;
		}

		Vector3 dir = to.Normal;

		var sibling = FindNearestCrowdingSibling();
		if ( sibling != null )
		{
			Vector3 away = ( WorldPosition - sibling.WorldPosition ).WithZ( 0f );
			if ( away.LengthSquared > 0.01f )
				dir = ( dir + away.Normal ).Normal;
		}

		float hopScale = MathF.Max( MathF.Sqrt( ScaleFactor ), 0.6f );
		float bodyHeight = 2f * _modelHalf * WorldScale.x;
		float rise = MathF.Max( 0f, destination.z - WorldPosition.z );

		float vertical = HopHeight * hopScale;
		float minVertical = MathF.Sqrt( 2f * MathF.Max( 1f, Gravity ) * ( rise + bodyHeight ) );
		if ( vertical < minVertical )
			vertical = minVertical;

		float airTime = 2f * vertical / MathF.Max( 1f, Gravity );
		float horizontal = pounce
			? dist / MathF.Max( 0.1f, airTime )
			: MathF.Min( HopSpeed * ScaleFactor, dist / MathF.Max( 0.1f, airTime ) );

		_velocity = dir * horizontal + Vector3.Up * vertical;
		_airborne = true;
		_landDamageApplied = false;
		VisualSquash = -0.3f;
	}

	SlimeKing FindNearestCrowdingSibling()
	{
		float threshold = 2f * _modelHalf * WorldScale.x;
		float bestSqr = threshold * threshold;
		SlimeKing best = null;

		foreach ( var s in Scene.GetAllComponents<SlimeKing>() )
		{
			if ( s == null || !s.IsValid() || s == this || s.IsDead || s.IsHiddenRoot )
				continue;
			if ( s.Root != Root )
				continue;

			float dSqr = ( s.WorldPosition - WorldPosition ).WithZ( 0f ).LengthSquared;
			if ( dSqr <= bestSqr )
			{
				bestSqr = dSqr;
				best = s;
			}
		}

		return best;
	}

	void UpdateAirborne()
	{
		_velocity = new Vector3( _velocity.x, _velocity.y, _velocity.z - Gravity * Time.Delta );
		WorldPosition += _velocity * Time.Delta;

		VisualSquash = MathX.Clamp( _velocity.z / 500f, -0.15f, 0.35f );

		if ( _velocity.z >= 0f )
			return;

		float groundZ = GroundZAt( WorldPosition, _spawnPosition.z );

		float scanHeight = 100f + 0.4f * _modelHalf * WorldScale.x;
		if ( !_landDamageApplied && WorldPosition.z <= groundZ + scanHeight )
		{
			_landDamageApplied = true;
			ApplyLandDamage( ProjectLandingPoint( groundZ ) );
		}

		if ( WorldPosition.z <= groundZ )
		{
			WorldPosition = WorldPosition.WithZ( groundZ );
			_airborne = false;
			_velocity = Vector3.Zero;
			VisualSquash = -0.6f;
			_pauseTimer = Game.Random.Float( HopPauseMin, HopPauseMax );
			BroadcastSquish( WorldPosition );
			BroadcastLandImpact( WorldPosition, LandRadiusBase * ScaleFactor );
		}
	}

	Vector3 ProjectLandingPoint( float groundZ )
	{
		float drop = MathF.Max( 0f, WorldPosition.z - groundZ );
		float g = MathF.Max( 1f, Gravity );
		float vz = _velocity.z;
		float t = ( vz + MathF.Sqrt( vz * vz + 2f * g * drop ) ) / g;
		Vector3 point = WorldPosition + _velocity.WithZ( 0f ) * t;
		return point.WithZ( groundZ );
	}

	void ApplyLandDamage( Vector3 center )
	{
		float radius = LandRadiusBase * ScaleFactor;
		float radiusSqr = radius * radius;

		foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
		{
			if ( pc == null || !pc.IsValid() )
				continue;

			var playerObj = pc.GameObject;
			if ( ( playerObj.WorldPosition - center ).WithZ( 0f ).LengthSquared > radiusSqr )
				continue;

			ApplyDamageToPlayer( playerObj, ScaledLandDamage );
			DismountRider( playerObj );
			ApplyKnockbackToPlayer( playerObj, ScaledLandKnockback );
		}
	}

	void UpdateTelegraphing()
	{
		_telegraphTimer += Time.Delta;
		TelegraphFraction = MathF.Min( 1f, _telegraphTimer / MathF.Max( 0.1f, SpikeTelegraphDuration ) );

		VisualSquash = -0.15f - 0.45f * TelegraphFraction + MathF.Sin( Time.Now * 18f ) * 0.05f;

		if ( TelegraphFraction < 1f )
			return;

		BurstCounter++;
		VisualSquash = 0.35f;
		ApplySpikeDamage();
		TelegraphFraction = 0f;
		_spikeCooldownRemaining = SpikeCooldown;
		_pauseTimer = Game.Random.Float( HopPauseMin, HopPauseMax );
		_state = SlimeState.Hopping;
	}

	void ApplySpikeDamage()
	{
		float radius = SpikeRadiusBase * ScaleFactor;
		float radiusSqr = radius * radius;

		foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
		{
			if ( pc == null || !pc.IsValid() )
				continue;

			var playerObj = pc.GameObject;
			if ( ( playerObj.WorldPosition - WorldPosition ).WithZ( 0f ).LengthSquared > radiusSqr )
				continue;

			ApplyDamageToPlayer( playerObj, ScaledSpikeDamage );
			DismountRider( playerObj );
		}
	}

	float GroundZAt( Vector3 p, float fallbackZ )
	{
		var tr = Scene.Trace
			.Ray( p + Vector3.Up * 50f, p - Vector3.Up * 2000f )
			.Size( 2f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "slimeking", "pet", "player" )
			.Run();
		return tr.Hit ? tr.HitPosition.z : fallbackZ;
	}

	bool EnsureValidTarget()
	{
		if ( _target == null || !_target.IsValid() )
			return false;

		var health = _target.Components.Get<PlayerHealth>();
		if ( health == null || health.IsDead )
			return false;

		return true;
	}

	GameObject FindNearestPlayerInAggroRange()
	{
		GameObject best = null;
		float bestDistSq = AggroRadius * AggroRadius;

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

	void ApplyDamageToPlayer( GameObject playerObj, int baseDamage )
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

		int finalDamage = (int)( baseDamage * triangleMult * ( 1f - armorReduction ) / defenceMult / defenceBuffMult );
		if ( finalDamage < 1 )
			finalDamage = 1;

		playerHealth.TakeDamage( finalDamage );
		DamagePopupBroadcaster.Broadcast( playerObj.WorldPosition + Vector3.Up * 60f, finalDamage, playerHealth.MaxHealth, false );
	}

	void DismountRider( GameObject playerObj )
	{
		ulong steamId = playerObj?.Network?.Owner?.SteamId ?? 0ul;
		if ( steamId == 0ul )
			return;

		BroadcastDismountRider( steamId );
	}

	[Rpc.Broadcast]
	void BroadcastDismountRider( ulong targetSteamId )
	{
		if ( Connection.Local == null || Connection.Local.SteamId != targetSteamId )
			return;

		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null || !player.IsValid() )
			return;

		var pc = player.Components.Get<PlayerController>();
		if ( pc == null )
			return;

		foreach ( var chair in Scene.GetAllComponents<BaseChair>() )
		{
			if ( chair == null || !chair.IsOccupied )
				continue;

			if ( chair.GetOccupant() == pc )
				chair.AskToLeave( pc );
		}
	}

	void ApplyKnockbackToPlayer( GameObject playerObj, float force )
	{
		if ( playerObj == null || !playerObj.IsValid() )
			return;

		var receiver = playerObj.Components.Get<KnockbackReceiver>();
		if ( receiver == null )
			return;

		var direction = ( playerObj.WorldPosition - WorldPosition ).WithZ( 0f );
		if ( direction.LengthSquared < 0.0001f )
			direction = WorldRotation.Forward.WithZ( 0f );

		receiver.ApplyKnockback( direction.Normal, force, 0.4f, 0.8f );
	}

	SkillType GetAttackerSkill( GameObject attacker )
	{
		if ( attacker == null || !attacker.IsValid() )
			return SkillType.Attack;

		var inventory = attacker.Components.Get<Inventory>();
		var weaponDef = inventory?.GetEquippedWeaponDef();

		if ( weaponDef == null )
			return SkillType.Attack;

		if ( weaponDef.Type == ItemType.RangedWeapon )
			return SkillType.Archery;

		if ( weaponDef.Type == ItemType.MagicWeapon )
			return SkillType.Magic;

		return SkillType.Attack;
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
				Root._familyContributors[ownerConnection.SteamId] = GetAttackerSkill( attacker );
		}

		CurrentHealth -= damage;

		if ( CurrentHealth <= 0 )
		{
			CurrentHealth = 0;
			Die();
		}
		else if ( _state == SlimeState.Dormant && attacker != null && attacker.IsValid() )
		{
			_target = attacker;
			_pauseTimer = 0.2f;
			_state = SlimeState.Hopping;
		}
	}

	void Die()
	{
		IsDead = true;
		_state = SlimeState.Dead;
		TelegraphFraction = 0f;
		VisualSquash = -0.7f;
		_airborne = false;
		BroadcastSquish( WorldPosition );

		bool split = Generation < 2;
		if ( split )
			SpawnSplits();

		Root.OnFamilyMemberDied( split );

		if ( _familyRoot == null || !_familyRoot.IsValid() )
		{
			IsHiddenRoot = true;
			BroadcastSetBodyVisible( false );
		}
		else
		{
			DestroyAfterSquash();
		}
	}

	async void DestroyAfterSquash()
	{
		await Task.DelaySeconds( 0.3f );
		if ( GameObject != null && GameObject.IsValid() )
			GameObject.Destroy();
	}

	void SpawnSplits()
	{
		if ( SplitPrefab == null )
			return;

		int childGeneration = Generation + 1;
		var root = Root;

		Vector3 childScale = root._baseWorldScale * GenerationScale( childGeneration );
		float separation = 1.4f * _modelHalf * childScale.x;
		float theta = Game.Random.Float( 0f, MathF.PI * 2f );

		for ( int i = 0; i < 2; i++ )
		{
			float angle = theta + i * MathF.PI;
			Vector3 offset = new Vector3( MathF.Cos( angle ), MathF.Sin( angle ), 0f ) * separation;
			var go = SplitPrefab.Clone( WorldPosition + offset + Vector3.Up * 10f );
			go.Name = $"SlimeKing_Gen{childGeneration}";
			go.WorldScale = childScale;
			EnsureMembraneOn( go, childGeneration );

			var child = go.Components.Get<SlimeKing>();
			if ( child != null )
			{
				child.Generation = childGeneration;
				child.BaseMaxHealth = BaseMaxHealth;
				child.MaxHealth = MaxHealthForGeneration( childGeneration );
				child.CurrentHealth = child.MaxHealth;
				child.SlimeColor = SlimeColor;
				child.BodyColor = BodyColor;
				child.CombatStyle = CombatStyle;
				child.AggroRadius = AggroRadius;
				child.LeashRadius = LeashRadius;
				child.SpikeTriggerRange = SpikeTriggerRange;
				child.HopSpeed = HopSpeed;
				child.HopHeight = HopHeight;
				child.PounceRange = PounceRange;
				child.HopPauseMin = HopPauseMin;
				child.HopPauseMax = HopPauseMax;
				child.Gravity = Gravity;
				child.LandDamage = LandDamage;
				child.LandRadiusBase = LandRadiusBase;
				child.LandKnockback = LandKnockback;
				child.SpikeDamage = SpikeDamage;
				child.SpikeRadiusBase = SpikeRadiusBase;
				child.SpikeTelegraphDuration = SpikeTelegraphDuration;
				child.SpikeCooldown = SpikeCooldown;
				child.SplitPrefab = SplitPrefab;
				child._familyRoot = root;
			}

			go.NetworkSpawn();
		}
	}

	void EnsureMembraneOn( GameObject go, int childGeneration )
	{
		GameObject visual = null;
		foreach ( var c in go.Children )
		{
			if ( c.Name == "Visual" )
			{
				visual = c;
				break;
			}
		}

		bool visualExisted = visual != null;
		if ( visual == null )
		{
			visual = new GameObject();
			visual.Name = "Visual";
			visual.Parent = go;
			visual.LocalPosition = Vector3.Zero;
		}

		visual.Enabled = true;

		var mr = visual.Components.Get<ModelRenderer>();
		bool rendererExisted = mr != null;
		if ( mr == null )
			mr = visual.Components.Create<ModelRenderer>();

		if ( mr.Model == null )
			mr.Model = Model.Load( "models/dev/sphere.vmdl" );

		mr.Enabled = true;
		mr.Tint = SlimeColor.WithAlpha( 0.66f );

		Log.Info( $"[SlimeKing] Split spawn Gen={childGeneration} scale={go.WorldScale.x}: visualExisted={visualExisted} rendererExisted={rendererExisted} model={mr.Model?.Name ?? "null"}" );
	}

	void OnFamilyMemberDied( bool split )
	{
		if ( split )
			_familyAlive += 2;

		_familyAlive--;

		if ( _familyAlive > 0 )
			return;

		AwardLootToContributors();
		_respawnRequested = false;
	}

	void UpdateRespawn()
	{
		if ( _familyRoot != null && _familyRoot.IsValid() )
			return;

		if ( _familyAlive > 0 )
			return;

		if ( !_respawnRequested )
		{
			_respawnTimer = Game.Random.Float( RespawnDelayMin, RespawnDelayMax );
			_respawnRequested = true;
		}

		_respawnTimer -= Time.Delta;
		if ( _respawnTimer > 0f )
			return;

		Respawn();
	}

	void Respawn()
	{
		Generation = 0;
		MaxHealth = BaseMaxHealth;
		CurrentHealth = MaxHealth;
		IsDead = false;
		IsHiddenRoot = false;
		_respawnRequested = false;
		_state = SlimeState.Dormant;
		_target = null;
		_airborne = false;
		_velocity = Vector3.Zero;
		TelegraphFraction = 0f;
		_spikeCooldownRemaining = 0f;
		_familyAlive = 1;
		ApplyBodyColor();
		WorldPosition = _spawnPosition;
		WorldScale = _baseWorldScale;
		BroadcastSetBodyVisible( true );
		GameManager.Instance?.BroadcastServerNotice( ReformLines[Game.Random.Int( 0, ReformLines.Length - 1 )] );
	}

	static readonly string[] ReformLines =
	{
		"The Slime King has reformed...",
		"A thousand droplets gather... the Slime King rises.",
		"The ground squelches. The Slime King has returned.",
		"Long live the Slime King. He bubbles back into being...",
		"Something gelatinous stirs in the distance...",
		"The Slime King wobbles once more. All hail."
	};

	[Rpc.Broadcast]
	void BroadcastSetBodyVisible( bool visible )
	{
		if ( _visual != null && _visual.IsValid() && _visual != GameObject )
			_visual.Enabled = visible;
		else if ( _visualRenderer != null && _visualRenderer.IsValid() )
			_visualRenderer.Enabled = visible;

		if ( BodyCollider != null )
			BodyCollider.Enabled = visible;
	}

	[Rpc.Broadcast]
	void BroadcastSquish( Vector3 position )
	{
		SoundLibrary.PlaySlimeSquish( position );
	}

	[Rpc.Broadcast]
	void BroadcastLandImpact( Vector3 center, float radius )
	{
		_landFxCenter = center;
		_landFxRadius = radius;
		_landFxDuration = MathF.Max( 0.2f, 0.5f * ScaleFactor );
		_landFxRemaining = _landFxDuration;
		_landFxSeed = ( _landFxSeed + 1 ) % 1000;
		SoundLibrary.PlaySlimeKingLand( center, Generation );
	}

	void UpdateLandFx()
	{
		if ( _landFxRemaining <= 0f )
			return;

		_landFxRemaining -= Time.Delta;

		float dur = MathF.Max( 0.05f, _landFxDuration );
		float t = MathX.Clamp( 1f - _landFxRemaining / dur, 0f, 1f );

		float ringT = MathF.Min( 1f, t / 0.7f );
		float ringAlpha = 0.7f * ( 1f - ringT );
		if ( ringAlpha > 0.02f )
		{
			float ringRadius = _landFxRadius * ( 0.2f + 0.8f * ringT );
			SpellGizmo.SoftRing( _landFxCenter + Vector3.Up * 4f, ringRadius, 4f * ScaleFactor + 2f, SlimeColor.WithAlpha( ringAlpha ), 32 );
		}

		float dropletAlpha = 0.8f * ( 1f - t );
		if ( dropletAlpha <= 0.02f )
			return;

		float elapsed = t * dur;
		Gizmo.Draw.Color = SlimeColor.WithAlpha( dropletAlpha );

		for ( int i = 0; i < 7; i++ )
		{
			float seed = _landFxSeed * 7.31f + i * 2.39f;
			float ang = seed % ( MathF.PI * 2f );
			float outSpeed = ( 90f + ( seed * 37f ) % 120f ) * ScaleFactor;
			float upSpeed = ( 160f + ( seed * 53f ) % 140f ) * ScaleFactor;

			float px = _landFxCenter.x + MathF.Cos( ang ) * outSpeed * elapsed;
			float py = _landFxCenter.y + MathF.Sin( ang ) * outSpeed * elapsed;
			float pz = _landFxCenter.z + upSpeed * elapsed - 0.5f * Gravity * elapsed * elapsed;
			if ( pz < _landFxCenter.z )
				pz = _landFxCenter.z;

			float size = ( 2.5f + seed % 2f ) * ScaleFactor;
			Gizmo.Draw.SolidSphere( new Vector3( px, py, pz ), size );
		}
	}

	void AwardLootToContributors()
	{
		if ( _familyContributors.Count == 0 )
			return;

		if ( LootTable == null )
		{
			_familyContributors.Clear();
			return;
		}

		var rng = new Random();

		int contributorCount = _familyContributors.Count;

		int goldPool = LootTable.RollGoldPool( rng );
		int goldPerPlayer = contributorCount > 0
			? (int)Math.Ceiling( goldPool / (double)contributorCount )
			: 0;

		float oddsScale = GroupLootRetention + ( 1f - GroupLootRetention ) / contributorCount;

		var entries = LootTable.Entries ?? new List<LootEntry>();

		foreach ( var contributor in _familyContributors )
		{
			ulong steamId = contributor.Key;
			SkillType skill = contributor.Value;

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

			BroadcastSlimeLoot( steamId, goldPerPlayer, rolledItems.ToArray(), rolledAmounts.ToArray(), skill, CombatXpReward );
		}

		_familyContributors.Clear();
	}

	[Rpc.Broadcast]
	void BroadcastSlimeLoot( ulong recipientSteamId, int gold, ItemId[] items, int[] amounts, SkillType skill, int combatXp )
	{
		if ( Connection.Local == null || Connection.Local.SteamId != recipientSteamId )
			return;

		var localPlayer = FindLocalPlayerForLoot();
		if ( localPlayer == null )
			return;

		var inventory = localPlayer.Components.Get<Inventory>();
		if ( inventory == null )
			return;

		AchievementTracker.OnBossKilled();

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

		var skills = localPlayer.Components.Get<Skills>();
		if ( skills != null && combatXp > 0 )
		{
			skills.AddXp( skill, combatXp );
			GameLog.Add( $"You gained {combatXp} {skill} XP for defeating The Slime King.", "#8fd18f" );
		}

		if ( gainedAny )
			SoundLibrary.PlayReceiveItem();

		if ( !PetColorState.IsUnlocked() )
		{
			PetColorState.Unlock();
			GameLog.Add( "The Slime King's essence seeps into your pet... You can now recolor your slime at the Pets menu!", "#39d94a" );
		}
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

	float FlatDistance( Vector3 a, Vector3 b )
	{
		a.z = 0;
		b.z = 0;
		return Vector3.DistanceBetween( a, b );
	}

	void TrackVelocity()
	{
		var cur = WorldPosition;
		if ( _lastPos != Vector3.Zero )
			_velSmooth = Vector3.Lerp( _velSmooth, ( cur - _lastPos ) / MathF.Max( Time.Delta, 0.001f ), Time.Delta * 5f );
		_lastPos = cur;
	}

	void SetupVisuals()
	{
		ResolveVisual();
		if ( _visual == null )
			return;

		_visualRenderer = _visual.Components.Get<ModelRenderer>();
		if ( _visualRenderer == null )
		{
			_visualRenderer = _visual.Components.Create<ModelRenderer>();
			_visualRenderer.Model = Model.Load( "models/dev/sphere.vmdl" );
		}

		if ( _visualRenderer.Model == null )
			_visualRenderer.Model = Model.Load( "models/dev/sphere.vmdl" );

		if ( !IsDead && !IsHiddenRoot )
		{
			_visual.Enabled = true;
			_visualRenderer.Enabled = true;
		}

		_visualRenderer.Tint = SlimeColor.WithAlpha( 0.66f );

		Log.Info( $"[SlimeKing] SetupVisuals: Gen={Generation} scale={WorldScale.x} proxy={IsProxy} visual={_visual.Name} visualEnabled={_visual.Enabled} model={_visualRenderer.Model?.Name ?? "null"}" );

		_metricsCaptured = TryCaptureModelMetrics();
		EnsureBubbles();
	}

	bool _metricsFailLogged;

	bool TryCaptureModelMetrics()
	{
		if ( _visualRenderer == null || !_visualRenderer.IsValid() || _visualRenderer.Model == null )
		{
			if ( !_metricsFailLogged )
			{
				_metricsFailLogged = true;
				Log.Info( $"[SlimeKing] Metrics pending: Gen={Generation} rendererValid={_visualRenderer != null && _visualRenderer.IsValid()} modelLoaded={_visualRenderer?.Model != null}" );
			}
			return false;
		}

		float h = _visualRenderer.Model.Bounds.Size.z * 0.5f;
		if ( h <= 0.1f )
			return false;

		_modelHalf = h;

		Log.Info( $"[SlimeKing] Metrics captured: Gen={Generation} WorldScale={WorldScale.x} modelHalf={_modelHalf} worldBoundsZ={_visualRenderer.Bounds.Size.z}" );

		AlignCollider();
		return true;
	}

	void AlignCollider()
	{
		if ( BodyCollider is SphereCollider sphere )
		{
			sphere.Center = new Vector3( 0f, 0f, _modelHalf * ( 1f - GroundSink ) );
			sphere.Radius = _modelHalf;
		}
		else if ( BodyCollider is BoxCollider box )
		{
			box.Center = new Vector3( 0f, 0f, _modelHalf * ( 1f - GroundSink ) );
			box.Scale = new Vector3( _modelHalf * 2f, _modelHalf * 2f, _modelHalf * 2f );
		}
	}

	void ResolveVisual()
	{
		if ( _visual != null && _visual.IsValid() )
			return;

		if ( ModelRenderer != null && ModelRenderer.IsValid() )
		{
			_visual = ModelRenderer.GameObject;
			return;
		}

		foreach ( var c in GameObject.Children )
		{
			if ( c.Name == "Visual" )
			{
				_visual = c;
				return;
			}
		}

		foreach ( var c in GameObject.Children )
		{
			if ( c.Components.Get<ModelRenderer>() != null )
			{
				_visual = c;
				return;
			}
		}

		var go = new GameObject();
		go.Name = "Visual";
		go.Parent = GameObject;
		go.LocalPosition = Vector3.Zero;
		var mr = go.Components.Create<ModelRenderer>();
		mr.Model = Model.Load( "models/dev/sphere.vmdl" );
		_visual = go;
	}

	void EnsureBubbles()
	{
		foreach ( var c in _visual.Children )
		{
			if ( c.Name == "PetBubble" )
				return;
		}

		var color = SlimeColor.WithAlpha( 0.55f );
		float[] bx = { 6f, -5f, 3f, -4f };
		float[] by = { 4f, 5f, -2f, 3f };
		float[] bz = { 3f, -3f, 4f, -2f };
		for ( int i = 0; i < 4; i++ )
		{
			var bubble = new GameObject();
			bubble.Name = "PetBubble";
			bubble.Parent = _visual;
			bubble.LocalPosition = new Vector3( bx[i], by[i], bz[i] );
			bubble.LocalScale = Vector3.One * ( 0.15f + i * 0.03f );
			var br = bubble.Components.Create<ModelRenderer>();
			br.Model = Model.Load( "models/dev/sphere.vmdl" );
			br.Tint = new Color( color.r + 0.1f, color.g + 0.1f, color.b + 0.1f, 0.3f );
		}
	}

	void ApplyTint()
	{
		if ( _visualRenderer == null || !_visualRenderer.IsValid() )
			return;

		_visualRenderer.Tint = SlimeColor.WithAlpha( 0.66f );

		if ( _visual == null || !_visual.IsValid() )
			return;

		foreach ( var c in _visual.Children )
		{
			if ( c == null || !c.IsValid() || c.Name != "PetBubble" )
				continue;

			var br = c.Components.Get<ModelRenderer>();
			if ( br != null )
				br.Tint = new Color( MathF.Min( SlimeColor.r + 0.1f, 1f ), MathF.Min( SlimeColor.g + 0.1f, 1f ), MathF.Min( SlimeColor.b + 0.1f, 1f ), 0.3f );
		}
	}

	void ApplyVisual()
	{
		if ( _visual == null || !_visual.IsValid() )
			return;

		float sq = VisualSquash;
		float sy = 1f + sq;
		float sxz = 1f - sq * 0.7f;
		float radius = _modelHalf * sy;
		_visual.LocalScale = new Vector3( sxz, sxz, sy );
		_visual.LocalPosition = new Vector3( 0f, 0f, radius * ( 1f - GroundSink ) );
	}

	void DrawBubbles()
	{
		if ( IsDead || IsHiddenRoot )
			return;

		if ( _visual == null || !_visual.IsValid() )
			return;

		float time = Time.Now;
		var pos = WorldPosition;
		float bodyScale = WorldScale.x;
		float centerZ = pos.z + _modelHalf * bodyScale;
		var color = SlimeColor.WithAlpha( 1f );
		bool moving = _velSmooth.WithZ( 0 ).Length > 20f;

		for ( int i = 0; i < 6; i++ )
		{
			float seed = i * 2.1f;
			float cycle = ( time * 0.5f + seed ) % 1f;
			float angle = seed * 3.7f;
			float spread = 5f * bodyScale;
			float px = pos.x + MathF.Cos( angle ) * spread;
			float py = pos.y + MathF.Sin( angle ) * spread;
			float pz = centerZ + 6f * bodyScale - cycle * 10f * bodyScale;
			float alpha = MathF.Sin( cycle * MathF.PI ) * 0.4f;
			float size = ( 0.8f + ( 1f - cycle ) * 1.2f ) * bodyScale;
			Gizmo.Draw.Color = color.WithAlpha( alpha );
			Gizmo.Draw.SolidSphere( new Vector3( px, py, pz ), size );
		}

		if ( !moving )
			return;

		for ( int i = 0; i < 8; i++ )
		{
			float seed = i * 1.7f + 0.5f;
			float cycle = ( time * 0.8f + seed ) % 1f;
			float angle = time * ( 1.5f + i * 0.3f ) + seed;
			float radius = ( 3f + MathF.Sin( time * 2f + seed ) * 4f ) * bodyScale;
			float bx = pos.x + MathF.Cos( angle ) * radius;
			float by = pos.y + MathF.Sin( angle ) * radius;
			float bz = centerZ + ( 2f + MathF.Sin( time * 3f + seed * 2f ) * 5f ) * bodyScale;
			float alpha = MathF.Sin( cycle * MathF.PI ) * 0.5f;
			float size = ( 0.6f + MathF.Sin( time * 4f + seed ) * 0.3f ) * bodyScale;
			Gizmo.Draw.Color = new Color( MathF.Min( color.r + 0.05f, 1f ), MathF.Min( color.g + 0.15f, 1f ), MathF.Min( color.b + 0.1f, 1f ), alpha );
			Gizmo.Draw.SolidSphere( new Vector3( bx, by, bz ), size );
		}
	}

	static readonly (float Tilt, int Count, float Offset)[] SpikeRings =
	{
		( -18f, 6, 0.5f ),
		( 12f, 8, 0f ),
		( 42f, 6, 0.25f ),
		( 68f, 4, 0.6f ),
		( 90f, 1, 0f )
	};

	void DrawSpikes( float extension, Color color )
	{
		float worldScale = WorldScale.x;
		float bodyR = _modelHalf * worldScale;
		float maxRadius = SpikeRadiusBase * ScaleFactor;
		Vector3 bodyCenter = WorldPosition + Vector3.Up * ( 0.7f * _modelHalf * worldScale );

		float length = MathF.Max( bodyR * 0.2f, ( maxRadius * 1.15f - bodyR * 0.5f ) * extension );
		float girth = bodyR * 0.16f * ( 0.4f + 0.6f * extension );

		Gizmo.Draw.Color = color;

		foreach ( var ring in SpikeRings )
		{
			float tilt = ring.Tilt * MathF.PI / 180f;
			float cosT = MathF.Cos( tilt );
			float sinT = MathF.Sin( tilt );

			for ( int i = 0; i < ring.Count; i++ )
			{
				float angle = ( ( i + ring.Offset ) / ring.Count ) * MathF.PI * 2f;
				Vector3 dir = new Vector3( MathF.Cos( angle ) * cosT, MathF.Sin( angle ) * cosT, sinT );

				Vector3 basePos = bodyCenter + dir * ( bodyR * 0.5f );
				Gizmo.Draw.SolidCone( basePos, dir * length, girth );
			}
		}
	}

	void DrawTelegraph()
	{
		if ( TelegraphFraction <= 0f || IsDead )
			return;

		float maxRadius = SpikeRadiusBase * ScaleFactor;
		Color dark = new Color( SlimeColor.r * 0.45f, SlimeColor.g * 0.45f, SlimeColor.b * 0.45f, 0.95f );

		SpellGizmo.SoftRing( WorldPosition, maxRadius, 5f, dark.WithAlpha( 0.35f + 0.4f * TelegraphFraction ), 32 );

		Color warn = Color.Lerp( SlimeColor, new Color( 1f, 0.15f, 0.1f ), TelegraphFraction );
		SpellGizmo.SoftRing( WorldPosition + Vector3.Up * 3f, maxRadius, 7f, warn.WithAlpha( 0.15f + 0.55f * TelegraphFraction ), 40 );

		if ( TelegraphFraction > 0.05f )
			SpellGizmo.SoftRing( WorldPosition + Vector3.Up * 3f, maxRadius * TelegraphFraction, 3f, warn.WithAlpha( 0.5f * TelegraphFraction ), 28 );
	}

	void UpdateBurstFx()
	{
		if ( BurstCounter != _lastBurstCounter )
		{
			_lastBurstCounter = BurstCounter;
			_burstFxRemaining = 0.45f;
			SoundLibrary.PlaySlimeSquish( WorldPosition );
			SoundLibrary.PlaySlimeKingSpikeBurst( WorldPosition );
		}

		if ( _burstFxRemaining <= 0f )
			return;

		_burstFxRemaining -= Time.Delta;

		float t = 1f - ( _burstFxRemaining / 0.45f );
		float scale = ScaleFactor;
		float extension = MathF.Min( 1f, t * 4f );
		float alpha = t < 0.5f ? 1f : 1f - ( t - 0.5f ) * 2f;
		float radius = SpikeRadiusBase * scale * t;

		Vector3 bodyCenter = WorldPosition + Vector3.Up * ( 0.7f * _modelHalf * WorldScale.x );

		Color burst = new Color( SlimeColor.r * 0.45f, SlimeColor.g * 0.45f, SlimeColor.b * 0.45f, alpha );
		SpellGizmo.SoftRing( bodyCenter, radius, 8f * alpha + 2f, burst.WithAlpha( alpha * 0.6f ), 32 );
		DrawSpikes( extension, burst );

		if ( t < 0.667f )
		{
			float st = t * 1.5f;
			float bodyR = _modelHalf * WorldScale.x;
			Color spikeCol = SlimeColor.WithAlpha( 1f - st );

			for ( int i = 0; i < 9; i++ )
			{
				float ang = ( i / 9f ) * MathF.PI * 2f;
				Vector3 dir = new Vector3( MathF.Cos( ang ), MathF.Sin( ang ), 0.55f ).Normal;
				Vector3 basePos = bodyCenter + dir * ( bodyR * 0.6f );
				float len = bodyR * 0.5f + 90f * ScaleFactor * st;
				_paint.ShadedCone( basePos, dir, len, 6f * ScaleFactor + bodyR * 0.08f, spikeCol );
			}
		}
	}

	void DrawCrown()
	{
		if ( Generation != 0 || IsDead || IsHiddenRoot )
			return;

		float scale = WorldScale.x;

		float topZ;
		if ( _visualRenderer != null && _visualRenderer.IsValid() )
			topZ = _visualRenderer.Bounds.Maxs.z;
		else
			topZ = WorldPosition.z + 2f * _modelHalf * scale * ( 1f + VisualSquash );

		float crownScale = 0.45f * WorldScale.z;
		Vector3 crownCenter = new Vector3( WorldPosition.x, WorldPosition.y, topZ ) + Vector3.Up * 4f * crownScale;

		CrownVfx.Draw( _paint, crownCenter, Rotation.Identity, crownScale, 7, 16f, 18f,
			new Color( 0.88f, 0.75f, 0.38f ), new Color( 0.79f, 0.66f, 0.30f ), Color.White, Time.Now );
	}
}
