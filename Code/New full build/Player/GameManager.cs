using Sandbox;
using Sandbox.Network;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class GameManager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public Vector3 SpawnPoint { get; set; } = new Vector3( 0f, 0f, 50f );
	[Property] public bool AutoCreateLobby { get; set; } = true;
	[Property] public int MaxPlayers { get; set; } = 64;

	public static GameManager Instance { get; private set; }

	public class ChatMessage
	{
		public string Sender { get; set; }
		public string Text { get; set; }
		public RealTimeSince Created { get; set; }
	}

	public List<ChatMessage> ChatMessages { get; private set; } = new();
	public bool ChatOpen { get; set; }
	public string ChatInput { get; set; } = "";

	const int MaxChatMessages = 100;

	protected override async void OnStart()
	{
		Instance = this;

		// Initialize Network Storage as early as possible so any player that joins immediately has it ready.
		NetworkStorageConfig.EnsureInitialized();

		if ( !AutoCreateLobby )
			return;

		if ( !Networking.IsActive )
		{
			Networking.CreateLobby( new LobbyConfig
			{
				MaxPlayers = MaxPlayers,
				Privacy = LobbyPrivacy.Public,
				Name = "Terry's Quest Server"
			} );
		}
	}

	protected override void OnUpdate()
	{
		if ( Input.Pressed( "Chat" ) && !ChatOpen )
		{
			ChatOpen = true;
			Mouse.Visibility = MouseVisibility.Visible;
		}
	}

	public void SendChat( string text )
	{
		if ( string.IsNullOrWhiteSpace( text ) )
			return;

		var player = PlayerHelper.GetLocalPlayer();
		string name = "Player";
		ulong steamId = 0;

		if ( player != null )
		{
			var pc = player.Components.Get<PlayerController>();
			if ( pc != null )
			{
				name = pc.Network.Owner?.DisplayName ?? "Player";
				steamId = pc.Network.Owner?.SteamId ?? 0;
			}
		}

		BroadcastChat( name, text, steamId );
	}

	[Rpc.Broadcast]
	void BroadcastChat( string sender, string text, ulong speakerSteamId )
	{
		ChatMessages.Add( new ChatMessage
		{
			Sender = sender,
			Text = text,
			Created = 0
		} );

		if ( ChatMessages.Count > MaxChatMessages )
			ChatMessages.RemoveAt( 0 );

		// Trigger the speech bubble for the speaking player, if we can find them.
		// steamId == 0 means it's a server message ("X has joined") — no bubble for those.
		if ( speakerSteamId != 0 )
		{
			var bubble = FindBubbleForSteamId( speakerSteamId );
			if ( bubble != null )
				bubble.ShowMessage( text );
		}
	}

	PlayerSpeechBubble FindBubbleForSteamId( ulong steamId )
	{
		foreach ( var bubble in Scene.GetAllComponents<PlayerSpeechBubble>() )
		{
			var ownerId = bubble.Network.Owner?.SteamId ?? 0;
			if ( ownerId == steamId )
				return bubble;
		}
		return null;
	}

	public void CloseChat()
	{
		ChatOpen = false;
		ChatInput = "";
		Mouse.Visibility = MouseVisibility.Hidden;
	}

	public void OnActive( Connection connection )
	{
		if ( PlayerPrefab == null )
		{
			Log.Warning( "GameManager: No PlayerPrefab assigned!" );
			return;
		}

		var player = PlayerPrefab.Clone( SpawnPoint );
		player.Name = $"Player - {connection.DisplayName}";
		player.NetworkSpawn( connection );

		Log.Info( $"Player spawned: {connection.DisplayName}" );

		// Server messages have steamId 0 — no bubble.
		BroadcastChat( "Server", $"{connection.DisplayName} has joined.", 0 );
	}

	public void OnDisconnected( Connection connection )
	{
		Log.Info( $"Player disconnected: {connection.DisplayName}" );

		BroadcastChat( "Server", $"{connection.DisplayName} has left.", 0 );
	}

	public void OnBecameHost( Connection previousHost )
	{
		Log.Info( "This client is now the host." );
	}
}