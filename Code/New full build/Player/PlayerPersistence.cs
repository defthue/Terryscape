using Sandbox;

public sealed class PlayerPersistence : Component
{
	[Property] public float AutoSaveIntervalSeconds { get; set; } = 30f;

	/// <summary>
	/// The local player's persistence component. Other systems use this to trigger
	/// immediate saves on important events (level up, item gain, quest complete).
	/// </summary>
	public static PlayerPersistence Local { get; private set; }

	bool _loadAttempted;
	bool _loadComplete;
	RealTimeSince _timeSinceLastSave;

	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		// Reset any HUDs that might still be marked "open" from a previous session.
		// These are static-state HUDs whose flags survive scene reloads — without this
		// reset, closing the game mid-bank (or mid-journal, etc.) would leave the HUD
		// stuck open on next join, blocking the WelcomeHud and other interactions.
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

	// Searches the entire player hierarchy (root + all children) for a component.
	// Necessary because Inventory, Skills, BankStorage may live on different GameObjects
	// inside the player prefab.
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

		// Now that the inventory has its persisted quest list, let every NPC re-check
		// whether their quest is already completed by this player.
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

	/// <summary>
	/// Triggers an immediate cloud save. Safe to call from anywhere on the local player.
	/// No-ops if called before the load completes (so save-on-AddItem during the starter
	/// kit grant doesn't fire 10 saves in a row).
	/// </summary>
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
			// Still set defaults so the endpoint receives empty fields and doesn't error.
			data.Bank = new System.Collections.Generic.Dictionary<string, int>();
			data.BankUnique = new System.Collections.Generic.List<PlayerSaveData.UniqueItemEntry>();
		}

		// Compute denormalized leaderboard fields just before saving. These are flat
		// top-level numbers so sbox.cool can sort/query by them without walking nested
		// objects. NodesMined is already in data (Inventory.ToSaveData set it).
		data.TotalLevel = ComputeTotalLevel( data.Skills );
		data.TotalGold = inventory.GetItemCount( ItemId.GoldCoin );
		data.TotalKills = inventory.GetTotalKills();

		var ok = await TerryScapeBackend.SaveAsync( data );
		if ( ok )
			Log.Info( "[PlayerPersistence] Save successful." );
	}

	// Sums all skill levels into a single total. RuneScape-style "total level" stat,
	// used by the leaderboard.
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