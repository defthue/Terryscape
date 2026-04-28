using Sandbox;

public sealed class SoundLibrary : Component
{
	const string CHOP = "Sounds/chop.sound";
	const string ORE_HIT = "Sounds/HitResourceOre.sound";
	const string MONSTER_HIT = "Sounds/HitMonsterSound.sound";
	const string HIT_NOTHING = "Sounds/HitNothing.sound";
	const string FORAGE = "Sounds/Forage.sound";
	const string MONSTER_DEATH = "Sounds/MonsterDeath.sound";
	const string RECEIVE_ITEM = "Sounds/ReceiveItem.sound";
	const string SELL_BUY = "Sounds/SellBuy.sound";
	const string WORKBENCH_CRAFT = "Sounds/WorkbenchCraft.sound";
	const string ANVIL_CRAFT = "Sounds/AnvilCraft.sound";
	const string USE_FURNACE = "Sounds/UseFurnace.sound";
	const string FURNACE_BACKGROUND = "Sounds/FurnaceBackground.sound";
	const string BOW_PULL = "Sounds/BowPull.sound";
	const string BOW_RELEASE = "Sounds/BowRelease.sound";
	const string FIREBALL = "Sounds/Fireball.sound";
	const string ICE_SHARD = "Sounds/IceShard.sound";
	const string DARK_BLAST = "Sounds/DarkBlast.sound";
	const string EQUIP = "Sounds/Equip.sound";
	const string TELEPORT = "Sounds/TeleportSound.sound";

	static SoundLibrary _instance;
	static SoundHandle _furnaceLoopHandle;

	static SoundLibrary GetInstance()
	{
		if ( _instance.IsValid() )
			return _instance;

		var scene = Game.ActiveScene;
		if ( scene == null )
			return null;

		_instance = scene.GetAllComponents<SoundLibrary>().FirstOrDefault();
		if ( _instance.IsValid() )
			return _instance;

		var go = scene.CreateObject();
		go.Name = "SoundLibrary";
		_instance = go.Components.Create<SoundLibrary>();
		return _instance;
	}

	static Vector3 GetLocalListenerPosition()
	{
		var scene = Game.ActiveScene;
		if ( scene == null )
			return Vector3.Zero;

		var camera = scene.Camera;
		if ( camera != null )
			return camera.WorldPosition;

		return Vector3.Zero;
	}

	public static void PlayChop( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSound( CHOP, position );
	}

	public static void PlayOreHit( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSound( ORE_HIT, position );
	}

	public static void PlayMonsterHit( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSound( MONSTER_HIT, position );
	}

	public static void PlayForage( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSound( FORAGE, position );
	}

	public static void PlayMonsterDeath( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSound( MONSTER_DEATH, position );
	}

	public static void PlayBowPull( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSound( BOW_PULL, position );
	}

	public static void PlayBowRelease( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSound( BOW_RELEASE, position );
	}

	public static void PlayFireball( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSound( FIREBALL, position );
	}

	public static void PlayIceShard( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSound( ICE_SHARD, position );
	}

	public static void PlayDarkBlast( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSound( DARK_BLAST, position );
	}

	public static void PlayTeleport( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSound( TELEPORT, position );
	}

	public static void PlayHitNothing()
	{
		Sound.Play( HIT_NOTHING, GetLocalListenerPosition() );
	}

	public static void PlayReceiveItem()
	{
		Sound.Play( RECEIVE_ITEM, GetLocalListenerPosition() );
	}

	public static void PlaySellBuy()
	{
		Sound.Play( SELL_BUY, GetLocalListenerPosition() );
	}

	public static void PlayWorkbenchCraft()
	{
		Sound.Play( WORKBENCH_CRAFT, GetLocalListenerPosition() );
	}

	public static void PlayAnvilCraft()
	{
		Sound.Play( ANVIL_CRAFT, GetLocalListenerPosition() );
	}

	public static void PlayUseFurnace()
	{
		Sound.Play( USE_FURNACE, GetLocalListenerPosition() );
	}

	public static void PlayEquip()
	{
		Sound.Play( EQUIP, GetLocalListenerPosition() );
	}

	public static void StartFurnaceLoop()
	{
		StopFurnaceLoop();
		_furnaceLoopHandle = Sound.Play( FURNACE_BACKGROUND, GetLocalListenerPosition() );
	}

	public static void StopFurnaceLoop()
	{
		if ( _furnaceLoopHandle.IsValid() )
		{
			_furnaceLoopHandle.Stop();
			_furnaceLoopHandle = null;
		}
	}

	[Rpc.Broadcast]
	void BroadcastWorldSound( string soundPath, Vector3 position )
	{
		Sound.Play( soundPath, position );
	}
}