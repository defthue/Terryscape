using Sandbox;
using System;
using System.Collections.Generic;

public static class PlayerHelper
{
	public static bool IsLocalPlayer( GameObject go )
	{
		if ( go == null )
			return false;

		var current = go;
		while ( current != null && current is not Scene )
		{
			var pc = current.Components.Get<PlayerController>();
			if ( pc != null )
				return !pc.IsProxy;

			current = current.Parent;
		}

		return !go.IsProxy;
	}

	public static GameObject GetLocalPlayer()
	{
		foreach ( var pc in Game.ActiveScene.GetAllComponents<PlayerController>() )
		{
			if ( !pc.IsProxy )
				return pc.GameObject;
		}

		return null;
	}

	public static GameObject GetNearestPlayer( Vector3 position )
	{
		GameObject closest = null;
		float closestDist = float.MaxValue;

		foreach ( var pc in Game.ActiveScene.GetAllComponents<PlayerController>() )
		{
			float dist = Vector3.DistanceBetween( position, pc.WorldPosition );
			if ( dist < closestDist )
			{
				closestDist = dist;
				closest = pc.GameObject;
			}
		}

		return closest;
	}

	public static float GetDistanceToNearestPlayer( Vector3 position )
	{
		float closestDist = float.MaxValue;

		foreach ( var pc in Game.ActiveScene.GetAllComponents<PlayerController>() )
		{
			float dist = Vector3.DistanceBetween( position, pc.WorldPosition );
			if ( dist < closestDist )
				closestDist = dist;
		}

		return closestDist;
	}

	public static List<GameObject> GetAllPlayers()
	{
		var players = new List<GameObject>();

		foreach ( var pc in Game.ActiveScene.GetAllComponents<PlayerController>() )
			players.Add( pc.GameObject );

		return players;
	}

	public static Inventory GetLocalInventory()
	{
		var player = GetLocalPlayer();
		if ( player == null )
			return null;

		return player.Components.Get<Inventory>();
	}

	public static Inventory GetInventory( GameObject player )
	{
		if ( player == null )
			return null;

		return player.Components.Get<Inventory>();
	}

	public static Skills GetSkills( GameObject player )
	{
		if ( player == null )
			return null;

		return player.Components.Get<Skills>();
	}
}