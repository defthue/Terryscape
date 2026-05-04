using Sandbox;
using System.Collections.Generic;

public sealed class CraftingStation : Component
{
	[Property] public StationType Station { get; set; }
	[Property] public string StationName { get; set; } = "Crafting Station";
	[Property] public float InteractDistance { get; set; } = 200f;

	public static CraftingStation ActiveStation { get; private set; }

	protected override void OnUpdate()
	{
		if ( ActiveStation == this )
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
		else if ( ActiveStation == null )
		{
			if ( NpcInteract.ActiveNpc != null )
				return;

			if ( ShopStation.ActiveShop != null || ShopStation.ShowingChoice )
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

			Open();
		}
	}

	void Open()
	{
		ActiveStation = this;
		Mouse.Visibility = MouseVisibility.Visible;

		if ( Station == StationType.Furnace )
			SoundLibrary.StartFurnaceLoop();
	}

	public static void Close()
	{
		if ( ActiveStation != null && ActiveStation.Station == StationType.Furnace )
			SoundLibrary.StopFurnaceLoop();

		ActiveStation = null;
		Mouse.Visibility = MouseVisibility.Hidden;
	}

	public bool IsPlayerInRange()
	{
		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
			return false;

		var distance = Vector3.DistanceBetween( WorldPosition, player.WorldPosition );
		return distance <= InteractDistance;
	}

	public List<RecipeDefinition> GetAvailableRecipes()
	{
		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return new List<RecipeDefinition>();

		var recipes = new List<RecipeDefinition>();

		foreach ( var recipe in RecipeDatabase.GetByStation( Station ) )
		{
			if ( inventory.IsRecipeUnlocked( recipe.Id ) )
				recipes.Add( recipe );
		}

		return recipes;
	}

	public bool TryCraft( string recipeId )
	{
		return TryCraftInternal( recipeId, false );
	}

	public int TryCraftAll( string recipeId )
	{
		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return 0;

		var recipe = RecipeDatabase.GetById( recipeId );
		if ( recipe == null )
		{
			GameLog.Add( "Unknown recipe.", "#c86464" );
			return 0;
		}

		int crafted = 0;
		int totalOutput = 0;

		while ( TryCraftInternal( recipeId, true ) )
		{
			crafted++;
			totalOutput += recipe.OutputAmount;
		}

		if ( crafted == 0 )
			return 0;

		var outputDef = ItemDatabase.Get( recipe.OutputItem );
		string outputName = outputDef != null ? outputDef.Name : recipe.OutputItem.ToString();

		GameLog.Add( $"You crafted {totalOutput}x {outputName}!", "#4caf78" );

		switch ( Station )
		{
			case StationType.Workbench: SoundLibrary.PlayWorkbenchCraft(); break;
			case StationType.Anvil: SoundLibrary.PlayAnvilCraft(); break;
			case StationType.Furnace: SoundLibrary.PlayUseFurnace(); break;
		}

		return crafted;
	}

	bool TryCraftInternal( string recipeId, bool silent )
	{
		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return false;

		var recipe = RecipeDatabase.GetById( recipeId );
		if ( recipe == null )
		{
			if ( !silent )
				GameLog.Add( "Unknown recipe.", "#c86464" );
			return false;
		}

		if ( recipe.Station != Station )
		{
			if ( !silent )
				GameLog.Add( "This recipe requires a different crafting station.", "#c86464" );
			return false;
		}

		if ( !inventory.IsRecipeUnlocked( recipeId ) )
		{
			if ( !silent )
				GameLog.Add( "You haven't unlocked this recipe yet.", "#c86464" );
			return false;
		}

		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
			return false;

		var skills = player.Components.Get<Skills>();
		if ( skills == null )
			return false;

		if ( !skills.MeetsRequirement( recipe.SkillRequired, recipe.LevelRequired ) )
		{
			if ( !silent )
				GameLog.Add( $"You need {recipe.SkillRequired} level {recipe.LevelRequired} to craft {recipe.Name}.", "#c86464" );
			return false;
		}

		if ( !inventory.HasIngredients( recipe ) )
		{
			if ( !silent )
				GameLog.Add( $"You don't have the required materials for {recipe.Name}.", "#c86464" );
			return false;
		}

		inventory.RemoveIngredients( recipe );
		inventory.AddItem( recipe.OutputItem, recipe.OutputAmount );

		var outputDef = ItemDatabase.Get( recipe.OutputItem );
		string outputName = outputDef != null ? outputDef.Name : recipe.OutputItem.ToString();

		if ( !silent )
		{
			if ( recipe.OutputAmount > 1 )
				GameLog.Add( $"You crafted {recipe.OutputAmount}x {outputName}!", "#4caf78" );
			else
				GameLog.Add( $"You crafted {outputName}!", "#4caf78" );
		}

		int xpAward = recipe.XpReward;
		if ( recipe.XpSkill == SkillType.Crafting )
			xpAward = (int)System.Math.Ceiling( xpAward * 1.25f );

		skills.AddXp( recipe.XpSkill, xpAward );

		if ( !silent )
		{
			switch ( Station )
			{
				case StationType.Workbench: SoundLibrary.PlayWorkbenchCraft(); break;
				case StationType.Anvil: SoundLibrary.PlayAnvilCraft(); break;
				case StationType.Furnace: SoundLibrary.PlayUseFurnace(); break;
			}
		}

		return true;
	}

	Inventory GetPlayerInventory()
	{
		return PlayerHelper.GetLocalInventory();
	}
}