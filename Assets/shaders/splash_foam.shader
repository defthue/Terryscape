HEADER
{
	Description = "Rumbling foam mound for Terry's Quest";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	Depth();
}

COMMON
{
	#include "common/shared.hlsl"

	float g_flRumble < Attribute( "Rumble" ); Default( 1.0 ); >;
	float g_flRumbleSpeed < Attribute( "RumbleSpeed" ); Default( 2.0 ); >;
	float g_flMoundHeight < Attribute( "MoundHeight" ); Default( 90.0 ); >;

	float2 HashCell( float2 p )
	{
		p = p - 64.0 * floor( p / 64.0 );
		p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
		return frac( sin( p ) * 43758.5453 );
	}

	float ValueNoise( float2 p )
	{
		float2 cell = floor( p );
		float2 f = frac( p );
		float2 u = f * f * ( 3.0 - 2.0 * f );
		float a = HashCell( cell ).x;
		float b = HashCell( cell + float2( 1.0, 0.0 ) ).x;
		float c = HashCell( cell + float2( 0.0, 1.0 ) ).x;
		float d = HashCell( cell + float2( 1.0, 1.0 ) ).x;
		return lerp( lerp( a, b, u.x ), lerp( c, d, u.x ), u.y );
	}

	float Fbm( float2 p )
	{
		return ValueNoise( p ) * 0.55 + ValueNoise( p * 2.13 + 7.7 ) * 0.28 + ValueNoise( p * 4.31 + 13.1 ) * 0.17;
	}

	float RumbleBump( float2 localPos )
	{
		float bt = g_flTime * g_flRumbleSpeed;
		float n1 = Fbm( localPos * 0.012 + float2( bt * 0.35, bt * 0.55 ) );
		float n2 = Fbm( localPos * 0.034 + float2( -bt * 0.6, 3.7 + bt * 0.9 ) );
		return n1 * 0.7 + n2 * 0.3;
	}
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput v )
	{
		float4 tc = float4( v.vTexCoord, v.vTexCoord2 );

		float heightNorm = tc.z;
		float bump = RumbleBump( tc.xy );
		float lift = lerp( 0.6, 1.0, heightNorm );
		float displace = ( bump - 0.40 ) * g_flRumble * g_flMoundHeight * 0.75 * lift;

		v.vPositionOs.xyz += v.vNormalOs.xyz * displace;

		PixelInput o = ProcessVertex( v );
		o.vTextureCoords = tc;
		return FinalizeVertex( o );
	}
}

PS
{
	#include "common/pixel.hlsl"

	RenderState( DepthWriteEnable, true );
	RenderState( CullMode, NONE );

	float3 g_vFoamColor < Attribute( "FoamColor" ); Default3( 0.99, 1.00, 1.00 ); >;
	float3 g_vShadowColor < Attribute( "ShadowColor" ); Default3( 0.72, 0.82, 0.92 ); >;
	float g_flCrevice < Attribute( "Crevice" ); Default( 1.0 ); >;
	float g_flMistyTop < Attribute( "MistyTop" ); Default( 0.45 ); >;

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float2 localPos = i.vTextureCoords.xy;
		float heightNorm = i.vTextureCoords.z;

		float bt = g_flTime * g_flRumbleSpeed;

		float depth3d = Fbm( localPos * 0.03 + float2( bt * 0.5, -bt * 0.7 ) );
		float crevice = smoothstep( 0.55, 0.30, depth3d ) * g_flCrevice;

		float3 foam = lerp( g_vFoamColor, g_vShadowColor, crevice );

		float bump = RumbleBump( localPos );
		float crestNorm = saturate( heightNorm * ( 0.55 + bump * 0.6 ) );

		foam = lerp( foam, float3( 1.0, 1.0, 1.0 ), smoothstep( 0.55, 0.85, crestNorm ) * 0.5 );

		if ( g_flMistyTop > 0.01 )
		{
			float crest = smoothstep( 1.0 - g_flMistyTop * 0.5, 1.0, crestNorm );
			float tear = Fbm( localPos * 0.05 + float2( bt * 0.8, bt * 1.3 ) );

			if ( crest > 0.0 && tear + 0.15 < crest )
				discard;
		}

		return float4( foam, 1.0 );
	}
}
