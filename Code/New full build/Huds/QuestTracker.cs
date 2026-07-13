using Sandbox;
using System.Collections.Generic;

// Session-only client state for the Guidebook "Locate" feature. Tracks a single
// quest the player wants navigation help toward. No persistence, no networking —
// this only drives the tracked map pins.
public static class QuestTracker
{
	const int MaxChainDepth = 32;

	public static string TrackedQuestId { get; private set; }

	public static void Track( string questId )
	{
		TrackedQuestId = string.IsNullOrEmpty( questId ) ? null : questId;
	}

	public static void Untrack()
	{
		TrackedQuestId = null;
	}

	public static bool IsTracking( string questId )
	{
		return !string.IsNullOrEmpty( TrackedQuestId ) && TrackedQuestId == questId;
	}

	// Finds the NpcInteract whose QuestId matches, searching every component in the
	// scene. Quests can share a GameObject and chains can span different NPCs, so we
	// never assume one-per-GameObject.
	public static NpcInteract FindQuest( Scene scene, string questId )
	{
		if ( scene == null || string.IsNullOrEmpty( questId ) )
			return null;

		foreach ( var npc in scene.GetAllComponents<NpcInteract>() )
		{
			if ( npc.QuestId == questId )
				return npc;
		}

		return null;
	}

	// Walks a quest chain backward from the given start quest until it finds the first
	// quest the player can actually do (state Available). Locked and already-handled
	// quests are stepped over via their PreviousQuestId. Returns null when nothing in
	// the chain is currently doable (e.g. the whole chain is completed).
	public static NpcInteract ResolveChain( Scene scene, NpcInteract start )
	{
		if ( scene == null || start == null )
			return null;

		var current = start;
		var visited = new HashSet<string>();
		int depth = 0;

		while ( current != null && depth < MaxChainDepth )
		{
			if ( !string.IsNullOrEmpty( current.QuestId ) && !visited.Add( current.QuestId ) )
				break;

			if ( current.State == NpcInteract.QuestState.Available )
				return current;

			if ( string.IsNullOrEmpty( current.PreviousQuestId ) )
				break;

			var prev = FindQuest( scene, current.PreviousQuestId );
			if ( prev == null )
				break;

			current = prev;
			depth++;
		}

		return null;
	}

	// Resolves the currently tracked quest to the navigation target: the first
	// doable quest in its chain, or null if the tracked quest is gone / the chain
	// is fully completed.
	public static NpcInteract ResolveTarget( Scene scene )
	{
		if ( scene == null || string.IsNullOrEmpty( TrackedQuestId ) )
			return null;

		var tracked = FindQuest( scene, TrackedQuestId );
		if ( tracked == null )
			return null;

		return ResolveChain( scene, tracked );
	}

	// Human-facing label for the resolved target, prefixed with "First: " when the
	// resolved quest is an earlier chain quest rather than the tracked one.
	public static string GetResolvedLabel( Scene scene )
	{
		var target = ResolveTarget( scene );
		if ( target == null )
			return "";

		var title = target.GetJournalTitle();
		if ( !string.IsNullOrEmpty( TrackedQuestId ) && target.QuestId != TrackedQuestId )
			return $"First: {title}";

		return title;
	}

	// Drops tracking when the tracked quest is genuinely finished. That means either
	// the quest no longer exists, or it is Completed (repeatable quests cycle through
	// OnCooldown and never reach Completed) with no remaining incomplete chain target.
	// A quest that is merely un-resolvable for now (locked chain, repeatable on cooldown)
	// is left tracked. Meant to run once per frame from MinimapHud, not every path.
	public static void ValidateTracked( Scene scene )
	{
		if ( string.IsNullOrEmpty( TrackedQuestId ) )
			return;

		var tracked = FindQuest( scene, TrackedQuestId );
		if ( tracked == null )
		{
			Untrack();
			return;
		}

		if ( tracked.State == NpcInteract.QuestState.Completed && ResolveChain( scene, tracked ) == null )
			Untrack();
	}
}
