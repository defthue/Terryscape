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
				Log.Warning( "[TerryScapeBackend] load-player returned no value after retries — saves are blocked for this session." );
				return new LoadResult { Success = false, Save = null };
			}

			var json = result.Value;

			var players = ExtractObject( json, "players" );

			if ( !players.HasValue || !HasMeaningfulFields( players.Value ) )
			{
				Log.Info( "[TerryScapeBackend] No existing save for this player." );
				return new LoadResult { Success = true, Save = null };
			}

			var save = new PlayerSaveData
			{
				Version = players.Value.Int( "version", 1 ),
				SavedAt = players.Value.Str( "savedAt", "" ),
				PlayerName = players.Value.Str( "playerName", "" ),
				EquippedAmmoId = players.Value.Str( "equippedAmmoId", "None" ),
				EquippedAmmoQty = players.Value.Int( "equippedAmmoQty", 0 ),
				EquippedAmmoSlotIndex = players.Value.Int( "equippedAmmoSlotIndex", 0 ),
				InventoryExpansions = players.Value.Int( "inventoryExpansions", 0 ),
				NodesMined = players.Value.Int( "nodesMined", 0 ),
				TotalLevel = players.Value.Int( "totalLevel", 0 ),
				TotalGold = players.Value.Int( "totalGold", 0 ),
				TotalKills = players.Value.Int( "totalKills", 0 ),
				CurrentMana = players.Value.Int( "currentMana", -1 )
			};

			var skills = ExtractObject( json, "skills" );
			if ( skills.HasValue )
				ParseSkillsFromCollection( save, skills.Value );

			var inventory = ExtractObject( json, "inventory" );
			if ( inventory.HasValue )
				ParseSlotsFromCollection( save, inventory.Value );

			var equipment = ExtractObject( json, "equipment" );
			if ( equipment.HasValue )
			{
				ParseEquippedFromCollection( save, equipment.Value );
				ParseEquippedSlotIndicesFromCollection( save, equipment.Value );
			}

			var bank = ExtractObject( json, "bank" );
			if ( bank.HasValue )
				ParseBankStackablesFromCollection( save, bank.Value );

			var bankUnique = ExtractObject( json, "bank_unique" );
			if ( bankUnique.HasValue )
				ParseBankUniqueFromCollection( save, bankUnique.Value );

			var progression = ExtractObject( json, "progression" );
			if ( progression.HasValue )
				ParseProgressionFromCollection( save, progression.Value );

			var kills = ExtractObject( json, "kills" );
			if ( kills.HasValue )
				ParseKillsFromCollection( save, kills.Value );

			Log.Info( $"[TerryScapeBackend] Loaded save from {save.SavedAt}." );
			return new LoadResult { Success = true, Save = save };
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[TerryScapeBackend] LoadAsync failed: {ex.Message}" );
			return new LoadResult { Success = false, Save = null };
		}
	}

	public static async Task<bool> SaveAllAsync( PlayerSaveData data )
	{
		if ( data == null )
			return false;

		NetworkStorageConfig.EnsureInitialized();

		try
		{
			data.SavedAt = DateTime.UtcNow.ToString( "o" );

			var payload = new
			{
				savedAt = data.SavedAt,
				playerName = data.PlayerName ?? "",
				currency = 0,
				currentMana = data.CurrentMana,
				totalLevel = data.TotalLevel,
				totalGold = data.TotalGold,
				totalKills = data.TotalKills,
				nodesMined = data.NodesMined,
				inventoryExpansions = data.InventoryExpansions,
				equippedAmmoId = data.EquippedAmmoId ?? "None",
				equippedAmmoQty = data.EquippedAmmoQty,
				equippedAmmoSlotIndex = data.EquippedAmmoSlotIndex,
				skills = BuildSkillsPayload( data.Skills ),
				slots = BuildSlotsPayload( data.Slots ),
				equipped = BuildEquippedPayload( data.Equipped ),
				equippedSlotIndices = data.EquippedSlotIndices ?? new Dictionary<string, int>(),
				bankStackables = data.Bank ?? new Dictionary<string, int>(),
				bankUnique = BuildUniqueItemsPayload( data.BankUnique ),
				recipes = data.Recipes ?? new List<string>(),
				stones = data.Stones ?? new List<string>(),
				quests = data.Quests ?? new List<string>(),
				discoveredQuests = data.DiscoveredQuests ?? new List<string>(),
				unlockedSpells = data.UnlockedSpells ?? new List<string>(),
				chestClaims = data.ChestClaims ?? new Dictionary<string, string>(),
				kills = data.Kills ?? new Dictionary<string, int>()
			};

			var result = await CallEndpointWithRetry( "save-all", payload, SaveMaxAttempts );
			if ( !result.HasValue )
			{
				Log.Warning( "[TerryScapeBackend] save-all returned no value after retries — treating as failed save." );
				return false;
			}

			return true;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[TerryScapeBackend] SaveAllAsync failed: {ex.Message}" );
			return false;
		}
	}

	public static async Task<bool> SaveStatsAsync( PlayerSaveData data )
	{
		if ( data == null )
			return false;

		NetworkStorageConfig.EnsureInitialized();

		try
		{
			data.SavedAt = DateTime.UtcNow.ToString( "o" );

			var payload = new
			{
				savedAt = data.SavedAt,
				playerName = data.PlayerName ?? "",
				currency = 0,
				currentMana = data.CurrentMana,
				totalLevel = data.TotalLevel,
				totalGold = data.TotalGold,
				totalKills = data.TotalKills,
				nodesMined = data.NodesMined,
				inventoryExpansions = data.InventoryExpansions,
				equippedAmmoId = data.EquippedAmmoId ?? "None",
				equippedAmmoQty = data.EquippedAmmoQty,
				equippedAmmoSlotIndex = data.EquippedAmmoSlotIndex
			};

			var result = await CallEndpointWithRetry( "save-stats", payload, SaveMaxAttempts );
			return result.HasValue;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[TerryScapeBackend] SaveStatsAsync failed: {ex.Message}" );
			return false;
		}
	}

	public static async Task<bool> SaveSkillsAsync( PlayerSaveData data )
	{
		if ( data == null )
			return false;

		NetworkStorageConfig.EnsureInitialized();

		try
		{
			var payload = new { entries = BuildSkillsPayload( data.Skills ) };
			var result = await CallEndpointWithRetry( "save-skills", payload, SaveMaxAttempts );
			return result.HasValue;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[TerryScapeBackend] SaveSkillsAsync failed: {ex.Message}" );
			return false;
		}
	}

	public static async Task<bool> SaveInventoryAsync( PlayerSaveData data )
	{
		if ( data == null )
			return false;

		NetworkStorageConfig.EnsureInitialized();

		try
		{
			var payload = new
			{
				slots = BuildSlotsPayload( data.Slots ),
				equipped = BuildEquippedPayload( data.Equipped ),
				equippedSlotIndices = data.EquippedSlotIndices ?? new Dictionary<string, int>()
			};

			var result = await CallEndpointWithRetry( "save-inventory", payload, SaveMaxAttempts );
			return result.HasValue;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[TerryScapeBackend] SaveInventoryAsync failed: {ex.Message}" );
			return false;
		}
	}

	public static async Task<bool> SaveBankAsync( PlayerSaveData data )
	{
		if ( data == null )
			return false;

		NetworkStorageConfig.EnsureInitialized();

		try
		{
			var payload = new
			{
				stackables = data.Bank ?? new Dictionary<string, int>(),
				items = BuildUniqueItemsPayload( data.BankUnique )
			};

			var result = await CallEndpointWithRetry( "save-bank", payload, SaveMaxAttempts );
			return result.HasValue;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[TerryScapeBackend] SaveBankAsync failed: {ex.Message}" );
			return false;
		}
	}

	public static async Task<bool> SaveProgressionAsync( PlayerSaveData data )
	{
		if ( data == null )
			return false;

		NetworkStorageConfig.EnsureInitialized();

		try
		{
			var payload = new
			{
				recipes = data.Recipes ?? new List<string>(),
				stones = data.Stones ?? new List<string>(),
				quests = data.Quests ?? new List<string>(),
				discoveredQuests = data.DiscoveredQuests ?? new List<string>(),
				unlockedSpells = data.UnlockedSpells ?? new List<string>(),
				chestClaims = data.ChestClaims ?? new Dictionary<string, string>()
			};

			var result = await CallEndpointWithRetry( "save-progression", payload, SaveMaxAttempts );
			return result.HasValue;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[TerryScapeBackend] SaveProgressionAsync failed: {ex.Message}" );
			return false;
		}
	}

	public static async Task<bool> SaveKillsAsync( PlayerSaveData data )
	{
		if ( data == null )
			return false;

		NetworkStorageConfig.EnsureInitialized();

		try
		{
			var payload = new { counts = data.Kills ?? new Dictionary<string, int>() };
			var result = await CallEndpointWithRetry( "save-kills", payload, SaveMaxAttempts );
			return result.HasValue;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[TerryScapeBackend] SaveKillsAsync failed: {ex.Message}" );
			return false;
		}
	}

	static JsonElement? ExtractObject( JsonElement parent, string propName )
	{
		if ( !parent.TryGetProperty( propName, out var el ) )
			return null;

		if ( el.ValueKind == JsonValueKind.Object )
			return el;

		if ( el.ValueKind == JsonValueKind.String )
		{
			var raw = el.GetString();
			if ( string.IsNullOrEmpty( raw ) )
				return null;

			try { return JsonDocument.Parse( raw ).RootElement; }
			catch { return null; }
		}

		return null;
	}

	static bool HasMeaningfulFields( JsonElement obj )
	{
		if ( obj.ValueKind != JsonValueKind.Object )
			return false;

		foreach ( var prop in obj.EnumerateObject() )
		{
			if ( prop.Value.ValueKind != JsonValueKind.Null && prop.Value.ValueKind != JsonValueKind.Undefined )
				return true;
		}
		return false;
	}

	static void ParseSkillsFromCollection( PlayerSaveData save, JsonElement skillsCol )
	{
		if ( !skillsCol.TryGetProperty( "entries", out var entries ) )
			return;

		if ( entries.ValueKind != JsonValueKind.Object )
			return;

		foreach ( var prop in entries.EnumerateObject() )
		{
			if ( prop.Value.ValueKind != JsonValueKind.Object )
				continue;

			save.Skills[prop.Name] = new PlayerSaveData.SkillEntry
			{
				Level = prop.Value.Int( "level", 1 ),
				Xp = prop.Value.Int( "xp", 0 )
			};
		}
	}

	static void ParseSlotsFromCollection( PlayerSaveData save, JsonElement inventoryCol )
	{
		if ( !inventoryCol.TryGetProperty( "slots", out var slots ) )
			return;

		if ( slots.ValueKind != JsonValueKind.Array )
			return;

		foreach ( var entry in slots.EnumerateArray() )
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
				Socket2Percent = entry.Float( "socket2Percent", 0f ),
				CustomName = entry.Str( "customName", "" )
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

	static void ParseEquippedFromCollection( PlayerSaveData save, JsonElement equipmentCol )
	{
		if ( !equipmentCol.TryGetProperty( "equipped", out var equipped ) )
			return;

		if ( equipped.ValueKind != JsonValueKind.Object )
			return;

		foreach ( var prop in equipped.EnumerateObject() )
		{
			if ( prop.Value.ValueKind != JsonValueKind.Object )
				continue;

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
				Socket2Percent = prop.Value.Float( "socket2Percent", 0f ),
				CustomName = prop.Value.Str( "customName", "" )
			};
		}
	}

	static void ParseEquippedSlotIndicesFromCollection( PlayerSaveData save, JsonElement equipmentCol )
	{
		if ( !equipmentCol.TryGetProperty( "equippedSlotIndices", out var indices ) )
			return;

		if ( indices.ValueKind != JsonValueKind.Object )
			return;

		foreach ( var prop in indices.EnumerateObject() )
		{
			if ( prop.Value.ValueKind == JsonValueKind.Number )
				save.EquippedSlotIndices[prop.Name] = prop.Value.GetInt32();
		}
	}

	static void ParseBankStackablesFromCollection( PlayerSaveData save, JsonElement bankCol )
	{
		if ( !bankCol.TryGetProperty( "stackables", out var stackables ) )
			return;

		if ( stackables.ValueKind != JsonValueKind.Object )
			return;

		foreach ( var prop in stackables.EnumerateObject() )
		{
			if ( prop.Value.ValueKind == JsonValueKind.Number )
				save.Bank[prop.Name] = prop.Value.GetInt32();
		}
	}

	static void ParseBankUniqueFromCollection( PlayerSaveData save, JsonElement bankUniqueCol )
	{
		if ( !bankUniqueCol.TryGetProperty( "items", out var items ) )
			return;

		if ( items.ValueKind != JsonValueKind.Array )
			return;

		foreach ( var entry in items.EnumerateArray() )
		{
			if ( entry.ValueKind != JsonValueKind.Object )
				continue;

			save.BankUnique.Add( new PlayerSaveData.UniqueItemEntry
			{
				ItemId = entry.Str( "itemId", "None" ),
				Enchantment = entry.Str( "enchantment", "None" ),
				EnchantmentPercent = entry.Float( "percent", 0f ),
				Socket1ItemId = entry.Str( "socket1Id", "None" ),
				Socket1Enchantment = entry.Str( "socket1Enchantment", "None" ),
				Socket1Percent = entry.Float( "socket1Percent", 0f ),
				Socket2ItemId = entry.Str( "socket2Id", "None" ),
				Socket2Enchantment = entry.Str( "socket2Enchantment", "None" ),
				Socket2Percent = entry.Float( "socket2Percent", 0f ),
				CustomName = entry.Str( "customName", "" )
			} );
		}
	}

	static void ParseProgressionFromCollection( PlayerSaveData save, JsonElement progressionCol )
	{
		ParseStringArrayField( save.Recipes, progressionCol, "recipes" );
		ParseStringArrayField( save.Stones, progressionCol, "stones" );
		ParseStringArrayField( save.Quests, progressionCol, "quests" );
		ParseStringArrayField( save.DiscoveredQuests, progressionCol, "discoveredQuests" );
		ParseStringArrayField( save.UnlockedSpells, progressionCol, "unlockedSpells" );

		if ( progressionCol.TryGetProperty( "chestClaims", out var claims ) && claims.ValueKind == JsonValueKind.Object )
		{
			foreach ( var prop in claims.EnumerateObject() )
			{
				if ( prop.Value.ValueKind == JsonValueKind.String )
					save.ChestClaims[prop.Name] = prop.Value.GetString();
			}
		}
	}

	static void ParseKillsFromCollection( PlayerSaveData save, JsonElement killsCol )
	{
		if ( !killsCol.TryGetProperty( "counts", out var counts ) )
			return;

		if ( counts.ValueKind != JsonValueKind.Object )
			return;

		foreach ( var prop in counts.EnumerateObject() )
		{
			if ( prop.Value.ValueKind == JsonValueKind.Number )
				save.Kills[prop.Name] = prop.Value.GetInt32();
		}
	}

	static void ParseStringArrayField( List<string> target, JsonElement parent, string propName )
	{
		if ( target == null )
			return;

		if ( !parent.TryGetProperty( propName, out var el ) )
			return;

		if ( el.ValueKind != JsonValueKind.Array )
			return;

		foreach ( var item in el.EnumerateArray() )
		{
			if ( item.ValueKind == JsonValueKind.String )
				target.Add( item.GetString() );
		}
	}

	static Dictionary<string, object> BuildSkillsPayload( Dictionary<string, PlayerSaveData.SkillEntry> skills )
	{
		var result = new Dictionary<string, object>();
		if ( skills == null )
			return result;

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
				percent = item.EnchantmentPercent,
				socket1Id = item.Socket1ItemId ?? "None",
				socket1Enchantment = item.Socket1Enchantment ?? "None",
				socket1Percent = item.Socket1Percent,
				socket2Id = item.Socket2ItemId ?? "None",
				socket2Enchantment = item.Socket2Enchantment ?? "None",
				socket2Percent = item.Socket2Percent,
				customName = item.CustomName ?? ""
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
				socket2Percent = kv.Value.Socket2Percent,
				customName = kv.Value.CustomName ?? ""
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
				socket2Percent = entry.Socket2Percent,
				customName = entry.CustomName ?? ""
			} );
		}
		return result;
	}
}