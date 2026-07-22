using Sandbox;
using System;

public static class HudDistanceTaper
{
	public static (float Scale, float Opacity) Evaluate( float distance, float fullSizeRange, float maxRange, float minScale, float fadeRange = 100f )
	{
		float scale = 1f;
		if ( distance > fullSizeRange )
		{
			float span = MathF.Max( 1f, maxRange - fullSizeRange );
			float t = MathX.Clamp( ( distance - fullSizeRange ) / span, 0f, 1f );
			t = t * t * ( 3f - 2f * t );
			scale = MathX.Lerp( 1f, minScale, t );
		}

		float fadeStart = maxRange - fadeRange;
		float opacity = 1f;
		if ( distance > fadeStart )
			opacity = MathX.Clamp( 1f - ( distance - fadeStart ) / fadeRange, 0f, 1f );

		return ( scale, opacity );
	}
}
