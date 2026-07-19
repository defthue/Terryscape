using Sandbox;
using System;
using System.Collections.Generic;

public sealed class UnderwaterEffect : Component
{
	[Property, Group( "Water" )] public Color WaterColor { get; set; } = new Color( 0.10f, 0.42f, 0.66f );
	[Property, Group( "Water" )] public float FogDistance { get; set; } = 500f;

	[Property, Group( "Bubbles" ), Range( 0f, 1f )] public float BubbleAmount { get; set; } = 0.5f;
	[Property, Group( "Bubbles" )] public float BubbleSize { get; set; } = 5f;

	GradientFog _fog;
	float _submergence;
	bool _isUnderwater;
	List<BoxCollider> _waterBoxes = new();
	float _nextBoxScan;

	class Bubble
	{
		public GameObject Go;
		public ModelRenderer Renderer;
		public float Rise;
		public float WobblePhase;
		public float Scale;
	}

	List<Bubble> _bubbles = new();

	protected override void OnEnabled()
	{
		_fog = Components.GetOrCreate<GradientFog>();
		_fog.Enabled = false;
	}

	protected override void OnDisabled()
	{
		if ( _fog != null && _fog.IsValid() )
			_fog.Enabled = false;
		ClearBubbles();
	}

	protected override void OnUpdate()
	{
		var camera = Scene.Camera;
		if ( camera == null || !camera.IsValid() )
			return;

		Vector3 camPos = camera.WorldPosition;

		if ( Time.Now >= _nextBoxScan )
		{
			_nextBoxScan = Time.Now + 1f;
			ScanWaterBoxes();
		}

		_isUnderwater = IsInsideWater( camPos );

		float target = _isUnderwater ? 1f : 0f;
		_submergence = _submergence.LerpTo( target, Time.Delta * 8f );

		UpdateFog();
		UpdateBubbles( camPos );
	}

	void ScanWaterBoxes()
	{
		_waterBoxes.Clear();
		foreach ( var box in Scene.GetAllComponents<BoxCollider>() )
		{
			if ( box == null || !box.IsValid() )
				continue;
			if ( !box.IsTrigger )
				continue;
			if ( !box.GameObject.Tags.Has( "water" ) )
				continue;
			_waterBoxes.Add( box );
		}
	}

	bool IsInsideWater( Vector3 pos )
	{
		foreach ( var box in _waterBoxes )
		{
			if ( box == null || !box.IsValid() )
				continue;

			var go = box.GameObject;
			Vector3 local = go.WorldRotation.Inverse * ( pos - go.WorldPosition );
			local -= box.Center;

			var half = box.Scale * 0.5f;
			if ( MathF.Abs( local.x ) <= half.x && MathF.Abs( local.y ) <= half.y && MathF.Abs( local.z ) <= half.z )
				return true;
		}

		return false;
	}

	void UpdateFog()
	{
		if ( _fog == null || !_fog.IsValid() )
			return;

		if ( _submergence < 0.01f )
		{
			_fog.Enabled = false;
			return;
		}

		_fog.Enabled = true;
		_fog.Color = WaterColor.WithAlpha( _submergence );
		_fog.StartDistance = 0f;
		_fog.EndDistance = MathF.Max( FogDistance, 50f );
		_fog.FalloffExponent = 1f;
		_fog.Height = 100000f;
		_fog.VerticalFalloffExponent = 0.01f;
	}

	int TargetBubbleCount()
	{
		if ( !_isUnderwater || BubbleAmount <= 0.01f )
			return 0;
		return (int)( 4f + BubbleAmount * 12f );
	}

	void UpdateBubbles( Vector3 camPos )
	{
		int target = TargetBubbleCount();

		while ( _bubbles.Count < target )
			_bubbles.Add( CreateBubble( camPos ) );

		while ( _bubbles.Count > target )
		{
			var last = _bubbles[_bubbles.Count - 1];
			if ( last.Go != null && last.Go.IsValid() )
				last.Go.Destroy();
			_bubbles.RemoveAt( _bubbles.Count - 1 );
		}

		foreach ( var bubble in _bubbles )
			StepBubble( bubble, camPos );
	}

	Bubble CreateBubble( Vector3 camPos )
	{
		var go = Scene.CreateObject();
		go.Name = "UnderwaterBubble";
		go.Parent = GameObject;
		go.Flags |= GameObjectFlags.NotSaved | GameObjectFlags.Hidden;

		var renderer = go.Components.Create<ModelRenderer>();
		renderer.Model = Model.Load( "models/dev/sphere.vmdl" );

		var bubble = new Bubble { Go = go, Renderer = renderer };
		ResetBubble( bubble, camPos, true );
		return bubble;
	}

	void ResetBubble( Bubble bubble, Vector3 camPos, bool anywhere )
	{
		float angle = Random.Shared.Float( 0f, MathF.PI * 2f );
		float dist = Random.Shared.Float( 30f, 160f );
		float zOffset = anywhere
			? Random.Shared.Float( -80f, 60f )
			: Random.Shared.Float( -100f, -40f );

		bubble.Go.WorldPosition = camPos + new Vector3(
			MathF.Cos( angle ) * dist,
			MathF.Sin( angle ) * dist,
			zOffset );

		bubble.Rise = Random.Shared.Float( 10f, 24f );
		bubble.WobblePhase = Random.Shared.Float( 0f, 6.28f );
		bubble.Scale = Random.Shared.Float( 0.5f, 1.1f ) * MathF.Max( BubbleSize, 0.5f ) / 50f;

		bubble.Renderer.Tint = new Color( 0.85f, 0.95f, 1.00f, 0.30f );
	}

	void StepBubble( Bubble bubble, Vector3 camPos )
	{
		if ( bubble.Go == null || !bubble.Go.IsValid() )
			return;

		bubble.WobblePhase += Time.Delta * 2.2f;
		float wobbleX = MathF.Sin( bubble.WobblePhase ) * 4f * Time.Delta;
		float wobbleY = MathF.Cos( bubble.WobblePhase * 0.7f ) * 4f * Time.Delta;

		bubble.Go.WorldPosition += new Vector3( wobbleX, wobbleY, bubble.Rise * Time.Delta );
		bubble.Go.WorldScale = new Vector3( bubble.Scale );

		Vector3 rel = bubble.Go.WorldPosition - camPos;
		if ( rel.z > 80f || new Vector2( rel.x, rel.y ).Length > 220f )
			ResetBubble( bubble, camPos, false );
	}

	void ClearBubbles()
	{
		foreach ( var bubble in _bubbles )
		{
			if ( bubble.Go != null && bubble.Go.IsValid() )
				bubble.Go.Destroy();
		}
		_bubbles.Clear();
	}
}
