HEADER
{
	Description = "Stylized toon pond and ocean water for Terry's Quest";
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
	#define S_TRANSLUCENT 1

	float g_flWaveHeight < Attribute( "WaveHeight" ); Default( 6.0 ); >;
	float g_flWaveLength < Attribute( "WaveLength" ); Default( 220.0 ); >;
	float g_flWaveSpeed < Attribute( "WaveSpeed" ); Default( 1.0 ); >;
	float g_flWaveDirectionDegrees < Attribute( "WaveDirectionDegrees" ); Default( 15.0 ); >;
	float g_flWaveIrregularity < Attribute( "WaveIrregularity" ); Default( 0.5 ); >;
	float2 g_vSurfaceSize < Attribute( "SurfaceSize" ); Default2( 1024.0, 1024.0 ); >;

	float2 HashCell( float2 p )
	{
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

	float2 PlaneLocalPos( float2 uv )
	{
		return ( uv - 0.5 ) * g_vSurfaceSize;
	}

	float2 WaveDir()
	{
		float rad = g_flWaveDirectionDegrees * 0.01745329;
		return float2( cos( rad ), sin( rad ) );
	}

	float PrimaryWavePhase( float2 localPos )
	{
		return dot( localPos, WaveDir() ) * ( 6.28318530 / g_flWaveLength ) + g_flTime * g_flWaveSpeed;
	}

	float ComputeWave( float2 localPos )
	{
		float2 dir = WaveDir();
		float2 perp = float2( -dir.y, dir.x );
		float phase1 = PrimaryWavePhase( localPos );
		float phase2 = dot( localPos, perp ) * ( 6.28318530 / ( g_flWaveLength * 0.437 ) ) + g_flTime * g_flWaveSpeed * 1.618;
		float ampNoise = ValueNoise( localPos * 0.0015 + float2( g_flTime * 0.02, 0.0 ) );
		float amp = g_flWaveHeight * lerp( 1.0, 0.35 + 1.3 * ampNoise, g_flWaveIrregularity );
		return ( sin( phase1 ) + sin( phase2 ) * 0.5 ) * amp;
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
		float2 localPos = PlaneLocalPos( v.vTexCoord.xy );
		float wave = ComputeWave( localPos );

		v.vPositionOs.z += wave;

		PixelInput o = ProcessVertex( v );
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

	float3 g_vShallowColor < Attribute( "ShallowColor" ); Default3( 0.25, 0.85, 0.95 ); >;
	float3 g_vDeepColor < Attribute( "DeepColor" ); Default3( 0.10, 0.45, 0.75 ); >;
	float3 g_vFoamColor < Attribute( "FoamColor" ); Default3( 0.97, 0.99, 1.00 ); >;
	float g_flShallowOpacity < Attribute( "ShallowOpacity" ); Default( 0.60 ); >;
	float g_flDeepOpacity < Attribute( "DeepOpacity" ); Default( 0.90 ); >;
	float g_flDepthFade < Attribute( "DepthFade" ); Default( 140.0 ); >;
	float g_flCrestHighlight < Attribute( "CrestHighlight" ); Default( 0.1 ); >;
	float g_flFoamSize < Attribute( "FoamSize" ); Default( 26.0 ); >;
	float g_flFoamWobble < Attribute( "FoamWobble" ); Default( 0.5 ); >;
	float g_flFoamSpeed < Attribute( "FoamSpeed" ); Default( 1.0 ); >;
	float g_flSparkleStrength < Attribute( "SparkleStrength" ); Default( 0.12 ); >;
	float g_flSparkleSize < Attribute( "SparkleSize" ); Default( 125.0 ); >;
	float g_flSparkleSpeed < Attribute( "SparkleSpeed" ); Default( 1.0 ); >;
	float g_flSparkleDensity < Attribute( "SparkleDensity" ); Default( 0.5 ); >;
	float g_flSparkleParallax < Attribute( "SparkleParallax" ); Default( 0.12 ); >;

	float SparkleField( float2 uv, float2 domainOffset, float thresholdBoost )
	{
		float n1 = ValueNoise( uv + domainOffset + float2( g_flTime * g_flSparkleSpeed * 0.04, 0.0 ) );
		float n2 = ValueNoise( uv * 1.7 + domainOffset - float2( 0.0, g_flTime * g_flSparkleSpeed * 0.03 ) );
		return step( 0.88 + thresholdBoost, n1 * n2 * 2.0 );
	}

	float SparkleCells( float2 localPos )
	{
		float size = max( g_flSparkleSize, 1.0 );
		float2 uv = localPos / size;

		float2 drift = float2( 0.93, 0.37 ) * g_flTime * g_flSparkleParallax * 0.03;

		float cullA = ValueNoise( uv * 0.7 + 61.3 );
		float cullB = ValueNoise( uv * 0.7 + 113.7 );
		float boostA = ( 1.0 - g_flSparkleDensity ) * cullA * 2.0;
		float boostB = ( 1.0 - g_flSparkleDensity ) * cullB * 2.0;

		float fieldA = SparkleField( uv + drift, float2( 0.0, 0.0 ), boostA );
		float fieldB = SparkleField( uv - drift, float2( 47.9, 23.1 ), boostB );

		return max( fieldA, fieldB * 0.75 );
	}

	float4 MainPs( PixelInput i, bool bIsFrontFace : SV_IsFrontFace ) : SV_Target0
	{
		float2 localPos = PlaneLocalPos( i.vTextureCoords.xy );

		float cells = SparkleCells( localPos );

		if ( !bIsFrontFace )
			return float4( g_vDeepColor + g_flSparkleStrength * 0.5 * cells, g_flDeepOpacity );

		float3 worldPos = i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz;
		float3 scenePos = Depth::GetWorldPosition( i.vPositionSs.xy );
		float depthBelow = max( worldPos.z - scenePos.z, 0.0 );

		float depthT = saturate( depthBelow / g_flDepthFade );
		depthT = floor( depthT * 3.0 ) / 3.0 + 0.15;
		depthT = saturate( depthT );

		float3 waterColor = lerp( g_vShallowColor, g_vDeepColor, depthT );
		float alpha = lerp( g_flShallowOpacity, g_flDeepOpacity, depthT );

		waterColor += g_flSparkleStrength * cells;

		float waveT = sin( PrimaryWavePhase( localPos ) ) * 0.5 + 0.5;
		float crest = step( 0.75, waveT );
		waterColor += g_flCrestHighlight * crest;

		float2 foamUv = localPos * 0.03;
		float foamNoise = ValueNoise( foamUv + g_flTime * g_flFoamSpeed * 0.18 );
		float band = lerp( 1.0, 0.55 + 0.9 * foamNoise, g_flFoamWobble );
		float foam = step( depthBelow, g_flFoamSize * band );

		float3 finalColor = lerp( waterColor, g_vFoamColor, foam );
		float finalAlpha = lerp( alpha, 0.95, foam );

		return float4( finalColor, finalAlpha );
	}
}
