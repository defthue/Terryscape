using Sandbox;

public sealed class SoundLibrary : Component
{
	const string CHOP = "Sounds/chop.sound";
	const string ORE_HIT = "Sounds/HitResourceOre.sound";
	const string MONSTER_HIT = "Sounds/HitMonsterSound.sound";

	static SoundLibrary _instance;

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

	[Rpc.Broadcast]
	void BroadcastWorldSound( string soundPath, Vector3 position )
	{
		Sound.Play( soundPath, position );
	}
}
