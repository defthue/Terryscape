using Sandbox;
using System.Collections.Generic;

public sealed class HeadHider : Component
{
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }
	[Property] public PlayerController PlayerController { get; set; }
	[Property] public CameraComponent PlayerCamera { get; set; }

	[Property, Group( "Camera Offset" )] public Vector3 CameraOffset { get; set; } = new Vector3( 8f, 0f, 0f );

	List<ModelRenderer> _clothingRenderers = new();
	bool _cachedClothing = false;

	protected override void OnUpdate()
	{
		if ( BodyRenderer == null || PlayerController == null )
			return;

		bool isLocal = PlayerHelper.IsLocalPlayer( GameObject );

		if ( !isLocal )
		{
			BodyRenderer.SetBodyGroup( "Head", 0 );
			SetClothingRenderType( ModelRenderer.ShadowRenderType.On );
			return;
		}

		if ( !_cachedClothing )
		{
			CacheClothingRenderers();
			_cachedClothing = true;
		}

		if ( PlayerController.ThirdPerson )
		{
			BodyRenderer.SetBodyGroup( "Head", 0 );
			SetClothingRenderType( ModelRenderer.ShadowRenderType.On );
		}
		else
		{
			BodyRenderer.SetBodyGroup( "Head", 1 );
			SetClothingRenderType( ModelRenderer.ShadowRenderType.ShadowsOnly );

			var sceneModel = BodyRenderer.SceneModel;
			if ( sceneModel != null )
			{
				var headTransform = sceneModel.GetBoneWorldTransform( "head" );

				var cam = PlayerCamera;
				if ( cam == null )
					cam = Scene.Camera;

				if ( cam != null )
				{
					cam.WorldPosition = headTransform.Position + headTransform.Rotation * CameraOffset;
					cam.ZNear = 3f;
				}
			}
		}
	}

	void CacheClothingRenderers()
	{
		_clothingRenderers.Clear();

		var body = BodyRenderer.GameObject;
		foreach ( var child in body.Children )
		{
			var renderer = child.Components.Get<ModelRenderer>();
			if ( renderer != null && renderer != BodyRenderer )
				_clothingRenderers.Add( renderer );
		}
	}

	void SetClothingRenderType( ModelRenderer.ShadowRenderType renderType )
	{
		foreach ( var renderer in _clothingRenderers )
		{
			if ( renderer != null && renderer.IsValid )
				renderer.RenderType = renderType;
		}
	}
}