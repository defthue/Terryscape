using Sandbox;

public sealed class PvpState : Component
{
	[Sync] public bool InArena { get; set; }

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		var arena = PvpArena.Active;
		bool inside = arena != null && arena.Contains( WorldPosition );

		if ( inside != InArena )
			InArena = inside;
	}
}
