using Sandbox;

public static class InteractPriority
{
	public static bool StationWantsUse()
	{
		if ( AnyStationUiOpen() )
			return true;

		var scene = Game.ActiveScene;
		if ( scene == null )
			return false;

		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
			return false;

		var playerPos = player.WorldPosition;

		foreach ( var s in scene.GetAllComponents<CraftingStation>() )
			if ( s.IsValid() && s.IsPlayerInRange() )
				return true;

		foreach ( var s in scene.GetAllComponents<BankStation>() )
			if ( s.IsValid() && s.IsPlayerInRange() )
				return true;

		foreach ( var s in scene.GetAllComponents<ShopStation>() )
			if ( s.IsValid() && s.IsPlayerInRange() )
				return true;

		foreach ( var s in scene.GetAllComponents<EnchantingStation>() )
			if ( s.IsValid() && s.IsPlayerInRange() )
				return true;

		foreach ( var s in scene.GetAllComponents<TeleportStone>() )
			if ( s.IsValid() && s.IsPlayerInRange() )
				return true;

		foreach ( var s in scene.GetAllComponents<DailyChest>() )
			if ( s.IsValid() && s.IsPlayerInRange() )
				return true;

		foreach ( var s in scene.GetAllComponents<DuelMaster>() )
			if ( s.IsValid() && Vector3.DistanceBetween( s.WorldPosition, playerPos ) <= s.InteractDistance )
				return true;

		foreach ( var n in scene.GetAllComponents<NpcInteract>() )
		{
			if ( !n.IsValid() )
				continue;

			if ( Vector3.DistanceBetween( n.WorldPosition, playerPos ) > n.InteractDistance )
				continue;

			if ( n.Components.Get<ShopStation>() != null || NpcInteract.NpcHasAvailableQuest( n.GameObject ) )
				return true;
		}

		return false;
	}

	static bool AnyStationUiOpen()
	{
		if ( CraftingStation.ActiveStation != null ) return true;
		if ( ShopStation.ActiveShop != null || ShopStation.ShowingChoice ) return true;
		if ( BankStation.ActiveBank != null ) return true;
		if ( TeleportStone.ActiveStone != null ) return true;
		if ( EnchantingStation.ActiveStation != null ) return true;
		if ( NpcInteract.ActiveNpc != null ) return true;
		if ( JournalStation.IsOpen ) return true;
		if ( LeaderboardStation.IsOpen ) return true;
		if ( SpellbookStation.IsOpen ) return true;
		if ( DailyChest.RewardHudOpen ) return true;
		if ( DuelMaster.IsOpen ) return true;
		if ( DuelManager.LocalDuelUiOpen ) return true;
		return false;
	}
}
