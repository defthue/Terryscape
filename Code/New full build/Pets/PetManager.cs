using Sandbox;
using System;
using System.Collections.Generic;

public sealed class PetManager : Component
{
	public static PetManager Local { get; private set; }

	[Property] public GameObject PetSlimePrefab { get; set; }

	public List<PetKind> Library { get; private set; } = new();
	public PetKind ActiveSlot { get; private set; } = PetKind.Slime;

	GameObject _slime;
	bool _spawned;

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

		DespawnSlime();
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

	void RefreshSlime()
	{
		DespawnSlime();

		if ( ActiveSlot == PetKind.None )
			return;

		if ( PetSlimePrefab == null )
		{
			Log.Warning( "[Pet] No PetSlimePrefab assigned on PetManager." );
			return;
		}

		var go = PetSlimePrefab.Clone( WorldPosition + Vector3.Up * 10f );
		go.Name = $"Pet_{ActiveSlot}";

		var slime = go.Components.Get<PetSlime>();
		if ( slime != null )
		{
			slime.OwnerSteamId = Network.Owner?.SteamId ?? 0ul;
			slime.Kind = ActiveSlot;
		}

		if ( Networking.IsActive )
			go.NetworkSpawn();

		_slime = go;
	}

	void DespawnSlime()
	{
		if ( _slime != null && _slime.IsValid() )
			_slime.Destroy();
		_slime = null;
	}
}
