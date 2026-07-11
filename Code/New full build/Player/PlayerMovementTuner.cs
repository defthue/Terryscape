using Sandbox;

public sealed class PlayerMovementTuner : Component
{
	[Property] public float WalkSpeedMultiplier { get; set; } = 1.15f;
	[Property] public float RunSpeedMultiplier { get; set; } = 1.15f;

	PlayerController _pc;
	float _baseWalkSpeed;
	float _baseRunSpeed;
	float _rawWalkSpeed;
	float _rawRunSpeed;
	bool _initialized;

	protected override void OnStart()
	{
		_pc = Components.Get<PlayerController>();
		if ( _pc == null )
			return;

		_rawWalkSpeed = _pc.WalkSpeed;
		_rawRunSpeed = _pc.RunSpeed;
		_baseWalkSpeed = _pc.WalkSpeed * WalkSpeedMultiplier;
		_baseRunSpeed = _pc.RunSpeed * RunSpeedMultiplier;
		_initialized = true;
	}

	protected override void OnUpdate()
	{
		if ( !_initialized || _pc == null )
			return;

		float speedMult = GetSpeedMultiplier();
		bool forceWalk = ShouldForceWalk();

		bool dueling = IsActiveDuelist();
		float baseWalk = dueling ? _rawWalkSpeed : _baseWalkSpeed;
		float baseRun = dueling ? _rawRunSpeed : _baseRunSpeed;

		float walk = baseWalk * speedMult;
		float run = forceWalk ? walk : baseRun * speedMult;

		_pc.WalkSpeed = walk;
		_pc.RunSpeed = run;
	}

	bool IsActiveDuelist()
	{
		var dm = DuelManager.Instance;
		return dm != null && dm.MatchActive && dm.IsDuelist( GameObject );
	}

	float GetSpeedMultiplier()
	{
		float mult = 1f;

		var stoneskin = Components.Get<StoneskinBuff>();
		if ( stoneskin != null )
			mult *= stoneskin.EffectiveSpeedMultiplier;

		return mult;
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
