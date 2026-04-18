using Sandbox;
using System;
using System.Collections.Generic;

public sealed class NpcInteract : Component
{
	[Property] public string NpcName { get; set; } = "Villager";
	[Property] public string QuestId { get; set; } = "";
	[Property] public string PreviousQuestId { get; set; } = "";

	[Property, TextArea] public string DialogueOffer { get; set; } = "Can you help me out?";
	[Property, TextArea] public string DialogueProgress { get; set; } = "You're not done yet.";
	[Property, TextArea] public string DialogueReady { get; set; } = "Thank you! Here is your reward.";
	[Property, TextArea] public string DialogueDone { get; set; } = "Thanks again for your help.";
	[Property, TextArea] public string DialogueLocked { get; set; } = "I don't have anything for you yet.";

	[Property] public List<ItemId> RequiredItemIds { get; set; } = new();
	[Property] public List<int> RequiredItemAmounts { get; set; } = new();

	[Property] public List<string> RequiredKillTypes { get; set; } = new();
	[Property] public List<int> RequiredKillAmounts { get; set; } = new();

	[Property] public List<ItemId> RewardItemIds { get; set; } = new();
	[Property] public List<int> RewardItemAmounts { get; set; } = new();

	[Property] public string UnlocksRecipe { get; set; } = "";
	[Property] public bool Repeatable { get; set; } = false;
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

	protected override void OnUpdate()
	{
		if ( _cooldownRemaining > 0f )
		{
			_cooldownRemaining -= Time.Delta;
			if ( _cooldownRemaining <= 0f )
				State = QuestState.Available;
		}

		UpdateState();

		if ( !IsActiveQuest() )
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
		if ( shop != null )
		{
			ShopStation.ShowingChoice = true;
			ShopStation.ChoosingShop = shop;
			Mouse.Visibility = MouseVisibility.Visible;
		}
		else
		{
			OpenDialogue();
		}
	}

	bool IsActiveQuest()
	{
		var allQuests = Components.GetAll<NpcInteract>();

		foreach ( var quest in allQuests )
		{
			if ( quest.State == QuestState.Completed && !quest.Repeatable )
				continue;

			if ( quest.State == QuestState.Locked )
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

		return false;
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
			if ( quest.State == QuestState.Completed && !quest.Repeatable )
				continue;

			if ( quest.State == QuestState.Locked )
				continue;

			return quest;
		}

		NpcInteract lastCompleted = null;
		foreach ( var quest in allQuests )
		{
			if ( quest.State == QuestState.Completed )
				lastCompleted = quest;
		}

		return lastCompleted;
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

		if ( State == QuestState.OnCooldown || State == QuestState.Completed )
			return DialogueDone;

		if ( CanComplete() )
			return DialogueReady;

		if ( HasAnyProgress() )
			return DialogueProgress;

		return DialogueOffer;
	}

	bool HasAnyProgress()
	{
		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		int count = Math.Min( RequiredItemIds.Count, RequiredItemAmounts.Count );
		for ( int i = 0; i < count; i++ )
		{
			if ( inventory.GetItemCount( RequiredItemIds[i] ) > 0 )
				return true;
		}

		int killCount = Math.Min( RequiredKillTypes.Count, RequiredKillAmounts.Count );
		for ( int i = 0; i < killCount; i++ )
		{
			if ( inventory.GetKillCount( RequiredKillTypes[i] ) > 0 )
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

		int reqCount = Math.Min( RequiredItemIds.Count, RequiredItemAmounts.Count );
		for ( int i = 0; i < reqCount; i++ )
			inventory.RemoveItem( RequiredItemIds[i], RequiredItemAmounts[i] );

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
}