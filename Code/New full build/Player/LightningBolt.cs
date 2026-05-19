using Sandbox;
using System;
using System.Collections.Generic;

public sealed class LightningBolt : Component
{
	[Property] public int Segments { get; set; } = 12;
	[Property] public int MicroSubdivisions { get; set; } = 3;
	[Property] public float MicroJitterAmount { get; set; } = 4f;
	[Property] public float JitterAmount { get; set; } = 18f;
	[Property] public float EndJitter { get; set; } = 8f;
	[Property] public float Thickness { get; set; } = 1.5f;
	[Property] public float StartThicknessRatio { get; set; } = 0.3f;
	[Property] public float HaloThicknessMultiplier { get; set; } = 5.5f;
	[Property] public float HaloAlpha { get; set; } = 0.3f;

	[Property] public float RegenerateInterval { get; set; } = 0.07f;

	[Property] public bool IsMainTrunk { get; set; } = false;
	[Property] public int ForkCount { get; set; } = 4;
	[Property] public int SubForkChance { get; set; } = 65;
	[Property] public int ForkSegmentsMin { get; set; } = 2;
	[Property] public int ForkSegmentsMax { get; set; } = 4;
	[Property] public float ForkLengthMin { get; set; } = 0.15f;
	[Property] public float ForkLengthMax { get; set; } = 0.3f;
	[Property] public float ForkThicknessRatio { get; set; } = 0.3f;
	[Property] public float SubForkThicknessRatio { get; set; } = 0.18f;

	[Property] public Color BoltColor { get; set; } = new Color( 0.78f, 0.88f, 1f, 1f );
	[Property] public Color HaloColor { get; set; } = new Color( 0.4f, 0.6f, 1f, 1f );
	[Property] public float BiasStrength { get; set; } = 0.4f;

	public Vector3 OriginPosition { get; set; }
	public Vector3 TargetPosition { get; set; }

	struct SegmentPair
	{
		public ModelRenderer Core;
		public ModelRenderer Halo;
	}

	List<SegmentPair> _mainSegs = new();
	List<SegmentPair> _forkSegs = new();
	List<SegmentPair> _subForkSegs = new();

	Vector3[] _mainPoints;
	Vector2 _biasDirection;

	int _effectiveForkCount;
	bool _built;
	float _regenTimer;

	protected override void OnStart()
	{
		_biasDirection = new Vector2(
			Game.Random.Float( -1f, 1f ),
			Game.Random.Float( -1f, 1f )
		);
		if ( _biasDirection.LengthSquared < 0.01f )
			_biasDirection = new Vector2( 1f, 0f );
		_biasDirection = _biasDirection.Normal;

		_effectiveForkCount = IsMainTrunk ? ForkCount + 2 : ForkCount;

		BuildSegments();
		_built = true;
	}

	void BuildSegments()
	{
		_mainPoints = new Vector3[Segments + 1];

		int mainMicroSlots = Segments * MicroSubdivisions;
		for ( int i = 0; i < mainMicroSlots; i++ )
			_mainSegs.Add( CreateSegmentPair( $"MainMicro{i}" ) );

		int forkMicroSlots = _effectiveForkCount * ForkSegmentsMax * MicroSubdivisions;
		for ( int i = 0; i < forkMicroSlots; i++ )
			_forkSegs.Add( CreateSegmentPair( $"ForkMicro{i}" ) );

		int subForkMicroSlots = _effectiveForkCount * 3 * MicroSubdivisions;
		for ( int i = 0; i < subForkMicroSlots; i++ )
			_subForkSegs.Add( CreateSegmentPair( $"SubForkMicro{i}" ) );
	}

	SegmentPair CreateSegmentPair( string name )
	{
		var coreGo = new GameObject( true, name + "_core" );
		coreGo.SetParent( GameObject );
		var core = coreGo.Components.Create<ModelRenderer>();
		core.Model = Model.Load( "models/dev/box.vmdl" );
		core.Tint = BoltColor;

		var haloGo = new GameObject( true, name + "_halo" );
		haloGo.SetParent( GameObject );
		var halo = haloGo.Components.Create<ModelRenderer>();
		halo.Model = Model.Load( "models/dev/box.vmdl" );
		var haloColorInit = HaloColor;
		haloColorInit.a = HaloAlpha;
		halo.Tint = haloColorInit;

		return new SegmentPair { Core = core, Halo = halo };
	}

	protected override void OnUpdate()
	{
		if ( !_built )
			return;

		_regenTimer -= Time.Delta;
		if ( _regenTimer > 0f )
			return;
		_regenTimer = RegenerateInterval;

		RegenerateMainPath();
		ApplyMainSegments();
		RegenerateAndApplyForks();
	}

	void RegenerateMainPath()
	{
		_mainPoints[0] = OriginPosition;
		_mainPoints[Segments] = TargetPosition + RandomVectorInSphere( EndJitter );

		Vector3 path = TargetPosition - OriginPosition;
		if ( path.LengthSquared < 0.01f )
		{
			for ( int i = 1; i < Segments; i++ )
				_mainPoints[i] = OriginPosition;
			return;
		}

		Vector3 dir = path.Normal;
		Vector3 perpA = Vector3.Cross( dir, Vector3.Up ).Normal;
		if ( perpA.LengthSquared < 0.01f )
			perpA = Vector3.Cross( dir, Vector3.Forward ).Normal;
		Vector3 perpB = Vector3.Cross( dir, perpA ).Normal;

		Vector3 biasVec = perpA * _biasDirection.x + perpB * _biasDirection.y;

		for ( int i = 1; i < Segments; i++ )
		{
			float baseT = (float)i / Segments;
			float wiggle = Game.Random.Float( -0.5f / Segments, 0.5f / Segments );
			float t = baseT + wiggle;
			Vector3 baseP = OriginPosition + path * t;

			float startTaper = MathF.Min( 1f, t * 2.5f );
			float a = Game.Random.Float( -JitterAmount, JitterAmount ) * startTaper;
			float b = Game.Random.Float( -JitterAmount, JitterAmount ) * startTaper;
			float biasFalloff = 4f * t * ( 1f - t );

			_mainPoints[i] = baseP
				+ perpA * a
				+ perpB * b
				+ biasVec * JitterAmount * BiasStrength * biasFalloff;
		}
	}

	void ApplyMainSegments()
	{
		int rendererIndex = 0;
		float invSegments = 1f / Segments;

		for ( int i = 0; i < Segments; i++ )
		{
			float segT = ( i + 0.5f ) * invSegments;
			float brightness = 0.75f + 0.45f * segT;
			float segThickness = Thickness * ( 1f - ( 1f - StartThicknessRatio ) * segT );

			rendererIndex = RenderMicroSegments(
				_mainSegs, rendererIndex,
				_mainPoints[i], _mainPoints[i + 1],
				segThickness, brightness, 1f, MicroJitterAmount );
		}

		for ( int i = rendererIndex; i < _mainSegs.Count; i++ )
			DisableSegmentPair( _mainSegs[i] );
	}

	int RenderMicroSegments( List<SegmentPair> pool, int startIndex, Vector3 from, Vector3 to, float thickness, float brightness, float alpha, float microJitter )
	{
		Vector3 segPath = to - from;
		float length = segPath.Length;
		if ( length < 0.01f )
			return startIndex;

		Vector3 segDir = segPath / length;
		Vector3 microPerpA = Vector3.Cross( segDir, Vector3.Up ).Normal;
		if ( microPerpA.LengthSquared < 0.01f )
			microPerpA = Vector3.Cross( segDir, Vector3.Forward ).Normal;
		Vector3 microPerpB = Vector3.Cross( segDir, microPerpA ).Normal;

		Vector3 prev = from;
		int rendererIndex = startIndex;

		for ( int m = 0; m < MicroSubdivisions; m++ )
		{
			if ( rendererIndex >= pool.Count )
				break;

			Vector3 next;
			if ( m == MicroSubdivisions - 1 )
			{
				next = to;
			}
			else
			{
				float mt = (float)( m + 1 ) / MicroSubdivisions;
				Vector3 baseP = from + segPath * mt;
				float ma = Game.Random.Float( -microJitter, microJitter );
				float mb = Game.Random.Float( -microJitter, microJitter );
				next = baseP + microPerpA * ma + microPerpB * mb;
			}

			ApplySegmentPair( pool[rendererIndex], prev, next, thickness, brightness, alpha );

			prev = next;
			rendererIndex++;
		}

		return rendererIndex;
	}

	void RegenerateAndApplyForks()
	{
		int forkRendererIndex = 0;
		int subForkRendererIndex = 0;
		float forkThickness = Thickness * ForkThicknessRatio;
		float subForkThickness = Thickness * SubForkThicknessRatio;
		float forkMicroJitter = MicroJitterAmount * 0.6f;

		for ( int f = 0; f < _effectiveForkCount; f++ )
		{
			int maxBranch = Math.Max( 2, Segments / 2 );
			int parentSeg = Game.Random.Int( 1, maxBranch );
			int segCount = Game.Random.Int( ForkSegmentsMin, ForkSegmentsMax );
			float lengthRatio = Game.Random.Float( ForkLengthMin, ForkLengthMax );

			Vector3 forkStart = _mainPoints[parentSeg];

			Vector3 trunkLocalDir = ( _mainPoints[parentSeg + 1] - _mainPoints[parentSeg - 1] );
			if ( trunkLocalDir.LengthSquared < 0.01f )
				trunkLocalDir = ( TargetPosition - OriginPosition );
			trunkLocalDir = trunkLocalDir.Normal;

			Vector3 perpA = Vector3.Cross( trunkLocalDir, Vector3.Up ).Normal;
			if ( perpA.LengthSquared < 0.01f )
				perpA = Vector3.Cross( trunkLocalDir, Vector3.Forward ).Normal;
			Vector3 perpB = Vector3.Cross( trunkLocalDir, perpA ).Normal;

			float sideA = Game.Random.Float( -1f, 1f );
			float sideB = Game.Random.Float( -1f, 1f );
			Vector3 sideDir = ( perpA * sideA + perpB * sideB ).Normal;

			float pathLength = ( TargetPosition - OriginPosition ).Length;
			Vector3 forkEnd = forkStart + ( trunkLocalDir * 1.2f + sideDir * 0.5f ).Normal * pathLength * lengthRatio;

			Vector3 forkPath = forkEnd - forkStart;
			Vector3 forkDir = forkPath.Normal;
			Vector3 fPerpA = Vector3.Cross( forkDir, Vector3.Up ).Normal;
			if ( fPerpA.LengthSquared < 0.01f )
				fPerpA = Vector3.Cross( forkDir, Vector3.Forward ).Normal;
			Vector3 fPerpB = Vector3.Cross( forkDir, fPerpA ).Normal;

			Vector3 prev = forkStart;
			float forkJitter = JitterAmount * 0.55f;
			Vector3[] forkPoints = new Vector3[segCount + 1];
			forkPoints[0] = forkStart;
			forkPoints[segCount] = forkEnd;

			for ( int s = 0; s < segCount; s++ )
			{
				float t = (float)( s + 1 ) / segCount;
				Vector3 next;
				if ( s == segCount - 1 )
				{
					next = forkEnd;
				}
				else
				{
					Vector3 baseP = forkStart + forkPath * t;
					float ja = Game.Random.Float( -forkJitter, forkJitter );
					float jb = Game.Random.Float( -forkJitter, forkJitter );
					next = baseP + fPerpA * ja + fPerpB * jb;
				}
				forkPoints[s + 1] = next;

				float taper = 1f - 0.85f * ( (float)s / segCount );
				float fade = 1f - 0.5f * ( (float)s / segCount );

				forkRendererIndex = RenderMicroSegments(
					_forkSegs, forkRendererIndex,
					prev, next,
					forkThickness * taper, 1f, fade, forkMicroJitter );

				prev = next;
			}

			if ( Game.Random.Int( 1, 100 ) <= SubForkChance && segCount >= 2 )
			{
				int subParent = Game.Random.Int( 1, segCount - 1 );
				Vector3 subStart = forkPoints[subParent];

				int subSegCount = Game.Random.Int( 2, 3 );
				float subLengthRatio = Game.Random.Float( 0.4f, 0.7f ) * lengthRatio;

				Vector3 forkLocalDir = ( forkPoints[subParent + 1] - forkPoints[subParent - 1] );
				if ( forkLocalDir.LengthSquared < 0.01f )
					forkLocalDir = forkPath;
				forkLocalDir = forkLocalDir.Normal;

				Vector3 sfPerpA = Vector3.Cross( forkLocalDir, Vector3.Up ).Normal;
				if ( sfPerpA.LengthSquared < 0.01f )
					sfPerpA = Vector3.Cross( forkLocalDir, Vector3.Forward ).Normal;
				Vector3 sfPerpB = Vector3.Cross( forkLocalDir, sfPerpA ).Normal;

				float subSideA = Game.Random.Float( -1f, 1f );
				float subSideB = Game.Random.Float( -1f, 1f );
				Vector3 subSideDir = ( sfPerpA * subSideA + sfPerpB * subSideB ).Normal;
				Vector3 subEnd = subStart + ( forkLocalDir * 1.2f + subSideDir * 0.5f ).Normal * pathLength * subLengthRatio;

				Vector3 subPath = subEnd - subStart;
				Vector3 subDir = subPath.Normal;
				Vector3 sPerpA = Vector3.Cross( subDir, Vector3.Up ).Normal;
				if ( sPerpA.LengthSquared < 0.01f )
					sPerpA = Vector3.Cross( subDir, Vector3.Forward ).Normal;
				Vector3 sPerpB = Vector3.Cross( subDir, sPerpA ).Normal;

				Vector3 sPrev = subStart;
				float subJitter = forkJitter * 0.6f;

				for ( int s = 0; s < subSegCount; s++ )
				{
					float t = (float)( s + 1 ) / subSegCount;
					Vector3 next;
					if ( s == subSegCount - 1 )
					{
						next = subEnd;
					}
					else
					{
						Vector3 baseP = subStart + subPath * t;
						float ja = Game.Random.Float( -subJitter, subJitter );
						float jb = Game.Random.Float( -subJitter, subJitter );
						next = baseP + sPerpA * ja + sPerpB * jb;
					}

					float taper = 1f - 0.85f * ( (float)s / subSegCount );
					float fade = 1f - 0.6f * ( (float)s / subSegCount );

					subForkRendererIndex = RenderMicroSegments(
						_subForkSegs, subForkRendererIndex,
						sPrev, next,
						subForkThickness * taper, 1f, fade, forkMicroJitter * 0.6f );

					sPrev = next;
				}
			}
		}

		for ( int i = forkRendererIndex; i < _forkSegs.Count; i++ )
			DisableSegmentPair( _forkSegs[i] );

		for ( int i = subForkRendererIndex; i < _subForkSegs.Count; i++ )
			DisableSegmentPair( _subForkSegs[i] );
	}

	void ApplySegmentPair( SegmentPair pair, Vector3 from, Vector3 to, float thickness, float brightness, float alphaMult )
	{
		if ( pair.Core != null && pair.Core.IsValid() )
		{
			pair.Core.Enabled = true;
			PositionSegment( pair.Core.GameObject, from, to, thickness );

			var c = BoltColor;
			c.r = MathF.Min( 1f, BoltColor.r * brightness );
			c.g = MathF.Min( 1f, BoltColor.g * brightness );
			c.b = MathF.Min( 1f, BoltColor.b * brightness );
			c.a = BoltColor.a * alphaMult;
			pair.Core.Tint = c;
		}

		if ( pair.Halo != null && pair.Halo.IsValid() )
		{
			pair.Halo.Enabled = true;
			PositionSegment( pair.Halo.GameObject, from, to, thickness * HaloThicknessMultiplier );

			var c = HaloColor;
			c.a = HaloAlpha * alphaMult;
			pair.Halo.Tint = c;
		}
	}

	void DisableSegmentPair( SegmentPair pair )
	{
		if ( pair.Core != null && pair.Core.IsValid() )
			pair.Core.Enabled = false;
		if ( pair.Halo != null && pair.Halo.IsValid() )
			pair.Halo.Enabled = false;
	}

	void PositionSegment( GameObject go, Vector3 from, Vector3 to, float thickness )
	{
		Vector3 diff = to - from;
		float length = diff.Length;

		if ( length < 0.01f )
		{
			go.Enabled = false;
			return;
		}
		go.Enabled = true;

		Vector3 mid = ( from + to ) * 0.5f;
		go.WorldPosition = mid;

		Vector3 dir = diff / length;
		go.WorldRotation = Rotation.LookAt( dir );

		float boxUnit = 50f;
		go.WorldScale = new Vector3( length / boxUnit, thickness / boxUnit, thickness / boxUnit );
	}

	Vector3 RandomVectorInSphere( float radius )
	{
		return new Vector3(
			Game.Random.Float( -radius, radius ),
			Game.Random.Float( -radius, radius ),
			Game.Random.Float( -radius, radius )
		);
	}
}