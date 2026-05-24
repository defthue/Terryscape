using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class PlayerHealth : Component
{
	[Property] public int BaseHealth { get; set; } = 100;
	[Property] public float RegenDelay { get; set; } = 3f;
	[Property] public float RegenInterval { get; set; } = 0.5f;
	[Property] public int RegenAmount { get; set; } = 2;
	[Property] public GameObject RespawnPoint { get; set; }

	[Sync] public int MaxHealth { get; set; } = 100;
	[Sync] public int CurrentHealth { get; set; }
	[Sync] public bool IsDead { get; set; }

	float _timeSinceLastHit = 0f;
	float _regenTimer = 0f;
	bool _wasHit = false;

	protected override void OnStart()
	{
		UpdateMaxHealth();
		CurrentHealth = MaxHealth;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		UpdateMaxHealth();

		if ( IsDead )
			return;

		if ( _wasHit )
		{
			_timeSinceLastHit += Time.Delta;

			if ( _timeSinceLastHit >= RegenDelay )
			{
				_regenTimer += Time.Delta;

				if ( _regenTimer >= RegenInterval && CurrentHealth < MaxHealth )
				{
					CurrentHealth = Math.Min( CurrentHealth + RegenAmount, MaxHealth );
					_regenTimer = 0f;

					if ( CurrentHealth >= MaxHealth )
					{
						_wasHit = false;
						_timeSinceLastHit = 0f;
					}
				}
			}
		}
	}

	void UpdateMaxHealth()
	{
		var skills = Components.Get<Skills>();
		int baseMax;
		if ( skills == null )
		{
			baseMax = BaseHealth;
		}
		else
		{
			int attackLevel = skills.GetLevel( SkillType.Attack );
			int archeryLevel = skills.GetLevel( SkillType.Archery );
			int magicLevel = skills.GetLevel( SkillType.Magic );

			baseMax = BaseHealth + ( attackLevel - 1 ) + ( archeryLevel - 1 ) + ( magicLevel - 1 );
		}

		var inventory = Components.Get<Inventory>();
		float vitalityBonus = inventory != null ? inventory.GetEnchantmentBonus( EnchantmentType.Vitality ) : 0f;
		MaxHealth = (int)( baseMax * ( 1f + vitalityBonus / 100f ) );
	}

	public void TakeDamage( int damage )
	{
		int reduced = damage;

		var stoneskin = StoneskinBuff.GetActive( GameObject );
		if ( stoneskin != null )
		{
			reduced = (int)( reduced * stoneskin.DamageMultiplier );
			if ( reduced < 1 ) reduced = 1;
		}

		ApplyDamage( reduced );
	}

	[Rpc.Broadcast]
	void ApplyDamage( int damage )
	{
		if ( IsProxy )
			return;

		if ( IsDead )
			return;

		CurrentHealth -= damage;
		CurrentHealth = Math.Max( CurrentHealth, 0 );
		_wasHit = true;
		_timeSinceLastHit = 0f;
		_regenTimer = 0f;

		GameLog.Add( $"You took {damage} damage. ({CurrentHealth}/{MaxHealth} HP left)", "#c86464" );

		if ( CurrentHealth > 0 )
		{
			var skills = Components.Get<Skills>();
			if ( skills != null && damage > 0 )
				skills.AddXp( SkillType.Defence, damage * 4 );
		}

		if ( CurrentHealth <= 0 )
			Die();
	}

	public void Heal( int amount )
	{
		if ( IsDead )
			return;

		CurrentHealth = Math.Min( CurrentHealth + amount, MaxHealth );
	}

	public void RefillToMax()
	{
		UpdateMaxHealth();
		CurrentHealth = MaxHealth;
		_wasHit = false;
		_timeSinceLastHit = 0f;
		_regenTimer = 0f;
	}

	void Die()
	{
		IsDead = true;

		GameLog.Add( "You have died!", "#c86464" );

		var inventory = Components.Get<Inventory>();
		if ( inventory != null )
			ApplyDeathPenalty( inventory );

		Respawn();
	}

	void ApplyDeathPenalty( Inventory inventory )
	{
		int currentGold = inventory.GetItemCount( ItemId.GoldCoin );
		if ( currentGold <= 0 )
			return;

		float lossPercent = Game.Random.Float( 0.10f, 0.20f );
		int goldLost = (int)( currentGold * lossPercent );
		if ( goldLost <= 0 )
			goldLost = 1;
		if ( goldLost > currentGold )
			goldLost = currentGold;

		inventory.RemoveItem( ItemId.GoldCoin, goldLost );
		GameLog.Add( $"You lost {goldLost} gold on death. ({(int)(lossPercent * 100f)}%)", "#c86464" );
	}

	void Respawn()
	{
		CurrentHealth = MaxHealth;
		IsDead = false;
		_wasHit = false;
		_timeSinceLastHit = 0f;
		_regenTimer = 0f;

		var gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		if ( gm != null )
			GameObject.WorldPosition = gm.SpawnPoint;
		else if ( RespawnPoint != null )
			GameObject.WorldPosition = RespawnPoint.WorldPosition;

		GameLog.Add( $"You respawned with full health. ({CurrentHealth}/{MaxHealth} HP)", "#6db8f0" );

		PlayerPersistence.Local?.SaveNow( SaveSection.Inventory | SaveSection.Stats );
	}
}