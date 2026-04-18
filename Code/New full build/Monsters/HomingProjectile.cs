using Sandbox;
using System;

public sealed class HomingProjectile : Component
{
	[Property] public float TurnSpeed { get; set; } = 5f;
	[Property] public float TargetHeightOffset { get; set; } = 40f;

	public GameObject Target { get; set; }

	protected override void OnUpdate()
	{
		if ( Target == null || !Target.IsValid() )
			return;

		var spellProj = Components.Get<SpellProjectile>();
		if ( spellProj == null )
			return;

		Vector3 currentDir = spellProj.Velocity.Normal;
		Vector3 targetPos = Target.WorldPosition + Vector3.Up * TargetHeightOffset;
		Vector3 desiredDir = ( targetPos - WorldPosition ).Normal;

		Vector3 newDir = Vector3.Lerp( currentDir, desiredDir, TurnSpeed * Time.Delta ).Normal;
		float speed = spellProj.Velocity.Length;
		spellProj.Velocity = newDir * speed;
	}
}