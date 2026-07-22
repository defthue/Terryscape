public static class ChatColors
{
	public const string ServerGold = "#f0c040";
	public const string Prestige = "#a080d0";

	static readonly string[] Palette =
	{
		"#5a9cf0",
		"#4ecbe0",
		"#3fbfa0",
		"#57c85a",
		"#a8d84a",
		"#f09040",
		"#f07a6a",
		"#f08ab8",
		"#d968d9",
		"#b49af0",
		"#78b8f5",
		"#7ee0b0",
		"#f5b48a",
		"#8a95e8"
	};

	public static string ForSteamId( ulong steamId )
	{
		if ( steamId == 0ul )
			return null;

		return Palette[(int)( steamId % (ulong)Palette.Length )];
	}
}
