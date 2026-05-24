using Sandbox;

public enum EnchantmentType
{
	None,
	Sharpness,
	Piercing,
	Power,
	Toughness,
	Vitality,
	Focus
}

public class ItemInstance
{
	public ItemId ItemId;
	public EnchantmentType Enchantment;
	public float EnchantmentPercent;
	public ItemInstance Socket1;
	public ItemInstance Socket2;

	public ItemInstance()
	{
		ItemId = ItemId.None;
		Enchantment = EnchantmentType.None;
		EnchantmentPercent = 0f;
		Socket1 = null;
		Socket2 = null;
	}

	public ItemInstance( ItemId itemId )
	{
		ItemId = itemId;
		Enchantment = EnchantmentType.None;
		EnchantmentPercent = 0f;
		Socket1 = null;
		Socket2 = null;
	}

	public ItemInstance( ItemId itemId, EnchantmentType enchantment, float percent )
	{
		ItemId = itemId;
		Enchantment = enchantment;
		EnchantmentPercent = percent;
		Socket1 = null;
		Socket2 = null;
	}

	public bool IsEnchanted => Enchantment != EnchantmentType.None && EnchantmentPercent > 0f;

	public bool IsRune
	{
		get
		{
			var def = ItemDatabase.Get( ItemId );
			return def != null && def.Type == ItemType.Rune;
		}
	}

	public bool IsSocketable
	{
		get
		{
			var def = ItemDatabase.Get( ItemId );
			if ( def == null )
				return false;
			return def.Type == ItemType.Ring || def.Type == ItemType.Amulet;
		}
	}

	public int MaxSockets => IsSocketable ? 2 : 0;

	public int SocketsUsed
	{
		get
		{
			int n = 0;
			if ( Socket1 != null ) n++;
			if ( Socket2 != null ) n++;
			return n;
		}
	}

	public ItemInstance GetSocket( int index )
	{
		if ( index == 0 ) return Socket1;
		if ( index == 1 ) return Socket2;
		return null;
	}

	public void SetSocket( int index, ItemInstance rune )
	{
		if ( index == 0 ) Socket1 = rune;
		else if ( index == 1 ) Socket2 = rune;
	}

	public bool HasEnchantmentInSocket( EnchantmentType type )
	{
		if ( Socket1 != null && Socket1.Enchantment == type ) return true;
		if ( Socket2 != null && Socket2.Enchantment == type ) return true;
		return false;
	}

	public string GetDisplayName()
	{
		var def = ItemDatabase.Get( ItemId );
		string baseName = def != null ? def.Name : ItemId.ToString();

		if ( IsRune && IsEnchanted )
			return $"{baseName} (+{EnchantmentPercent:F1}% {Enchantment})";

		if ( IsSocketable && SocketsUsed > 0 )
			return $"{baseName} [{SocketsUsed}/{MaxSockets}]";

		return baseName;
	}

	public string GetShortStat()
	{
		if ( IsRune && IsEnchanted )
			return $"+{EnchantmentPercent:F1}% {Enchantment}";

		if ( IsSocketable && SocketsUsed > 0 )
		{
			string a = Socket1 != null ? $"+{Socket1.EnchantmentPercent:F1}% {Socket1.Enchantment}" : "empty";
			string b = Socket2 != null ? $"+{Socket2.EnchantmentPercent:F1}% {Socket2.Enchantment}" : "empty";
			return $"{a} / {b}";
		}

		return "";
	}
}