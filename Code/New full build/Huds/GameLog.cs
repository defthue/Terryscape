using Sandbox;
using System.Collections.Generic;

public static class GameLog
{
    public class LogMessage
    {
        public string Text { get; set; }
        public string Color { get; set; }
        public RealTimeSince Created { get; set; }
    }

    public static List<LogMessage> Messages { get; private set; } = new();
    public static float MessageLifetime { get; set; } = 6f;

    public static void Add( string text, string color = "#e8e8e8" )
    {
        Messages.Add( new LogMessage
        {
            Text = text,
            Color = color,
            Created = 0
        } );

        if ( Messages.Count > 50 )
            Messages.RemoveAt( 0 );
    }

    public static void Prune()
    {
        Messages.RemoveAll( m => m.Created > MessageLifetime );
    }
}