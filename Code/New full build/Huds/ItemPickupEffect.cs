using Sandbox;
using System.Collections.Generic;

public static class ItemPickupEffect
{
	public static Dictionary<ItemId, RealTimeSince> RecentItems = new();

	public const float BumpDuration = 0.6f;

	public static void Trigger( ItemId id )
	{
		if ( id == ItemId.None )
			return;

		if ( RecentItems.Count > 0 )
		{
			List<ItemId> stale = null;
			foreach ( var kv in RecentItems )
			{
				if ( (float)kv.Value > BumpDuration )
				{
					stale ??= new List<ItemId>();
					stale.Add( kv.Key );
				}
			}
			if ( stale != null )
			{
				foreach ( var k in stale )
					RecentItems.Remove( k );
			}
		}

		RecentItems[id] = 0f;
	}

	public static bool IsRecentlyReceived( ItemId id )
	{
		if ( !RecentItems.TryGetValue( id, out var since ) )
			return false;

		return (float)since <= BumpDuration;
	}
}
