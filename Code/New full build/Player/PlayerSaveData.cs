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

	public Dictionary<string, int> EquippedSlotIndices { get; set; } = new();
	public int EquippedAmmoSlotIndex { get; set; } = 0;

	public List<InventorySlotEntry> Slots { get; set; } = new();
	public int InventoryExpansions { get; set; } = 0;

	public List<string> Recipes { get; set; } = new();
	public List<string> Stones { get; set; } = new();
	public List<string> Quests { get; set; } = new();

	public List<string> DiscoveredQuests { get; set; } = new();

	public Dictionary<string, int> Kills { get; set; } = new();

	public Dictionary<string, int> Bank { get; set; } = new();
	public List<UniqueItemEntry> BankUnique { get; set; } = new();

	public int NodesMined { get; set; } = 0;

	public int TotalLevel { get; set; } = 0;
	public int TotalGold { get; set; } = 0;
	public int TotalKills { get; set; } = 0;

	public Dictionary<string, string> ChestClaims { get; set; } = new();

	public int CurrentMana { get; set; } = -1;
	public List<string> UnlockedSpells { get; set; } = new();
	public Dictionary<string, string> SpellSlots { get; set; } = new();

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

	public class InventorySlotEntry
	{
		public int Slot { get; set; } = 0;
		public string ItemId { get; set; } = "None";
		public int Count { get; set; } = 0;
		public bool IsUnique { get; set; } = false;
		public string Enchantment { get; set; } = "None";
		public float EnchantmentPercent { get; set; } = 0f;
	}
}