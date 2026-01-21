using Godot;

public partial class Floor : Node2D
{
	[Export] public float FloorWidth { get; set; } = 1152.0f;

	private const float DespawnOffset = 500.0f;

	private bool _gameRunning = false;
	private Bird _bird;
	private Main _mainScene;

	private Node2D _segment1;
	private Node2D _segment2;
	private Area2D _area1;
	private Area2D _area2;

	public override void _Ready()
	{
		_segment1 = GetNode<Node2D>("FloorSegment1");
		_segment2 = GetNode<Node2D>("FloorSegment2");

		_area1 = GetNode<Area2D>("FloorSegment1/Area2D");
		_area2 = GetNode<Area2D>("FloorSegment2/Area2D");

		if (_area1 != null)
		{
			_area1.BodyEntered += OnBodyEntered;
		}

		if (_area2 != null)
		{
			_area2.BodyEntered += OnBodyEntered;
		}

		_bird = GetNode<Bird>("/root/main/Bird");
		_mainScene = GetNode<Main>("/root/main");
	}

	public override void _ExitTree()
	{
		if (_area1 != null && IsInstanceValid(_area1))
		{
			_area1.BodyEntered -= OnBodyEntered;
		}

		if (_area2 != null && IsInstanceValid(_area2))
		{
			_area2.BodyEntered -= OnBodyEntered;
		}

		// Clear references to allow garbage collection
		_segment1 = null;
		_segment2 = null;
		_area1 = null;
		_area2 = null;
		_bird = null;
		_mainScene = null;

		base._ExitTree();
	}

	public override void _Process(double delta)
	{
		if (!_gameRunning || _bird == null || _segment1 == null || _segment2 == null)
			return;

		float birdX = _bird.GlobalPosition.X;
		float globalX = GlobalPosition.X;

		RecycleSegmentIfNeeded(_segment1, birdX, globalX);
		RecycleSegmentIfNeeded(_segment2, birdX, globalX);
	}

	private void RecycleSegmentIfNeeded(Node2D segment, float birdX, float floorGlobalX)
	{
		float segmentWorldX = floorGlobalX + segment.Position.X;

		// If segment is fully offscreen to the left
		if (segmentWorldX + FloorWidth < birdX - DespawnOffset)
		{
			// Find the rightmost segment position
			float rightmostX = Mathf.Max(_segment1.Position.X, _segment2.Position.X);

			// Move this segment to the right
			segment.Position = new Vector2(rightmostX + FloorWidth, segment.Position.Y);
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Bird bird && _mainScene != null)
		{
			bird.StopFalling();
			_mainScene.GameOver();
		}
	}

	public void StartScrolling()
	{
		_gameRunning = true;
	}

	public void StopScrolling()
	{
		_gameRunning = false;
	}

	public void Reset()
	{
		_gameRunning = false;

		if (_segment1 != null)
		{
			_segment1.Position = new Vector2(0, 0);
		}

		if (_segment2 != null)
		{
			_segment2.Position = new Vector2(FloorWidth, 0);
		}
	}
}
