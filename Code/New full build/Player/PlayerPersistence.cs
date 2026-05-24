using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox;

public sealed class PlayerPersistence : Component
{
	[Property] public float DebounceSeconds { get; set; } = 5f;
	[Property] public float SafetyNetSeconds { get; set; } = 90f;

	public static PlayerPersistence Local { get; private set; }

	bool _loadAttempted;
	bool _loadComplete;
	bool _savesBlocked;

	SaveSection _dirty = SaveSection.None;
	RealTimeSince _timeSinceDirty;
	RealTimeSince _timeSinceFirstDirty;
	bool _hasPendingDirty;

	Task _saveInFlight;
	bool _resaveQueued;
	SaveSection _resaveQueuedSections;

	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		ResetTransientHudState();
		Local = this;
		NetworkStorageConfig.EnsureInitialized();
		_ = LoadOnStartAsync();
	}

	void ResetTransientHudState()
	{
		JournalStation.Close();
		LeaderboardStation.Close();
		SpellbookStation.Close();

		if ( BankStation.ActiveBank != null )
			BankStation.Close();

		if ( CraftingStation.ActiveStation != null )
			CraftingStation.Close();

		if ( ShopStation.ActiveShop != null )
			ShopStation.CloseShop();

		ShopStation.ShowingChoice = false;
		ShopStation.ChoosingShop = null;
		ShopStation.ClearPendingSellAll();

		if ( EnchantingStation.ActiveStation != null )
			EnchantingStation.Close();

		if ( TeleportStone.ActiveStone != null )
			TeleportStone.Close();

		if ( NpcInteract.ActiveNpc != null )
			NpcInteract.ActiveNpc.CloseDialogue();

		Mouse.Visibility = MouseVisibility.Visible;
	}

	protected override void OnDestroy()
	{
		if ( IsProxy )
			return;

		if ( Local == this )
			Local = null;

		if ( !_loadComplete || _savesBlocked )
			return;

		_ = LogoutSaveAsync();
	}

	async Task LogoutSaveAsync()
	{
		try
		{
			if ( _saveInFlight != null )
				await _saveInFlight;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[PlayerPersistence] Logout: prior save threw: {ex.Message}" );
		}

		var data = BuildSaveData();
		if ( data == null )
			return;

		var ok = await TerryScapeBackend.SaveAllAsync( data );
		if ( ok )
			Log.Info( "[PlayerPersistence] Logout save successful." );
		else
			Log.Warning( "[PlayerPersistence] Logout save failed." );
	}

	T FindComponentInPlayer<T>() where T : Component
	{
		var component = Components.Get<T>();
		if ( component != null )
			return component;

		return Components.GetInChildren<T>();
	}

	async Task LoadOnStartAsync()
	{
		if ( _loadAttempted )
			return;

		_loadAttempted = true;

		var result = await TerryScapeBackend.LoadAsync();

		if ( !result.Success )
		{
			_savesBlocked = true;
			Log.Warning( "[PlayerPersistence] Load failed — saves are blocked for this session to prevent overwriting real data with empty state. Reconnect or restart to retry." );
			GameLog.Add( "Could not load your save. Please rejoin the server — playing now will not save your progress.", "#e87878" );
			GameManager.Instance?.AddLocalChatMessage( "[Save] Load failed. Please rejoin — playing now will not save progress." );
			return;
		}

		var inventory = FindComponentInPlayer<Inventory>();
		var skills = FindComponentInPlayer<Skills>();
		var bank = FindComponentInPlayer<BankStorage>();

		var save = result.Save;

		if ( save == null )
		{
			Log.Info( "[PlayerPersistence] No save found. Granting starter kit." );

			if ( inventory != null )
				inventory.GrantStarterKit();

			var newHealth = FindComponentInPlayer<PlayerHealth>();
			if ( newHealth != null )
				newHealth.RefillToMax();

			SpellbookState.ApplySaveData( null );

			_loadComplete = true;
			RefreshAllNpcQuestState();
			return;
		}

		Log.Info( $"[PlayerPersistence] Applying loaded save (saved {save.SavedAt})." );

		if ( skills != null )
			skills.ApplySaveData( save.Skills );

		var health = FindComponentInPlayer<PlayerHealth>();
		if ( health != null )
			health.RefillToMax();

		if ( inventory != null )
			inventory.ApplySaveData( save );

		if ( bank != null )
			bank.ApplySaveData( save );

		SpellbookState.ApplySaveData( save.UnlockedSpells );

		var mana = FindComponentInPlayer<ManaSystem>();
		if ( mana != null && save.CurrentMana >= 0 )
		{
			int clamped = save.CurrentMana;
			if ( clamped > mana.MaxMana )
				clamped = mana.MaxMana;
			mana.CurrentMana = clamped;
		}

		_loadComplete = true;
		RefreshAllNpcQuestState();
	}

	void RefreshAllNpcQuestState()
	{
		foreach ( var npc in Scene.GetAllComponents<NpcInteract>() )
			npc.RefreshFromPersistedState();
	}

	public void MarkDirty( SaveSection sections )
	{
		if ( IsProxy || !_loadComplete || _savesBlocked )
			return;

		if ( sections == SaveSection.None )
			return;

		if ( _dirty == SaveSection.None )
		{
			_timeSinceFirstDirty = 0f;
			_hasPendingDirty = true;
		}

		_dirty |= sections;
		_timeSinceDirty = 0f;
	}

	public void SaveNow( SaveSection sections )
	{
		if ( IsProxy || !_loadComplete || _savesBlocked )
			return;

		MarkDirty( sections );
		FlushPendingSave();
	}

	public void RequestSaveNow()
	{
		if ( IsProxy || !_loadComplete || _savesBlocked )
			return;

		MarkDirty( SaveSection.All );
		FlushPendingSave();
	}

	void FlushPendingSave()
	{
		if ( _dirty == SaveSection.None )
			return;

		if ( _saveInFlight != null && !_saveInFlight.IsCompleted )
		{
			_resaveQueued = true;
			_resaveQueuedSections |= _dirty;
			_dirty = SaveSection.None;
			_hasPendingDirty = false;
			return;
		}

		var sections = _dirty;
		_dirty = SaveSection.None;
		_hasPendingDirty = false;

		_saveInFlight = RunSaveAsync( sections );
	}

	protected override void OnUpdate()
	{
		if ( IsProxy || !_loadComplete || _savesBlocked )
			return;

		if ( !_hasPendingDirty )
			return;

		bool debounceElapsed = _timeSinceDirty >= DebounceSeconds;
		bool safetyNetTripped = _timeSinceFirstDirty >= SafetyNetSeconds;

		if ( !debounceElapsed && !safetyNetTripped )
			return;

		FlushPendingSave();
	}

	async Task RunSaveAsync( SaveSection sections )
	{
		try
		{
			var data = BuildSaveData();
			if ( data == null )
				return;

			var ok = await TerryScapeBackend.SaveAllAsync( data );

			if ( ok )
			{
				Log.Info( $"[PlayerPersistence] Save successful (sections: {sections})." );
			}
			else
			{
				_dirty |= sections;
				_timeSinceDirty = 0f;
				if ( !_hasPendingDirty )
				{
					_timeSinceFirstDirty = 0f;
					_hasPendingDirty = true;
				}
				Log.Warning( "[PlayerPersistence] Save failed. Sections re-marked dirty for retry." );
				GameLog.Add( "Save failed — will retry. Your recent progress may be temporarily unsaved.", "#c9a84c" );
			}
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[PlayerPersistence] RunSaveAsync threw: {ex.Message}" );
			_dirty |= sections;
		}
		finally
		{
			if ( _resaveQueued )
			{
				var queued = _resaveQueuedSections;
				_resaveQueued = false;
				_resaveQueuedSections = SaveSection.None;
				_dirty |= queued;
				_timeSinceDirty = 0f;
				if ( !_hasPendingDirty )
				{
					_timeSinceFirstDirty = 0f;
					_hasPendingDirty = true;
				}
			}

			_saveInFlight = null;
		}
	}

	PlayerSaveData BuildSaveData()
	{
		var inventory = FindComponentInPlayer<Inventory>();
		var skills = FindComponentInPlayer<Skills>();
		var bank = FindComponentInPlayer<BankStorage>();

		if ( inventory == null || skills == null )
			return null;

		var data = new PlayerSaveData
		{
			Version = 1,
			PlayerName = Network.Owner?.DisplayName ?? "",
			Skills = skills.ToSaveData()
		};

		inventory.ToSaveData( data );

		if ( bank != null )
		{
			bank.ToSaveData( data );
		}
		else
		{
			data.Bank = new Dictionary<string, int>();
			data.BankUnique = new List<PlayerSaveData.UniqueItemEntry>();
		}

		data.UnlockedSpells = SpellbookState.ToSaveData();

		var mana = FindComponentInPlayer<ManaSystem>();
		data.CurrentMana = mana != null ? mana.CurrentMana : -1;

		data.TotalLevel = ComputeTotalLevel( data.Skills );
		data.TotalGold = inventory.GetItemCount( ItemId.GoldCoin )
			+ ( bank != null ? bank.GetItemCount( ItemId.GoldCoin ) : 0 );
		data.TotalKills = inventory.GetTotalKills();

		return data;
	}

	static int ComputeTotalLevel( Dictionary<string, PlayerSaveData.SkillEntry> skills )
	{
		if ( skills == null )
			return 0;

		int total = 0;
		foreach ( var kv in skills )
			total += kv.Value.Level;
		return total;
	}
}