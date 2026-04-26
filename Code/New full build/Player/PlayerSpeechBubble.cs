using Sandbox;

public sealed class PlayerSpeechBubble : Component
{
	public string CurrentMessage { get; private set; } = "";
	public bool IsVisible { get; private set; } = false;

	const float BubbleDurationSeconds = 15f;

	RealTimeSince _shownAt = 0f;

	public void ShowMessage( string text )
	{
		if ( string.IsNullOrWhiteSpace( text ) )
			return;

		CurrentMessage = text;
		IsVisible = true;
		_shownAt = 0f;
	}

	protected override void OnUpdate()
	{
		if ( !IsVisible )
			return;

		if ( _shownAt >= BubbleDurationSeconds )
		{
			IsVisible = false;
			CurrentMessage = "";
		}
	}
}