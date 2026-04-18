using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class PlayerHealth : Component
{
	[Property] public int BaseHealth { get; set; } = 100;
	[Property] public float RegenDelay { get; set; } = 10f;
	[Property] public float RegenInterval { get; set; } = 3f;
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
					GameLog.Add( $"You regenerated {RegenAmount} HP. ({CurrentHealth}/{MaxHealth} HP)", "#4caf78" );

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
		if ( skills == null )
		{
			MaxHealth = BaseHealth;
			return;
		}

		int attackLevel = skills.GetLevel( SkillType.Attack );
		int archeryLevel = skills.GetLevel( SkillType.Archery );
		int magicLevel = skills.GetLevel( SkillType.Magic );

		MaxHealth = BaseHealth + ( attackLevel - 1 ) + ( archeryLevel - 1 ) + ( magicLevel - 1 );
	}

	public void TakeDamage( int damage )
	{
		ApplyDamage( damage );
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

		if ( CurrentHealth <= 0 )
			Die();
	}

	public void Heal( int amount )
	{
		if ( IsDead )
			return;

		CurrentHealth = Math.Min( CurrentHealth + amount, MaxHealth );
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
		var items = new Dictionary<ItemId, int>( inventory.GetAllItems() );
		int totalLost = 0;

		foreach ( var kv in items )
		{
			var def = ItemDatabase.Get( kv.Key );
			if ( def == null )
				continue;

			if ( def.Type == ItemType.Potion )
			{
				if ( kv.Value > 0 )
				{
					inventory.RemoveItem( kv.Key, kv.Value );
					GameLog.Add( $"Lost {kv.Value}x {def.Name}.", "#c86464" );
					totalLost += kv.Value;
				}
				continue;
			}

			int halfAmount = kv.Value / 2;
			if ( halfAmount > 0 )
			{
				inventory.RemoveItem( kv.Key, halfAmount );
				GameLog.Add( $"Lost {halfAmount}x {def.Name}.", "#c86464" );
				totalLost += halfAmount;
			}
		}

		if ( totalLost > 0 )
			GameLog.Add( $"You lost items on death. Equipment and bank are safe.", "#c86464" );
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
	}
}