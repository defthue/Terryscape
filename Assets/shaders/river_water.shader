HEADER
{
	Description = "Stylized toon river water for Terry's Quest";
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
	float g_flFlowSpeed < Attribute( "FlowSpeed" ); Default( 40.0 ); >;
	float g_flWaveIrregularity < Attribute( "WaveIrregularity" ); Default( 0.5 ); >;

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
		float2 u = f * f * f * ( f * ( f * 6.0 - 15.0 ) + 10.0 );
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



	float2 RibbonPos( float4 texCoords )
	{
		float arc = texCoords.z * 128.0 + texCoords.x;
		return float2( arc - g_flFlowSpeed * g_flTime, texCoords.y );
	}

	float ComputeWave( float2 localPos )
	{
		float2 dir = normalize( float2( 1.0, 0.25 ) );
		float2 perp = float2( -dir.y, dir.x );
		float phase1 = dot( localPos, dir ) * ( 6.28318530 / g_flWaveLength ) + g_flTime * g_flWaveSpeed;
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
		float4 tc = float4( v.vTexCoord, v.vTexCoord2 );
		float2 localPos = RibbonPos( tc );
		float wave = ComputeWave( localPos );

		v.vPositionOs.xyz += v.vNormalOs.xyz * wave;

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

	float3 g_vShallowColor < Attribute( "ShallowColor" ); Default3( 0.25, 0.85, 0.95 ); >;
	float3 g_vDeepColor < Attribute( "DeepColor" ); Default3( 0.10, 0.45, 0.75 ); >;
	float3 g_vFoamColor < Attribute( "FoamColor" ); Default3( 0.97, 0.99, 1.00 ); >;
	float g_flShallowOpacity < Attribute( "ShallowOpacity" ); Default( 0.60 ); >;
	float g_flDeepOpacity < Attribute( "DeepOpacity" ); Default( 0.90 ); >;
	float g_flDepthFade < Attribute( "DepthFade" ); Default( 140.0 ); >;
	float g_flFoamSize < Attribute( "FoamSize" ); Default( 26.0 ); >;
	float g_flFoamWobble < Attribute( "FoamWobble" ); Default( 0.5 ); >;
	float g_flFoamSpeed < Attribute( "FoamSpeed" ); Default( 1.0 ); >;
	float g_flFoamDetail < Attribute( "FoamDetail" ); Default( 33.0 ); >;
	float g_flSparkleStrength < Attribute( "SparkleStrength" ); Default( 0.12 ); >;
	float g_flSparkleSize < Attribute( "SparkleSize" ); Default( 125.0 ); >;
	float g_flSparkleSpeed < Attribute( "SparkleSpeed" ); Default( 1.0 ); >;
	float g_flSparkleDensity < Attribute( "SparkleDensity" ); Default( 0.5 ); >;
	float g_flSparkleParallax < Attribute( "SparkleParallax" ); Default( 0.12 ); >;
	float g_flStreakStrength < Attribute( "StreakStrength" ); Default( 0.7 ); >;
	float g_flStreakSpacing < Attribute( "StreakSpacing" ); Default( 35.0 ); >;
	float g_flStreakWidth < Attribute( "StreakWidth" ); Default( 4.0 ); >;
	float g_flStreakCoverage < Attribute( "StreakCoverage" ); Default( 0.5 ); >;
	float g_flStreakLength < Attribute( "StreakLength" ); Default( 300.0 ); >;
	float g_flStreakWarp < Attribute( "StreakWarp" ); Default( 20.0 ); >;
	float g_flStreakSpeedVariation < Attribute( "StreakSpeedVariation" ); Default( 0.5 ); >;

	float StreakLines( float2 localPos )
	{
		if ( g_flStreakStrength <= 0.0 )
			return 0.0;

		float spacing = max( g_flStreakSpacing, 4.0 );
		float stripeIndex = floor( localPos.y / spacing );
		float stripeCoord = ( frac( localPos.y / spacing ) - 0.5 ) * spacing;
		float stripeHash = Hash1D( stripeIndex + 5.1 );

		float speedMul = 0.10 + ( Hash1D( stripeIndex + 91.7 ) - 0.5 ) * 0.5 * g_flStreakSpeedVariation;
		float strandX = localPos.x - g_flFlowSpeed * speedMul * g_flTime;

		float bend = ( ValueNoise( float2( strandX * 0.003, stripeIndex * 7.31 ) ) - 0.5 ) * 2.0 * g_flStreakWarp;

		float segmentLength = max( g_flStreakLength, 20.0 );
		float n = ValueNoise1D( strandX / segmentLength + stripeHash * 57.0 );
		float threshold = 1.0 - g_flStreakCoverage;
		float band = max( g_flStreakCoverage * 0.5, 0.001 );
		float headroom = saturate( ( n - threshold ) / band );

		float halfWidth = g_flStreakWidth * headroom;
		return 1.0 - smoothstep( halfWidth - 1.2, halfWidth, abs( stripeCoord - bend ) );
	}

	float SparkleField( float2 localPos, float size, float2 domainOffset, float thresholdBoost )
	{
		float2 surfUv = localPos / size + domainOffset;
		float n1 = ValueNoise( surfUv + float2( g_flTime * g_flSparkleSpeed * 0.04, 0.0 ) );
		float2 slowPos = localPos + float2( g_flFlowSpeed * g_flSparkleParallax * g_flTime, 0.0 );
		float2 slowUv = slowPos / size + domainOffset;
		float2 rotUv = float2( slowUv.x * 0.83 - slowUv.y * 0.55, slowUv.x * 0.55 + slowUv.y * 0.83 );
		float n2 = ValueNoise( rotUv * 1.7 - float2( 0.0, g_flTime * g_flSparkleSpeed * 0.03 ) );
		float threshold = 0.88 + thresholdBoost;
		return smoothstep( threshold, threshold + 0.04, n1 * n2 * 2.0 );
	}

	float SparkleCells( float2 localPos )
	{
		float size = max( g_flSparkleSize, 1.0 );

		float lowT = saturate( g_flSparkleDensity * 2.0 );
		float cullNoise = ValueNoise( localPos / ( size * 1.4 ) + 61.3 );
		float baseBoost = ( 1.0 - lowT ) * cullNoise * 1.4;
		float cells = SparkleField( localPos, size, float2( 0.0, 0.0 ), baseBoost );

		float highT = saturate( ( g_flSparkleDensity - 0.5 ) * 2.0 );
		if ( highT > 0.0 )
		{
			float growNoise = ValueNoise( localPos / ( size * 1.4 ) + 113.7 );
			float extraBoost = ( 1.0 - highT ) * ( 0.4 + growNoise * 1.0 );
			float extraCells = SparkleField( localPos, size, float2( 47.9, 23.1 ), extraBoost );
			cells = max( cells, extraCells );
		}

		return cells;
	}

	float4 MainPs( PixelInput i, bool bIsFrontFace : SV_IsFrontFace ) : SV_Target0
	{
		float2 localPos = RibbonPos( i.vTextureCoords );

		float cells = SparkleCells( localPos );

		if ( !bIsFrontFace )
			return float4( g_vDeepColor + g_flSparkleStrength * 0.5 * cells, g_flDeepOpacity );

		float3 worldPos = i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz;
		float3 scenePos = Depth::GetWorldPosition( i.vPositionSs.xy );
		float depthBelow = length( worldPos - scenePos );

		float depthT = saturate( depthBelow / g_flDepthFade );
		depthT = floor( depthT * 3.0 ) / 3.0 + 0.15;
		depthT = saturate( depthT );

		float3 waterColor = lerp( g_vShallowColor, g_vDeepColor, depthT );
		float alpha = lerp( g_flShallowOpacity, g_flDeepOpacity, depthT );

		waterColor += g_flSparkleStrength * cells;

		float streak = StreakLines( localPos ) * g_flStreakStrength;
		waterColor = lerp( waterColor, g_vFoamColor, streak );

		float2 foamUv = localPos / max( g_flFoamDetail, 1.0 );
		float foamNoise = ValueNoise( foamUv + g_flTime * g_flFoamSpeed * 0.18 );
		float band = lerp( 1.0, 0.55 + 0.9 * foamNoise, g_flFoamWobble );
		float foam = step( depthBelow, g_flFoamSize * band );

		float3 finalColor = lerp( waterColor, g_vFoamColor, foam );
		float finalAlpha = lerp( alpha, 0.95, foam );

		return float4( finalColor, finalAlpha );
	}
}
