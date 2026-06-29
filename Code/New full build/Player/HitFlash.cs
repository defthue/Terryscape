using Sandbox;
using System.Collections.Generic;

public sealed class HitFlash : Component
{
	[Property] public float Duration { get; set; } = 0.1f;
	[Property] public Color FlashColor { get; set; } = new Color( 1f, 0.3f, 0.3f );

	float _timer;
	bool _active;
	Dictionary<ModelRenderer, Color> _modelTints = new();
	Dictionary<SkinnedModelRenderer, Color> _skinnedTints = new();

	public static void Trigger( GameObject target )
	{
		if ( target == null || !target.IsValid() )
			return;

		var flash = target.Components.Get<HitFlash>();
		if ( flash == null )
			flash = target.Components.Create<HitFlash>();

		flash.Begin();
	}

	void Begin()
	{
		if ( _active )
			Restore();

		_modelTints.Clear();
		_skinnedTints.Clear();

		foreach ( var r in GameObject.Components.GetAll<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants ) )
		{
			if ( r == null || !r.Enabled )
				continue;
			_skinnedTints[r] = r.Tint;
			r.Tint = FlashColor;
		}

		foreach ( var r in GameObject.Components.GetAll<ModelRenderer>( FindMode.EnabledInSelfAndDescendants ) )
		{
			if ( r == null || !r.Enabled )
				continue;
			if ( r is SkinnedModelRenderer )
				continue;
			_modelTints[r] = r.Tint;
			r.Tint = FlashColor;
		}

		_active = true;
		_timer = Duration;
	}

	void Restore()
	{
		foreach ( var kv in _skinnedTints )
		{
			if ( kv.Key != null && kv.Key.IsValid() )
				kv.Key.Tint = kv.Value;
		}
		foreach ( var kv in _modelTints )
		{
			if ( kv.Key != null && kv.Key.IsValid() )
				kv.Key.Tint = kv.Value;
		}
		_skinnedTints.Clear();
		_modelTints.Clear();
		_active = false;
	}

	protected override void OnUpdate()
	{
		if ( !_active )
			return;

		_timer -= Time.Delta;
		if ( _timer <= 0f )
			Restore();
	}

	protected override void OnDisabled()
	{
		if ( _active )
			Restore();
	}
}
