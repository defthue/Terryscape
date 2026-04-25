using Sandbox;
using System;
using System.Collections.Generic;

public sealed class NpcInteract : Component
{
	[Property] public string NpcName { get; set; } = "Villager";
	[Property] public string QuestId { get; set; } = "";
	[Property] public string PreviousQuestId { get; set; } = "";

	[Property, TextArea] public string DialogueOffer { get; set; } = "Can you help me out?";
	[Property, TextArea] public string DialogueReady { get; set; } = "Thank you! Here is your reward.";
	[Property, TextArea] public string DialogueDone { get; set; } = "";
	[Property, TextArea] public string DialogueLocked { get; set; } = "";

	[Property] public List<ItemId> RequiredItemIds { get; set; } = new();
	[Property] public List<int> RequiredItemAmounts { get; set; } = new();

	[Property] public List<string> RequiredKillTypes { get; set; } = new();
	[Property] public List<int> RequiredKillAmounts { get; set; } = new();

	[Property] public List<ItemId> RewardItemIds { get; set; } = new();
	[Property] public List<int> RewardItemAmounts { get; set; } = new();

	[Property] public string UnlocksRecipe { get; set; } = "";
	[Property] public bool Repeatable { get; set; } = false;
	[Property] public bool ConsumeRequiredItems { get; set; } = true;
	[Property] public float CooldownDuration { get; set; } = 60f;
	[Property] public float InteractDistance { get; set; } = 150f;

	public static NpcInteract ActiveNpc { get; private set; }

	public enum QuestState { Locked, Available, OnCooldown, Completed }
	public QuestState State { get; private set; } = QuestState.Available;

	float _cooldownRemaining = 0f;

	public struct QuestItem
	{
		public ItemId Item;
		public int Amount;
	}

	public struct QuestKill
	{
		public string MonsterType;
		public int Amount;
	}

	protected override void OnStart()
	{
		// Sync NPC state with what the player's inventory remembers from their cloud save.
		// Note: the cloud save loads async, so this might run before the inventory is populated.
		// PlayerPersistence calls RefreshFromPersistedState() on every NPC after the load finishes.
		CheckPersistedCompletion();
	}

	/// <summary>
	/// Public hook for PlayerPersistence to call once the cloud save has been loaded
	/// and applied to the player's inventory. Re-checks completion state.
	/// </summary>
	public void RefreshFromPersistedState()
	{
		CheckPersistedCompletion();
	}

	void CheckPersistedCompletion()
	{
		if ( string.IsNullOrEmpty( QuestId ) )
			return;

		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return;

		if ( !inventory.IsQuestCompleted( QuestId ) )
			return;

		// Repeatable quests stay Available — "you've done it before, you can do it again."
		// We don't persist cooldowns, so a freshly loaded repeatable quest is always ready to redo.
		if ( Repeatable )
			return;

		State = QuestState.Completed;
	}

	protected override void OnUpdate()
	{
		if ( _cooldownRemaining > 0f )
		{
			_cooldownRemaining -= Time.Delta;
			if ( _cooldownRemaining <= 0f )
				State = QuestState.Available;
		}

		UpdateState();

		if ( !IsActiveOnThisNpc() )
			return;

		if ( ActiveNpc != null )
			return;

		if ( CraftingStation.ActiveStation != null )
			return;

		if ( ShopStation.ActiveShop != null || ShopStation.ShowingChoice )
			return;

		if ( TeleportStone.ActiveStone != null )
			return;

		if ( BankStation.ActiveBank != null )
			return;

		if ( EnchantingStation.ActiveStation != null )
			return;

		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
			return;

		var distance = Vector3.DistanceBetween( WorldPosition, player.WorldPosition );
		if ( distance > InteractDistance )
			return;

		if ( !Input.Pressed( "use" ) )
			return;

		var shop = Components.Get<ShopStation>();
		bool questAvailable = HasAvailableQuestOnNpc();

		if ( shop != null && questAvailable )
		{
			ShopStation.ShowingChoice = true;
			ShopStation.ChoosingShop = shop;
			Mouse.Visibility = MouseVisibility.Visible;
		}
		else if ( shop != null )
		{
			shop.OpenShop();
		}
		else if ( questAvailable )
		{
			OpenDialogue();
		}
	}

	bool HasAvailableQuestOnNpc()
	{
		var allQuests = GameObject.Components.GetAll<NpcInteract>();

		foreach ( var quest in allQuests )
		{
			if ( quest.State == QuestState.Locked )
				continue;

			if ( quest.State == QuestState.Completed && !quest.Repeatable )
			{
				if ( !quest.HasFollowUpOnThisNpc() )
					continue;

				return true;
			}

			return true;
		}

		return false;
	}

	public static bool NpcHasAvailableQuest( GameObject npcObject )
	{
		if ( npcObject == null )
			return false;

		var allQuests = npcObject.Components.GetAll<NpcInteract>();

		foreach ( var quest in allQuests )
		{
			if ( quest.State == QuestState.Locked )
				continue;

			if ( quest.State == QuestState.Completed && !quest.Repeatable )
			{
				if ( !quest.HasFollowUpOnThisNpc() )
					continue;

				return true;
			}

			return true;
		}

		return false;
	}

	bool IsActiveOnThisNpc()
	{
		var allQuests = GameObject.Components.GetAll<NpcInteract>();

		foreach ( var quest in allQuests )
		{
			if ( quest.State == QuestState.Locked )
				continue;

			if ( quest.State == QuestState.Completed && !quest.Repeatable )
				continue;

			return quest == this;
		}

		NpcInteract lastCompleted = null;
		foreach ( var quest in allQuests )
		{
			if ( quest.State == QuestState.Completed )
				lastCompleted = quest;
		}

		if ( lastCompleted != null )
			return lastCompleted == this;

		NpcInteract firstLocked = null;
		foreach ( var quest in allQuests )
		{
			if ( quest.State == QuestState.Locked )
			{
				firstLocked = quest;
				break;
			}
		}

		return firstLocked == this;
	}

	void UpdateState()
	{
		if ( State == QuestState.OnCooldown || State == QuestState.Completed )
			return;

		if ( !string.IsNullOrEmpty( PreviousQuestId ) )
		{
			var inventory = GetPlayerInventory();
			if ( inventory == null || !inventory.IsQuestCompleted( PreviousQuestId ) )
			{
				State = QuestState.Locked;
				return;
			}
		}

		if ( State == QuestState.Locked )
			State = QuestState.Available;
	}

	public static NpcInteract GetActiveQuestFor( GameObject npcObject )
	{
		var allQuests = npcObject.Components.GetAll<NpcInteract>();

		foreach ( var quest in allQuests )
		{
			if ( quest.State == QuestState.Locked )
				continue;

			if ( quest.State == QuestState.Completed && !quest.Repeatable )
				continue;

			return quest;
		}

		NpcInteract lastCompleted = null;
		foreach ( var quest in allQuests )
		{
			if ( quest.State == QuestState.Completed )
				lastCompleted = quest;
		}

		if ( lastCompleted != null )
			return lastCompleted;

		foreach ( var quest in allQuests )
		{
			if ( quest.State == QuestState.Locked )
				return quest;
		}

		return null;
	}

	public void OpenDialogue()
	{
		ActiveNpc = this;
		Mouse.Visibility = MouseVisibility.Visible;
	}

	public void CloseDialogue()
	{
		if ( ActiveNpc == this )
		{
			ActiveNpc = null;
			Mouse.Visibility = MouseVisibility.Hidden;
		}
	}

	public string GetDialogueText()
	{
		if ( State == QuestState.Locked )
			return DialogueLocked;

		if ( State == QuestState.OnCooldown )
			return DialogueDone;

		if ( State == QuestState.Completed )
		{
			if ( HasFollowUpOnThisNpc() )
				return DialogueOffer;

			return DialogueDone;
		}

		if ( CanComplete() )
			return DialogueReady;

		return DialogueOffer;
	}

	public bool HasFollowUpOnThisNpc()
	{
		if ( string.IsNullOrEmpty( QuestId ) )
			return false;

		var allQuests = GameObject.Components.GetAll<NpcInteract>();

		foreach ( var quest in allQuests )
		{
			if ( quest == this )
				continue;

			if ( quest.PreviousQuestId == QuestId )
				return true;
		}

		return false;
	}

	public bool CanComplete()
	{
		if ( State == QuestState.Locked || State == QuestState.OnCooldown || State == QuestState.Completed )
			return false;

		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		int count = Math.Min( RequiredItemIds.Count, RequiredItemAmounts.Count );
		for ( int i = 0; i < count; i++ )
		{
			if ( !inventory.HasItem( RequiredItemIds[i], RequiredItemAmounts[i] ) )
				return false;
		}

		int killCount = Math.Min( RequiredKillTypes.Count, RequiredKillAmounts.Count );
		for ( int i = 0; i < killCount; i++ )
		{
			if ( inventory.GetKillCount( RequiredKillTypes[i] ) < RequiredKillAmounts[i] )
				return false;
		}

		return true;
	}

	public void CompleteQuest()
	{
		if ( !CanComplete() )
			return;

		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return;

		if ( ConsumeRequiredItems )
		{
			int reqCount = Math.Min( RequiredItemIds.Count, RequiredItemAmounts.Count );
			for ( int i = 0; i < reqCount; i++ )
				inventory.RemoveItem( RequiredItemIds[i], RequiredItemAmounts[i] );
		}

		int rewCount = Math.Min( RewardItemIds.Count, RewardItemAmounts.Count );
		for ( int i = 0; i < rewCount; i++ )
		{
			inventory.AddItem( RewardItemIds[i], RewardItemAmounts[i] );
			var def = ItemDatabase.Get( RewardItemIds[i] );
			string name = def != null ? def.Name : RewardItemIds[i].ToString();
			GameLog.Add( $"Received {RewardItemAmounts[i]}x {name}!", "#f0c040" );
		}

		if ( !string.IsNullOrEmpty( UnlocksRecipe ) )
		{
			foreach ( var raw in UnlocksRecipe.Split( ',' ) )
			{
				var id = raw.Trim();
				if ( string.IsNullOrEmpty( id ) )
					continue;

				inventory.UnlockRecipe( id );
				var recipe = RecipeDatabase.GetById( id );
				if ( recipe != null )
					GameLog.Add( $"Learned recipe: {recipe.Name}!", "#f0c040" );
			}
		}

		if ( !string.IsNullOrEmpty( QuestId ) )
		{
			inventory.CompleteQuest( QuestId );
			GameLog.Add( $"Quest complete!", "#f0c040" );
		}

		if ( Repeatable )
		{
			State = QuestState.OnCooldown;
			_cooldownRemaining = CooldownDuration;
		}
		else
		{
			State = QuestState.Completed;
		}

		CloseDialogue();
	}

	public List<QuestItem> GetRequiredItems()
	{
		var list = new List<QuestItem>();
		int count = Math.Min( RequiredItemIds.Count, RequiredItemAmounts.Count );
		for ( int i = 0; i < count; i++ )
			list.Add( new QuestItem { Item = RequiredItemIds[i], Amount = RequiredItemAmounts[i] } );
		return list;
	}

	public List<QuestKill> GetRequiredKills()
	{
		var list = new List<QuestKill>();
		int count = Math.Min( RequiredKillTypes.Count, RequiredKillAmounts.Count );
		for ( int i = 0; i < count; i++ )
			list.Add( new QuestKill { MonsterType = RequiredKillTypes[i], Amount = RequiredKillAmounts[i] } );
		return list;
	}

	public List<QuestItem> GetRewardItems()
	{
		var list = new List<QuestItem>();
		int count = Math.Min( RewardItemIds.Count, RewardItemAmounts.Count );
		for ( int i = 0; i < count; i++ )
			list.Add( new QuestItem { Item = RewardItemIds[i], Amount = RewardItemAmounts[i] } );
		return list;
	}

	public string GetItemName( ItemId id )
	{
		var def = ItemDatabase.Get( id );
		return def != null ? def.Name : id.ToString();
	}

	Inventory GetPlayerInventory()
	{
		return PlayerHelper.GetLocalInventory();
	}

	public bool ShouldShowMarker()
	{
		if ( State == QuestState.Locked )
			return false;

		if ( State == QuestState.OnCooldown )
			return false;

		if ( State == QuestState.Completed )
			return HasFollowUpOnThisNpc();

		return true;
	}

	public bool IsMarkerPulsing()
	{
		return State == QuestState.Available && CanComplete();
	}

	public string GetMarkerColor()
	{
		if ( IsMarkerPulsing() )
			return "#ffe88a";

		return "#f0c040";
	}
}