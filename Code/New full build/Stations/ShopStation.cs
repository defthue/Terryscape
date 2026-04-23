using Sandbox;

public sealed class ShopStation : Component
{
	[Property] public ShopId Shop { get; set; } = ShopId.None;
	[Property] public float InteractDistance { get; set; } = 150f;

	public static ShopStation ActiveShop { get; private set; }
	public static ShopStation ChoosingShop { get; set; }
	public static bool ShowingChoice { get; set; }

	public static ItemId PendingSellAllItem { get; set; } = ItemId.None;
	public static int PendingSellAllAmount { get; set; }
	public static int PendingSellAllTotalGold { get; set; }

	bool HasQuest => Components.Get<NpcInteract>() != null;

	protected override void OnUpdate()
	{
		if ( ActiveShop == this || ( ShowingChoice && ChoosingShop == this ) )
		{
			if ( !IsPlayerInRange() )
			{
				CloseAll();
				return;
			}
		}

		if ( HasQuest )
			return;

		if ( ActiveShop != null || ShowingChoice )
			return;

		if ( NpcInteract.ActiveNpc != null )
			return;

		if ( CraftingStation.ActiveStation != null )
			return;

		if ( TeleportStone.ActiveStone != null )
			return;

		if ( BankStation.ActiveBank != null )
			return;

		if ( EnchantingStation.ActiveStation != null )
			return;

		if ( !IsPlayerInRange() )
			return;

		if ( !Input.Pressed( "use" ) )
			return;

		OpenShop();
	}

	public void OpenShop()
	{
		ShowingChoice = false;
		ChoosingShop = null;
		ActiveShop = this;
		Mouse.Visibility = MouseVisibility.Visible;
	}

	public void OpenQuest()
	{
		ShowingChoice = false;
		ChoosingShop = null;

		var quest = NpcInteract.GetActiveQuestFor( GameObject );
		if ( quest == null )
			return;

		quest.OpenDialogue();
	}

	public static void CloseAll()
	{
		ShowingChoice = false;
		ChoosingShop = null;
		ActiveShop = null;
		ClearPendingSellAll();
		Mouse.Visibility = MouseVisibility.Hidden;
	}

	public static void CloseShop()
	{
		ActiveShop = null;
		ClearPendingSellAll();
		Mouse.Visibility = MouseVisibility.Hidden;
	}

	public bool IsPlayerInRange()
	{
		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
			return false;

		return Vector3.DistanceBetween( WorldPosition, player.WorldPosition ) <= InteractDistance;
	}

	public ShopDefinition GetShopDefinition()
	{
		return ShopDatabase.Get( Shop );
	}

	public bool TryBuy( ItemId item )
	{
		var shop = GetShopDefinition();
		if ( shop == null )
			return false;

		var entry = shop.GetEntry( item );
		if ( entry == null || entry.BuyPrice <= 0 )
			return false;

		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		if ( !inventory.HasItem( ItemId.GoldCoin, entry.BuyPrice ) )
		{
			GameLog.Add( "You don't have enough gold.", "#c86464" );
			return false;
		}

		inventory.RemoveItem( ItemId.GoldCoin, entry.BuyPrice );
		inventory.AddItem( item, 1 );

		var def = ItemDatabase.Get( item );
		string name = def != null ? def.Name : item.ToString();
		GameLog.Add( $"Bought {name} for {entry.BuyPrice} gold.", "#f0c040" );
		return true;
	}

	public bool TrySell( ItemId item )
	{
		var shop = GetShopDefinition();
		if ( shop == null )
			return false;

		var entry = shop.GetEntry( item );
		if ( entry == null || entry.SellPrice <= 0 )
			return false;

		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		if ( !inventory.HasItem( item, 1 ) )
			return false;

		inventory.RemoveItem( item, 1 );
		inventory.AddItem( ItemId.GoldCoin, entry.SellPrice );

		var def = ItemDatabase.Get( item );
		string name = def != null ? def.Name : item.ToString();
		GameLog.Add( $"Sold {name} for {entry.SellPrice} gold.", "#f0c040" );
		return true;
	}

	public bool RequestSellAll( ItemId item )
	{
		var shop = GetShopDefinition();
		if ( shop == null )
			return false;

		var entry = shop.GetEntry( item );
		if ( entry == null || entry.SellPrice <= 0 )
			return false;

		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		int amount = inventory.GetItemCount( item );
		if ( amount <= 0 )
			return false;

		PendingSellAllItem = item;
		PendingSellAllAmount = amount;
		PendingSellAllTotalGold = amount * entry.SellPrice;
		return true;
	}

	public bool ConfirmSellAll()
	{
		if ( PendingSellAllItem == ItemId.None || PendingSellAllAmount <= 0 )
			return false;

		var shop = GetShopDefinition();
		if ( shop == null )
		{
			ClearPendingSellAll();
			return false;
		}

		var entry = shop.GetEntry( PendingSellAllItem );
		if ( entry == null || entry.SellPrice <= 0 )
		{
			ClearPendingSellAll();
			return false;
		}

		var inventory = GetPlayerInventory();
		if ( inventory == null )
		{
			ClearPendingSellAll();
			return false;
		}

		int actualAmount = inventory.GetItemCount( PendingSellAllItem );
		if ( actualAmount <= 0 )
		{
			ClearPendingSellAll();
			return false;
		}

		int amountToSell = System.Math.Min( actualAmount, PendingSellAllAmount );
		int totalGold = amountToSell * entry.SellPrice;

		inventory.RemoveItem( PendingSellAllItem, amountToSell );
		inventory.AddItem( ItemId.GoldCoin, totalGold );

		var def = ItemDatabase.Get( PendingSellAllItem );
		string name = def != null ? def.Name : PendingSellAllItem.ToString();
		GameLog.Add( $"Sold {amountToSell}x {name} for {totalGold} gold.", "#f0c040" );

		ClearPendingSellAll();
		return true;
	}

	public static void ClearPendingSellAll()
	{
		PendingSellAllItem = ItemId.None;
		PendingSellAllAmount = 0;
		PendingSellAllTotalGold = 0;
	}

	public bool TrySellUnique( int uniqueIndex )
	{
		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		var items = inventory.GetUniqueItems();
		if ( uniqueIndex < 0 || uniqueIndex >= items.Count )
			return false;

		var instance = items[uniqueIndex];

		if ( instance.IsEnchanted )
		{
			GameLog.Add( "You can't sell enchanted items at a shop.", "#c86464" );
			return false;
		}

		var shop = GetShopDefinition();
		if ( shop == null )
			return false;

		var entry = shop.GetEntry( instance.ItemId );
		if ( entry == null || entry.SellPrice <= 0 )
		{
			GameLog.Add( "This shop doesn't buy that item.", "#c86464" );
			return false;
		}

		inventory.RemoveUniqueItem( uniqueIndex );
		inventory.AddItem( ItemId.GoldCoin, entry.SellPrice );

		var def = ItemDatabase.Get( instance.ItemId );
		string name = def != null ? def.Name : instance.ItemId.ToString();
		GameLog.Add( $"Sold {name} for {entry.SellPrice} gold.", "#f0c040" );
		return true;
	}

	Inventory GetPlayerInventory()
	{
		return PlayerHelper.GetLocalInventory();
	}

	public bool ShouldShowMarker()
	{
		if ( Shop == ShopId.None )
			return false;

		var quests = GameObject.Components.GetAll<NpcInteract>();
		foreach ( var quest in quests )
		{
			if ( quest.ShouldShowMarker() )
				return false;
		}

		return true;
	}

	public string GetMarkerColor()
	{
		return "#4db8c9";
	}
}