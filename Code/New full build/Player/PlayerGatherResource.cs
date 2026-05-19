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
	[Property] public float StaffMeleeDamageMultiplier { get; set; } = 0.5f;

	public static bool UIOpen { get; private set; } = false;
	public static bool IsForaging { get; private set; } = false;

	float _cooldownRemaining = 0f;
	float _punchStanceTimer = 0f;

	Dictionary<ResourceNode, int> _localNodeHealth = new();

	ResourceNode _autoGatherNode = null;

	public static void ForceCloseUI()
	{
		UIOpen = false;
		Mouse.Visibility = MouseVisibility.Hidden;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		bool anyHudOpenNow =
			ShopStation.ActiveShop != null ||
			ShopStation.ShowingChoice ||
			BankStation.ActiveBank != null ||
			CraftingStation.ActiveStation != null ||
			EnchantingStation.ActiveStation != null ||
			TeleportStone.ActiveStone != null ||
			JournalStation.IsOpen ||
			LeaderboardStation.IsOpen ||
			SpellbookStation.IsOpen ||
			NpcInteract.ActiveNpc != null ||
			MinimapState.IsFullMapOpen ||
			WelcomeHudState.IsOpen ||
			BlackjackSeat.LocalSeat != null;

		if ( UIOpen && anyHudOpenNow )
			UIOpen = false;

		if ( BlackjackSeat.LocalSeat != null )
		{
			if ( Input.Pressed( "inventory" ) )
			{
				if ( Mouse.Visibility == MouseVisibility.Visible )
					Mouse.Visibility = MouseVisibility.Hidden;
				else
					Mouse.Visibility = MouseVisibility.Visible;
			}
			return;
		}

		if ( Input.Pressed( "inventory" ) )
		{
			bool anyHudOpen = anyHudOpenNow;

			if ( UIOpen || anyHudOpen )
			{
				if ( ShopStation.ActiveShop != null || ShopStation.ShowingChoice )
					ShopStation.CloseAll();

				if ( BankStation.ActiveBank != null )
					BankStation.Close();

				if ( CraftingStation.ActiveStation != null )
					CraftingStation.Close();

				if ( EnchantingStation.ActiveStation != null )
					EnchantingStation.Close();

				if ( TeleportStone.ActiveStone != null )
					TeleportStone.Close();

				if ( JournalStation.IsOpen )
					JournalStation.Close();

				if ( LeaderboardStation.IsOpen )
					LeaderboardStation.Close();

				if ( SpellbookStation.IsOpen )
					SpellbookStation.Close();

				if ( NpcInteract.ActiveNpc != null )
					NpcInteract.ActiveNpc.CloseDialogue();

				if ( MinimapState.IsFullMapOpen )
					MinimapState.IsFullMapOpen = false;

				if ( WelcomeHudState.IsOpen )
					WelcomeHudState.IsOpen = false;

				UIOpen = false;
				Mouse.Visibility = MouseVisibility.Hidden;
			}
			else
			{
				UIOpen = true;
				Mouse.Visibility = MouseVisibility.Visible;
			}
		}

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

		var potionSystemComp = GameObject.Components.Get<PotionSystem>();
		bool drinking = potionSystemComp != null && potionSystemComp.IsDrinking;

		var shooterComp = GameObject.Components.Get<ProjectileShooter>();
		bool drawing = shooterComp != null && shooterComp.IsDrawing;

		if ( _autoGatherNode != null && !drinking && !drawing && _cooldownRemaining <= 0f )
		{
			if ( !_autoGatherNode.IsValid() || _autoGatherNode.IsBroken )
			{
				_autoGatherNode = null;
			}
			else
			{
				_cooldownRemaining = SwingCooldown;
				Punch();
			}
		}

		if ( UIOpen )
			return;

		if ( ShopStation.ActiveShop != null || ShopStation.ShowingChoice )
			return;

		if ( BankStation.ActiveBank != null )
			return;

		if ( CraftingStation.ActiveStation != null )
			return;

		if ( EnchantingStation.ActiveStation != null )
			return;

		if ( TeleportStone.ActiveStone != null )
			return;

		if ( NpcInteract.ActiveNpc != null )
			return;

		if ( JournalStation.IsOpen )
			return;

		if ( LeaderboardStation.IsOpen )
			return;

		if ( SpellbookStation.IsOpen )
			return;

		if ( MinimapState.IsFullMapOpen )
			return;

		if ( WelcomeHudState.IsOpen )
			return;

		if ( BlackjackSeat.LocalSeat != null )
			return;

		if ( drinking )
			return;

		if ( drawing )
			return;

		if ( Input.Pressed( "attack1" ) )
		{
			var inventory = GameObject.Components.Get<Inventory>();
			if ( inventory != null && inventory.IsWeaponRanged() )
			{
				if ( shooterComp != null )
					shooterComp.StartDraw();
				return;
			}

			if ( inventory != null && inventory.IsWeaponMagic() )
				return;

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
			_autoGatherNode = null;
			TriggerSwingAnimation( false );
			GameLog.Add( "You swing but hit nothing.", "#6a6a6a" );
			SoundLibrary.PlayHitNothing();
			return;
		}

		var node = trace.GameObject.Components.Get<ResourceNode>();
		var monster = trace.GameObject.Components.Get<Monster>();
		var boss = trace.GameObject.Components.Get<Boss>();

		if ( node == null && monster == null && boss == null )
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
				boss = retryTrace.GameObject.Components.Get<Boss>();
			}

			if ( node == null && monster == null && boss == null )
			{
				_autoGatherNode = null;
				TriggerSwingAnimation( false );
				GameLog.Add( "You swing but hit nothing.", "#6a6a6a" );
				SoundLibrary.PlayHitNothing();
				return;
			}
		}

		var inventory = GameObject.Components.Get<Inventory>();
		var skills = GameObject.Components.Get<Skills>();

		if ( inventory == null || skills == null )
			return;

		if ( boss != null )
		{
			_autoGatherNode = null;
			TriggerSwingAnimation( false );
			HandleBossHit( boss, inventory, skills );
			return;
		}

		if ( monster != null )
		{
			_autoGatherNode = null;
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

		float staffMeleeMult = ( weaponDef != null && weaponDef.Type == ItemType.MagicWeapon ) ? StaffMeleeDamageMultiplier : 1f;

		float enchantMult = 1f;
		if ( weaponDef != null && ( weaponDef.Type == ItemType.MeleeWeapon || weaponDef.Type == ItemType.Tool ) )
			enchantMult = 1f + inventory.GetEnchantmentBonus( EnchantmentType.Sharpness ) / 100f;

		int damage = (int)( weaponPower * skillBonus * triangleMult * buffMult * staffMeleeMult * enchantMult );
		if ( damage < 1 ) damage = 1;

		bool isCrit = CombatConstants.RollCrit();
		if ( isCrit )
			damage = (int)( damage * CombatConstants.CritMultiplier );

		var manaCombat = GameObject.Components.Get<ManaSystem>();
		if ( manaCombat != null )
			manaCombat.MarkCombat();

		GameLog.Add( $"You hit {monster.MonsterName} for {damage} damage{( isCrit ? " (CRIT!)" : "" )}. ({monster.CurrentHealth}/{monster.MaxHealth} HP left)", "#a8c8a8" );

		SoundLibrary.PlayMonsterHit( monster.WorldPosition );

		monster.TakeDamage( damage, GameObject );

		DamagePopupBroadcaster.Broadcast( monster.WorldPosition + Vector3.Up * 50f, damage, monster.MaxHealth, isCrit );
	}

	void HandleBossHit( Boss boss, Inventory inventory, Skills skills )
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
		float triangleMult = CombatTriangle.GetDealMultiplier( playerStyle, boss.CombatStyle );

		float buffMult = 1f;
		var potionSystem = GameObject.Components.Get<PotionSystem>();
		if ( potionSystem != null )
		{
			if ( combatSkill == SkillType.Attack )
				buffMult = potionSystem.GetBuffMultiplier( BuffType.Attack );
			else if ( combatSkill == SkillType.Magic )
				buffMult = potionSystem.GetBuffMultiplier( BuffType.Magic );
		}

		float staffMeleeMult = ( weaponDef != null && weaponDef.Type == ItemType.MagicWeapon ) ? StaffMeleeDamageMultiplier : 1f;

		float enchantMult = 1f;
		if ( weaponDef != null && ( weaponDef.Type == ItemType.MeleeWeapon || weaponDef.Type == ItemType.Tool ) )
			enchantMult = 1f + inventory.GetEnchantmentBonus( EnchantmentType.Sharpness ) / 100f;

		int damage = (int)( weaponPower * skillBonus * triangleMult * buffMult * staffMeleeMult * enchantMult );
		if ( damage < 1 ) damage = 1;

		bool isCrit = CombatConstants.RollCrit();
		if ( isCrit )
			damage = (int)( damage * CombatConstants.CritMultiplier );

		var manaCombat = GameObject.Components.Get<ManaSystem>();
		if ( manaCombat != null )
			manaCombat.MarkCombat();

		GameLog.Add( $"You hit {boss.BossName} for {damage} damage{( isCrit ? " (CRIT!)" : "" )}. ({boss.CurrentHealth}/{boss.MaxHealth} HP left)", "#a8c8a8" );

		SoundLibrary.PlayMonsterHit( boss.WorldPosition );

		boss.TakeDamage( damage, GameObject );

		DamagePopupBroadcaster.Broadcast( boss.WorldPosition + Vector3.Up * 50f, damage, boss.MaxHealth, isCrit );
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
				_autoGatherNode = null;
				GameLog.Add( "You need empty hands to forage.", "#c86464" );
				TriggerSwingAnimation( false );
				SoundLibrary.PlayHitNothing();
				return;
			}

			if ( !skills.MeetsRequirement( SkillType.Enchanting, node.RequiredLevel ) )
			{
				_autoGatherNode = null;
				GameLog.Add( $"You need Enchanting level {node.RequiredLevel} to gather this.", "#c86464" );
				TriggerSwingAnimation( false );
				SoundLibrary.PlayHitNothing();
				return;
			}

			float skillBonus = skills.GetToolPower( SkillType.Enchanting );
			int totalDamage = (int)skillBonus;
			if ( totalDamage < 1 ) totalDamage = 1;

			_autoGatherNode = node;
			HitNode( node, totalDamage, inventory, skills );
			return;
		}

		if ( weaponDef != null && ( weaponDef.Type == ItemType.MeleeWeapon || weaponDef.Type == ItemType.RangedWeapon || weaponDef.Type == ItemType.MagicWeapon ) )
		{
			_autoGatherNode = null;
			GameLog.Add( "You can't harvest resources with a weapon!", "#c86464" );
			TriggerSwingAnimation( false );
			SoundLibrary.PlayHitNothing();
			return;
		}

		if ( !isTier0 )
		{
			if ( node.RequiresHatchet() && !inventory.IsWeaponHatchet() )
			{
				_autoGatherNode = null;
				GameLog.Add( "You need a hatchet to harvest this.", "#c86464" );
				TriggerSwingAnimation( false );
				SoundLibrary.PlayHitNothing();
				return;
			}

			if ( node.RequiresPickaxe() && !inventory.IsWeaponPickaxe() )
			{
				_autoGatherNode = null;
				GameLog.Add( "You need a pickaxe to harvest this.", "#c86464" );
				TriggerSwingAnimation( false );
				SoundLibrary.PlayHitNothing();
				return;
			}

			int requiredToolTier = node.Tier - 1;
			if ( requiredToolTier > 0 && weaponDef != null && weaponDef.Tier < requiredToolTier )
			{
				_autoGatherNode = null;
				GameLog.Add( $"You need a tier {requiredToolTier}+ tool to harvest this.", "#c86464" );
				TriggerSwingAnimation( false );
				SoundLibrary.PlayHitNothing();
				return;
			}

			if ( node.RequiredLevel > 1 && !skills.MeetsRequirement( node.GetSkillType(), node.RequiredLevel ) )
			{
				_autoGatherNode = null;
				GameLog.Add( $"You need {node.GetSkillType()} level {node.RequiredLevel} to harvest this.", "#c86464" );
				TriggerSwingAnimation( false );
				SoundLibrary.PlayHitNothing();
				return;
			}
		}

		if ( !bareHanded )
		{
			if ( node.GatherSkill == GatherType.Woodcutting && inventory.IsWeaponPickaxe() )
			{
				_autoGatherNode = null;
				GameLog.Add( "You can't chop trees with a pickaxe.", "#c86464" );
				TriggerSwingAnimation( false );
				SoundLibrary.PlayHitNothing();
				return;
			}

			if ( node.GatherSkill == GatherType.Mining && inventory.IsWeaponHatchet() )
			{
				_autoGatherNode = null;
				GameLog.Add( "You can't mine rocks with a hatchet.", "#c86464" );
				TriggerSwingAnimation( false );
				SoundLibrary.PlayHitNothing();
				return;
			}
		}

		float toolPower = bareHanded ? 1.0f : inventory.GetToolPower();
		SkillType gatherSkill = node.GetSkillType();
		float gatherSkillBonus = skills.GetToolPower( gatherSkill );

		int damage = (int)( toolPower * gatherSkillBonus );
		if ( damage < 1 ) damage = 1;

		_autoGatherNode = node;
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

		if ( node.GatherSkill == GatherType.Woodcutting )
			SoundLibrary.PlayChop( node.WorldPosition );
		else if ( node.GatherSkill == GatherType.Mining )
			SoundLibrary.PlayOreHit( node.WorldPosition );
		else if ( node.GatherSkill == GatherType.Foraging )
			SoundLibrary.PlayForage( node.WorldPosition );

		bool willBreak = _localNodeHealth[node] <= 0;

		node.TakeDamage( damage, GameObject );

		if ( willBreak )
		{
			int harvestAmount = node.GetHarvestAmount();
			var (placed, banked) = inventory.AddItemOrBank( node.ResourceItem, harvestAmount );

			if ( placed > 0 || banked > 0 )
			{
				SoundLibrary.PlayReceiveItem();
				ItemPickupEffect.Trigger( node.ResourceItem );
			}

			inventory.AddNodeMined();

			var def = ItemDatabase.Get( node.ResourceItem );
			string itemName = def != null ? def.Name : node.ResourceItem.ToString();

			if ( placed > 0 )
				GameLog.Add( $"You collected {placed}x {itemName} from {node.GetDisplayName()}.", "#6db8f0" );

			if ( banked > 0 )
				GameLog.Add( $"Inventory full — {banked}x {itemName} sent to your bank.", "#c9a84c" );

			skills.AddXp( node.GetSkillType(), node.XpReward );

			_localNodeHealth.Remove( node );
			_autoGatherNode = null;
		}
	}
}