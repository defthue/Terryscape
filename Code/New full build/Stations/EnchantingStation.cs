using Sandbox;
using System;

public sealed class EnchantingStation : Component
{
	[Property] public string StationName { get; set; } = "Enchanting Altar";
	[Property] public float InteractDistance { get; set; } = 200f;

	[Property] public int CraftStoneCost { get; set; } = 5;
	[Property] public int CraftDustCost { get; set; } = 5;
	[Property] public int CraftXp { get; set; } = 10;

	[Property] public int EnchantRandomDustCost { get; set; } = 10;
	[Property] public int EnchantTargetedDustCost { get; set; } = 30;
	[Property] public int EnchantXp { get; set; } = 15;

	[Property] public int CombineDustCost { get; set; } = 15;
	[Property] public int CombineXp { get; set; } = 50;
	[Property] public int CombineUnlockLevel { get; set; } = 20;
	[Property] public int CombineCapLevelTier2 { get; set; } = 30;
	[Property] public int CombineCapLevelTier3 { get; set; } = 40;
	[Property] public float CombineCapTier1 { get; set; } = 6f;
	[Property] public float CombineCapTier2 { get; set; } = 9f;
	[Property] public float CombineCapTier3 { get; set; } = 12f;
	[Property] public float CombineLowerContribution { get; set; } = 0.3f;

	[Property] public int SocketXp { get; set; } = 5;
	[Property] public int SecondSocketUnlockLevel { get; set; } = 40;

	[Property] public int ExtractDustCost { get; set; } = 75;

	[Property] public int RenameDustCost { get; set; } = 1;

	[Property] public int SkillCap { get; set; } = 50;

	public const string RuneRecipeId = "rune";
	public const int MaxCustomNameLength = 24;

	public static EnchantingStation ActiveStation { get; private set; }

	public int LastCombineResultSlot { get; private set; } = -1;

	public static readonly EnchantmentType[] EnchantTypes = new[]
	{
		EnchantmentType.Sharpness,
		EnchantmentType.Piercing,
		EnchantmentType.Arcana,
		EnchantmentType.Toughness,
		EnchantmentType.Vitality,
		EnchantmentType.Focus
	};

	protected override void OnUpdate()
	{
		if ( ActiveStation == this )
		{
			if ( !IsPlayerInRange() )
				Close();
			return;
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

	public float GetMinRollPercent( int level )
	{
		int clamped = Math.Clamp( level, 1, SkillCap );
		return 1.0f + ( clamped - 1 ) * 0.04f;
	}

	public float GetMaxRollPercent( int level )
	{
		int clamped = Math.Clamp( level, 1, SkillCap );
		return 2.0f + ( clamped - 1 ) * 0.10f;
	}

	public float GetCombineCap( int level )
	{
		if ( level < CombineUnlockLevel ) return 0f;
		if ( level < CombineCapLevelTier2 ) return CombineCapTier1;
		if ( level < CombineCapLevelTier3 ) return CombineCapTier2;
		return CombineCapTier3;
	}

	public int GetNextCombineCapLevel( int level )
	{
		if ( level < CombineUnlockLevel ) return CombineUnlockLevel;
		if ( level < CombineCapLevelTier2 ) return CombineCapLevelTier2;
		if ( level < CombineCapLevelTier3 ) return CombineCapLevelTier3;
		return -1;
	}

	public float GetNextCombineCapValue( int level )
	{
		if ( level < CombineUnlockLevel ) return CombineCapTier1;
		if ( level < CombineCapLevelTier2 ) return CombineCapTier2;
		if ( level < CombineCapLevelTier3 ) return CombineCapTier3;
		return CombineCapTier3;
	}

	public float PreviewCombineResult( float pctA, float pctB, int level, out float waste )
	{
		float cap = GetCombineCap( level );
		float hi = MathF.Max( pctA, pctB );
		float lo = MathF.Min( pctA, pctB );
		float raw = hi + lo * CombineLowerContribution;
		float result = (float)Math.Round( MathF.Min( raw, cap ), 1 );
		waste = MathF.Max( 0f, (float)Math.Round( raw - cap, 1 ) );
		return result;
	}

	public int GetMaxSocketsForLevel( int level )
	{
		return level >= SecondSocketUnlockLevel ? 2 : 1;
	}

	public bool TryCraftRune()
	{
		var ctx = GetContext();
		if ( ctx == null ) return false;

		if ( !ctx.Inventory.IsRecipeUnlocked( RuneRecipeId ) )
		{
			GameLog.Add( "You haven't learned how to craft runes yet.", "#c86464" );
			return false;
		}
		if ( !ctx.Inventory.HasItem( ItemId.Rock, CraftStoneCost ) )
		{
			GameLog.Add( $"You need {CraftStoneCost} Stone to craft a rune.", "#c86464" );
			return false;
		}
		if ( !ctx.Inventory.HasItem( ItemId.ArcaneDust, CraftDustCost ) )
		{
			GameLog.Add( $"You need {CraftDustCost} Arcane Dust to craft a rune.", "#c86464" );
			return false;
		}
		if ( !ctx.Inventory.CanFitStackable( ItemId.Rune, 1 ) )
		{
			GameLog.Add( "Not enough inventory space.", "#c86464" );
			return false;
		}

		ctx.Inventory.RemoveItem( ItemId.Rock, CraftStoneCost );
		ctx.Inventory.RemoveItem( ItemId.ArcaneDust, CraftDustCost );
		ctx.Inventory.AddItem( ItemId.Rune, 1 );
		ctx.Skills.AddXp( SkillType.Enchanting, CraftXp );

		GameLog.Add( "Crafted a Rune.", "#a080d0" );
		return true;
	}

	public bool TryEnchantRandom( int runeSlotIndex )
	{
		return DoEnchant( runeSlotIndex, EnchantmentType.None, false );
	}

	public bool TryEnchantTargeted( int runeSlotIndex, EnchantmentType type )
	{
		if ( type == EnchantmentType.None )
		{
			GameLog.Add( "Pick an enchantment to target.", "#c86464" );
			return false;
		}
		return DoEnchant( runeSlotIndex, type, true );
	}

	bool DoEnchant( int runeSlotIndex, EnchantmentType targetedType, bool isTargeted )
	{
		var ctx = GetContext();
		if ( ctx == null ) return false;

		var slot = ctx.Inventory.GetSlot( runeSlotIndex );
		if ( slot == null || !slot.IsStack || slot.ItemId != ItemId.Rune )
		{
			GameLog.Add( "Select a blank rune.", "#c86464" );
			return false;
		}

		int dustCost = isTargeted ? EnchantTargetedDustCost : EnchantRandomDustCost;
		if ( !ctx.Inventory.HasItem( ItemId.ArcaneDust, dustCost ) )
		{
			GameLog.Add( $"You need {dustCost} Arcane Dust to enchant this rune.", "#c86464" );
			return false;
		}

		int level = ctx.Skills.GetLevel( SkillType.Enchanting );
		float minPct = GetMinRollPercent( level );
		float maxPct = GetMaxRollPercent( level );

		EnchantmentType chosen = isTargeted ? targetedType : EnchantTypes[Random.Shared.Next( EnchantTypes.Length )];
		float rolled = (float)Math.Round( minPct + Random.Shared.NextSingle() * ( maxPct - minPct ), 1 );

		ctx.Inventory.RemoveItem( ItemId.ArcaneDust, dustCost );
		ctx.Inventory.RemoveFromSlot( runeSlotIndex, 1 );

		var enchanted = new ItemInstance( ItemId.Rune, chosen, rolled );
		if ( !ctx.Inventory.AddUniqueItemOrBank( enchanted ) )
			GameLog.Add( "Inventory full — enchanted rune sent to your bank.", "#c9a84c" );

		ctx.Skills.AddXp( SkillType.Enchanting, EnchantXp );
		GameLog.Add( $"Enchanted Rune: +{rolled:F1}% {chosen}!", "#a080d0" );
		return true;
	}

	public bool TryCombine( int slotA, int slotB )
	{
		LastCombineResultSlot = -1;

		var ctx = GetContext();
		if ( ctx == null ) return false;

		int level = ctx.Skills.GetLevel( SkillType.Enchanting );
		if ( level < CombineUnlockLevel )
		{
			GameLog.Add( $"Combine unlocks at Enchanting level {CombineUnlockLevel}.", "#c86464" );
			return false;
		}
		if ( slotA == slotB )
		{
			GameLog.Add( "Select two different runes.", "#c86464" );
			return false;
		}

		var aSlot = ctx.Inventory.GetSlot( slotA );
		var bSlot = ctx.Inventory.GetSlot( slotB );
		if ( aSlot == null || bSlot == null || !aSlot.IsUnique || !bSlot.IsUnique )
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
		if ( a.Enchantment != b.Enchantment )
		{
			GameLog.Add( "Runes must have the same enchantment.", "#c86464" );
			return false;
		}

		float cap = GetCombineCap( level );
		if ( a.EnchantmentPercent >= cap - 0.0001f && b.EnchantmentPercent >= cap - 0.0001f )
		{
			GameLog.Add( "Both runes are already at cap.", "#c86464" );
			return false;
		}
		if ( !ctx.Inventory.HasItem( ItemId.ArcaneDust, CombineDustCost ) )
		{
			GameLog.Add( $"You need {CombineDustCost} Arcane Dust to combine.", "#c86464" );
			return false;
		}

		float hi = MathF.Max( a.EnchantmentPercent, b.EnchantmentPercent );
		float lo = MathF.Min( a.EnchantmentPercent, b.EnchantmentPercent );
		float result = (float)Math.Round( MathF.Min( hi + lo * CombineLowerContribution, cap ), 1 );

		ctx.Inventory.RemoveItem( ItemId.ArcaneDust, CombineDustCost );

		int hiSlot = slotA > slotB ? slotA : slotB;
		int loSlot = slotA > slotB ? slotB : slotA;
		ctx.Inventory.RemoveFromSlot( hiSlot, 1 );
		ctx.Inventory.RemoveFromSlot( loSlot, 1 );

		var combined = new ItemInstance( ItemId.Rune, a.Enchantment, result );
		if ( !ctx.Inventory.AddUniqueItemOrBank( combined ) )
			GameLog.Add( "Inventory full — combined rune sent to your bank.", "#c9a84c" );

		for ( int i = 0; i < ctx.Inventory.MaxSlots; i++ )
		{
			var s = ctx.Inventory.GetSlot( i );
			if ( s != null && s.IsUnique && ReferenceEquals( s.Unique, combined ) )
			{
				LastCombineResultSlot = i;
				break;
			}
		}

		ctx.Skills.AddXp( SkillType.Enchanting, CombineXp );
		GameLog.Add( $"Combined into +{result:F1}% {a.Enchantment} Rune.", "#a080d0" );
		return true;
	}

	public bool TrySocket( int jewelrySlotIndex, int socketIndex, int runeSlotIndex )
	{
		var ctx = GetContext();
		if ( ctx == null ) return false;

		int level = ctx.Skills.GetLevel( SkillType.Enchanting );
		if ( socketIndex >= GetMaxSocketsForLevel( level ) )
		{
			GameLog.Add( $"Second socket unlocks at Enchanting level {SecondSocketUnlockLevel}.", "#c86464" );
			return false;
		}

		var jSlot = ctx.Inventory.GetSlot( jewelrySlotIndex );
		var rSlot = ctx.Inventory.GetSlot( runeSlotIndex );
		if ( jSlot == null || rSlot == null || !jSlot.IsUnique || !rSlot.IsUnique )
		{
			GameLog.Add( "Select a ring or amulet and an enchanted rune.", "#c86464" );
			return false;
		}

		var jewelry = jSlot.Unique;
		var rune = rSlot.Unique;
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
		ctx.Inventory.RemoveFromSlot( runeSlotIndex, 1 );
		ctx.Skills.AddXp( SkillType.Enchanting, SocketXp );

		GameLog.Add( $"Socketed +{rune.EnchantmentPercent:F1}% {rune.Enchantment} into {jewelry.GetDisplayName()}.", "#a080d0" );
		return true;
	}

	public bool TryExtract( int jewelrySlotIndex, int socketIndex )
	{
		var ctx = GetContext();
		if ( ctx == null ) return false;

		var jSlot = ctx.Inventory.GetSlot( jewelrySlotIndex );
		if ( jSlot == null || !jSlot.IsUnique )
			return false;

		var jewelry = jSlot.Unique;
		if ( !jewelry.IsSocketable )
			return false;

		var rune = jewelry.GetSocket( socketIndex );
		if ( rune == null )
		{
			GameLog.Add( "That socket is empty.", "#c86464" );
			return false;
		}
		if ( !ctx.Inventory.HasItem( ItemId.ArcaneDust, ExtractDustCost ) )
		{
			GameLog.Add( $"You need {ExtractDustCost} Arcane Dust to extract this rune.", "#c86464" );
			return false;
		}

		ctx.Inventory.RemoveItem( ItemId.ArcaneDust, ExtractDustCost );
		jewelry.SetSocket( socketIndex, null );

		var extracted = new ItemInstance( rune.ItemId, rune.Enchantment, rune.EnchantmentPercent );
		if ( !ctx.Inventory.AddUniqueItemOrBank( extracted ) )
			GameLog.Add( "Inventory full — extracted rune sent to your bank.", "#c9a84c" );

		GameLog.Add( $"Extracted +{rune.EnchantmentPercent:F1}% {rune.Enchantment} rune.", "#a080d0" );
		return true;
	}

	public bool TryRename( int jewelrySlotIndex, string newName )
	{
		var ctx = GetContext();
		if ( ctx == null ) return false;

		var jSlot = ctx.Inventory.GetSlot( jewelrySlotIndex );
		if ( jSlot == null || !jSlot.IsUnique )
		{
			GameLog.Add( "Select a ring or amulet to rename.", "#c86464" );
			return false;
		}

		var jewelry = jSlot.Unique;
		if ( !jewelry.IsSocketable )
		{
			GameLog.Add( "Only rings and amulets can be renamed.", "#c86464" );
			return false;
		}

		string trimmed = newName?.Trim() ?? "";
		if ( trimmed.Length == 0 )
		{
			GameLog.Add( "Enter a name first.", "#c86464" );
			return false;
		}
		if ( trimmed.Length > MaxCustomNameLength )
		{
			GameLog.Add( $"Names can be at most {MaxCustomNameLength} characters.", "#c86464" );
			return false;
		}
		if ( !NameFilter.IsAllowed( trimmed ) )
		{
			GameLog.Add( "That name is not allowed.", "#c86464" );
			return false;
		}
		if ( !ctx.Inventory.HasItem( ItemId.ArcaneDust, RenameDustCost ) )
		{
			GameLog.Add( $"You need {RenameDustCost} Arcane Dust to rename.", "#c86464" );
			return false;
		}

		ctx.Inventory.RemoveItem( ItemId.ArcaneDust, RenameDustCost );
		jewelry.CustomName = trimmed;
		PlayerPersistence.Local?.MarkDirty( SaveSection.Inventory );

		GameLog.Add( $"Renamed to \"{trimmed}\".", "#a080d0" );
		return true;
	}

	class Context
	{
		public Inventory Inventory;
		public Skills Skills;
	}

	Context GetContext()
	{
		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null ) return null;

		var inv = player.Components.Get<Inventory>();
		var skills = player.Components.Get<Skills>();
		if ( inv == null || skills == null ) return null;

		return new Context { Inventory = inv, Skills = skills };
	}
}