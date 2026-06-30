using Sandbox;
using System;

public sealed class PetSlime : Component
{
	[Sync] public ulong OwnerSteamId { get; set; }
	[Sync] public PetKind Kind { get; set; } = PetKind.Slime;

	Vector3 _targetPos;
	float _orbitTimer;
	float _orbitAngle;

	Vector3 _lastPos;
	Vector3 _velSmooth;

	float _jumpPhase;
	bool _inAir;
	float _idleTimer;
	float _squash;

	float _wobbleTime;
	float _wobbleAmp;

	float _curScale;
	float _modelHalf = 14f;

	bool _driving;
	float _mountYaw;
	bool _jumpQueued;

	float _mHopPhase;
	bool _mInAir;
	bool _mBigHop;
	float _mIdle;

	float _groundSmooth;
	bool _groundInit;

	bool _localMounted;
	float _camSmoothZ;
	bool _camInit;

	bool _visualsSetUp;
	float _proxyDelay;

	GameObject _ownerCache;
	GameObject _visual;
	GameObject _seat;
	GameObject _shadow;
	ModelRenderer _visualRenderer;
	BaseChair _chair;
	CharacterController _cc;

	const float StepCap = 16f;
	const float SeatLift = -3f;

	// Slime hop tuning (from Sketchy Wizard PetWorldEntity)
	const float HopSpeed = 4f;
	const float HopArc = 30f;
	const float JumpArc = 60f;
	const float HopLandSquash = -0.6f;
	const float HopPrepSquash = -0.3f;
	const float HopPeakStretch = 0.35f;
	const float RestSquash = -0.12f;
	const float IdleMinMoving = 0.3f;
	const float IdleMaxMoving = 0.7f;
	const float IdleMinRest = 1.5f;
	const float IdleMaxRest = 4f;

	// Jelly wobble on landing
	const float WobbleFreq = 16f;
	const float WobbleDecay = 6f;

	// How smooth the camera is while riding (lower = smoother / more lag)
	const float CamSmoothRate = 18f;

	protected override void OnStart()
	{
		GameObject.Tags.Add( "pet" );

		if ( !IsProxy )
		{
			_visualsSetUp = true;
			SetupVisuals();
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy && !_visualsSetUp )
		{
			_proxyDelay += Time.Delta;
			if ( _proxyDelay >= 0.5f )
			{
				_visualsSetUp = true;
				SetupVisuals();
			}
		}

		TrackVelocity();

		var chair = GetChair();
		bool occupied = chair != null && chair.IsOccupied;
		var def = PetDatabase.Get( Kind );

		if ( occupied )
		{
			_curScale = def.MountedScale;
			if ( !IsProxy && OccupantIsLocal( chair ) )
			{
				_localMounted = true;
				DriveMounted( def );
			}
			else
			{
				_localMounted = false;
				_driving = false;
			}
		}
		else
		{
			_localMounted = false;
			_curScale = def.FollowScale;
			_driving = false;
			if ( !IsProxy )
			{
				var owner = ResolveOwner();
				if ( owner != null )
				{
					TickFollow( owner );
					FollowHop();
				}
			}
		}

		ApplyVisual();
		UpdateShadow();
		DrawBubbles();
	}

	protected override void OnPreRender()
	{
		if ( !_localMounted )
		{
			_camInit = false;
			return;
		}

		var cam = Scene.Camera;
		if ( cam == null )
			return;

		var pos = cam.WorldPosition;
		if ( !_camInit )
		{
			_camSmoothZ = pos.z;
			_camInit = true;
		}
		else
		{
			_camSmoothZ = MathX.Lerp( _camSmoothZ, pos.z, Time.Delta * CamSmoothRate );
		}

		cam.WorldPosition = new Vector3( pos.x, pos.y, _camSmoothZ );
	}

	BaseChair GetChair()
	{
		if ( _chair == null || !_chair.IsValid() )
			_chair = Components.Get<BaseChair>();
		return _chair;
	}

	public bool MountAvailableFor( ulong steamId )
	{
		if ( OwnerSteamId != steamId )
			return false;
		var chair = GetChair();
		return chair != null && !chair.IsOccupied;
	}

	public Vector3 MountAimCenter()
	{
		return WorldPosition + Vector3.Up * ( _modelHalf * _curScale );
	}

	public float MountAimRadius()
	{
		return MathF.Max( _modelHalf * _curScale * 2.5f, 28f );
	}

	CharacterController GetCC()
	{
		if ( _cc == null || !_cc.IsValid() )
			_cc = Components.Get<CharacterController>();
		return _cc;
	}

	bool OccupantIsLocal( BaseChair chair )
	{
		var occ = chair.GetOccupant();
		return occ.IsValid() && !occ.IsProxy;
	}

	GameObject ResolveOwner()
	{
		if ( _ownerCache != null && _ownerCache.IsValid() )
			return _ownerCache;

		foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
		{
			var conn = pc.Network.Owner;
			if ( conn != null && conn.SteamId == OwnerSteamId )
			{
				_ownerCache = pc.GameObject;
				return _ownerCache;
			}
		}
		return null;
	}

	void TrackVelocity()
	{
		var cur = WorldPosition;
		if ( _lastPos != Vector3.Zero )
			_velSmooth = Vector3.Lerp( _velSmooth, ( cur - _lastPos ) / MathF.Max( Time.Delta, 0.001f ), Time.Delta * 5f );
		_lastPos = cur;
	}

	void StartWobble()
	{
		_wobbleTime = 0f;
		_wobbleAmp = HopLandSquash - RestSquash;
	}

	float GroundSquash( bool moving )
	{
		_wobbleTime += Time.Delta;
		float w = MathF.Exp( -WobbleDecay * _wobbleTime ) * MathF.Cos( WobbleFreq * _wobbleTime );
		float s = RestSquash + _wobbleAmp * w;
		if ( !moving )
			s += MathF.Sin( Time.Now * 2.5f ) * 0.05f;
		return s;
	}

	void DriveMounted( PetDef def )
	{
		float dt = Time.Delta;

		if ( !_driving )
		{
			_driving = true;
			_mountYaw = WorldRotation.Yaw();
			_jumpQueued = false;
			_mInAir = false;
			_mBigHop = false;
			_mHopPhase = 0f;
			_mIdle = 0f;
		}

		_mountYaw += Input.AnalogLook.yaw;
		WorldRotation = Rotation.FromYaw( _mountYaw );

		var fwd = WorldRotation.Forward.WithZ( 0f ).Normal;
		var rgt = WorldRotation.Right.WithZ( 0f ).Normal;

		Vector3 wish = Vector3.Zero;
		if ( Input.Down( "Forward" ) ) wish += fwd;
		if ( Input.Down( "Backward" ) ) wish -= fwd;
		if ( Input.Down( "Right" ) ) wish += rgt;
		if ( Input.Down( "Left" ) ) wish -= rgt;

		bool moving = wish.Length > 0.1f;
		if ( moving ) wish = wish.Normal;

		if ( Input.Pressed( "Jump" ) )
			_jumpQueued = true;

		var preXY = WorldPosition;
		float gBefore = GroundZAt( preXY );

		var cc = GetCC();
		if ( cc != null )
		{
			cc.Velocity = new Vector3( wish.x * def.MoveSpeed, wish.y * def.MoveSpeed, 0f );
			cc.Move();
		}
		else if ( moving )
		{
			var step = WorldPosition + wish * def.MoveSpeed * dt;
			WorldPosition = new Vector3( step.x, step.y, WorldPosition.z );
		}

		var p = WorldPosition;
		float gAfter = GroundZAt( p );

		if ( gAfter - gBefore > StepCap )
		{
			p = new Vector3( preXY.x, preXY.y, p.z );
			gAfter = gBefore;
		}

		if ( !_groundInit )
		{
			_groundSmooth = gAfter;
			_groundInit = true;
		}
		else
		{
			_groundSmooth = MathX.Lerp( _groundSmooth, gAfter, dt * 12f );
		}
		float ground = _groundSmooth;

		float nz;
		if ( _mInAir )
		{
			_mHopPhase += dt * HopSpeed;
			if ( _mHopPhase >= MathF.PI )
			{
				_mInAir = false;
				_mBigHop = false;
				_mHopPhase = 0f;
				_squash = HopLandSquash;
				StartWobble();
				_mIdle = moving
					? Game.Random.Float( IdleMinMoving, IdleMaxMoving )
					: Game.Random.Float( IdleMinRest, IdleMaxRest );
				nz = ground;
			}
			else
			{
				float arc = MathF.Sin( _mHopPhase ) * ( _mBigHop ? JumpArc : HopArc );
				nz = ground + arc;
				_squash = MathF.Sin( _mHopPhase ) * HopPeakStretch;
			}
		}
		else
		{
			nz = ground;
			_mIdle -= dt;

			if ( _jumpQueued )
			{
				_mInAir = true;
				_mBigHop = true;
				_mHopPhase = 0f;
				_squash = HopPrepSquash;
				_jumpQueued = false;
			}
			else if ( _mIdle <= 0f && moving )
			{
				_mInAir = true;
				_mBigHop = false;
				_mHopPhase = 0f;
				_squash = HopPrepSquash;
			}
			else
			{
				_squash = GroundSquash( moving );
			}
		}

		p.z = nz;
		WorldPosition = p;

		UpdateSeat();
	}

	void UpdateSeat()
	{
		ResolveSeat();
		if ( _seat == null || !_seat.IsValid() )
			return;

		float topZ;
		if ( _visualRenderer != null && _visualRenderer.IsValid() )
			topZ = _visualRenderer.Bounds.Maxs.z;
		else
			topZ = WorldPosition.z + 2f * _modelHalf * _curScale * ( 1f + _squash );

		_seat.WorldPosition = new Vector3( _seat.WorldPosition.x, _seat.WorldPosition.y, topZ + SeatLift );
	}

	void TickFollow( GameObject owner )
	{
		float dt = Time.Delta;
		var playerPos = owner.WorldPosition;

		_orbitTimer -= dt;
		if ( _orbitTimer <= 0f )
		{
			_orbitTimer = 2f + Game.Random.Float( 0f, 3f );
			_orbitAngle = Game.Random.Float( -MathF.PI, MathF.PI );
		}
		_orbitAngle += dt * 0.3f;

		float orbitDist = 70f + MathF.Sin( Time.Now * 0.7f ) * 15f;
		_targetPos = playerPos + new Vector3(
			MathF.Cos( _orbitAngle ) * orbitDist,
			MathF.Sin( _orbitAngle ) * orbitDist,
			0f );

		var cur = WorldPosition;
		float dPlayer = ( playerPos - cur ).WithZ( 0 ).Length;
		float dTarget = ( _targetPos - cur ).WithZ( 0 ).Length;

		if ( dPlayer > 500f )
		{
			WorldPosition = new Vector3( _targetPos.x, _targetPos.y, cur.z );
			return;
		}

		float speed = dPlayer > 200f
			? 400f + dPlayer * 2f
			: ( dTarget > 20f ? 80f + dTarget * 1.5f : 30f );
		speed = MathF.Min( speed, 600f );

		if ( dTarget > 8f )
		{
			var md = ( _targetPos - cur ).WithZ( 0 ).Normal;
			var np = cur + md * speed * dt;
			WorldPosition = new Vector3( np.x, np.y, cur.z );
		}
	}

	void FollowHop()
	{
		float dt = Time.Delta;

		float moveSpeed = _velSmooth.WithZ( 0 ).Length;
		bool isMoving = moveSpeed > 20f;

		var pos = WorldPosition;
		float groundZ = GroundZAt( pos );

		if ( _inAir )
		{
			_jumpPhase += dt * HopSpeed;
			if ( _jumpPhase >= MathF.PI )
			{
				_inAir = false;
				_jumpPhase = 0f;
				_squash = HopLandSquash;
				StartWobble();
				_idleTimer = isMoving
					? Game.Random.Float( IdleMinMoving, IdleMaxMoving )
					: Game.Random.Float( IdleMinRest, IdleMaxRest );
			}
			else
			{
				float arc = MathF.Sin( _jumpPhase ) * HopArc;
				pos.z = groundZ + arc;
				_squash = MathF.Sin( _jumpPhase ) * HopPeakStretch;
			}
		}
		else
		{
			_idleTimer -= dt;
			if ( _idleTimer <= 0f && isMoving )
			{
				_inAir = true;
				_jumpPhase = 0f;
				_squash = HopPrepSquash;
			}
			else
			{
				_squash = GroundSquash( isMoving );
			}
			pos.z = groundZ;
		}

		WorldPosition = pos;
	}

	float GroundZAt( Vector3 p )
	{
		var tr = Scene.Trace
			.Ray( p + Vector3.Up * 40f, p - Vector3.Up * 200f )
			.Size( 2f )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();
		return tr.Hit ? tr.HitPosition.z : p.z;
	}

	void ApplyVisual()
	{
		if ( _visual == null || !_visual.IsValid() )
			return;

		float sq = IsProxy ? 0f : _squash;
		float sy = 1f + sq;
		float sxz = 1f - sq * 0.5f;
		_visual.LocalScale = new Vector3( sxz, sxz, sy ) * _curScale;
		_visual.LocalPosition = new Vector3( 0f, 0f, _modelHalf * _curScale * sy );
	}

	void UpdateShadow()
	{
		if ( _shadow == null || !_shadow.IsValid() )
			return;

		_shadow.Enabled = true;

		float groundZ = GroundZAt( WorldPosition );
		float height = MathF.Max( 0f, WorldPosition.z - groundZ );
		float fade = MathF.Min( height / 40f, 1f );

		float footprint = _curScale * 0.9f * ( 1f - 0.35f * fade );
		float alpha = 0.35f * ( 1f - 0.55f * fade );

		_shadow.WorldPosition = new Vector3( WorldPosition.x, WorldPosition.y, groundZ + 1f );
		_shadow.WorldRotation = Rotation.Identity;
		_shadow.WorldScale = new Vector3( footprint, footprint, 0.04f );

		var sr = _shadow.Components.Get<ModelRenderer>();
		if ( sr != null )
			sr.Tint = new Color( 0f, 0f, 0f, alpha );
	}

	void DrawBubbles()
	{
		float time = Time.Now;
		var pos = WorldPosition;
		float centerZ = pos.z + _modelHalf * _curScale;
		var color = PetDatabase.SlimeColor( 1f );
		bool moving = _velSmooth.WithZ( 0 ).Length > 20f;

		for ( int i = 0; i < 6; i++ )
		{
			float seed = i * 2.1f;
			float cycle = ( time * 0.5f + seed ) % 1f;
			float angle = seed * 3.7f;
			float spread = 5f * _curScale;
			float px = pos.x + MathF.Cos( angle ) * spread;
			float py = pos.y + MathF.Sin( angle ) * spread;
			float pz = centerZ + 6f * _curScale - cycle * 10f * _curScale;
			float alpha = MathF.Sin( cycle * MathF.PI ) * 0.4f;
			float size = ( 0.8f + ( 1f - cycle ) * 1.2f ) * _curScale;
			Gizmo.Draw.Color = color.WithAlpha( alpha );
			Gizmo.Draw.SolidSphere( new Vector3( px, py, pz ), size );
		}

		if ( !moving )
			return;

		for ( int i = 0; i < 8; i++ )
		{
			float seed = i * 1.7f + 0.5f;
			float cycle = ( time * 0.8f + seed ) % 1f;
			float angle = time * ( 1.5f + i * 0.3f ) + seed;
			float radius = ( 3f + MathF.Sin( time * 2f + seed ) * 4f ) * _curScale;
			float bx = pos.x + MathF.Cos( angle ) * radius;
			float by = pos.y + MathF.Sin( angle ) * radius;
			float bz = centerZ + ( 2f + MathF.Sin( time * 3f + seed * 2f ) * 5f ) * _curScale;
			float alpha = MathF.Sin( cycle * MathF.PI ) * 0.5f;
			float size = ( 0.6f + MathF.Sin( time * 4f + seed ) * 0.3f ) * _curScale;
			Gizmo.Draw.Color = new Color( 0.2f, 1f, 0.35f, alpha );
			Gizmo.Draw.SolidSphere( new Vector3( bx, by, bz ), size );
		}
	}

	void SetupVisuals()
	{
		ResolveVisual();
		ResolveSeat();
		if ( _visual == null )
			return;

		_visualRenderer = _visual.Components.Get<ModelRenderer>();
		if ( _visualRenderer != null )
		{
			_visualRenderer.Tint = PetDatabase.SlimeColor( 0.55f );
			if ( _visualRenderer.Model != null )
			{
				float h = _visualRenderer.Model.Bounds.Size.z * 0.5f;
				if ( h > 0.1f )
					_modelHalf = h;
			}
		}

		EnsureBubbles();
		EnsureShadow();
	}

	void ResolveVisual()
	{
		if ( _visual != null && _visual.IsValid() )
			return;

		foreach ( var c in GameObject.Children )
		{
			if ( c.Name == "Visual" )
			{
				_visual = c;
				return;
			}
		}

		foreach ( var c in GameObject.Children )
		{
			if ( c.Components.Get<ModelRenderer>() != null )
			{
				_visual = c;
				return;
			}
		}
	}

	void ResolveSeat()
	{
		if ( _seat != null && _seat.IsValid() )
			return;

		foreach ( var c in GameObject.Children )
		{
			if ( c.Name == "Sit" )
			{
				_seat = c;
				return;
			}
		}
	}

	void EnsureBubbles()
	{
		foreach ( var c in _visual.Children )
		{
			if ( c.Name == "PetBubble" )
				return;
		}

		var color = PetDatabase.SlimeColor( 0.55f );
		float[] bx = { 6f, -5f, 3f, -4f };
		float[] by = { 4f, 5f, -2f, 3f };
		float[] bz = { 3f, -3f, 4f, -2f };
		for ( int i = 0; i < 4; i++ )
		{
			var bubble = new GameObject();
			bubble.Name = "PetBubble";
			bubble.Parent = _visual;
			bubble.LocalPosition = new Vector3( bx[i], by[i], bz[i] );
			bubble.LocalScale = Vector3.One * ( 0.15f + i * 0.03f );
			var br = bubble.Components.Create<ModelRenderer>();
			br.Model = Model.Load( "models/dev/sphere.vmdl" );
			br.Tint = new Color( color.r + 0.1f, color.g + 0.1f, color.b + 0.1f, 0.3f );
		}
	}

	void EnsureShadow()
	{
		foreach ( var c in GameObject.Children )
		{
			if ( c.Name == "PetShadow" )
			{
				_shadow = c;
				return;
			}
		}

		var shadow = new GameObject();
		shadow.Name = "PetShadow";
		shadow.Parent = GameObject;
		var sr = shadow.Components.Create<ModelRenderer>();
		sr.Model = Model.Load( "models/dev/sphere.vmdl" );
		sr.Tint = new Color( 0f, 0f, 0f, 0.35f );
		_shadow = shadow;
	}
}
