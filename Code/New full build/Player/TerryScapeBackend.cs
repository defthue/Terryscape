using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

public static class TerryScapeBackend
{
	public struct LoadResult
	{
		public bool Success;
		public PlayerSaveData Save;
	}

	const int LoadMaxAttempts = 4;
	const int SaveMaxAttempts = 2;

	static readonly int[] BackoffDelaysMs = { 500, 1000, 2000, 4000 };

	static async Task<JsonElement?> CallEndpointWithRetry( string endpoint, object payload, int maxAttempts )
	{
		Exception lastException = null;

		for ( int attempt = 0; attempt < maxAttempts; attempt++ )
		{
			try
			{
				JsonElement? result;
				if ( payload == null )
					result = await NetworkStorage.CallEndpoint( endpoint );
				else
					result = await NetworkStorage.CallEndpoint( endpoint, payload );

				if ( result.HasValue )
					return result;

				Log.Warning( $"[TerryScapeBackend] {endpoint} returned no value (attempt {attempt + 1}/{maxAttempts})." );
			}
			catch ( Exception ex )
			{
				lastException = ex;
				Log.Warning( $"[TerryScapeBackend] {endpoint} threw on attempt {attempt + 1}/{maxAttempts}: {ex.Message}" );
			}

			if ( attempt < maxAttempts - 1 )
			{
				int delayMs = BackoffDelaysMs[Math.Min( attempt, BackoffDelaysMs.Length - 1 )];
				await Task.Delay( delayMs );
			}
		}

		if ( lastException != null )
			Log.Warning( $"[TerryScapeBackend] {endpoint} failed after {maxAttempts} attempts. Last error: {lastException.Message}" );
		else
			Log.Warning( $"[TerryScapeBackend] {endpoint} failed after {maxAttempts} attempts (all returned no value)." );

		return null;
	}

	public static async Task<LoadResult> LoadAsync()
	{
		NetworkStorageConfig.EnsureInitialized();

		try
		{
			var result = await CallEndpointWithRetry( "load-player", null, LoadMaxAttempts );
			if ( !result.HasValue )
			{
				Log.Warning( "[TerryScapeBackend] load-player returned no value after retries — treating as failure to avoid overwriting real save with empty state." );
				return new LoadResult { Success = false, Save = null };
			}

			var json = result.Value;

			if ( !json.TryGetProperty( "version", out var versionProp ) ||
				versionProp.ValueKind == JsonValueKind.Null ||
				versionProp.ValueKind == JsonValueKind.Undefined )
			{
				Log.Info( "[TerryScapeBackend] No existing save for this player." );
				return new LoadResult { Success = true, Save = null };
			}

			var save = new PlayerSaveData
			{
				Version = json.Int( "version", 1 ),
				SavedAt = json.Str( "savedAt", "" ),
				PlayerName = json.Str( "playerName", "" ),
				EquippedAmmoId = json.Str( "equippedAmmoId", "None" ),
				EquippedAmmoQty = json.Int( "equippedAmmoQty", 0 ),
				NodesMined = json.Int( "nodesMined", 0 ),
				TotalLevel = json.Int( "totalLevel", 0 ),
				TotalGold = json.Int( "totalGold", 0 ),
				TotalKills = json.Int( "totalKills", 0 )
			};

			if ( json.TryGetProperty( "skills", out var skillsEl ) && skillsEl.ValueKind == JsonValueKind.Object )
			{
				foreach ( var prop in skillsEl.EnumerateObject() )
				{
					var entry = new PlayerSaveData.SkillEntry
					{
						Level = prop.Value.Int( "level", 1 ),
						Xp = prop.Value.Int( "xp", 0 )
					};
					save.Skills[prop.Name] = entry;
				}
			}

			if ( json.TryGetProperty( "stackables", out var stackEl ) && stackEl.ValueKind == JsonValueKind.Object )
			{
				foreach ( var prop in stackEl.EnumerateObject() )
				{
					save.Stackables[prop.Name] = prop.Value.ValueKind == JsonValueKind.Number
						? prop.Value.GetInt32()
						: 0;
				}
			}

			if ( json.TryGetProperty( "uniqueItems", out var uniqueEl ) && uniqueEl.ValueKind == JsonValueKind.Array )
			{
				foreach ( var item in uniqueEl.EnumerateArray() )
				{
					save.UniqueItems.Add( new PlayerSaveData.UniqueItemEntry
					{
						ItemId = item.Str( "itemId", "None" ),
						Enchantment = item.Str( "enchantment", "None" ),
						EnchantmentPercent = item.Float( "percent", 0f )
					} );
				}
			}

			if ( json.TryGetProperty( "equipped", out var equipEl ) && equipEl.ValueKind == JsonValueKind.Object )
			{
				foreach ( var prop in equipEl.EnumerateObject() )
				{
					save.Equipped[prop.Name] = new PlayerSaveData.UniqueItemEntry
					{
						ItemId = prop.Value.Str( "itemId", "None" ),
						Enchantment = prop.Value.Str( "enchantment", "None" ),
						EnchantmentPercent = prop.Value.Float( "percent", 0f )
					};
				}
			}

			if ( json.TryGetProperty( "recipes", out var recipesEl ) && recipesEl.ValueKind == JsonValueKind.Array )
			{
				foreach ( var r in recipesEl.EnumerateArray() )
					if ( r.ValueKind == JsonValueKind.String ) save.Recipes.Add( r.GetString() );
			}

			if ( json.TryGetProperty( "stones", out var stonesEl ) && stonesEl.ValueKind == JsonValueKind.Array )
			{
				foreach ( var s in stonesEl.EnumerateArray() )
					if ( s.ValueKind == JsonValueKind.String ) save.Stones.Add( s.GetString() );
			}

			if ( json.TryGetProperty( "quests", out var questsEl ) && questsEl.ValueKind == JsonValueKind.Array )
			{
				foreach ( var q in questsEl.EnumerateArray() )
					if ( q.ValueKind == JsonValueKind.String ) save.Quests.Add( q.GetString() );
			}

			if ( json.TryGetProperty( "discoveredQuests", out var discEl ) && discEl.ValueKind == JsonValueKind.Array )
			{
				foreach ( var q in discEl.EnumerateArray() )
					if ( q.ValueKind == JsonValueKind.String ) save.DiscoveredQuests.Add( q.GetString() );
			}

			if ( json.TryGetProperty( "kills", out var killsEl ) && killsEl.ValueKind == JsonValueKind.Object )
			{
				foreach ( var prop in killsEl.EnumerateObject() )
				{
					save.Kills[prop.Name] = prop.Value.ValueKind == JsonValueKind.Number
						? prop.Value.GetInt32()
						: 0;
				}
			}

			if ( json.TryGetProperty( "bank", out var bankEl ) && bankEl.ValueKind == JsonValueKind.Object )
			{
				foreach ( var prop in bankEl.EnumerateObject() )
				{
					save.Bank[prop.Name] = prop.Value.ValueKind == JsonValueKind.Number
						? prop.Value.GetInt32()
						: 0;
				}
			}

			if ( json.TryGetProperty( "bankUnique", out var bankUniqueEl ) && bankUniqueEl.ValueKind == JsonValueKind.Array )
			{
				foreach ( var item in bankUniqueEl.EnumerateArray() )
				{
					save.BankUnique.Add( new PlayerSaveData.UniqueItemEntry
					{
						ItemId = item.Str( "itemId", "None" ),
						Enchantment = item.Str( "enchantment", "None" ),
						EnchantmentPercent = item.Float( "percent", 0f )
					} );
				}
			}

			if ( json.TryGetProperty( "chestClaims", out var chestEl ) && chestEl.ValueKind == JsonValueKind.Object )
			{
				foreach ( var prop in chestEl.EnumerateObject() )
				{
					if ( prop.Value.ValueKind == JsonValueKind.String )
						save.ChestClaims[prop.Name] = prop.Value.GetString();
				}
			}

			Log.Info( $"[TerryScapeBackend] Loaded save from {save.SavedAt}." );
			return new LoadResult { Success = true, Save = save };
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[TerryScapeBackend] LoadAsync failed: {ex.Message}" );
			return new LoadResult { Success = false, Save = null };
		}
	}

	public static async Task<bool> SaveAsync( PlayerSaveData data )
	{
		if ( data == null )
			return false;

		NetworkStorageConfig.EnsureInitialized();

		try
		{
			data.SavedAt = DateTime.UtcNow.ToString( "o" );

			var payload = new
			{
				version = data.Version,
				savedAt = data.SavedAt,
				playerName = data.PlayerName ?? "",
				skills = BuildSkillsPayload( data.Skills ),
				stackables = data.Stackables,
				uniqueItems = BuildUniqueItemsPayload( data.UniqueItems ),
				equipped = BuildEquippedPayload( data.Equipped ),
				equippedAmmoId = data.EquippedAmmoId ?? "None",
				equippedAmmoQty = data.EquippedAmmoQty,
				recipes = data.Recipes,
				stones = data.Stones,
				quests = data.Quests,
				discoveredQuests = data.DiscoveredQuests ?? new List<string>(),
				kills = data.Kills,
				bank = data.Bank ?? new Dictionary<string, int>(),
				bankUnique = BuildUniqueItemsPayload( data.BankUnique ),
				nodesMined = data.NodesMined,
				totalLevel = data.TotalLevel,
				totalGold = data.TotalGold,
				totalKills = data.TotalKills,
				chestClaims = data.ChestClaims ?? new Dictionary<string, string>()
			};

			var result = await CallEndpointWithRetry( "save-player", payload, SaveMaxAttempts );
			if ( !result.HasValue )
			{
				Log.Warning( "[TerryScapeBackend] save-player returned no value after retries." );
				return false;
			}

			return true;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[TerryScapeBackend] SaveAsync failed: {ex.Message}" );
			return false;
		}
	}

	static Dictionary<string, object> BuildSkillsPayload( Dictionary<string, PlayerSaveData.SkillEntry> skills )
	{
		var result = new Dictionary<string, object>();
		foreach ( var kv in skills )
		{
			result[kv.Key] = new { level = kv.Value.Level, xp = kv.Value.Xp };
		}
		return result;
	}

	static List<object> BuildUniqueItemsPayload( List<PlayerSaveData.UniqueItemEntry> items )
	{
		var result = new List<object>();
		if ( items == null )
			return result;

		foreach ( var item in items )
		{
			result.Add( new
			{
				itemId = item.ItemId ?? "None",
				enchantment = item.Enchantment ?? "None",
				percent = item.EnchantmentPercent
			} );
		}
		return result;
	}

	static Dictionary<string, object> BuildEquippedPayload( Dictionary<string, PlayerSaveData.UniqueItemEntry> equipped )
	{
		var result = new Dictionary<string, object>();
		foreach ( var kv in equipped )
		{
			result[kv.Key] = new
			{
				itemId = kv.Value.ItemId ?? "None",
				enchantment = kv.Value.Enchantment ?? "None",
				percent = kv.Value.EnchantmentPercent
			};
		}
		return result;
	}
}