using Sandbox;

public sealed class PlayerPersistence : Component
{
	[Property] public float AutoSaveIntervalSeconds { get; set; } = 30f;

	public static PlayerPersistence Local { get; private set; }

	bool _loadAttempted;
	bool _loadComplete;
	RealTimeSince _timeSinceLastSave;

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
		// Each Close() below is guarded so it only runs when the HUD was actually
		// active. Avoids the side effect of HUD Close() methods setting the mouse
		// to hidden — the WelcomeHud needs the mouse visible on fresh join.

		JournalStation.Close();
		LeaderboardStation.Close();

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

		// The WelcomeHud opens on every join and needs the mouse visible so the player
		// can navigate it. Restore mouse visible after the HUD resets above.
		Mouse.Visibility = MouseVisibility.Visible;
	}

	protected override void OnDestroy()
	{
		if ( IsProxy )
			return;

		if ( Local == this )
			Local = null;

		if ( !_loadComplete )
			return;

		_ = SaveAsync();
	}

	T FindComponentInPlayer<T>() where T : Component
	{
		var component = Components.Get<T>();
		if ( component != null )
			return component;

		return Components.GetInChildren<T>();
	}

	async System.Threading.Tasks.Task LoadOnStartAsync()
	{
		if ( _loadAttempted )
			return;

		_loadAttempted = true;
		_timeSinceLastSave = 0f;

		var save = await TerryScapeBackend.LoadAsync();

		var inventory = FindComponentInPlayer<Inventory>();
		var skills = FindComponentInPlayer<Skills>();
		var bank = FindComponentInPlayer<BankStorage>();

		if ( save == null )
		{
			Log.Info( "[PlayerPersistence] No save found. Granting starter kit." );

			if ( inventory != null )
				inventory.GrantStarterKit();

			_loadComplete = true;
			RefreshAllNpcQuestState();
			return;
		}

		Log.Info( $"[PlayerPersistence] Applying loaded save (saved {save.SavedAt})." );

		if ( skills != null )
			skills.ApplySaveData( save.Skills );

		if ( inventory != null )
			inventory.ApplySaveData( save );

		if ( bank != null )
			bank.ApplySaveData( save );

		_loadComplete = true;

		RefreshAllNpcQuestState();
	}

	void RefreshAllNpcQuestState()
	{
		foreach ( var npc in Scene.GetAllComponents<NpcInteract>() )
			npc.RefreshFromPersistedState();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( !_loadComplete )
			return;

		if ( _timeSinceLastSave >= AutoSaveIntervalSeconds )
		{
			_timeSinceLastSave = 0f;
			_ = SaveAsync();
		}
	}

	public void RequestSaveNow()
	{
		if ( IsProxy )
			return;

		if ( !_loadComplete )
			return;

		_timeSinceLastSave = 0f;
		_ = SaveAsync();
	}

	public async System.Threading.Tasks.Task SaveAsync()
	{
		var inventory = FindComponentInPlayer<Inventory>();
		var skills = FindComponentInPlayer<Skills>();
		var bank = FindComponentInPlayer<BankStorage>();

		if ( inventory == null || skills == null )
			return;

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
			data.Bank = new System.Collections.Generic.Dictionary<string, int>();
			data.BankUnique = new System.Collections.Generic.List<PlayerSaveData.UniqueItemEntry>();
		}

		data.TotalLevel = ComputeTotalLevel( data.Skills );
		data.TotalGold = inventory.GetItemCount( ItemId.GoldCoin );
		data.TotalKills = inventory.GetTotalKills();

		var ok = await TerryScapeBackend.SaveAsync( data );
		if ( ok )
			Log.Info( "[PlayerPersistence] Save successful." );
	}

	static int ComputeTotalLevel( System.Collections.Generic.Dictionary<string, PlayerSaveData.SkillEntry> skills )
	{
		if ( skills == null )
			return 0;

		int total = 0;
		foreach ( var kv in skills )
			total += kv.Value.Level;
		return total;
	}
}