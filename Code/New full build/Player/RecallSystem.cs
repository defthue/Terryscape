using Sandbox;
using System;
using System.Linq;

public sealed class RecallSystem : Component
{
	public static float CooldownRemaining;
	public static bool IsChanneling;
	public static float ChannelRemaining;

	const float Cooldown = 30f;
	const float ChannelDuration = 3f;
	const float MoveCancelThreshold = 5f;

	static readonly Color ArcanePurple = new Color( 0.62f, 0.30f, 1f );

	Vector3 _channelStartPos;

	GameObject _lightObject;
	PointLight _light;

	bool _respawnPending;
	Vector3 _respawnPos;
	float _respawnPendingTime;

	struct Mote
	{
		public Vector3 Position;
		public Vector3 Velocity;
		public float Life;
		public float MaxLife;
		public float Size;
	}

	Mote[] _burst = new Mote[128];
	int _burstCount;

	public void StartRecall()
	{
		if ( CooldownRemaining > 0f )
		{
			GameLog.Add( $"Recall ready in {MathF.Ceiling( CooldownRemaining )}s.", "#c86464" );
			return;
		}

		if ( IsChanneling )
			return;

		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
			return;

		var health = player.Components.Get<PlayerHealth>();
		if ( health != null && health.IsDead )
			return;

		if ( IsBlockedByArena( player ) )
		{
			GameLog.Add( "You can't recall from the arena.", "#c86464" );
			return;
		}

		_channelStartPos = player.WorldPosition;
		IsChanneling = true;
		ChannelRemaining = ChannelDuration;
		CreateLight();
		GameLog.Add( "Recalling to spawn…", "#a080d0" );
	}

	public void CancelChannel()
	{
		if ( !IsChanneling )
			return;

		IsChanneling = false;
		ChannelRemaining = 0f;
		DestroyLight();
		GameLog.Add( "Recall cancelled.", "#c86464" );
	}

	bool IsBlockedByArena( GameObject player )
	{
		var pvp = player.Components.Get<PvpState>();
		if ( pvp != null && pvp.InArena )
			return true;

		var dm = DuelManager.Instance;
		if ( dm != null && dm.MatchActive && dm.IsDuelist( player ) )
			return true;

		return false;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( CooldownRemaining > 0f )
		{
			CooldownRemaining -= Time.Delta;
			if ( CooldownRemaining < 0f )
				CooldownRemaining = 0f;
		}

		TickRespawnHold();

		if ( IsChanneling )
			TickChannel();

		UpdateBurst( Time.Delta );
		RenderBurst();
	}

	void TickChannel()
	{
		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
		{
			CancelChannel();
			return;
		}

		var health = player.Components.Get<PlayerHealth>();
		bool dead = health != null && health.IsDead;
		bool moved = Vector3.DistanceBetween( player.WorldPosition, _channelStartPos ) > MoveCancelThreshold;

		if ( dead || moved || IsBlockedByArena( player ) )
		{
			CancelChannel();
			return;
		}

		ChannelRemaining -= Time.Delta;

		float progress = 1f - MathX.Clamp( ChannelRemaining / ChannelDuration, 0f, 1f );

		if ( _light != null && _light.IsValid() )
		{
			float pulse = 1f + MathF.Sin( Time.Now * 12f ) * 0.25f;
			_light.Radius = ( 90f + progress * 90f ) * pulse;
		}

		RenderChannel( player.WorldPosition, MathF.Max( ChannelRemaining, 0f ), progress );

		if ( ChannelRemaining <= 0f )
			CompleteRecall( player );
	}

	void CompleteRecall( GameObject player )
	{
		Vector3 fromPos = player.WorldPosition;

		LeaveOccupiedChairs( player );

		Vector3 spawn = ResolveSpawnPosition( player );
		player.WorldPosition = spawn;
		_respawnPos = spawn;
		_respawnPending = true;
		_respawnPendingTime = 0f;

		IsChanneling = false;
		ChannelRemaining = 0f;
		CooldownRemaining = Cooldown;

		SpawnBurst( fromPos );
		DestroyLight();

		SoundLibrary.PlayTeleport( fromPos );
		SoundLibrary.PlayTeleport( spawn );
		AchievementTracker.OnWarp();
		GameLog.Add( "Recalled to spawn.", "#a080d0" );
	}

	Vector3 ResolveSpawnPosition( GameObject player )
	{
		var gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		if ( gm != null )
			return gm.SpawnPoint;

		return player.WorldPosition;
	}

	void LeaveOccupiedChairs( GameObject player )
	{
		var pc = player.Components.Get<PlayerController>();
		if ( pc == null )
			return;

		foreach ( var chair in Scene.GetAllComponents<BaseChair>() )
		{
			if ( chair == null || !chair.IsOccupied )
				continue;

			if ( chair.GetOccupant() == pc )
				chair.AskToLeave( pc );
		}
	}

	bool IsSeatedInChair( GameObject player )
	{
		var pc = player.Components.Get<PlayerController>();
		if ( pc == null )
			return false;

		foreach ( var chair in Scene.GetAllComponents<BaseChair>() )
		{
			if ( chair != null && chair.IsOccupied && chair.GetOccupant() == pc )
				return true;
		}

		return false;
	}

	void TickRespawnHold()
	{
		if ( !_respawnPending )
			return;

		var player = PlayerHelper.GetLocalPlayer();
		if ( player == null )
		{
			_respawnPending = false;
			return;
		}

		_respawnPendingTime += Time.Delta;
		if ( !IsSeatedInChair( player ) || _respawnPendingTime > 1f )
		{
			player.WorldPosition = _respawnPos;
			_respawnPending = false;
		}
	}

	void CreateLight()
	{
		if ( _lightObject != null && _lightObject.IsValid() )
			return;

		_lightObject = new GameObject( true, "RecallLight" );
		_lightObject.SetParent( GameObject );
		_lightObject.LocalPosition = Vector3.Up * 40f;

		_light = _lightObject.Components.Create<PointLight>();
		_light.LightColor = ArcanePurple;
		_light.Radius = 150f;
	}

	void DestroyLight()
	{
		if ( _lightObject != null && _lightObject.IsValid() )
			_lightObject.Destroy();

		_lightObject = null;
		_light = null;
	}

	void RenderChannel( Vector3 basePos, float remaining, float progress )
	{
		float intensity = 0.5f + progress * 0.5f;
		int motesPerStrand = 10 + (int)( progress * 8f );
		float baseAngle = Time.Now * 3f;

		for ( int strand = 0; strand < 2; strand++ )
		{
			float phase = strand * MathF.PI;

			for ( int i = 0; i < motesPerStrand; i++ )
			{
				float f = (float)i / ( motesPerStrand - 1 );
				float height = f * 75f;
				float radius = MathX.Lerp( 35f, 15f, f );
				float ang = baseAngle + phase + f * MathF.PI * 3f;

				Vector3 pos = basePos + new Vector3(
					MathF.Cos( ang ) * radius,
					MathF.Sin( ang ) * radius,
					height );

				float size = ( 2f + progress * 1.6f ) * ( 1f - f * 0.3f );
				float alpha = intensity * ( 1f - f * 0.35f );
				SpellGizmo.SoftSphere( pos, size, ArcanePurple.WithAlpha( alpha ) );
			}
		}

		float ringRadius = 40f * MathX.Clamp( remaining / ChannelDuration, 0f, 1f );
		float ringPulse = 0.55f + MathF.Sin( Time.Now * 8f ) * 0.2f;
		SpellGizmo.SoftRing( basePos + Vector3.Up * 2f, ringRadius, 2.2f, ArcanePurple.WithAlpha( ringPulse ), 24 );
	}

	void SpawnBurst( Vector3 origin )
	{
		Vector3 center = origin + Vector3.Up * 35f;

		if ( _burstCount < _burst.Length )
		{
			_burst[_burstCount++] = new Mote
			{
				Position = center,
				Velocity = Vector3.Zero,
				Life = 0.3f,
				MaxLife = 0.3f,
				Size = 22f
			};
		}

		for ( int i = 0; i < 40; i++ )
		{
			if ( _burstCount >= _burst.Length )
				break;

			Vector3 dir = Vector3.Random.Normal;
			float speed = Game.Random.Float( 60f, 190f );

			_burst[_burstCount++] = new Mote
			{
				Position = center + Vector3.Random.Normal * 8f,
				Velocity = dir * speed + Vector3.Up * 40f,
				Life = 0.4f + Game.Random.Float( -0.05f, 0.12f ),
				MaxLife = 0.4f,
				Size = 4f + Game.Random.Float( -1f, 2f )
			};
		}
	}

	void UpdateBurst( float dt )
	{
		for ( int i = _burstCount - 1; i >= 0; i-- )
		{
			_burst[i].Life -= dt;
			if ( _burst[i].Life <= 0f )
			{
				_burst[i] = _burst[--_burstCount];
				continue;
			}

			_burst[i].Position += _burst[i].Velocity * dt;
			_burst[i].Velocity *= ( 1f - dt * 2.5f );
			_burst[i].Size += dt * 6f;
		}
	}

	void RenderBurst()
	{
		for ( int i = 0; i < _burstCount; i++ )
		{
			float t = 1f - ( _burst[i].Life / _burst[i].MaxLife );
			float alpha = ( 1f - t ) * 0.8f;
			var col = new Color( 0.7f + t * 0.25f, 0.4f + t * 0.3f, 1f, alpha );
			SpellGizmo.SoftSphere( _burst[i].Position, _burst[i].Size, col );
		}
	}
}
