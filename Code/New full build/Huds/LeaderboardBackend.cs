using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

public static class LeaderboardBackend
{
	/// <summary>
	/// One row in the leaderboard, parsed from the player_data record. We pull only
	/// the fields needed for sorting and display — not the full save payload.
	/// </summary>
	public class Entry
	{
		public string PlayerName { get; set; } = "";
		public int TotalLevel { get; set; }
		public int TotalGold { get; set; }
		public int NodesMined { get; set; }
		public int TotalKills { get; set; }
	}

	/// <summary>
	/// Fetches every player record from the cloud and returns them as a flat list.
	/// The HUD sorts client-side by whichever stat the player is viewing.
	/// Returns an empty list on any failure.
	///
	/// IMPORTANT: We compute TotalLevel/TotalGold/TotalKills client-side from the raw
	/// skills/stackables/kills fields rather than reading the denormalized totalLevel/
	/// totalGold/totalKills fields directly. Reason: those denormalized fields were
	/// added recently — older save records don't have them. Computing client-side
	/// means old records still show accurate stats without forcing every player to
	/// re-save first.
	///
	/// NodesMined is the exception — we read it from the field directly because there's
	/// no fallback way to compute lifetime nodes mined from existing fields. Old records
	/// will show 0 nodes mined, which is correct behavior since we only just started
	/// tracking it.
	/// </summary>
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

			if ( !json.TryGetProperty( "players", out var playersEl ) || playersEl.ValueKind != JsonValueKind.Array )
			{
				Log.Warning( "[LeaderboardBackend] response missing 'players' array." );
				return entries;
			}

			foreach ( var record in playersEl.EnumerateArray() )
			{
				var entry = new Entry
				{
					PlayerName = record.Str( "playerName", "Unknown" ),
					TotalLevel = ComputeTotalLevel( record ),
					TotalGold = ComputeTotalGold( record ),
					NodesMined = record.Int( "nodesMined", 0 ),
					TotalKills = ComputeTotalKills( record )
				};

				// Skip records with empty player names — these are usually stale or
				// half-initialized rows that would clutter the leaderboard.
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

	// Sums all skill levels from the "skills" object in a player record.
	// Each skill is an object with "level" and "xp" properties.
	static int ComputeTotalLevel( JsonElement record )
	{
		if ( !record.TryGetProperty( "skills", out var skillsEl ) || skillsEl.ValueKind != JsonValueKind.Object )
			return 0;

		int total = 0;
		foreach ( var skill in skillsEl.EnumerateObject() )
		{
			if ( skill.Value.ValueKind != JsonValueKind.Object )
				continue;

			if ( skill.Value.TryGetProperty( "level", out var levelEl ) && levelEl.ValueKind == JsonValueKind.Number )
			{
				total += levelEl.GetInt32();
			}
		}
		return total;
	}

	// Reads the GoldCoin entry from the "stackables" map in a player record.
	// Doesn't include gold in the bank — that's a deliberate choice; "gold" on the
	// leaderboard tracks gold in your pocket.
	static int ComputeTotalGold( JsonElement record )
	{
		if ( !record.TryGetProperty( "stackables", out var stackablesEl ) || stackablesEl.ValueKind != JsonValueKind.Object )
			return 0;

		if ( stackablesEl.TryGetProperty( "GoldCoin", out var goldEl ) && goldEl.ValueKind == JsonValueKind.Number )
		{
			return goldEl.GetInt32();
		}

		return 0;
	}

	// Sums all kill counts across every monster type from the "kills" object.
	static int ComputeTotalKills( JsonElement record )
	{
		if ( !record.TryGetProperty( "kills", out var killsEl ) || killsEl.ValueKind != JsonValueKind.Object )
			return 0;

		int total = 0;
		foreach ( var kill in killsEl.EnumerateObject() )
		{
			if ( kill.Value.ValueKind == JsonValueKind.Number )
			{
				total += kill.Value.GetInt32();
			}
		}
		return total;
	}
}