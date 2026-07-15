using System.Collections.Generic;

public static class ShopPricing
{
	public static int GetSellPrice( ItemId id )
	{
		switch ( id )
		{
			case ItemId.Sticks: return 1;
			case ItemId.Rock: return 1;
			case ItemId.Coal: return 1;
			case ItemId.ArcaneDust: return 2;

			case ItemId.AshwoodLog: return 2;
			case ItemId.ElmheartLog: return 4;
			case ItemId.IronbarkLog: return 6;
			case ItemId.GhostwoodLog: return 8;
			case ItemId.DuskoakLog: return 10;
			case ItemId.WorldrootLog: return 12;

			case ItemId.CoppiteOre: return 2;
			case ItemId.AshsteelOre: return 4;
			case ItemId.ColdveinOre: return 7;
			case ItemId.SolariumOre: return 10;
			case ItemId.LunariteOre: return 14;
			case ItemId.AbyssiumOre: return 18;

			case ItemId.CoppiteBar: return 5;
			case ItemId.AshsteelBar: return 10;
			case ItemId.ColdveinBar: return 18;
			case ItemId.SolariumBar: return 28;
			case ItemId.LunariteBar: return 40;
			case ItemId.AbyssiumBar: return 55;

			case ItemId.Gem: return 15;

			case ItemId.SageLeaf: return 2;
			case ItemId.Thornroot: return 4;
			case ItemId.Spiralvine: return 7;
			case ItemId.Moonbloom: return 11;
			case ItemId.VoidcapMushroom: return 16;
			case ItemId.Starbloom: return 22;

			case ItemId.WildBerries: return 2;
			case ItemId.BlueMoss: return 4;
			case ItemId.Goldpetal: return 7;
			case ItemId.Whisperfern: return 11;
			case ItemId.NightshadeStem: return 16;
			case ItemId.Liferoot: return 22;
			case ItemId.RoughFiber: return 2;
			case ItemId.CaveLichen: return 4;

			case ItemId.PrimitiveHatchet: return 1;
			case ItemId.PrimitivePickaxe: return 1;
			case ItemId.PrimitiveSword: return 2;

			case ItemId.CoppiteHatchet: return 4;
			case ItemId.AshsteelHatchet: return 10;
			case ItemId.ColdveinHatchet: return 18;
			case ItemId.SolariumHatchet: return 28;
			case ItemId.LunariteHatchet: return 40;
			case ItemId.AbyssiumHatchet: return 55;

			case ItemId.CoppitePickaxe: return 4;
			case ItemId.AshsteelPickaxe: return 10;
			case ItemId.ColdveinPickaxe: return 18;
			case ItemId.SolariumPickaxe: return 28;
			case ItemId.LunaritePickaxe: return 40;
			case ItemId.AbyssiumPickaxe: return 55;

			case ItemId.CoppiteSword: return 8;
			case ItemId.AshsteelSword: return 18;
			case ItemId.ColdveinSword: return 32;
			case ItemId.SolariumSword: return 50;
			case ItemId.LunariteSword: return 72;
			case ItemId.AbyssiumSword: return 100;

			case ItemId.AshwoodBow: return 7;
			case ItemId.ElmheartBow: return 16;
			case ItemId.IronbarkBow: return 28;
			case ItemId.GhostwoodBow: return 44;
			case ItemId.DuskoakBow: return 64;
			case ItemId.WorldrootBow: return 88;

			case ItemId.AshwoodStaff: return 7;
			case ItemId.ElmheartStaff: return 16;
			case ItemId.IronbarkStaff: return 28;
			case ItemId.GhostwoodStaff: return 44;
			case ItemId.DuskoakStaff: return 64;
			case ItemId.WorldrootStaff: return 88;
			case ItemId.SlimerootStaff: return 176;

			case ItemId.CoppiteArrow: return 1;
			case ItemId.AshsteelArrow: return 2;
			case ItemId.ColdveinArrow: return 3;
			case ItemId.SolariumArrow: return 5;
			case ItemId.LunariteArrow: return 7;
			case ItemId.AbyssiumArrow: return 10;

			case ItemId.CoppiteShield: return 5;
			case ItemId.AshsteelShield: return 12;
			case ItemId.ColdveinShield: return 22;
			case ItemId.SolariumShield: return 35;
			case ItemId.LunariteShield: return 50;
			case ItemId.AbyssiumShield: return 70;

			case ItemId.CoppiteHeavyHelm: return 4;
			case ItemId.CoppiteHeavyChestplate: return 7;
			case ItemId.CoppiteHeavyLegs: return 5;
			case ItemId.AshsteelHeavyHelm: return 9;
			case ItemId.AshsteelHeavyChestplate: return 16;
			case ItemId.AshsteelHeavyLegs: return 11;
			case ItemId.ColdveinHeavyHelm: return 16;
			case ItemId.ColdveinHeavyChestplate: return 28;
			case ItemId.ColdveinHeavyLegs: return 20;
			case ItemId.SolariumHeavyHelm: return 25;
			case ItemId.SolariumHeavyChestplate: return 44;
			case ItemId.SolariumHeavyLegs: return 32;
			case ItemId.LunariteHeavyHelm: return 36;
			case ItemId.LunariteHeavyChestplate: return 64;
			case ItemId.LunariteHeavyLegs: return 46;
			case ItemId.AbyssiumHeavyHelm: return 50;
			case ItemId.AbyssiumHeavyChestplate: return 88;
			case ItemId.AbyssiumHeavyLegs: return 64;

			case ItemId.CoppiteMediumHelm: return 3;
			case ItemId.CoppiteMediumChestplate: return 6;
			case ItemId.CoppiteMediumLegs: return 4;
			case ItemId.AshsteelMediumHelm: return 7;
			case ItemId.AshsteelMediumChestplate: return 13;
			case ItemId.AshsteelMediumLegs: return 9;
			case ItemId.ColdveinMediumHelm: return 13;
			case ItemId.ColdveinMediumChestplate: return 24;
			case ItemId.ColdveinMediumLegs: return 17;
			case ItemId.SolariumMediumHelm: return 21;
			case ItemId.SolariumMediumChestplate: return 38;
			case ItemId.SolariumMediumLegs: return 27;
			case ItemId.LunariteMediumHelm: return 30;
			case ItemId.LunariteMediumChestplate: return 55;
			case ItemId.LunariteMediumLegs: return 38;
			case ItemId.AbyssiumMediumHelm: return 42;
			case ItemId.AbyssiumMediumChestplate: return 76;
			case ItemId.AbyssiumMediumLegs: return 54;

			case ItemId.CoppiteLightHelm: return 2;
			case ItemId.CoppiteLightChestplate: return 5;
			case ItemId.CoppiteLightLegs: return 3;
			case ItemId.AshsteelLightHelm: return 6;
			case ItemId.AshsteelLightChestplate: return 11;
			case ItemId.AshsteelLightLegs: return 7;
			case ItemId.ColdveinLightHelm: return 11;
			case ItemId.ColdveinLightChestplate: return 20;
			case ItemId.ColdveinLightLegs: return 14;
			case ItemId.SolariumLightHelm: return 17;
			case ItemId.SolariumLightChestplate: return 32;
			case ItemId.SolariumLightLegs: return 22;
			case ItemId.LunariteLightHelm: return 25;
			case ItemId.LunariteLightChestplate: return 46;
			case ItemId.LunariteLightLegs: return 32;
			case ItemId.AbyssiumLightHelm: return 35;
			case ItemId.AbyssiumLightChestplate: return 64;
			case ItemId.AbyssiumLightLegs: return 44;

			case ItemId.Ring: return 25;
			case ItemId.Amulet: return 25;

			case ItemId.Rune: return 8;

			case ItemId.LesserHealingPotion: return 3;
			case ItemId.HealingPotion: return 7;
			case ItemId.GreaterHealingPotion: return 16;
			case ItemId.AttackPotion: return 7;
			case ItemId.DefencePotion: return 7;
			case ItemId.ArcheryPotion: return 11;
			case ItemId.MagicPotion: return 11;
			case ItemId.ElixirOfPower: return 30;

			case ItemId.LesserManaPotion: return 3;
			case ItemId.ManaPotion: return 7;
			case ItemId.GreaterManaPotion: return 16;

			case ItemId.GlassVial: return 1;
			case ItemId.CrystalVial: return 8;
			case ItemId.MonsterBone: return 1;
			case ItemId.MonsterHide: return 2;
			case ItemId.Nugget: return 3;

			case ItemId.GoldCoin: return 0;

			default: return 0;
		}
	}
}