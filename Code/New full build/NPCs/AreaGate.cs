using Sandbox;
using System.Linq;

public sealed class AreaGate : Component
{
	[Property] public string RequiredQuestId { get; set; } = "";
	[Property] public string GateName { get; set; } = "Locked Gate";
	[Property] public GameObject PlayerObject { get; set; }
	[Property] public float CheckDistance { get; set; } = 300f;
	[Property] public bool ShowMessage { get; set; } = true;

	bool _isOpen;
	bool _messageShown;

	protected override void OnUpdate()
	{
		if ( _isOpen )
			return;

		if ( string.IsNullOrEmpty( RequiredQuestId ) )
			return;

		var inventory = GetPlayerInventory();
		if ( inventory == null )
			return;

		if ( inventory.IsQuestCompleted( RequiredQuestId ) )
		{
			OpenGate();
			return;
		}

		if ( ShowMessage && !_messageShown && PlayerObject != null )
		{
			var distance = Vector3.DistanceBetween( WorldPosition, PlayerObject.WorldPosition );
			if ( distance <= CheckDistance )
			{
				GameLog.Add( $"{GateName}: You need to complete a quest to pass.", "#c86464" );
				_messageShown = true;
			}
			else if ( _messageShown && distance > CheckDistance * 1.5f )
			{
				_messageShown = false;
			}
		}
	}

	void OpenGate()
	{
		_isOpen = true;

		var collider = Components.Get<Collider>();
		if ( collider != null )
			collider.Enabled = false;

		var renderer = Components.Get<ModelRenderer>();
		if ( renderer != null )
			renderer.Enabled = false;

		if ( ShowMessage )
			GameLog.Add( $"{GateName} has opened!", "#a080d0" );
	}

	Inventory GetPlayerInventory()
	{
		if ( PlayerObject == null )
			return null;

		return PlayerObject.Components.Get<Inventory>();
	}
}
