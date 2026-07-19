using Sandbox;
using System;
using System.Collections.Generic;

public sealed class FishShadows : Component
{
	[Property, Group( "Fish" ), Range( 1, 3 )] public int MaxFish { get; set; } = 2;
	[Property, Group( "Fish" ), Range( 0f, 1f )] public float Activity { get; set; } = 0.5f;
	[Property, Group( "Fish" ), Range( 10f, 60f )] public float FishSize { get; set; } = 25f;
	[Property, Group( "Fish" ), Range( 10f, 100f )] public float SwimSpeed { get; set; } = 35f;
	[Property, Group( "Fish" ), Range( 20f, 150f )] public float DepthBelowSurface { get; set; } = 50f;
	[Property, Group( "Fish" )] public Color FishColor { get; set; } = new Color( 0.05f, 0.12f, 0.20f, 0.55f );

	class Fish
	{
		public GameObject Root;
		public ModelRenderer Body;
		public ModelRenderer Tail;
		public GameObject TailGo;
		public int State;
		public float Timer;
		public float Alpha;
		public Vector2 Position;
		public Vector2 Target;
		public float Heading;
		public float WagPhase;
	}

	List<Fish> _fish = new();
	WaterVolume _water;
	Random _random = new();

	protected override void OnStart()
	{
		_water = Components.Get<WaterVolume>();
	}

	protected override void OnDisabled()
	{
		foreach ( var f in _fish )
		{
			if ( f.Root != null && f.Root.IsValid() )
				f.Root.Destroy();
		}
		_fish.Clear();
	}

	protected override void OnUpdate()
	{
		if ( _water == null || !_water.IsValid() )
			return;

		while ( _fish.Count < MaxFish )
			_fish.Add( CreateFish() );

		while ( _fish.Count > MaxFish )
		{
			var last = _fish[_fish.Count - 1];
			if ( last.Root != null && last.Root.IsValid() )
				last.Root.Destroy();
			_fish.RemoveAt( _fish.Count - 1 );
		}

		foreach ( var f in _fish )
			UpdateFish( f );
	}

	Fish CreateFish()
	{
		var root = new GameObject();
		root.Name = "FishShadow";
		root.Parent = GameObject;
		root.Flags |= GameObjectFlags.NotSaved | GameObjectFlags.Hidden;

		var bodyGo = new GameObject();
		bodyGo.Name = "Body";
		bodyGo.Parent = root;
		bodyGo.LocalPosition = Vector3.Zero;
		var body = bodyGo.Components.Create<ModelRenderer>();
		body.Model = Model.Load( "models/dev/sphere.vmdl" );

		var tailGo = new GameObject();
		tailGo.Name = "Tail";
		tailGo.Parent = root;
		var tail = tailGo.Components.Create<ModelRenderer>();
		tail.Model = Model.Load( "models/dev/sphere.vmdl" );

		var f = new Fish
		{
			Root = root,
			Body = body,
			Tail = tail,
			TailGo = tailGo,
			State = 0,
			Timer = HiddenDuration(),
			Alpha = 0f,
			WagPhase = (float)_random.NextDouble() * 6.28f
		};

		ApplyScale( f );
		ApplyAlpha( f );
		RandomizePath( f );
		return f;
	}

	float HiddenDuration()
	{
		float shortest = 8f;
		float longest = 90f;
		float mean = longest + ( shortest - longest ) * Activity;
		return mean * ( 0.5f + (float)_random.NextDouble() );
	}

	float SwimDuration()
	{
		return 12f + (float)_random.NextDouble() * 14f;
	}

	Vector2 SwimArea()
	{
		float margin = MathF.Max( FishSize * 4f, 150f );
		var half = _water.Size * 0.5f;
		return new Vector2( MathF.Max( half.x - margin, 10f ), MathF.Max( half.y - margin, 10f ) );
	}

	Vector2 RandomPoint()
	{
		var area = SwimArea();
		return new Vector2(
			( (float)_random.NextDouble() * 2f - 1f ) * area.x,
			( (float)_random.NextDouble() * 2f - 1f ) * area.y );
	}

	void RandomizePath( Fish f )
	{
		f.Position = RandomPoint();
		f.Target = RandomPoint();
		f.Heading = MathF.Atan2( f.Target.y - f.Position.y, f.Target.x - f.Position.x );
	}

	void ApplyScale( Fish f )
	{
		float unit = 50f;
		float len = FishSize;
		f.Body.GameObject.LocalScale = new Vector3( len / unit, len * 0.34f / unit, len * 0.26f / unit );
		f.TailGo.LocalScale = new Vector3( len * 0.45f / unit, len * 0.36f / unit, len * 0.14f / unit );
	}

	void ApplyAlpha( Fish f )
	{
		var c = new Color( FishColor.r, FishColor.g, FishColor.b, FishColor.a * f.Alpha );
		if ( f.Body != null && f.Body.IsValid() )
			f.Body.Tint = c;
		if ( f.Tail != null && f.Tail.IsValid() )
			f.Tail.Tint = c;
	}

	void UpdateFish( Fish f )
	{
		if ( f.Root == null || !f.Root.IsValid() )
			return;

		f.Timer -= Time.Delta;

		if ( f.State == 0 )
		{
			if ( f.Timer <= 0f )
			{
				RandomizePath( f );
				ApplyScale( f );
				f.State = 1;
				f.Timer = 1.5f;
			}
			return;
		}

		if ( f.State == 1 )
		{
			f.Alpha = MathF.Min( 1f - f.Timer / 1.5f, 1f );
			if ( f.Timer <= 0f )
			{
				f.State = 2;
				f.Timer = SwimDuration();
				f.Alpha = 1f;
			}
		}
		else if ( f.State == 2 )
		{
			if ( f.Timer <= 0f )
			{
				f.State = 3;
				f.Timer = 1.5f;
			}
		}
		else if ( f.State == 3 )
		{
			f.Alpha = MathF.Max( f.Timer / 1.5f, 0f );
			if ( f.Timer <= 0f )
			{
				f.State = 0;
				f.Timer = HiddenDuration();
				f.Alpha = 0f;
			}
		}

		Swim( f );
		ApplyAlpha( f );
	}

	void Swim( Fish f )
	{
		var toTarget = f.Target - f.Position;
		if ( toTarget.Length < FishSize * 1.5f )
		{
			f.Target = RandomPoint();
			toTarget = f.Target - f.Position;
		}

		float desired = MathF.Atan2( toTarget.y, toTarget.x );
		float diff = desired - f.Heading;
		while ( diff > MathF.PI ) diff -= MathF.PI * 2f;
		while ( diff < -MathF.PI ) diff += MathF.PI * 2f;
		float turnRate = 1.2f;
		f.Heading += Math.Clamp( diff, -turnRate * Time.Delta, turnRate * Time.Delta );

		float speed = SwimSpeed * ( 0.85f + 0.15f * MathF.Sin( f.WagPhase * 0.5f ) );
		f.Position += new Vector2( MathF.Cos( f.Heading ), MathF.Sin( f.Heading ) ) * speed * Time.Delta;

		var area = SwimArea();
		f.Position.x = Math.Clamp( f.Position.x, -area.x, area.x );
		f.Position.y = Math.Clamp( f.Position.y, -area.y, area.y );

		f.WagPhase += Time.Delta * ( 4f + SwimSpeed * 0.06f );

		float bob = MathF.Sin( f.WagPhase * 0.23f ) * 4f;
		f.Root.LocalPosition = new Vector3( f.Position.x, f.Position.y, -DepthBelowSurface + bob );
		f.Root.LocalRotation = Rotation.FromYaw( f.Heading * ( 180f / MathF.PI ) );

		float wag = MathF.Sin( f.WagPhase ) * 28f;
		f.TailGo.LocalPosition = new Vector3( -FishSize * 0.6f, 0f, 0f );
		f.TailGo.LocalRotation = Rotation.FromYaw( wag );
	}
}
