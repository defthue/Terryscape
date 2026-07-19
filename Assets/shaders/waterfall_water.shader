HEADER
{
	Description = "Stylized toon waterfall for Terry's Quest";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
}

COMMON
{
	#include "common/shared.hlsl"
	#define S_TRANSLUCENT 1

	float g_flPatternSpeed < Attribute( "PatternSpeed" ); Default( 280.0 ); >;
	float g_flWaveHeight < Attribute( "WaveHeight" ); Default( 5.0 ); >;
	float g_flWaveLength < Attribute( "WaveLength" ); Default( 150.0 ); >;

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

	float Hash1D( float p )
	{
		p = p - 256.0 * floor( p / 256.0 );
		return frac( sin( p * 127.1 ) * 43758.5453 );
	}

	float ValueNoise1D( float p )
	{
		float cell = floor( p );
		float f = frac( p );
		float u = f * f * ( 3.0 - 2.0 * f );
		return lerp( Hash1D( cell ), Hash1D( cell + 1.0 ), u );
	}

	float FallCoord( float4 texCoords )
	{
		return ( texCoords.x - g_flTime ) * g_flPatternSpeed;
	}

	float WavePhase( float u, float perimeter )
	{
		float phase1 = u * ( 6.28318530 / g_flWaveLength ) + perimeter * 0.015;
		float phase2 = perimeter * ( 6.28318530 / ( g_flWaveLength * 0.437 ) ) + g_flTime * 1.618;
		return sin( phase1 ) + sin( phase2 ) * 0.4;
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

		if ( tc.z > -0.5 )
		{
			float wave = WavePhase( FallCoord( tc ), tc.y ) * g_flWaveHeight;
			v.vPositionOs.xyz += v.vNormalOs.xyz * wave;
		}

		PixelInput o = ProcessVertex( v );
		o.vTextureCoords = tc;
		return FinalizeVertex( o );
	}
}

PS
{
	#include "common/pixel.hlsl"
	#include "common/classes/Depth.hlsl"

	RenderState( BlendEnable, true );
	RenderState( SrcBlend, SRC_ALPHA );
	RenderState( DstBlend, INV_SRC_ALPHA );
	RenderState( DepthWriteEnable, false );
	RenderState( CullMode, NONE );

	float3 g_vBrightColor < Attribute( "BrightColor" ); Default3( 0.35, 0.80, 0.97 ); >;
	float3 g_vBaseColor < Attribute( "BaseColor" ); Default3( 0.16, 0.55, 0.85 ); >;
	float3 g_vFoamColor < Attribute( "FoamColor" ); Default3( 0.97, 0.99, 1.00 ); >;
	float g_flOpacity < Attribute( "Opacity" ); Default( 0.85 ); >;
	float g_flFoaminess < Attribute( "Foaminess" ); Default( 0.5 ); >;
	float g_flLipFoamFrac < Attribute( "LipFoamFrac" ); Default( 0.05 ); >;
	float g_flEdgeFoam < Attribute( "EdgeFoam" ); Default( 0.5 ); >;
	float g_flBottomFroth < Attribute( "BottomFroth" ); Default( 0.5 ); >;
	float g_flContactFoam < Attribute( "ContactFoam" ); Default( 35.0 ); >;
	float g_flRipples < Attribute( "Ripples" ); Default( 0.6 ); >;
	float g_flRippleSpeed < Attribute( "RippleSpeed" ); Default( 1.0 ); >;
	float g_flRippleReach < Attribute( "RippleReach" ); Default( 1.8 ); >;
	float g_flRippleSpacing < Attribute( "RippleSpacing" ); Default( 60.0 ); >;
	float g_flRippleNoise < Attribute( "RippleNoise" ); Default( 0.5 ); >;
	float g_flRippleThickness < Attribute( "RippleThickness" ); Default( 9.0 ); >;
	float StreakMask( float u, float perimeter )
	{
		float spacing = 34.0 - g_flFoaminess * 14.0;
		float stripeIndex = floor( perimeter / spacing );
		float stripeCoord = ( frac( perimeter / spacing ) - 0.5 ) * spacing;
		float stripeHash = Hash1D( stripeIndex + 5.1 );

		float bend = ( ValueNoise( float2( u * 0.004, stripeIndex * 7.31 ) ) - 0.5 ) * 8.0;

		float n = ValueNoise1D( u * 0.006 + stripeHash * 57.0 );
		float coverage = 0.30 + g_flFoaminess * 0.40;
		float threshold = 1.0 - coverage;
		float band = max( coverage * 0.5, 0.001 );
		float headroom = saturate( ( n - threshold ) / band );

		float halfWidth = ( 2.5 + g_flFoaminess * 3.0 ) * headroom;
		return 1.0 - smoothstep( halfWidth - 1.2, halfWidth, abs( stripeCoord - bend ) );
	}

	float4 RipplePs( float4 tc )
	{
		float2 localPos = tc.xy;
		float segHalf = -tc.z - 1.0;
		float halfOut = max( tc.w, 1.0 );

		float segY = clamp( localPos.y, -segHalf, segHalf );
		float2 d = float2( localPos.x, localPos.y - segY );
		float angle = atan2( d.y, d.x );

		float wobble = ( ValueNoise( float2( angle * 3.0, g_flTime * g_flRippleSpeed * 0.5 ) ) - 0.5 ) * g_flRippleNoise * 0.25;
		float ringNorm = length( d ) / halfOut + wobble;

		if ( ringNorm < 0.75 )
			discard;

		float fade = 1.0 - smoothstep( 1.0, g_flRippleReach, ringNorm );
		if ( fade <= 0.001 )
			discard;

		float spacing = max( g_flRippleSpacing, 5.0 );
		float phase = ringNorm * halfOut / spacing - g_flTime * g_flRippleSpeed * 1.4;
		float halfBand = clamp( g_flRippleThickness / spacing * 0.5, 0.01, 0.45 );
		float band = step( abs( frac( phase ) - 0.72 ), halfBand );

		float erode = ValueNoise( float2( angle * 6.0, ringNorm * 4.0 - g_flTime * g_flRippleSpeed * 0.8 ) );
		band *= step( g_flRippleNoise * 0.45, erode );

		float alpha = band * fade * g_flRipples * 0.55;

		if ( alpha < 0.01 )
			discard;

		return float4( g_vFoamColor, alpha );
	}

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		if ( i.vTextureCoords.z < -0.5 )
			return RipplePs( i.vTextureCoords );

		float perimeter = i.vTextureCoords.y;
		float alongNorm = i.vTextureCoords.z;
		float edge = i.vTextureCoords.w;
		float u = FallCoord( i.vTextureCoords );

		float3 worldPos = i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz;

		float contact = 0.0;
		if ( g_flContactFoam > 0.5 )
		{
			float3 scenePos = Depth::GetWorldPosition( i.vPositionSs.xy );
			float sceneDist = length( worldPos - scenePos );
			float contactNoise = ValueNoise( float2( perimeter * 0.045, u * 0.012 ) );
			float band = g_flContactFoam * ( 0.55 + 0.9 * contactNoise );
			contact = step( sceneDist, band );
		}

		float shade = ValueNoise( float2( perimeter * 0.02, u * 0.004 ) );
		float3 waterColor = lerp( g_vBrightColor, g_vBaseColor, 0.35 + 0.5 * shade );

		float streak = StreakMask( u, perimeter );
		waterColor = lerp( waterColor, g_vFoamColor, streak );

		float edgeNoise = ValueNoise( float2( u * 0.01, perimeter * 0.05 ) );
		float edgeMask = saturate( edge * g_flEdgeFoam * ( 1.2 + edgeNoise ) );
		edgeMask = smoothstep( 0.4, 0.8, edgeMask );

		float lipNoise = ValueNoise( float2( perimeter * 0.04, g_flTime * 0.6 ) );
		float lip = step( alongNorm, g_flLipFoamFrac * ( 0.5 + lipNoise ) );

		float frothStart = 1.0 - 0.25 * g_flBottomFroth;
		float frothZone = saturate( ( alongNorm - frothStart ) / max( 1.0 - frothStart, 0.001 ) );
		float frothNoise = ValueNoise( float2( perimeter * 0.06, u * 0.02 ) );
		float froth = step( 1.0 - frothZone * 0.8, frothNoise ) * step( 0.01, g_flBottomFroth );

		float foamMask = max( max( max( streak, edgeMask ), max( lip, froth ) ), contact );
		float3 finalColor = lerp( waterColor, g_vFoamColor, max( max( edgeMask, contact ), max( lip, froth ) ) );

		float alpha = lerp( g_flOpacity, 0.97, foamMask );

		return float4( finalColor, alpha );
	}
}
