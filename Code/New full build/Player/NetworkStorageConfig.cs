using Sandbox;

public static class NetworkStorageConfig
{
	public const string ProjectId = "f36d466e23024ac0";
	public const string PublicKey = "sbox_ns_c19855d21fd74b6db7f21401d8c665b4";

	static bool _initialized;

	public static void EnsureInitialized()
	{
		if ( _initialized )
			return;

		if ( NetworkStorage.IsConfigured )
		{
			_initialized = true;
			return;
		}

		NetworkStorage.Configure( ProjectId, PublicKey );
		_initialized = true;

		Log.Info( "NetworkStorage configured for project Terry's Quest." );
	}
}