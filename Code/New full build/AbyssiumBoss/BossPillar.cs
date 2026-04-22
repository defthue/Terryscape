using Sandbox;
using System;

public sealed class BossPillar : Component
{
	[Property, Group( "Identity" )] public string PillarName { get; set; } = "Healing Pillar";

	[Property, Group( "Stats" )] public int MaxHealth { get; set; } = 200;
	[Property, Group( "Stats" )] public float RespawnDelay { get; set; } = 180f;

	[Property, Group( "References" )] public ModelRenderer ModelRenderer { get; set; }
	[Property, Group( "References" )] public Collider PillarCollider { get; set; }

	[Property, Group( "Loot" )] public ItemId LootItem { get; set; } = ItemId.None;
	[Property, Group( "Loot" )] public int LootAmount { get; set; } = 1;
	[Property, Group( "Loot" )] public float LootChance { get; set; } = 100f;

	[Sync] public int CurrentHealth { get; set; }
	[Sync] public bool IsDead { get; set; }
	[Sync] public GameObject FirstAttacker { get; set; }

	float _respawnTimer;

	public bool IsAlive => !IsDead;

	protected override void OnStart()
	{
		CurrentHealth = MaxHealth;
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost )
			return;

		if ( !IsDead )
			return;

		_respawnTimer -= Time.Delta;
		if ( _respawnTimer <= 0f )
			Respawn();
	}

	[Rpc.Host]
	public void TakeDamage( int damage, GameObject attacker )
	{
		if ( IsDead )
			return;

		if ( FirstAttacker == null && attacker != null )
			FirstAttacker = attacker;

		CurrentHealth -= damage;

		if ( CurrentHealth <= 0 )
		{
			CurrentHealth = 0;
			Die();
		}
	}

	void Die()
	{
		IsDead = true;
		_respawnTimer = RespawnDelay;
		AwardLoot();
		BroadcastDeath();
	}

	void Respawn()
	{
		IsDead = false;
		CurrentHealth = MaxHealth;
		FirstAttacker = null;
		BroadcastRespawn();
	}

	void AwardLoot()
	{
		if ( FirstAttacker == null || !FirstAttacker.IsValid() )
			return;

		if ( LootItem == ItemId.None || LootChance <= 0f )
			return;

		var inventory = FirstAttacker.Components.Get<Inventory>();
		if ( inventory == null )
			return;

		var rng = new Random();
		if ( (float)( rng.NextDouble() * 100.0 ) < LootChance )
			inventory.AddItem( LootItem, LootAmount );
	}

	[Rpc.Broadcast]
	void BroadcastDeath()
	{
		if ( PillarCollider != null )
			PillarCollider.Enabled = false;

		if ( ModelRenderer != null )
			ModelRenderer.Enabled = false;
	}

	[Rpc.Broadcast]
	void BroadcastRespawn()
	{
		if ( PillarCollider != null )
			PillarCollider.Enabled = true;

		if ( ModelRenderer != null )
			ModelRenderer.Enabled = true;
	}
}
