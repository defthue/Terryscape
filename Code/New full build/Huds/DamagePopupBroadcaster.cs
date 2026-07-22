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
		public ulong AttackerSteamId;
		public ulong TargetSteamId;
	}

	static DamagePopupBroadcaster _instance;

	public List<ActivePopup> ActivePopups { get; } = new();

	[Property] public float MaxVisibleDistance { get; set; } = 3000f;
	[Property] public float PopupLifetime { get; set; } = 1.2f;

	public const int TierPoison = 4;

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

	public static ulong SteamIdOf( GameObject obj )
	{
		return obj?.Network?.Owner?.SteamId ?? 0ul;
	}

	public static void Broadcast( Vector3 worldPos, int damage, int targetMaxHealth, bool isCrit, ulong attackerSteamId, ulong targetSteamId )
	{
		int tier = ComputeColorTier( damage, targetMaxHealth );

		var gm = GameManager.Instance;
		if ( gm != null )
			gm.BroadcastDamagePopup( worldPos, damage, tier, isCrit, attackerSteamId, targetSteamId );
		else
			AddLocal( worldPos, damage, tier, isCrit, attackerSteamId, targetSteamId );
	}

	public static void BroadcastPoison( Vector3 worldPos, int damage, ulong attackerSteamId, ulong targetSteamId )
	{
		var gm = GameManager.Instance;
		if ( gm != null )
			gm.BroadcastDamagePopup( worldPos, damage, TierPoison, false, attackerSteamId, targetSteamId );
		else
			AddLocal( worldPos, damage, TierPoison, false, attackerSteamId, targetSteamId );
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

	public static void AddLocal( Vector3 worldPos, int damage, int tier, bool isCrit, ulong attackerSteamId, ulong targetSteamId )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.AddPopup( worldPos, damage, tier, isCrit, attackerSteamId, targetSteamId );
	}

	void AddPopup( Vector3 worldPos, int damage, int tier, bool isCrit, ulong attackerSteamId, ulong targetSteamId )
	{
		ActivePopups.Add( new ActivePopup
		{
			WorldPosition = worldPos + Vector3.Up * 40f,
			Damage = damage,
			ColorTier = tier,
			IsCrit = isCrit,
			SpawnTime = Time.Now,
			AttackerSteamId = attackerSteamId,
			TargetSteamId = targetSteamId
		} );
	}

	public static void ShowLocal( Vector3 worldPos, int damage, int targetMaxHealth, bool isCrit, ulong attackerSteamId, ulong targetSteamId )
	{
		int tier = ComputeColorTier( damage, targetMaxHealth );
		AddLocal( worldPos, damage, tier, isCrit, attackerSteamId, targetSteamId );
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