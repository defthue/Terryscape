using Sandbox;
using System;
using System.Collections.Generic;

public sealed class ArmorGlow : Component
{
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }
	[Property] public float DrawDistanceMax { get; set; } = 2500f;
	[Property] public float ShapeScale { get; set; } = 1f;

	[Sync] public ItemId HeadItem { get; set; } = ItemId.None;
	[Sync] public ItemId ChestItem { get; set; } = ItemId.None;
	[Sync] public ItemId LegsItem { get; set; } = ItemId.None;
	[Sync] public bool HeadEnchanted { get; set; }
	[Sync] public bool ChestEnchanted { get; set; }
	[Sync] public bool LegsEnchanted { get; set; }

	static readonly string[] HeadCandidates = { "head" };
	static readonly string[] ChestCandidates = { "spine_2", "spine_middle", "chest", "spine_1" };
	static readonly string[] PelvisCandidates = { "pelvis", "hips", "spine_0" };
	static readonly string[] ShoulderLCandidates = { "clavicle_L", "arm_upper_L" };
	static readonly string[] ShoulderRCandidates = { "clavicle_R", "arm_upper_R" };

	string _headBone;
	string _chestBone;
	string _pelvisBone;
	string _shoulderLBone;
	string _shoulderRBone;

	PlayerController _pc;
	bool _resolved;

	protected override void OnStart()
	{
		if ( BodyRenderer == null )
			BodyRenderer = Components.GetInChildren<SkinnedModelRenderer>();

		_pc = Components.Get<PlayerController>();
	}

	void ResolveBones()
	{
		if ( _resolved )
			return;

		if ( BodyRenderer == null || BodyRenderer.Model == null )
			return;

		var available = new HashSet<string>();
		foreach ( var bone in BodyRenderer.Model.Bones.AllBones )
			available.Add( bone.Name );

		_headBone = PickBone( available, HeadCandidates );
		_chestBone = PickBone( available, ChestCandidates );
		_pelvisBone = PickBone( available, PelvisCandidates );
		_shoulderLBone = PickBone( available, ShoulderLCandidates );
		_shoulderRBone = PickBone( available, ShoulderRCandidates );

		Log.Info( $"[ArmorGlow] bones: head={_headBone ?? "fallback"} chest={_chestBone ?? "fallback"} pelvis={_pelvisBone ?? "fallback"} shoulders={_shoulderLBone ?? "fallback"}/{_shoulderRBone ?? "fallback"}" );

		_resolved = true;
	}

	string PickBone( HashSet<string> available, string[] candidates )
	{
		foreach ( var name in candidates )
		{
			if ( available.Contains( name ) )
				return name;
		}

		return null;
	}

	(Vector3 pos, Rotation rot) AnchorTx( string bone, Vector3 fallbackOffset )
	{
		if ( bone != null && BodyRenderer != null && BodyRenderer.SceneModel != null )
		{
			var tx = BodyRenderer.SceneModel.GetBoneWorldTransform( bone );
			return (tx.Position, tx.Rotation);
		}

		return (WorldPosition + fallbackOffset, GameObject.WorldRotation);
	}

	protected override void OnUpdate()
	{
		ResolveBones();

		if ( PlayerHelper.IsLocalPlayer( GameObject ) )
			WriteSyncState();

		Draw();
	}

	void WriteSyncState()
	{
		var inventory = Components.Get<Inventory>();
		if ( inventory == null )
			return;

		var (headId, headEnch) = ReadSlot( inventory, EquipSlot.Head );
		var (chestId, chestEnch) = ReadSlot( inventory, EquipSlot.Chest );
		var (legsId, legsEnch) = ReadSlot( inventory, EquipSlot.Legs );

		HeadItem = headId;
		HeadEnchanted = headEnch;
		ChestItem = chestId;
		ChestEnchanted = chestEnch;
		LegsItem = legsId;
		LegsEnchanted = legsEnch;
	}

	(ItemId id, bool enchanted) ReadSlot( Inventory inventory, EquipSlot slot )
	{
		var unique = inventory.GetEquippedUnique( slot );
		if ( unique != null )
		{
			bool magic = unique.IsEnchanted || ( unique.IsSocketable && unique.SocketsUsed > 0 );
			return (unique.ItemId, magic);
		}

		return (inventory.GetEquipped( slot ), false);
	}

	void Draw()
	{
		if ( BodyRenderer == null || !BodyRenderer.IsValid() )
			return;

		if ( HeadItem == ItemId.None && ChestItem == ItemId.None && LegsItem == ItemId.None )
			return;

		var camera = Scene.Camera;
		if ( camera == null )
			return;

		if ( Vector3.DistanceBetween( camera.WorldPosition, WorldPosition ) > DrawDistanceMax )
			return;

		bool hideUpper = PlayerHelper.IsLocalPlayer( GameObject ) && _pc != null && !_pc.ThirdPerson;

		if ( !hideUpper && HeadItem != ItemId.None )
			DrawHead();

		if ( !hideUpper && ChestItem != ItemId.None )
			DrawChest();

		if ( LegsItem != ItemId.None )
			DrawLegs();
	}

	void Glow( Vector3 pos, float size, Color color )
	{
		Gizmo.Draw.Color = color.WithAlpha( color.a * 0.35f );
		Gizmo.Draw.SolidSphere( pos, size * 1.5f * ShapeScale );

		Gizmo.Draw.Color = color.WithAlpha( color.a * 0.8f );
		Gizmo.Draw.SolidSphere( pos, size * ShapeScale );
	}

	void Strip( Vector3 from, Vector3 to, float size, Color color, int points )
	{
		if ( points < 2 ) points = 2;
		for ( int i = 0; i < points; i++ )
			Glow( Vector3.Lerp( from, to, (float)i / ( points - 1 ) ), size, color );
	}

	void Band( Vector3 center, Rotation rot, float radius, float size, Color color, int points )
	{
		for ( int i = 0; i < points; i++ )
		{
			float ang = ( (float)i / points ) * MathF.PI * 2f;
			var offset = rot.Forward * ( MathF.Cos( ang ) * radius ) + rot.Right * ( MathF.Sin( ang ) * radius );
			Glow( center + offset, size, color );
		}
	}

	void OverArc( Vector3 center, Vector3 dirA, Vector3 up, float radius, float size, Color color, int points, float startDeg, float endDeg )
	{
		if ( points < 2 ) points = 2;
		for ( int i = 0; i < points; i++ )
		{
			float t = (float)i / ( points - 1 );
			float ang = MathX.DegreeToRadian( startDeg + ( endDeg - startDeg ) * t );
			var pos = center + dirA * ( MathF.Cos( ang ) * radius ) + up * ( MathF.Sin( ang ) * radius );
			Glow( pos, size, color );
		}
	}

	void DrawHead()
	{
		if ( !TryGetColor( HeadItem, HeadEnchanted, 0f, out var color ) )
			return;

		var (head, rot) = AnchorTx( _headBone, Vector3.Up * 64f );
		var brow = head + Vector3.Up * 3f;

		Band( brow, rot, 8.5f, 1.6f, color, 16 );
		OverArc( brow, rot.Forward, Vector3.Up, 8.5f, 1.6f, color, 8, 15f, 165f );

		if ( HeadEnchanted )
			DrawMote( head + Vector3.Up * 10f, 11f, 0f );
	}

	void DrawChest()
	{
		if ( !TryGetColor( ChestItem, ChestEnchanted, 2f, out var color ) )
			return;

		var (chest, rot) = AnchorTx( _chestBone, Vector3.Up * 48f );
		var (shoulderL, _) = AnchorTx( _shoulderLBone, Vector3.Up * 56f + Vector3.Left * 12f );
		var (shoulderR, _) = AnchorTx( _shoulderRBone, Vector3.Up * 56f + Vector3.Right * 12f );

		OverArc( shoulderL + Vector3.Up * 2f, rot.Forward, Vector3.Up, 6f, 1.8f, color, 7, 0f, 180f );
		OverArc( shoulderL + Vector3.Up * 2f, rot.Forward, Vector3.Up, 4f, 1.5f, color, 5, 0f, 180f );
		OverArc( shoulderR + Vector3.Up * 2f, rot.Forward, Vector3.Up, 6f, 1.8f, color, 7, 0f, 180f );
		OverArc( shoulderR + Vector3.Up * 2f, rot.Forward, Vector3.Up, 4f, 1.5f, color, 5, 0f, 180f );

		var plateBase = chest + rot.Forward * 7f;
		Strip( plateBase + Vector3.Up * 8f, plateBase - Vector3.Up * 7f, 1.8f, color, 6 );
		Strip( plateBase + rot.Right * 5f + Vector3.Up * 7f, plateBase + rot.Right * 4f - Vector3.Up * 5f, 1.6f, color, 5 );
		Strip( plateBase + rot.Left * 5f + Vector3.Up * 7f, plateBase + rot.Left * 4f - Vector3.Up * 5f, 1.6f, color, 5 );

		if ( ChestEnchanted )
			DrawMote( chest, 16f, 2f );
	}

	void DrawLegs()
	{
		if ( !TryGetColor( LegsItem, LegsEnchanted, 4f, out var color ) )
			return;

		var (pelvis, rot) = AnchorTx( _pelvisBone, Vector3.Up * 34f );

		Band( pelvis + Vector3.Up * 2f, rot, 11f, 1.8f, color, 14 );

		var thighL = pelvis + rot.Left * 9f;
		var thighR = pelvis + rot.Right * 9f;
		Strip( thighL, thighL + rot.Left * 1.5f - Vector3.Up * 13f, 1.6f, color, 5 );
		Strip( thighR, thighR + rot.Right * 1.5f - Vector3.Up * 13f, 1.6f, color, 5 );

		if ( LegsEnchanted )
			DrawMote( pelvis, 15f, 4f );
	}

	void DrawMote( Vector3 center, float radius, float phase )
	{
		float ang = Time.Now * 2.5f + phase;
		var pos = center + new Vector3( MathF.Cos( ang ) * radius, MathF.Sin( ang ) * radius, MathF.Sin( Time.Now * 2f + phase ) * 4f );
		Glow( pos, 1.8f, new Color( 0.63f, 0.5f, 0.82f, 0.6f ) );
	}

	bool TryGetColor( ItemId id, bool enchanted, float pulsePhase, out Color color )
	{
		color = default;

		var def = ItemDatabase.Get( id );
		if ( def == null || def.Tier < 1 )
			return false;

		float alpha;
		Color baseColor;

		switch ( def.Tier )
		{
			case 1: baseColor = new Color( 0.78f, 0.49f, 0.24f ); alpha = 0.22f; break;
			case 2: baseColor = new Color( 0.60f, 0.66f, 0.54f ); alpha = 0.27f; break;
			case 3: baseColor = new Color( 0.43f, 0.72f, 0.94f ); alpha = 0.32f; break;
			case 4: baseColor = new Color( 0.94f, 0.75f, 0.25f ); alpha = 0.37f; break;
			case 5: baseColor = new Color( 0.72f, 0.66f, 0.91f ); alpha = 0.44f; break;
			default: baseColor = new Color( 0.63f, 0.25f, 0.75f ); alpha = 0.52f; break;
		}

		if ( enchanted )
			alpha *= 0.8f + 0.2f * MathF.Sin( Time.Now * 3f + pulsePhase );

		color = baseColor.WithAlpha( alpha );
		return true;
	}
}