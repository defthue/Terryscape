using Sandbox;

public static class SpellVfx
{
	public static Sprite GlowSprite { get; set; }

	public static ModelRenderer CreateOrb( GameObject parent, Color tint, float scale, string model )
	{
		var go = new GameObject( true, "SpellOrb" );
		if ( parent != null )
		{
			go.SetParent( parent );
			go.LocalPosition = Vector3.Zero;
			go.LocalRotation = Rotation.Identity;
		}
		go.LocalScale = Vector3.One * scale;

		var r = go.Components.Create<ModelRenderer>();
		r.Model = Model.Load( model );
		r.Tint = tint;
		return r;
	}

	public static SpriteRenderer CreateSprite( GameObject parent, Sprite sprite, Color color, float size, bool additive = true, bool lit = false )
	{
		var go = new GameObject( true, "SpellSprite" );
		if ( parent != null )
		{
			go.SetParent( parent );
			go.LocalPosition = Vector3.Zero;
		}

		var sr = go.Components.Create<SpriteRenderer>();
		if ( sprite != null )
			sr.Sprite = sprite;
		sr.Color = color;
		sr.Size = new Vector2( size, size );
		sr.Additive = additive;
		return sr;
	}

	public static PointLight CreateLight( GameObject parent, Color color, float radius )
	{
		var go = new GameObject( true, "SpellLight" );
		if ( parent != null )
		{
			go.SetParent( parent );
			go.LocalPosition = Vector3.Zero;
		}

		var l = go.Components.Create<PointLight>();
		l.LightColor = color;
		l.Radius = radius;
		return l;
	}
}
