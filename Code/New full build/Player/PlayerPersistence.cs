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

		var result = await TerryScapeBackend.LoadAsync();

		if ( !result.Success )
		{
			Log.Warning( "[PlayerPersistence] Load failed — saves are blocked for this session to prevent overwriting real data with empty state. Reconnect or restart to retry." );
			GameLog.Add( "Could not load your save. Please rejoin the server — playing now will not save your progress.", "#e87878" );
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

		var unlocked = SpellbookState.ToSaveData();
		data.UnlockedSpells = unlocked;

		var mana = FindComponentInPlayer<ManaSystem>();
		data.CurrentMana = mana != null ? mana.CurrentMana : -1;

		data.TotalLevel = ComputeTotalLevel( data.Skills );
		data.TotalGold = inventory.GetItemCount( ItemId.GoldCoin )
			+ ( bank != null ? bank.GetItemCount( ItemId.GoldCoin ) : 0 );
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