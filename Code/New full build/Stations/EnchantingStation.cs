using Sandbox;
using System;
using System.Collections.Generic;

public sealed class EnchantingStation : Component
{
	[Property] public string StationName { get; set; } = "Enchanting Altar";
	[Property] public float InteractDistance { get; set; } = 200f;

	[Property] public int RoughCraftLevel { get; set; } = 1;
	[Property] public int FineCraftLevel { get; set; } = 25;
	[Property] public int PristineCraftLevel { get; set; } = 50;

	[Property] public int RoughCraftStone { get; set; } = 5;
	[Property] public int RoughCraftDust { get; set; } = 3;
	[Property] public int FineCraftStone { get; set; } = 10;
	[Property] public int FineCraftDust { get; set; } = 10;
	[Property] public int PristineCraftStone { get; set; } = 20;
	[Property] public int PristineCraftDust { get; set; } = 25;

	[Property] public int RoughCraftXp { get; set; } = 5;
	[Property] public int FineCraftXp { get; set; } = 20;
	[Property] public int PristineCraftXp { get; set; } = 80;

	[Property] public int RoughEnchantDust { get; set; } = 3;
	[Property] public int FineEnchantDust { get; set; } = 8;
	[Property] public int PristineEnchantDust { get; set; } = 20;

	[Property] public int RoughEnchantXp { get; set; } = 10;
	[Property] public int FineEnchantXp { get; set; } = 30;
	[Property] public int PristineEnchantXp { get; set; } = 80;

	[Property] public float RoughMinPercent { get; set; } = 1f;
	[Property] public float RoughMaxPercent { get; set; } = 2f;
	[Property] public float FineMinPercent { get; set; } = 2f;
	[Property] public float FineMaxPercent { get; set; } = 4f;
	[Property] public float PristineMinPercent { get; set; } = 4f;
	[Property] public float PristineMaxPercent { get; set; } = 6f;

	[Property] public float RoughTierCap { get; set; } = 4f;
	[Property] public float FineTierCap { get; set; } = 8f;
	[Property] public float PristineTierCap { get; set; } = 12f;

	[Property] public int RoughCombineDust { get; set; } = 5;
	[Property] public int FineCombineDust { get; set; } = 12;
	[Property] public int PristineCombineDust { get; set; } = 30;

	[Property] public int RoughCombineXp { get; set; } = 15;
	[Property] public int FineCombineXp { get; set; } = 40;
	[Property] public int PristineCombineXp { get; set; } = 100;

	[Property] public float CombineMinBonus { get; set; } = 0.5f;
	[Property] public float CombineMaxBonus { get; set; } = 1.5f;

	[Property] public int TierUpRoughDust { get; set; } = 15;
	[Property] public int TierUpFineDust { get; set; } = 30;
	[Property] public int TierUpFineLevel { get; set; } = 25;
	[Property] public int TierUpPristineLevel { get; set; } = 50;
	[Property] public int TierUpRoughXp { get; set; } = 50;
	[Property] public int TierUpFineXp { get; set; } = 150;

	[Property] public int RoughExtractDust { get; set; } = 20;
	[Property] public int FineExtractDust { get; set; } = 50;
	[Property] public int PristineExtractDust { get; set; } = 150;

	[Property] public int SocketXp { get; set; } = 5;

	public static EnchantingStation ActiveStation { get; private set; }

	static readonly EnchantmentType[] RollableTypes = new[]
	{
		EnchantmentType.Sharpness,
		EnchantmentType.Piercing,
		EnchantmentType.Power,
		EnchantmentType.Toughness,
		EnchantmentType.Vitality,
		EnchantmentType.Focus
	};

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

	public int GetCraftLevel( RuneTier tier )
	{
		switch ( tier )
		{
			case RuneTier.Rough: return RoughCraftLevel;
			case RuneTier.Fine: return FineCraftLevel;
			case RuneTier.Pristine: return PristineCraftLevel;
			default: return 1;
		}
	}

	public int GetCraftStone( RuneTier tier )
	{
		switch ( tier )
		{
			case RuneTier.Rough: return RoughCraftStone;
			case RuneTier.Fine: return FineCraftStone;
			case RuneTier.Pristine: return PristineCraftStone;
			default: return 0;
		}
	}

	public int GetCraftDust( RuneTier tier )
	{
		switch ( tier )
		{
			case RuneTier.Rough: return RoughCraftDust;
			case RuneTier.Fine: return FineCraftDust;
			case RuneTier.Pristine: return PristineCraftDust;
			default: return 0;
		}
	}

	public int GetEnchantDust( RuneTier tier )
	{
		switch ( tier )
		{
			case RuneTier.Rough: return RoughEnchantDust;
			case RuneTier.Fine: return FineEnchantDust;
			case RuneTier.Pristine: return PristineEnchantDust;
			default: return 0;
		}
	}

	public int GetCombineDust( RuneTier tier )
	{
		switch ( tier )
		{
			case RuneTier.Rough: return RoughCombineDust;
			case RuneTier.Fine: return FineCombineDust;
			case RuneTier.Pristine: return PristineCombineDust;
			default: return 0;
		}
	}

	public int GetExtractDust( RuneTier tier )
	{
		switch ( tier )
		{
			case RuneTier.Rough: return RoughExtractDust;
			case RuneTier.Fine: return FineExtractDust;
			case RuneTier.Pristine: return PristineExtractDust;
			default: return 0;
		}
	}

	public float GetTierCap( RuneTier tier )
	{
		switch ( tier )
		{
			case RuneTier.Rough: return RoughTierCap;
			case RuneTier.Fine: return FineTierCap;
			case RuneTier.Pristine: return PristineTierCap;
			default: return 0f;
		}
	}

	public ItemId GetBlankRuneItemId( RuneTier tier )
	{
		switch ( tier )
		{
			case RuneTier.Rough: return ItemId.RoughRune;
			case RuneTier.Fine: return ItemId.FineRune;
			case RuneTier.Pristine: return ItemId.PristineRune;
			default: return ItemId.None;
		}
	}

	public static RuneTier GetRuneTierFromItemId( ItemId id )
	{
		switch ( id )
		{
			case ItemId.RoughRune: return RuneTier.Rough;
			case ItemId.FineRune: return RuneTier.Fine;
			case ItemId.PristineRune: return RuneTier.Pristine;
			default: return RuneTier.None;
		}
	}

	float GetMinPercent( RuneTier tier )
	{
		switch ( tier )
		{
			case RuneTier.Rough: return RoughMinPercent;
			case RuneTier.Fine: return FineMinPercent;
			case RuneTier.Pristine: return PristineMinPercent;
			default: return 0f;
		}
	}

	float GetMaxPercent( RuneTier tier )
	{
		switch ( tier )
		{
			case RuneTier.Rough: return RoughMaxPercent;
			case RuneTier.Fine: return FineMaxPercent;
			case RuneTier.Pristine: return PristineMaxPercent;
			default: return 0f;
		}
	}

	public bool TryCraftBlankRune( RuneTier tier )
	{
		var inventory = GetPlayerInventory();
		var skills = GetPlayerSkills();
		if ( inventory == null || skills == null )
			return false;

		int levelReq = GetCraftLevel( tier );
		if ( !skills.MeetsRequirement( SkillType.Enchanting, levelReq ) )
		{
			GameLog.Add( $"You need Enchanting level {levelReq} to craft this rune.", "#c86464" );
			return false;
		}

		int stoneCost = GetCraftStone( tier );
		int dustCost = GetCraftDust( tier );

		if ( !inventory.HasItem( ItemId.Rock, stoneCost ) )
		{
			GameLog.Add( $"You need {stoneCost} Stone to craft this rune.", "#c86464" );
			return false;
		}
		if ( !inventory.HasItem( ItemId.ArcaneDust, dustCost ) )
		{
			GameLog.Add( $"You need {dustCost} Arcane Dust to craft this rune.", "#c86464" );
			return false;
		}

		var blankId = GetBlankRuneItemId( tier );
		if ( !inventory.CanFitStackable( blankId, 1 ) )
		{
			GameLog.Add( "Not enough inventory space.", "#c86464" );
			return false;
		}

		inventory.RemoveItem( ItemId.Rock, stoneCost );
		inventory.RemoveItem( ItemId.ArcaneDust, dustCost );
		inventory.AddItem( blankId, 1 );

		int xp = tier == RuneTier.Rough ? RoughCraftXp : tier == RuneTier.Fine ? FineCraftXp : PristineCraftXp;
		skills.AddXp( SkillType.Enchanting, xp );

		GameLog.Add( $"Crafted a {tier} Rune.", "#a080d0" );
		return true;
	}

	public bool TryEnchantBlankRune( int slotIndex )
	{
		var inventory = GetPlayerInventory();
		var skills = GetPlayerSkills();
		if ( inventory == null || skills == null )
			return false;

		var slot = inventory.GetSlot( slotIndex );
		if ( slot == null || !slot.IsStack )
		{
			GameLog.Add( "Selected slot is empty.", "#c86464" );
			return false;
		}

		var tier = GetRuneTierFromItemId( slot.ItemId );
		if ( tier == RuneTier.None )
		{
			GameLog.Add( "That isn't a blank rune.", "#c86464" );
			return false;
		}

		int dustCost = GetEnchantDust( tier );
		if ( !inventory.HasItem( ItemId.ArcaneDust, dustCost ) )
		{
			GameLog.Add( $"You need {dustCost} Arcane Dust to enchant this rune.", "#c86464" );
			return false;
		}

		float minPct = GetMinPercent( tier );
		float maxPct = GetMaxPercent( tier );

		var randomType = RollableTypes[Random.Shared.Next( RollableTypes.Length )];
		float randomPercent = (float)Math.Round( minPct + Random.Shared.NextSingle() * ( maxPct - minPct ), 1 );

		inventory.RemoveItem( ItemId.ArcaneDust, dustCost );
		inventory.RemoveFromSlot( slotIndex, 1 );

		var enchantedRune = new ItemInstance( GetBlankRuneItemId( tier ), randomType, randomPercent );
		bool placed = inventory.AddUniqueItemOrBank( enchantedRune );

		if ( !placed )
			GameLog.Add( $"Inventory full — enchanted {tier} Rune sent to your bank.", "#c9a84c" );

		int xp = tier == RuneTier.Rough ? RoughEnchantXp : tier == RuneTier.Fine ? FineEnchantXp : PristineEnchantXp;
		skills.AddXp( SkillType.Enchanting, xp );

		GameLog.Add( $"Enchanted {tier} Rune: +{randomPercent:F1}% {randomType}!", "#a080d0" );
		return true;
	}

	public bool TryCombineSame( int slotA, int slotB )
	{
		var inventory = GetPlayerInventory();
		var skills = GetPlayerSkills();
		if ( inventory == null || skills == null )
			return false;

		var aSlot = inventory.GetSlot( slotA );
		var bSlot = inventory.GetSlot( slotB );
		if ( aSlot == null || bSlot == null || !aSlot.IsUnique || !bSlot.IsUnique || slotA == slotB )
		{
			GameLog.Add( "Select two enchanted runes.", "#c86464" );
			return false;
		}

		var a = aSlot.Unique;
		var b = bSlot.Unique;
		if ( !a.IsRune || !b.IsRune || !a.IsEnchanted || !b.IsEnchanted )
		{
			GameLog.Add( "Both must be enchanted runes.", "#c86464" );
			return false;
		}

		if ( a.RuneTier != b.RuneTier )
		{
			GameLog.Add( "Runes must be the same tier.", "#c86464" );
			return false;
		}
		if ( a.Enchantment != b.Enchantment )
		{
			GameLog.Add( "Runes must have the same enchantment.", "#c86464" );
			return false;
		}

		float cap = GetTierCap( a.RuneTier );
		if ( a.EnchantmentPercent >= cap - 0.0001f || b.EnchantmentPercent >= cap - 0.0001f )
		{
			GameLog.Add( "Rune is already at max — combining would have no effect.", "#c86464" );
			return false;
		}

		int dustCost = GetCombineDust( a.RuneTier );
		if ( !inventory.HasItem( ItemId.ArcaneDust, dustCost ) )
		{
			GameLog.Add( $"You need {dustCost} Arcane Dust to combine.", "#c86464" );
			return false;
		}

		float bonus = CombineMinBonus + Random.Shared.NextSingle() * ( CombineMaxBonus - CombineMinBonus );
		float result = MathF.Max( a.EnchantmentPercent, b.EnchantmentPercent ) + bonus;
		if ( result > cap ) result = cap;
		result = (float)Math.Round( result, 1 );

		inventory.RemoveItem( ItemId.ArcaneDust, dustCost );

		int removeFirst = slotA > slotB ? slotA : slotB;
		int removeSecond = slotA > slotB ? slotB : slotA;
		inventory.RemoveFromSlot( removeFirst, 1 );
		inventory.RemoveFromSlot( removeSecond, 1 );

		var combined = new ItemInstance( GetBlankRuneItemId( a.RuneTier ), a.Enchantment, result );
		bool placed = inventory.AddUniqueItemOrBank( combined );

		if ( !placed )
			GameLog.Add( $"Inventory full — combined rune sent to your bank.", "#c9a84c" );

		int xp = a.RuneTier == RuneTier.Rough ? RoughCombineXp : a.RuneTier == RuneTier.Fine ? FineCombineXp : PristineCombineXp;
		skills.AddXp( SkillType.Enchanting, xp );

		GameLog.Add( $"Combined into +{result:F1}% {a.Enchantment} {a.RuneTier} Rune.", "#a080d0" );
		return true;
	}

	public bool TryTierUp( int slotA, int slotB, int slotC )
	{
		var inventory = GetPlayerInventory();
		var skills = GetPlayerSkills();
		if ( inventory == null || skills == null )
			return false;

		var sA = inventory.GetSlot( slotA );
		var sB = inventory.GetSlot( slotB );
		var sC = inventory.GetSlot( slotC );
		if ( sA == null || sB == null || sC == null || slotA == slotB || slotA == slotC || slotB == slotC )
		{
			GameLog.Add( "Select three distinct runes.", "#c86464" );
			return false;
		}
		if ( !sA.IsUnique || !sB.IsUnique || !sC.IsUnique )
		{
			GameLog.Add( "Tier-up requires three enchanted runes.", "#c86464" );
			return false;
		}

		var a = sA.Unique;
		var b = sB.Unique;
		var c = sC.Unique;
		if ( !a.IsRune || !b.IsRune || !c.IsRune || !a.IsEnchanted || !b.IsEnchanted || !c.IsEnchanted )
		{
			GameLog.Add( "Tier-up requires three enchanted runes.", "#c86464" );
			return false;
		}

		var sourceTier = a.RuneTier;
		if ( b.RuneTier != sourceTier || c.RuneTier != sourceTier )
		{
			GameLog.Add( "All three runes must be the same tier.", "#c86464" );
			return false;
		}
		if ( sourceTier == RuneTier.Pristine )
		{
			GameLog.Add( "Pristine runes are the highest tier.", "#c86464" );
			return false;
		}

		var resultTier = sourceTier == RuneTier.Rough ? RuneTier.Fine : RuneTier.Pristine;
		int levelReq = resultTier == RuneTier.Fine ? TierUpFineLevel : TierUpPristineLevel;
		int dustCost = resultTier == RuneTier.Fine ? TierUpRoughDust : TierUpFineDust;
		int xp = resultTier == RuneTier.Fine ? TierUpRoughXp : TierUpFineXp;

		if ( !skills.MeetsRequirement( SkillType.Enchanting, levelReq ) )
		{
			GameLog.Add( $"You need Enchanting level {levelReq} to tier up.", "#c86464" );
			return false;
		}
		if ( !inventory.HasItem( ItemId.ArcaneDust, dustCost ) )
		{
			GameLog.Add( $"You need {dustCost} Arcane Dust to tier up.", "#c86464" );
			return false;
		}

		var resultId = GetBlankRuneItemId( resultTier );
		if ( !inventory.CanFitStackable( resultId, 1 ) )
		{
			GameLog.Add( "Not enough inventory space.", "#c86464" );
			return false;
		}

		inventory.RemoveItem( ItemId.ArcaneDust, dustCost );

		var sortedIndices = new int[] { slotA, slotB, slotC };
		Array.Sort( sortedIndices );
		for ( int i = sortedIndices.Length - 1; i >= 0; i-- )
			inventory.RemoveFromSlot( sortedIndices[i], 1 );

		inventory.AddItem( resultId, 1 );
		skills.AddXp( SkillType.Enchanting, xp );

		GameLog.Add( $"Tiered up three {sourceTier} Runes into a {resultTier} Rune.", "#a080d0" );
		return true;
	}

	public bool TrySocketRune( int itemSlotIndex, int socketIndex, int runeSlotIndex )
	{
		var inventory = GetPlayerInventory();
		var skills = GetPlayerSkills();
		if ( inventory == null || skills == null )
			return false;

		var itemSlot = inventory.GetSlot( itemSlotIndex );
		var runeSlot = inventory.GetSlot( runeSlotIndex );
		if ( itemSlot == null || runeSlot == null )
			return false;
		if ( !itemSlot.IsUnique || !runeSlot.IsUnique )
		{
			GameLog.Add( "Select a ring or amulet and an enchanted rune.", "#c86464" );
			return false;
		}

		var jewelry = itemSlot.Unique;
		var rune = runeSlot.Unique;

		if ( !jewelry.IsSocketable )
		{
			GameLog.Add( "Only rings and amulets can be socketed.", "#c86464" );
			return false;
		}
		if ( !rune.IsRune || !rune.IsEnchanted )
		{
			GameLog.Add( "Only enchanted runes can be socketed.", "#c86464" );
			return false;
		}
		if ( socketIndex < 0 || socketIndex >= jewelry.MaxSockets )
		{
			GameLog.Add( "Invalid socket.", "#c86464" );
			return false;
		}
		if ( jewelry.GetSocket( socketIndex ) != null )
		{
			GameLog.Add( "That socket is already filled.", "#c86464" );
			return false;
		}
		if ( jewelry.HasEnchantmentInSocket( rune.Enchantment ) )
		{
			GameLog.Add( $"This item already has {rune.Enchantment} socketed.", "#c86464" );
			return false;
		}

		jewelry.SetSocket( socketIndex, new ItemInstance( rune.ItemId, rune.Enchantment, rune.EnchantmentPercent ) );
		inventory.RemoveFromSlot( runeSlotIndex, 1 );

		skills.AddXp( SkillType.Enchanting, SocketXp );

		GameLog.Add( $"Socketed +{rune.EnchantmentPercent:F1}% {rune.Enchantment} into {jewelry.GetDisplayName()}.", "#a080d0" );
		return true;
	}

	public bool TryExtractRune( int itemSlotIndex, int socketIndex )
	{
		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		var itemSlot = inventory.GetSlot( itemSlotIndex );
		if ( itemSlot == null || !itemSlot.IsUnique )
			return false;

		var jewelry = itemSlot.Unique;
		if ( !jewelry.IsSocketable )
			return false;

		var rune = jewelry.GetSocket( socketIndex );
		if ( rune == null )
		{
			GameLog.Add( "That socket is empty.", "#c86464" );
			return false;
		}

		int dustCost = GetExtractDust( rune.RuneTier );
		if ( !inventory.HasItem( ItemId.ArcaneDust, dustCost ) )
		{
			GameLog.Add( $"You need {dustCost} Arcane Dust to extract this rune.", "#c86464" );
			return false;
		}

		inventory.RemoveItem( ItemId.ArcaneDust, dustCost );
		jewelry.SetSocket( socketIndex, null );

		var extracted = new ItemInstance( rune.ItemId, rune.Enchantment, rune.EnchantmentPercent );
		bool placed = inventory.AddUniqueItemOrBank( extracted );

		if ( !placed )
			GameLog.Add( "Inventory full — extracted rune sent to your bank.", "#c9a84c" );

		GameLog.Add( $"Extracted +{rune.EnchantmentPercent:F1}% {rune.Enchantment} rune.", "#a080d0" );
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
