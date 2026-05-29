using Sandbox;
using System;
using System.Collections.Generic;

public class ChestLootItem
{
	[Property] public ItemId Item { get; set; } = ItemId.None;
	[Property] public int MinAmount { get; set; } = 1;
	[Property] public int MaxAmount { get; set; } = 1;
	[Property] public float Weight { get; set; } = 1f;
}

public class ChestLootTier
{
	[Property] public string Name { get; set; } = "Common";
	[Property] public float Weight { get; set; } = 1f;
	[Property] public List<ChestLootItem> Items { get; set; } = new();
}

[GameResource( "Chest Loot Table", "lchest", "Tiered loot for daily chests." )]
public class ChestLootTable : GameResource
{
	[Property] public int GoldMin { get; set; } = 0;
	[Property] public int GoldMax { get; set; } = 0;
	[Property] public int Pulls { get; set; } = 1;
	[Property] public List<ChestLootTier> Tiers { get; set; } = new();
}
