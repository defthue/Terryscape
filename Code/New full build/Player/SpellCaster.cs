using Sandbox;
using System;

public sealed class SpellCaster : Component
{
	[Property, Group( "Spell Prefabs" )] public GameObject FireballPrefab { get; set; }
	[Property, Group( "Spell Prefabs" )] public GameObject IceShardPrefab { get; set; }
	[Property, Group( "Spell Prefabs" )] public GameObject DarkBlastPrefab { get; set; }
	[Property, Group( "Spell Prefabs" )] public GameObject BarrierPrefab { get; set; }

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
	[Property, Group( "Barrier" )] public float BarrierWidth { get; set; } = 4f;
	[Property, Group( "Barrier" )] public float BarrierHeight { get; set; } = 3f;
	[Property, Group( "Barrier" )] public float BarrierDepth { get; set; } = 0.2f;
	[Property, Group( "Barrier" )] public float BarrierDuration { get; set; } = 5f;
	[Property, Group( "Barrier" )] public Color BarrierTint { get; set; } = new Color( 0.2f, 0.4f, 1f, 0.4f );
	[Property, Group( "Barrier" )] public Vector3 BarrierColliderSize { get; set; } = new Vector3( 200f, 20f, 150f );

	[Property, Group( "Frozen Bonus" )] public float FrozenBonusDamage { get; set; } = 1.5f;

	public bool IsCasting { get; private set; }
	public bool IsCastReady { get; private set; }
	public float CastProgress => _activeSpell != null && _activeSpell.MinCastTime > 0f ? MathF.Min( _castTimer / _activeSpell.MinCastTime, 1f ) : 1f;
	public SpellDefinition ActiveSpell => _activeSpell;

	SpellDefinition _activeSpell;
	float _castTimer;
	string _castAction;
	Vector3 _castStartPos;

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
			default: return null;
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		var inventory = GameObject.Components.Get<Inventory>();
		if ( inventory == null || !inventory.IsWeaponMagic() )
		{
			if ( IsCasting )
				CancelCast();
			return;
		}

		if ( PlayerGatherResource.UIOpen )
			return;

		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null && potionSystem.IsDrinking )
			return;

		if ( !IsCasting )
		{
			if ( Input.Pressed( "Slot1" ) )
				StartCast( SpellId.Fireball, "Slot1" );
			else if ( Input.Pressed( "Slot2" ) )
				StartCast( SpellId.IceShard, "Slot2" );
			else if ( Input.Pressed( "Slot3" ) )
				StartCast( SpellId.DarkBlast, "Slot3" );
			else if ( Input.Pressed( "Slot4" ) )
				StartCast( SpellId.ArcaneBarrier, "Slot4" );
		}
		else
		{
			float movedDist = ( WorldPosition - _castStartPos ).Length;
			if ( movedDist > 5f )
			{
				CancelCast();
				GameLog.Add( "Cast cancelled — you moved.", "#6a6a6a" );
				return;
			}

			_castTimer += Time.Delta;

			if ( _castTimer >= _activeSpell.MinCastTime )
			{
				IsCastReady = true;
				ReleaseCast();
			}
		}
	}

	void StartCast( SpellId spellId, string action )
	{
		var spell = SpellDatabase.Get( spellId );
		if ( spell == null )
			return;

		var mana = GameObject.Components.Get<ManaSystem>();
		if ( mana == null || !mana.HasMana( spell.ManaCost ) )
		{
			GameLog.Add( $"Not enough mana to cast {spell.Name}. ({( mana != null ? mana.CurrentMana : 0 )}/{spell.ManaCost})", "#c86464" );
			return;
		}

		if ( spell.Type == SpellType.Projectile )
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
		_castStartPos = WorldPosition;
		IsCasting = true;
		IsCastReady = false;
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

		if ( BodyRenderer != null )
		{
			BodyRenderer.Set( "holdtype", 6 );
			BodyRenderer.Set( "holdtype_attack", 0 );
			BodyRenderer.Set( "b_attack", true );
		}

		BroadcastCastAnim();

		if ( spell.Type == SpellType.Barrier )
		{
			SpawnBarrier( spell );
			return;
		}

		SpawnProjectile( spell );
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

		float totalPower = staffPower * spell.DamageMultiplier * skillBonus * buffMult;
		int damage = (int)totalPower;
		if ( damage < 1 ) damage = 1;

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

		var projectile = prefab.Clone( spawnPos );
		if ( projectile == null )
			return;

		projectile.WorldRotation = AimSource.WorldRotation;
		projectile.NetworkSpawn();

		var spellProj = projectile.Components.Get<SpellProjectile>();
		if ( spellProj != null )
		{
			spellProj.Velocity = aimForward * spell.ProjectileSpeed;
			spellProj.Damage = damage;
			spellProj.Shooter = GameObject;
			spellProj.SpellId = spell.Id;
			spellProj.MaxRange = spell.MaxRange;
			spellProj.MaxLifetime = spell.MaxLifetime;
			spellProj.TraceRadius = spell.TraceRadius;
			spellProj.FreezeDuration = spell.FreezeDuration;
			spellProj.FrozenBonusDamage = FrozenBonusDamage;
		}

		skills.AddXp( SkillType.Magic, 2 );

		switch ( spell.Id )
		{
			case SpellId.Fireball: SoundLibrary.PlayFireball( spawnPos ); break;
			case SpellId.IceShard: SoundLibrary.PlayIceShard( spawnPos ); break;
			case SpellId.DarkBlast: SoundLibrary.PlayDarkBlast( spawnPos ); break;
		}

		int manaLeft = 0;
		var manaCheck = GameObject.Components.Get<ManaSystem>();
		if ( manaCheck != null )
			manaLeft = manaCheck.CurrentMana;

		GameLog.Add( $"You cast {spell.Name}! ({damage} power, {manaLeft} mana left)", "#7a5aaa" );
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

		var barrierRotation = Rotation.LookAt( flatForward, Vector3.Up ) * Rotation.FromYaw( 90f );

		CreateBarrierLocal( spawnPos, barrierRotation );
		PushOverlapping( spawnPos, flatForward, BarrierWidth * 25f, 30f );
		BroadcastBarrier( spawnPos, barrierRotation );

		int manaLeft = 0;
		var manaCheck = GameObject.Components.Get<ManaSystem>();
		if ( manaCheck != null )
			manaLeft = manaCheck.CurrentMana;

		GameLog.Add( $"You conjure an Arcane Barrier! ({manaLeft} mana left)", "#7a5aaa" );
	}

	void CreateBarrierLocal( Vector3 pos, Rotation rot )
	{
		if ( BarrierPrefab != null )
		{
			var barrier = BarrierPrefab.Clone( pos );
			barrier.WorldRotation = rot * Rotation.FromYaw( 90f );
			barrier.Tags.Add( "solid" );

			var barrierComp = barrier.Components.Get<ArcaneBarrier>();
			if ( barrierComp == null )
				barrierComp = barrier.Components.Create<ArcaneBarrier>();
			barrierComp.Duration = BarrierDuration;

			foreach ( var col in barrier.Components.GetAll<Collider>() )
				col.Destroy();

			var collider = barrier.Components.Create<BoxCollider>();
			collider.Scale = BarrierColliderSize;
			collider.Static = true;

			var renderer = barrier.Components.Get<ModelRenderer>();
			if ( renderer != null )
				renderer.Tint = BarrierTint;

			return;
		}

		var fallback = new GameObject( true, "ArcaneBarrier" );
		fallback.WorldPosition = pos;
		fallback.WorldRotation = rot;
		fallback.WorldScale = new Vector3( BarrierWidth, BarrierDepth, BarrierHeight );
		fallback.Tags.Add( "solid" );

		var fbBarrier = fallback.Components.Create<ArcaneBarrier>();
		fbBarrier.Duration = BarrierDuration;

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
}