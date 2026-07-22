using Sandbox;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public enum GatherType
{
	Woodcutting,
	Mining,
	Foraging
}

public sealed class ResourceNode : Component
{
	[Property] public string DisplayName { get; set; } = "";
	[Property] public GatherType GatherSkill { get; set; }
	[Property] public ItemId ResourceItem { get; set; }

	[Property, Group( "Yield" )] public int ResourceAmount { get; set; } = 1;
	// Optional. When > 0 and greater than ResourceAmount, harvested amount is rolled between
	// ResourceAmount (min) and this value (max), inclusive. Leave at 0 for fixed yield.
	[Property, Group( "Yield" )] public int ResourceAmountMax { get; set; } = 0;

	[Property] public int Tier { get; set; } = 1;
	[Property] public int RequiredLevel { get; set; } = 1;

	[Property] public int MaxHealth { get; set; } = 3;
	[Property] public float RespawnMin { get; set; } = 5f;
	[Property] public float RespawnMax { get; set; } = 20f;

	// Optional manual references. Leave unset to auto-detect from the GameObject.
	// If set, the manual reference takes priority over auto-detection.
	[Property] public Collider NodeCollider { get; set; }
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }
	[Property] public ModelRenderer StaticBodyRenderer { get; set; }

	[Property] public int XpReward { get; set; } = 1;

	// Distance culling. When the local camera is farther than this many units away,
	// the renderer and collider are disabled to save GPU/physics cost.
	// Purely a client-side visual/physics optimization — networked state
	// (IsBroken, harvesting via Rpc.Host) is unaffected. Each client decides
	// independently what to cull based on their own local camera position.
	// NOTE: Renamed from MaxDrawDistance to DrawDistanceMax to force scene/prefab
	// instances to fall back to the code default (any old saved MaxDrawDistance
	// values become orphaned data that s&box will ignore).
	[Property, Group( "Culling" )] public float DrawDistanceMax { get; set; } = 5000f;

	// DEPRECATED — kept so existing scene nodes don't lose property data on load.
	// Resource nodes now always poof out instantly when harvested.
	[Property, Hide] public float ShrinkDuration { get; set; } = 0.8f;
	[Property, Hide] public bool InstantHarvest { get; set; } = false;

	[Sync] public int CurrentHealth { get; set; }
	[Sync] public bool IsBroken { get; set; }

	Vector3 _originalScale;
	bool _localBroken = false;

	Dictionary<ulong, int> _contributors = new();

	// Distance culling state. Tracks whether this node is currently culled
	// for the local client based on distance to the local camera.
	bool _localCulled = false;
	float _nextCullCheckTime = 0f;

	// Resolved at OnStart from manual properties OR from the GameObject's components.
	// Whichever renderer/collider is present, we'll find it.
	Collider _resolvedCollider;
	SkinnedModelRenderer _resolvedSkinnedRenderer;
	ModelRenderer _resolvedStaticRenderer;

	public string GetDisplayName()
	{
		if ( !string.IsNullOrEmpty( DisplayName ) )
			return DisplayName;

		var def = ItemDatabase.Get( ResourceItem );
		if ( def != null )
			return def.Name;

		return Regex.Replace( GameObject.Name, @"\s*\([^)]*\)\s*$", "" ).Trim();
	}

	public SkillType GetSkillType()
	{
		switch ( GatherSkill )
		{
			case GatherType.Woodcutting: return SkillType.Woodcutting;
			case GatherType.Mining: return SkillType.Mining;
			case GatherType.Foraging: return SkillType.Enchanting;
			default: return SkillType.None;
		}
	}

	public bool RequiresHatchet()
	{
		return GatherSkill == GatherType.Woodcutting;
	}

	public bool RequiresPickaxe()
	{
		return GatherSkill == GatherType.Mining;
	}

	public bool RequiresEmptyHands()
	{
		return GatherSkill == GatherType.Foraging;
	}

	// Returns the actual amount of resources to award when this node is harvested.
	// Uses fixed yield if ResourceAmountMax is unset/invalid, otherwise rolls a random
	// integer between ResourceAmount (min) and ResourceAmountMax (max), inclusive.
	public int GetHarvestAmount()
	{
		int min = Math.Max( 1, ResourceAmount );

		if ( ResourceAmountMax <= min )
			return min;

		// Random.Shared.Next is exclusive of upper bound, so add 1 to make inclusive.
		return Random.Shared.Next( min, ResourceAmountMax + 1 );
	}

	protected override void OnStart()
	{
		ResolveReferences();

		if ( GatherSkill == GatherType.Foraging && _resolvedCollider != null )
			_resolvedCollider.IsTrigger = true;

		CurrentHealth = MaxHealth;
		_originalScale = GameObject.LocalScale;

		// Stagger initial cull checks across nodes so we don't hammer the system
		// with every node checking distance on the same frame.
		_nextCullCheckTime = Time.Now + Random.Shared.NextSingle() * 0.5f;

		// Initial visibility state — match whatever IsBroken says when we start.
		// Late-joining clients receive IsBroken=true via Sync before OnStart runs,
		// so we honor that here instead of always showing the node.
		// Distance culling will kick in on the first OnUpdate tick.
		if ( IsBroken )
		{
			_localBroken = true;
			ShowNode( false );
		}
		else
		{
			// Compute initial cull state immediately so joining players don't see
			// a flash of distant nodes before the first cull check runs.
			_localCulled = ShouldCullForDistance();
			ShowNode( !_localCulled );
		}
	}

	// Picks up manual property assignments first, falls back to whatever component
	// is on the GameObject. This means most nodes don't need ANYTHING wired up
	// in the inspector — the script finds the renderer and collider automatically.
	void ResolveReferences()
	{
		_resolvedCollider = NodeCollider != null ? NodeCollider : Components.Get<Collider>();

		_resolvedSkinnedRenderer = BodyRenderer != null ? BodyRenderer : Components.Get<SkinnedModelRenderer>();

		// Look up a static ModelRenderer too. If a SkinnedModelRenderer is present,
		// Components.Get<ModelRenderer>() may also return that since SkinnedModelRenderer
		// inherits from ModelRenderer — so prefer the manual assignment when set.
		if ( StaticBodyRenderer != null )
		{
			_resolvedStaticRenderer = StaticBodyRenderer;
		}
		else if ( _resolvedSkinnedRenderer == null )
		{
			// Only auto-detect a plain ModelRenderer if we don't have a skinned one.
			// Otherwise the SkinnedModelRenderer would also satisfy a ModelRenderer lookup
			// and we'd be operating on the same component twice.
			_resolvedStaticRenderer = Components.Get<ModelRenderer>();
		}
	}

	protected override void OnUpdate()
	{
		// Reconcile local visual state with the synced IsBroken flag.
		// This handles edge cases where the BroadcastBreak/BroadcastRespawn RPC was missed
		// (late-joiners, network hiccups) — without this, the node can end up visible
		// but uncollidable on some clients.
		if ( IsBroken && !_localBroken )
		{
			_localBroken = true;
			ShowNode( false );
		}

		if ( !IsBroken && _localBroken )
		{
			_localBroken = false;
			GameObject.LocalScale = _originalScale;
			// Re-evaluate culling on respawn so we don't show a node that's far away.
			_localCulled = ShouldCullForDistance();
			ShowNode( !_localCulled );
		}

		// Distance culling. Only runs when the node is not broken — if it's broken,
		// it's already hidden and we don't need to do anything.
		// Throttled to ~2 checks per second per node, with staggered start times,
		// so this doesn't add meaningful overhead even with thousands of nodes.
		if ( !IsBroken && Time.Now >= _nextCullCheckTime )
		{
			_nextCullCheckTime = Time.Now + 0.5f;

			bool shouldCull = ShouldCullForDistance();
			if ( shouldCull != _localCulled )
			{
				_localCulled = shouldCull;
				ShowNode( !_localCulled );
			}
		}
	}

	// Returns true if this node should be culled (hidden) based on distance
	// to the local camera. Falls back to "don't cull" if we can't find a
	// reference camera — better to show than to hide.
	bool ShouldCullForDistance()
	{
		if ( DrawDistanceMax <= 0f )
			return false;

		var camera = Scene.Camera;
		if ( camera == null )
			return false;

		float sqrDist = ( WorldPosition - camera.WorldPosition ).LengthSquared;
		float maxSqr = DrawDistanceMax * DrawDistanceMax;

		return sqrDist > maxSqr;
	}

	public bool CanHarvest( Inventory inventory )
	{
		if ( inventory == null )
			return false;

		if ( GatherSkill == GatherType.Woodcutting && !inventory.IsWeaponHatchet() )
			return false;

		if ( GatherSkill == GatherType.Mining && !inventory.IsWeaponPickaxe() )
			return false;

		if ( GatherSkill == GatherType.Foraging )
		{
			var weaponId = inventory.GetEquipped( EquipSlot.Weapon );
			if ( weaponId != ItemId.None )
				return false;
		}

		return true;
	}

	public void TakeDamage( int damage, GameObject harvester )
	{
		if ( IsBroken )
			return;

		RequestDamage( damage );
	}

	[Rpc.Host]
	void RequestDamage( int damage )
	{
		if ( IsBroken )
			return;

		ulong harvesterId = Rpc.Caller?.SteamId ?? 0ul;
		if ( harvesterId != 0ul && damage > 0 )
		{
			_contributors.TryGetValue( harvesterId, out int total );
			_contributors[harvesterId] = total + damage;
		}

		CurrentHealth -= damage;

		if ( CurrentHealth <= 0 )
		{
			IsBroken = true;
			AwardHarvestToContributors();
			BroadcastBreak();
			StartRespawnTimer();
		}
	}

	void AwardHarvestToContributors()
	{
		if ( _contributors.Count == 0 )
			return;

		long totalDamage = 0;
		foreach ( var kv in _contributors )
			totalDamage += kv.Value;

		if ( totalDamage <= 0 )
		{
			_contributors.Clear();
			return;
		}

		int amount = GetHarvestAmount();

		foreach ( var kv in _contributors )
		{
			if ( kv.Value <= 0 )
				continue;

			int share = (int)Math.Ceiling( amount * (double)kv.Value / totalDamage );
			if ( share <= 0 )
				continue;

			BroadcastHarvestReward( kv.Key, ResourceItem, share, GetDisplayName(), XpReward );
		}

		_contributors.Clear();
	}

	[Rpc.Broadcast]
	void BroadcastHarvestReward( ulong recipientSteamId, ItemId itemId, int amount, string nodeName, int xpReward )
	{
		if ( Connection.Local == null || Connection.Local.SteamId != recipientSteamId )
			return;

		var localPlayer = FindLocalPlayer();
		if ( localPlayer == null )
			return;

		var inventory = localPlayer.Components.Get<Inventory>();
		var skills = localPlayer.Components.Get<Skills>();

		if ( inventory != null )
		{
			var (placed, banked) = inventory.AddItemOrBank( itemId, amount );

			if ( placed > 0 || banked > 0 )
			{
				SoundLibrary.PlayReceiveItem();
				ItemPickupEffect.Trigger( itemId );
			}

			var def = ItemDatabase.Get( itemId );
			string itemName = def != null ? def.Name : itemId.ToString();

			if ( placed > 0 )
				GameLog.Add( $"You collected {placed}x {itemName} from {nodeName}.", "#6db8f0" );

			if ( banked > 0 )
				GameLog.Add( $"Inventory full — {banked}x {itemName} sent to your bank.", "#c9a84c" );

			inventory.AddNodeMined();
			AchievementTracker.OnNodeGathered();
		}

		if ( skills != null && xpReward > 0 )
			skills.AddXp( GetSkillType(), xpReward );
	}

	GameObject FindLocalPlayer()
	{
		foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
		{
			var owner = pc.Network.Owner;
			if ( owner != null && Connection.Local != null && owner.SteamId == Connection.Local.SteamId )
				return pc.GameObject;
		}
		return null;
	}

	[Rpc.Broadcast]
	void BroadcastBreak()
	{
		_localBroken = true;
		ShowNode( false );
	}

	async void StartRespawnTimer()
	{
		if ( !Networking.IsHost )
			return;

		float respawnTime = Random.Shared.NextSingle() * ( RespawnMax - RespawnMin ) + RespawnMin;
		await Task.DelaySeconds( respawnTime );

		CurrentHealth = MaxHealth;
		IsBroken = false;
		_contributors.Clear();
		BroadcastRespawn();
	}

	[Rpc.Broadcast]
	void BroadcastRespawn()
	{
		_localBroken = false;
		GameObject.LocalScale = _originalScale;
		// Re-evaluate distance culling on respawn — the player may have walked away
		// while the node was broken, in which case we shouldn't show it.
		_localCulled = ShouldCullForDistance();
		ShowNode( !_localCulled );
	}

	void ShowNode( bool visible )
	{
		if ( _resolvedSkinnedRenderer != null )
			_resolvedSkinnedRenderer.Enabled = visible;

		if ( _resolvedStaticRenderer != null )
			_resolvedStaticRenderer.Enabled = visible;

		if ( _resolvedCollider != null )
			_resolvedCollider.Enabled = visible;
	}
}