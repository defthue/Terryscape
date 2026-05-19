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

public struct ArmorPenalties
{
	public float ManaRegenPenalty;
	public float RangedDrawPenalty;
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

	public static float GetEffectiveArmorValue( Inventory inventory )
	{
		if ( inventory == null )
			return 0f;

		float total = 0f;
		EquipSlot[] armorSlots = { EquipSlot.Head, EquipSlot.Chest, EquipSlot.Legs, EquipSlot.Shield };

		foreach ( var slot in armorSlots )
		{
			var id = inventory.GetEquipped( slot );
			if ( id == ItemId.None )
				continue;

			var def = ItemDatabase.Get( id );
			if ( def == null )
				continue;

			total += def.ArmorValue;
		}

		float toughnessBonus = inventory.GetEnchantmentBonus( EnchantmentType.Toughness );
		total *= 1f + toughnessBonus / 100f;

		return total;
	}

	public static ArmorPenalties GetEquippedArmorPenalties( Inventory inventory )
	{
		var penalties = new ArmorPenalties();
		if ( inventory == null )
			return penalties;

		EquipSlot[] slots = { EquipSlot.Head, EquipSlot.Chest, EquipSlot.Legs };

		foreach ( var slot in slots )
		{
			var id = inventory.GetEquipped( slot );
			if ( id == ItemId.None )
				continue;

			var def = ItemDatabase.Get( id );
			if ( def == null )
				continue;

			switch ( def.Type )
			{
				case ItemType.HeavyArmor:
					penalties.ManaRegenPenalty += 0.1f;
					penalties.RangedDrawPenalty += 0.1f;
					break;
				case ItemType.MediumArmor:
					penalties.ManaRegenPenalty += 0.07f;
					break;
			}
		}

		return penalties;
	}

	public static float GetArmorReduction( float armorValue )
	{
		return armorValue / ( armorValue + 100f );
	}
}
