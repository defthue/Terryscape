using Sandbox;

public static class NetworkStorageConfig
{
	static bool _initialized;

	public static void EnsureInitialized()
	{
		if ( _initialized )
			return;

		NetworkStorage.EnsureConfigured();
		_initialized = true;
	}
}