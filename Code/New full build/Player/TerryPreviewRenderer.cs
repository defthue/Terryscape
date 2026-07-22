using Sandbox;
using System;

public sealed class TerryPreviewRenderer : Component
{
	public static TerryPreviewRenderer Instance { get; private set; }
	public static Texture PreviewTexture => Instance != null ? Instance._texture : null;

	[Property] public SkinnedModelRenderer SourceBody { get; set; }
	[Property] public HeldToolController SourceTool { get; set; }
	[Property] public Model FallbackBodyModel { get; set; }

	[Property, Group( "Output" )] public int RenderWidth { get; set; } = 280;
	[Property, Group( "Output" )] public int RenderHeight { get; set; } = 480;
	[Property, Group( "Output" )] public float RenderInterval { get; set; } = 0.08f;
	[Property, Group( "Output" )] public float WarmupSeconds { get; set; } = 0.75f;
	[Property, Group( "Output" )] public float WeaponSettleDelay { get; set; } = 0.2f;
	[Property, Group( "Output" )] public Color BackgroundColor { get; set; } = new Color( 0.05f, 0.04f, 0.024f, 1f );
	[Property, Group( "Output" )] public bool RenderWithAlpha { get; set; } = true;

	[Property, Group( "Framing" )] public float CameraDistance { get; set; } = 112f;
	[Property, Group( "Framing" )] public float CameraHeight { get; set; } = 36f;
	[Property, Group( "Framing" )] public float CameraPitch { get; set; } = 0f;
	[Property, Group( "Framing" )] public float FieldOfView { get; set; } = 32f;
	[Property, Group( "Framing" )] public float ModelYaw { get; set; } = 180f;

	[Property, Group( "Lighting" )] public Color KeyLightColor { get; set; } = new Color( 1f, 0.96f, 0.88f ) * 8f;
	[Property, Group( "Lighting" )] public Color FillLightColor { get; set; } = new Color( 0.5f, 0.55f, 0.65f ) * 3f;
	[Property, Group( "Lighting" )] public float LightRadius { get; set; } = 900f;

	[Property, Group( "Placement" )] public Vector3 VoidOrigin { get; set; } = new Vector3( 0f, 0f, 100000f );

	const string PreviewTag = "terry_preview";
	const float BuildRetryInterval = 1f;

	GameObject _rig;
	GameObject _cameraGo;
	CameraComponent _camera;
	SkinnedModelRenderer _body;
	ModelRenderer _weapon;

	Texture _texture;
	Bitmap _bitmap;
	TimeSince _sinceRender;
	TimeSince _sinceTrigger;
	bool _warmedUp;
	bool _pendingSingle;

	Vector3 _weaponOffsetPos = Vector3.Zero;
	Angles _weaponOffsetRot = Angles.Zero;
	ItemId _shownWeapon = ItemId.None;

	bool _built;
	bool _active;
	bool _loggedRender;

	TimeSince _sinceBuildAttempt;
	bool _hasAttemptedBuild;
	bool _buildFailedLogged;
	bool _loggedRenderError;

	protected override void OnEnabled()
	{
		Instance = this;
		Log.Info( "[TerryPreview] enabled" );
	}

	protected override void OnDisabled()
	{
		if ( Instance == this )
			Instance = null;

		Teardown();
	}

	public void SetActive( bool on )
	{
		if ( on && !_active )
		{
			_sinceTrigger = 0f;
			_pendingSingle = true;
			_hasAttemptedBuild = false;
		}

		_active = on;
	}

	protected override void OnUpdate()
	{
		if ( !_active )
			return;

		if ( !_built )
		{
			if ( _hasAttemptedBuild && _sinceBuildAttempt < BuildRetryInterval )
				return;

			_hasAttemptedBuild = true;
			_sinceBuildAttempt = 0f;

			if ( !TryBuild() )
				return;
		}

		UpdateWeapon();
		FollowWeaponBone();

		if ( !_warmedUp )
		{
			if ( _sinceRender >= RenderInterval )
			{
				_sinceRender = 0f;
				RenderFrame();
			}

			if ( _sinceTrigger >= WarmupSeconds )
				_warmedUp = true;
		}
		else if ( _pendingSingle && _sinceTrigger >= WeaponSettleDelay )
		{
			_pendingSingle = false;
			RenderFrame();
		}
	}

	bool TryBuild()
	{
		try
		{
			Build();

			if ( _built )
				_buildFailedLogged = false;

			return _built;
		}
		catch ( Exception ex )
		{
			DestroyRig();

			if ( !_buildFailedLogged )
			{
				_buildFailedLogged = true;
				Log.Warning( $"[TerryPreview] preview build failed, will retry: {ex}" );
			}

			return false;
		}
	}

	void Build()
	{
		DestroyRig();

		var bodyModel = ResolveBodyModel();
		if ( bodyModel == null )
		{
			if ( !_buildFailedLogged )
			{
				_buildFailedLogged = true;
				Log.Warning( "[TerryPreview] Build waiting — no local body model yet." );
			}
			return;
		}

		Log.Info( $"[TerryPreview] building rig with body model '{bodyModel.Name}'" );

		_rig = new GameObject( true, "TerryPreviewRig" );
		_rig.WorldPosition = VoidOrigin;
		_rig.WorldRotation = Rotation.FromYaw( ModelYaw );
		_rig.Tags.Add( PreviewTag );

		_body = _rig.Components.Create<SkinnedModelRenderer>();
		_body.Model = bodyModel;
		_body.GameObject.Tags.Add( PreviewTag );

		var clothing = ClothingContainer.CreateFromLocalUser();
		clothing?.Apply( _body );

		foreach ( var child in _body.GameObject.Children )
			child.Tags.Add( PreviewTag );

		var weaponGo = new GameObject( true, "TerryPreviewWeapon" );
		weaponGo.SetParent( _rig );
		weaponGo.Tags.Add( PreviewTag );
		_weapon = weaponGo.Components.Create<ModelRenderer>();
		_weapon.Enabled = false;

		BuildLight( "Key", new Vector3( -120f, 90f, 150f ), KeyLightColor );
		BuildLight( "Fill", new Vector3( -110f, -120f, 60f ), FillLightColor );

		_cameraGo = new GameObject( true, "TerryPreviewCamera" );
		_camera = _cameraGo.Components.Create<CameraComponent>();
		_camera.Enabled = false;
		_camera.BackgroundColor = BackgroundColor;
		_camera.FieldOfView = FieldOfView;
		_camera.ZNear = 1f;
		_camera.ZFar = 5000f;
		_camera.Priority = -1000;
		_camera.RenderTags.Add( PreviewTag );

		PositionCamera();

		_bitmap = new Bitmap( RenderWidth, RenderHeight );

		_built = true;
		_shownWeapon = ItemId.None;
		Log.Info( "[TerryPreview] rig + camera built" );
	}

	void BuildLight( string suffix, Vector3 offset, Color color )
	{
		var go = new GameObject( true, "TerryPreviewLight" + suffix );
		go.SetParent( _rig );
		go.WorldPosition = VoidOrigin + offset;
		go.Tags.Add( PreviewTag );

		var light = go.Components.Create<PointLight>();
		light.LightColor = color;
		light.Radius = LightRadius;
	}

	void DestroyRig()
	{
		_built = false;

		_rig?.Destroy();
		_rig = null;

		_cameraGo?.Destroy();
		_cameraGo = null;

		_camera = null;
		_body = null;
		_weapon = null;
		_texture = null;
		_bitmap = null;
	}

	void Teardown()
	{
		_active = false;
		_hasAttemptedBuild = false;

		DestroyRig();
	}

	void PositionCamera()
	{
		if ( _cameraGo == null || _rig == null )
			return;

		var origin = _rig.WorldPosition;
		_rig.WorldRotation = Rotation.FromYaw( ModelYaw );

		var target = origin + Vector3.Up * CameraHeight;
		var camPos = target - Vector3.Forward * CameraDistance;

		_cameraGo.WorldPosition = camPos;
		_cameraGo.WorldRotation = Rotation.From( CameraPitch, 0f, 0f );
		_camera.FieldOfView = FieldOfView;
		_camera.BackgroundColor = BackgroundColor;
	}

	void RenderFrame()
	{
		if ( _camera == null || _bitmap == null )
			return;

		try
		{
			PositionCamera();

			_camera.RenderToBitmap( _bitmap, RenderWithAlpha );
			_texture = _bitmap.ToTexture();
		}
		catch ( Exception ex )
		{
			if ( !_loggedRenderError )
			{
				_loggedRenderError = true;
				Log.Warning( $"[TerryPreview] render failed: {ex.Message}" );
			}
			return;
		}

		if ( !_loggedRender )
		{
			_loggedRender = true;
			Log.Info( $"[TerryPreview] RenderToBitmap produced {( _texture != null ? _texture.Width + "x" + _texture.Height : "null" )}" );
		}
	}

	void UpdateWeapon()
	{
		var tool = ResolveTool();
		var id = tool != null ? tool.CurrentWeaponId : ItemId.None;

		if ( id == _shownWeapon )
			return;

		_shownWeapon = id;
		_sinceTrigger = 0f;
		_pendingSingle = true;

		if ( id == ItemId.None || tool == null )
		{
			_weapon.Enabled = false;
			_body.Set( "holdtype", 0 );
			return;
		}

		var entry = FindToolEntry( tool, id );
		if ( entry != null && entry.Model != null )
		{
			_weapon.Model = entry.Model;
			_weapon.Enabled = true;
			_weaponOffsetPos = entry.PositionOffset;
			_weaponOffsetRot = entry.RotationOffset;
			_body.Set( "holdtype", HoldTypeFor( id ) );
		}
		else
		{
			_weapon.Enabled = false;
			_body.Set( "holdtype", 0 );
		}
	}

	void FollowWeaponBone()
	{
		if ( _weapon == null || !_weapon.Enabled )
			return;

		if ( _body == null || _body.SceneModel == null )
			return;

		var boneTx = _body.SceneModel.GetBoneWorldTransform( "hold_R" );
		_weapon.WorldPosition = boneTx.Position + boneTx.Rotation * _weaponOffsetPos;
		_weapon.WorldRotation = boneTx.Rotation * _weaponOffsetRot.ToRotation();
	}

	Model ResolveBodyModel()
	{
		if ( SourceBody != null && SourceBody.Model != null )
			return SourceBody.Model;

		var body = FindLocalBody();
		if ( body != null && body.Model != null )
			return body.Model;

		return FallbackBodyModel;
	}

	SkinnedModelRenderer FindLocalBody()
	{
		foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
		{
			if ( !PlayerHelper.IsLocalPlayer( pc.GameObject ) )
				continue;

			return pc.GameObject.Components.GetInChildren<SkinnedModelRenderer>( true );
		}

		return null;
	}

	HeldToolController ResolveTool()
	{
		if ( SourceTool != null )
			return SourceTool;

		foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
		{
			if ( !PlayerHelper.IsLocalPlayer( pc.GameObject ) )
				continue;

			return pc.GameObject.Components.GetInChildren<HeldToolController>( true );
		}

		return null;
	}

	ToolModelEntry FindToolEntry( HeldToolController tool, ItemId id )
	{
		foreach ( var entry in tool.ToolModels )
		{
			if ( entry.ItemId == id )
				return entry;
		}

		return null;
	}

	int HoldTypeFor( ItemId id )
	{
		var def = ItemDatabase.Get( id );
		if ( def == null )
			return 6;

		if ( def.Type == ItemType.RangedWeapon )
			return 2;

		return 6;
	}
}