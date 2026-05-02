public enum Suit { Clubs, Diamonds, Hearts, Spades }

public struct Card
{
	public Suit Suit;
	public int Rank;

	public Card( Suit suit, int rank )
	{
		Suit = suit;
		Rank = rank;
	}

	public string SuitName => Suit switch
	{
		Suit.Clubs => "clubs",
		Suit.Diamonds => "diamonds",
		Suit.Hearts => "hearts",
		Suit.Spades => "spades",
		_ => "clubs"
	};

	public string RankName => Rank switch
	{
		1 => "A",
		11 => "J",
		12 => "Q",
		13 => "K",
		_ => Rank.ToString()
	};

	public string TexturePath => $"/Cards/{SuitName}_{RankName}.vtex";

	public int BlackjackValue => Rank switch
	{
		1 => 11,
		11 => 10,
		12 => 10,
		13 => 10,
		_ => Rank
	};

	public bool IsAce => Rank == 1;

	public override string ToString() => $"{RankName}{Suit.ToString()[0]}";

	public int Encode() => (int)Suit * 13 + (Rank - 1);

	public static Card Decode( int code )
	{
		int suitIdx = code / 13;
		int rankIdx = code % 13;
		return new Card( (Suit)suitIdx, rankIdx + 1 );
	}
}
