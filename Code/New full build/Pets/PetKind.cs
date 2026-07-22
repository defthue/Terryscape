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
	public string Lore;
	public string FoundText;
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

	static readonly Color[] SlimeColors = new[]
	{
		new Color( 0.15f, 0.85f, 0.25f ),
		new Color( 0.20f, 0.55f, 0.95f ),
		new Color( 0.65f, 0.35f, 0.90f ),
		new Color( 0.98f, 0.55f, 0.20f ),
		new Color( 0.95f, 0.45f, 0.65f ),
		new Color( 0.20f, 0.80f, 0.80f ),
	};

	public static int SlimeColorCount => SlimeColors.Length;

	public static PetDef Get( PetKind kind )
	{
		return kind switch
		{
			PetKind.Slime => new PetDef
			{
				Name = "Slime",
				Description = "A wobbly companion. Walk up to it and press E to ride.",
				Lore = "Found at the pond's edge and never left. Squishy and loyal",
				FoundText = "Starting companion",
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

	public static Color SlimeColorByIndex( int index, float alpha )
	{
		if ( SlimeColors.Length == 0 )
			return new Color( 0.15f, 0.85f, 0.25f, alpha );

		if ( index < 0 || index >= SlimeColors.Length )
			index = 0;

		var c = SlimeColors[index];
		return new Color( c.r, c.g, c.b, alpha );
	}
}
