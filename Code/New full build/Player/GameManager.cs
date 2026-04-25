using Sandbox;
using Sandbox.Network;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class GameManager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public Vector3 SpawnPoint { get; set; } = new Vector3( 0f, 0f, 50f );
	[Property] public bool AutoCreateLobby { get; set; } = true;
	[Property] public int MaxPlayers { get; set; } = 20;

	public static GameManager Instance { get; private set; }

	public class ChatMessage
	{
		public string Sender { get; set; }
		public string Text { get; set; }
		public RealTimeSince Created { get; set; }
	}

	public List<ChatMessage> ChatMessages { get; private set; } = new();
	public float ChatLifetime { get; set; } = 60f;
	public bool ChatOpen { get; set; }
	public string ChatInput { get; set; } = "";

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
				Name = "TerryScape Server"
			} );
		}
	}

	protected override void OnUpdate()
	{
		ChatMessages.RemoveAll( m => m.Created > ChatLifetime );

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

		if ( player != null )
		{
			var pc = player.Components.Get<PlayerController>();
			if ( pc != null )
				name = pc.Network.Owner?.DisplayName ?? "Player";
		}

		BroadcastChat( name, text );
	}

	[Rpc.Broadcast]
	void BroadcastChat( string sender, string text )
	{
		ChatMessages.Add( new ChatMessage
		{
			Sender = sender,
			Text = text,
			Created = 0
		} );

		if ( ChatMessages.Count > 100 )
			ChatMessages.RemoveAt( 0 );
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

		BroadcastChat( "Server", $"{connection.DisplayName} has joined." );
	}

	public void OnDisconnected( Connection connection )
	{
		Log.Info( $"Player disconnected: {connection.DisplayName}" );

		BroadcastChat( "Server", $"{connection.DisplayName} has left." );
	}

	public void OnBecameHost( Connection previousHost )
	{
		Log.Info( "This client is now the host." );
	}
}