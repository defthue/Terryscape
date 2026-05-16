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

			if ( json.TryGetProperty( "uniqueItems", out var uniqueEl ) )
			{
				JsonElement? uniqueArr = null;
				if ( uniqueEl.ValueKind == JsonValueKind.Array )
				{
					uniqueArr = uniqueEl;
				}
				else if ( uniqueEl.ValueKind == JsonValueKind.String )
				{
					var raw = uniqueEl.GetString();
					if ( !string.IsNullOrEmpty( raw ) )
					{
						try { uniqueArr = JsonDocument.Parse( raw ).RootElement; }
						catch { }
					}
				}

				if ( uniqueArr.HasValue && uniqueArr.Value.ValueKind == JsonValueKind.Array )
				{
					foreach ( var item in uniqueArr.Value.EnumerateArray() )
					{
						save.UniqueItems.Add( new PlayerSaveData.UniqueItemEntry
						{
							ItemId = item.Str( "itemId", "None" ),
							Enchantment = item.Str( "enchantment", "None" ),
							EnchantmentPercent = item.Float( "percent", 0f )
						} );
					}
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

			if ( json.TryGetProperty( "equippedSlotIndices", out var equipIdxEl ) )
			{
				JsonElement? equipIdxObj = null;
				if ( equipIdxEl.ValueKind == JsonValueKind.Object )
				{
					equipIdxObj = equipIdxEl;
				}
				else if ( equipIdxEl.ValueKind == JsonValueKind.String )
				{
					var raw = equipIdxEl.GetString();
					if ( !string.IsNullOrEmpty( raw ) )
					{
						try { equipIdxObj = JsonDocument.Parse( raw ).RootElement; }
						catch { }
					}
				}

				if ( equipIdxObj.HasValue && equipIdxObj.Value.ValueKind == JsonValueKind.Object )
				{
					foreach ( var prop in equipIdxObj.Value.EnumerateObject() )
					{
						save.EquippedSlotIndices[prop.Name] = prop.Value.ValueKind == JsonValueKind.Number
							? prop.Value.GetInt32()
							: 0;
					}
				}
			}

			if ( json.TryGetProperty( "slots", out var slotsEl ) )
			{
				JsonElement? slotsArr = null;
				if ( slotsEl.ValueKind == JsonValueKind.Array )
				{
					slotsArr = slotsEl;
				}
				else if ( slotsEl.ValueKind == JsonValueKind.String )
				{
					var raw = slotsEl.GetString();
					if ( !string.IsNullOrEmpty( raw ) )
					{
						try { slotsArr = JsonDocument.Parse( raw ).RootElement; }
						catch { }
					}
				}

				if ( slotsArr.HasValue && slotsArr.Value.ValueKind == JsonValueKind.Array )
				{
					foreach ( var entry in slotsArr.Value.EnumerateArray() )
					{
						var slotEntry = new PlayerSaveData.InventorySlotEntry
						{
							Slot = entry.Int( "slot", 0 ),
							ItemId = entry.Str( "itemId", "None" ),
							Count = entry.Int( "count", 0 ),
							Enchantment = entry.Str( "enchantment", "None" ),
							EnchantmentPercent = entry.Float( "percent", 0f )
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

			if ( json.TryGetProperty( "chestClaims", out var chestEl ) )
			{
				JsonElement? chestObj = null;
				if ( chestEl.ValueKind == JsonValueKind.Object )
				{
					chestObj = chestEl;
				}
				else if ( chestEl.ValueKind == JsonValueKind.String )
				{
					var raw = chestEl.GetString();
					if ( !string.IsNullOrEmpty( raw ) )
					{
						try { chestObj = JsonDocument.Parse( raw ).RootElement; }
						catch { }
					}
				}

				if ( chestObj.HasValue && chestObj.Value.ValueKind == JsonValueKind.Object )
				{
					foreach ( var prop in chestObj.Value.EnumerateObject() )
					{
						if ( prop.Value.ValueKind == JsonValueKind.String )
							save.ChestClaims[prop.Name] = prop.Value.GetString();
					}
				}
			}

			save.CurrentMana = json.Int( "currentMana", -1 );

			if ( json.TryGetProperty( "unlockedSpells", out var unlockedSpellsEl ) && unlockedSpellsEl.ValueKind == JsonValueKind.Array )
			{
				foreach ( var el in unlockedSpellsEl.EnumerateArray() )
				{
					if ( el.ValueKind == JsonValueKind.String )
						save.UnlockedSpells.Add( el.GetString() );
				}
			}

			if ( json.TryGetProperty( "spellSlots", out var spellSlotsEl ) )
			{
				JsonElement? slotsObj = null;
				if ( spellSlotsEl.ValueKind == JsonValueKind.Object )
				{
					slotsObj = spellSlotsEl;
				}
				else if ( spellSlotsEl.ValueKind == JsonValueKind.String )
				{
					var raw = spellSlotsEl.GetString();
					if ( !string.IsNullOrEmpty( raw ) )
					{
						try { slotsObj = JsonDocument.Parse( raw ).RootElement; }
						catch { }
					}
				}

				if ( slotsObj.HasValue && slotsObj.Value.ValueKind == JsonValueKind.Object )
				{
					foreach ( var prop in slotsObj.Value.EnumerateObject() )
					{
						if ( prop.Value.ValueKind == JsonValueKind.String )
							save.SpellSlots[prop.Name] = prop.Value.GetString();
					}
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

			string slotsJson = JsonSerializer.Serialize( BuildSlotsPayload( data.Slots ) );
			string uniqueItemsJson = JsonSerializer.Serialize( BuildUniqueItemsPayload( data.UniqueItems ) );
			string equippedSlotIndicesJson = JsonSerializer.Serialize( data.EquippedSlotIndices ?? new Dictionary<string, int>() );
			string chestClaimsJson = JsonSerializer.Serialize( data.ChestClaims ?? new Dictionary<string, string>() );
			string spellSlotsJson = JsonSerializer.Serialize( data.SpellSlots ?? new Dictionary<string, string>() );

			var payload = new
			{
				version = data.Version,
				savedAt = data.SavedAt,
				playerName = data.PlayerName ?? "",
				skills = BuildSkillsPayload( data.Skills ),
				stackables = data.Stackables,
				uniqueItems = uniqueItemsJson,
				equipped = BuildEquippedPayload( data.Equipped ),
				equippedSlotIndices = equippedSlotIndicesJson,
				equippedAmmoId = data.EquippedAmmoId ?? "None",
				equippedAmmoQty = data.EquippedAmmoQty,
				equippedAmmoSlotIndex = data.EquippedAmmoSlotIndex,
				slots = slotsJson,
				inventoryExpansions = data.InventoryExpansions,
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
				chestClaims = chestClaimsJson,
				currentMana = data.CurrentMana,
				unlockedSpells = data.UnlockedSpells ?? new List<string>(),
				spellSlots = spellSlotsJson
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
		if ( equipped == null )
			return result;

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
				percent = entry.EnchantmentPercent
			} );
		}
		return result;
	}
}