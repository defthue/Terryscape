using Sandbox;
using System.Collections.Generic;

public sealed class DuelManager : Component
{
	public static DuelManager Instance { get; private set; }

	[Property] public GameObject PadA { get; set; }
	[Property] public GameObject PadB { get; set; }
	[Property] public GameObject ReturnPoint { get; set; }
	[Property] public float CountdownSeconds { get; set; } = 3f;
	[Property] public float BetweenRoundSeconds { get; set; } = 2f;
	[Property] public float MatchEndSeconds { get; set; } = 3f;
	[Property] public float ChallengeTimeoutSeconds { get; set; } = 15f;

	[Sync] public bool MatchActive { get; set; }
	[Sync] public bool RoundLive { get; set; }
	[Sync] public GameObject DuelistA { get; set; }
	[Sync] public GameObject DuelistB { get; set; }
	[Sync] public ulong DuelistASteamId { get; set; }
	[Sync] public ulong DuelistBSteamId { get; set; }
	[Sync] public int RoundsToWin { get; set; }
	[Sync] public int ScoreA { get; set; }
	[Sync] public int ScoreB { get; set; }
	[Sync] public bool NormalizedActive { get; set; }
	[Sync] public int NormalizedHP { get; set; } = 100;
	[Property] public int NormalizedHitPower { get; set; } = 10;
	[Sync] public float PhaseTimer { get; set; }

	[Sync] public bool LobbyActive { get; set; }
	[Sync] public ulong LobbyChallengerSteamId { get; set; }
	[Sync] public ulong LobbyTargetSteamId { get; set; }
	[Sync] public int LobbyMode { get; set; }
	[Sync] public int LobbyPaceIndex { get; set; }
	[Sync] public int LobbyRounds { get; set; }
	[Sync] public bool LobbyChallengerLocked { get; set; }
	[Sync] public bool LobbyTargetLocked { get; set; }

	[Property] public int MatchmakingRounds { get; set; } = 3;
	[Property] public bool MatchmakingNormalized { get; set; } = true;
	[Property] public int MatchmakingPaceIndex { get; set; } = 1;

	[Sync] public List<ulong> QueueA { get; set; } = new();
	[Sync] public List<ulong> QueueB { get; set; } = new();
	[Sync] public List<ulong> Pool { get; set; } = new();

	struct PendingMatch
	{
		public ulong A;
		public ulong B;
		public int Rounds;
		public bool Normalized;
		public int Hp;
		public bool FromMatchmaking;
	}

	readonly List<PendingMatch> _arenaQueue = new();
	readonly List<ulong> _pool = new();

	public enum Phase { Idle, Countdown, Live, RoundOver, MatchOver }
	[Sync] public Phase CurrentPhase { get; set; } = Phase.Idle;

	GameObject _pendingChallenger;
	GameObject _pendingTarget;
	int _pendingRounds;
	float _pendingExpire;

	bool _localChallengePending;
	GameObject _localChallenger;
	int _localChallengeRounds;

	protected override void OnEnabled() { Instance = this; }
	protected override void OnDisabled() { if ( Instance == this ) Instance = null; }

	protected override void OnStart()
	{
		Instance = this;

		Log.Info( $"[DuelManager] OnStart Network.Active={Network.Active} IsProxy={IsProxy} IsHost={Networking.IsHost} Id={GameObject.Id}" );
	}

	public bool IsDuelist( GameObject go )
	{
		if ( go == null )
			return false;

		ulong id = SteamIdOf( go );
		if ( id == 0 )
			return false;

		return id == DuelistASteamId || id == DuelistBSteamId;
	}

	static ulong SteamIdOf( GameObject go )
	{
		var owner = go?.Network?.Owner;
		return owner != null ? owner.SteamId : 0ul;
	}

	public GameObject FindDuelist( ulong steamId )
	{
		if ( steamId == 0 )
			return null;

		foreach ( var p in PlayerHelper.GetAllPlayers() )
		{
			var owner = p?.Network?.Owner;
			if ( owner != null && owner.SteamId == steamId )
				return p;
		}

		return null;
	}

	protected override void OnUpdate()
	{
		UpdateLocalDuelCursor();

		if ( !Networking.IsHost )
			return;

		ExpirePending();

		if ( LobbyActive )
		{
			var lc = FindDuelist( LobbyChallengerSteamId );
			var lt = FindDuelist( LobbyTargetSteamId );
			if ( lc == null || !lc.IsValid() || lt == null || !lt.IsValid() )
				CloseLobby();
		}

		CleanQueues();
		TryMatchmake();
		TryStartNext();

		if ( !MatchActive )
			return;

		if ( !DuelistsValid() )
		{
			ResolveForfeit();
			return;
		}

		switch ( CurrentPhase )
		{
			case Phase.Countdown: TickCountdown(); break;
			case Phase.Live: TickLive(); break;
			case Phase.RoundOver: TickRoundOver(); break;
			case Phase.MatchOver: TickMatchOver(); break;
		}
	}

	public bool LocalChallengePending => _localChallengePending;
	public GameObject LocalChallenger => _localChallenger;
	public int LocalChallengeRounds => _localChallengeRounds;

	public void AcceptLocalChallenge()
	{
		if ( !_localChallengePending )
			return;

		_localChallengePending = false;
		RespondChallenge( PlayerHelper.GetLocalPlayer(), true );
	}

	public void DeclineLocalChallenge()
	{
		if ( !_localChallengePending )
			return;

		_localChallengePending = false;
		RespondChallenge( PlayerHelper.GetLocalPlayer(), false );
	}

	public void ClearLocalChallenge()
	{
		_localChallengePending = false;
	}

	public static bool LocalDuelUiOpen => DuelMaster.IsOpen || ( Instance != null && ( Instance._localChallengePending || Instance.LocalInLobby ) );

	bool _cursorShownByDuel;

	void UpdateLocalDuelCursor()
	{
		bool wantCursor = DuelMaster.IsOpen || _localChallengePending || LocalInLobby;

		if ( wantCursor )
		{
			Mouse.Visibility = MouseVisibility.Visible;
			_cursorShownByDuel = true;
		}
		else if ( _cursorShownByDuel )
		{
			Mouse.Visibility = MouseVisibility.Hidden;
			_cursorShownByDuel = false;
		}
	}

	void ExpirePending()
	{
		if ( _pendingTarget == null )
			return;

		if ( Time.Now >= _pendingExpire )
		{
			_pendingChallenger = null;
			_pendingTarget = null;
		}
	}

	bool DuelistsValid()
	{
		if ( DuelistA == null || !DuelistA.IsValid() || DuelistB == null || !DuelistB.IsValid() )
			return false;

		var sa = DuelistA.Components.Get<PvpState>();
		var sb = DuelistB.Components.Get<PvpState>();
		if ( sa == null || sb == null )
			return false;

		return sa.InArena && sb.InArena;
	}

	[Rpc.Broadcast]
	public void RequestChallenge( GameObject challenger, GameObject target, int rounds )
	{
		if ( !Networking.IsHost )
			return;

		if ( LobbyActive )
			return;

		if ( _pendingTarget != null )
			return;

		if ( challenger == null || target == null || challenger == target )
			return;

		var cs = challenger.Components.Get<PvpState>();
		var ts = target.Components.Get<PvpState>();
		if ( cs == null || ts == null || !cs.InArena || !ts.InArena )
			return;

		if ( IsBusyInDuelSystem( SteamIdOf( challenger ) ) || IsBusyInDuelSystem( SteamIdOf( target ) ) )
			return;

		_pendingChallenger = challenger;
		_pendingTarget = target;
		_pendingRounds = rounds < 1 ? 1 : rounds;
		_pendingExpire = Time.Now + ChallengeTimeoutSeconds;

		BroadcastChallengePrompt( challenger, target, _pendingRounds );
	}

	[Rpc.Broadcast]
	void BroadcastChallengePrompt( GameObject challenger, GameObject target, int rounds )
	{
		if ( !PlayerHelper.IsLocalPlayer( target ) )
			return;

		_localChallengePending = true;
		_localChallenger = challenger;
		_localChallengeRounds = rounds;

		string name = challenger?.Network?.Owner?.DisplayName ?? "Someone";
		GameLog.Add( $"{name} challenges you to a duel (best of {rounds}). Press E to accept.", "#e0c060" );
	}

	[Rpc.Broadcast]
	public void RespondChallenge( GameObject target, bool accept )
	{
		if ( !Networking.IsHost )
			return;

		if ( _pendingTarget == null || target != _pendingTarget )
			return;

		var challenger = _pendingChallenger;
		int rounds = _pendingRounds;

		_pendingChallenger = null;
		_pendingTarget = null;

		if ( !accept )
			return;

		if ( challenger == null || !challenger.IsValid() || target == null || !target.IsValid() )
			return;

		OpenLobby( challenger, target, rounds );
	}

	void StartMatch( GameObject a, GameObject b, int rounds, bool normalized = false, int normalizedHp = 100 )
	{
		DuelistA = a;
		DuelistB = b;
		DuelistASteamId = SteamIdOf( a );
		DuelistBSteamId = SteamIdOf( b );
		RoundsToWin = ( rounds / 2 ) + 1;
		ScoreA = 0;
		ScoreB = 0;
		NormalizedActive = normalized;
		NormalizedHP = normalizedHp;
		MatchActive = true;

		string an = a?.Network?.Owner?.DisplayName ?? "Someone";
		string bn = b?.Network?.Owner?.DisplayName ?? "Someone";
		GameManager.Instance?.BroadcastServerNotice( $"A duel has begun between {an} and {bn} in the Colosseum!" );

		GameLog.Add( "Duel accepted! Get ready...", "#6db8f0" );
		BeginRound();
	}

	void OpenLobby( GameObject challenger, GameObject target, int rounds )
	{
		LobbyChallengerSteamId = SteamIdOf( challenger );
		LobbyTargetSteamId = SteamIdOf( target );
		LobbyMode = 0;
		LobbyPaceIndex = 1;
		LobbyRounds = ( rounds == 1 || rounds == 3 || rounds == 5 ) ? rounds : 1;
		LobbyChallengerLocked = false;
		LobbyTargetLocked = false;
		LobbyActive = true;
	}

	[Rpc.Broadcast]
	public void RequestSetLobbyMode( ulong actor, int mode )
	{
		if ( !Networking.IsHost ) return;
		if ( !LobbyActive || actor != LobbyChallengerSteamId ) return;
		LobbyMode = mode == 1 ? 1 : 0;
		LobbyChallengerLocked = false;
		LobbyTargetLocked = false;
	}

	[Rpc.Broadcast]
	public void RequestSetLobbyPace( ulong actor, int paceIndex )
	{
		if ( !Networking.IsHost ) return;
		if ( !LobbyActive || actor != LobbyChallengerSteamId ) return;
		LobbyPaceIndex = paceIndex < 0 ? 0 : ( paceIndex > 2 ? 2 : paceIndex );
		LobbyChallengerLocked = false;
		LobbyTargetLocked = false;
	}

	[Rpc.Broadcast]
	public void RequestSetLobbyRounds( ulong actor, int rounds )
	{
		if ( !Networking.IsHost ) return;
		if ( !LobbyActive || actor != LobbyChallengerSteamId ) return;
		if ( rounds == 1 || rounds == 3 || rounds == 5 )
			LobbyRounds = rounds;
		LobbyChallengerLocked = false;
		LobbyTargetLocked = false;
	}

	[Rpc.Broadcast]
	public void RequestSetLobbyLock( ulong actor, bool locked )
	{
		if ( !Networking.IsHost ) return;
		if ( !LobbyActive ) return;

		if ( actor == LobbyChallengerSteamId )
			LobbyChallengerLocked = locked;
		else if ( actor == LobbyTargetSteamId )
			LobbyTargetLocked = locked;
		else
			return;

		if ( LobbyChallengerLocked && LobbyTargetLocked )
			BeginMatchFromLobby();
	}

	[Rpc.Broadcast]
	public void RequestCancelLobby( ulong actor )
	{
		if ( !Networking.IsHost ) return;
		if ( !LobbyActive ) return;
		if ( actor != LobbyChallengerSteamId && actor != LobbyTargetSteamId ) return;
		CloseLobby();
	}

	void BeginMatchFromLobby()
	{
		ulong challengerId = LobbyChallengerSteamId;
		ulong targetId = LobbyTargetSteamId;
		int rounds = LobbyRounds;
		bool normalized = LobbyMode == 1;
		int hp = PaceToHp( LobbyPaceIndex );

		CloseLobby();

		if ( challengerId == 0 || targetId == 0 )
			return;

		EnqueueMatch( challengerId, targetId, rounds, normalized, hp, false );
	}

	void CloseLobby()
	{
		LobbyActive = false;
		LobbyChallengerSteamId = 0ul;
		LobbyTargetSteamId = 0ul;
		LobbyChallengerLocked = false;
		LobbyTargetLocked = false;
	}

	bool IsInArena( GameObject go )
	{
		var s = go?.Components.Get<PvpState>();
		return s != null && s.InArena;
	}

	bool IsBusyInDuelSystem( ulong steamId )
	{
		if ( steamId == 0 ) return true;
		if ( steamId == DuelistASteamId || steamId == DuelistBSteamId ) return true;
		if ( steamId == LobbyChallengerSteamId || steamId == LobbyTargetSteamId ) return true;
		if ( _pool.Contains( steamId ) ) return true;
		foreach ( var m in _arenaQueue )
			if ( m.A == steamId || m.B == steamId ) return true;
		return false;
	}

	public bool IsInQueueSystem( ulong steamId )
	{
		if ( steamId == 0 ) return false;
		if ( Pool != null && Pool.Contains( steamId ) ) return true;
		if ( QueueA != null && QueueA.Contains( steamId ) ) return true;
		if ( QueueB != null && QueueB.Contains( steamId ) ) return true;
		return false;
	}

	void EnqueueMatch( ulong a, ulong b, int rounds, bool normalized, int hp, bool fromMatchmaking )
	{
		_arenaQueue.Add( new PendingMatch
		{
			A = a,
			B = b,
			Rounds = rounds,
			Normalized = normalized,
			Hp = hp,
			FromMatchmaking = fromMatchmaking
		} );

		SyncQueueState();
		TryStartNext();
	}

	[Rpc.Broadcast]
	public void RequestJoinQueue( ulong actor )
	{
		if ( !Networking.IsHost ) return;

		var go = FindDuelist( actor );
		if ( go == null || !go.IsValid() || !IsInArena( go ) ) return;
		if ( IsBusyInDuelSystem( actor ) ) return;

		_pool.Add( actor );
		SyncQueueState();
		TryMatchmake();
		TryStartNext();
	}

	[Rpc.Broadcast]
	public void RequestLeaveQueue( ulong actor )
	{
		if ( !Networking.IsHost ) return;
		if ( actor == 0 ) return;

		bool changed = _pool.Remove( actor );

		for ( int i = _arenaQueue.Count - 1; i >= 0; i-- )
		{
			var m = _arenaQueue[i];
			if ( m.A != actor && m.B != actor ) continue;

			ulong partner = m.A == actor ? m.B : m.A;
			_arenaQueue.RemoveAt( i );
			changed = true;

			if ( m.FromMatchmaking && partner != 0 && !IsBusyInDuelSystem( partner ) && IsInArena( FindDuelist( partner ) ) )
				_pool.Insert( 0, partner );
		}

		if ( changed )
		{
			SyncQueueState();
			TryMatchmake();
			TryStartNext();
		}
	}

	void TryMatchmake()
	{
		bool changed = false;

		while ( _pool.Count >= 2 )
		{
			ulong a = _pool[0];
			ulong b = _pool[1];
			_pool.RemoveRange( 0, 2 );

			_arenaQueue.Add( new PendingMatch
			{
				A = a,
				B = b,
				Rounds = MatchmakingRounds,
				Normalized = MatchmakingNormalized,
				Hp = PaceToHp( MatchmakingPaceIndex ),
				FromMatchmaking = true
			} );
			changed = true;
		}

		if ( changed )
			SyncQueueState();
	}

	void TryStartNext()
	{
		if ( !Networking.IsHost ) return;
		if ( MatchActive ) return;

		while ( _arenaQueue.Count > 0 )
		{
			var m = _arenaQueue[0];
			_arenaQueue.RemoveAt( 0 );

			var a = FindDuelist( m.A );
			var b = FindDuelist( m.B );
			bool aOk = a != null && a.IsValid() && IsInArena( a );
			bool bOk = b != null && b.IsValid() && IsInArena( b );

			if ( aOk && bOk )
			{
				SyncQueueState();
				StartMatch( a, b, m.Rounds, m.Normalized, m.Hp );
				return;
			}

			if ( m.FromMatchmaking )
			{
				ulong survivor = aOk ? m.A : ( bOk ? m.B : 0 );
				if ( survivor != 0 && !IsBusyInDuelSystem( survivor ) )
					_pool.Insert( 0, survivor );
			}
		}

		SyncQueueState();
	}

	void CleanQueues()
	{
		bool changed = false;

		for ( int i = _pool.Count - 1; i >= 0; i-- )
		{
			var go = FindDuelist( _pool[i] );
			if ( go == null || !go.IsValid() || !IsInArena( go ) )
			{
				_pool.RemoveAt( i );
				changed = true;
			}
		}

		for ( int i = _arenaQueue.Count - 1; i >= 0; i-- )
		{
			var m = _arenaQueue[i];
			var a = FindDuelist( m.A );
			var b = FindDuelist( m.B );
			bool aOk = a != null && a.IsValid() && IsInArena( a );
			bool bOk = b != null && b.IsValid() && IsInArena( b );

			if ( aOk && bOk )
				continue;

			_arenaQueue.RemoveAt( i );
			changed = true;

			if ( m.FromMatchmaking )
			{
				ulong survivor = aOk ? m.A : ( bOk ? m.B : 0 );
				if ( survivor != 0 && !IsBusyInDuelSystem( survivor ) )
					_pool.Insert( 0, survivor );
			}
		}

		if ( changed )
			SyncQueueState();
	}

	void SyncQueueState()
	{
		var a = new List<ulong>();
		var b = new List<ulong>();
		foreach ( var m in _arenaQueue )
		{
			a.Add( m.A );
			b.Add( m.B );
		}
		QueueA = a;
		QueueB = b;
		Pool = new List<ulong>( _pool );
	}

	static int PaceToHp( int paceIndex )
	{
		switch ( paceIndex )
		{
			case 0: return 50;
			case 2: return 150;
			default: return 100;
		}
	}

	public bool LocalInLobby
	{
		get
		{
			if ( !LobbyActive ) return false;
			ulong local = PlayerHelper.GetLocalPlayer()?.Network?.Owner?.SteamId ?? 0ul;
			return local != 0ul && ( local == LobbyChallengerSteamId || local == LobbyTargetSteamId );
		}
	}

	void BeginRound()
	{
		RoundLive = false;
		CurrentPhase = Phase.Countdown;
		PhaseTimer = CountdownSeconds;

		int nMax = NormalizedActive ? NormalizedHP : 0;
		ResetDuelist( DuelistA, PadA, nMax );
		ResetDuelist( DuelistB, PadB, nMax );

		BroadcastCountdownSound();
	}

	[Rpc.Broadcast]
	void BroadcastCountdownSound()
	{
		var local = PlayerHelper.GetLocalPlayer();
		var state = local?.Components.Get<PvpState>();
		if ( state == null || !state.InArena )
			return;

		SoundLibrary.PlayCountdown();
	}

	void ResetDuelist( GameObject duelist, GameObject pad, int normalizedMax )
	{
		if ( duelist == null )
			return;

		var health = duelist.Components.Get<PlayerHealth>();
		if ( health == null )
			return;

		Vector3 pos = pad != null ? pad.WorldPosition : duelist.WorldPosition;
		health.ArenaReset( pos, normalizedMax );
	}

	void TickCountdown()
	{
		PhaseTimer -= Time.Delta;
		if ( PhaseTimer > 0f )
			return;

		RoundLive = true;
		CurrentPhase = Phase.Live;
		GameLog.Add( "Fight!", "#6db8f0" );
	}

	void TickLive()
	{
		bool aDead = IsDead( DuelistA );
		bool bDead = IsDead( DuelistB );

		if ( !aDead && !bDead )
			return;

		RoundLive = false;

		if ( bDead )
			ScoreA++;
		else if ( aDead )
			ScoreB++;

		CurrentPhase = Phase.RoundOver;
		PhaseTimer = BetweenRoundSeconds;

		GameLog.Add( $"Round over. Score {ScoreA} - {ScoreB}.", "#a8c8a8" );
	}

	void TickRoundOver()
	{
		PhaseTimer -= Time.Delta;
		if ( PhaseTimer > 0f )
			return;

		if ( ScoreA >= RoundsToWin || ScoreB >= RoundsToWin )
		{
			CurrentPhase = Phase.MatchOver;
			PhaseTimer = MatchEndSeconds;

			GameObject winner = ScoreA > ScoreB ? DuelistA : DuelistB;
			string name = winner?.Network?.Owner?.DisplayName ?? "Someone";
			GameLog.Add( $"{name} wins the duel!", "#e0c060" );

			ulong winnerSteamId = winner?.Network?.Owner?.SteamId ?? 0ul;
			if ( winnerSteamId != 0ul )
				BroadcastDuelWin( winnerSteamId );

			ResetDuelist( DuelistA, ReturnPoint != null ? ReturnPoint : PadA, 0 );
			ResetDuelist( DuelistB, ReturnPoint != null ? ReturnPoint : PadB, 0 );
			return;
		}

		BeginRound();
	}

	void TickMatchOver()
	{
		PhaseTimer -= Time.Delta;
		if ( PhaseTimer > 0f )
			return;

		EndMatch();
	}

	void ResolveForfeit()
	{
		GameObject winner = null;

		if ( DuelistA != null && DuelistA.IsValid() )
		{
			var sa = DuelistA.Components.Get<PvpState>();
			if ( sa != null && sa.InArena )
				winner = DuelistA;
		}

		if ( winner == null && DuelistB != null && DuelistB.IsValid() )
		{
			var sb = DuelistB.Components.Get<PvpState>();
			if ( sb != null && sb.InArena )
				winner = DuelistB;
		}

		if ( winner != null )
		{
			string name = winner.Network?.Owner?.DisplayName ?? "Someone";
			GameLog.Add( $"{name} wins by forfeit.", "#e0c060" );
			ulong winnerSteamId = winner.Network?.Owner?.SteamId ?? 0ul;
			if ( winnerSteamId != 0ul )
				BroadcastDuelWin( winnerSteamId );
			ResetDuelist( winner, ReturnPoint != null ? ReturnPoint : PadA, 0 );
		}

		EndMatch();
	}

	[Rpc.Broadcast]
	void BroadcastDuelWin( ulong winnerSteamId )
	{
		if ( Connection.Local == null || Connection.Local.SteamId != winnerSteamId )
			return;

		AchievementTracker.OnDuelWon();
	}

	void EndMatch()
	{
		if ( DuelistA != null && DuelistA.IsValid() )
			DuelistA.Components.Get<PlayerHealth>()?.EndNormalizedMode();
		if ( DuelistB != null && DuelistB.IsValid() )
			DuelistB.Components.Get<PlayerHealth>()?.EndNormalizedMode();

		MatchActive = false;
		NormalizedActive = false;
		RoundLive = false;
		DuelistA = null;
		DuelistB = null;
		DuelistASteamId = 0;
		DuelistBSteamId = 0;
		ScoreA = 0;
		ScoreB = 0;
		CurrentPhase = Phase.Idle;

		TryStartNext();
	}

	bool IsDead( GameObject duelist )
	{
		if ( duelist == null )
			return false;

		var health = duelist.Components.Get<PlayerHealth>();
		return health != null && health.IsDead;
	}
}
