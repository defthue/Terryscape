using Sandbox;
using System;
using System.Collections.Generic;

public sealed class LightningBoltChannel : Component
{
	[Property] public float DamageTickInterval { get; set; } = 0.2f;
	[Property] public float ConeRange { get; set; } = 500f;
	[Property] public float ConeAngleDegrees { get; set; } = 20f;
	[Property] public int MaxTargets { get; set; } = 3;

	[Property] public int BoltCount { get; set; } = 3;
	[Property] public float BoltSpreadAngle { get; set; } = 5f;
	[Property] public float OriginJitter { get; set; } = 6f;
	[Property] public float TrunkLengthRatio { get; set; } = 0.55f;
	[Property] public float CastTime { get; set; } = 0.2f;
	[Property] public float WindupDuration { get; set; } = 0.35f;
	[Property] public float AimTraceDistance { get; set; } = 1000f;
	[Property] public float SoundStopFadeOut { get; set; } = 0.15f;
	[Property] public Color BoltColor { get; set; } = new Color( 0.78f, 0.88f, 1f, 1f );
	[Property] public Color HaloColor { get; set; } = new Color( 0.4f, 0.6f, 1f, 1f );

	[Property] public float ForwardOffset { get; set; } = 60f;
	[Property] public float HeightOffset { get; set; } = 40f;
	[Property] public float LateralOffset { get; set; } = 0f;

	public bool VisualOnly { get; set; }

	public bool IsActive { get; private set; }
	public float TimeRemaining { get; private set; }
	public float TimeElapsed { get; private set; }

	GameObject _aimSource;
	List<LightningBolt> _bolts = new();
	List<GameObject> _currentTargets = new();
	float _damageTickTimer;
	float _manaAccum;
	bool _boltsSpawned;
	SoundHandle _soundHandle;
	GameObject _caster;
	SpellDefinition _spell;

	public void Begin( GameObject caster, GameObject aimSource, SpellDefinition spell )
	{
		_caster = caster;
		_aimSource = aimSource;
		_spell = spell;
		IsActive = true;
		TimeRemaining = spell != null ? spell.MaxLifetime : 5f;
		TimeElapsed = 0f;
		_damageTickTimer = 0f;
		_manaAccum = 0f;
		_boltsSpawned = false;
	}

	void SpawnBolts()
	{
		ClearBolts();

		for ( int i = 0; i < BoltCount; i++ )
		{
			var go = new GameObject( true, $"LightningBolt{i}" );
			go.SetParent( GameObject );

			var bolt = go.Components.Create<LightningBolt>();
			bolt.BoltColor = BoltColor;
			bolt.HaloColor = HaloColor;
			bolt.IsMainTrunk = true;
			bolt.Thickness = 2f;

			_bolts.Add( bolt );
		}
	}

	public void End()
	{
		IsActive = false;
		ClearBolts();
		_currentTargets.Clear();

		SoundLibrary.StopLightningBoltLoop( _soundHandle, SoundStopFadeOut );
		_soundHandle = default;
	}

	void ClearBolts()
	{
		foreach ( var bolt in _bolts )
		{
			if ( bolt != null && bolt.IsValid() )
				bolt.GameObject.Destroy();
		}
		_bolts.Clear();
	}

	protected override void OnUpdate()
	{
		if ( !IsActive )
			return;

		if ( _caster == null || !_caster.IsValid() )
		{
			End();
			return;
		}

		TimeRemaining -= Time.Delta;
		TimeElapsed += Time.Delta;
		if ( TimeRemaining <= 0f )
		{
			End();
			return;
		}

		if ( TimeElapsed < CastTime )
			return;

		if ( !_boltsSpawned )
		{
			SpawnBolts();
			_boltsSpawned = true;
			if ( !VisualOnly )
				_soundHandle = SoundLibrary.PlayLightningBoltLoop( GetOrigin() );
		}

		if ( !VisualOnly )
		{
			var mana = _caster.Components.Get<ManaSystem>();
			if ( mana == null )
			{
				End();
				return;
			}

			_manaAccum += ( _spell != null ? _spell.ManaCost : 2f ) * Time.Delta;
			while ( _manaAccum >= 1f )
			{
				if ( !mana.ConsumeMana( 1 ) )
				{
					End();
					return;
				}
				_manaAccum -= 1f;
			}

			mana.MarkCombat();
		}

		UpdateTargets();
		UpdateBolts();

		if ( !VisualOnly )
		{
			_damageTickTimer += Time.Delta;
			if ( _damageTickTimer >= DamageTickInterval && TimeElapsed >= CastTime + WindupDuration )
			{
				_damageTickTimer = 0f;
				ApplyDamageTick();
			}
		}
	}

	void UpdateTargets()
	{
		_currentTargets.Clear();

		Vector3 origin = _caster != null && _caster.IsValid() ? _caster.WorldPosition : GetOrigin();
		Vector3 forward = GetForward();
		float cosThreshold = MathF.Cos( ConeAngleDegrees * MathF.PI / 180f );
		float rangeSqr = ConeRange * ConeRange;
		float closeRangeSqr = 150f * 150f;

		var candidates = new List<(GameObject obj, float dot)>();

		foreach ( var monster in Scene.GetAllComponents<Monster>() )
		{
			if ( monster == null || !monster.IsValid() || monster.IsDead )
				continue;

			Vector3 to = monster.WorldPosition - origin;
			float distSqr = to.LengthSquared;
			if ( distSqr > rangeSqr || distSqr < 1f )
				continue;

			Vector3 dir = to.Normal;
			float dot = forward.Dot( dir );

			bool isClose = distSqr <= closeRangeSqr;
			float effectiveThreshold = isClose ? 0f : cosThreshold;
			if ( dot < effectiveThreshold )
				continue;

			candidates.Add( ( monster.GameObject, dot ) );
		}

		foreach ( var boss in Scene.GetAllComponents<Boss>() )
		{
			if ( boss == null || !boss.IsValid() || boss.IsDead )
				continue;

			Vector3 to = boss.WorldPosition - origin;
			float distSqr = to.LengthSquared;
			if ( distSqr > rangeSqr || distSqr < 1f )
				continue;

			Vector3 dir = to.Normal;
			float dot = forward.Dot( dir );

			bool isClose = distSqr <= closeRangeSqr;
			float effectiveThreshold = isClose ? 0f : cosThreshold;
			if ( dot < effectiveThreshold )
				continue;

			candidates.Add( ( boss.GameObject, dot ) );
		}

		var dm = DuelManager.Instance;
		if ( dm != null && dm.MatchActive && dm.RoundLive && dm.IsDuelist( _caster ) )
		{
			GameObject opponent = dm.DuelistA == _caster ? dm.DuelistB : dm.DuelistA;
			if ( opponent != null && opponent.IsValid() )
			{
				var oppHealth = opponent.Components.Get<PlayerHealth>();
				if ( oppHealth != null && !oppHealth.IsDead )
				{
					Vector3 to = opponent.WorldPosition - origin;
					float distSqr = to.LengthSquared;
					if ( distSqr <= rangeSqr && distSqr >= 1f )
					{
						Vector3 dir = to.Normal;
						float dot = forward.Dot( dir );

						bool isClose = distSqr <= closeRangeSqr;
						float effectiveThreshold = isClose ? 0f : cosThreshold;
						if ( dot >= effectiveThreshold )
							candidates.Add( ( opponent, dot ) );
					}
				}
			}
		}

		candidates.Sort( ( a, b ) => b.dot.CompareTo( a.dot ) );

		int count = Math.Min( candidates.Count, MaxTargets );
		for ( int i = 0; i < count; i++ )
			_currentTargets.Add( candidates[i].obj );
	}

	void UpdateBolts()
	{
		Vector3 origin = GetOrigin();
		Vector3 forward = GetForward();
		Vector3 perpA = Vector3.Cross( forward, Vector3.Up ).Normal;
		if ( perpA.LengthSquared < 0.01f )
			perpA = Vector3.Cross( forward, Vector3.Forward ).Normal;
		Vector3 perpB = Vector3.Cross( forward, perpA ).Normal;

		float spreadRad = BoltSpreadAngle * MathF.PI / 180f;

		float postCastTime = TimeElapsed - CastTime;
		float windup = WindupDuration > 0f ? MathF.Min( 1f, postCastTime / WindupDuration ) : 1f;
		windup = windup * windup * ( 3f - 2f * windup );

		for ( int i = 0; i < _bolts.Count; i++ )
		{
			var bolt = _bolts[i];
			if ( bolt == null || !bolt.IsValid() )
				continue;

			float oa = Game.Random.Float( -OriginJitter, OriginJitter );
			float ob = Game.Random.Float( -OriginJitter, OriginJitter );
			bolt.OriginPosition = origin + perpA * oa + perpB * ob;

			if ( i < _currentTargets.Count && _currentTargets[i] != null && _currentTargets[i].IsValid() )
			{
				Vector3 targetPos = _currentTargets[i].WorldPosition + Vector3.Up * 30f;
				bolt.TargetPosition = Vector3.Lerp( origin, targetPos, windup );
				continue;
			}

			float sectorBaseAngle = ( i + 0.5f ) / _bolts.Count * MathF.PI * 2f;
			float sectorJitter = Game.Random.Float( -0.3f, 0.3f ) * ( MathF.PI * 2f / _bolts.Count );
			float roll = sectorBaseAngle + sectorJitter;

			float coneAngle = Game.Random.Float( spreadRad * 0.6f, spreadRad );

			Vector3 spreadDir = forward * MathF.Cos( coneAngle )
				+ perpA * MathF.Sin( coneAngle ) * MathF.Cos( roll )
				+ perpB * MathF.Sin( coneAngle ) * MathF.Sin( roll );
			spreadDir = spreadDir.Normal;

			float rangeJitter = Game.Random.Float( 0.75f, 1f );
			bolt.TargetPosition = origin + spreadDir * ConeRange * TrunkLengthRatio * rangeJitter * windup;
		}
	}

	void ApplyDamageTick()
	{
		if ( _spell == null || _caster == null )
			return;

		var inventory = _caster.Components.Get<Inventory>();
		var skills = _caster.Components.Get<Skills>();
		if ( inventory == null || skills == null )
			return;

		var weaponDef = inventory.GetEquippedWeaponDef();
		float staffPower = weaponDef != null ? weaponDef.WeaponPower : 1f;
		float skillBonus = skills.GetCombatPower( SkillType.Magic );

		float buffMult = 1f;
		var potionSystem = _caster.Components.Get<PotionSystem>();
		if ( potionSystem != null )
			buffMult = potionSystem.GetBuffMultiplier( BuffType.Magic );

		float sicknessMult = 1f;
		var mana = _caster.Components.Get<ManaSystem>();
		if ( mana != null )
			sicknessMult = mana.GetManaDamageMultiplier();

		float damagePerTick = _spell != null ? _spell.DamageMultiplier : 1f;
		float power = staffPower * damagePerTick * skillBonus * buffMult * sicknessMult;
		int damage = (int)power;
		if ( damage < 1 ) damage = 1;

		foreach ( var target in _currentTargets )
		{
			if ( target == null || !target.IsValid() )
				continue;

			var pvpTarget = PvpCombat.ResolveTarget( target, _caster );
			if ( pvpTarget != null )
			{
				int dealt = PvpCombat.ResolveDamage( damage, CombatStyle.Magic, pvpTarget );
				var targetHealth = pvpTarget.Components.Get<PlayerHealth>();
				if ( targetHealth != null )
				{
					targetHealth.TakeDamage( dealt );
					_caster?.Components.Get<PlayerCombat>()?.NotifyPvpHit( pvpTarget, dealt, false, false );
				}
				continue;
			}

			var monster = target.Components.Get<Monster>();
			if ( monster != null && !monster.IsDead )
			{
				float triangleMult = CombatTriangle.GetDealMultiplier( CombatStyle.Magic, monster.CombatStyle );
				int dealt = (int)( damage * triangleMult );
				if ( dealt < 1 ) dealt = 1;

				monster.TakeDamage( dealt, _caster );
				DamagePopupBroadcaster.Broadcast( monster.WorldPosition + Vector3.Up * 50f, dealt, monster.MaxHealth, false );
				continue;
			}

			var boss = target.Components.Get<Boss>();
			if ( boss != null && !boss.IsDead )
			{
				float triangleMult = CombatTriangle.GetDealMultiplier( CombatStyle.Magic, boss.CombatStyle );
				int dealt = (int)( damage * triangleMult );
				if ( dealt < 1 ) dealt = 1;

				boss.TakeDamage( dealt, _caster );
				DamagePopupBroadcaster.Broadcast( boss.WorldPosition + Vector3.Up * 50f, dealt, boss.MaxHealth, false );
			}
		}

		if ( _currentTargets.Count > 0 )
			skills.AddXp( SkillType.Magic, 1 );
	}

	Vector3 GetOrigin()
	{
		if ( _caster == null || !_caster.IsValid() )
			return WorldPosition;

		Vector3 forward = GetRawForward();
		Vector3 right;
		if ( _aimSource != null && _aimSource.IsValid() )
			right = _aimSource.WorldRotation.Right;
		else
			right = _caster.WorldRotation.Right;

		return _caster.WorldPosition
			+ Vector3.Up * HeightOffset
			+ forward * ForwardOffset
			+ right * LateralOffset;
	}

	Vector3 GetRawForward()
	{
		if ( _aimSource != null && _aimSource.IsValid() )
			return _aimSource.WorldRotation.Forward;
		if ( _caster != null )
			return _caster.WorldRotation.Forward;
		return Vector3.Forward;
	}

	Vector3 GetForward()
	{
		if ( VisualOnly )
			return GetRawForward();

		var camera = Scene.Camera;
		if ( camera != null )
		{
			Vector3 camPos = camera.WorldPosition;
			Vector3 camForward = camera.WorldRotation.Forward;
			Vector3 camEnd = camPos + camForward * AimTraceDistance;

			var aimTrace = Scene.Trace
				.Ray( camPos, camEnd )
				.UseHitboxes( true )
				.IgnoreGameObjectHierarchy( _caster != null && _caster.IsValid() ? _caster : GameObject )
				.Run();

			Vector3 aimPoint = aimTrace.Hit ? aimTrace.HitPosition : camEnd;
			Vector3 origin = GetOrigin();
			Vector3 dir = aimPoint - origin;
			if ( dir.LengthSquared > 0.01f )
				return dir.Normal;
		}

		return GetRawForward();
	}
}