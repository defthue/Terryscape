using Sandbox;
using System;
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

	// DEPRECATED — kept so existing scene nodes don't lose property data on load.
	// Resource nodes now always poof out instantly when harvested.
	[Property, Hide] public float ShrinkDuration { get; set; } = 0.8f;
	[Property, Hide] public bool InstantHarvest { get; set; } = false;

	[Sync] public int CurrentHealth { get; set; }
	[Sync] public bool IsBroken { get; set; }

	Vector3 _originalScale;
	bool _localBroken = false;

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

		CurrentHealth = MaxHealth;
		_originalScale = GameObject.LocalScale;

		// Initial visibility state — match whatever IsBroken says when we start.
		// Late-joining clients receive IsBroken=true via Sync before OnStart runs,
		// so we honor that here instead of always showing the node.
		if ( IsBroken )
		{
			_localBroken = true;
			ShowNode( false );
		}
		else
		{
			ShowNode( true );
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
			ShowNode( true );
		}
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

		CurrentHealth -= damage;

		if ( CurrentHealth <= 0 )
		{
			IsBroken = true;
			BroadcastBreak();
			StartRespawnTimer();
		}
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
		BroadcastRespawn();
	}

	[Rpc.Broadcast]
	void BroadcastRespawn()
	{
		_localBroken = false;
		GameObject.LocalScale = _originalScale;
		ShowNode( true );
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