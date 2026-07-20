using System.Text;

public static class NameFilter
{
	static readonly string[] Blocked =
	{
		"nigger",
		"nigga",
	};

	public static bool IsAllowed( string name )
	{
		if ( string.IsNullOrWhiteSpace( name ) )
			return false;

		string normalized = Normalize( name );

		foreach ( var word in Blocked )
		{
			if ( normalized.Contains( word ) )
				return false;
		}

		return true;
	}

	static string Normalize( string input )
	{
		var sb = new StringBuilder( input.Length );

		foreach ( var raw in input.ToLowerInvariant() )
		{
			char c = raw;
			switch ( c )
			{
				case '1': case '!': case '|': c = 'i'; break;
				case '3': c = 'e'; break;
				case '4': case '@': c = 'a'; break;
				case '0': c = 'o'; break;
				case '5': case '$': c = 's'; break;
				case '7': c = 't'; break;
			}

			if ( c >= 'a' && c <= 'z' )
				sb.Append( c );
		}

		return sb.ToString();
	}
}
