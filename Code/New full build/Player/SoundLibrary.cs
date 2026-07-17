using Sandbox;
using System.Collections.Generic;

public sealed class SoundLibrary : Component
{
	const string CHOP = "Sounds/chop.sound";
	const string ORE_HIT = "Sounds/HitResourceOre.sound";
	const string MONSTER_HIT = "Sounds/HitMonsterSound.sound";
	const string PVP_HIT = "Sounds/PvpHit.sound";
	const string COUNTDOWN = "Sounds/321fight.sound";
	const string SLIME_SQUISH = "Sounds/slimesquish.sound";
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
	const string LIGHTNING_BOLT = "Sounds/LightningBolt.sound";
	const string EQUIP = "Sounds/Equip.sound";
	const string CANT_USE = "Sounds/CantUse.sound";
	const string SWORD_BOSS = "Sounds/SwordBoss.sound";
	const string BOSS_ROAR = "Sounds/BossRoar.sound";
	const string BOSS_KICK = "Sounds/BossKick.sound";
	const string BOSS_DEATH_GRASP = "Sounds/BossDeathGrasp.sound";
	const string BOSS_DEATH_KNEES = "Sounds/BossDeathKnees.sound";
	const string BOSS_DEATH_FALL = "Sounds/BossDeathFall.sound";
	const string TELEPORT = "Sounds/TeleportSound.sound";
	const string SMALL_MONSTER_ATTACK = "Sounds/SmallMonsterAttack.sound";
	const string LARGE_MONSTER_ATTACK = "Sounds/LargeMonsterAttack.sound";
	const string CARD_SHUFFLE = "Sounds/CardShuffle.sound";
	const string CARD_DEALT = "Sounds/CardDealt.sound";
	const string ACID_SPIT_IMPACT = "Sounds/AcidSpitImpact.sound";
	const string ICE_SHARD_IMPACT = "Sounds/IceShardImpact.sound";
	const string ARROW_IMPACT = "Sounds/ArrowImpact.sound";
	const string MAGIC_MISSILE = "Sounds/MagicMissile.sound";
	const string SINGULARITY = "Sounds/Singularity.sound";
	const string LEVEL_UP = "Sounds/LevelUp.sound";
	const string SEND_TO_BANK = "Sounds/SendToBank.sound";
	const string SLIME_KING_LAND = "Sounds/SlimeKingLand.sound";
	const string SLIME_KING_SPIKE_BURST = "Sounds/SlimeKingSpikeBurst.sound";

	const float FireballVolume = 0.5f;

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

	static readonly Dictionary<string, float> _oneShotLastPlayed = new();
	const float OneShotThrottle = 0.1f;

	static bool ThrottledRecently( string soundPath )
	{
		if ( _oneShotLastPlayed.TryGetValue( soundPath, out float last ) && Time.Now - last < OneShotThrottle )
			return true;

		_oneShotLastPlayed[soundPath] = Time.Now;
		return false;
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

	static SoundHandle PlayLockedReturning( string soundPath )
	{
		var handle = Sound.Play( soundPath, GetLocalListenerPosition() );
		if ( handle.IsValid() )
			_listenerLockedSounds.Add( handle );
		return handle;
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
		if ( GetInstance() == null )
			return;

		PlayLocked( soundPath );
		BroadcastWorldSoundForOthers( soundPath, position );
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

	public static void PlayPvpHit( Vector3 position )
	{
		PlayPlayerActionSound( PVP_HIT, position );
	}

	public static void PlayPvpHitLocal( Vector3 position )
	{
		Sound.Play( PVP_HIT, position );
	}

	public static void PlayCountdown()
	{
		Sound.Play( COUNTDOWN, GetLocalListenerPosition() );
	}

	public static void PlaySlimeSquish( Vector3 position )
	{
		Sound.Play( SLIME_SQUISH, position );
	}

	public static void PlaySlimeKingLand( Vector3 position, int generation )
	{
		var handle = Sound.Play( SLIME_KING_LAND, position );
		if ( handle.IsValid() )
			handle.Volume = generation == 0 ? 1f : generation == 1 ? 0.7f : 0.45f;
	}

	public static void PlaySlimeKingSpikeBurst( Vector3 position )
	{
		Sound.Play( SLIME_KING_SPIKE_BURST, position );
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
		if ( GetInstance() == null )
			return;

		var handle = PlayLockedReturning( FIREBALL );
		if ( handle.IsValid() )
			handle.Volume = FireballVolume;

		BroadcastFireballForOthers( position );
	}

	public static void PlayIceShard( Vector3 position )
	{
		PlayPlayerActionSound( ICE_SHARD, position );
	}

	public static void PlayDarkBlast( Vector3 position )
	{
		PlayPlayerActionSound( DARK_BLAST, position );
	}

	public static SoundHandle PlayLightningBoltLoop( Vector3 position )
	{
		if ( GetInstance() == null )
			return default;

		var handle = PlayLockedReturning( LIGHTNING_BOLT );
		BroadcastWorldSoundForOthers( LIGHTNING_BOLT, position );
		return handle;
	}

	public static void StopLightningBoltLoop( SoundHandle handle, float fadeOut = 0.15f )
	{
		if ( handle.IsValid() )
			handle.Stop( fadeOut );
	}

	public static void PlayTeleport( Vector3 position )
	{
		PlayPlayerActionSound( TELEPORT, position );
	}

	public static void PlayMonsterDeath( Vector3 position )
	{
		BroadcastWorldSoundForAll( MONSTER_DEATH, position );
	}

	public static void PlaySwordBoss( Vector3 position )
	{
		BroadcastWorldSoundForAll( SWORD_BOSS, position );
	}

	public static void PlayBossRoar( Vector3 position )
	{
		BroadcastWorldSoundForAll( BOSS_ROAR, position );
	}

	public static void PlayBossKick( Vector3 position )
	{
		BroadcastWorldSoundForAll( BOSS_KICK, position );
	}

	public static void PlayBossDeathGrasp( Vector3 position )
	{
		BroadcastWorldSoundForAll( BOSS_DEATH_GRASP, position );
	}

	public static void PlayBossDeathKnees( Vector3 position )
	{
		BroadcastWorldSoundForAll( BOSS_DEATH_KNEES, position );
	}

	public static void PlayBossDeathFall( Vector3 position )
	{
		BroadcastWorldSoundForAll( BOSS_DEATH_FALL, position );
	}

	public static void PlaySmallMonsterAttack( Vector3 position )
	{
		BroadcastWorldSoundForAll( SMALL_MONSTER_ATTACK, position );
	}

	public static void PlayLargeMonsterAttack( Vector3 position )
	{
		BroadcastWorldSoundForAll( LARGE_MONSTER_ATTACK, position );
	}

	public static void PlayCardShuffle( Vector3 position )
	{
		BroadcastWorldSoundForAll( CARD_SHUFFLE, position );
	}

	public static void PlayCardDealt( Vector3 position )
	{
		BroadcastWorldSoundForAll( CARD_DEALT, position );
	}

	public static void PlayAcidSpitImpact( Vector3 position )
	{
		BroadcastWorldSoundForAll( ACID_SPIT_IMPACT, position );
	}

	public static void PlayIceShardImpact( Vector3 position )
	{
		BroadcastWorldSoundForAll( ICE_SHARD_IMPACT, position );
	}

	public static void PlayArrowImpact( Vector3 position )
	{
		BroadcastWorldSoundForAll( ARROW_IMPACT, position );
	}

	public static void PlayMagicMissile( Vector3 position )
	{
		PlayPlayerActionSound( MAGIC_MISSILE, position );
	}

	public static void PlaySingularity( Vector3 position )
	{
		BroadcastWorldSoundForAll( SINGULARITY, position );
	}

	public static void PlayHitNothing()
	{
		Sound.Play( HIT_NOTHING, GetLocalListenerPosition() );
	}

	public static void PlayReceiveItem()
	{
		if ( ThrottledRecently( RECEIVE_ITEM ) )
			return;

		Sound.Play( RECEIVE_ITEM, GetLocalListenerPosition() );
	}

	public static void PlayLevelUp()
	{
		if ( ThrottledRecently( LEVEL_UP ) )
			return;

		PlayLocked( LEVEL_UP );
	}

	public static void PlaySendToBank()
	{
		if ( ThrottledRecently( SEND_TO_BANK ) )
			return;

		PlayLocked( SEND_TO_BANK );
	}

	public static void PlaySellBuy()
	{
		if ( ThrottledRecently( SELL_BUY ) )
			return;

		Sound.Play( SELL_BUY, GetLocalListenerPosition() );
	}

	public static void PlayWorkbenchCraft()
	{
		if ( ThrottledRecently( WORKBENCH_CRAFT ) )
			return;

		Sound.Play( WORKBENCH_CRAFT, GetLocalListenerPosition() );
	}

	public static void PlayAnvilCraft()
	{
		if ( ThrottledRecently( ANVIL_CRAFT ) )
			return;

		Sound.Play( ANVIL_CRAFT, GetLocalListenerPosition() );
	}

	public static void PlayUseFurnace()
	{
		if ( ThrottledRecently( USE_FURNACE ) )
			return;

		Sound.Play( USE_FURNACE, GetLocalListenerPosition() );
	}

	public static void PlayEquip()
	{
		Sound.Play( EQUIP, GetLocalListenerPosition() );
	}

	public static void PlayCantUse()
	{
		Sound.Play( CANT_USE, GetLocalListenerPosition() );
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
	static void BroadcastWorldSoundForOthers( string soundPath, Vector3 position )
	{
		if ( Rpc.Caller != null && Connection.Local != null && Rpc.Caller.Id == Connection.Local.Id )
			return;

		Sound.Play( soundPath, position );
	}

	[Rpc.Broadcast]
	static void BroadcastFireballForOthers( Vector3 position )
	{
		if ( Rpc.Caller != null && Connection.Local != null && Rpc.Caller.Id == Connection.Local.Id )
			return;

		var handle = Sound.Play( FIREBALL, position );
		if ( handle.IsValid() )
			handle.Volume = FireballVolume;
	}

	[Rpc.Broadcast]
	static void BroadcastWorldSoundForAll( string soundPath, Vector3 position )
	{
		Sound.Play( soundPath, position );
	}
}