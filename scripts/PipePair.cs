using Godot;
using System;

public partial class PipePair : Node2D
{
	[Export] public float GapSize { get; set; } = 200.0f;

	private StaticBody2D _topPipe;
	private StaticBody2D _bottomPipe;
	private Area2D _scoreArea;
	private bool _hasScored = false;
	private Main _mainScene;

	private float _lastGapSize;
	private bool _isInitialized = false;

	public void SetGapSize(float gapSize)
	{
		GapSize = gapSize;
	}

	public void Reconfigure(float gapSize)
	{
		GapSize = gapSize;
		_hasScored = false;
		UpdatePipeConfiguration();
	}

	public bool HasScored() => _hasScored;

	public void MarkAsScored() => _hasScored = true;

	public override void _Ready()
	{
		_topPipe = GetNode<StaticBody2D>("TopPipe");
		_bottomPipe = GetNode<StaticBody2D>("BottomPipe");
		_scoreArea = GetNode<Area2D>("ScoreArea");
		_mainScene = GetNode<Main>("/root/main");

		UpdatePipeConfiguration();
		_isInitialized = true;
		_lastGapSize = GapSize;

		_scoreArea.BodyEntered += OnScoreAreaEntered;
	}

	public override void _Process(double delta)
	{
		// Disable _Process - not needed at runtime
		SetProcess(false);
	}

	private void UpdatePipeConfiguration()
	{
		if (_topPipe == null || _bottomPipe == null || _scoreArea == null)
			return;

		// Position the pipes based on gap size
		_topPipe.Position = new Vector2(0, -GapSize / 2);
		_bottomPipe.Position = new Vector2(0, GapSize / 2);

		// Update score area height to match gap
		var scoreCollision = _scoreArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (scoreCollision?.Shape is RectangleShape2D scoreShape)
		{
			scoreShape.Size = new Vector2(scoreShape.Size.X, GapSize);
		}
	}

	public override void _ExitTree()
	{
		// Disconnect signals
		if (_scoreArea != null && IsInstanceValid(_scoreArea))
		{
			_scoreArea.BodyEntered -= OnScoreAreaEntered;
		}

		// Clear references to allow garbage collection
		_topPipe = null;
		_bottomPipe = null;
		_scoreArea = null;
		_mainScene = null;

		base._ExitTree();
	}

	private void OnScoreAreaEntered(Node2D body)
	{
		if (body is Bird && !_hasScored && _mainScene != null)
		{
			// Only count if game is still running
			if (_mainScene.IsGameRunning())
			{
				_hasScored = true;
				_mainScene.IncrementScore();
			}
		}
	}
}