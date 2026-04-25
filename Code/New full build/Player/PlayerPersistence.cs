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

		Local = this;

		NetworkStorageConfig.EnsureInitialized();

		_ = LoadOnStartAsync();
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

	async System.Threading.Tasks.Task LoadOnStartAsync()
	{
		if ( _loadAttempted )
			return;

		_loadAttempted = true;
		_timeSinceLastSave = 0f;

		var save = await TerryScapeBackend.LoadAsync();

		var inventory = Components.Get<Inventory>();
		var skills = Components.Get<Skills>();

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
		var inventory = Components.Get<Inventory>();
		var skills = Components.Get<Skills>();

		if ( inventory == null || skills == null )
			return;

		var data = new PlayerSaveData
		{
			Version = 1,
			PlayerName = Network.Owner?.DisplayName ?? "",
			Skills = skills.ToSaveData()
		};

		inventory.ToSaveData( data );

		var ok = await TerryScapeBackend.SaveAsync( data );
		if ( ok )
			Log.Info( "[PlayerPersistence] Save successful." );
	}
}