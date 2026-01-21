using Godot;
using System;

public partial class Bird : CharacterBody2D
{
	// Constants
	private const uint PipeCollisionLayer = 2;
	private const float CameraOffsetX = 100.0f;
	
	// Movement parameters matching original Flappy Bird
	[Export] public float Speed { get; set; } = 200.0f; // Horizontal speed
	[Export] public float JumpVelocity { get; set; } = -400.0f; // Flap strength
	[Export] public float Gravity { get; set; } = 1000.0f; // Gravity (reduced from 1200)
	[Export] public float MaxFallSpeed { get; set; } = 800.0f; // Terminal velocity

	// Rotation parameters
	[Export] public float MinRotation { get; set; } = -0.5f; // Max upward tilt (radians)
	[Export] public float MaxRotation { get; set; } = 1.5f; // Max downward tilt (radians)
	[Export] public float RotationScale { get; set; } = 0.002f; // Velocity to rotation conversion factor
	[Export] public float CeilingY { get; set; } = 100.0f;

	private Vector2 _startPosition;
	private bool _isFlying = false;
	private bool _isDead = false;
	private bool _isStopped = false;

	private AnimatedSprite2D _animatedSprite;
	private Main _mainScene;
	private Camera2D _camera;

	public override void _Ready()
	{
		_startPosition = Position;
		_mainScene = GetNode<Main>("/root/main");
		_animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		_camera = GetNodeOrNull<Camera2D>("Camera2D");

		// Offset camera to position bird left of center
		if (_camera != null)
		{
			_camera.Offset = new Vector2(CameraOffsetX, 0);
		}

		// Disable collisions until game starts
		CollisionMask = 0;

		_animatedSprite?.Play("idle");
	}
	
	public override void _ExitTree()
	{
		// Clear references to allow garbage collection
		_mainScene = null;
		_animatedSprite = null;
		_camera = null;
		
		base._ExitTree();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isStopped)
			return;

		if (_isDead)
		{
			HandleDeathPhysics(delta);
			return;
		}

		if (!_isFlying)
			return;

		HandleFlyingPhysics(delta);
	}

	private void HandleFlyingPhysics(double delta)
	{
		Vector2 velocity = Velocity;
		
		// Apply custom gravity
		velocity.Y += Gravity * (float)delta;
		
		// Cap fall speed (terminal velocity)
		if (velocity.Y > MaxFallSpeed)
		{
			velocity.Y = MaxFallSpeed;
		}
		
		// Constant horizontal speed
		velocity.X = Speed;

		// Ceiling collision
		if (Position.Y < CeilingY)
		{
			Position = new Vector2(Position.X, CeilingY);
			velocity.Y = Mathf.Max(velocity.Y, 0);
		}

		Velocity = velocity;
		MoveAndSlide();

		// Rotate based on y velocity - simple function with clamping
		Rotation = Mathf.Clamp(velocity.Y * RotationScale, MinRotation, MaxRotation);
	}

	private void HandleDeathPhysics(double delta)
	{
		Vector2 v = Velocity;
		
		// Apply custom gravity
		v.Y += Gravity * (float)delta;
		
		// Cap fall speed
		if (v.Y > MaxFallSpeed)
		{
			v.Y = MaxFallSpeed;
		}
		
		v.X = 0; // No horizontal movement when dead
		Velocity = v;

		MoveAndSlide();
		// Floor.cs will detect collision via Area2D and call StopFalling()
	}

	public void Flap()
	{
		if (_isDead || _isStopped || !_isFlying)
			return;

		Vector2 velocity = Velocity;
		velocity.Y = JumpVelocity;
		Velocity = velocity;
	}

	public void StartFlying()
	{
		_isFlying = true;
		Rotation = 0.0f;

		// Only collide with pipes (layer 2)
		CollisionMask = PipeCollisionLayer;

		_animatedSprite?.Play("flying");
	}

	public void OnPipeHit()
	{
		if (_isDead)
			return;

		_isFlying = false;
		_isDead = true;

		// Disable pipe collisions, bird will fall to floor naturally
		CollisionMask = 0;

		Vector2 v = Velocity;
		v.X = 0;
		Velocity = v;

		_animatedSprite?.Play("idle");
		
		// Trigger game over immediately (plays hit sound and handles game over logic)
		_mainScene?.GameOver();
	}

	// Public API methods
	public bool IsDead() => _isDead;
	public bool IsFlying() => _isFlying;
	
	public void StopFalling()
	{
		_isStopped = true;
		Velocity = Vector2.Zero;
		
		// Stop the animation
		if (_animatedSprite != null)
		{
			_animatedSprite.Stop();
		}
	}

	public void Reset()
	{
		Position = _startPosition;
		Velocity = Vector2.Zero;
		Rotation = 0;

		_isFlying = false;
		_isDead = false;
		_isStopped = false;

		CollisionMask = 0;

		SetPhysicsProcess(true);

		_animatedSprite?.Play("idle");
	}
}
