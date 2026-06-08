using Sandbox;
using System;
using System.Collections.Generic;

[AssetType( Name = "Loot Table", Extension = "loot" )]
public class LootTable : GameResource
{
	[Property, Group( "Gold" )] public int GoldMin { get; set; } = 0;
	[Property, Group( "Gold" )] public int GoldMax { get; set; } = 0;

	[Property, Group( "Items" )] public List<LootEntry> Entries { get; set; } = new();

	public int RollGoldPool( Random rng )
	{
		if ( GoldMax <= 0 && GoldMin <= 0 )
			return 0;

		int lo = Math.Min( GoldMin, GoldMax );
		int hi = Math.Max( GoldMin, GoldMax );
		if ( hi <= lo )
			return lo < 0 ? 0 : lo;

		return rng.Next( lo, hi + 1 );
	}

	public int RollEntryAmount( Random rng, LootEntry entry )
	{
		if ( entry == null )
			return 0;

		int lo = Math.Min( entry.MinAmount, entry.MaxAmount );
		int hi = Math.Max( entry.MinAmount, entry.MaxAmount );
		if ( lo < 1 ) lo = 1;
		if ( hi < lo ) hi = lo;

		return rng.Next( lo, hi + 1 );
	}
}
