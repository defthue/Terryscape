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
	[Property] public int ResourceAmount { get; set; } = 1;
	[Property] public int Tier { get; set; } = 1;
	[Property] public int RequiredLevel { get; set; } = 1;

	[Property] public int MaxHealth { get; set; } = 3;
	[Property] public float RespawnMin { get; set; } = 5f;
	[Property] public float RespawnMax { get; set; } = 20f;

	[Property] public Collider NodeCollider { get; set; }
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }

	[Property] public int XpReward { get; set; } = 1;

	[Property] public float ShrinkDuration { get; set; } = 0.8f;
	[Property] public bool InstantHarvest { get; set; } = false;

	GameObject _stumpColliderObject;

	[Sync] public int CurrentHealth { get; set; }
	[Sync] public bool IsBroken { get; set; }

	Vector3 _originalScale;
	bool _localBroken = false;

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

	protected override void OnStart()
	{
		CurrentHealth = MaxHealth;
		_originalScale = GameObject.LocalScale;
		ShowNode( true );
	}

	protected override void OnUpdate()
	{
		if ( IsBroken && !_localBroken )
		{
			_localBroken = true;
			if ( NodeCollider != null )
				NodeCollider.Enabled = false;
		}

		if ( !IsBroken && _localBroken )
		{
			_localBroken = false;
			GameObject.LocalScale = _originalScale;
			ShowNode( true );

			if ( _stumpColliderObject != null )
			{
				_stumpColliderObject.Destroy();
				_stumpColliderObject = null;
			}
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

		if ( InstantHarvest )
		{
			ShowNode( false );
		}
		else
		{
			_ = ShrinkTree();
		}

		if ( NodeCollider != null )
			NodeCollider.Enabled = false;

		if ( !InstantHarvest )
		{
			_stumpColliderObject = new GameObject();
			_stumpColliderObject.WorldPosition = GameObject.WorldPosition;
			_stumpColliderObject.Name = "StumpCollider";

			var box = _stumpColliderObject.Components.Create<BoxCollider>();
			box.Scale = new Vector3( 26f, 26f, 400f );
			box.Center = new Vector3( 0f, 0f, 200f );
		}
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

		if ( _stumpColliderObject != null )
		{
			_stumpColliderObject.Destroy();
			_stumpColliderObject = null;
		}

		ShowNode( true );
	}

	async Task ShrinkTree()
	{
		float elapsed = 0f;
		Vector3 startScale = GameObject.LocalScale;
		Vector3 targetScale = _originalScale * 0.2f;

		while ( elapsed < ShrinkDuration )
		{
			elapsed += Time.Delta;
			float t = elapsed / ShrinkDuration;
			float eased = t * t;

			GameObject.LocalScale = Vector3.Lerp( startScale, targetScale, eased );

			await Task.Frame();
		}

		GameObject.LocalScale = targetScale;
	}

	void ShowNode( bool visible )
	{
		if ( BodyRenderer != null )
			BodyRenderer.Enabled = visible;

		if ( NodeCollider != null )
			NodeCollider.Enabled = visible;
	}
}