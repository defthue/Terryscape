using Sandbox;
using System;

public sealed class PvpArena : Component
{
	public static PvpArena Active { get; private set; }

	[Property] public float Radius { get; set; } = 600f;
	[Property] public float HeightTolerance { get; set; } = 400f;
	[Property] public GameObject RespawnPoint { get; set; }

	protected override void OnEnabled()
	{
		Active = this;
	}

	protected override void OnDisabled()
	{
		if ( Active == this )
			Active = null;
	}

	public bool Contains( Vector3 worldPos )
	{
		var center = WorldPosition;
		float dz = MathF.Abs( worldPos.z - center.z );
		if ( dz > HeightTolerance )
			return false;

		float dx = worldPos.x - center.x;
		float dy = worldPos.y - center.y;
		return ( dx * dx + dy * dy ) <= ( Radius * Radius );
	}

	public Vector3 GetRespawnPosition()
	{
		if ( RespawnPoint != null )
			return RespawnPoint.WorldPosition;
		return WorldPosition;
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.Color = new Color( 1f, 0.5f, 0.1f ).WithAlpha( 0.85f );

		int segments = 48;
		float top = HeightTolerance;
		float bottom = -HeightTolerance;

		Vector3 prevTop = default;
		Vector3 prevBottom = default;

		for ( int i = 0; i <= segments; i++ )
		{
			float a = ( i / (float)segments ) * MathF.PI * 2f;
			float x = MathF.Cos( a ) * Radius;
			float y = MathF.Sin( a ) * Radius;

			var curTop = new Vector3( x, y, top );
			var curBottom = new Vector3( x, y, bottom );

			if ( i > 0 )
			{
				Gizmo.Draw.Line( prevTop, curTop );
				Gizmo.Draw.Line( prevBottom, curBottom );
			}

			if ( i % 8 == 0 )
				Gizmo.Draw.Line( curBottom, curTop );

			prevTop = curTop;
			prevBottom = curBottom;
		}
	}
}
