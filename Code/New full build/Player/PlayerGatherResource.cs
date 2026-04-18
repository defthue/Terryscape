using Sandbox;
using System.Collections.Generic;

public sealed class PlayerGatherResource : Component
{
	[Property] public GameObject DirectionSource { get; set; }
	[Property] public float HeightOffset { get; set; } = 30f;
	[Property] public float ForwardOffset { get; set; } = 10f;
	[Property] public float TraceDistance { get; set; } = 120f;
	[Property] public float TraceRadius { get; set; } = 20f;
	[Property] public bool ShowDebugTrace { get; set; } = true;
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }
	[Property] public float SwingCooldown { get; set; } = 0.8f;
	[Property] public float PunchStanceResetTime { get; set; } = 2.0f;
	[Property] public int ForageHoldType { get; set; } = 4;
	[Property] public int ForageHoldTypeAttack { get; set; } = 0;

	public static bool UIOpen { get; private set; } = false;
	public static bool IsForaging { get; private set; } = false;

	float _cooldownRemaining = 0f;
	float _punchStanceTimer = 0f;

	Dictionary<ResourceNode, int> _localNodeHealth = new();

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( Input.Pressed( "inventory" ) )
		{
			UIOpen = !UIOpen;
			Mouse.Visibility = UIOpen ? MouseVisibility.Visible : MouseVisibility.Hidden;
		}

		if ( UIOpen )
			return;

		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null && potionSystem.IsDrinking )
			return;

		var shooter = GameObject.Components.Get<ProjectileShooter>();
		if ( shooter != null && shooter.IsDrawing )
			return;

		if ( _cooldownRemaining > 0f )
			_cooldownRemaining -= Time.Delta;

		if ( _punchStanceTimer > 0f )
		{
			_punchStanceTimer -= Time.Delta;

			if ( _punchStanceTimer <= 0f )
			{
				IsForaging = false;
				ResetToIdle();
			}
		}

		if ( Input.Pressed( "attack1" ) )
		{
			var inventory = GameObject.Components.Get<Inventory>();
			if ( inventory != null && inventory.IsWeaponRanged() )
			{
				if ( shooter != null )
					shooter.StartDraw();
				return;
			}

			if ( _cooldownRemaining <= 0f )
			{
				_cooldownRemaining = SwingCooldown;
				Punch();
			}
		}
	}

	HeldToolController GetHeldToolController()
	{
		foreach ( var child in GameObject.Children )
		{
			var controller = child.Components.Get<HeldToolController>();
			if ( controller != null )
				return controller;
		}
		return null;
	}

	void TriggerSwingAnimation( bool foraging )
	{
		if ( BodyRenderer == null )
			return;

		var inventory = GameObject.Components.Get<Inventory>();
		bool hasWeapon = inventory != null && inventory.GetEquipped( EquipSlot.Weapon ) != ItemId.None;

		if ( foraging )
		{
			IsForaging = true;
			var controller = GetHeldToolController();
			if ( controller != null )
			{
				controller.IsPunching = true;
				controller.SetHoldType( ForageHoldType );
			}
			BodyRenderer.Set( "holdtype", ForageHoldType );
			BodyRenderer.Set( "holdtype_attack", ForageHoldTypeAttack );
			_punchStanceTimer = PunchStanceResetTime;
		}
		else if ( !hasWeapon )
		{
			var controller = GetHeldToolController();
			if ( controller != null )
			{
				controller.IsPunching = true;
				controller.SetHoldType( 5 );
			}
			_punchStanceTimer = PunchStanceResetTime;
		}

		BodyRenderer.Set( "b_attack", true );
		BroadcastAttack();
	}

	[Rpc.Broadcast]
	void BroadcastAttack()
	{
		if ( BodyRenderer != null )
			BodyRenderer.Set( "b_attack", true );
	}

	void ResetToIdle()
	{
		var inventory = GameObject.Components.Get<Inventory>();
		bool hasWeapon = inventory != null && inventory.GetEquipped( EquipSlot.Weapon ) != ItemId.None;

		if ( hasWeapon )
			return;

		var controller = GetHeldToolController();
		if ( controller != null )
		{
			controller.IsPunching = false;
			controller.SetHoldType( 0 );
		}

		if ( BodyRenderer != null )
			BodyRenderer.Set( "holdtype", 0 );
	}

	void Punch()
	{
		if ( NpcInteract.ActiveNpc != null )
			NpcInteract.ActiveNpc.CloseDialogue();

		var cam = Scene.Camera;
		if ( cam == null )
			return;

		var forward = cam.WorldRotation.Forward;
		var playerPos = GameObject.WorldPosition;
		var start = playerPos + Vector3.Up * HeightOffset + forward * ForwardOffset;
		var end = start + forward * TraceDistance;

		var trace = Scene.Trace
			.Ray( start, end )
			.Radius( TraceRadius )
			.UseHitboxes( true )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( ShowDebugTrace )
		{
			var color = trace.Hit ? Color.Green : Color.Red;
			Gizmo.Draw.Color = color;
			Gizmo.Draw.Line( start, trace.Hit ? trace.EndPosition : end );
			if ( trace.Hit )
				Gizmo.Draw.LineSphere( trace.EndPosition, TraceRadius );
			else
				Gizmo.Draw.LineSphere( end, TraceRadius );
			Gizmo.Draw.LineSphere( start, TraceRadius );
		}

		if ( !trace.Hit )
		{
			TriggerSwingAnimation( false );
			GameLog.Add( "You swing but hit nothing.", "#6a6a6a" );
			return;
		}

		var node = trace.GameObject.Components.Get<ResourceNode>();
		var monster = trace.GameObject.Components.Get<Monster>();

		if ( node == null && monster == null )
		{
			var retryTrace = Scene.Trace
				.Ray( start, end )
				.Radius( TraceRadius )
				.UseHitboxes( true )
				.IgnoreGameObjectHierarchy( GameObject )
				.IgnoreGameObjectHierarchy( trace.GameObject )
				.Run();

			if ( retryTrace.Hit )
			{
				node = retryTrace.GameObject.Components.Get<ResourceNode>();
				monster = retryTrace.GameObject.Components.Get<Monster>();
			}

			if ( node == null && monster == null )
			{
				TriggerSwingAnimation( false );
				GameLog.Add( "You swing but hit nothing.", "#6a6a6a" );
				return;
			}
		}

		var inventory = GameObject.Components.Get<Inventory>();
		var skills = GameObject.Components.Get<Skills>();

		if ( inventory == null || skills == null )
			return;

		if ( monster != null )
		{
			TriggerSwingAnimation( false );
			HandleCombatHit( monster, inventory, skills );
			return;
		}

		if ( node != null )
		{
			TriggerSwingAnimation( node.GatherSkill == GatherType.Foraging );
			HandleResourceHit( node, inventory, skills );
		}
	}

	void HandleCombatHit( Monster monster, Inventory inventory, Skills skills )
	{
		var weaponDef = inventory.GetEquippedWeaponDef();
		CombatStyle playerStyle = CombatTriangle.GetStyleFromWeapon( weaponDef );

		SkillType combatSkill = SkillType.Attack;
		float weaponPower = 1f;

		if ( weaponDef != null )
		{
			weaponPower = weaponDef.WeaponPower;

			if ( weaponDef.Type == ItemType.MagicWeapon )
				combatSkill = SkillType.Magic;
		}

		float skillBonus = skills.GetCombatPower( combatSkill );
		float triangleMult = CombatTriangle.GetDealMultiplier( playerStyle, monster.CombatStyle );

		float buffMult = 1f;
		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null )
		{
			if ( combatSkill == SkillType.Attack )
				buffMult = potionSystem.GetBuffMultiplier( BuffType.Attack );
			else if ( combatSkill == SkillType.Magic )
				buffMult = potionSystem.GetBuffMultiplier( BuffType.Magic );
		}

		int damage = (int)( weaponPower * skillBonus * triangleMult * buffMult );
		if ( damage < 1 ) damage = 1;

		GameLog.Add( $"You hit {monster.MonsterName} for {damage} damage. ({monster.CurrentHealth}/{monster.MaxHealth} HP left)", "#a8c8a8" );

		monster.TakeDamage( damage, GameObject );
	}

	void HandleResourceHit( ResourceNode node, Inventory inventory, Skills skills )
	{
		var weaponDef = inventory.GetEquippedWeaponDef();
		bool bareHanded = weaponDef == null;
		bool isTier0 = node.Tier == 0;
		bool isForaging = node.GatherSkill == GatherType.Foraging;

		if ( isForaging )
		{
			if ( !bareHanded )
			{
				GameLog.Add( "You need empty hands to forage.", "#c86464" );
				return;
			}

			if ( !skills.MeetsRequirement( SkillType.Enchanting, node.RequiredLevel ) )
			{
				GameLog.Add( $"You need Enchanting level {node.RequiredLevel} to gather this.", "#c86464" );
				return;
			}

			float skillBonus = skills.GetToolPower( SkillType.Enchanting );
			int totalDamage = (int)skillBonus;
			if ( totalDamage < 1 ) totalDamage = 1;

			HitNode( node, totalDamage, inventory, skills );
			return;
		}

		if ( weaponDef != null && ( weaponDef.Type == ItemType.MeleeWeapon || weaponDef.Type == ItemType.RangedWeapon || weaponDef.Type == ItemType.MagicWeapon ) )
		{
			GameLog.Add( "You can't harvest resources with a weapon!", "#c86464" );
			return;
		}

		if ( !isTier0 )
		{
			if ( node.RequiresHatchet() && !inventory.IsWeaponHatchet() )
			{
				GameLog.Add( "You need a hatchet to harvest this.", "#c86464" );
				return;
			}

			if ( node.RequiresPickaxe() && !inventory.IsWeaponPickaxe() )
			{
				GameLog.Add( "You need a pickaxe to harvest this.", "#c86464" );
				return;
			}

			int requiredToolTier = node.Tier - 1;
			if ( requiredToolTier > 0 && weaponDef != null && weaponDef.Tier < requiredToolTier )
			{
				GameLog.Add( $"You need a tier {requiredToolTier}+ tool to harvest this.", "#c86464" );
				return;
			}

			if ( node.RequiredLevel > 1 && !skills.MeetsRequirement( node.GetSkillType(), node.RequiredLevel ) )
			{
				GameLog.Add( $"You need {node.GetSkillType()} level {node.RequiredLevel} to harvest this.", "#c86464" );
				return;
			}
		}

		if ( !bareHanded )
		{
			if ( node.GatherSkill == GatherType.Woodcutting && inventory.IsWeaponPickaxe() )
			{
				GameLog.Add( "You can't chop trees with a pickaxe.", "#c86464" );
				return;
			}

			if ( node.GatherSkill == GatherType.Mining && inventory.IsWeaponHatchet() )
			{
				GameLog.Add( "You can't mine rocks with a hatchet.", "#c86464" );
				return;
			}
		}

		float toolPower = bareHanded ? 1.0f : inventory.GetToolPower();
		SkillType gatherSkill = node.GetSkillType();
		float gatherSkillBonus = skills.GetToolPower( gatherSkill );

		int damage = (int)( toolPower * gatherSkillBonus );
		if ( damage < 1 ) damage = 1;

		HitNode( node, damage, inventory, skills );
	}

	void HitNode( ResourceNode node, int damage, Inventory inventory, Skills skills )
	{
		if ( node.IsBroken )
		{
			_localNodeHealth.Remove( node );
			return;
		}

		if ( !_localNodeHealth.ContainsKey( node ) || _localNodeHealth[node] > node.CurrentHealth )
			_localNodeHealth[node] = node.CurrentHealth;

		_localNodeHealth[node] -= damage;

		GameLog.Add( $"You hit {node.GetDisplayName()} for {damage} damage. ({System.Math.Max( 0, _localNodeHealth[node] )}/{node.MaxHealth} HP left)", "#a8c8a8" );

		bool willBreak = _localNodeHealth[node] <= 0;

		node.TakeDamage( damage, GameObject );

		if ( willBreak )
		{
			inventory.AddItem( node.ResourceItem, node.ResourceAmount );

			var def = ItemDatabase.Get( node.ResourceItem );
			string itemName = def != null ? def.Name : node.ResourceItem.ToString();
			GameLog.Add( $"You collected {node.ResourceAmount}x {itemName} from {node.GetDisplayName()}.", "#6db8f0" );

			skills.AddXp( node.GetSkillType(), node.XpReward );

			_localNodeHealth.Remove( node );
		}
	}
}