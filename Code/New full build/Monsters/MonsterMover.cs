using Sandbox;
using System;

public sealed class MonsterMover : Component
{
	[Property, Group( "Agent" )] public float AgentHeight { get; set; } = 64f;
	[Property, Group( "Agent" )] public float AgentRadius { get; set; } = 32f;
	[Property, Group( "Agent" )] public float Acceleration { get; set; } = 600f;
	[Property, Group( "Grounding" )] public float SnapMaxDelta { get; set; } = 40f;

	NavMeshAgent _agent;
	Vector3 _destination;
	bool _hasDestination;
	float _nextReissueTime;

	public Vector3 Velocity => _agent != null && _agent.IsValid() ? _agent.Velocity : Vector3.Zero;

	public float Speed => Velocity.WithZ( 0f ).Length;

	protected override void OnStart()
	{
		if ( !Networking.IsHost )
			return;

		EnsureAgent();
	}

	void EnsureAgent()
	{
		if ( _agent != null && _agent.IsValid() )
			return;

		_agent = Components.GetOrCreate<NavMeshAgent>();
		_agent.Height = AgentHeight;
		_agent.Radius = AgentRadius;
		_agent.Acceleration = Acceleration;
		_agent.UpdateRotation = false;
	}

	public void MoveTo( Vector3 position, float speed )
	{
		if ( !Networking.IsHost )
			return;

		EnsureAgent();
		_agent.MaxSpeed = speed;

		bool newTarget = !_hasDestination || Vector3.DistanceBetween( _destination, position ) > 16f;
		_destination = position;
		_hasDestination = true;

		if ( newTarget )
		{
			_agent.MoveTo( position );
			_nextReissueTime = Time.Now + 0.5f;
		}
	}

	public void Stop()
	{
		if ( !Networking.IsHost )
			return;

		if ( _agent == null || !_agent.IsValid() )
			return;

		_hasDestination = false;
		_agent.Stop();
	}

	public bool HasArrived( float tolerance = 24f )
	{
		if ( !_hasDestination )
			return true;

		return ( _destination - WorldPosition ).WithZ( 0f ).Length <= tolerance;
	}

	public void Teleport( Vector3 position )
	{
		if ( !Networking.IsHost )
			return;

		EnsureAgent();
		_hasDestination = false;
		_agent.Stop();
		WorldPosition = position;
		_agent.SetAgentPosition( position );
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost )
			return;

		ReissueIfStalled();
		SnapToGround();
	}

	void ReissueIfStalled()
	{
		if ( !_hasDestination || _agent == null || !_agent.IsValid() )
			return;

		if ( HasArrived() )
			return;

		if ( Time.Now < _nextReissueTime )
			return;

		_nextReissueTime = Time.Now + 0.5f;
		_agent.MoveTo( _destination );
	}

	void SnapToGround()
	{
		var trace = Scene.Trace
			.Ray( WorldPosition + Vector3.Up * 32f, WorldPosition + Vector3.Down * 96f )
			.WithoutTags( "monster", "boss", "player" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( !trace.Hit )
			return;

		float delta = trace.HitPosition.z - WorldPosition.z;
		float abs = MathF.Abs( delta );

		if ( abs < 0.5f || abs > SnapMaxDelta )
			return;

		WorldPosition = WorldPosition.WithZ( trace.HitPosition.z );
	}
}