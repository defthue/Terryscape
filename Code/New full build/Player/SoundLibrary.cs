using Sandbox;
using System.Collections.Generic;

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
	const string SMALL_MONSTER_ATTACK = "Sounds/SmallMonsterAttack.sound";
	const string LARGE_MONSTER_ATTACK = "Sounds/LargeMonsterAttack.sound";
	const string CARD_SHUFFLE = "Sounds/CardShuffle.sound";
	const string CARD_DEALT = "Sounds/CardDealt.sound";

	static SoundLibrary _instance;
	static SoundHandle _furnaceLoopHandle;
	static List<SoundHandle> _listenerLockedSounds = new();

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

	static void PlayLocked( string soundPath )
	{
		var handle = Sound.Play( soundPath, GetLocalListenerPosition() );
		if ( handle.IsValid() )
			_listenerLockedSounds.Add( handle );
	}

	protected override void OnUpdate()
	{
		if ( _listenerLockedSounds.Count == 0 )
			return;

		var listenerPos = GetLocalListenerPosition();

		for ( int i = _listenerLockedSounds.Count - 1; i >= 0; i-- )
		{
			var handle = _listenerLockedSounds[i];
			if ( !handle.IsValid() || handle.IsStopped )
			{
				_listenerLockedSounds.RemoveAt( i );
				continue;
			}

			handle.Position = listenerPos;
		}
	}

	static void PlayPlayerActionSound( string soundPath, Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		PlayLocked( soundPath );
		instance.BroadcastWorldSoundForOthers( soundPath, position );
	}

	public static void PlayChop( Vector3 position )
	{
		PlayPlayerActionSound( CHOP, position );
	}

	public static void PlayOreHit( Vector3 position )
	{
		PlayPlayerActionSound( ORE_HIT, position );
	}

	public static void PlayMonsterHit( Vector3 position )
	{
		PlayPlayerActionSound( MONSTER_HIT, position );
	}

	public static void PlayForage( Vector3 position )
	{
		PlayPlayerActionSound( FORAGE, position );
	}

	public static void PlayBowPull( Vector3 position )
	{
		PlayPlayerActionSound( BOW_PULL, position );
	}

	public static void PlayBowRelease( Vector3 position )
	{
		PlayPlayerActionSound( BOW_RELEASE, position );
	}

	public static void PlayFireball( Vector3 position )
	{
		PlayPlayerActionSound( FIREBALL, position );
	}

	public static void PlayIceShard( Vector3 position )
	{
		PlayPlayerActionSound( ICE_SHARD, position );
	}

	public static void PlayDarkBlast( Vector3 position )
	{
		PlayPlayerActionSound( DARK_BLAST, position );
	}

	public static void PlayTeleport( Vector3 position )
	{
		PlayPlayerActionSound( TELEPORT, position );
	}

	public static void PlayMonsterDeath( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSoundForAll( MONSTER_DEATH, position );
	}

	public static void PlaySmallMonsterAttack( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSoundForAll( SMALL_MONSTER_ATTACK, position );
	}

	public static void PlayLargeMonsterAttack( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSoundForAll( LARGE_MONSTER_ATTACK, position );
	}

	public static void PlayCardShuffle( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSoundForAll( CARD_SHUFFLE, position );
	}

	public static void PlayCardDealt( Vector3 position )
	{
		var instance = GetInstance();
		if ( instance == null )
			return;

		instance.BroadcastWorldSoundForAll( CARD_DEALT, position );
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
	void BroadcastWorldSoundForOthers( string soundPath, Vector3 position )
	{
		if ( Rpc.Caller != null && Connection.Local != null && Rpc.Caller.Id == Connection.Local.Id )
			return;

		Sound.Play( soundPath, position );
	}

	[Rpc.Broadcast]
	void BroadcastWorldSoundForAll( string soundPath, Vector3 position )
	{
		Sound.Play( soundPath, position );
	}
}