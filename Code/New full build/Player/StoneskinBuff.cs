using Sandbox;
using System;
using System.Collections.Generic;

public sealed class StoneskinBuff : Component
{
	public float TimeRemaining { get; private set; }
	public float TotalDuration { get; private set; }

	[Property] public float SpeedMultiplier { get; set; } = 0.5f;
	[Property] public float DamageMultiplier { get; set; } = 0.5f;
	[Property] public Color StoneTint { get; set; } = new Color( 0.55f, 0.55f, 0.6f, 1f );
	[Property] public float FadeOutDuration { get; set; } = 0.5f;

	[Property] public int IndicatorParticleCount { get; set; } = 5;
	[Property] public float IndicatorOrbitRadius { get; set; } = 35f;
	[Property] public float IndicatorOrbitSpeed { get; set; } = 1.5f;
	[Property] public float IndicatorHeight { get; set; } = 60f;
	[Property] public float IndicatorParticleSize { get; set; } = 11f;
	[Property] public Color IndicatorColor { get; set; } = new Color( 0.7f, 0.7f, 0.75f, 0.85f );

	public bool VisualOnly { get; set; }

	public bool IsExpired => TimeRemaining <= 0f;
	public float EffectiveSpeedMultiplier => TimeRemaining > 0f ? SpeedMultiplier : 1f;

	Color _originalBodyTint;
	bool _tintRestored;
	bool _tintCaptured;

	SkinnedModelRenderer _bodyRenderer;

	List<IndicatorParticle> _indicatorParticles = new();
	float _orbitTime;

	class IndicatorParticle
	{
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

		var caster = Components.Get<SpellCaster>();
		_bodyRenderer = caster != null ? caster.BodyRenderer : Components.GetInChildren<SkinnedModelRenderer>();

		if ( _bodyRenderer != null && !_tintCaptured )
		{
			_originalBodyTint = _bodyRenderer.Tint;
			_tintCaptured = true;
		}

		if ( _bodyRenderer != null )
			_bodyRenderer.Tint = StoneTint;

		_tintRestored = false;

		if ( _indicatorParticles.Count == 0 )
			BuildIndicator();
	}

	void BuildIndicator()
	{
		for ( int i = 0; i < IndicatorParticleCount; i++ )
		{
			_indicatorParticles.Add( new IndicatorParticle
			{
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
			float angle = p.OrbitOffset + _orbitTime * IndicatorOrbitSpeed;
			float bob = MathF.Sin( _orbitTime * 2f + p.PulsePhase ) * 8f;

			Vector3 worldPos = WorldPosition + new Vector3(
				MathF.Cos( angle ) * IndicatorOrbitRadius,
				MathF.Sin( angle ) * IndicatorOrbitRadius,
				p.HeightOffset + bob
			);

			float pulse = 1f + 0.2f * MathF.Sin( _orbitTime * 3f + p.PulsePhase );
			float size = IndicatorParticleSize * pulse;

			var c = IndicatorColor;
			c.a = IndicatorColor.a * globalFade;

			SpellGizmo.SoftSphere( worldPos, size, c );
		}
	}

	protected override void OnUpdate()
	{
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
		RestoreTint();
		DestroyIndicator();

		if ( !VisualOnly )
			GameLog.Add( "Stoneskin fades away.", "#a0a0a8" );

		GameObject.Components.Get<StoneskinBuff>()?.Destroy();
	}

	void RestoreTint()
	{
		if ( _tintRestored )
			return;

		if ( _bodyRenderer != null && _tintCaptured )
			_bodyRenderer.Tint = _originalBodyTint;

		_tintRestored = true;
	}

	void DestroyIndicator()
	{
		_indicatorParticles.Clear();
	}

	protected override void OnDestroy()
	{
		RestoreTint();
		DestroyIndicator();
	}
}