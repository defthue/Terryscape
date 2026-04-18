public enum EnchantmentType
{
	None,
	Attack,
	Defence,
	Archery,
	Magic
}

public class ItemInstance
{
	public ItemId ItemId;
	public EnchantmentType Enchantment;
	public float EnchantmentPercent;

	public ItemInstance( ItemId itemId )
	{
		ItemId = itemId;
		Enchantment = EnchantmentType.None;
		EnchantmentPercent = 0f;
	}

	public ItemInstance( ItemId itemId, EnchantmentType enchantment, float percent )
	{
		ItemId = itemId;
		Enchantment = enchantment;
		EnchantmentPercent = percent;
	}

	public bool IsEnchanted => Enchantment != EnchantmentType.None && EnchantmentPercent > 0f;

	public string GetDisplayName()
	{
		var def = ItemDatabase.Get( ItemId );
		string baseName = def != null ? def.Name : ItemId.ToString();

		if ( !IsEnchanted )
			return baseName;

		return $"{baseName} (+{EnchantmentPercent:F1}% {Enchantment})";
	}

	public string GetShortStat()
	{
		if ( !IsEnchanted )
			return "";

		return $"+{EnchantmentPercent:F1}% {Enchantment}";
	}
}
