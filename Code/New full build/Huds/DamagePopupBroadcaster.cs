using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class DamagePopupBroadcaster : Component
{
	public class ActivePopup
	{
		public Vector3 WorldPosition;
		public int Damage;
		public int ColorTier;
		public bool IsCrit;
		public float SpawnTime;
	}

	static DamagePopupBroadcaster _instance;

	public List<ActivePopup> ActivePopups { get; } = new();

	[Property] public float MaxVisibleDistance { get; set; } = 3000f;
	[Property] public float PopupLifetime { get; set; } = 1.2f;

	protected override void OnStart()
	{
		_instance = this;
	}

	protected override void OnDestroy()
	{
		if ( _instance == this )
			_instance = null;
	}

	static DamagePopupBroadcaster GetInstance()
	{
		if ( _instance.IsValid() )
			return _instance;

		var scene = Game.ActiveScene;
		if ( scene == null )
			return null;

		_instance = scene.GetAllComponents<DamagePopupBroadcaster>().FirstOrDefault();
		if ( _instance.IsValid() )
			return _instance;

		var go = scene.CreateObject();
		go.Name = "DamagePopupBroadcaster";
		_instance = go.Components.Create<DamagePopupBroadcaster>();
		return _instance;
	}

	public static void Broadcast( Vector3 worldPos, int damage, int targetMaxHealth, bool isCrit )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		int tier = ComputeColorTier( damage, targetMaxHealth );
		instance.RpcSpawnPopup( worldPos, damage, tier, isCrit );
	}

	static int ComputeColorTier( int damage, int targetMaxHealth )
	{
		if ( targetMaxHealth <= 0 )
			return 1;

		float pct = (float)damage / targetMaxHealth;
		if ( pct < 0.02f ) return 0;
		if ( pct < 0.08f ) return 1;
		if ( pct < 0.20f ) return 2;
		return 3;
	}

	[Rpc.Broadcast]
	void RpcSpawnPopup( Vector3 worldPos, int damage, int tier, bool isCrit )
	{
		ActivePopups.Add( new ActivePopup
		{
			WorldPosition = worldPos + Vector3.Up * 40f,
			Damage = damage,
			ColorTier = tier,
			IsCrit = isCrit,
			SpawnTime = Time.Now
		} );
	}

	protected override void OnUpdate()
	{
		for ( int i = ActivePopups.Count - 1; i >= 0; i-- )
		{
			if ( Time.Now - ActivePopups[i].SpawnTime > PopupLifetime )
				ActivePopups.RemoveAt( i );
		}
	}
}
