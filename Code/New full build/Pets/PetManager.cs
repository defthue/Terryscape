using Sandbox;
using System;
using System.Collections.Generic;

public sealed class PetManager : Component
{
	public static PetManager Local { get; private set; }

	[Property] public GameObject PetSlimePrefab { get; set; }

	public List<PetKind> Library { get; private set; } = new();
	public PetKind ActiveSlot { get; private set; } = PetKind.Slime;

	public GameObject ActiveSlime => _slime != null && _slime.IsValid() ? _slime : null;

	GameObject _slime;
	bool _spawned;
	bool _pendingDespawn;
	bool _suppressedForArena;
	bool _wasInArena;
	int _colorIndex = -1;

	protected override void OnStart()
	{
		if ( !Library.Contains( PetKind.Slime ) )
			Library.Add( PetKind.Slime );

		if ( !IsProxy )
			Local = this;
	}

	protected override void OnDestroy()
	{
		if ( Local == this )
			Local = null;

		ForceDestroySlime();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( !_spawned && Networking.IsActive )
		{
			_spawned = true;
			RefreshSlime();
		}

		UpdateArenaState();
		UpdatePendingDespawn();
	}

	void UpdateArenaState()
	{
		var player = PlayerHelper.GetLocalPlayer();
		var state = player?.Components.Get<PvpState>();
		bool inArena = state != null && state.InArena;

		if ( inArena && !_wasInArena )
		{
			_suppressedForArena = true;
			DespawnSlime();
		}
		else if ( !inArena && _wasInArena )
		{
			_suppressedForArena = false;
			RefreshSlime();
		}

		_wasInArena = inArena;
	}

	void UpdatePendingDespawn()
	{
		if ( !_pendingDespawn )
			return;

		if ( _slime == null || !_slime.IsValid() )
		{
			_pendingDespawn = false;
			return;
		}

		var chair = _slime.Components.Get<BaseChair>();
		if ( chair == null || !chair.IsOccupied )
		{
			_slime.Destroy();
			_slime = null;
			_pendingDespawn = false;
		}
	}

	public void Summon( PetKind kind )
	{
		if ( !Library.Contains( kind ) )
			return;
		ActiveSlot = kind;
		RefreshSlime();
	}

	public void Unsummon()
	{
		ActiveSlot = PetKind.None;
		RefreshSlime();
	}

	public bool IsActive( PetKind kind ) => ActiveSlot == kind;

	public void ReapplyColor()
	{
		var slime = ActiveSlime?.Components.Get<PetSlime>();
		if ( slime != null )
		{
			slime.RefreshColorFromState();
			_colorIndex = slime.ColorIndex;
			return;
		}

		RefreshSlime();
	}

	void RefreshSlime()
	{
		DespawnSlime();

		if ( _pendingDespawn )
			return;

		if ( _suppressedForArena )
			return;

		if ( ActiveSlot == PetKind.None )
			return;

		if ( PetSlimePrefab == null )
		{
			Log.Warning( "[Pet] No PetSlimePrefab assigned on PetManager." );
			return;
		}

		if ( _colorIndex < 0 )
			_colorIndex = Game.Random.Int( 0, PetDatabase.SlimeColorCount - 1 );

		var go = PetSlimePrefab.Clone( WorldPosition + Vector3.Up * 10f );
		go.Name = $"Pet_{ActiveSlot}";

		var slime = go.Components.Get<PetSlime>();
		if ( slime != null )
		{
			slime.OwnerSteamId = Network.Owner?.SteamId ?? 0ul;
			slime.Kind = ActiveSlot;
			slime.ColorIndex = _colorIndex;

			var overrideColor = PetColorState.GetColor();
			if ( overrideColor != null )
			{
				slime.HasOverrideColor = true;
				slime.OverrideColor = overrideColor.Value;
			}
		}

		if ( Networking.IsActive )
			go.NetworkSpawn();

		_slime = go;
	}

	void DespawnSlime()
	{
		if ( _slime == null || !_slime.IsValid() )
		{
			_slime = null;
			_pendingDespawn = false;
			return;
		}

		var chair = _slime.Components.Get<BaseChair>();
		if ( chair != null && chair.IsOccupied )
		{
			var occ = chair.GetOccupant();
			if ( occ != null && occ.IsValid() )
				chair.AskToLeave( occ );

			_pendingDespawn = true;
			return;
		}

		_slime.Destroy();
		_slime = null;
		_pendingDespawn = false;
	}

	void ForceDestroySlime()
	{
		if ( _slime != null && _slime.IsValid() )
			_slime.Destroy();
		_slime = null;
		_pendingDespawn = false;
	}
}
