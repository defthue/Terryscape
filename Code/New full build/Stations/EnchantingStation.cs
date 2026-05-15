using Sandbox;
using System;
using System.Collections.Generic;

public sealed class EnchantingStation : Component
{
	[Property] public string StationName { get; set; } = "Enchanting Altar";
	[Property] public float InteractDistance { get; set; } = 200f;

	[Property] public int RoughEssenceCost { get; set; } = 3;
	[Property] public int FineEssenceCost { get; set; } = 5;
	[Property] public int PristineEssenceCost { get; set; } = 8;

	[Property] public float RoughMinPercent { get; set; } = 1f;
	[Property] public float RoughMaxPercent { get; set; } = 3f;
	[Property] public float FineMinPercent { get; set; } = 2f;
	[Property] public float FineMaxPercent { get; set; } = 5f;
	[Property] public float PristineMinPercent { get; set; } = 3f;
	[Property] public float PristineMaxPercent { get; set; } = 8f;

	[Property] public float RoughMaxCap { get; set; } = 5f;
	[Property] public float FineMaxCap { get; set; } = 8f;
	[Property] public float PristineMaxCap { get; set; } = 12f;

	[Property] public int RoughLevelRequired { get; set; } = 10;
	[Property] public int FineLevelRequired { get; set; } = 30;
	[Property] public int PristineLevelRequired { get; set; } = 50;

	[Property] public int RoughEnchantXp { get; set; } = 40;
	[Property] public int FineEnchantXp { get; set; } = 100;
	[Property] public int PristineEnchantXp { get; set; } = 200;

	[Property] public int RoughCombineXp { get; set; } = 25;
	[Property] public int FineCombineXp { get; set; } = 60;
	[Property] public int PristineCombineXp { get; set; } = 120;

	[Property] public int CombineEssenceCost { get; set; } = 2;

	[Property] public int RoughSalvageReturn { get; set; } = 2;
	[Property] public int FineSalvageReturn { get; set; } = 3;
	[Property] public int PristineSalvageReturn { get; set; } = 5;

	public static EnchantingStation ActiveStation { get; private set; }

	protected override void OnUpdate()
	{
		if ( ActiveStation == this )
		{
			if ( !IsPlayerInRange() )
			{
				Close();
				return;
			}
		}

		if ( ActiveStation != null )
			return;

		if ( NpcInteract.ActiveNpc != null )
			return;

		if ( CraftingStation.ActiveStation != null )
			return;

		if ( ShopStation.ActiveShop != null || ShopStation.ShowingChoice )
			return;

		if ( TeleportStone.ActiveStone != null )
			return;

		if ( BankStation.ActiveBank != null )
			return;

		if ( !IsPlayerInRange() )
			return;

		if ( !Input.Pressed( "use" ) )
			return;

		Open();
	}

	void Open()
	{
		ActiveStation = this;
		Mouse.Visibility = MouseVisibility.Visible;
	}

	public static void Close()
	{
		ActiveStation = null;
		Mouse.Visibility = MouseVisibility.Hidden;
	}

	public bool IsPlayerInRange()
	{
		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
			return false;

		return Vector3.DistanceBetween( WorldPosition, player.WorldPosition ) <= InteractDistance;
	}

	public int GetEssenceCost( ItemId itemId )
	{
		var def = ItemDatabase.Get( itemId );
		if ( def == null )
			return 0;

		switch ( def.Tier )
		{
			case 1: return RoughEssenceCost;
			case 3: return FineEssenceCost;
			case 5: return PristineEssenceCost;
			default: return RoughEssenceCost;
		}
	}

	public int GetLevelRequired( ItemId itemId )
	{
		var def = ItemDatabase.Get( itemId );
		if ( def == null )
			return 1;

		switch ( def.Tier )
		{
			case 1: return RoughLevelRequired;
			case 3: return FineLevelRequired;
			case 5: return PristineLevelRequired;
			default: return RoughLevelRequired;
		}
	}

	public int GetEnchantXp( ItemId itemId )
	{
		var def = ItemDatabase.Get( itemId );
		if ( def == null )
			return 0;

		switch ( def.Tier )
		{
			case 1: return RoughEnchantXp;
			case 3: return FineEnchantXp;
			case 5: return PristineEnchantXp;
			default: return RoughEnchantXp;
		}
	}

	public int GetCombineXp( ItemId itemId )
	{
		var def = ItemDatabase.Get( itemId );
		if ( def == null )
			return 0;

		switch ( def.Tier )
		{
			case 1: return RoughCombineXp;
			case 3: return FineCombineXp;
			case 5: return PristineCombineXp;
			default: return RoughCombineXp;
		}
	}

	public float GetMaxCap( ItemId itemId )
	{
		var def = ItemDatabase.Get( itemId );
		if ( def == null )
			return 5f;

		switch ( def.Tier )
		{
			case 1: return RoughMaxCap;
			case 3: return FineMaxCap;
			case 5: return PristineMaxCap;
			default: return RoughMaxCap;
		}
	}

	float GetMinPercent( int tier )
	{
		switch ( tier )
		{
			case 1: return RoughMinPercent;
			case 3: return FineMinPercent;
			case 5: return PristineMinPercent;
			default: return RoughMinPercent;
		}
	}

	float GetMaxPercent( int tier )
	{
		switch ( tier )
		{
			case 1: return RoughMaxPercent;
			case 3: return FineMaxPercent;
			case 5: return PristineMaxPercent;
			default: return RoughMaxPercent;
		}
	}

	public bool IsEnchantable( ItemId itemId )
	{
		var def = ItemDatabase.Get( itemId );
		if ( def == null )
			return false;

		return def.Type == ItemType.Ring || def.Type == ItemType.Amulet;
	}

	public bool TryEnchant( ItemId itemId )
	{
		var inventory = GetPlayerInventory();
		var skills = GetPlayerSkills();
		if ( inventory == null || skills == null )
			return false;

		if ( !IsEnchantable( itemId ) )
			return false;

		int levelReq = GetLevelRequired( itemId );
		if ( !skills.MeetsRequirement( SkillType.Enchanting, levelReq ) )
		{
			GameLog.Add( $"You need Enchanting level {levelReq} to enchant this.", "#c86464" );
			return false;
		}

		int essenceCost = GetEssenceCost( itemId );
		if ( !inventory.HasItem( ItemId.ArcaneDust, essenceCost ) )
		{
			GameLog.Add( $"You need {essenceCost} Arcane Dust to enchant this.", "#c86464" );
			return false;
		}

		if ( !inventory.HasItem( itemId, 1 ) )
		{
			GameLog.Add( "You don't have that item.", "#c86464" );
			return false;
		}

		var def = ItemDatabase.Get( itemId );
		float minPct = GetMinPercent( def.Tier );
		float maxPct = GetMaxPercent( def.Tier );

		EnchantmentType[] types = { EnchantmentType.Attack, EnchantmentType.Defence, EnchantmentType.Archery, EnchantmentType.Magic };
		var randomType = types[Random.Shared.Next( types.Length )];
		float randomPercent = (float)Math.Round( minPct + Random.Shared.NextSingle() * ( maxPct - minPct ), 1 );

		inventory.RemoveItem( itemId, 1 );
		inventory.RemoveItem( ItemId.ArcaneDust, essenceCost );

		var instance = new ItemInstance( itemId, randomType, randomPercent );
		bool placedInInv = inventory.AddUniqueItemOrBank( instance );

		if ( !placedInInv )
		{
			var defForLog = ItemDatabase.Get( instance.ItemId );
			string itemName = defForLog != null ? defForLog.Name : instance.ItemId.ToString();
			GameLog.Add( $"Inventory full — {itemName} sent to your bank.", "#c9a84c" );
		}

		skills.AddXp( SkillType.Enchanting, GetEnchantXp( itemId ) );

		string name = def != null ? def.Name : itemId.ToString();
		GameLog.Add( $"Enchanted {name}: +{randomPercent:F1}% {randomType}!", "#a080d0" );

		return true;
	}

	public bool TryCombine( int indexA, int indexB )
	{
		var inventory = GetPlayerInventory();
		var skills = GetPlayerSkills();
		if ( inventory == null || skills == null )
			return false;

		var items = inventory.GetUniqueItems();
		if ( indexA < 0 || indexA >= items.Count || indexB < 0 || indexB >= items.Count || indexA == indexB )
			return false;

		var itemA = items[indexA];
		var itemB = items[indexB];

		if ( !itemA.IsEnchanted || !itemB.IsEnchanted )
		{
			GameLog.Add( "Both items must be enchanted to combine.", "#c86464" );
			return false;
		}

		if ( itemA.ItemId != itemB.ItemId )
		{
			GameLog.Add( "Both items must be the same type to combine.", "#c86464" );
			return false;
		}

		if ( itemA.Enchantment != itemB.Enchantment )
		{
			GameLog.Add( "Both items must have the same enchantment type to combine.", "#c86464" );
			return false;
		}

		float maxCap = GetMaxCap( itemA.ItemId );
		if ( itemA.EnchantmentPercent >= maxCap - 0.05f || itemB.EnchantmentPercent >= maxCap - 0.05f )
		{
			GameLog.Add( "This item is already at maximum enchantment.", "#c86464" );
			return false;
		}

		if ( !inventory.HasItem( ItemId.ArcaneDust, CombineEssenceCost ) )
		{
			GameLog.Add( $"You need {CombineEssenceCost} Arcane Dust to combine.", "#c86464" );
			return false;
		}

		float combined = itemA.EnchantmentPercent + itemB.EnchantmentPercent;
		if ( combined > maxCap )
			combined = maxCap;

		combined = (float)Math.Round( combined, 1 );

		inventory.RemoveItem( ItemId.ArcaneDust, CombineEssenceCost );

		int removeFirst = indexA > indexB ? indexA : indexB;
		int removeSecond = indexA > indexB ? indexB : indexA;
		inventory.RemoveUniqueItem( removeFirst );
		inventory.RemoveUniqueItem( removeSecond );

		var result = new ItemInstance( itemA.ItemId, itemA.Enchantment, combined );
		bool placedInInv = inventory.AddUniqueItemOrBank( result );

		if ( !placedInInv )
		{
			var defForLog = ItemDatabase.Get( result.ItemId );
			string itemName = defForLog != null ? defForLog.Name : result.ItemId.ToString();
			GameLog.Add( $"Inventory full — {itemName} sent to your bank.", "#c9a84c" );
		}

		skills.AddXp( SkillType.Enchanting, GetCombineXp( itemA.ItemId ) );

		var def = ItemDatabase.Get( itemA.ItemId );
		string name = def != null ? def.Name : itemA.ItemId.ToString();
		GameLog.Add( $"Combined into {name}: +{combined:F1}% {itemA.Enchantment}!", "#a080d0" );

		return true;
	}

	public int GetSalvageReturn( ItemId itemId )
	{
		var def = ItemDatabase.Get( itemId );
		if ( def == null )
			return RoughSalvageReturn;

		switch ( def.Tier )
		{
			case 1: return RoughSalvageReturn;
			case 3: return FineSalvageReturn;
			case 5: return PristineSalvageReturn;
			default: return RoughSalvageReturn;
		}
	}

	public bool TrySalvage( int uniqueIndex )
	{
		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		var items = inventory.GetUniqueItems();
		if ( uniqueIndex < 0 || uniqueIndex >= items.Count )
			return false;

		var instance = items[uniqueIndex];

		if ( !instance.IsEnchanted )
		{
			GameLog.Add( "Only enchanted items can be salvaged.", "#c86464" );
			return false;
		}

		int dustReturn = GetSalvageReturn( instance.ItemId );

		inventory.RemoveUniqueItem( uniqueIndex );
		var (placed, banked) = inventory.AddItemOrBank( ItemId.ArcaneDust, dustReturn );

		var def = ItemDatabase.Get( instance.ItemId );
		string name = def != null ? def.Name : instance.ItemId.ToString();
		GameLog.Add( $"Salvaged {name} for {dustReturn} Arcane Dust.", "#a080d0" );

		if ( banked > 0 )
			GameLog.Add( $"Inventory full — {banked}x Arcane Dust sent to your bank.", "#c9a84c" );

		return true;
	}

	Inventory GetPlayerInventory()
	{
		return PlayerHelper.GetLocalInventory();
	}

	Skills GetPlayerSkills()
	{
		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
			return null;

		return player.Components.Get<Skills>();
	}
}