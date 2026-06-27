using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

public static class LeaderboardBackend
{
	const string LevelQueryId = "query_f0090b98877d";
	const string GoldQueryId = "query_0d002d9aac54";
	const string KillsQueryId = "query_f5e3a2aff202";
	const string NodesQueryId = "query_1af8b4451c8b";

	static bool _loggedRawOnce;

	public class Entry
	{
		public string SteamId { get; set; } = "";
		public string PlayerName { get; set; } = "";
		public int TotalLevel { get; set; }
		public int TotalGold { get; set; }
		public int NodesMined { get; set; }
		public int TotalKills { get; set; }
	}

	public static async Task<List<Entry>> FetchAllAsync()
	{
		var map = new Dictionary<string, Entry>();

		NetworkStorageConfig.EnsureInitialized();

		await RunBoardAsync( LevelQueryId, ( e, v ) => e.TotalLevel = v, map, !_loggedRawOnce );
		await RunBoardAsync( GoldQueryId, ( e, v ) => e.TotalGold = v, map, false );
		await RunBoardAsync( KillsQueryId, ( e, v ) => e.TotalKills = v, map, false );
		await RunBoardAsync( NodesQueryId, ( e, v ) => e.NodesMined = v, map, false );

		_loggedRawOnce = true;

		var entries = new List<Entry>( map.Count );
		foreach ( var entry in map.Values )
		{
			if ( string.IsNullOrEmpty( entry.PlayerName ) )
				entry.PlayerName = entry.SteamId;

			entries.Add( entry );
		}

		Log.Info( $"[LeaderboardBackend] Merged {entries.Count} players across 4 queries." );
		return entries;
	}

	static async Task RunBoardAsync( string queryId, Action<Entry, int> apply, Dictionary<string, Entry> map, bool logRaw )
	{
		try
		{
			var result = await NetworkStorage.RunQuery( queryId );
			if ( !result.HasValue )
			{
				Log.Warning( $"[LeaderboardBackend] query {queryId} returned no value." );
				return;
			}

			var root = result.Value;

			if ( logRaw )
			{
				var raw = root.GetRawText();
				if ( raw.Length > 4000 )
					raw = raw.Substring( 0, 4000 ) + "…";
				Log.Info( $"[LeaderboardBackend] raw {queryId}: {raw}" );
			}

			if ( !root.TryGetProperty( "entries", out var entriesEl ) || entriesEl.ValueKind != JsonValueKind.Array )
			{
				Log.Warning( $"[LeaderboardBackend] query {queryId} missing 'entries' array." );
				return;
			}

			foreach ( var item in entriesEl.EnumerateArray() )
			{
				if ( item.ValueKind != JsonValueKind.Object )
					continue;

				if ( !item.TryGetProperty( "key", out var keyEl ) || keyEl.ValueKind != JsonValueKind.String )
					continue;

				var steamId = keyEl.GetString();
				if ( string.IsNullOrEmpty( steamId ) )
					continue;

				if ( !map.TryGetValue( steamId, out var entry ) )
				{
					entry = new Entry { SteamId = steamId };
					map[steamId] = entry;
				}

				if ( item.TryGetProperty( "value", out var valueEl ) )
					apply( entry, ReadInt( valueEl ) );

				var name = ReadName( item );
				if ( !string.IsNullOrEmpty( name ) )
					entry.PlayerName = name;
			}
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[LeaderboardBackend] query {queryId} failed: {ex.Message}" );
		}
	}

	static int ReadInt( JsonElement el )
	{
		switch ( el.ValueKind )
		{
			case JsonValueKind.Number:
				return (int)el.GetDouble();
			case JsonValueKind.String:
				return int.TryParse( el.GetString(), out var v ) ? v : 0;
			default:
				return 0;
		}
	}

	static string ReadName( JsonElement entry )
	{
		if ( entry.TryGetProperty( "playerName", out var pn ) )
		{
			if ( pn.ValueKind == JsonValueKind.String )
				return pn.GetString();

			if ( pn.ValueKind == JsonValueKind.Object && pn.TryGetProperty( "default", out var nested ) && nested.ValueKind == JsonValueKind.String )
				return nested.GetString();
		}

		if ( entry.TryGetProperty( "playerName.default", out var flat ) && flat.ValueKind == JsonValueKind.String )
			return flat.GetString();

		if ( entry.TryGetProperty( "name", out var nm ) && nm.ValueKind == JsonValueKind.String )
			return nm.GetString();

		return null;
	}
}