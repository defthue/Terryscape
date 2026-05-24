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
	const float CraftingXpMultiplier = 2f;

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
		int finalXp = xpSkill == SkillType.Crafting ? (int)(xpReward * CraftingXpMultiplier) : xpReward;

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
			XpReward = finalXp
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
			new[] { new RecipeIngredient( ItemId.CoppiteBar, 15 ), new RecipeIngredient( ItemId.AshwoodLog, 6 ) },
			ItemId.CoppiteHatchet, 1, SkillType.Smithing, 1, SkillType.Smithing, 15 ) );

		Add( Define( "coppite_pickaxe", "Coppite Pickaxe", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.CoppiteBar, 15 ), new RecipeIngredient( ItemId.AshwoodLog, 6 ) },
			ItemId.CoppitePickaxe, 1, SkillType.Smithing, 1, SkillType.Smithing, 15 ) );

		Add( Define( "ashsteel_hatchet", "Ashsteel Hatchet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AshsteelBar, 15 ), new RecipeIngredient( ItemId.ElmheartLog, 6 ) },
			ItemId.AshsteelHatchet, 1, SkillType.Smithing, 10, SkillType.Smithing, 35 ) );

		Add( Define( "ashsteel_pickaxe", "Ashsteel Pickaxe", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AshsteelBar, 15 ), new RecipeIngredient( ItemId.ElmheartLog, 6 ) },
			ItemId.AshsteelPickaxe, 1, SkillType.Smithing, 10, SkillType.Smithing, 35 ) );

		Add( Define( "coldvein_hatchet", "Coldvein Hatchet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.ColdveinBar, 15 ), new RecipeIngredient( ItemId.IronbarkLog, 6 ) },
			ItemId.ColdveinHatchet, 1, SkillType.Smithing, 20, SkillType.Smithing, 65 ) );

		Add( Define( "coldvein_pickaxe", "Coldvein Pickaxe", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.ColdveinBar, 15 ), new RecipeIngredient( ItemId.IronbarkLog, 6 ) },
			ItemId.ColdveinPickaxe, 1, SkillType.Smithing, 20, SkillType.Smithing, 65 ) );

		Add( Define( "solarium_hatchet", "Solarium Hatchet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.SolariumBar, 15 ), new RecipeIngredient( ItemId.GhostwoodLog, 6 ) },
			ItemId.SolariumHatchet, 1, SkillType.Smithing, 30, SkillType.Smithing, 100 ) );

		Add( Define( "solarium_pickaxe", "Solarium Pickaxe", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.SolariumBar, 15 ), new RecipeIngredient( ItemId.GhostwoodLog, 6 ) },
			ItemId.SolariumPickaxe, 1, SkillType.Smithing, 30, SkillType.Smithing, 100 ) );

		Add( Define( "lunarite_hatchet", "Lunarite Hatchet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.LunariteBar, 15 ), new RecipeIngredient( ItemId.DuskoakLog, 6 ) },
			ItemId.LunariteHatchet, 1, SkillType.Smithing, 40, SkillType.Smithing, 150 ) );

		Add( Define( "lunarite_pickaxe", "Lunarite Pickaxe", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.LunariteBar, 15 ), new RecipeIngredient( ItemId.DuskoakLog, 6 ) },
			ItemId.LunaritePickaxe, 1, SkillType.Smithing, 40, SkillType.Smithing, 150 ) );

		Add( Define( "abyssium_hatchet", "Abyssium Hatchet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AbyssiumBar, 15 ), new RecipeIngredient( ItemId.WorldrootLog, 6 ) },
			ItemId.AbyssiumHatchet, 1, SkillType.Smithing, 50, SkillType.Smithing, 220 ) );

		Add( Define( "abyssium_pickaxe", "Abyssium Pickaxe", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AbyssiumBar, 15 ), new RecipeIngredient( ItemId.WorldrootLog, 6 ) },
			ItemId.AbyssiumPickaxe, 1, SkillType.Smithing, 50, SkillType.Smithing, 220 ) );

		Add( Define( "coppite_sword", "Coppite Sword", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.CoppiteBar, 24 ), new RecipeIngredient( ItemId.AshwoodLog, 6 ) },
			ItemId.CoppiteSword, 1, SkillType.Smithing, 1, SkillType.Smithing, 20 ) );

		Add( Define( "ashsteel_sword", "Ashsteel Sword", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AshsteelBar, 24 ), new RecipeIngredient( ItemId.ElmheartLog, 6 ) },
			ItemId.AshsteelSword, 1, SkillType.Smithing, 10, SkillType.Smithing, 45 ) );

		Add( Define( "coldvein_sword", "Coldvein Sword", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.ColdveinBar, 24 ), new RecipeIngredient( ItemId.IronbarkLog, 6 ) },
			ItemId.ColdveinSword, 1, SkillType.Smithing, 20, SkillType.Smithing, 80 ) );

		Add( Define( "solarium_sword", "Solarium Sword", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.SolariumBar, 24 ), new RecipeIngredient( ItemId.GhostwoodLog, 6 ) },
			ItemId.SolariumSword, 1, SkillType.Smithing, 30, SkillType.Smithing, 120 ) );

		Add( Define( "lunarite_sword", "Lunarite Sword", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.LunariteBar, 24 ), new RecipeIngredient( ItemId.DuskoakLog, 6 ) },
			ItemId.LunariteSword, 1, SkillType.Smithing, 40, SkillType.Smithing, 180 ) );

		Add( Define( "abyssium_sword", "Abyssium Sword", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AbyssiumBar, 24 ), new RecipeIngredient( ItemId.WorldrootLog, 6 ) },
			ItemId.AbyssiumSword, 1, SkillType.Smithing, 50, SkillType.Smithing, 260 ) );

		Add( Define( "coppite_shield", "Coppite Shield", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.CoppiteBar, 18 ), new RecipeIngredient( ItemId.AshwoodLog, 12 ) },
			ItemId.CoppiteShield, 1, SkillType.Smithing, 1, SkillType.Smithing, 18 ) );

		Add( Define( "ashsteel_shield", "Ashsteel Shield", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AshsteelBar, 18 ), new RecipeIngredient( ItemId.ElmheartLog, 12 ) },
			ItemId.AshsteelShield, 1, SkillType.Smithing, 10, SkillType.Smithing, 40 ) );

		Add( Define( "coldvein_shield", "Coldvein Shield", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.ColdveinBar, 18 ), new RecipeIngredient( ItemId.IronbarkLog, 12 ) },
			ItemId.ColdveinShield, 1, SkillType.Smithing, 20, SkillType.Smithing, 70 ) );

		Add( Define( "solarium_shield", "Solarium Shield", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.SolariumBar, 18 ), new RecipeIngredient( ItemId.GhostwoodLog, 12 ) },
			ItemId.SolariumShield, 1, SkillType.Smithing, 30, SkillType.Smithing, 110 ) );

		Add( Define( "lunarite_shield", "Lunarite Shield", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.LunariteBar, 18 ), new RecipeIngredient( ItemId.DuskoakLog, 12 ) },
			ItemId.LunariteShield, 1, SkillType.Smithing, 40, SkillType.Smithing, 160 ) );

		Add( Define( "abyssium_shield", "Abyssium Shield", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.AbyssiumBar, 18 ), new RecipeIngredient( ItemId.WorldrootLog, 12 ) },
			ItemId.AbyssiumShield, 1, SkillType.Smithing, 50, SkillType.Smithing, 240 ) );

		AddHeavyArmorSet( "coppite_heavy", "Coppite Heavy", ItemId.CoppiteBar, 1,
			ItemId.CoppiteHeavyHelm, ItemId.CoppiteHeavyChestplate, ItemId.CoppiteHeavyLegs,
			15, 30, 22 );

		AddHeavyArmorSet( "ashsteel_heavy", "Ashsteel Heavy", ItemId.AshsteelBar, 10,
			ItemId.AshsteelHeavyHelm, ItemId.AshsteelHeavyChestplate, ItemId.AshsteelHeavyLegs,
			35, 65, 48 );

		AddHeavyArmorSet( "coldvein_heavy", "Coldvein Heavy", ItemId.ColdveinBar, 20,
			ItemId.ColdveinHeavyHelm, ItemId.ColdveinHeavyChestplate, ItemId.ColdveinHeavyLegs,
			60, 100, 75 );

		AddHeavyArmorSet( "solarium_heavy", "Solarium Heavy", ItemId.SolariumBar, 30,
			ItemId.SolariumHeavyHelm, ItemId.SolariumHeavyChestplate, ItemId.SolariumHeavyLegs,
			90, 150, 110 );

		AddHeavyArmorSet( "lunarite_heavy", "Lunarite Heavy", ItemId.LunariteBar, 40,
			ItemId.LunariteHeavyHelm, ItemId.LunariteHeavyChestplate, ItemId.LunariteHeavyLegs,
			130, 220, 160 );

		AddHeavyArmorSet( "abyssium_heavy", "Abyssium Heavy", ItemId.AbyssiumBar, 50,
			ItemId.AbyssiumHeavyHelm, ItemId.AbyssiumHeavyChestplate, ItemId.AbyssiumHeavyLegs,
			190, 320, 240 );

		AddMediumArmorSet( "coppite_medium", "Coppite Medium", ItemId.CoppiteBar, 1,
			ItemId.CoppiteMediumHelm, ItemId.CoppiteMediumChestplate, ItemId.CoppiteMediumLegs,
			15, 30, 22 );

		AddMediumArmorSet( "ashsteel_medium", "Ashsteel Medium", ItemId.AshsteelBar, 10,
			ItemId.AshsteelMediumHelm, ItemId.AshsteelMediumChestplate, ItemId.AshsteelMediumLegs,
			35, 65, 48 );

		AddMediumArmorSet( "coldvein_medium", "Coldvein Medium", ItemId.ColdveinBar, 20,
			ItemId.ColdveinMediumHelm, ItemId.ColdveinMediumChestplate, ItemId.ColdveinMediumLegs,
			60, 100, 75 );

		AddMediumArmorSet( "solarium_medium", "Solarium Medium", ItemId.SolariumBar, 30,
			ItemId.SolariumMediumHelm, ItemId.SolariumMediumChestplate, ItemId.SolariumMediumLegs,
			90, 150, 110 );

		AddMediumArmorSet( "lunarite_medium", "Lunarite Medium", ItemId.LunariteBar, 40,
			ItemId.LunariteMediumHelm, ItemId.LunariteMediumChestplate, ItemId.LunariteMediumLegs,
			130, 220, 160 );

		AddMediumArmorSet( "abyssium_medium", "Abyssium Medium", ItemId.AbyssiumBar, 50,
			ItemId.AbyssiumMediumHelm, ItemId.AbyssiumMediumChestplate, ItemId.AbyssiumMediumLegs,
			190, 320, 240 );

		AddLightArmorSet( "coppite_light", "Coppite Light", ItemId.CoppiteBar, 1,
			ItemId.CoppiteLightHelm, ItemId.CoppiteLightChestplate, ItemId.CoppiteLightLegs,
			15, 30, 22 );

		AddLightArmorSet( "ashsteel_light", "Ashsteel Light", ItemId.AshsteelBar, 10,
			ItemId.AshsteelLightHelm, ItemId.AshsteelLightChestplate, ItemId.AshsteelLightLegs,
			35, 65, 48 );

		AddLightArmorSet( "coldvein_light", "Coldvein Light", ItemId.ColdveinBar, 20,
			ItemId.ColdveinLightHelm, ItemId.ColdveinLightChestplate, ItemId.ColdveinLightLegs,
			60, 100, 75 );

		AddLightArmorSet( "solarium_light", "Solarium Light", ItemId.SolariumBar, 30,
			ItemId.SolariumLightHelm, ItemId.SolariumLightChestplate, ItemId.SolariumLightLegs,
			90, 150, 110 );

		AddLightArmorSet( "lunarite_light", "Lunarite Light", ItemId.LunariteBar, 40,
			ItemId.LunariteLightHelm, ItemId.LunariteLightChestplate, ItemId.LunariteLightLegs,
			130, 220, 160 );

		AddLightArmorSet( "abyssium_light", "Abyssium Light", ItemId.AbyssiumBar, 50,
			ItemId.AbyssiumLightHelm, ItemId.AbyssiumLightChestplate, ItemId.AbyssiumLightLegs,
			190, 320, 240 );

		Add( Define( "ring", "Ring", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.Gem, 1 ), new RecipeIngredient( ItemId.AshsteelBar, 3 ), new RecipeIngredient( ItemId.Nugget, 1 ) },
			ItemId.Ring, 1, SkillType.Smithing, 1, SkillType.Smithing, 50 ) );

		Add( Define( "amulet", "Amulet", StationType.Anvil,
			new[] { new RecipeIngredient( ItemId.Gem, 1 ), new RecipeIngredient( ItemId.AshsteelBar, 5 ), new RecipeIngredient( ItemId.Nugget, 2 ) },
			ItemId.Amulet, 1, SkillType.Smithing, 1, SkillType.Smithing, 70 ) );

		Add( Define( "ashwood_bow", "Ashwood Bow", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.AshwoodLog, 24 ), new RecipeIngredient( ItemId.RoughFiber, 12 ) },
			ItemId.AshwoodBow, 1, SkillType.Crafting, 1, SkillType.Crafting, 15 ) );

		Add( Define( "elmheart_bow", "Elmheart Bow", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.ElmheartLog, 24 ), new RecipeIngredient( ItemId.RoughFiber, 12 ) },
			ItemId.ElmheartBow, 1, SkillType.Crafting, 10, SkillType.Crafting, 35 ) );

		Add( Define( "ironbark_bow", "Ironbark Bow", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.IronbarkLog, 24 ), new RecipeIngredient( ItemId.RoughFiber, 12 ) },
			ItemId.IronbarkBow, 1, SkillType.Crafting, 20, SkillType.Crafting, 65 ) );

		Add( Define( "ghostwood_bow", "Ghostwood Bow", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.GhostwoodLog, 24 ), new RecipeIngredient( ItemId.RoughFiber, 12 ) },
			ItemId.GhostwoodBow, 1, SkillType.Crafting, 30, SkillType.Crafting, 100 ) );

		Add( Define( "duskoak_bow", "Duskoak Bow", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.DuskoakLog, 24 ), new RecipeIngredient( ItemId.RoughFiber, 12 ) },
			ItemId.DuskoakBow, 1, SkillType.Crafting, 40, SkillType.Crafting, 150 ) );

		Add( Define( "worldroot_bow", "Worldroot Bow", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.WorldrootLog, 24 ), new RecipeIngredient( ItemId.RoughFiber, 12 ) },
			ItemId.WorldrootBow, 1, SkillType.Crafting, 50, SkillType.Crafting, 220 ) );

		Add( Define( "coppite_arrows", "Coppite Arrows", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.CoppiteBar, 6 ), new RecipeIngredient( ItemId.AshwoodLog, 3 ) },
			ItemId.CoppiteArrow, 15, SkillType.Crafting, 1, SkillType.Crafting, 8 ) );

		Add( Define( "ashsteel_arrows", "Ashsteel Arrows", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.AshsteelBar, 6 ), new RecipeIngredient( ItemId.ElmheartLog, 3 ) },
			ItemId.AshsteelArrow, 15, SkillType.Crafting, 10, SkillType.Crafting, 20 ) );

		Add( Define( "coldvein_arrows", "Coldvein Arrows", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.ColdveinBar, 6 ), new RecipeIngredient( ItemId.IronbarkLog, 3 ) },
			ItemId.ColdveinArrow, 15, SkillType.Crafting, 20, SkillType.Crafting, 40 ) );

		Add( Define( "solarium_arrows", "Solarium Arrows", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.SolariumBar, 6 ), new RecipeIngredient( ItemId.GhostwoodLog, 3 ) },
			ItemId.SolariumArrow, 15, SkillType.Crafting, 30, SkillType.Crafting, 65 ) );

		Add( Define( "lunarite_arrows", "Lunarite Arrows", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.LunariteBar, 6 ), new RecipeIngredient( ItemId.DuskoakLog, 3 ) },
			ItemId.LunariteArrow, 15, SkillType.Crafting, 40, SkillType.Crafting, 100 ) );

		Add( Define( "abyssium_arrows", "Abyssium Arrows", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.AbyssiumBar, 6 ), new RecipeIngredient( ItemId.WorldrootLog, 3 ) },
			ItemId.AbyssiumArrow, 15, SkillType.Crafting, 50, SkillType.Crafting, 150 ) );

		Add( Define( "ashwood_staff", "Ashwood Staff", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.AshwoodLog, 24 ), new RecipeIngredient( ItemId.CoppiteBar, 12 ) },
			ItemId.AshwoodStaff, 1, SkillType.Crafting, 1, SkillType.Crafting, 15 ) );

		Add( Define( "elmheart_staff", "Elmheart Staff", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.ElmheartLog, 24 ), new RecipeIngredient( ItemId.AshsteelBar, 12 ) },
			ItemId.ElmheartStaff, 1, SkillType.Crafting, 10, SkillType.Crafting, 35 ) );

		Add( Define( "ironbark_staff", "Ironbark Staff", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.IronbarkLog, 24 ), new RecipeIngredient( ItemId.ColdveinBar, 12 ) },
			ItemId.IronbarkStaff, 1, SkillType.Crafting, 20, SkillType.Crafting, 65 ) );

		Add( Define( "ghostwood_staff", "Ghostwood Staff", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.GhostwoodLog, 24 ), new RecipeIngredient( ItemId.SolariumBar, 12 ) },
			ItemId.GhostwoodStaff, 1, SkillType.Crafting, 30, SkillType.Crafting, 100 ) );

		Add( Define( "duskoak_staff", "Duskoak Staff", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.DuskoakLog, 24 ), new RecipeIngredient( ItemId.LunariteBar, 12 ) },
			ItemId.DuskoakStaff, 1, SkillType.Crafting, 40, SkillType.Crafting, 150 ) );

		Add( Define( "worldroot_staff", "Worldroot Staff", StationType.Workbench,
			new[] { new RecipeIngredient( ItemId.WorldrootLog, 24 ), new RecipeIngredient( ItemId.AbyssiumBar, 12 ) },
			ItemId.WorldrootStaff, 1, SkillType.Crafting, 50, SkillType.Crafting, 220 ) );

		Add( Define( "lesser_healing_potion", "Lesser Healing Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.SageLeaf, 1 ), new RecipeIngredient( ItemId.WildBerries, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.LesserHealingPotion, 1, SkillType.Crafting, 1, SkillType.Crafting, 10 ) );

		Add( Define( "healing_potion", "Healing Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Thornroot, 1 ), new RecipeIngredient( ItemId.BlueMoss, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.HealingPotion, 1, SkillType.Crafting, 15, SkillType.Crafting, 30 ) );

		Add( Define( "greater_healing_potion", "Greater Healing Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.VoidcapMushroom, 1 ), new RecipeIngredient( ItemId.Liferoot, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.GreaterHealingPotion, 1, SkillType.Crafting, 35, SkillType.Crafting, 100 ) );

		Add( Define( "lesser_mana_potion", "Lesser Mana Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.BlueMoss, 1 ), new RecipeIngredient( ItemId.ArcaneDust, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.LesserManaPotion, 1, SkillType.Crafting, 5, SkillType.Crafting, 15 ) );

		Add( Define( "mana_potion", "Mana Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Whisperfern, 1 ), new RecipeIngredient( ItemId.ArcaneDust, 2 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.ManaPotion, 1, SkillType.Crafting, 20, SkillType.Crafting, 50 ) );

		Add( Define( "greater_mana_potion", "Greater Mana Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Moonbloom, 1 ), new RecipeIngredient( ItemId.ArcaneDust, 3 ), new RecipeIngredient( ItemId.CrystalVial, 1 ) },
			ItemId.GreaterManaPotion, 1, SkillType.Crafting, 35, SkillType.Crafting, 100 ) );

		Add( Define( "attack_potion", "Attack Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Spiralvine, 1 ), new RecipeIngredient( ItemId.NightshadeStem, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.AttackPotion, 1, SkillType.Crafting, 20, SkillType.Crafting, 50 ) );

		Add( Define( "defence_potion", "Defence Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Goldpetal, 1 ), new RecipeIngredient( ItemId.CaveLichen, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.DefencePotion, 1, SkillType.Crafting, 20, SkillType.Crafting, 50 ) );

		Add( Define( "archery_potion", "Archery Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Moonbloom, 1 ), new RecipeIngredient( ItemId.Whisperfern, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.ArcheryPotion, 1, SkillType.Crafting, 25, SkillType.Crafting, 65 ) );

		Add( Define( "magic_potion", "Magic Potion", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Whisperfern, 1 ), new RecipeIngredient( ItemId.ArcaneDust, 1 ), new RecipeIngredient( ItemId.GlassVial, 1 ) },
			ItemId.MagicPotion, 1, SkillType.Crafting, 25, SkillType.Crafting, 65 ) );

		Add( Define( "elixir_of_power", "Elixir of Power", StationType.AlchemyTable,
			new[] { new RecipeIngredient( ItemId.Starbloom, 1 ), new RecipeIngredient( ItemId.Liferoot, 1 ), new RecipeIngredient( ItemId.NightshadeStem, 1 ), new RecipeIngredient( ItemId.CrystalVial, 1 ) },
			ItemId.ElixirOfPower, 1, SkillType.Crafting, 45, SkillType.Crafting, 200 ) );
	}

	static void AddHeavyArmorSet(
		string idPrefix, string namePrefix, ItemId barItem, int level,
		ItemId helm, ItemId chest, ItemId legs,
		int xpHelm, int xpChest, int xpLegs
	)
	{
		Add( Define( $"{idPrefix}_helm", $"{namePrefix} Helm", StationType.Anvil,
			new[] { new RecipeIngredient( barItem, 18 ), new RecipeIngredient( ItemId.MonsterHide, 2 ), new RecipeIngredient( ItemId.RoughFiber, 2 ) },
			helm, 1, SkillType.Smithing, level, SkillType.Smithing, xpHelm ) );

		Add( Define( $"{idPrefix}_chestplate", $"{namePrefix} Chestplate", StationType.Anvil,
			new[] { new RecipeIngredient( barItem, 36 ), new RecipeIngredient( ItemId.MonsterHide, 2 ), new RecipeIngredient( ItemId.RoughFiber, 2 ) },
			chest, 1, SkillType.Smithing, level, SkillType.Smithing, xpChest ) );

		Add( Define( $"{idPrefix}_legs", $"{namePrefix} Legs", StationType.Anvil,
			new[] { new RecipeIngredient( barItem, 27 ), new RecipeIngredient( ItemId.MonsterHide, 2 ), new RecipeIngredient( ItemId.RoughFiber, 2 ) },
			legs, 1, SkillType.Smithing, level, SkillType.Smithing, xpLegs ) );
	}

	static void AddMediumArmorSet(
		string idPrefix, string namePrefix, ItemId barItem, int level,
		ItemId helm, ItemId chest, ItemId legs,
		int xpHelm, int xpChest, int xpLegs
	)
	{
		Add( Define( $"{idPrefix}_helm", $"{namePrefix} Helm", StationType.Workbench,
			new[] { new RecipeIngredient( barItem, 12 ), new RecipeIngredient( ItemId.MonsterHide, 6 ), new RecipeIngredient( ItemId.RoughFiber, 2 ) },
			helm, 1, SkillType.Crafting, level, SkillType.Crafting, xpHelm ) );

		Add( Define( $"{idPrefix}_chestplate", $"{namePrefix} Chestplate", StationType.Workbench,
			new[] { new RecipeIngredient( barItem, 24 ), new RecipeIngredient( ItemId.MonsterHide, 6 ), new RecipeIngredient( ItemId.RoughFiber, 2 ) },
			chest, 1, SkillType.Crafting, level, SkillType.Crafting, xpChest ) );

		Add( Define( $"{idPrefix}_legs", $"{namePrefix} Legs", StationType.Workbench,
			new[] { new RecipeIngredient( barItem, 18 ), new RecipeIngredient( ItemId.MonsterHide, 6 ), new RecipeIngredient( ItemId.RoughFiber, 2 ) },
			legs, 1, SkillType.Crafting, level, SkillType.Crafting, xpLegs ) );
	}

	static void AddLightArmorSet(
		string idPrefix, string namePrefix, ItemId barItem, int level,
		ItemId helm, ItemId chest, ItemId legs,
		int xpHelm, int xpChest, int xpLegs
	)
	{
		Add( Define( $"{idPrefix}_helm", $"{namePrefix} Helm", StationType.Workbench,
			new[] { new RecipeIngredient( barItem, 9 ), new RecipeIngredient( ItemId.RoughFiber, 9 ), new RecipeIngredient( ItemId.MonsterHide, 3 ) },
			helm, 1, SkillType.Crafting, level, SkillType.Crafting, xpHelm ) );

		Add( Define( $"{idPrefix}_chestplate", $"{namePrefix} Chestplate", StationType.Workbench,
			new[] { new RecipeIngredient( barItem, 18 ), new RecipeIngredient( ItemId.RoughFiber, 9 ), new RecipeIngredient( ItemId.MonsterHide, 3 ) },
			chest, 1, SkillType.Crafting, level, SkillType.Crafting, xpChest ) );

		Add( Define( $"{idPrefix}_legs", $"{namePrefix} Legs", StationType.Workbench,
			new[] { new RecipeIngredient( barItem, 12 ), new RecipeIngredient( ItemId.RoughFiber, 9 ), new RecipeIngredient( ItemId.MonsterHide, 3 ) },
			legs, 1, SkillType.Crafting, level, SkillType.Crafting, xpLegs ) );
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