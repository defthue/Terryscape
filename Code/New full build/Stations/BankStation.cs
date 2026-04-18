using Sandbox;

public sealed class BankStation : Component
{
	[Property] public string StationName { get; set; } = "Bank";
	[Property] public float InteractDistance { get; set; } = 200f;

	public static BankStation ActiveBank { get; private set; }

	protected override void OnUpdate()
	{
		if ( ActiveBank == this )
		{
			if ( !IsPlayerInRange() )
			{
				Close();
				return;
			}

			if ( Input.Pressed( "use" ) )
			{
				Close();
				return;
			}
		}
		else if ( ActiveBank == null )
		{
			if ( NpcInteract.ActiveNpc != null )
				return;

			if ( CraftingStation.ActiveStation != null )
				return;

			if ( ShopStation.ActiveShop != null || ShopStation.ShowingChoice )
				return;

			if ( TeleportStone.ActiveStone != null )
				return;

			if ( EnchantingStation.ActiveStation != null )
				return;

			if ( !IsPlayerInRange() )
				return;

			if ( !Input.Pressed( "use" ) )
				return;

			Open();
		}
	}

	void Open()
	{
		ActiveBank = this;
		Mouse.Visibility = MouseVisibility.Visible;
	}

	public static void Close()
	{
		ActiveBank = null;
		Mouse.Visibility = MouseVisibility.Hidden;
	}

	public bool IsPlayerInRange()
	{
		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
			return false;

		return Vector3.DistanceBetween( WorldPosition, player.WorldPosition ) <= InteractDistance;
	}

	public void DoDeposit( ItemId item, int amount )
	{
		var inventory = GetPlayerInventory();
		var bank = GetPlayerBank();
		if ( inventory == null || bank == null )
			return;

		int have = inventory.GetItemCount( item );
		if ( have <= 0 )
			return;

		int actual = amount > have ? have : amount;

		inventory.RemoveItem( item, actual );
		bank.Deposit( item, actual );

		var def = ItemDatabase.Get( item );
		string name = def != null ? def.Name : item.ToString();
		GameLog.Add( $"Deposited {actual}x {name}.", "#4caf78" );
	}

	public void DoWithdraw( ItemId item, int amount )
	{
		var inventory = GetPlayerInventory();
		var bank = GetPlayerBank();
		if ( inventory == null || bank == null )
			return;

		int have = bank.GetItemCount( item );
		if ( have <= 0 )
			return;

		int actual = amount > have ? have : amount;

		bank.Withdraw( item, actual );
		inventory.AddItem( item, actual );

		var def = ItemDatabase.Get( item );
		string name = def != null ? def.Name : item.ToString();
		GameLog.Add( $"Withdrew {actual}x {name}.", "#4caf78" );
	}

	public void DoDepositUnique( int inventoryIndex )
	{
		var inventory = GetPlayerInventory();
		var bank = GetPlayerBank();
		if ( inventory == null || bank == null )
			return;

		var items = inventory.GetUniqueItems();
		if ( inventoryIndex < 0 || inventoryIndex >= items.Count )
			return;

		var instance = items[inventoryIndex];
		inventory.RemoveUniqueItem( inventoryIndex );
		bank.DepositUnique( instance );

		GameLog.Add( $"Deposited {instance.GetDisplayName()}.", "#4caf78" );
	}

	public void DoWithdrawUnique( int bankIndex )
	{
		var inventory = GetPlayerInventory();
		var bank = GetPlayerBank();
		if ( inventory == null || bank == null )
			return;

		var instance = bank.WithdrawUnique( bankIndex );
		if ( instance == null )
			return;

		inventory.AddUniqueItem( instance );

		GameLog.Add( $"Withdrew {instance.GetDisplayName()}.", "#4caf78" );
	}

	Inventory GetPlayerInventory()
	{
		return PlayerHelper.GetLocalInventory();
	}

	BankStorage GetPlayerBank()
	{
		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
			return null;

		return player.Components.Get<BankStorage>();
	}
}