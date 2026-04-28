using Sandbox;

public sealed class LeaderboardStation : Component
{
	public static bool IsOpen { get; private set; } = false;

	public static void Open()
	{
		IsOpen = true;
		Mouse.Visibility = MouseVisibility.Visible;
	}

	public static void Close()
	{
		// Only touch the mouse if the leaderboard was actually open. Otherwise calling
		// Close() unconditionally (e.g. from PlayerPersistence on join) would hide the
		// mouse even when other HUDs like the WelcomeHud need it visible.
		if ( !IsOpen )
			return;

		IsOpen = false;
		Mouse.Visibility = MouseVisibility.Hidden;
	}
}
