using Sandbox;
using System;
using System.Globalization;

public static class PetColorState
{
	const string UnlockKey = "pet:colors_unlocked";
	const string ColorKey = "pet:slime_color";

	public static bool IsUnlocked()
	{
		var inv = PlayerHelper.GetLocalInventory();
		return inv != null && inv.GetProgressValue( UnlockKey ) == "1";
	}

	public static void Unlock()
	{
		PlayerHelper.GetLocalInventory()?.SetProgressValue( UnlockKey, "1" );
	}

	public static Color? GetColor()
	{
		var inv = PlayerHelper.GetLocalInventory();
		var raw = inv?.GetProgressValue( ColorKey );
		if ( string.IsNullOrEmpty( raw ) || raw == "random" )
			return null;
		return ParseHex( raw );
	}

	public static void SetColor( Color? color )
	{
		var inv = PlayerHelper.GetLocalInventory();
		if ( inv == null )
			return;

		if ( color == null )
		{
			inv.SetProgressValue( ColorKey, "random" );
			return;
		}

		var c = color.Value;
		string hex = $"#{(int)( c.r * 255f ):X2}{(int)( c.g * 255f ):X2}{(int)( c.b * 255f ):X2}";
		inv.SetProgressValue( ColorKey, hex );
	}

	static Color? ParseHex( string hex )
	{
		if ( hex.Length != 7 || hex[0] != '#' )
			return null;

		bool ok = int.TryParse( hex.Substring( 1, 2 ), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r )
			& int.TryParse( hex.Substring( 3, 2 ), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g )
			& int.TryParse( hex.Substring( 5, 2 ), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b );

		if ( !ok )
			return null;

		return new Color( r / 255f, g / 255f, b / 255f );
	}
}
