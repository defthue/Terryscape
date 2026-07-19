HEADER
{
	Description = "Stylized toon water for Terry's Quest";
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

	float g_flWaveAmplitude < Attribute( "WaveAmplitude" ); Default( 6.0 ); >;
	float g_flWaveLength < Attribute( "WaveLength" ); Default( 220.0 ); >;
	float g_flWaveSpeed < Attribute( "WaveSpeed" ); Default( 1.0 ); >;
	float2 g_vWaveDirection < Attribute( "WaveDirection" ); Default2( 1.0, 0.3 ); >;
	float g_flWaveSecondOctave < Attribute( "WaveSecondOctave" ); Default( 0.5 ); >;
	float2 g_vSurfaceSize < Attribute( "SurfaceSize" ); Default2( 1024.0, 1024.0 ); >;
	float2 g_vFlowDirection < Attribute( "FlowDirection" ); Default2( 0.0, -1.0 ); >;
	float g_flFlowSpeed < Attribute( "FlowSpeed" ); Default( 0.0 ); >;
	float g_flRibbonMode < Attribute( "RibbonMode" ); Default( 0.0 ); >;

	float2 FlowOffset()
	{
		float len = length( g_vFlowDirection );
		if ( len < 0.0001 )
			return float2( 0.0, 0.0 );
		return ( g_vFlowDirection / len ) * g_flFlowSpeed * g_flTime;
	}

	float2 PlaneLocalPos( float2 uv )
	{
		return ( uv - 0.5 ) * g_vSurfaceSize;
	}

	float2 DriftedLocalPos( float2 uv )
	{
		if ( g_flRibbonMode >= 0.5 )
			return float2( uv.x - g_flFlowSpeed * g_flTime, uv.y );
		return PlaneLocalPos( uv ) - FlowOffset();
	}

	float PrimaryWavePhase( float2 localPos )
	{
		float2 dir = normalize( g_vWaveDirection );
		return dot( localPos, dir ) * ( 6.28318530 / g_flWaveLength ) + g_flTime * g_flWaveSpeed;
	}

	float ComputeWave( float2 localPos )
	{
		float2 dir = normalize( g_vWaveDirection );
		float2 perp = float2( -dir.y, dir.x );
		float phase1 = PrimaryWavePhase( localPos );
		float phase2 = dot( localPos, perp ) * ( 6.28318530 / ( g_flWaveLength * 0.4 ) ) + g_flTime * g_flWaveSpeed * 1.7;
		return ( sin( phase1 ) + sin( phase2 ) * g_flWaveSecondOctave ) * g_flWaveAmplitude;
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
		float2 localPos = DriftedLocalPos( v.vTexCoord.xy );
		float wave = ComputeWave( localPos );

		if ( g_flRibbonMode >= 0.5 )
			v.vPositionOs.xyz += v.vNormalOs.xyz * wave;
		else
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
	float g_flDepthFadeDistance < Attribute( "DepthFadeDistance" ); Default( 140.0 ); >;
	float g_flFoamDistance < Attribute( "FoamDistance" ); Default( 26.0 ); >;
	float g_flFoamNoiseScale < Attribute( "FoamNoiseScale" ); Default( 0.03 ); >;
	float g_flFoamScrollSpeed < Attribute( "FoamScrollSpeed" ); Default( 18.0 ); >;
	float g_flSurfaceNoiseScale < Attribute( "SurfaceNoiseScale" ); Default( 0.008 ); >;
	float g_flSurfaceScrollSpeed < Attribute( "SurfaceScrollSpeed" ); Default( 10.0 ); >;
	float g_flSurfaceHighlight < Attribute( "SurfaceHighlight" ); Default( 0.12 ); >;
	float g_flShallowAlpha < Attribute( "ShallowAlpha" ); Default( 0.60 ); >;
	float g_flDeepAlpha < Attribute( "DeepAlpha" ); Default( 0.90 ); >;
	float g_flWaveCrestTint < Attribute( "WaveCrestTint" ); Default( 0.10 ); >;
	float g_flPatchScale < Attribute( "PatchScale" ); Default( 0.01 ); >;
	float g_flPatchDetailScale < Attribute( "PatchDetailScale" ); Default( 0.05 ); >;
	float g_flPatchDetailStrength < Attribute( "PatchDetailStrength" ); Default( 0.6 ); >;
	float g_flPatchCoverage < Attribute( "PatchCoverage" ); Default( 0.22 ); >;
	float g_flPatchDriftSpeed < Attribute( "PatchDriftSpeed" ); Default( 0.02 ); >;
	float g_flPatchHaloStrength < Attribute( "PatchHaloStrength" ); Default( 0.45 ); >;
	float g_flStreakStrength < Attribute( "StreakStrength" ); Default( 0.0 ); >;
	float g_flStreakLaneSpacing < Attribute( "StreakLaneSpacing" ); Default( 30.0 ); >;
	float g_flStreakWidth < Attribute( "StreakWidth" ); Default( 4.0 ); >;
	float g_flStreakLengthFrequency < Attribute( "StreakLengthFrequency" ); Default( 0.004 ); >;
	float g_flStreakCoverage < Attribute( "StreakCoverage" ); Default( 0.35 ); >;
	float g_flStreakWobbleFrequency < Attribute( "StreakWobbleFrequency" ); Default( 0.003 ); >;
	float g_flStreakWobbleAmount < Attribute( "StreakWobbleAmount" ); Default( 8.0 ); >;
	float g_flStreakCrestBias < Attribute( "StreakCrestBias" ); Default( 0.5 ); >;

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

	float ErodeNoise( float baseN, float detailN, float strength )
	{
		return baseN * ( 1.0 + strength * ( detailN * 2.0 - 1.0 ) );
	}

	float PatchNoise( float2 localPos )
	{
		float2 drift = g_flTime * g_flPatchDriftSpeed * float2( 1.0, 0.35 );
		float baseN = ValueNoise( localPos * g_flPatchScale + drift );
		float detailN = ValueNoise( localPos * g_flPatchDetailScale + drift * 1.7 );
		return ErodeNoise( baseN, detailN, g_flPatchDetailStrength );
	}

	float StreakStrands( float along, float across, float waveT )
	{
		float spacing = max( g_flStreakLaneSpacing, 1.0 );
		float laneIndex = floor( across / spacing );
		float laneCoord = ( frac( across / spacing ) - 0.5 ) * spacing;
		float laneHash = Hash1D( laneIndex + 13.37 );

		float n = ValueNoise1D( along * g_flStreakLengthFrequency + laneHash * 97.0 );
		n *= lerp( 1.0, waveT, g_flStreakCrestBias );

		float threshold = 1.0 - g_flStreakCoverage;
		float taperBand = max( g_flStreakCoverage * 0.5, 0.001 );
		float headroom = saturate( ( n - threshold ) / taperBand );

		float wobble = ( ValueNoise1D( along * g_flStreakWobbleFrequency + laneHash * 211.0 ) - 0.5 ) * 2.0 * g_flStreakWobbleAmount;

		float halfWidth = g_flStreakWidth * headroom;
		float dist = abs( laneCoord - wobble );
		return 1.0 - smoothstep( halfWidth - 1.5, halfWidth, dist );
	}

	float StreakMask( float2 localPos, float waveT )
	{
		if ( g_flStreakStrength <= 0.0 )
			return 0.0;

		float along;
		float across;
		if ( g_flRibbonMode >= 0.5 )
		{
			along = localPos.x;
			across = localPos.y;
		}
		else
		{
			float len = length( g_vFlowDirection );
			if ( len < 0.0001 )
				return 0.0;

			float2 dir = g_vFlowDirection / len;
			float2 perp = float2( -dir.y, dir.x );
			along = dot( localPos, dir );
			across = dot( localPos, perp );
		}

		return StreakStrands( along, across, waveT );
	}

	float4 MainPs( PixelInput i, bool bIsFrontFace : SV_IsFrontFace ) : SV_Target0
	{
		float2 localPos = DriftedLocalPos( i.vTextureCoords.xy );
		float3 worldPos = i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz;

		float2 surfUv = worldPos.xy * g_flSurfaceNoiseScale;
		float n1 = ValueNoise( surfUv + float2( g_flTime * g_flSurfaceScrollSpeed * 0.004, 0.0 ) );
		float2 rotUv = float2( surfUv.x * 0.83 - surfUv.y * 0.55, surfUv.x * 0.55 + surfUv.y * 0.83 );
		float n2 = ValueNoise( rotUv * 1.7 - float2( 0.0, g_flTime * g_flSurfaceScrollSpeed * 0.003 ) );
		float cells = smoothstep( 0.88, 0.92, n1 * n2 * 2.0 );

		if ( !bIsFrontFace )
			return float4( g_vDeepColor + g_flSurfaceHighlight * 0.5 * cells, g_flDeepAlpha );

		float3 scenePos = Depth::GetWorldPosition( i.vPositionSs.xy );
		float depthBelow = max( worldPos.z - scenePos.z, 0.0 );

		float depthT = saturate( depthBelow / g_flDepthFadeDistance );
		depthT = floor( depthT * 3.0 ) / 3.0 + 0.15;
		depthT = saturate( depthT );

		float3 waterColor = lerp( g_vShallowColor, g_vDeepColor, depthT );
		float alpha = lerp( g_flShallowAlpha, g_flDeepAlpha, depthT );

		waterColor += g_flSurfaceHighlight * cells;

		float waveT = sin( PrimaryWavePhase( localPos ) ) * 0.5 + 0.5;
		float crest = step( 0.75, waveT );

		float streak = StreakMask( localPos, waveT ) * g_flStreakStrength;
		waterColor = lerp( waterColor, g_vFoamColor, streak );

		waterColor += g_flWaveCrestTint * crest;

		float patchNoise = PatchNoise( localPos );
		float halo = step( 1.0 - g_flPatchCoverage, patchNoise );
		float core = step( 1.0 - g_flPatchCoverage * 0.5, patchNoise );
		waterColor = lerp( waterColor, g_vFoamColor, halo * g_flPatchHaloStrength );
		waterColor = lerp( waterColor, g_vFoamColor, core );
		alpha = lerp( alpha, 0.95, core );

		float2 foamUv = worldPos.xy * g_flFoamNoiseScale;
		float foamNoise = ValueNoise( foamUv + g_flTime * g_flFoamScrollSpeed * 0.01 );
		float foamThreshold = g_flFoamDistance * ( 0.55 + 0.9 * foamNoise );
		float foam = step( depthBelow, foamThreshold );

		float3 finalColor = lerp( waterColor, g_vFoamColor, foam );
		float finalAlpha = lerp( alpha, 0.95, foam );

		return float4( finalColor, finalAlpha );
	}
}
