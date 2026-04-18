public enum CombatStyle
{
	None,
	Melee,
	Ranged,
	Magic
}

public enum ArmorClass
{
	None,
	Heavy,
	Medium,
	Light
}

public static class CombatTriangle
{
	public static float GetDealMultiplier( CombatStyle attacker, CombatStyle defender )
	{
		if ( attacker == CombatStyle.None || defender == CombatStyle.None )
			return 1f;

		if ( attacker == defender )
			return 1f;

		if ( attacker == CombatStyle.Melee && defender == CombatStyle.Magic )
			return 1.2f;

		if ( attacker == CombatStyle.Ranged && defender == CombatStyle.Melee )
			return 1.2f;

		if ( attacker == CombatStyle.Magic && defender == CombatStyle.Ranged )
			return 1.2f;

		return 0.8f;
	}

	public static float GetTakeMultiplier( CombatStyle attacker, CombatStyle defender )
	{
		return GetDealMultiplier( attacker, defender );
	}

	public static CombatStyle GetStyleFromWeapon( ItemDefinition weaponDef )
	{
		if ( weaponDef == null )
			return CombatStyle.Melee;

		switch ( weaponDef.Type )
		{
			case ItemType.MeleeWeapon:
			case ItemType.Tool:
				return CombatStyle.Melee;
			case ItemType.RangedWeapon:
				return CombatStyle.Ranged;
			case ItemType.MagicWeapon:
				return CombatStyle.Magic;
			default:
				return CombatStyle.Melee;
		}
	}

	public static ArmorClass GetArmorClass( ItemDefinition armorDef )
	{
		if ( armorDef == null )
			return ArmorClass.None;

		switch ( armorDef.Type )
		{
			case ItemType.HeavyArmor:
				return ArmorClass.Heavy;
			case ItemType.MediumArmor:
				return ArmorClass.Medium;
			case ItemType.LightArmor:
				return ArmorClass.Light;
			default:
				return ArmorClass.None;
		}
	}

	public static CombatStyle GetMatchingStyle( ArmorClass armorClass )
	{
		switch ( armorClass )
		{
			case ArmorClass.Heavy: return CombatStyle.Melee;
			case ArmorClass.Medium: return CombatStyle.Ranged;
			case ArmorClass.Light: return CombatStyle.Magic;
			default: return CombatStyle.None;
		}
	}

	public static bool IsArmorEffective( CombatStyle playerStyle, ItemDefinition armorDef )
	{
		if ( armorDef == null )
			return false;

		var armorClass = GetArmorClass( armorDef );
		if ( armorClass == ArmorClass.None )
			return true;

		var matchingStyle = GetMatchingStyle( armorClass );
		return matchingStyle == playerStyle;
	}

	public static float GetEffectiveArmorValue( CombatStyle playerStyle, Inventory inventory )
	{
		float total = 0f;
		EquipSlot[] armorSlots = { EquipSlot.Head, EquipSlot.Chest, EquipSlot.Legs };

		foreach ( var slot in armorSlots )
		{
			var id = inventory.GetEquipped( slot );
			if ( id == ItemId.None )
				continue;

			var def = ItemDatabase.Get( id );
			if ( def == null )
				continue;

			if ( IsArmorEffective( playerStyle, def ) )
				total += def.ArmorValue;
		}

		var shieldId = inventory.GetEquipped( EquipSlot.Shield );
		if ( shieldId != ItemId.None )
		{
			var shieldDef = ItemDatabase.Get( shieldId );
			if ( shieldDef != null )
				total += shieldDef.ArmorValue;
		}

		return total;
	}

	public static float GetArmorReduction( float armorValue )
	{
		return armorValue / ( armorValue + 100f );
	}
}