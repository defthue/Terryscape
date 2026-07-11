using Sandbox;

public static class AimResolver
{
	const float NearFieldThreshold = 50f;

	public struct Result
	{
		public Vector3 CameraPos;
		public Vector3 CameraForward;
		public Vector3 AimPoint;
		public bool Hit;

		public Vector3 LaunchDirectionFrom( Vector3 spawnPos )
		{
			Vector3 toTarget = AimPoint - spawnPos;
			if ( toTarget.Length >= NearFieldThreshold )
				return toTarget.Normal;

			return CameraForward;
		}
	}

	public static bool TryGetCamera( out Vector3 pos, out Rotation rot )
	{
		if ( ArcherAimCamera.HasRenderedCamera )
		{
			pos = ArcherAimCamera.RenderedCamPos;
			rot = ArcherAimCamera.RenderedCamRot;
			return true;
		}

		var camera = Game.ActiveScene?.Camera;
		if ( camera != null )
		{
			pos = camera.WorldPosition;
			rot = camera.WorldRotation;
			return true;
		}

		pos = Vector3.Zero;
		rot = Rotation.Identity;
		return false;
	}

	public static Result Resolve( GameObject shooter, float traceDistance )
	{
		if ( !TryGetCamera( out var camPos, out var camRot ) )
		{
			Vector3 fwd = shooter != null ? shooter.WorldRotation.Forward : Vector3.Forward;
			Vector3 origin = shooter != null ? shooter.WorldPosition : Vector3.Zero;
			return new Result
			{
				CameraPos = origin,
				CameraForward = fwd,
				AimPoint = origin + fwd * traceDistance,
				Hit = false
			};
		}

		Vector3 camForward = camRot.Forward;
		Vector3 camEnd = camPos + camForward * traceDistance;

		var trace = Game.ActiveScene.Trace
			.Ray( camPos, camEnd )
			.UseHitboxes( true );

		if ( shooter != null )
			trace = trace.IgnoreGameObjectHierarchy( shooter );

		var pet = ResolveActivePet();
		if ( pet != null )
			trace = trace.IgnoreGameObjectHierarchy( pet );

		var tr = trace.Run();
		Vector3 aimPoint = tr.Hit ? tr.HitPosition : camEnd;

		return new Result
		{
			CameraPos = camPos,
			CameraForward = camForward,
			AimPoint = aimPoint,
			Hit = tr.Hit
		};
	}

	static GameObject ResolveActivePet()
	{
		var pm = PetManager.Local;
		return pm != null ? pm.ActiveSlime : null;
	}
}
