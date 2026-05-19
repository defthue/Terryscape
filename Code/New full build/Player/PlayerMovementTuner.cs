using Sandbox;

public sealed class PlayerMovementTuner : Component
{
	[Property] public float WalkSpeedMultiplier { get; set; } = 1.15f;
	[Property] public float RunSpeedMultiplier { get; set; } = 1.15f;

	PlayerController _pc;
	float _baseRunSpeed;
	bool _forcedWalkActive;

	protected override void OnStart()
	{
		_pc = Components.Get<PlayerController>();
		if ( _pc == null )
			return;

		_pc.WalkSpeed *= WalkSpeedMultiplier;
		_pc.RunSpeed *= RunSpeedMultiplier;
		_baseRunSpeed = _pc.RunSpeed;
	}

	protected override void OnUpdate()
	{
		if ( _pc == null )
			return;

		bool shouldForceWalk = ShouldForceWalk();

		if ( shouldForceWalk )
		{
			if ( !_forcedWalkActive )
			{
				_baseRunSpeed = _pc.RunSpeed;
				_forcedWalkActive = true;
			}
			_pc.RunSpeed = _pc.WalkSpeed;
		}
		else if ( _forcedWalkActive )
		{
			_pc.RunSpeed = _baseRunSpeed;
			_forcedWalkActive = false;
		}
		else
		{
			_baseRunSpeed = _pc.RunSpeed;
		}
	}

	bool ShouldForceWalk()
	{
		var shooter = Components.Get<ProjectileShooter>();
		if ( shooter != null && shooter.IsDrawing )
			return true;

		var caster = Components.Get<SpellCaster>();
		if ( caster != null && ( caster.IsCasting || caster.IsChannelling ) )
			return true;

		var potions = Components.Get<PotionSystem>();
		if ( potions != null && potions.IsDrinking )
			return true;

		return false;
	}
}
