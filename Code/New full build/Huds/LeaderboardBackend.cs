using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

public static class LeaderboardBackend
{
	public class Entry
	{
		public string PlayerName { get; set; } = "";
		public int TotalLevel { get; set; }
		public int TotalGold { get; set; }
		public int NodesMined { get; set; }
		public int TotalKills { get; set; }
	}

	public static async Task<List<Entry>> FetchAllAsync()
	{
		var entries = new List<Entry>();

		NetworkStorageConfig.EnsureInitialized();

		try
		{
			var result = await NetworkStorage.CallEndpoint( "leaderboard" );
			if ( !result.HasValue )
			{
				Log.Warning( "[LeaderboardBackend] leaderboard endpoint returned no value." );
				return entries;
			}

			var json = result.Value;

			if ( !json.TryGetProperty( "entriesByPlayer", out var byPlayerEl ) )
			{
				Log.Warning( "[LeaderboardBackend] response missing 'entriesByPlayer'." );
				return entries;
			}

			var byPlayer = UnwrapToObject( byPlayerEl );
			if ( !byPlayer.HasValue )
			{
				Log.Warning( "[LeaderboardBackend] 'entriesByPlayer' is not an object." );
				return entries;
			}

			foreach ( var prop in byPlayer.Value.EnumerateObject() )
			{
				if ( prop.Value.ValueKind != JsonValueKind.Object )
					continue;

				var entry = new Entry
				{
					PlayerName = prop.Value.Str( "playerName", "Unknown" ),
					TotalLevel = prop.Value.Int( "totalLevel", 0 ),
					TotalGold = prop.Value.Int( "totalGold", 0 ),
					NodesMined = prop.Value.Int( "nodesMined", 0 ),
					TotalKills = prop.Value.Int( "totalKills", 0 )
				};

				if ( string.IsNullOrEmpty( entry.PlayerName ) )
					continue;

				entries.Add( entry );
			}

			Log.Info( $"[LeaderboardBackend] Fetched {entries.Count} player records." );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[LeaderboardBackend] FetchAllAsync failed: {ex.Message}" );
		}

		return entries;
	}

	static JsonElement? UnwrapToObject( JsonElement el )
	{
		if ( el.ValueKind == JsonValueKind.Object )
			return el;

		if ( el.ValueKind == JsonValueKind.String )
		{
			var raw = el.GetString();
			if ( string.IsNullOrEmpty( raw ) )
				return null;

			try
			{
				var parsed = JsonDocument.Parse( raw ).RootElement;
				if ( parsed.ValueKind == JsonValueKind.Object )
					return parsed;
			}
			catch { }
		}

		return null;
	}
}