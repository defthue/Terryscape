using Sandbox;

public enum EnchantmentType
{
	None,
	Sharpness,
	Piercing,
	Arcana,
	Toughness,
	Vitality,
	Focus
}

public static class EnchantmentTypes
{
	public static EnchantmentType Parse( string value )
	{
		if ( string.IsNullOrEmpty( value ) )
			return EnchantmentType.None;

		if ( value == "Power" )
			return EnchantmentType.Arcana;

		return System.Enum.TryParse<EnchantmentType>( value, out var parsed ) ? parsed : EnchantmentType.None;
	}

	public static string GetDescription( EnchantmentType type )
	{
		switch ( type )
		{
			case EnchantmentType.Sharpness: return "Increases melee damage dealt.";
			case EnchantmentType.Piercing: return "Increases ranged damage dealt.";
			case EnchantmentType.Arcana: return "Increases magic damage dealt.";
			case EnchantmentType.Toughness: return "Reduces damage taken.";
			case EnchantmentType.Vitality: return "Increases maximum health.";
			case EnchantmentType.Focus: return "Increases maximum mana.";
			default: return "";
		}
	}

	public static string GetColor( EnchantmentType type )
	{
		switch ( type )
		{
			case EnchantmentType.Sharpness: return "#d08080";
			case EnchantmentType.Piercing: return "#a0c080";
			case EnchantmentType.Arcana: return "#8090d0";
			case EnchantmentType.Toughness: return "#b0a080";
			case EnchantmentType.Vitality: return "#d09090";
			case EnchantmentType.Focus: return "#80c0d0";
			default: return "#8a7a5c";
		}
	}

	public static string GetGlyph( EnchantmentType type )
	{
		switch ( type )
		{
			case EnchantmentType.Sharpness: return "⚔";
			case EnchantmentType.Piercing: return "➶";
			case EnchantmentType.Arcana: return "✦";
			case EnchantmentType.Toughness: return "🛡";
			case EnchantmentType.Vitality: return "❤";
			case EnchantmentType.Focus: return "◉";
			default: return "ᛟ";
		}
	}
}

public class ItemInstance
{
	public ItemId ItemId;
	public EnchantmentType Enchantment;
	public float EnchantmentPercent;
	public ItemInstance Socket1;
	public ItemInstance Socket2;
	public string CustomName;

	public ItemInstance()
	{
		ItemId = ItemId.None;
		Enchantment = EnchantmentType.None;
		EnchantmentPercent = 0f;
		Socket1 = null;
		Socket2 = null;
		CustomName = null;
	}

	public ItemInstance( ItemId itemId )
	{
		ItemId = itemId;
		Enchantment = EnchantmentType.None;
		EnchantmentPercent = 0f;
		Socket1 = null;
		Socket2 = null;
		CustomName = null;
	}

	public ItemInstance( ItemId itemId, EnchantmentType enchantment, float percent )
	{
		ItemId = itemId;
		Enchantment = enchantment;
		EnchantmentPercent = percent;
		Socket1 = null;
		Socket2 = null;
		CustomName = null;
	}

	public bool IsEnchanted => Enchantment != EnchantmentType.None && EnchantmentPercent > 0f;

	public bool HasCustomName => !string.IsNullOrEmpty( CustomName );

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

	public string GetBaseName()
	{
		var def = ItemDatabase.Get( ItemId );
		return def != null ? def.Name : ItemId.ToString();
	}

	public string GetDisplayName()
	{
		string baseName = HasCustomName ? CustomName : GetBaseName();

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