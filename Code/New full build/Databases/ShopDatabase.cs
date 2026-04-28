using System.Collections.Generic;

public enum ShopId
{
	None,
	GeneralStore,
	Blacksmith
}

/// <summary>
/// Default presets for the items each ShopId sells. Used as a fallback when a
/// ShopStation in the scene doesn't have a custom ItemsForSale list configured.
/// Lets us add a "General Store" shop to the scene with zero per-shop setup.
///
/// Override-by-design: if a scene-level shop populates ItemsForSale in the inspector,
/// ShopStation uses THAT and ignores the preset here. So this is just a starting
/// point, not a hard contract.
/// </summary>
public static class ShopDefaults
{
	public class DefaultOffer
	{
		public ItemId Item;
		public int Price;
	}

	public static List<DefaultOffer> GetDefaultItemsForSale( ShopId id )
	{
		var list = new List<DefaultOffer>();

		switch ( id )
		{
			case ShopId.GeneralStore:
				list.Add( new DefaultOffer { Item = ItemId.LesserHealingPotion, Price = 5 } );
				list.Add( new DefaultOffer { Item = ItemId.LesserManaPotion, Price = 5 } );
				list.Add( new DefaultOffer { Item = ItemId.RoughFiber, Price = 3 } );
				list.Add( new DefaultOffer { Item = ItemId.MonsterHide, Price = 4 } );
				list.Add( new DefaultOffer { Item = ItemId.GlassVial, Price = 2 } );
				list.Add( new DefaultOffer { Item = ItemId.CrystalVial, Price = 15 } );
				break;

			case ShopId.Blacksmith:
				// For now both shops sell the same things. Differentiate later when we
				// decide each shop's identity.
				list.Add( new DefaultOffer { Item = ItemId.LesserHealingPotion, Price = 5 } );
				list.Add( new DefaultOffer { Item = ItemId.LesserManaPotion, Price = 5 } );
				list.Add( new DefaultOffer { Item = ItemId.RoughFiber, Price = 3 } );
				list.Add( new DefaultOffer { Item = ItemId.MonsterHide, Price = 4 } );
				list.Add( new DefaultOffer { Item = ItemId.GlassVial, Price = 2 } );
				list.Add( new DefaultOffer { Item = ItemId.CrystalVial, Price = 15 } );
				break;
		}

		return list;
	}

	public static string GetDefaultName( ShopId id )
	{
		switch ( id )
		{
			case ShopId.GeneralStore: return "General Store";
			case ShopId.Blacksmith: return "Blacksmith";
			default: return "Shop";
		}
	}
}