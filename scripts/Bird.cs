using Godot;
using System;

public partial class Bird : CharacterBody2D
{
	[Export] public float Speed { get; set; } = 300.0f;
	[Export] public float JumpVelocity { get; set; } = -350.0f;

	[Export] public float MinRotation { get; set; } = -0.5f;
	[Export] public float MaxRotation { get; set; } = 1.5f;
	[Export] public float RotationSpeed { get; set; } = 0.002f;
	[Export] public float CeilingY { get; set; } = 100.0f;

	private Vector2 _startPosition;
	private bool _isFlying = false;
	private bool _isDead = false;
	private bool _isStopped = false;

	private AnimatedSprite2D _animatedSprite;
	private Main _mainScene;

	public override void _Ready()
	{
		_startPosition = Position;
		_mainScene = GetNode<Main>("/root/main");
		_animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		// Disable collisions until game starts
		CollisionMask = 0;

		_animatedSprite?.Play("idle");
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
		velocity += GetGravity() * (float)delta;
		velocity.X = Speed;

		if (Position.Y < CeilingY)
		{
			Position = new Vector2(Position.X, CeilingY);
			velocity.Y = Mathf.Max(velocity.Y, 0);
		}

		Velocity = velocity;
		MoveAndSlide();

		// Rotation based on vertical velocity
		float targetRot = velocity.Y * RotationSpeed;
		Rotation = Mathf.Clamp(targetRot, MinRotation, MaxRotation);

		// Check pipe collisions
		if (IsFlying() && GetSlideCollisionCount() > 0)
		{

		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);
			var collider = collision.GetCollider();

			if (collider is StaticBody2D body && (body.CollisionLayer & 2) != 0)
			{
				OnPipeHit();
				return;
			}
		}
		}
	}

	private void HandleDeathPhysics(double delta)
	{
		Vector2 v = Velocity;
		v += GetGravity() * (float)delta;
		v.X = 0; // no horizontal movement
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

		// Only collide with pipes (layer 2)
		CollisionMask = 2;

		_animatedSprite?.Play("flying");
	}

	public void OnPipeHit()
	{
		if (_isDead)
			return;

		_isFlying = false;
		_isDead = true;

		// Disable pipe collisions, bird will fall to floor naturally
		// Floor.cs will detect collision and call StopFalling() + GameOver()
		CollisionMask = 0;

		Vector2 v = Velocity;
		v.X = 0;
		Velocity = v;

		_animatedSprite?.Play("idle");
	}

	// Public API methods expected by Main and Floor
	public bool IsDead() => _isDead;
	public bool IsFlying() => _isFlying;
	
	public void Die()
	{
		// Already handled by OnPipeHit, this is called by Floor
		// Keep for compatibility but don't duplicate logic
		if (_isDead) return;
		
		_isDead = true;
		_isFlying = false;
		CollisionMask = 0;
	}

	public void StopFalling()
	{
		_isStopped = true;
		Velocity = Vector2.Zero;
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
