using Sandbox;

public static class PvpCombat
{
	public static bool CanDamage( GameObject attacker, GameObject target )
	{
		if ( attacker == null || target == null || attacker == target )
			return false;

		var dm = DuelManager.Instance;
		if ( dm == null || !dm.MatchActive || !dm.RoundLive )
			return false;

		if ( !dm.IsDuelist( attacker ) || !dm.IsDuelist( target ) )
			return false;

		var targetHealth = target.Components.Get<PlayerHealth>();
		if ( targetHealth == null || targetHealth.IsDead )
			return false;

		return true;
	}

	public static int ResolveDamage( float rawOffence, CombatStyle attackerStyle, GameObject target )
	{
		var targetInventory = target.Components.Get<Inventory>();
		var targetSkills = target.Components.Get<Skills>();

		var targetWeaponDef = targetInventory?.GetEquippedWeaponDef();
		CombatStyle targetStyle = CombatTriangle.GetStyleFromWeapon( targetWeaponDef );
		float triangleMult = CombatTriangle.GetDealMultiplier( attackerStyle, targetStyle );

		float armorValue = targetInventory != null ? CombatTriangle.GetEffectiveArmorValue( targetInventory ) : 0f;
		float armorReduction = CombatTriangle.GetArmorReduction( armorValue );

		float defenceMult = targetSkills != null ? targetSkills.GetDefenceMultiplier() : 1f;

		float defenceBuffMult = 1f;
		var potionSystem = target.Components.Get<PotionSystem>();
		if ( potionSystem != null )
			defenceBuffMult = potionSystem.GetBuffMultiplier( BuffType.Defence );

		int finalDamage = (int)( rawOffence * triangleMult * ( 1f - armorReduction ) / defenceMult / defenceBuffMult );
		if ( finalDamage < 1 ) finalDamage = 1;

		return finalDamage;
	}

	public static GameObject ResolveTarget( GameObject hit, GameObject attacker )
	{
		if ( hit == null )
			return null;

		var root = hit;
		while ( root.Parent != null && root.Parent != Game.ActiveScene )
			root = root.Parent;

		var pc = root.Components.Get<PlayerController>();
		if ( pc == null )
			return null;

		var targetObj = pc.GameObject;
		return CanDamage( attacker, targetObj ) ? targetObj : null;
	}
}
