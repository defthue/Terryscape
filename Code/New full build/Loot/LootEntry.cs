using Sandbox;

public class LootEntry
{
	[Property] public ItemId Item { get; set; } = ItemId.None;
	[Property] public int MinAmount { get; set; } = 1;
	[Property] public int MaxAmount { get; set; } = 1;
	[Property, Range( 0f, 100f )] public float ChancePercent { get; set; } = 100f;
}
