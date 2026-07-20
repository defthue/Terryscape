using Sandbox;
using Sandbox.Network;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class GameManager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public Vector3 SpawnPoint { get; set; } = new Vector3( 0f, 0f, 50f );
	[Property] public float SpawnYawDegrees { get; set; } = 0f;
	[Property] public float SpawnPitchDegrees { get; set; } = 0f;
	[Property] public bool AutoCreateLobby { get; set; } = true;
	[Property] public int MaxPlayers { get; set; } = 64;

	[Property] public int MaxChatMessageLength { get; set; } = 200;
	[Property] public float ChatCooldownSeconds { get; set; } = 1.0f;

	public static GameManager Instance { get; private set; }

	public class ChatMessage
	{
		public string Sender { get; set; }
		public string Text { get; set; }
		public RealTimeSince Created { get; set; }
		public long Sequence { get; set; }
	}

	public List<ChatMessage> ChatMessages { get; private set; } = new();
	public bool ChatOpen { get; set; }
	public string ChatInput { get; set; } = "";

	public RealTimeSince TimeSinceLastSentChat { get; private set; } = 999f;
	public RealTimeSince TimeSinceChatBlocked { get; private set; } = 999f;

	const int MaxChatMessages = 100;

	static long _nextChatSequence = 0;

	public void AddLocalChatMessage( string text )
	{
		ChatMessages.Add( new ChatMessage
		{
			Sender = null,
			Text = text,
			Created = 0,
			Sequence = _nextChatSequence++
		} );

		if ( ChatMessages.Count > MaxChatMessages )
			ChatMessages.RemoveAt( 0 );
	}

	protected override async void OnStart()
	{
		Instance = this;

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

	public bool SendChat( string text )
	{
		if ( string.IsNullOrWhiteSpace( text ) )
			return false;

		if ( TimeSinceLastSentChat < ChatCooldownSeconds )
		{
			TimeSinceChatBlocked = 0f;
			return false;
		}

		var trimmed = text.Trim();
		if ( trimmed.Length > MaxChatMessageLength )
			trimmed = trimmed.Substring( 0, MaxChatMessageLength );

		TimeSinceLastSentChat = 0f;

		var player = PlayerHelper.GetLocalPlayer();
		string name = "Player";
		ulong steamId = 0;

		if ( player != null )
		{
			var pc = player.Components.Get<PlayerController>();
			if ( pc != null )
			{
				name = pc.Network.Owner?.DisplayName ?? "Player";
				steamId = pc.Network.Owner?.SteamId ?? 0ul;
			}
		}

		BroadcastChat( name, trimmed, steamId );
		return true;
	}

	[Rpc.Broadcast]
	void BroadcastChat( string sender, string text, ulong speakerSteamId )
	{
		ChatMessages.Add( new ChatMessage
		{
			Sender = sender,
			Text = text,
			Created = 0,
			Sequence = _nextChatSequence++
		} );

		if ( ChatMessages.Count > MaxChatMessages )
			ChatMessages.RemoveAt( 0 );

		if ( speakerSteamId != 0 )
		{
			var bubble = FindBubbleForSteamId( speakerSteamId );
			if ( bubble != null )
				bubble.ShowMessage( text );
		}
	}

	public void BroadcastLevelMilestone( string playerName, string skillName, int level )
	{
		DoBroadcastLevelMilestone( playerName, skillName, level );
	}

	[Rpc.Broadcast]
	void DoBroadcastLevelMilestone( string playerName, string skillName, int level )
	{
		string text = $"{playerName} reached level {level} in {skillName}!";
		ChatMessages.Add( new ChatMessage
		{
			Sender = "Server",
			Text = text,
			Created = 0,
			Sequence = _nextChatSequence++
		} );

		if ( ChatMessages.Count > MaxChatMessages )
			ChatMessages.RemoveAt( 0 );
	}

	public void BroadcastServerNotice( string text )
	{
		DoBroadcastServerNotice( text );
	}

	[Rpc.Broadcast]
	void DoBroadcastServerNotice( string text )
	{
		if ( string.IsNullOrWhiteSpace( text ) )
			return;

		ChatMessages.Add( new ChatMessage
		{
			Sender = "Server",
			Text = text,
			Created = 0,
			Sequence = _nextChatSequence++
		} );

		if ( ChatMessages.Count > MaxChatMessages )
			ChatMessages.RemoveAt( 0 );
	}

	PlayerSpeechBubble FindBubbleForSteamId( ulong steamId )
	{
		foreach ( var bubble in Scene.GetAllComponents<PlayerSpeechBubble>() )
		{
			var ownerId = bubble.Network.Owner?.SteamId ?? 0ul;
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

		var player = PlayerPrefab.Clone( new Transform( SpawnPoint, Rotation.FromYaw( SpawnYawDegrees ) ) );
		player.Name = $"Player - {connection.DisplayName}";
		player.NetworkSpawn( connection );

		Log.Info( $"Player spawned: {connection.DisplayName}" );

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