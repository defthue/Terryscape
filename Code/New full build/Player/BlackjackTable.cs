using Sandbox;
using System;
using System.Collections.Generic;

public sealed class BlackjackTable : Component
{
	public enum Phase { Waiting, Betting, Dealing, PlayerTurns, DealerTurn, Payout, Reset }

	[Property] public List<BlackjackSeat> Seats { get; set; } = new();
	[Property] public GameObject DealerHandAnchor { get; set; }

	[Property] public float BettingDuration { get; set; } = 15f;
	[Property] public float TurnDuration { get; set; } = 20f;
	[Property] public float PayoutDuration { get; set; } = 5f;
	[Property] public float ResetDuration { get; set; } = 2f;
	[Property] public float DealStepDuration { get; set; } = 0.4f;
	[Property] public float ActionDelay { get; set; } = 0.5f;
	[Property] public float DealerSlowDelay { get; set; } = 1f;
	[Property] public int MinBet { get; set; } = 5;

	[Sync] public Phase CurrentPhase { get; set; } = Phase.Waiting;
	[Sync] public int ActiveSeatIndex { get; set; } = -1;
	[Sync] public int ActiveHandIndex { get; set; } = 0;
	[Sync] public float PhaseTimeRemaining { get; set; } = 0f;

	[Sync] public NetList<int> Seat0Hand0 { get; set; } = new();
	[Sync] public NetList<int> Seat0Hand1 { get; set; } = new();
	[Sync] public NetList<int> Seat1Hand0 { get; set; } = new();
	[Sync] public NetList<int> Seat1Hand1 { get; set; } = new();
	[Sync] public NetList<int> Seat2Hand0 { get; set; } = new();
	[Sync] public NetList<int> Seat2Hand1 { get; set; } = new();
	[Sync] public NetList<int> Seat3Hand0 { get; set; } = new();
	[Sync] public NetList<int> Seat3Hand1 { get; set; } = new();
	[Sync] public NetList<int> Seat4Hand0 { get; set; } = new();
	[Sync] public NetList<int> Seat4Hand1 { get; set; } = new();
	[Sync] public NetList<int> DealerHand { get; set; } = new();
	[Sync] public bool DealerHoleHidden { get; set; } = true;

	[Sync] public int Seat0Bet0 { get; set; } = 0;
	[Sync] public int Seat0Bet1 { get; set; } = 0;
	[Sync] public int Seat1Bet0 { get; set; } = 0;
	[Sync] public int Seat1Bet1 { get; set; } = 0;
	[Sync] public int Seat2Bet0 { get; set; } = 0;
	[Sync] public int Seat2Bet1 { get; set; } = 0;
	[Sync] public int Seat3Bet0 { get; set; } = 0;
	[Sync] public int Seat3Bet1 { get; set; } = 0;
	[Sync] public int Seat4Bet0 { get; set; } = 0;
	[Sync] public int Seat4Bet1 { get; set; } = 0;

	[Sync] public bool Seat0HasSplit { get; set; }
	[Sync] public bool Seat1HasSplit { get; set; }
	[Sync] public bool Seat2HasSplit { get; set; }
	[Sync] public bool Seat3HasSplit { get; set; }
	[Sync] public bool Seat4HasSplit { get; set; }

	[Sync] public bool Seat0Hand0Done { get; set; }
	[Sync] public bool Seat0Hand1Done { get; set; }
	[Sync] public bool Seat1Hand0Done { get; set; }
	[Sync] public bool Seat1Hand1Done { get; set; }
	[Sync] public bool Seat2Hand0Done { get; set; }
	[Sync] public bool Seat2Hand1Done { get; set; }
	[Sync] public bool Seat3Hand0Done { get; set; }
	[Sync] public bool Seat3Hand1Done { get; set; }
	[Sync] public bool Seat4Hand0Done { get; set; }
	[Sync] public bool Seat4Hand1Done { get; set; }

	[Sync, Change] public string LastResultText { get; set; } = "";

	List<int> _deck = new();
	int _deckIndex;
	int _dealStep;
	float _dealStepTimer;
	float _dealerStepTimer;
	float _actionDelayTimer;
	List<bool> _seatActiveThisRound = new() { false, false, false, false, false };
	List<bool> _seatWonThisRound = new() { false, false, false, false, false };
	bool _payoutSoundPlayed;

	RealTimeSince _phaseElapsed;
	float _phaseDuration;

	public NetList<int> GetHand( int seatIndex, int handIndex )
	{
		return (seatIndex, handIndex) switch
		{
			(0, 0) => Seat0Hand0, (0, 1) => Seat0Hand1,
			(1, 0) => Seat1Hand0, (1, 1) => Seat1Hand1,
			(2, 0) => Seat2Hand0, (2, 1) => Seat2Hand1,
			(3, 0) => Seat3Hand0, (3, 1) => Seat3Hand1,
			(4, 0) => Seat4Hand0, (4, 1) => Seat4Hand1,
			_ => null
		};
	}

	public int GetBet( int seatIndex, int handIndex )
	{
		return (seatIndex, handIndex) switch
		{
			(0, 0) => Seat0Bet0, (0, 1) => Seat0Bet1,
			(1, 0) => Seat1Bet0, (1, 1) => Seat1Bet1,
			(2, 0) => Seat2Bet0, (2, 1) => Seat2Bet1,
			(3, 0) => Seat3Bet0, (3, 1) => Seat3Bet1,
			(4, 0) => Seat4Bet0, (4, 1) => Seat4Bet1,
			_ => 0
		};
	}

	void SetBet( int seatIndex, int handIndex, int amount )
	{
		switch ( (seatIndex, handIndex) )
		{
			case (0, 0): Seat0Bet0 = amount; break;
			case (0, 1): Seat0Bet1 = amount; break;
			case (1, 0): Seat1Bet0 = amount; break;
			case (1, 1): Seat1Bet1 = amount; break;
			case (2, 0): Seat2Bet0 = amount; break;
			case (2, 1): Seat2Bet1 = amount; break;
			case (3, 0): Seat3Bet0 = amount; break;
			case (3, 1): Seat3Bet1 = amount; break;
			case (4, 0): Seat4Bet0 = amount; break;
			case (4, 1): Seat4Bet1 = amount; break;
		}
	}

	public bool GetHasSplit( int seatIndex )
	{
		return seatIndex switch
		{
			0 => Seat0HasSplit, 1 => Seat1HasSplit, 2 => Seat2HasSplit,
			3 => Seat3HasSplit, 4 => Seat4HasSplit, _ => false
		};
	}

	void SetHasSplit( int seatIndex, bool value )
	{
		switch ( seatIndex )
		{
			case 0: Seat0HasSplit = value; break;
			case 1: Seat1HasSplit = value; break;
			case 2: Seat2HasSplit = value; break;
			case 3: Seat3HasSplit = value; break;
			case 4: Seat4HasSplit = value; break;
		}
	}

	public bool GetHandDone( int seatIndex, int handIndex )
	{
		return (seatIndex, handIndex) switch
		{
			(0, 0) => Seat0Hand0Done, (0, 1) => Seat0Hand1Done,
			(1, 0) => Seat1Hand0Done, (1, 1) => Seat1Hand1Done,
			(2, 0) => Seat2Hand0Done, (2, 1) => Seat2Hand1Done,
			(3, 0) => Seat3Hand0Done, (3, 1) => Seat3Hand1Done,
			(4, 0) => Seat4Hand0Done, (4, 1) => Seat4Hand1Done,
			_ => true
		};
	}

	void SetHandDone( int seatIndex, int handIndex, bool value )
	{
		switch ( (seatIndex, handIndex) )
		{
			case (0, 0): Seat0Hand0Done = value; break;
			case (0, 1): Seat0Hand1Done = value; break;
			case (1, 0): Seat1Hand0Done = value; break;
			case (1, 1): Seat1Hand1Done = value; break;
			case (2, 0): Seat2Hand0Done = value; break;
			case (2, 1): Seat2Hand1Done = value; break;
			case (3, 0): Seat3Hand0Done = value; break;
			case (3, 1): Seat3Hand1Done = value; break;
			case (4, 0): Seat4Hand0Done = value; break;
			case (4, 1): Seat4Hand1Done = value; break;
		}
	}

	protected override void OnStart()
	{
		Log.Info( $"[BlackjackTable] OnStart on {(Networking.IsHost ? "HOST" : "CLIENT")}, IsProxy={IsProxy}, Network.Active={Network.Active}, GameObject.Id={GameObject.Id}" );
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost )
			return;

		if ( _phaseDuration > 0f )
			PhaseTimeRemaining = MathF.Max( 0f, _phaseDuration - _phaseElapsed );

		switch ( CurrentPhase )
		{
			case Phase.Waiting: TickWaiting(); break;
			case Phase.Betting: TickBetting(); break;
			case Phase.Dealing: TickDealing(); break;
			case Phase.PlayerTurns: TickPlayerTurns(); break;
			case Phase.DealerTurn: TickDealerTurn(); break;
			case Phase.Payout: TickPayout(); break;
			case Phase.Reset: TickReset(); break;
		}
	}

	void EnterPhase( Phase phase, float duration )
	{
		CurrentPhase = phase;
		_phaseDuration = duration;
		_phaseElapsed = 0f;
		PhaseTimeRemaining = duration;
		Log.Info( $"[Blackjack] -> {phase} ({duration:0.0}s)" );
	}

	bool AnySeatClaimed()
	{
		foreach ( var seat in Seats )
		{
			if ( seat == null ) continue;
			if ( seat.OccupantPlayer.IsValid() ) return true;
		}
		return false;
	}

	void TickWaiting()
	{
		_phaseDuration = 0f;
		PhaseTimeRemaining = 0f;

		if ( AnySeatClaimed() )
			EnterPhase( Phase.Betting, BettingDuration );
	}

	void TickBetting()
	{
		if ( !AnySeatClaimed() )
		{
			EnterPhase( Phase.Waiting, 0f );
			return;
		}

		if ( _phaseElapsed >= _phaseDuration )
			BeginDealing();
	}

	void BeginDealing()
	{
		ShuffleDeck();
		ClearAllHands();
		DealerHoleHidden = true;

		for ( int i = 0; i < Seats.Count; i++ )
		{
			var seat = Seats[i];
			bool seated = seat != null && seat.OccupantPlayer.IsValid();
			bool placedBet = GetBet( i, 0 ) >= MinBet;
			_seatActiveThisRound[i] = seated && placedBet;
		}

		_dealStep = 0;
		_dealStepTimer = 0f;
		BroadcastSaveSeatedPlayers();
		EnterPhase( Phase.Dealing, 0f );
	}

	void TickDealing()
	{
		_dealStepTimer -= Time.Delta;
		if ( _dealStepTimer > 0f )
			return;

		_dealStepTimer = DealStepDuration;

		if ( _dealStep < 2 )
		{
			for ( int i = 0; i < Seats.Count; i++ )
			{
				if ( !_seatActiveThisRound[i] )
					continue;
				DealCardToSeat( i, 0 );
			}

			DealCardToDealer();
			_dealStep++;

			if ( _dealStep >= 2 )
			{
				ActiveSeatIndex = FindNextActiveSeatHand( -1, 0 );
				ActiveHandIndex = 0;

				if ( ActiveSeatIndex < 0 )
				{
					_dealerStepTimer = DealerSlowDelay;
					EnterPhase( Phase.DealerTurn, 1f );
				}
				else
				{
					CheckAutoStandActiveHand();
					EnterPhase( Phase.PlayerTurns, TurnDuration );
				}
			}
		}
	}

	int FindNextActiveSeatHand( int afterSeat, int currentHand )
	{
		if ( afterSeat >= 0 && currentHand == 0 && GetHasSplit( afterSeat ) )
		{
			ActiveHandIndex = 1;
			return afterSeat;
		}

		for ( int i = afterSeat + 1; i < Seats.Count; i++ )
		{
			if ( _seatActiveThisRound[i] )
			{
				ActiveHandIndex = 0;
				return i;
			}
		}
		return -1;
	}

	void TickPlayerTurns()
	{
		if ( _actionDelayTimer > 0f )
		{
			_actionDelayTimer -= Time.Delta;
			return;
		}

		if ( ActiveSeatIndex < 0 )
		{
			_dealerStepTimer = DealerSlowDelay;
			EnterPhase( Phase.DealerTurn, 1f );
			return;
		}

		if ( GetHandDone( ActiveSeatIndex, ActiveHandIndex ) )
		{
			AdvanceTurn();
			return;
		}

		if ( _phaseElapsed >= _phaseDuration )
		{
			Log.Info( $"[Blackjack] seat {ActiveSeatIndex} hand {ActiveHandIndex} timed out (auto-stand)" );
			SetHandDone( ActiveSeatIndex, ActiveHandIndex, true );
			AdvanceTurn();
		}
	}

	void CheckAutoStandActiveHand()
	{
		var hand = GetHand( ActiveSeatIndex, ActiveHandIndex );
		if ( hand == null ) return;

		int total = HandTotal( hand );
		if ( total >= 21 )
			SetHandDone( ActiveSeatIndex, ActiveHandIndex, true );
	}

	void AdvanceTurn()
	{
		int nextSeat = FindNextActiveSeatHand( ActiveSeatIndex, ActiveHandIndex );

		if ( nextSeat < 0 )
		{
			ActiveSeatIndex = -1;
			ActiveHandIndex = 0;
			_dealerStepTimer = DealerSlowDelay;
			EnterPhase( Phase.DealerTurn, 1f );
			return;
		}

		ActiveSeatIndex = nextSeat;
		_phaseElapsed = 0f;
		_phaseDuration = TurnDuration;
		PhaseTimeRemaining = TurnDuration;

		CheckAutoStandActiveHand();
		Log.Info( $"[Blackjack] -> seat {ActiveSeatIndex} hand {ActiveHandIndex} turn" );
	}

	void TickDealerTurn()
	{
		_dealerStepTimer -= Time.Delta;
		if ( _dealerStepTimer > 0f )
			return;

		if ( DealerHoleHidden )
		{
			DealerHoleHidden = false;
			_dealerStepTimer = DealerSlowDelay;
			return;
		}

		int total = HandTotal( DealerHand );
		if ( total < 17 )
		{
			var pos = DealerHandAnchor != null ? DealerHandAnchor.WorldPosition : WorldPosition;
			DrawTo( DealerHand, pos );
			_dealerStepTimer = DealerSlowDelay;
			return;
		}

		ResolvePayouts();
		BroadcastSaveSeatedPlayers();
		_payoutSoundPlayed = false;
		EnterPhase( Phase.Payout, PayoutDuration );
	}

	void ResolvePayouts()
	{
		int dealerTotal = HandTotal( DealerHand );
		bool dealerBust = dealerTotal > 21;
		bool dealerBlackjack = dealerTotal == 21 && DealerHand.Count == 2;

		var summary = new System.Text.StringBuilder();

		for ( int i = 0; i < _seatWonThisRound.Count; i++ ) _seatWonThisRound[i] = false;

		for ( int seatIndex = 0; seatIndex < Seats.Count; seatIndex++ )
		{
			if ( !_seatActiveThisRound[seatIndex] ) continue;

			var seat = Seats[seatIndex];
			var player = seat?.OccupantPlayer;
			var inventory = player?.Components.Get<Inventory>();

			int handsToScore = GetHasSplit( seatIndex ) ? 2 : 1;
			int seatNet = 0;

			for ( int handIndex = 0; handIndex < handsToScore; handIndex++ )
			{
				var hand = GetHand( seatIndex, handIndex );
				int bet = GetBet( seatIndex, handIndex );
				int playerTotal = HandTotal( hand );
				bool playerBlackjack = playerTotal == 21 && hand.Count == 2 && !GetHasSplit( seatIndex );

				int payout = 0;
				string outcome;

				if ( playerTotal > 21 )
				{
					outcome = "bust";
					payout = 0;
				}
				else if ( playerBlackjack && !dealerBlackjack )
				{
					outcome = "blackjack";
					payout = bet + (bet * 3 / 2);
				}
				else if ( dealerBust )
				{
					outcome = "win";
					payout = bet * 2;
				}
				else if ( playerTotal > dealerTotal )
				{
					outcome = "win";
					payout = bet * 2;
				}
				else if ( playerTotal < dealerTotal )
				{
					outcome = "lose";
					payout = 0;
				}
				else
				{
					outcome = "push";
					payout = bet;
				}

				if ( payout > 0 && player.IsValid() )
					RpcAddPlayerGold( player.Id, payout );

				int net = payout - bet;
				seatNet += net;

				Log.Info( $"[Blackjack] seat {seatIndex} hand {handIndex}: {playerTotal} vs {dealerTotal} -> {outcome} (bet {bet}, payout {payout}, net {net:+#;-#;0})" );
			}

			if ( seatNet > 0 )
				_seatWonThisRound[seatIndex] = true;

			if ( player.IsValid() )
				summary.AppendLine( $"{player.Name}: {(seatNet >= 0 ? "+" : "")}{seatNet}g" );
		}

		LastResultText = summary.ToString();
	}

	void TickPayout()
	{
		if ( !_payoutSoundPlayed && _phaseElapsed >= 1f )
		{
			_payoutSoundPlayed = true;
			RpcPlayWinSoundForWinners( CollectWinningPlayerIds() );
		}

		if ( _phaseElapsed >= _phaseDuration )
		{
			SoundLibrary.PlayCardShuffle( WorldPosition );
			EnterPhase( Phase.Reset, ResetDuration );
		}
	}

	Guid[] CollectWinningPlayerIds()
	{
		var ids = new List<Guid>();
		for ( int i = 0; i < Seats.Count; i++ )
		{
			if ( !_seatWonThisRound[i] ) continue;
			var seat = Seats[i];
			var occ = seat?.OccupantPlayer;
			if ( !occ.IsValid() ) continue;
			ids.Add( occ.Id );
		}
		return ids.ToArray();
	}

	[Rpc.Broadcast]
	void RpcPlayWinSoundForWinners( Guid[] winnerIds )
	{
		var localPlayer = PlayerHelper.GetLocalPlayer();
		if ( localPlayer == null ) return;

		foreach ( var id in winnerIds )
		{
			if ( localPlayer.Id == id )
			{
				SoundLibrary.PlaySellBuy();
				break;
			}
		}
	}

	void TickReset()
	{
		if ( _phaseElapsed >= _phaseDuration )
		{
			ClearAllHands();
			ClearAllBets();
			DealerHoleHidden = true;
			LastResultText = "";

			if ( AnySeatClaimed() )
				EnterPhase( Phase.Betting, BettingDuration );
			else
				EnterPhase( Phase.Waiting, 0f );
		}
	}

	void ShuffleDeck()
	{
		_deck.Clear();
		for ( int s = 0; s < 4; s++ )
		{
			for ( int r = 1; r <= 13; r++ )
				_deck.Add( new Card( (Suit)s, r ).Encode() );
		}

		var rng = new Random();
		for ( int i = _deck.Count - 1; i > 0; i-- )
		{
			int j = rng.Next( i + 1 );
			(_deck[i], _deck[j]) = (_deck[j], _deck[i]);
		}

		_deckIndex = 0;
	}

	int DrawNext()
	{
		if ( _deckIndex >= _deck.Count )
			ShuffleDeck();

		int code = _deck[_deckIndex];
		_deckIndex++;
		return code;
	}

	void DrawTo( NetList<int> hand, Vector3 soundPosition )
	{
		hand.Add( DrawNext() );
		SoundLibrary.PlayCardDealt( soundPosition );
	}

	void DealCardToSeat( int seatIndex, int handIndex )
	{
		var hand = GetHand( seatIndex, handIndex );
		if ( hand == null ) return;
		var seat = Seats[seatIndex];
		var anchor = seat?.HandAnchor;
		var pos = anchor != null ? anchor.WorldPosition : WorldPosition;
		DrawTo( hand, pos );
	}

	void DealCardToDealer()
	{
		var pos = DealerHandAnchor != null ? DealerHandAnchor.WorldPosition : WorldPosition;
		DrawTo( DealerHand, pos );
	}

	Vector3 GetSeatHandPosition( int seatIndex )
	{
		if ( seatIndex < 0 || seatIndex >= Seats.Count ) return WorldPosition;
		var seat = Seats[seatIndex];
		var anchor = seat?.HandAnchor;
		return anchor != null ? anchor.WorldPosition : WorldPosition;
	}

	void ClearAllHands()
	{
		Seat0Hand0.Clear(); Seat0Hand1.Clear();
		Seat1Hand0.Clear(); Seat1Hand1.Clear();
		Seat2Hand0.Clear(); Seat2Hand1.Clear();
		Seat3Hand0.Clear(); Seat3Hand1.Clear();
		Seat4Hand0.Clear(); Seat4Hand1.Clear();
		DealerHand.Clear();

		for ( int i = 0; i < 5; i++ )
		{
			SetHasSplit( i, false );
			SetHandDone( i, 0, false );
			SetHandDone( i, 1, false );
		}
	}

	void ClearAllBets()
	{
		Seat0Bet0 = 0; Seat0Bet1 = 0;
		Seat1Bet0 = 0; Seat1Bet1 = 0;
		Seat2Bet0 = 0; Seat2Bet1 = 0;
		Seat3Bet0 = 0; Seat3Bet1 = 0;
		Seat4Bet0 = 0; Seat4Bet1 = 0;
	}

	public static int HandTotal( NetList<int> hand )
	{
		if ( hand == null ) return 0;

		int total = 0;
		int aces = 0;

		foreach ( var code in hand )
		{
			var card = Card.Decode( code );
			total += card.BlackjackValue;
			if ( card.IsAce ) aces++;
		}

		while ( total > 21 && aces > 0 )
		{
			total -= 10;
			aces--;
		}

		return total;
	}

	public bool TryClaimSeat( BlackjackSeat seat, GameObject player )
	{
		if ( seat == null || !player.IsValid() ) return false;

		foreach ( var s in Seats )
		{
			if ( s != null && s.OccupantPlayer == player ) return false;
		}

		if ( seat.OccupantPlayer.IsValid() ) return false;

		seat.OccupantPlayer = player;
		Log.Info( $"[Blackjack] {player.Name} claimed seat {seat.SeatIndex}" );
		return true;
	}

	public void ReleaseSeat( BlackjackSeat seat )
	{
		if ( seat == null ) return;

		if ( seat.OccupantPlayer.IsValid() )
		{
			int seatIdx = seat.SeatIndex;
			int totalBet = GetBet( seatIdx, 0 ) + GetBet( seatIdx, 1 );
			if ( totalBet > 0 )
				Log.Info( $"[Blackjack] {seat.OccupantPlayer.Name} forfeited {totalBet}g leaving seat {seatIdx}" );

			Log.Info( $"[Blackjack] {seat.OccupantPlayer.Name} left seat {seatIdx}" );
		}

		seat.OccupantPlayer = null;
	}

	[Rpc.Host]
	public void RpcRequestClaimSeat( int seatIndex, GameObject player )
	{
		if ( seatIndex < 0 || seatIndex >= Seats.Count ) return;
		var seat = Seats[seatIndex];
		if ( seat == null ) return;
		TryClaimSeat( seat, player );
	}

	[Rpc.Host]
	public void RpcRequestReleaseSeat( int seatIndex )
	{
		if ( seatIndex < 0 || seatIndex >= Seats.Count ) return;
		var seat = Seats[seatIndex];
		if ( seat == null ) return;
		ReleaseSeat( seat );
	}

	[Rpc.Broadcast]
	public void RpcPlaceBet( int seatIndex, int amount )
	{
		if ( !Networking.IsHost ) return;

		Log.Info( $"[Blackjack] RpcPlaceBet entered: seat={seatIndex}, amount={amount}, phase={CurrentPhase}" );

		if ( CurrentPhase != Phase.Betting ) { Log.Info( "  -> wrong phase" ); return; }
		if ( seatIndex < 0 || seatIndex >= Seats.Count ) { Log.Info( "  -> bad seat index" ); return; }
		if ( amount < 0 ) { Log.Info( "  -> negative amount" ); return; }

		var seat = Seats[seatIndex];
		if ( seat == null ) { Log.Info( "  -> seat null" ); return; }

		var occ = seat.OccupantPlayer;
		if ( !occ.IsValid() ) { Log.Info( "  -> occupant invalid" ); return; }

		SetBet( seatIndex, 0, amount );
		Log.Info( $"[Blackjack] seat {seatIndex} bet set to {amount}g" );
	}

	[Rpc.Broadcast]
	public void RpcHit( int seatIndex )
	{
		if ( !Networking.IsHost ) return;
		if ( !ValidateActionCaller( seatIndex ) ) return;
		if ( seatIndex != ActiveSeatIndex ) return;

		var hand = GetHand( ActiveSeatIndex, ActiveHandIndex );
		if ( hand == null ) return;
		if ( GetHandDone( ActiveSeatIndex, ActiveHandIndex ) ) return;

		DrawTo( hand, GetSeatHandPosition( ActiveSeatIndex ) );
		_actionDelayTimer = ActionDelay;

		int total = HandTotal( hand );
		if ( total >= 21 )
		{
			SetHandDone( ActiveSeatIndex, ActiveHandIndex, true );
		}
	}

	[Rpc.Broadcast]
	public void RpcStand( int seatIndex )
	{
		if ( !Networking.IsHost ) return;
		if ( !ValidateActionCaller( seatIndex ) ) return;
		if ( seatIndex != ActiveSeatIndex ) return;
		if ( GetHandDone( ActiveSeatIndex, ActiveHandIndex ) ) return;

		SetHandDone( ActiveSeatIndex, ActiveHandIndex, true );
		AdvanceTurn();
	}

	[Rpc.Broadcast]
	public void RpcDouble( int seatIndex )
	{
		if ( !Networking.IsHost ) return;
		if ( !ValidateActionCaller( seatIndex ) ) return;
		if ( seatIndex != ActiveSeatIndex ) return;

		var hand = GetHand( ActiveSeatIndex, ActiveHandIndex );
		if ( hand == null || hand.Count != 2 ) return;
		if ( GetHandDone( ActiveSeatIndex, ActiveHandIndex ) ) return;

		int currentBet = GetBet( seatIndex, ActiveHandIndex );
		SetBet( seatIndex, ActiveHandIndex, currentBet * 2 );

		DrawTo( hand, GetSeatHandPosition( ActiveSeatIndex ) );
		_actionDelayTimer = ActionDelay;
		SetHandDone( ActiveSeatIndex, ActiveHandIndex, true );
	}

	[Rpc.Broadcast]
	public void RpcSplit( int seatIndex )
	{
		if ( !Networking.IsHost ) return;
		if ( !ValidateActionCaller( seatIndex ) ) return;
		if ( seatIndex != ActiveSeatIndex ) return;
		if ( ActiveHandIndex != 0 ) return;
		if ( GetHasSplit( seatIndex ) ) return;

		var hand = GetHand( seatIndex, 0 );
		if ( hand == null || hand.Count != 2 ) return;

		var c1 = Card.Decode( hand[0] );
		var c2 = Card.Decode( hand[1] );
		if ( c1.Rank != c2.Rank ) return;

		int currentBet = GetBet( seatIndex, 0 );
		SetBet( seatIndex, 1, currentBet );

		var hand1 = GetHand( seatIndex, 1 );
		hand1.Clear();
		hand1.Add( hand[1] );
		hand.RemoveAt( 1 );

		SetHasSplit( seatIndex, true );

		var pos = GetSeatHandPosition( seatIndex );
		DrawTo( hand, pos );
		DrawTo( hand1, pos );
	}

	void BroadcastSaveSeatedPlayers()
	{
		var ids = new List<Guid>();
		foreach ( var seat in Seats )
		{
			if ( seat == null ) continue;
			var occ = seat.OccupantPlayer;
			if ( !occ.IsValid() ) continue;
			ids.Add( occ.Id );
		}
		if ( ids.Count == 0 ) return;

		RpcRequestPlayerSaves( ids.ToArray() );
	}

	[Rpc.Broadcast]
	void RpcRequestPlayerSaves( Guid[] playerIds )
	{
		var localPlayer = PlayerHelper.GetLocalPlayer();
		if ( localPlayer == null ) return;

		foreach ( var id in playerIds )
		{
			if ( localPlayer.Id == id )
			{
				var persistence = localPlayer.Components.Get<PlayerPersistence>();
				if ( persistence != null )
				{
					persistence.RequestSaveNow();
					Log.Info( "[Blackjack] Triggered save for local player" );
				}
				break;
			}
		}
	}

	[Rpc.Broadcast]
	void RpcAddPlayerGold( Guid playerId, int amount )
	{
		var localPlayer = PlayerHelper.GetLocalPlayer();
		if ( localPlayer == null ) return;
		if ( localPlayer.Id != playerId ) return;

		var inventory = localPlayer.Components.Get<Inventory>();
		if ( inventory == null ) return;

		var (placed, banked) = inventory.AddItemOrBank( ItemId.GoldCoin, amount );
		Log.Info( $"[Blackjack] Added {amount}g to local player (placed: {placed}, banked: {banked})" );

		if ( banked > 0 )
			GameLog.Add( $"Inventory full — {banked} gold sent to your bank.", "#c9a84c" );
	}

	bool ValidateActionCaller( int seatIndex )
	{
		if ( seatIndex < 0 || seatIndex >= Seats.Count ) return false;
		var seat = Seats[seatIndex];
		if ( seat == null ) return false;

		var occ = seat.OccupantPlayer;
		if ( !occ.IsValid() ) return false;

		return true;
	}

	void OnLastResultTextChanged( string oldVal, string newVal ) { }
}