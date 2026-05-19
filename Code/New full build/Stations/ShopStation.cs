using Sandbox;
using System.Collections.Generic;

public sealed class ShopStation : Component
{
	[Property] public ShopId Shop { get; set; } = ShopId.None;
	[Property] public string StationName { get; set; } = "";
	[Property] public float InteractDistance { get; set; } = 200f;

	[Property] public List<ShopSellOffer> ItemsForSale { get; set; } = new();

	[Property] public List<ShopBuyOverride> BuysFromPlayerOverrides { get; set; } = new();

	public static ShopStation ActiveShop { get; private set; }
	public static ShopStation ChoosingShop { get; set; }
	public static bool ShowingChoice { get; set; }

	public static ItemId PendingSellAllItem { get; set; } = ItemId.None;
	public static int PendingSellAllAmount { get; set; }
	public static int PendingSellAllTotalGold { get; set; }

	bool HasQuest => Components.Get<NpcInteract>() != null;

	public string DisplayName
	{
		get
		{
			if ( !string.IsNullOrEmpty( StationName ) )
				return StationName;

			return ShopDefaults.GetDefaultName( Shop );
		}
	}

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

		if ( JournalStation.IsOpen )
			return;

		if ( LeaderboardStation.IsOpen )
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

	public IEnumerable<(ItemId Item, int Price)> GetEffectiveItemsForSale()
	{
		if ( ItemsForSale != null && ItemsForSale.Count > 0 )
		{
			foreach ( var offer in ItemsForSale )
			{
				yield return (offer.Item, offer.Price);
			}
			yield break;
		}

		var defaults = ShopDefaults.GetDefaultItemsForSale( Shop );
		foreach ( var d in defaults )
		{
			yield return (d.Item, d.Price);
		}
	}

	public int GetSellPriceForPlayer( ItemId item )
	{
		foreach ( var (entryItem, price) in GetEffectiveItemsForSale() )
		{
			if ( entryItem == item )
				return price;
		}
		return 0;
	}

	public bool SellsItemToPlayer( ItemId item )
	{
		return GetSellPriceForPlayer( item ) > 0;
	}

	public int GetBuyPriceFromPlayer( ItemId item )
	{
		foreach ( var ov in BuysFromPlayerOverrides )
		{
			if ( ov.Item == item )
				return ov.Price;
		}

		return ShopPricing.GetSellPrice( item );
	}

	public bool BuysItemFromPlayer( ItemId item )
	{
		return GetBuyPriceFromPlayer( item ) > 0;
	}

	public bool TryBuy( ItemId item )
	{
		int price = GetSellPriceForPlayer( item );
		if ( price <= 0 )
			return false;

		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		if ( !inventory.HasItem( ItemId.GoldCoin, price ) )
		{
			GameLog.Add( "You don't have enough gold.", "#c86464" );
			return false;
		}

		inventory.RemoveItem( ItemId.GoldCoin, price );
		var (placed, banked) = inventory.AddItemOrBank( item, 1 );

		var def = ItemDatabase.Get( item );
		string name = def != null ? def.Name : item.ToString();
		GameLog.Add( $"Bought {name} for {price} gold.", "#f0c040" );

		if ( banked > 0 )
			GameLog.Add( $"Inventory full — {banked}x {name} sent to your bank.", "#c9a84c" );

		SoundLibrary.PlaySellBuy();
		return true;
	}

	public bool TryBuyMany( ItemId item, int count )
	{
		if ( count <= 0 )
			return false;

		int price = GetSellPriceForPlayer( item );
		if ( price <= 0 )
			return false;

		int total = price * count;

		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		if ( !inventory.HasItem( ItemId.GoldCoin, total ) )
		{
			GameLog.Add( "You don't have enough gold.", "#c86464" );
			return false;
		}

		inventory.RemoveItem( ItemId.GoldCoin, total );
		var (placed, banked) = inventory.AddItemOrBank( item, count );

		var def = ItemDatabase.Get( item );
		string name = def != null ? def.Name : item.ToString();
		GameLog.Add( $"Bought {count}x {name} for {total} gold.", "#f0c040" );

		if ( banked > 0 )
			GameLog.Add( $"Inventory full — {banked}x {name} sent to your bank.", "#c9a84c" );

		SoundLibrary.PlaySellBuy();
		return true;
	}

	public bool TrySell( ItemId item )
	{
		int price = GetBuyPriceFromPlayer( item );
		if ( price <= 0 )
			return false;

		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		if ( !inventory.HasItem( item, 1 ) )
			return false;

		inventory.RemoveItem( item, 1 );
		var (goldPlaced, goldBanked) = inventory.AddItemOrBank( ItemId.GoldCoin, price );

		if ( goldBanked > 0 )
			GameLog.Add( $"Inventory full — {goldBanked} gold sent to your bank.", "#c9a84c" );

		var def = ItemDatabase.Get( item );
		string name = def != null ? def.Name : item.ToString();
		GameLog.Add( $"Sold {name} for {price} gold.", "#f0c040" );

		SoundLibrary.PlaySellBuy();
		return true;
	}

	public bool RequestSellAll( ItemId item )
	{
		int price = GetBuyPriceFromPlayer( item );
		if ( price <= 0 )
			return false;

		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		int amount = inventory.GetItemCount( item );
		if ( amount <= 0 )
			return false;

		PendingSellAllItem = item;
		PendingSellAllAmount = amount;
		PendingSellAllTotalGold = amount * price;
		return true;
	}

	public bool ConfirmSellAll()
	{
		if ( PendingSellAllItem == ItemId.None || PendingSellAllAmount <= 0 )
			return false;

		int price = GetBuyPriceFromPlayer( PendingSellAllItem );
		if ( price <= 0 )
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
		int totalGold = amountToSell * price;

		inventory.RemoveItem( PendingSellAllItem, amountToSell );
		inventory.AddItem( ItemId.GoldCoin, totalGold );

		var def = ItemDatabase.Get( PendingSellAllItem );
		string name = def != null ? def.Name : PendingSellAllItem.ToString();
		GameLog.Add( $"Sold {amountToSell}x {name} for {totalGold} gold.", "#f0c040" );

		SoundLibrary.PlaySellBuy();

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

		if ( instance.IsSocketable && instance.SocketsUsed > 0 )
		{
			GameLog.Add( "Extract the runes before selling this item.", "#c86464" );
			return false;
		}

		int price = GetBuyPriceFromPlayer( instance.ItemId );
		if ( price <= 0 )
		{
			GameLog.Add( "This shop doesn't buy that item.", "#c86464" );
			return false;
		}

		inventory.RemoveUniqueItem( uniqueIndex );
		inventory.AddItem( ItemId.GoldCoin, price );

		var def = ItemDatabase.Get( instance.ItemId );
		string name = def != null ? def.Name : instance.ItemId.ToString();
		GameLog.Add( $"Sold {name} for {price} gold.", "#f0c040" );

		SoundLibrary.PlaySellBuy();
		return true;
	}

	Inventory GetPlayerInventory()
	{
		return PlayerHelper.GetLocalInventory();
	}

	public bool ShouldShowMarker()
	{
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

public class ShopSellOffer
{
	public ItemId Item { get; set; }
	public int Price { get; set; }
}

public class ShopBuyOverride
{
	public ItemId Item { get; set; }
	public int Price { get; set; }
}