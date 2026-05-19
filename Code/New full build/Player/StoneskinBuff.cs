using Sandbox;
using System;
using System.Collections.Generic;

public sealed class StoneskinBuff : Component
{
	[Sync] public float TimeRemaining { get; set; }
	[Sync] public float TotalDuration { get; set; }

	[Property] public float SpeedMultiplier { get; set; } = 0.5f;
	[Property] public float DamageMultiplier { get; set; } = 0.5f;
	[Property] public Color StoneTint { get; set; } = new Color( 0.55f, 0.55f, 0.6f, 1f );
	[Property] public float FadeOutDuration { get; set; } = 0.5f;

	[Property] public int IndicatorParticleCount { get; set; } = 5;
	[Property] public float IndicatorOrbitRadius { get; set; } = 35f;
	[Property] public float IndicatorOrbitSpeed { get; set; } = 1.5f;
	[Property] public float IndicatorHeight { get; set; } = 60f;
	[Property] public float IndicatorParticleSize { get; set; } = 28f;
	[Property] public Color IndicatorColor { get; set; } = new Color( 0.7f, 0.7f, 0.75f, 0.85f );
	[Property] public string SpritePath { get; set; } = "particle_glow.sprite";

	float _originalWalkSpeed;
	float _originalRunSpeed;
	Color _originalBodyTint;
	bool _speedRestored;
	bool _tintRestored;

	SkinnedModelRenderer _bodyRenderer;
	PlayerController _controller;

	GameObject _indicatorRoot;
	List<IndicatorParticle> _indicatorParticles = new();
	float _orbitTime;

	class IndicatorParticle
	{
		public GameObject Go;
		public SpriteRenderer Renderer;
		public float OrbitOffset;
		public float HeightOffset;
		public float PulsePhase;
	}

	public static StoneskinBuff GetActive( GameObject player )
	{
		if ( player == null )
			return null;

		var buff = player.Components.Get<StoneskinBuff>();
		if ( buff != null && buff.TimeRemaining > 0f )
			return buff;

		return null;
	}

	public void Begin( float duration )
	{
		TotalDuration = duration;
		TimeRemaining = duration;

		_controller = Components.Get<PlayerController>();
		if ( _controller != null )
		{
			_originalWalkSpeed = _controller.WalkSpeed;
			_originalRunSpeed = _controller.RunSpeed;
			_controller.WalkSpeed = _originalWalkSpeed * SpeedMultiplier;
			_controller.RunSpeed = _originalRunSpeed * SpeedMultiplier;
		}

		var caster = Components.Get<SpellCaster>();
		_bodyRenderer = caster != null ? caster.BodyRenderer : Components.GetInChildren<SkinnedModelRenderer>();

		if ( _bodyRenderer != null )
		{
			_originalBodyTint = _bodyRenderer.Tint;
			_bodyRenderer.Tint = StoneTint;
		}

		_speedRestored = false;
		_tintRestored = false;

		BuildIndicator();
	}

	void BuildIndicator()
	{
		Sprite spriteAsset = null;
		try { spriteAsset = ResourceLibrary.Get<Sprite>( SpritePath ); }
		catch ( System.Exception ) { spriteAsset = null; }

		_indicatorRoot = new GameObject( true, "StoneskinIndicator" );
		_indicatorRoot.SetParent( GameObject );
		_indicatorRoot.LocalPosition = Vector3.Zero;

		for ( int i = 0; i < IndicatorParticleCount; i++ )
		{
			var go = new GameObject( true, $"StoneskinParticle{i}" );
			go.SetParent( _indicatorRoot );

			var sr = go.Components.Create<SpriteRenderer>();
			if ( spriteAsset != null )
				sr.Sprite = spriteAsset;
			sr.Color = IndicatorColor;
			sr.Size = new Vector2( IndicatorParticleSize, IndicatorParticleSize );

			_indicatorParticles.Add( new IndicatorParticle
			{
				Go = go,
				Renderer = sr,
				OrbitOffset = ( (float)i / IndicatorParticleCount ) * MathF.PI * 2f,
				HeightOffset = Game.Random.Float( 0f, IndicatorHeight ),
				PulsePhase = Game.Random.Float( 0f, MathF.PI * 2f )
			} );
		}
	}

	void UpdateIndicator()
	{
		_orbitTime += Time.Delta;

		float fadeIn = MathF.Min( 1f, TimeRemaining / 0.5f );
		float globalFade = TimeRemaining <= 0.5f ? fadeIn : 1f;

		foreach ( var p in _indicatorParticles )
		{
			if ( p.Go == null || !p.Go.IsValid() ) continue;

			float angle = p.OrbitOffset + _orbitTime * IndicatorOrbitSpeed;
			float bob = MathF.Sin( _orbitTime * 2f + p.PulsePhase ) * 8f;

			p.Go.LocalPosition = new Vector3(
				MathF.Cos( angle ) * IndicatorOrbitRadius,
				MathF.Sin( angle ) * IndicatorOrbitRadius,
				p.HeightOffset + bob
			);

			float pulse = 1f + 0.2f * MathF.Sin( _orbitTime * 3f + p.PulsePhase );
			float size = IndicatorParticleSize * pulse;
			p.Renderer.Size = new Vector2( size, size );

			var c = IndicatorColor;
			c.a = IndicatorColor.a * globalFade;
			p.Renderer.Color = c;
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( TimeRemaining <= 0f )
			return;

		TimeRemaining -= Time.Delta;

		UpdateIndicator();

		if ( _bodyRenderer != null && TimeRemaining <= FadeOutDuration && TimeRemaining > 0f )
		{
			float t = TimeRemaining / FadeOutDuration;
			_bodyRenderer.Tint = Color.Lerp( _originalBodyTint, StoneTint, t );
		}

		if ( TimeRemaining <= 0f )
		{
			TimeRemaining = 0f;
			End();
		}
	}

	void End()
	{
		if ( !_speedRestored && _controller != null )
		{
			_controller.WalkSpeed = _originalWalkSpeed;
			_controller.RunSpeed = _originalRunSpeed;
			_speedRestored = true;
		}

		if ( !_tintRestored && _bodyRenderer != null )
		{
			_bodyRenderer.Tint = _originalBodyTint;
			_tintRestored = true;
		}

		if ( _indicatorRoot != null && _indicatorRoot.IsValid() )
		{
			_indicatorRoot.Destroy();
			_indicatorRoot = null;
		}

		GameLog.Add( "Stoneskin fades away.", "#a0a0a8" );

		GameObject.Components.Get<StoneskinBuff>()?.Destroy();
	}

	protected override void OnDestroy()
	{
		if ( !_speedRestored && _controller != null )
		{
			_controller.WalkSpeed = _originalWalkSpeed;
			_controller.RunSpeed = _originalRunSpeed;
		}

		if ( !_tintRestored && _bodyRenderer != null )
		{
			_bodyRenderer.Tint = _originalBodyTint;
		}

		if ( _indicatorRoot != null && _indicatorRoot.IsValid() )
			_indicatorRoot.Destroy();
	}
}