using System.Collections.Generic;

public enum ItemId
{
	None,

	AshwoodLog,
	ElmheartLog,
	IronbarkLog,
	GhostwoodLog,
	DuskoakLog,
	WorldrootLog,

	CoppiteOre,
	AshsteelOre,
	ColdveinOre,
	SolariumOre,
	LunariteOre,
	AbyssiumOre,

	Rock,
	Coal,

	RoughGem,
	FineGem,
	PristineGem,

	CoppiteBar,
	AshsteelBar,
	ColdveinBar,
	SolariumBar,
	LunariteBar,
	AbyssiumBar,

	SageLeaf,
	Thornroot,
	Spiralvine,
	Moonbloom,
	VoidcapMushroom,
	Starbloom,

	WildBerries,
	BlueMoss,
	Goldpetal,
	Whisperfern,
	NightshadeStem,
	Liferoot,
	RoughFiber,
	CaveLichen,

	ArcaneDust,

	CoppiteHatchet,
	AshsteelHatchet,
	ColdveinHatchet,
	SolariumHatchet,
	LunariteHatchet,
	AbyssiumHatchet,

	CoppitePickaxe,
	AshsteelPickaxe,
	ColdveinPickaxe,
	SolariumPickaxe,
	LunaritePickaxe,
	AbyssiumPickaxe,

	CoppiteSword,
	AshsteelSword,
	ColdveinSword,
	SolariumSword,
	LunariteSword,
	AbyssiumSword,

	AshwoodBow,
	ElmheartBow,
	IronbarkBow,
	GhostwoodBow,
	DuskoakBow,
	WorldrootBow,

	AshwoodStaff,
	ElmheartStaff,
	IronbarkStaff,
	GhostwoodStaff,
	DuskoakStaff,
	WorldrootStaff,

	CoppiteArrow,
	AshsteelArrow,
	ColdveinArrow,
	SolariumArrow,
	LunariteArrow,
	AbyssiumArrow,

	CoppiteShield,
	AshsteelShield,
	ColdveinShield,
	SolariumShield,
	LunariteShield,
	AbyssiumShield,

	CoppiteHeavyHelm,
	CoppiteHeavyChestplate,
	CoppiteHeavyLegs,
	AshsteelHeavyHelm,
	AshsteelHeavyChestplate,
	AshsteelHeavyLegs,
	ColdveinHeavyHelm,
	ColdveinHeavyChestplate,
	ColdveinHeavyLegs,
	SolariumHeavyHelm,
	SolariumHeavyChestplate,
	SolariumHeavyLegs,
	LunariteHeavyHelm,
	LunariteHeavyChestplate,
	LunariteHeavyLegs,
	AbyssiumHeavyHelm,
	AbyssiumHeavyChestplate,
	AbyssiumHeavyLegs,

	CoppiteMediumHelm,
	CoppiteMediumChestplate,
	CoppiteMediumLegs,
	AshsteelMediumHelm,
	AshsteelMediumChestplate,
	AshsteelMediumLegs,
	ColdveinMediumHelm,
	ColdveinMediumChestplate,
	ColdveinMediumLegs,
	SolariumMediumHelm,
	SolariumMediumChestplate,
	SolariumMediumLegs,
	LunariteMediumHelm,
	LunariteMediumChestplate,
	LunariteMediumLegs,
	AbyssiumMediumHelm,
	AbyssiumMediumChestplate,
	AbyssiumMediumLegs,

	CoppiteLightHelm,
	CoppiteLightChestplate,
	CoppiteLightLegs,
	AshsteelLightHelm,
	AshsteelLightChestplate,
	AshsteelLightLegs,
	ColdveinLightHelm,
	ColdveinLightChestplate,
	ColdveinLightLegs,
	SolariumLightHelm,
	SolariumLightChestplate,
	SolariumLightLegs,
	LunariteLightHelm,
	LunariteLightChestplate,
	LunariteLightLegs,
	AbyssiumLightHelm,
	AbyssiumLightChestplate,
	AbyssiumLightLegs,

	RoughRing,
	FineRing,
	PristineRing,
	RoughAmulet,
	FineAmulet,
	PristineAmulet,

	LesserHealingPotion,
	HealingPotion,
	AttackPotion,
	DefencePotion,
	ArcheryPotion,
	MagicPotion,
	GreaterHealingPotion,
	ElixirOfPower,

	LesserManaPotion,
	ManaPotion,
	GreaterManaPotion,

	GlassVial,
	CrystalVial,
	GoldCoin,
	MonsterBone,
	MonsterHide,
	Nugget,

	Sticks,
	PrimitiveHatchet,
	PrimitivePickaxe,
	PrimitiveSword
}

public enum ItemType
{
	Resource,
	Bar,
	Herb,
	Tool,
	MeleeWeapon,
	RangedWeapon,
	MagicWeapon,
	Arrow,
	Shield,
	HeavyArmor,
	MediumArmor,
	LightArmor,
	Ring,
	Amulet,
	Potion,
	Misc
}

public enum EquipSlot
{
	None,
	Weapon,
	Shield,
	Head,
	Chest,
	Legs,
	Ring,
	Amulet,
	Ammo
}

public enum SkillType
{
	None,
	Woodcutting,
	Mining,
	Enchanting,
	Smithing,
	Crafting,
	Attack,
	Defence,
	Archery,
	Magic
}

public class ItemDefinition
{
	public ItemId Id;
	public string Name;
	public ItemType Type;
	public int Tier;
	public int MaxStack;
	public EquipSlot Slot;
	public SkillType SkillRequired;
	public int LevelRequired;
	public float ToolPower;
	public float WeaponPower;
	public float ArmorValue;
}

public static class ItemDatabase
{
	static Dictionary<ItemId, ItemDefinition> _items;

	static ItemDefinition Define(
		ItemId id,
		string name,
		ItemType type,
		int tier = 0,
		int maxStack = 999,
		EquipSlot slot = EquipSlot.None,
		SkillType skillRequired = SkillType.None,
		int levelRequired = 0,
		float toolPower = 0f,
		float weaponPower = 0f,
		float armorValue = 0f
	)
	{
		return new ItemDefinition
		{
			Id = id,
			Name = name,
			Type = type,
			Tier = tier,
			MaxStack = maxStack,
			Slot = slot,
			SkillRequired = skillRequired,
			LevelRequired = levelRequired,
			ToolPower = toolPower,
			WeaponPower = weaponPower,
			ArmorValue = armorValue
		};
	}

	static void Build()
	{
		_items = new Dictionary<ItemId, ItemDefinition>();

		Add( Define( ItemId.AshwoodLog, "Ashwood Log", ItemType.Resource, tier: 1 ) );
		Add( Define( ItemId.ElmheartLog, "Elmheart Log", ItemType.Resource, tier: 2 ) );
		Add( Define( ItemId.IronbarkLog, "Ironbark Log", ItemType.Resource, tier: 3 ) );
		Add( Define( ItemId.GhostwoodLog, "Ghostwood Log", ItemType.Resource, tier: 4 ) );
		Add( Define( ItemId.DuskoakLog, "Duskoak Log", ItemType.Resource, tier: 5 ) );
		Add( Define( ItemId.WorldrootLog, "Worldroot Log", ItemType.Resource, tier: 6 ) );

		Add( Define( ItemId.CoppiteOre, "Coppite Ore", ItemType.Resource, tier: 1 ) );
		Add( Define( ItemId.AshsteelOre, "Ashsteel Ore", ItemType.Resource, tier: 2 ) );
		Add( Define( ItemId.ColdveinOre, "Coldvein Ore", ItemType.Resource, tier: 3 ) );
		Add( Define( ItemId.SolariumOre, "Solarium Ore", ItemType.Resource, tier: 4 ) );
		Add( Define( ItemId.LunariteOre, "Lunarite Ore", ItemType.Resource, tier: 5 ) );
		Add( Define( ItemId.AbyssiumOre, "Abyssium Ore", ItemType.Resource, tier: 6 ) );

		Add( Define( ItemId.Sticks, "Sticks", ItemType.Resource, tier: 0 ) );
		Add( Define( ItemId.Rock, "Rock", ItemType.Resource, tier: 0 ) );
		Add( Define( ItemId.Coal, "Coal", ItemType.Resource, tier: 0 ) );

		Add( Define( ItemId.RoughGem, "Rough Gem", ItemType.Resource, tier: 1 ) );
		Add( Define( ItemId.FineGem, "Fine Gem", ItemType.Resource, tier: 3 ) );
		Add( Define( ItemId.PristineGem, "Pristine Gem", ItemType.Resource, tier: 5 ) );

		Add( Define( ItemId.CoppiteBar, "Coppite Bar", ItemType.Bar, tier: 1 ) );
		Add( Define( ItemId.AshsteelBar, "Ashsteel Bar", ItemType.Bar, tier: 2 ) );
		Add( Define( ItemId.ColdveinBar, "Coldvein Bar", ItemType.Bar, tier: 3 ) );
		Add( Define( ItemId.SolariumBar, "Solarium Bar", ItemType.Bar, tier: 4 ) );
		Add( Define( ItemId.LunariteBar, "Lunarite Bar", ItemType.Bar, tier: 5 ) );
		Add( Define( ItemId.AbyssiumBar, "Abyssium Bar", ItemType.Bar, tier: 6 ) );

		Add( Define( ItemId.SageLeaf, "Sage Leaf", ItemType.Herb, tier: 1 ) );
		Add( Define( ItemId.Thornroot, "Thornroot", ItemType.Herb, tier: 2 ) );
		Add( Define( ItemId.Spiralvine, "Spiralvine", ItemType.Herb, tier: 3 ) );
		Add( Define( ItemId.Moonbloom, "Moonbloom", ItemType.Herb, tier: 4 ) );
		Add( Define( ItemId.VoidcapMushroom, "Voidcap Mushroom", ItemType.Herb, tier: 5 ) );
		Add( Define( ItemId.Starbloom, "Starbloom", ItemType.Herb, tier: 6 ) );

		Add( Define( ItemId.WildBerries, "Wild Berries", ItemType.Herb, tier: 1 ) );
		Add( Define( ItemId.BlueMoss, "Blue Moss", ItemType.Herb, tier: 2 ) );
		Add( Define( ItemId.Goldpetal, "Goldpetal", ItemType.Herb, tier: 3 ) );
		Add( Define( ItemId.Whisperfern, "Whisperfern", ItemType.Herb, tier: 4 ) );
		Add( Define( ItemId.NightshadeStem, "Nightshade Stem", ItemType.Herb, tier: 5 ) );
		Add( Define( ItemId.Liferoot, "Liferoot", ItemType.Herb, tier: 6 ) );
		Add( Define( ItemId.RoughFiber, "Rough Fiber", ItemType.Herb, tier: 1 ) );
		Add( Define( ItemId.CaveLichen, "Cave Lichen", ItemType.Herb, tier: 2 ) );

		Add( Define( ItemId.ArcaneDust, "Rune Essence", ItemType.Resource, tier: 0 ) );

		Add( Define( ItemId.PrimitiveHatchet, "Primitive Hatchet", ItemType.Tool, tier: 0, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Woodcutting, levelRequired: 1, toolPower: 1.0f, weaponPower: 2f ) );
		Add( Define( ItemId.PrimitivePickaxe, "Primitive Pickaxe", ItemType.Tool, tier: 0, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Mining, levelRequired: 1, toolPower: 1.0f, weaponPower: 2f ) );
		Add( Define( ItemId.PrimitiveSword, "Primitive Sword", ItemType.MeleeWeapon, tier: 0, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Attack, levelRequired: 1, weaponPower: 3f ) );

		Add( Define( ItemId.CoppiteHatchet, "Coppite Hatchet", ItemType.Tool, tier: 1, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Woodcutting, levelRequired: 1, toolPower: 2.0f, weaponPower: 4f ) );
		Add( Define( ItemId.AshsteelHatchet, "Ashsteel Hatchet", ItemType.Tool, tier: 2, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Woodcutting, levelRequired: 10, toolPower: 3.0f, weaponPower: 8f ) );
		Add( Define( ItemId.ColdveinHatchet, "Coldvein Hatchet", ItemType.Tool, tier: 3, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Woodcutting, levelRequired: 20, toolPower: 4.5f, weaponPower: 12f ) );
		Add( Define( ItemId.SolariumHatchet, "Solarium Hatchet", ItemType.Tool, tier: 4, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Woodcutting, levelRequired: 30, toolPower: 5.5f, weaponPower: 16f ) );
		Add( Define( ItemId.LunariteHatchet, "Lunarite Hatchet", ItemType.Tool, tier: 5, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Woodcutting, levelRequired: 40, toolPower: 7.0f, weaponPower: 20f ) );
		Add( Define( ItemId.AbyssiumHatchet, "Abyssium Hatchet", ItemType.Tool, tier: 6, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Woodcutting, levelRequired: 50, toolPower: 8.0f, weaponPower: 24f ) );

		Add( Define( ItemId.CoppitePickaxe, "Coppite Pickaxe", ItemType.Tool, tier: 1, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Mining, levelRequired: 1, toolPower: 2.0f, weaponPower: 4f ) );
		Add( Define( ItemId.AshsteelPickaxe, "Ashsteel Pickaxe", ItemType.Tool, tier: 2, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Mining, levelRequired: 10, toolPower: 3.0f, weaponPower: 8f ) );
		Add( Define( ItemId.ColdveinPickaxe, "Coldvein Pickaxe", ItemType.Tool, tier: 3, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Mining, levelRequired: 20, toolPower: 4.5f, weaponPower: 12f ) );
		Add( Define( ItemId.SolariumPickaxe, "Solarium Pickaxe", ItemType.Tool, tier: 4, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Mining, levelRequired: 30, toolPower: 5.5f, weaponPower: 16f ) );
		Add( Define( ItemId.LunaritePickaxe, "Lunarite Pickaxe", ItemType.Tool, tier: 5, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Mining, levelRequired: 40, toolPower: 7.0f, weaponPower: 20f ) );
		Add( Define( ItemId.AbyssiumPickaxe, "Abyssium Pickaxe", ItemType.Tool, tier: 6, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Mining, levelRequired: 50, toolPower: 8.0f, weaponPower: 24f ) );

		Add( Define( ItemId.CoppiteSword, "Coppite Sword", ItemType.MeleeWeapon, tier: 1, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Attack, levelRequired: 1, weaponPower: 6f ) );
		Add( Define( ItemId.AshsteelSword, "Ashsteel Sword", ItemType.MeleeWeapon, tier: 2, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Attack, levelRequired: 10, weaponPower: 12f ) );
		Add( Define( ItemId.ColdveinSword, "Coldvein Sword", ItemType.MeleeWeapon, tier: 3, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Attack, levelRequired: 20, weaponPower: 20f ) );
		Add( Define( ItemId.SolariumSword, "Solarium Sword", ItemType.MeleeWeapon, tier: 4, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Attack, levelRequired: 30, weaponPower: 30f ) );
		Add( Define( ItemId.LunariteSword, "Lunarite Sword", ItemType.MeleeWeapon, tier: 5, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Attack, levelRequired: 40, weaponPower: 42f ) );
		Add( Define( ItemId.AbyssiumSword, "Abyssium Sword", ItemType.MeleeWeapon, tier: 6, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Attack, levelRequired: 50, weaponPower: 56f ) );

		Add( Define( ItemId.AshwoodBow, "Ashwood Bow", ItemType.RangedWeapon, tier: 1, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Archery, levelRequired: 1, weaponPower: 5f ) );
		Add( Define( ItemId.ElmheartBow, "Elmheart Bow", ItemType.RangedWeapon, tier: 2, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Archery, levelRequired: 10, weaponPower: 10f ) );
		Add( Define( ItemId.IronbarkBow, "Ironbark Bow", ItemType.RangedWeapon, tier: 3, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Archery, levelRequired: 20, weaponPower: 17f ) );
		Add( Define( ItemId.GhostwoodBow, "Ghostwood Bow", ItemType.RangedWeapon, tier: 4, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Archery, levelRequired: 30, weaponPower: 26f ) );
		Add( Define( ItemId.DuskoakBow, "Duskoak Bow", ItemType.RangedWeapon, tier: 5, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Archery, levelRequired: 40, weaponPower: 37f ) );
		Add( Define( ItemId.WorldrootBow, "Worldroot Bow", ItemType.RangedWeapon, tier: 6, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Archery, levelRequired: 50, weaponPower: 50f ) );

		Add( Define( ItemId.AshwoodStaff, "Ashwood Staff", ItemType.MagicWeapon, tier: 1, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Magic, levelRequired: 1, weaponPower: 5f ) );
		Add( Define( ItemId.ElmheartStaff, "Elmheart Staff", ItemType.MagicWeapon, tier: 2, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Magic, levelRequired: 10, weaponPower: 10f ) );
		Add( Define( ItemId.IronbarkStaff, "Ironbark Staff", ItemType.MagicWeapon, tier: 3, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Magic, levelRequired: 20, weaponPower: 17f ) );
		Add( Define( ItemId.GhostwoodStaff, "Ghostwood Staff", ItemType.MagicWeapon, tier: 4, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Magic, levelRequired: 30, weaponPower: 26f ) );
		Add( Define( ItemId.DuskoakStaff, "Duskoak Staff", ItemType.MagicWeapon, tier: 5, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Magic, levelRequired: 40, weaponPower: 37f ) );
		Add( Define( ItemId.WorldrootStaff, "Worldroot Staff", ItemType.MagicWeapon, tier: 6, maxStack: 1, slot: EquipSlot.Weapon, skillRequired: SkillType.Magic, levelRequired: 50, weaponPower: 50f ) );

		Add( Define( ItemId.CoppiteArrow, "Coppite Arrow", ItemType.Arrow, tier: 1, slot: EquipSlot.Ammo, skillRequired: SkillType.Archery, levelRequired: 1, weaponPower: 2f ) );
		Add( Define( ItemId.AshsteelArrow, "Ashsteel Arrow", ItemType.Arrow, tier: 2, slot: EquipSlot.Ammo, skillRequired: SkillType.Archery, levelRequired: 10, weaponPower: 5f ) );
		Add( Define( ItemId.ColdveinArrow, "Coldvein Arrow", ItemType.Arrow, tier: 3, slot: EquipSlot.Ammo, skillRequired: SkillType.Archery, levelRequired: 20, weaponPower: 9f ) );
		Add( Define( ItemId.SolariumArrow, "Solarium Arrow", ItemType.Arrow, tier: 4, slot: EquipSlot.Ammo, skillRequired: SkillType.Archery, levelRequired: 30, weaponPower: 14f ) );
		Add( Define( ItemId.LunariteArrow, "Lunarite Arrow", ItemType.Arrow, tier: 5, slot: EquipSlot.Ammo, skillRequired: SkillType.Archery, levelRequired: 40, weaponPower: 20f ) );
		Add( Define( ItemId.AbyssiumArrow, "Abyssium Arrow", ItemType.Arrow, tier: 6, slot: EquipSlot.Ammo, skillRequired: SkillType.Archery, levelRequired: 50, weaponPower: 27f ) );

		Add( Define( ItemId.CoppiteShield, "Coppite Shield", ItemType.Shield, tier: 1, maxStack: 1, slot: EquipSlot.Shield, skillRequired: SkillType.Defence, levelRequired: 1, armorValue: 3f ) );
		Add( Define( ItemId.AshsteelShield, "Ashsteel Shield", ItemType.Shield, tier: 2, maxStack: 1, slot: EquipSlot.Shield, skillRequired: SkillType.Defence, levelRequired: 10, armorValue: 7f ) );
		Add( Define( ItemId.ColdveinShield, "Coldvein Shield", ItemType.Shield, tier: 3, maxStack: 1, slot: EquipSlot.Shield, skillRequired: SkillType.Defence, levelRequired: 20, armorValue: 12f ) );
		Add( Define( ItemId.SolariumShield, "Solarium Shield", ItemType.Shield, tier: 4, maxStack: 1, slot: EquipSlot.Shield, skillRequired: SkillType.Defence, levelRequired: 30, armorValue: 18f ) );
		Add( Define( ItemId.LunariteShield, "Lunarite Shield", ItemType.Shield, tier: 5, maxStack: 1, slot: EquipSlot.Shield, skillRequired: SkillType.Defence, levelRequired: 40, armorValue: 25f ) );
		Add( Define( ItemId.AbyssiumShield, "Abyssium Shield", ItemType.Shield, tier: 6, maxStack: 1, slot: EquipSlot.Shield, skillRequired: SkillType.Defence, levelRequired: 50, armorValue: 33f ) );

		Add( Define( ItemId.CoppiteHeavyHelm, "Coppite Heavy Helm", ItemType.HeavyArmor, tier: 1, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Defence, levelRequired: 1, armorValue: 2f ) );
		Add( Define( ItemId.CoppiteHeavyChestplate, "Coppite Heavy Chestplate", ItemType.HeavyArmor, tier: 1, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Defence, levelRequired: 1, armorValue: 5f ) );
		Add( Define( ItemId.CoppiteHeavyLegs, "Coppite Heavy Legs", ItemType.HeavyArmor, tier: 1, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Defence, levelRequired: 1, armorValue: 3f ) );
		Add( Define( ItemId.AshsteelHeavyHelm, "Ashsteel Heavy Helm", ItemType.HeavyArmor, tier: 2, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Defence, levelRequired: 10, armorValue: 5f ) );
		Add( Define( ItemId.AshsteelHeavyChestplate, "Ashsteel Heavy Chestplate", ItemType.HeavyArmor, tier: 2, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Defence, levelRequired: 10, armorValue: 10f ) );
		Add( Define( ItemId.AshsteelHeavyLegs, "Ashsteel Heavy Legs", ItemType.HeavyArmor, tier: 2, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Defence, levelRequired: 10, armorValue: 7f ) );
		Add( Define( ItemId.ColdveinHeavyHelm, "Coldvein Heavy Helm", ItemType.HeavyArmor, tier: 3, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Defence, levelRequired: 20, armorValue: 8f ) );
		Add( Define( ItemId.ColdveinHeavyChestplate, "Coldvein Heavy Chestplate", ItemType.HeavyArmor, tier: 3, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Defence, levelRequired: 20, armorValue: 16f ) );
		Add( Define( ItemId.ColdveinHeavyLegs, "Coldvein Heavy Legs", ItemType.HeavyArmor, tier: 3, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Defence, levelRequired: 20, armorValue: 11f ) );
		Add( Define( ItemId.SolariumHeavyHelm, "Solarium Heavy Helm", ItemType.HeavyArmor, tier: 4, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Defence, levelRequired: 30, armorValue: 12f ) );
		Add( Define( ItemId.SolariumHeavyChestplate, "Solarium Heavy Chestplate", ItemType.HeavyArmor, tier: 4, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Defence, levelRequired: 30, armorValue: 24f ) );
		Add( Define( ItemId.SolariumHeavyLegs, "Solarium Heavy Legs", ItemType.HeavyArmor, tier: 4, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Defence, levelRequired: 30, armorValue: 17f ) );
		Add( Define( ItemId.LunariteHeavyHelm, "Lunarite Heavy Helm", ItemType.HeavyArmor, tier: 5, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Defence, levelRequired: 40, armorValue: 17f ) );
		Add( Define( ItemId.LunariteHeavyChestplate, "Lunarite Heavy Chestplate", ItemType.HeavyArmor, tier: 5, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Defence, levelRequired: 40, armorValue: 34f ) );
		Add( Define( ItemId.LunariteHeavyLegs, "Lunarite Heavy Legs", ItemType.HeavyArmor, tier: 5, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Defence, levelRequired: 40, armorValue: 24f ) );
		Add( Define( ItemId.AbyssiumHeavyHelm, "Abyssium Heavy Helm", ItemType.HeavyArmor, tier: 6, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Defence, levelRequired: 50, armorValue: 23f ) );
		Add( Define( ItemId.AbyssiumHeavyChestplate, "Abyssium Heavy Chestplate", ItemType.HeavyArmor, tier: 6, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Defence, levelRequired: 50, armorValue: 46f ) );
		Add( Define( ItemId.AbyssiumHeavyLegs, "Abyssium Heavy Legs", ItemType.HeavyArmor, tier: 6, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Defence, levelRequired: 50, armorValue: 32f ) );

		Add( Define( ItemId.CoppiteMediumHelm, "Coppite Medium Helm", ItemType.MediumArmor, tier: 1, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Defence, levelRequired: 1, armorValue: 1f ) );
		Add( Define( ItemId.CoppiteMediumChestplate, "Coppite Medium Chestplate", ItemType.MediumArmor, tier: 1, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Defence, levelRequired: 1, armorValue: 3f ) );
		Add( Define( ItemId.CoppiteMediumLegs, "Coppite Medium Legs", ItemType.MediumArmor, tier: 1, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Defence, levelRequired: 1, armorValue: 2f ) );
		Add( Define( ItemId.AshsteelMediumHelm, "Ashsteel Medium Helm", ItemType.MediumArmor, tier: 2, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Defence, levelRequired: 10, armorValue: 3f ) );
		Add( Define( ItemId.AshsteelMediumChestplate, "Ashsteel Medium Chestplate", ItemType.MediumArmor, tier: 2, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Defence, levelRequired: 10, armorValue: 7f ) );
		Add( Define( ItemId.AshsteelMediumLegs, "Ashsteel Medium Legs", ItemType.MediumArmor, tier: 2, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Defence, levelRequired: 10, armorValue: 5f ) );
		Add( Define( ItemId.ColdveinMediumHelm, "Coldvein Medium Helm", ItemType.MediumArmor, tier: 3, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Defence, levelRequired: 20, armorValue: 6f ) );
		Add( Define( ItemId.ColdveinMediumChestplate, "Coldvein Medium Chestplate", ItemType.MediumArmor, tier: 3, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Defence, levelRequired: 20, armorValue: 12f ) );
		Add( Define( ItemId.ColdveinMediumLegs, "Coldvein Medium Legs", ItemType.MediumArmor, tier: 3, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Defence, levelRequired: 20, armorValue: 8f ) );
		Add( Define( ItemId.SolariumMediumHelm, "Solarium Medium Helm", ItemType.MediumArmor, tier: 4, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Defence, levelRequired: 30, armorValue: 9f ) );
		Add( Define( ItemId.SolariumMediumChestplate, "Solarium Medium Chestplate", ItemType.MediumArmor, tier: 4, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Defence, levelRequired: 30, armorValue: 18f ) );
		Add( Define( ItemId.SolariumMediumLegs, "Solarium Medium Legs", ItemType.MediumArmor, tier: 4, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Defence, levelRequired: 30, armorValue: 13f ) );
		Add( Define( ItemId.LunariteMediumHelm, "Lunarite Medium Helm", ItemType.MediumArmor, tier: 5, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Defence, levelRequired: 40, armorValue: 13f ) );
		Add( Define( ItemId.LunariteMediumChestplate, "Lunarite Medium Chestplate", ItemType.MediumArmor, tier: 5, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Defence, levelRequired: 40, armorValue: 26f ) );
		Add( Define( ItemId.LunariteMediumLegs, "Lunarite Medium Legs", ItemType.MediumArmor, tier: 5, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Defence, levelRequired: 40, armorValue: 18f ) );
		Add( Define( ItemId.AbyssiumMediumHelm, "Abyssium Medium Helm", ItemType.MediumArmor, tier: 6, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Defence, levelRequired: 50, armorValue: 18f ) );
		Add( Define( ItemId.AbyssiumMediumChestplate, "Abyssium Medium Chestplate", ItemType.MediumArmor, tier: 6, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Defence, levelRequired: 50, armorValue: 36f ) );
		Add( Define( ItemId.AbyssiumMediumLegs, "Abyssium Medium Legs", ItemType.MediumArmor, tier: 6, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Defence, levelRequired: 50, armorValue: 25f ) );

		Add( Define( ItemId.CoppiteLightHelm, "Coppite Light Helm", ItemType.LightArmor, tier: 1, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Magic, levelRequired: 1, armorValue: 1f ) );
		Add( Define( ItemId.CoppiteLightChestplate, "Coppite Light Chestplate", ItemType.LightArmor, tier: 1, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Magic, levelRequired: 1, armorValue: 2f ) );
		Add( Define( ItemId.CoppiteLightLegs, "Coppite Light Legs", ItemType.LightArmor, tier: 1, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Magic, levelRequired: 1, armorValue: 1f ) );
		Add( Define( ItemId.AshsteelLightHelm, "Ashsteel Light Helm", ItemType.LightArmor, tier: 2, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Magic, levelRequired: 10, armorValue: 2f ) );
		Add( Define( ItemId.AshsteelLightChestplate, "Ashsteel Light Chestplate", ItemType.LightArmor, tier: 2, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Magic, levelRequired: 10, armorValue: 5f ) );
		Add( Define( ItemId.AshsteelLightLegs, "Ashsteel Light Legs", ItemType.LightArmor, tier: 2, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Magic, levelRequired: 10, armorValue: 3f ) );
		Add( Define( ItemId.ColdveinLightHelm, "Coldvein Light Helm", ItemType.LightArmor, tier: 3, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Magic, levelRequired: 20, armorValue: 4f ) );
		Add( Define( ItemId.ColdveinLightChestplate, "Coldvein Light Chestplate", ItemType.LightArmor, tier: 3, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Magic, levelRequired: 20, armorValue: 9f ) );
		Add( Define( ItemId.ColdveinLightLegs, "Coldvein Light Legs", ItemType.LightArmor, tier: 3, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Magic, levelRequired: 20, armorValue: 6f ) );
		Add( Define( ItemId.SolariumLightHelm, "Solarium Light Helm", ItemType.LightArmor, tier: 4, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Magic, levelRequired: 30, armorValue: 7f ) );
		Add( Define( ItemId.SolariumLightChestplate, "Solarium Light Chestplate", ItemType.LightArmor, tier: 4, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Magic, levelRequired: 30, armorValue: 14f ) );
		Add( Define( ItemId.SolariumLightLegs, "Solarium Light Legs", ItemType.LightArmor, tier: 4, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Magic, levelRequired: 30, armorValue: 10f ) );
		Add( Define( ItemId.LunariteLightHelm, "Lunarite Light Helm", ItemType.LightArmor, tier: 5, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Magic, levelRequired: 40, armorValue: 10f ) );
		Add( Define( ItemId.LunariteLightChestplate, "Lunarite Light Chestplate", ItemType.LightArmor, tier: 5, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Magic, levelRequired: 40, armorValue: 20f ) );
		Add( Define( ItemId.LunariteLightLegs, "Lunarite Light Legs", ItemType.LightArmor, tier: 5, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Magic, levelRequired: 40, armorValue: 14f ) );
		Add( Define( ItemId.AbyssiumLightHelm, "Abyssium Light Helm", ItemType.LightArmor, tier: 6, maxStack: 1, slot: EquipSlot.Head, skillRequired: SkillType.Magic, levelRequired: 50, armorValue: 14f ) );
		Add( Define( ItemId.AbyssiumLightChestplate, "Abyssium Light Chestplate", ItemType.LightArmor, tier: 6, maxStack: 1, slot: EquipSlot.Chest, skillRequired: SkillType.Magic, levelRequired: 50, armorValue: 28f ) );
		Add( Define( ItemId.AbyssiumLightLegs, "Abyssium Light Legs", ItemType.LightArmor, tier: 6, maxStack: 1, slot: EquipSlot.Legs, skillRequired: SkillType.Magic, levelRequired: 50, armorValue: 20f ) );

		Add( Define( ItemId.RoughRing, "Rough Ring", ItemType.Ring, tier: 1, maxStack: 1, slot: EquipSlot.Ring, skillRequired: SkillType.Smithing, levelRequired: 1 ) );
		Add( Define( ItemId.FineRing, "Fine Ring", ItemType.Ring, tier: 3, maxStack: 1, slot: EquipSlot.Ring, skillRequired: SkillType.Smithing, levelRequired: 25 ) );
		Add( Define( ItemId.PristineRing, "Pristine Ring", ItemType.Ring, tier: 5, maxStack: 1, slot: EquipSlot.Ring, skillRequired: SkillType.Smithing, levelRequired: 45 ) );
		Add( Define( ItemId.RoughAmulet, "Rough Amulet", ItemType.Amulet, tier: 1, maxStack: 1, slot: EquipSlot.Amulet, skillRequired: SkillType.Smithing, levelRequired: 1 ) );
		Add( Define( ItemId.FineAmulet, "Fine Amulet", ItemType.Amulet, tier: 3, maxStack: 1, slot: EquipSlot.Amulet, skillRequired: SkillType.Smithing, levelRequired: 25 ) );
		Add( Define( ItemId.PristineAmulet, "Pristine Amulet", ItemType.Amulet, tier: 5, maxStack: 1, slot: EquipSlot.Amulet, skillRequired: SkillType.Smithing, levelRequired: 45 ) );

		Add( Define( ItemId.LesserHealingPotion, "Lesser Healing Potion", ItemType.Potion, tier: 1, maxStack: 50 ) );
		Add( Define( ItemId.HealingPotion, "Healing Potion", ItemType.Potion, tier: 2, maxStack: 50 ) );
		Add( Define( ItemId.AttackPotion, "Attack Potion", ItemType.Potion, tier: 2, maxStack: 50 ) );
		Add( Define( ItemId.DefencePotion, "Defence Potion", ItemType.Potion, tier: 2, maxStack: 50 ) );
		Add( Define( ItemId.ArcheryPotion, "Archery Potion", ItemType.Potion, tier: 3, maxStack: 50 ) );
		Add( Define( ItemId.MagicPotion, "Magic Potion", ItemType.Potion, tier: 3, maxStack: 50 ) );
		Add( Define( ItemId.GreaterHealingPotion, "Greater Healing Potion", ItemType.Potion, tier: 4, maxStack: 50 ) );
		Add( Define( ItemId.ElixirOfPower, "Elixir of Power", ItemType.Potion, tier: 6, maxStack: 50 ) );

		Add( Define( ItemId.LesserManaPotion, "Lesser Mana Potion", ItemType.Potion, tier: 1, maxStack: 50 ) );
		Add( Define( ItemId.ManaPotion, "Mana Potion", ItemType.Potion, tier: 2, maxStack: 50 ) );
		Add( Define( ItemId.GreaterManaPotion, "Greater Mana Potion", ItemType.Potion, tier: 4, maxStack: 50 ) );

		Add( Define( ItemId.GlassVial, "Empty Vial", ItemType.Misc, tier: 0, maxStack: 999 ) );
		Add( Define( ItemId.CrystalVial, "Crystal Vial", ItemType.Misc, tier: 4, maxStack: 999 ) );
		Add( Define( ItemId.GoldCoin, "Gold Coin", ItemType.Misc, tier: 0, maxStack: 99999 ) );
		Add( Define( ItemId.MonsterBone, "Monster Bone", ItemType.Misc, tier: 0, maxStack: 999 ) );
		Add( Define( ItemId.MonsterHide, "Monster Hide", ItemType.Misc, tier: 0, maxStack: 999 ) );
		Add( Define( ItemId.Nugget, "Nugget", ItemType.Misc, tier: 0, maxStack: 999 ) );
	}

	static void Add( ItemDefinition def )
	{
		_items[def.Id] = def;
	}

	public static ItemDefinition Get( ItemId id )
	{
		if ( _items == null )
			Build();

		if ( _items.TryGetValue( id, out var def ) )
			return def;

		return null;
	}

	public static IEnumerable<ItemDefinition> GetAll()
	{
		if ( _items == null )
			Build();

		return _items.Values;
	}

	public static IEnumerable<ItemDefinition> GetByType( ItemType type )
	{
		if ( _items == null )
			Build();

		foreach ( var def in _items.Values )
		{
			if ( def.Type == type )
				yield return def;
		}
	}

	public static IEnumerable<ItemDefinition> GetByTier( int tier )
	{
		if ( _items == null )
			Build();

		foreach ( var def in _items.Values )
		{
			if ( def.Tier == tier )
				yield return def;
		}
	}
}