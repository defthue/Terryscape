using Sandbox;
using System;
using System.Collections.Generic;

public static class DuelHealthPrediction
{
	class PredState { public float Display; public int LastAuth; }

	const float ReconcileSpeed = 14f;

	static Dictionary<ulong, PredState> _pred = new();

	public static void NotifyHit( ulong steamId, int dealt )
	{
		if ( steamId == 0 || dealt <= 0 )
			return;

		if ( _pred.TryGetValue( steamId, out var st ) )
			st.Display = MathF.Max( 0f, st.Display - dealt );
	}

	public static void Tick( ulong steamId, int auth )
	{
		if ( steamId == 0 )
			return;

		if ( !_pred.TryGetValue( steamId, out var st ) )
		{
			_pred[steamId] = new PredState { Display = auth, LastAuth = auth };
			return;
		}

		if ( auth > st.LastAuth )
			st.Display = auth;
		else if ( auth < st.Display )
			st.Display = MathX.Lerp( st.Display, auth, 1f - MathF.Exp( -ReconcileSpeed * Time.Delta ) );

		st.LastAuth = auth;
	}

	public static float GetDisplay( ulong steamId, int fallback )
	{
		if ( steamId != 0 && _pred.TryGetValue( steamId, out var st ) )
			return st.Display;

		return fallback;
	}

	public static void Clear()
	{
		if ( _pred.Count > 0 )
			_pred.Clear();
	}
}
