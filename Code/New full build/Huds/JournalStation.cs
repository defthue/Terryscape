using Sandbox;

public sealed class JournalStation : Component
{
	public static bool IsOpen { get; private set; } = false;

	// Which Guidebook page is active. 0 = Journal, 1..9 = skills in sidebar order
	// (matches the SkillType enum values Woodcutting..Magic).
	public static int PageIndex { get; set; } = 0;

	public static void Open()
	{
		IsOpen = true;
		Mouse.Visibility = MouseVisibility.Visible;
	}

	public static void OpenToJournal()
	{
		PageIndex = 0;
		Open();
	}

	public static void OpenToSkill( SkillType skill )
	{
		PageIndex = (int)skill;
		Open();
	}

	public static void Close()
	{
		// Only touch the mouse if the journal was actually open. Otherwise calling
		// Close() unconditionally (e.g. from PlayerPersistence on join, to clean up
		// transient HUD state) would hide the mouse even when other HUDs like the
		// WelcomeHud need it visible.
		if ( !IsOpen )
			return;

		IsOpen = false;
		Mouse.Visibility = MouseVisibility.Hidden;
	}
}
