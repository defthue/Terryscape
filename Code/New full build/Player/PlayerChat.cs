using Sandbox;
using System.Collections.Generic;

public sealed class PlayerChat : Component
{
	public static PlayerChat Instance { get; private set; }

	public class ChatMessage
	{
		public string Sender { get; set; }
		public string Text { get; set; }
		public RealTimeSince Created { get; set; }
	}

	public List<ChatMessage> Messages { get; private set; } = new();
	public float MessageLifetime { get; set; } = 30f;
	public bool IsOpen { get; set; }
	public string CurrentInput { get; set; } = "";

	protected override void OnStart()
	{
		Instance = this;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		Messages.RemoveAll( m => m.Created > MessageLifetime );

		if ( Input.Pressed( "Chat" ) && !IsOpen )
		{
			IsOpen = true;
			Mouse.Visibility = MouseVisibility.Visible;
		}
	}

	public void SendMessage( string text )
	{
		if ( string.IsNullOrWhiteSpace( text ) )
			return;

		string name = Network.Owner?.DisplayName ?? "Player";
		BroadcastMessage( name, text );
	}

	[Rpc.Broadcast]
	void BroadcastMessage( string sender, string text )
	{
		Messages.Add( new ChatMessage
		{
			Sender = sender,
			Text = text,
			Created = 0
		} );

		if ( Messages.Count > 50 )
			Messages.RemoveAt( 0 );
	}

	public void CloseChat()
	{
		IsOpen = false;
		CurrentInput = "";
		Mouse.Visibility = MouseVisibility.Hidden;
	}
}
