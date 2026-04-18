using Sandbox;
using System.Collections.Generic;

public sealed class HeldToolController : Component
{
	[Property] public ModelRenderer ToolRenderer { get; set; }
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }
	[Property] public string BoneName { get; set; } = "hold_R";
	[Property] public int MeleeHoldType { get; set; } = 6;
	[Property] public int RangedHoldType { get; set; } = 2;
	[Property] public int MagicHoldType { get; set; } = 6;

	[Property] public Vector3 DefaultPositionOffset { get; set; } = Vector3.Zero;
	[Property] public Angles DefaultRotationOffset { get; set; } = Angles.Zero;

	[Property] public List<ToolModelEntry> ToolModels { get; set; } = new();

	public bool IsPunching { get; set; } = false;

	[Sync] public int CurrentHoldType { get; set; } = 0;
	[Sync] public ItemId CurrentWeaponId { get; set; } = ItemId.None;

	Vector3 _currentPositionOffset = Vector3.Zero;
	Angles _currentRotationOffset = Angles.Zero;

	protected override void OnUpdate()
	{
		if ( PlayerHelper.IsLocalPlayer( GameObject ) )
			UpdateHeldTool();

		ApplyVisuals();
	}

	protected override void OnPreRender()
	{
		FollowBone();
	}

	void UpdateHeldTool()
	{
		if ( ToolRenderer == null )
			return;

		var inventory = GameObject.Parent?.Components.Get<Inventory>();

		if ( inventory == null )
		{
			CurrentWeaponId = ItemId.None;
			if ( !IsPunching )
				SetHoldType( 0 );
			return;
		}

		var weaponId = inventory.GetEquipped( EquipSlot.Weapon );
		CurrentWeaponId = weaponId;

		if ( weaponId == ItemId.None )
		{
			if ( !IsPunching )
				SetHoldType( 0 );
			return;
		}

		var def = ItemDatabase.Get( weaponId );
		if ( def != null )
		{
			if ( def.Type == ItemType.RangedWeapon )
				SetHoldType( RangedHoldType );
			else if ( def.Type == ItemType.MagicWeapon )
				SetHoldType( MagicHoldType );
			else
				SetHoldType( MeleeHoldType );
		}
	}

	void ApplyVisuals()
	{
		if ( ToolRenderer == null )
			return;

		if ( CurrentWeaponId == ItemId.None )
		{
			ToolRenderer.Enabled = false;
			_currentPositionOffset = DefaultPositionOffset;
			_currentRotationOffset = DefaultRotationOffset;
		}
		else
		{
			var entry = FindToolModel( CurrentWeaponId );
			if ( entry != null )
			{
				ToolRenderer.Model = entry.Model;
				ToolRenderer.Enabled = true;
				_currentPositionOffset = entry.PositionOffset;
				_currentRotationOffset = entry.RotationOffset;
			}
			else
			{
				ToolRenderer.Enabled = false;
				_currentPositionOffset = DefaultPositionOffset;
				_currentRotationOffset = DefaultRotationOffset;
			}
		}

		if ( BodyRenderer != null )
			BodyRenderer.Set( "holdtype", CurrentHoldType );
	}

	ToolModelEntry FindToolModel( ItemId id )
	{
		foreach ( var entry in ToolModels )
		{
			if ( entry.ItemId == id )
				return entry;
		}
		return null;
	}

	public void SetHoldType( int holdType )
	{
		CurrentHoldType = holdType;
	}

	void FollowBone()
	{
		if ( ToolRenderer == null || !ToolRenderer.Enabled )
			return;

		if ( BodyRenderer == null || BodyRenderer.SceneModel == null )
			return;

		var boneTx = BodyRenderer.SceneModel.GetBoneWorldTransform( BoneName );

		GameObject.WorldPosition = boneTx.Position + boneTx.Rotation * _currentPositionOffset;
		GameObject.WorldRotation = boneTx.Rotation * _currentRotationOffset.ToRotation();
	}
}

public class ToolModelEntry
{
	[Property] public ItemId ItemId { get; set; }
	[Property] public Model Model { get; set; }
	[Property] public Vector3 PositionOffset { get; set; } = Vector3.Zero;
	[Property] public Angles RotationOffset { get; set; } = Angles.Zero;
}