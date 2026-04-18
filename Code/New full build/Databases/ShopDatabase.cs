using System.Collections.Generic;

public enum ShopId
{
	None,
	GeneralStore,
	Blacksmith
}

public class ShopEntry
{
	public ItemId Item;
	public int BuyPrice;
	public int SellPrice;
}

public class ShopDefinition
{
	public ShopId Id;
	public string Name;
	public List<ShopEntry> Entries = new();

	public ShopEntry GetEntry( ItemId item )
	{
		foreach ( var entry in Entries )
		{
			if ( entry.Item == item )
				return entry;
		}

		return null;
	}
}

public static class ShopDatabase
{
	static Dictionary<ShopId, ShopDefinition> _shops;

	static ShopDefinition Define( ShopId id, string name )
	{
		return new ShopDefinition
		{
			Id = id,
			Name = name
		};
	}

	static ShopEntry Entry( ItemId item, int buyPrice = 0, int sellPrice = 0 )
	{
		return new ShopEntry
		{
			Item = item,
			BuyPrice = buyPrice,
			SellPrice = sellPrice
		};
	}

	static void Build()
	{
		_shops = new Dictionary<ShopId, ShopDefinition>();

		var general = Define( ShopId.GeneralStore, "General Store" );
		general.Entries.Add( Entry( ItemId.Sticks, buyPrice: 2, sellPrice: 1 ) );
		general.Entries.Add( Entry( ItemId.GlassVial, buyPrice: 5, sellPrice: 2 ) );
		general.Entries.Add( Entry( ItemId.LesserHealingPotion, buyPrice: 10, sellPrice: 3 ) );
		general.Entries.Add( Entry( ItemId.Rock, buyPrice: 0, sellPrice: 1 ) );
		general.Entries.Add( Entry( ItemId.Coal, buyPrice: 0, sellPrice: 2 ) );
		general.Entries.Add( Entry( ItemId.AshwoodLog, buyPrice: 0, sellPrice: 2 ) );
		general.Entries.Add( Entry( ItemId.CoppiteOre, buyPrice: 0, sellPrice: 3 ) );
		general.Entries.Add( Entry( ItemId.PrimitiveHatchet, buyPrice: 0, sellPrice: 3 ) );
		general.Entries.Add( Entry( ItemId.PrimitivePickaxe, buyPrice: 0, sellPrice: 3 ) );
		general.Entries.Add( Entry( ItemId.PrimitiveSword, buyPrice: 0, sellPrice: 4 ) );
		Add( general );

		var blacksmith = Define( ShopId.Blacksmith, "Blacksmith" );
		blacksmith.Entries.Add( Entry( ItemId.CoppiteOre, buyPrice: 0, sellPrice: 5 ) );
		blacksmith.Entries.Add( Entry( ItemId.AshsteelOre, buyPrice: 0, sellPrice: 12 ) );
		blacksmith.Entries.Add( Entry( ItemId.ColdveinOre, buyPrice: 0, sellPrice: 25 ) );
		blacksmith.Entries.Add( Entry( ItemId.CoppiteBar, buyPrice: 0, sellPrice: 12 ) );
		blacksmith.Entries.Add( Entry( ItemId.AshsteelBar, buyPrice: 0, sellPrice: 28 ) );
		blacksmith.Entries.Add( Entry( ItemId.ColdveinBar, buyPrice: 0, sellPrice: 55 ) );
		blacksmith.Entries.Add( Entry( ItemId.CoppiteSword, buyPrice: 0, sellPrice: 30 ) );
		blacksmith.Entries.Add( Entry( ItemId.CoppiteShield, buyPrice: 0, sellPrice: 25 ) );
		blacksmith.Entries.Add( Entry( ItemId.CoppiteHeavyHelm, buyPrice: 0, sellPrice: 20 ) );
		blacksmith.Entries.Add( Entry( ItemId.CoppiteHeavyChestplate, buyPrice: 0, sellPrice: 35 ) );
		blacksmith.Entries.Add( Entry( ItemId.CoppiteHeavyLegs, buyPrice: 0, sellPrice: 25 ) );
		Add( blacksmith );
	}

	static void Add( ShopDefinition shop )
	{
		_shops[shop.Id] = shop;
	}

	public static ShopDefinition Get( ShopId id )
	{
		if ( _shops == null )
			Build();

		if ( _shops.TryGetValue( id, out var shop ) )
			return shop;

		return null;
	}

	public static IEnumerable<ShopDefinition> GetAll()
	{
		if ( _shops == null )
			Build();

		return _shops.Values;
	}
}