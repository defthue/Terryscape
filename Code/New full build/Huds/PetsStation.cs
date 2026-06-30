using Sandbox;

public sealed class PetsStation : Component
{
	public static bool IsOpen { get; private set; } = false;

	public static void Open()
	{
		IsOpen = true;
		Mouse.Visibility = MouseVisibility.Visible;
	}

	public static void Close()
	{
		if ( !IsOpen )
			return;

		IsOpen = false;
		Mouse.Visibility = MouseVisibility.Hidden;
	}
}
