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
				EquippedAmmoSlotIndex = json.Int( "equippedAmmoSlotIndex", 0 ),
				InventoryExpansions = json.Int( "inventoryExpansions", 0 ),
				NodesMined = json.Int( "nodesMined", 0 ),
				TotalLevel = json.Int( "totalLevel", 0 ),
				TotalGold = json.Int( "totalGold", 0 ),
				TotalKills = json.Int( "totalKills", 0 ),
				CurrentMana = json.Int( "currentMana", -1 )
			};

			ParseSkillsAny( save, json, "skills" );
			ParseDictIntAny( save.Stackables, json, "stackables" );
			ParseUniqueItemArrayAny( save.UniqueItems, json, "uniqueItems" );
			ParseEquippedAny( save, json, "equipped" );
			ParseDictIntAny( save.EquippedSlotIndices, json, "equippedSlotIndices" );
			ParseSlotsAny( save, json, "slots" );
			ParseStringArrayAny( save.Recipes, json, "recipes" );
			ParseStringArrayAny( save.Stones, json, "stones" );
			ParseStringArrayAny( save.Quests, json, "quests" );
			ParseStringArrayAny( save.DiscoveredQuests, json, "discoveredQuests" );
			ParseDictIntAny( save.Kills, json, "kills" );
			ParseDictIntAny( save.Bank, json, "bank" );
			ParseUniqueItemArrayAny( save.BankUnique, json, "bankUnique" );
			ParseDictStringAny( save.ChestClaims, json, "chestClaims" );
			ParseStringArrayAny( save.UnlockedSpells, json, "unlockedSpells" );

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
				skills = JsonSerializer.Serialize( BuildSkillsPayload( data.Skills ) ),
				stackables = JsonSerializer.Serialize( data.Stackables ?? new Dictionary<string, int>() ),
				uniqueItems = JsonSerializer.Serialize( BuildUniqueItemsPayload( data.UniqueItems ) ),
				equipped = JsonSerializer.Serialize( BuildEquippedPayload( data.Equipped ) ),
				equippedSlotIndices = JsonSerializer.Serialize( data.EquippedSlotIndices ?? new Dictionary<string, int>() ),
				equippedAmmoId = data.EquippedAmmoId ?? "None",
				equippedAmmoQty = data.EquippedAmmoQty,
				equippedAmmoSlotIndex = data.EquippedAmmoSlotIndex,
				slots = JsonSerializer.Serialize( BuildSlotsPayload( data.Slots ) ),
				inventoryExpansions = data.InventoryExpansions,
				recipes = JsonSerializer.Serialize( data.Recipes ?? new List<string>() ),
				stones = JsonSerializer.Serialize( data.Stones ?? new List<string>() ),
				quests = JsonSerializer.Serialize( data.Quests ?? new List<string>() ),
				discoveredQuests = JsonSerializer.Serialize( data.DiscoveredQuests ?? new List<string>() ),
				kills = JsonSerializer.Serialize( data.Kills ?? new Dictionary<string, int>() ),
				bank = JsonSerializer.Serialize( data.Bank ?? new Dictionary<string, int>() ),
				bankUnique = JsonSerializer.Serialize( BuildUniqueItemsPayload( data.BankUnique ) ),
				nodesMined = data.NodesMined,
				totalLevel = data.TotalLevel,
				totalGold = data.TotalGold,
				totalKills = data.TotalKills,
				chestClaims = JsonSerializer.Serialize( data.ChestClaims ?? new Dictionary<string, string>() ),
				currentMana = data.CurrentMana,
				unlockedSpells = JsonSerializer.Serialize( data.UnlockedSpells ?? new List<string>() )
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

	static JsonElement? UnwrapAny( JsonElement parent, string propName )
	{
		if ( !parent.TryGetProperty( propName, out var el ) )
			return null;

		if ( el.ValueKind == JsonValueKind.Object || el.ValueKind == JsonValueKind.Array )
			return el;

		if ( el.ValueKind == JsonValueKind.String )
		{
			var raw = el.GetString();
			if ( string.IsNullOrEmpty( raw ) )
				return null;

			try
			{
				return JsonDocument.Parse( raw ).RootElement;
			}
			catch
			{
				return null;
			}
		}

		return null;
	}

	static void ParseSkillsAny( PlayerSaveData save, JsonElement parent, string propName )
	{
		var el = UnwrapAny( parent, propName );
		if ( !el.HasValue || el.Value.ValueKind != JsonValueKind.Object )
			return;

		foreach ( var prop in el.Value.EnumerateObject() )
		{
			save.Skills[prop.Name] = new PlayerSaveData.SkillEntry
			{
				Level = prop.Value.Int( "level", 1 ),
				Xp = prop.Value.Int( "xp", 0 )
			};
		}
	}

	static void ParseDictIntAny( Dictionary<string, int> target, JsonElement parent, string propName )
	{
		if ( target == null )
			return;

		var el = UnwrapAny( parent, propName );
		if ( !el.HasValue || el.Value.ValueKind != JsonValueKind.Object )
			return;

		foreach ( var prop in el.Value.EnumerateObject() )
		{
			if ( prop.Value.ValueKind == JsonValueKind.Number )
				target[prop.Name] = prop.Value.GetInt32();
		}
	}

	static void ParseDictStringAny( Dictionary<string, string> target, JsonElement parent, string propName )
	{
		if ( target == null )
			return;

		var el = UnwrapAny( parent, propName );
		if ( !el.HasValue || el.Value.ValueKind != JsonValueKind.Object )
			return;

		foreach ( var prop in el.Value.EnumerateObject() )
		{
			if ( prop.Value.ValueKind == JsonValueKind.String )
				target[prop.Name] = prop.Value.GetString();
		}
	}

	static void ParseStringArrayAny( List<string> target, JsonElement parent, string propName )
	{
		if ( target == null )
			return;

		var el = UnwrapAny( parent, propName );
		if ( !el.HasValue || el.Value.ValueKind != JsonValueKind.Array )
			return;

		foreach ( var item in el.Value.EnumerateArray() )
		{
			if ( item.ValueKind == JsonValueKind.String )
				target.Add( item.GetString() );
		}
	}

	static void ParseUniqueItemArrayAny( List<PlayerSaveData.UniqueItemEntry> target, JsonElement parent, string propName )
	{
		if ( target == null )
			return;

		var el = UnwrapAny( parent, propName );
		if ( !el.HasValue || el.Value.ValueKind != JsonValueKind.Array )
			return;

		foreach ( var item in el.Value.EnumerateArray() )
		{
			target.Add( new PlayerSaveData.UniqueItemEntry
			{
				ItemId = item.Str( "itemId", "None" ),
				Enchantment = item.Str( "enchantment", "None" ),
				EnchantmentPercent = item.Float( "percent", 0f ),
				Socket1ItemId = item.Str( "socket1Id", "None" ),
				Socket1Enchantment = item.Str( "socket1Enchantment", "None" ),
				Socket1Percent = item.Float( "socket1Percent", 0f ),
				Socket2ItemId = item.Str( "socket2Id", "None" ),
				Socket2Enchantment = item.Str( "socket2Enchantment", "None" ),
				Socket2Percent = item.Float( "socket2Percent", 0f )
			} );
		}
	}

	static void ParseEquippedAny( PlayerSaveData save, JsonElement parent, string propName )
	{
		var el = UnwrapAny( parent, propName );
		if ( !el.HasValue || el.Value.ValueKind != JsonValueKind.Object )
			return;

		foreach ( var prop in el.Value.EnumerateObject() )
		{
			save.Equipped[prop.Name] = new PlayerSaveData.UniqueItemEntry
			{
				ItemId = prop.Value.Str( "itemId", "None" ),
				Enchantment = prop.Value.Str( "enchantment", "None" ),
				EnchantmentPercent = prop.Value.Float( "percent", 0f ),
				Socket1ItemId = prop.Value.Str( "socket1Id", "None" ),
				Socket1Enchantment = prop.Value.Str( "socket1Enchantment", "None" ),
				Socket1Percent = prop.Value.Float( "socket1Percent", 0f ),
				Socket2ItemId = prop.Value.Str( "socket2Id", "None" ),
				Socket2Enchantment = prop.Value.Str( "socket2Enchantment", "None" ),
				Socket2Percent = prop.Value.Float( "socket2Percent", 0f )
			};
		}
	}

	static void ParseSlotsAny( PlayerSaveData save, JsonElement parent, string propName )
	{
		var el = UnwrapAny( parent, propName );
		if ( !el.HasValue || el.Value.ValueKind != JsonValueKind.Array )
			return;

		foreach ( var entry in el.Value.EnumerateArray() )
		{
			var slotEntry = new PlayerSaveData.InventorySlotEntry
			{
				Slot = entry.Int( "slot", 0 ),
				ItemId = entry.Str( "itemId", "None" ),
				Count = entry.Int( "count", 0 ),
				Enchantment = entry.Str( "enchantment", "None" ),
				EnchantmentPercent = entry.Float( "percent", 0f ),
				Socket1ItemId = entry.Str( "socket1Id", "None" ),
				Socket1Enchantment = entry.Str( "socket1Enchantment", "None" ),
				Socket1Percent = entry.Float( "socket1Percent", 0f ),
				Socket2ItemId = entry.Str( "socket2Id", "None" ),
				Socket2Enchantment = entry.Str( "socket2Enchantment", "None" ),
				Socket2Percent = entry.Float( "socket2Percent", 0f )
			};

			if ( entry.TryGetProperty( "isUnique", out var uProp ) )
			{
				if ( uProp.ValueKind == JsonValueKind.True )
					slotEntry.IsUnique = true;
				else if ( uProp.ValueKind == JsonValueKind.False )
					slotEntry.IsUnique = false;
			}

			save.Slots.Add( slotEntry );
		}
	}

	static Dictionary<string, object> BuildSkillsPayload( Dictionary<string, PlayerSaveData.SkillEntry> skills )
	{
		var result = new Dictionary<string, object>();
		if ( skills == null )
			return result;

		foreach ( var kv in skills )
			result[kv.Key] = new { level = kv.Value.Level, xp = kv.Value.Xp };

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
				percent = item.EnchantmentPercent,
				socket1Id = item.Socket1ItemId ?? "None",
				socket1Enchantment = item.Socket1Enchantment ?? "None",
				socket1Percent = item.Socket1Percent,
				socket2Id = item.Socket2ItemId ?? "None",
				socket2Enchantment = item.Socket2Enchantment ?? "None",
				socket2Percent = item.Socket2Percent
			} );
		}
		return result;
	}

	static Dictionary<string, object> BuildEquippedPayload( Dictionary<string, PlayerSaveData.UniqueItemEntry> equipped )
	{
		var result = new Dictionary<string, object>();
		if ( equipped == null )
			return result;

		foreach ( var kv in equipped )
		{
			result[kv.Key] = new
			{
				itemId = kv.Value.ItemId ?? "None",
				enchantment = kv.Value.Enchantment ?? "None",
				percent = kv.Value.EnchantmentPercent,
				socket1Id = kv.Value.Socket1ItemId ?? "None",
				socket1Enchantment = kv.Value.Socket1Enchantment ?? "None",
				socket1Percent = kv.Value.Socket1Percent,
				socket2Id = kv.Value.Socket2ItemId ?? "None",
				socket2Enchantment = kv.Value.Socket2Enchantment ?? "None",
				socket2Percent = kv.Value.Socket2Percent
			};
		}
		return result;
	}

	static List<object> BuildSlotsPayload( List<PlayerSaveData.InventorySlotEntry> slots )
	{
		var result = new List<object>();
		if ( slots == null )
			return result;

		foreach ( var entry in slots )
		{
			result.Add( new
			{
				slot = entry.Slot,
				itemId = entry.ItemId ?? "None",
				count = entry.Count,
				isUnique = entry.IsUnique,
				enchantment = entry.Enchantment ?? "None",
				percent = entry.EnchantmentPercent,
				socket1Id = entry.Socket1ItemId ?? "None",
				socket1Enchantment = entry.Socket1Enchantment ?? "None",
				socket1Percent = entry.Socket1Percent,
				socket2Id = entry.Socket2ItemId ?? "None",
				socket2Enchantment = entry.Socket2Enchantment ?? "None",
				socket2Percent = entry.Socket2Percent
			} );
		}
		return result;
	}
}