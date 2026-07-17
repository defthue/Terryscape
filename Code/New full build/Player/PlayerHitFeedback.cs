using Sandbox;

public static class PlayerHitFeedback
{
	[Rpc.Broadcast]
	public static void Broadcast( ulong victimSteamId )
	{
		if ( victimSteamId == 0ul )
			return;

		var scene = Game.ActiveScene;
		if ( scene == null )
			return;

		foreach ( var pc in scene.GetAllComponents<PlayerController>() )
		{
			var conn = pc.Network.Owner;
			if ( conn == null || conn.SteamId != victimSteamId )
				continue;

			HitFlash.Trigger( pc.GameObject );
			SoundLibrary.PlayPvpHitLocal( pc.GameObject.WorldPosition );
			return;
		}
	}
}
