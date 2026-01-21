using Godot;

public partial class Bird : CharacterBody2D
{
	private const uint PipeCollisionLayer = 2;
	private const float CameraOffsetX = 100.0f;

	[Export] public float Speed { get; set; } = 200.0f;
	[Export] public float JumpVelocity { get; set; } = -400.0f;
	[Export] public float Gravity { get; set; } = 1000.0f;
	[Export] public float MaxFallSpeed { get; set; } = 800.0f;

	[Export] public float MinRotation { get; set; } = -0.5f;
	[Export] public float MaxRotation { get; set; } = 1.5f;
	[Export] public float RotationScale { get; set; } = 0.002f;
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

		if (_camera != null)
		{
			_camera.Offset = new Vector2(CameraOffsetX, 0);
		}

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

		// Apply gravity and cap fall speed
		velocity.Y += Gravity * (float)delta;
		velocity.Y = CapFallSpeed(velocity.Y);
		velocity.X = Speed;

		// Prevent bird from flying too high
		if (Position.Y < CeilingY)
		{
			Position = new Vector2(Position.X, CeilingY);
			velocity.Y = Mathf.Max(velocity.Y, 0);
		}

		// Update velocity
		Velocity = velocity;
		MoveAndSlide();

		// Check for collisions with pipes
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);
			if (collision != null && collision.GetCollider() is StaticBody2D collider)
			{
				OnPipeHit();
				break;
			}
		}

		Rotation = Mathf.Clamp(velocity.Y * RotationScale, MinRotation, MaxRotation);
	}

	private void HandleDeathPhysics(double delta)
	{
		Vector2 v = Velocity;

		v.Y += Gravity * (float)delta;
		v.Y = CapFallSpeed(v.Y);
		v.X = 0;
		Velocity = v;

		MoveAndSlide();
	}

	private float CapFallSpeed(float yVelocity)
	{
		return Mathf.Min(yVelocity, MaxFallSpeed);
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

		// Disable pipe collision to allow bird to fall freely
		CollisionMask = 0;


		// Stop movement, animation, and notify main scene
		Vector2 v = Velocity;
		v.X = 0;
		Velocity = v;

		_animatedSprite?.Play("idle");
		_mainScene?.GameOver();
	}

	public bool IsDead() => _isDead;
	public bool IsFlying() => _isFlying;

	public void StopFalling()
	{
		_isStopped = true;
		Velocity = Vector2.Zero;
		_animatedSprite?.Stop();
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
