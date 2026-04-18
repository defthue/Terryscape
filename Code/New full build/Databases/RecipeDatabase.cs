using System.Collections.Generic;

public enum StationType
{
	Anvil,
	Furnace,
	Workbench,
	AlchemyTable,
	EnchantingAltar
}

public class RecipeIngredient
{
	public ItemId Item;
	public int Amount;

	public RecipeIngredient( ItemId item, int amount )
	{
		Item = item;
		Amount = amount;
	}
}

public class RecipeDefinition
{
	public string Id;
	public string Name;
	public StationType Station;
	public RecipeIngredient[] Ingredients;
	public ItemId OutputItem;
	public int OutputAmount;
	public SkillType SkillRequired;
	public int LevelRequired;
	public SkillType XpSkill;
	public int XpReward;
}

public static class RecipeDatabase
{
	static List<RecipeDefinition> _recipes;

	static RecipeDefinition Define(
		string id,
		string name,
		StationType station,
		RecipeIngredient[] ingredients,
		ItemId outputItem,
		int outputAmount,
		SkillType skillRequired,
		int levelRequired,
		SkillType xpSkill,
		int xpReward
	)
	{
		return new RecipeDefinition
		{
			Id = id,
			Name = name,
			Station = station,
			Ingredients = ingredients,
			OutputItem = outputItem,
			OutputAmount = outputAmount,
			SkillRequired = skillRequired,
			LevelRequired = levelRequired,
			XpSkill = xpSkill,
			XpReward = xpReward
		};
	}

	static void Build()
	{
		_recipes = new List<RecipeDefinition>();

		Add( Define( "primitive_hatchet", "Primitive Hatchet", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.Sticks, 3 ), new RecipeIngredient( ItemId.Rock, 5 ) },
			ItemId.PrimitiveHatchet, 1, SkillType.Smithing, 1, SkillType.Smithing, 5 ) );

		Add( Define( "primitive_pickaxe", "Primitive Pickaxe", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.Sticks, 3 ), new RecipeIngredient( ItemId.Rock, 5 ) },
			ItemId.PrimitivePickaxe, 1, SkillType.Smithing, 1, SkillType.Smithing, 5 ) );

		Add( Define( "primitive_sword", "Primitive Sword", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.Sticks, 2 ), new RecipeIngredient( ItemId.Rock, 8 ) },
			ItemId.PrimitiveSword, 1, SkillType.Smithing, 1, SkillType.Smithing, 8 ) );

		Add( Define( "smelt_coppite", "Smelt Coppite Bar", StationType.Furnace,
			new[] { new RecipeIngredient( ItemId.CoppiteOre, 1 ), new RecipeIngredient( ItemId.Coal, 1 ) },
			ItemId.CoppiteBar, 1, SkillType.Smithing, 1, SkillType.Smithing, 10 ) );

		Add( Define( "smelt_ashsteel", "Smelt Ashsteel Bar", StationType.Furnace,
			new[] { new RecipeIngredient( ItemId.AshsteelOre, 1 ), new RecipeIngredient( ItemId.Coal, 2 ) },
			ItemId.AshsteelBar, 1, SkillType.Smithing, 10, SkillType.Smithing, 25 ) );

		Add( Define( "smelt_coldvein", "Smelt Coldvein Bar", StationType.Furnace,
			new[] { new RecipeIngredient( ItemId.ColdveinOre, 1 ), new RecipeIngredient( ItemId.Coal, 3 ) },
			ItemId.ColdveinBar, 1, SkillType.Smithing, 20, SkillType.Smithing, 50 ) );

		Add( Define( "smelt_solarium", "Smelt Solarium Bar", StationType.Furnace,
			new[] { new RecipeIngredient( ItemId.SolariumOre, 1 ), new RecipeIngredient( ItemId.Coal, 4 ) },
			ItemId.SolariumBar, 1, SkillType.Smithing, 30, SkillType.Smithing, 80 ) );

		Add( Define( "smelt_lunarite", "Smelt Lunarite Bar", StationType.Furnace,
			new[] { new RecipeIngredient( ItemId.LunariteOre, 1 ), new RecipeIngredient( ItemId.Coal, 5 ) },
			ItemId.LunariteBar, 1, SkillType.Smithing, 40, SkillType.Smithing, 120 ) );

		Add( Define( "smelt_abyssium", "Smelt Abyssium Bar", StationType.Furnace,
			new[] { new RecipeIngredient( ItemId.AbyssiumOre, 1 ), new RecipeIngredient( ItemId.Coal, 6 ) },
			ItemId.AbyssiumBar, 1, SkillType.Smithing, 50, SkillType.Smithing, 180 ) );

		Add( Define( "coppite_hatchet", "Coppite Hatchet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.CoppiteBar, 2 ), new RecipeIngredient( ItemId.AshwoodLog, 1 ) },
			ItemId.CoppiteHatchet, 1, SkillType.Smithing, 1, SkillType.Smithing, 15 ) );

		Add( Define( "coppite_pickaxe", "Coppite Pickaxe", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.CoppiteBar, 2 ), new RecipeIngredient( ItemId.AshwoodLog, 1 ) },
			ItemId.CoppitePickaxe, 1, SkillType.Smithing, 1, SkillType.Smithing, 15 ) );

		Add( Define( "ashsteel_hatchet", "Ashsteel Hatchet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AshsteelBar, 2 ), new RecipeIngredient( ItemId.ElmheartLog, 1 ) },
			ItemId.AshsteelHatchet, 1, SkillType.Smithing, 10, SkillType.Smithing, 35 ) );

		Add( Define( "ashsteel_pickaxe", "Ashsteel Pickaxe", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AshsteelBar, 2 ), new RecipeIngredient( ItemId.ElmheartLog, 1 ) },
			ItemId.AshsteelPickaxe, 1, SkillType.Smithing, 10, SkillType.Smithing, 35 ) );

		Add( Define( "coldvein_hatchet", "Coldvein Hatchet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.ColdveinBar, 3 ), new RecipeIngredient( ItemId.IronbarkLog, 1 ) },
			ItemId.ColdveinHatchet, 1, SkillType.Smithing, 20, SkillType.Smithing, 65 ) );

		Add( Define( "coldvein_pickaxe", "Coldvein Pickaxe", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.ColdveinBar, 3 ), new RecipeIngredient( ItemId.IronbarkLog, 1 ) },
			ItemId.ColdveinPickaxe, 1, SkillType.Smithing, 20, SkillType.Smithing, 65 ) );

		Add( Define( "solarium_hatchet", "Solarium Hatchet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.SolariumBar, 3 ), new RecipeIngredient( ItemId.GhostwoodLog, 1 ) },
			ItemId.SolariumHatchet, 1, SkillType.Smithing, 30, SkillType.Smithing, 100 ) );

		Add( Define( "solarium_pickaxe", "Solarium Pickaxe", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.SolariumBar, 3 ), new RecipeIngredient( ItemId.GhostwoodLog, 1 ) },
			ItemId.SolariumPickaxe, 1, SkillType.Smithing, 30, SkillType.Smithing, 100 ) );

		Add( Define( "lunarite_hatchet", "Lunarite Hatchet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.LunariteBar, 4 ), new RecipeIngredient( ItemId.DuskoakLog, 1 ) },
			ItemId.LunariteHatchet, 1, SkillType.Smithing, 40, SkillType.Smithing, 150 ) );

		Add( Define( "lunarite_pickaxe", "Lunarite Pickaxe", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.LunariteBar, 4 ), new RecipeIngredient( ItemId.DuskoakLog, 1 ) },
			ItemId.LunaritePickaxe, 1, SkillType.Smithing, 40, SkillType.Smithing, 150 ) );

		Add( Define( "abyssium_hatchet", "Abyssium Hatchet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AbyssiumBar, 5 ), new RecipeIngredient( ItemId.WorldrootLog, 1 ) },
			ItemId.AbyssiumHatchet, 1, SkillType.Smithing, 50, SkillType.Smithing, 220 ) );

		Add( Define( "abyssium_pickaxe", "Abyssium Pickaxe", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AbyssiumBar, 5 ), new RecipeIngredient( ItemId.WorldrootLog, 1 ) },
			ItemId.AbyssiumPickaxe, 1, SkillType.Smithing, 50, SkillType.Smithing, 220 ) );

		Add( Define( "coppite_sword", "Coppite Sword", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.CoppiteBar, 3 ), new RecipeIngredient( ItemId.AshwoodLog, 1 ) },
			ItemId.CoppiteSword, 1, SkillType.Smithing, 1, SkillType.Smithing, 20 ) );

		Add( Define( "ashsteel_sword", "Ashsteel Sword", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AshsteelBar, 3 ), new RecipeIngredient( ItemId.ElmheartLog, 1 ) },
			ItemId.AshsteelSword, 1, SkillType.Smithing, 10, SkillType.Smithing, 45 ) );

		Add( Define( "coldvein_sword", "Coldvein Sword", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.ColdveinBar, 4 ), new RecipeIngredient( ItemId.IronbarkLog, 1 ) },
			ItemId.ColdveinSword, 1, SkillType.Smithing, 20, SkillType.Smithing, 80 ) );

		Add( Define( "solarium_sword", "Solarium Sword", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.SolariumBar, 4 ), new RecipeIngredient( ItemId.GhostwoodLog, 1 ) },
			ItemId.SolariumSword, 1, SkillType.Smithing, 30, SkillType.Smithing, 120 ) );

		Add( Define( "lunarite_sword", "Lunarite Sword", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.LunariteBar, 5 ), new RecipeIngredient( ItemId.DuskoakLog, 1 ) },
			ItemId.LunariteSword, 1, SkillType.Smithing, 40, SkillType.Smithing, 180 ) );

		Add( Define( "abyssium_sword", "Abyssium Sword", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AbyssiumBar, 6 ), new RecipeIngredient( ItemId.WorldrootLog, 1 ) },
			ItemId.AbyssiumSword, 1, SkillType.Smithing, 50, SkillType.Smithing, 260 ) );

		Add( Define( "coppite_shield", "Coppite Shield", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.CoppiteBar, 2 ), new RecipeIngredient( ItemId.AshwoodLog, 2 ) },
			ItemId.CoppiteShield, 1, SkillType.Smithing, 1, SkillType.Smithing, 18 ) );

		Add( Define( "ashsteel_shield", "Ashsteel Shield", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AshsteelBar, 3 ), new RecipeIngredient( ItemId.ElmheartLog, 2 ) },
			ItemId.AshsteelShield, 1, SkillType.Smithing, 10, SkillType.Smithing, 40 ) );

		Add( Define( "coldvein_shield", "Coldvein Shield", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.ColdveinBar, 3 ), new RecipeIngredient( ItemId.IronbarkLog, 2 ) },
			ItemId.ColdveinShield, 1, SkillType.Smithing, 20, SkillType.Smithing, 70 ) );

		Add( Define( "solarium_shield", "Solarium Shield", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.SolariumBar, 4 ), new RecipeIngredient( ItemId.GhostwoodLog, 2 ) },
			ItemId.SolariumShield, 1, SkillType.Smithing, 30, SkillType.Smithing, 110 ) );

		Add( Define( "lunarite_shield", "Lunarite Shield", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.LunariteBar, 5 ), new RecipeIngredient( ItemId.DuskoakLog, 2 ) },
			ItemId.LunariteShield, 1, SkillType.Smithing, 40, SkillType.Smithing, 160 ) );

		Add( Define( "abyssium_shield", "Abyssium Shield", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AbyssiumBar, 6 ), new RecipeIngredient( ItemId.WorldrootLog, 2 ) },
			ItemId.AbyssiumShield, 1, SkillType.Smithing, 50, SkillType.Smithing, 240 ) );

		AddArmorSet( "coppite_heavy", "Coppite Heavy", StationType.Anvil, ItemId.CoppiteBar, 1, 1, SkillType.Smithing,
			ItemId.CoppiteHeavyHelm, ItemId.CoppiteHeavyChestplate, ItemId.CoppiteHeavyLegs,
			helmBars: 2, chestBars: 4, legsBars: 3, xpHelm: 15, xpChest: 30, xpLegs: 22 );

		AddArmorSet( "ashsteel_heavy", "Ashsteel Heavy", StationType.Anvil, ItemId.AshsteelBar, 2, 10, SkillType.Smithing,
			ItemId.AshsteelHeavyHelm, ItemId.AshsteelHeavyChestplate, ItemId.AshsteelHeavyLegs,
			helmBars: 2, chestBars: 5, legsBars: 3, xpHelm: 35, xpChest: 65, xpLegs: 48 );

		AddArmorSet( "coldvein_heavy", "Coldvein Heavy", StationType.Anvil, ItemId.ColdveinBar, 3, 20, SkillType.Smithing,
			ItemId.ColdveinHeavyHelm, ItemId.ColdveinHeavyChestplate, ItemId.ColdveinHeavyLegs,
			helmBars: 3, chestBars: 5, legsBars: 4, xpHelm: 60, xpChest: 100, xpLegs: 75 );

		AddArmorSet( "solarium_heavy", "Solarium Heavy", StationType.Anvil, ItemId.SolariumBar, 4, 30, SkillType.Smithing,
			ItemId.SolariumHeavyHelm, ItemId.SolariumHeavyChestplate, ItemId.SolariumHeavyLegs,
			helmBars: 3, chestBars: 6, legsBars: 4, xpHelm: 90, xpChest: 150, xpLegs: 110 );

		AddArmorSet( "lunarite_heavy", "Lunarite Heavy", StationType.Anvil, ItemId.LunariteBar, 5, 40, SkillType.Smithing,
			ItemId.LunariteHeavyHelm, ItemId.LunariteHeavyChestplate, ItemId.LunariteHeavyLegs,
			helmBars: 4, chestBars: 7, legsBars: 5, xpHelm: 130, xpChest: 220, xpLegs: 160 );

		AddArmorSet( "abyssium_heavy", "Abyssium Heavy", StationType.Anvil, ItemId.AbyssiumBar, 6, 50, SkillType.Smithing,
			ItemId.AbyssiumHeavyHelm, ItemId.AbyssiumHeavyChestplate, ItemId.AbyssiumHeavyLegs,
			helmBars: 5, chestBars: 8, legsBars: 6, xpHelm: 190, xpChest: 320, xpLegs: 240 );

		AddArmorSet( "coppite_medium", "Coppite Medium", StationType.Workbench, ItemId.CoppiteBar, 1, 1, SkillType.Crafting,
			ItemId.CoppiteMediumHelm, ItemId.CoppiteMediumChestplate, ItemId.CoppiteMediumLegs,
			helmBars: 1, chestBars: 2, legsBars: 2, xpHelm: 15, xpChest: 30, xpLegs: 22 );

		AddArmorSet( "ashsteel_medium", "Ashsteel Medium", StationType.Workbench, ItemId.AshsteelBar, 2, 10, SkillType.Crafting,
			ItemId.AshsteelMediumHelm, ItemId.AshsteelMediumChestplate, ItemId.AshsteelMediumLegs,
			helmBars: 1, chestBars: 3, legsBars: 2, xpHelm: 35, xpChest: 65, xpLegs: 48 );

		AddArmorSet( "coldvein_medium", "Coldvein Medium", StationType.Workbench, ItemId.ColdveinBar, 3, 20, SkillType.Crafting,
			ItemId.ColdveinMediumHelm, ItemId.ColdveinMediumChestplate, ItemId.ColdveinMediumLegs,
			helmBars: 2, chestBars: 3, legsBars: 2, xpHelm: 60, xpChest: 100, xpLegs: 75 );

		AddArmorSet( "solarium_medium", "Solarium Medium", StationType.Workbench, ItemId.SolariumBar, 4, 30, SkillType.Crafting,
			ItemId.SolariumMediumHelm, ItemId.SolariumMediumChestplate, ItemId.SolariumMediumLegs,
			helmBars: 2, chestBars: 4, legsBars: 3, xpHelm: 90, xpChest: 150, xpLegs: 110 );

		AddArmorSet( "lunarite_medium", "Lunarite Medium", StationType.Workbench, ItemId.LunariteBar, 5, 40, SkillType.Crafting,
			ItemId.LunariteMediumHelm, ItemId.LunariteMediumChestplate, ItemId.LunariteMediumLegs,
			helmBars: 3, chestBars: 5, legsBars: 3, xpHelm: 130, xpChest: 220, xpLegs: 160 );

		AddArmorSet( "abyssium_medium", "Abyssium Medium", StationType.Workbench, ItemId.AbyssiumBar, 6, 50, SkillType.Crafting,
			ItemId.AbyssiumMediumHelm, ItemId.AbyssiumMediumChestplate, ItemId.AbyssiumMediumLegs,
			helmBars: 3, chestBars: 5, legsBars: 4, xpHelm: 190, xpChest: 320, xpLegs: 240 );

		AddArmorSet( "coppite_light", "Coppite Light", StationType.Workbench, ItemId.CoppiteBar, 1, 1, SkillType.Crafting,
			ItemId.CoppiteLightHelm, ItemId.CoppiteLightChestplate, ItemId.CoppiteLightLegs,
			helmBars: 1, chestBars: 2, legsBars: 1, xpHelm: 15, xpChest: 30, xpLegs: 22 );

		AddArmorSet( "ashsteel_light", "Ashsteel Light", StationType.Workbench, ItemId.AshsteelBar, 2, 10, SkillType.Crafting,
			ItemId.AshsteelLightHelm, ItemId.AshsteelLightChestplate, ItemId.AshsteelLightLegs,
			helmBars: 1, chestBars: 2, legsBars: 2, xpHelm: 35, xpChest: 65, xpLegs: 48 );

		AddArmorSet( "coldvein_light", "Coldvein Light", StationType.Workbench, ItemId.ColdveinBar, 3, 20, SkillType.Crafting,
			ItemId.ColdveinLightHelm, ItemId.ColdveinLightChestplate, ItemId.ColdveinLightLegs,
			helmBars: 2, chestBars: 3, legsBars: 2, xpHelm: 60, xpChest: 100, xpLegs: 75 );

		AddArmorSet( "solarium_light", "Solarium Light", StationType.Workbench, ItemId.SolariumBar, 4, 30, SkillType.Crafting,
			ItemId.SolariumLightHelm, ItemId.SolariumLightChestplate, ItemId.SolariumLightLegs,
			helmBars: 2, chestBars: 4, legsBars: 3, xpHelm: 90, xpChest: 150, xpLegs: 110 );

		AddArmorSet( "lunarite_light", "Lunarite Light", StationType.Workbench, ItemId.LunariteBar, 5, 40, SkillType.Crafting,
			ItemId.LunariteLightHelm, ItemId.LunariteLightChestplate, ItemId.LunariteLightLegs,
			helmBars: 3, chestBars: 4, legsBars: 3, xpHelm: 130, xpChest: 220, xpLegs: 160 );

		AddArmorSet( "abyssium_light", "Abyssium Light", StationType.Workbench, ItemId.AbyssiumBar, 6, 50, SkillType.Crafting,
			ItemId.AbyssiumLightHelm, ItemId.AbyssiumLightChestplate, ItemId.AbyssiumLightLegs,
			helmBars: 3, chestBars: 5, legsBars: 4, xpHelm: 190, xpChest: 320, xpLegs: 240 );

		Add( Define( "rough_ring", "Rough Ring", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.RoughGem, 1 ), new RecipeIngredient( ItemId.CoppiteBar, 1 ) },
			ItemId.RoughRing, 1, SkillType.Smithing, 1, SkillType.Smithing, 20 ) );

		Add( Define( "rough_amulet", "Rough Amulet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.RoughGem, 1 ), new RecipeIngredient( ItemId.CoppiteBar, 2 ) },
			ItemId.RoughAmulet, 1, SkillType.Smithing, 1, SkillType.Smithing, 25 ) );

		Add( Define( "fine_ring", "Fine Ring", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.FineGem, 1 ), new RecipeIngredient( ItemId.ColdveinBar, 1 ) },
			ItemId.FineRing, 1, SkillType.Smithing, 25, SkillType.Smithing, 70 ) );

		Add( Define( "fine_amulet", "Fine Amulet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.FineGem, 1 ), new RecipeIngredient( ItemId.ColdveinBar, 2 ) },
			ItemId.FineAmulet, 1, SkillType.Smithing, 25, SkillType.Smithing, 85 ) );

		Add( Define( "pristine_ring", "Pristine Ring", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.PristineGem, 1 ), new RecipeIngredient( ItemId.LunariteBar, 1 ) },
			ItemId.PristineRing, 1, SkillType.Smithing, 45, SkillType.Smithing, 180 ) );

		Add( Define( "pristine_amulet", "Pristine Amulet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.PristineGem, 1 ), new RecipeIngredient( ItemId.LunariteBar, 2 ) },
			ItemId.PristineAmulet, 1, SkillType.Smithing, 45, SkillType.Smithing, 220 ) );

		Add( Define( "ashwood_bow", "Ashwood Bow", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.AshwoodLog, 2 ), new RecipeIngredient( ItemId.RoughFiber, 3 ) },
			ItemId.AshwoodBow, 1, SkillType.Crafting, 1, SkillType.Crafting, 15 ) );

		Add( Define( "elmheart_bow", "Elmheart Bow", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.ElmheartLog, 2 ), new RecipeIngredient( ItemId.RoughFiber, 4 ) },
			ItemId.ElmheartBow, 1, SkillType.Crafting, 10, SkillType.Crafting, 35 ) );

		Add( Define( "ironbark_bow", "Ironbark Bow", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.IronbarkLog, 3 ), new RecipeIngredient( ItemId.RoughFiber, 5 ) },
			ItemId.IronbarkBow, 1, SkillType.Crafting, 20, SkillType.Crafting, 65 ) );

		Add( Define( "ghostwood_bow", "Ghostwood Bow", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.GhostwoodLog, 3 ), new RecipeIngredient( ItemId.RoughFiber, 6 ) },
			ItemId.GhostwoodBow, 1, SkillType.Crafting, 30, SkillType.Crafting, 100 ) );

		Add( Define( "duskoak_bow", "Duskoak Bow", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.DuskoakLog, 4 ), new RecipeIngredient( ItemId.RoughFiber, 7 ) },
			ItemId.DuskoakBow, 1, SkillType.Crafting, 40, SkillType.Crafting, 150 ) );

		Add( Define( "worldroot_bow", "Worldroot Bow", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.WorldrootLog, 5 ), new RecipeIngredient( ItemId.RoughFiber, 8 ) },
			ItemId.WorldrootBow, 1, SkillType.Crafting, 50, SkillType.Crafting, 220 ) );

		Add( Define( "coppite_arrows", "Coppite Arrows", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.CoppiteBar, 1 ), new RecipeIngredient( ItemId.AshwoodLog, 1 ) },
			ItemId.CoppiteArrow, 15, SkillType.Crafting, 1, SkillType.Crafting, 8 ) );

		Add( Define( "ashsteel_arrows", "Ashsteel Arrows", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.AshsteelBar, 1 ), new RecipeIngredient( ItemId.ElmheartLog, 1 ) },
			ItemId.AshsteelArrow, 15, SkillType.Crafting, 10, SkillType.Crafting, 20 ) );

		Add( Define( "coldvein_arrows", "Coldvein Arrows", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.ColdveinBar, 1 ), new RecipeIngredient( ItemId.IronbarkLog, 1 ) },
			ItemId.ColdveinArrow, 15, SkillType.Crafting, 20, SkillType.Crafting, 40 ) );

		Add( Define( "solarium_arrows", "Solarium Arrows", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.SolariumBar, 1 ), new RecipeIngredient( ItemId.GhostwoodLog, 1 ) },
			ItemId.SolariumArrow, 15, SkillType.Crafting, 30, SkillType.Crafting, 65 ) );

		Add( Define( "lunarite_arrows", "Lunarite Arrows", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.LunariteBar, 1 ), new RecipeIngredient( ItemId.DuskoakLog, 1 ) },
			ItemId.LunariteArrow, 15, SkillType.Crafting, 40, SkillType.Crafting, 100 ) );

		Add( Define( "abyssium_arrows", "Abyssium Arrows", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.AbyssiumBar, 1 ), new RecipeIngredient( ItemId.WorldrootLog, 1 ) },
			ItemId.AbyssiumArrow, 15, SkillType.Crafting, 50, SkillType.Crafting, 150 ) );

		Add( Define( "ashwood_staff", "Ashwood Staff", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.AshwoodLog, 3 ), new RecipeIngredient( ItemId.SageLeaf, 2 ) },
			ItemId.AshwoodStaff, 1, SkillType.Crafting, 1, SkillType.Crafting, 15 ) );

		Add( Define( "elmheart_staff", "Elmheart Staff", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.ElmheartLog, 3 ), new RecipeIngredient( ItemId.Thornroot, 2 ) },
			ItemId.ElmheartStaff, 1, SkillType.Crafting, 10, SkillType.Crafting, 35 ) );

		Add( Define( "ironbark_staff", "Ironbark Staff", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.IronbarkLog, 4 ), new RecipeIngredient( ItemId.Spiralvine, 2 ) },
			ItemId.IronbarkStaff, 1, SkillType.Crafting, 20, SkillType.Crafting, 65 ) );

		Add( Define( "ghostwood_staff", "Ghostwood Staff", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.GhostwoodLog, 4 ), new RecipeIngredient( ItemId.Moonbloom, 3 ) },
			ItemId.GhostwoodStaff, 1, SkillType.Crafting, 30, SkillType.Crafting, 100 ) );

		Add( Define( "duskoak_staff", "Duskoak Staff", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.DuskoakLog, 5 ), new RecipeIngredient( ItemId.VoidcapMushroom, 3 ) },
			ItemId.DuskoakStaff, 1, SkillType.Crafting, 40, SkillType.Crafting, 150 ) );

		Add( Define( "worldroot_staff", "Worldroot Staff", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.WorldrootLog, 6 ), new RecipeIngredient( ItemId.Starbloom, 3 ) },
			ItemId.WorldrootStaff, 1, SkillType.Crafting, 50, SkillType.Crafting, 220 ) );

		Add( Define( "lesser_healing_potion", "Lesser Healing Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.SageLeaf, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.LesserHealingPotion, 1, SkillType.Crafting, 1, SkillType.Crafting, 10 ) );

		Add( Define( "healing_potion", "Healing Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Thornroot, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.HealingPotion, 1, SkillType.Crafting, 15, SkillType.Crafting, 30 ) );

		Add( Define( "attack_potion", "Attack Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Spiralvine, 1 ), new RecipeIngredient( ItemId.BlueMoss, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.AttackPotion, 1, SkillType.Crafting, 20, SkillType.Crafting, 50 ) );

		Add( Define( "defence_potion", "Defence Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Goldpetal, 1 ), new RecipeIngredient( ItemId.RoughFiber, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.DefencePotion, 1, SkillType.Crafting, 20, SkillType.Crafting, 50 ) );

		Add( Define( "archery_potion", "Archery Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Moonbloom, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.ArcheryPotion, 1, SkillType.Crafting, 25, SkillType.Crafting, 65 ) );

		Add( Define( "magic_potion", "Magic Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Whisperfern, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.MagicPotion, 1, SkillType.Crafting, 25, SkillType.Crafting, 65 ) );

		Add( Define( "greater_healing_potion", "Greater Healing Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.VoidcapMushroom, 1 ), new RecipeIngredient( ItemId.Goldpetal, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.GreaterHealingPotion, 1, SkillType.Crafting, 35, SkillType.Crafting, 100 ) );

		Add( Define( "elixir_of_power", "Elixir of Power", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Starbloom, 1 ), new RecipeIngredient( ItemId.Liferoot, 1 ), new RecipeIngredient( ItemId.CrystalVial, 1 ) },
			ItemId.ElixirOfPower, 1, SkillType.Crafting, 45, SkillType.Crafting, 200 ) );

		Add( Define( "lesser_mana_potion", "Lesser Mana Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.BlueMoss, 1 ), new RecipeIngredient( ItemId.ArcaneDust, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.LesserManaPotion, 1, SkillType.Crafting, 5, SkillType.Crafting, 15 ) );

		Add( Define( "mana_potion", "Mana Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Whisperfern, 1 ), new RecipeIngredient( ItemId.ArcaneDust, 2 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.ManaPotion, 1, SkillType.Crafting, 20, SkillType.Crafting, 50 ) );

		Add( Define( "greater_mana_potion", "Greater Mana Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Moonbloom, 1 ), new RecipeIngredient( ItemId.ArcaneDust, 3 ), new RecipeIngredient( ItemId.CrystalVial, 1 ) },
			ItemId.GreaterManaPotion, 1, SkillType.Crafting, 35, SkillType.Crafting, 100 ) );
	}

	static void AddArmorSet(
		string idPrefix, string namePrefix, StationType station, ItemId barItem, int tier, int level, SkillType skill,
		ItemId helm, ItemId chest, ItemId legs,
		int helmBars, int chestBars, int legsBars,
		int xpHelm, int xpChest, int xpLegs
	)
	{
		Add( Define( $"{idPrefix}_helm", $"{namePrefix} Helm", station,
			new[] { new RecipeIngredient( barItem, helmBars ) },
			helm, 1, skill, level, skill, xpHelm ) );

		Add( Define( $"{idPrefix}_chestplate", $"{namePrefix} Chestplate", station,
			new[] { new RecipeIngredient( barItem, chestBars ) },
			chest, 1, skill, level, skill, xpChest ) );

		Add( Define( $"{idPrefix}_legs", $"{namePrefix} Legs", station,
			new[] { new RecipeIngredient( barItem, legsBars ) },
			legs, 1, skill, level, skill, xpLegs ) );
	}

	static void Add( RecipeDefinition recipe )
	{
		_recipes.Add( recipe );
	}

	public static RecipeDefinition GetById( string id )
	{
		if ( _recipes == null )
			Build();

		foreach ( var recipe in _recipes )
		{
			if ( recipe.Id == id )
				return recipe;
		}

		return null;
	}

	public static IEnumerable<RecipeDefinition> GetAll()
	{
		if ( _recipes == null )
			Build();

		return _recipes;
	}

	public static IEnumerable<RecipeDefinition> GetByStation( StationType station )
	{
		if ( _recipes == null )
			Build();

		foreach ( var recipe in _recipes )
		{
			if ( recipe.Station == station )
				yield return recipe;
		}
	}
}