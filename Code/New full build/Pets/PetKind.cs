using Sandbox;

public enum PetKind
{
	None,
	Slime
}

public struct PetDef
{
	public string Name;
	public string Description;
	public float FollowScale;
	public float MountedScale;
	public float MoveSpeed;
	public float HopImpulse;
	public float JumpImpulse;
	public string IconColor;
}

public static class PetDatabase
{
	public static readonly PetKind[] AllPets = { PetKind.Slime };

	public static PetDef Get( PetKind kind )
	{
		return kind switch
		{
			PetKind.Slime => new PetDef
			{
				Name = "Slime",
				Description = "A wobbly green companion. Walk up to it and press E to ride.",
				FollowScale = 0.55f,
				MountedScale = 1.0f,
				MoveSpeed = 500f,
				HopImpulse = 300f,
				JumpImpulse = 520f,
				IconColor = "#39d94a"
			},
			_ => default
		};
	}

	public static Color SlimeColor( float alpha )
	{
		return new Color( 0.15f, 0.85f, 0.25f, alpha );
	}
}
