using Sandbox;
using System.Collections.Generic;

public static class GameLog
{
	public class LogMessage
	{
		public string Text { get; set; }
		public string Color { get; set; }
		public RealTimeSince Created { get; set; }
		public long Sequence { get; set; }
	}

	public static List<LogMessage> Messages { get; private set; } = new();

	const int MaxMessages = 100;

	static long _nextSequence = 0;

	public static bool FocusAllTabRequested { get; private set; } = false;

	public static void RequestFocusAllTab()
	{
		FocusAllTabRequested = true;
	}

	public static void ConsumeFocusAllTabRequest()
	{
		FocusAllTabRequested = false;
	}

	public static void Add( string text, string color = "#e8e8e8" )
	{
		Messages.Add( new LogMessage
		{
			Text = text,
			Color = color,
			Created = 0,
			Sequence = _nextSequence++
		} );

		if ( Messages.Count > MaxMessages )
			Messages.RemoveAt( 0 );
	}

	// Kept for backwards compatibility; now a no-op since we removed time-based expiry.
	public static void Prune()
	{
	}
}