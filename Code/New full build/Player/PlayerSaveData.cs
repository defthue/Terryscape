using System.Collections.Generic;

public class PlayerSaveData
{
	public int Version { get; set; } = 1;
	public string SavedAt { get; set; } = "";
	public string PlayerName { get; set; } = "";

	public Dictionary<string, SkillEntry> Skills { get; set; } = new();

	public Dictionary<string, int> Stackables { get; set; } = new();
	public List<UniqueItemEntry> UniqueItems { get; set; } = new();
	public Dictionary<string, UniqueItemEntry> Equipped { get; set; } = new();
	public string EquippedAmmoId { get; set; } = "None";
	public int EquippedAmmoQty { get; set; } = 0;

	public List<string> Recipes { get; set; } = new();
	public List<string> Stones { get; set; } = new();
	public List<string> Quests { get; set; } = new();

	// NEW: Quest IDs the player has opened the dialogue for at least once.
	// Used by the journal HUD to show quests the player knows about, including
	// ones they haven't completed yet.
	public List<string> DiscoveredQuests { get; set; } = new();

	public Dictionary<string, int> Kills { get; set; } = new();

	// Bank storage — stackable items and unique items kept separately, mirroring inventory.
	public Dictionary<string, int> Bank { get; set; } = new();
	public List<UniqueItemEntry> BankUnique { get; set; } = new();

	// NEW: Total resource nodes harvested over the player's lifetime.
	// Used by leaderboards. Increments by 1 every time a resource node is broken.
	public int NodesMined { get; set; } = 0;

	// NEW: Denormalized leaderboard fields. Computed at save time from the other fields,
	// stored as flat top-level numbers so sbox.cool can sort/query them efficiently
	// without needing to walk nested objects.
	public int TotalLevel { get; set; } = 0;
	public int TotalGold { get; set; } = 0;
	public int TotalKills { get; set; } = 0;

	public class SkillEntry
	{
		public int Level { get; set; } = 1;
		public int Xp { get; set; } = 0;
	}

	public class UniqueItemEntry
	{
		public string ItemId { get; set; } = "None";
		public string Enchantment { get; set; } = "None";
		public float EnchantmentPercent { get; set; } = 0f;
	}
}