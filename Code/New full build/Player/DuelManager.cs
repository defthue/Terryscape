using Sandbox;

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
	[Sync] public float PhaseTimer { get; set; }

	enum Phase { Idle, Countdown, Live, RoundOver, MatchOver }
	Phase _phase = Phase.Idle;

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
		return owner != null ? owner.SteamId : 0;
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

		if ( !MatchActive )
			return;

		if ( !DuelistsValid() )
		{
			ResolveForfeit();
			return;
		}

		switch ( _phase )
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

	public static bool LocalDuelUiOpen => DuelMaster.IsOpen || ( Instance != null && Instance._localChallengePending );

	bool _cursorShownByDuel;

	void UpdateLocalDuelCursor()
	{
		bool wantCursor = DuelMaster.IsOpen || _localChallengePending;

		if ( wantCursor && !_cursorShownByDuel )
		{
			Mouse.Visibility = MouseVisibility.Visible;
			_cursorShownByDuel = true;
		}
		else if ( !wantCursor && _cursorShownByDuel )
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

		if ( MatchActive )
			return;

		if ( challenger == null || target == null || challenger == target )
			return;

		var cs = challenger.Components.Get<PvpState>();
		var ts = target.Components.Get<PvpState>();
		if ( cs == null || ts == null || !cs.InArena || !ts.InArena )
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

		StartMatch( challenger, target, rounds );
	}

	void StartMatch( GameObject a, GameObject b, int rounds )
	{
		DuelistA = a;
		DuelistB = b;
		DuelistASteamId = SteamIdOf( a );
		DuelistBSteamId = SteamIdOf( b );
		RoundsToWin = ( rounds / 2 ) + 1;
		ScoreA = 0;
		ScoreB = 0;
		MatchActive = true;

		GameLog.Add( "Duel accepted! Get ready...", "#6db8f0" );
		BeginRound();
	}

	void BeginRound()
	{
		RoundLive = false;
		_phase = Phase.Countdown;
		PhaseTimer = CountdownSeconds;

		ResetDuelist( DuelistA, PadA );
		ResetDuelist( DuelistB, PadB );
	}

	void ResetDuelist( GameObject duelist, GameObject pad )
	{
		if ( duelist == null )
			return;

		var health = duelist.Components.Get<PlayerHealth>();
		if ( health == null )
			return;

		Vector3 pos = pad != null ? pad.WorldPosition : duelist.WorldPosition;
		health.ArenaReset( pos );
	}

	void TickCountdown()
	{
		PhaseTimer -= Time.Delta;
		if ( PhaseTimer > 0f )
			return;

		RoundLive = true;
		_phase = Phase.Live;
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

		_phase = Phase.RoundOver;
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
			_phase = Phase.MatchOver;
			PhaseTimer = MatchEndSeconds;

			GameObject winner = ScoreA > ScoreB ? DuelistA : DuelistB;
			string name = winner?.Network?.Owner?.DisplayName ?? "Someone";
			GameLog.Add( $"{name} wins the duel!", "#e0c060" );

			ResetDuelist( DuelistA, ReturnPoint != null ? ReturnPoint : PadA );
			ResetDuelist( DuelistB, ReturnPoint != null ? ReturnPoint : PadB );
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
			ResetDuelist( winner, ReturnPoint != null ? ReturnPoint : PadA );
		}

		EndMatch();
	}

	void EndMatch()
	{
		MatchActive = false;
		RoundLive = false;
		DuelistA = null;
		DuelistB = null;
		DuelistASteamId = 0;
		DuelistBSteamId = 0;
		ScoreA = 0;
		ScoreB = 0;
		_phase = Phase.Idle;
	}

	bool IsDead( GameObject duelist )
	{
		if ( duelist == null )
			return false;

		var health = duelist.Components.Get<PlayerHealth>();
		return health != null && health.IsDead;
	}
}
