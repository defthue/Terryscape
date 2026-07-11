using Sandbox;

public sealed class ArcherAimCamera : Component
{
	[Property] public CameraComponent PlayerCamera { get; set; }

	[Property, Group( "Aim Offset" )] public float RightOffset { get; set; } = 35f;
	[Property, Group( "Aim Offset" )] public float ForwardOffset { get; set; } = 25f;
	[Property, Group( "Aim Offset" )] public float UpOffset { get; set; } = 12f;

	[Property, Group( "Aim Offset" )] public float AimFov { get; set; } = 70f;
	[Property, Group( "Aim Offset" )] public float DefaultFov { get; set; } = 80f;

	[Property, Group( "Aim Offset" )] public float AimYawOffset { get; set; } = -3f;
	[Property, Group( "Aim Offset" )] public float AimPitchOffset { get; set; } = 4f;
	[Property, Group( "Aim Offset" )] public float AimRollOffset { get; set; } = 0f;

	[Property, Group( "Smoothing" )] public float OffsetLerpSpeed { get; set; } = 12f;
	[Property, Group( "Smoothing" )] public float FovLerpSpeed { get; set; } = 10f;

	[Property, Group( "Draw Zoom" )] public float MaxDrawFovZoom { get; set; } = 6f;

	[Property, Group( "Aim Mode" )] public float AimModeHoldDuration { get; set; } = 3f;

	public static ArcherAimCamera Local { get; private set; }

	public static Vector3 RenderedCamPos { get; private set; }
	public static Rotation RenderedCamRot { get; private set; }
	public static bool HasRenderedCamera { get; private set; }

	float _lastAimActivityTime = -100f;
	float _currentRight = 0f;
	float _currentForward = 0f;
	float _currentUp = 0f;
	float _currentFov;
	float _currentYawOffset = 0f;
	float _currentPitchOffset = 0f;
	float _currentRollOffset = 0f;

	public static void NotifyAimActivity()
	{
		if ( Local != null )
			Local._lastAimActivityTime = Time.Now;
	}

	protected override void OnStart()
	{
		_currentFov = DefaultFov;
	}

	bool IsActivelyDrawing()
	{
		var shooter = Components.Get<ProjectileShooter>();
		return shooter != null && shooter.IsDrawing;
	}

	bool IsActivelyCasting()
	{
		var caster = Components.Get<SpellCaster>();
		return caster != null && caster.IsCasting;
	}

	bool IsActivelyDrawingOrCasting()
	{
		return IsActivelyDrawing() || IsActivelyCasting();
	}

	bool IsInAimMode()
	{
		if ( IsActivelyDrawingOrCasting() )
			return true;

		return Time.Now - _lastAimActivityTime < AimModeHoldDuration;
	}

	float GetBowDrawProgress()
	{
		var shooter = Components.Get<ProjectileShooter>();
		if ( shooter != null && shooter.IsDrawing )
			return shooter.DrawProgress;

		return 0f;
	}

	protected override void OnUpdate()
	{
		if ( !PlayerHelper.IsLocalPlayer( GameObject ) )
			return;

		Local = this;
	}

	protected override void OnPreRender()
	{
		if ( !PlayerHelper.IsLocalPlayer( GameObject ) )
			return;

		if ( PlayerCamera == null )
			return;

		var pc = Components.Get<PlayerController>();
		if ( pc == null || !pc.ThirdPerson )
		{
			_currentRight = 0f;
			_currentForward = 0f;
			_currentUp = 0f;
			_currentFov = DefaultFov;
			_currentYawOffset = 0f;
			_currentPitchOffset = 0f;
			_currentRollOffset = 0f;
			HasRenderedCamera = false;
			return;
		}

		var inventory = Components.Get<Inventory>();
		bool hasAimWeapon = inventory != null && ( inventory.IsWeaponRanged() || inventory.IsWeaponMagic() );

		bool aimMode = hasAimWeapon && IsInAimMode();

		float targetRight = aimMode ? RightOffset : 0f;
		float targetForward = aimMode ? ForwardOffset : 0f;
		float targetUp = aimMode ? UpOffset : 0f;

		float targetYawOffset = aimMode ? AimYawOffset : 0f;
		float targetPitchOffset = aimMode ? AimPitchOffset : 0f;
		float targetRollOffset = aimMode ? AimRollOffset : 0f;

		float bowDrawProgress = GetBowDrawProgress();
		bool bowDrawing = IsActivelyDrawing();
		float targetFov = aimMode
			? ( bowDrawing ? AimFov - ( MaxDrawFovZoom * bowDrawProgress ) : AimFov )
			: DefaultFov;

		float lerpAmt = OffsetLerpSpeed * Time.Delta;
		_currentRight = MathX.Lerp( _currentRight, targetRight, lerpAmt );
		_currentForward = MathX.Lerp( _currentForward, targetForward, lerpAmt );
		_currentUp = MathX.Lerp( _currentUp, targetUp, lerpAmt );
		_currentYawOffset = MathX.Lerp( _currentYawOffset, targetYawOffset, lerpAmt );
		_currentPitchOffset = MathX.Lerp( _currentPitchOffset, targetPitchOffset, lerpAmt );
		_currentRollOffset = MathX.Lerp( _currentRollOffset, targetRollOffset, lerpAmt );
		_currentFov = MathX.Lerp( _currentFov, targetFov, FovLerpSpeed * Time.Delta );

		var camRot = PlayerCamera.WorldRotation;
		var offset =
			camRot.Right * _currentRight +
			camRot.Forward * _currentForward +
			Vector3.Up * _currentUp;

		PlayerCamera.WorldPosition += offset;

		var angles = PlayerCamera.WorldRotation.Angles();
		angles.pitch += _currentPitchOffset;
		angles.yaw += _currentYawOffset;
		angles.roll += _currentRollOffset;
		PlayerCamera.WorldRotation = angles.ToRotation();

		PlayerCamera.FieldOfView = _currentFov;

		RenderedCamPos = PlayerCamera.WorldPosition;
		RenderedCamRot = PlayerCamera.WorldRotation;
		HasRenderedCamera = true;
	}
}