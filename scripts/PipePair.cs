using Godot;
using System;

[Tool]
public partial class PipePair : Node2D
{
	[Export] public float GapSize { get; set; } = 200.0f;
	
	private StaticBody2D _topPipe;
	private StaticBody2D _bottomPipe;
	private Area2D _scoreArea;
	private bool _hasScored = false; // Track if this pipe has already been scored
	private Main _mainScene; // Cache Main reference
	
	// Track last values to detect changes in editor
	private float _lastGapSize;
	private bool _isInitialized = false;
	
	// Call this before adding to scene tree to set custom gap size
	public void SetGapSize(float gapSize)
	{
		GapSize = gapSize;
	}
	
	// Call this to reconfigure the pipe when reusing from pool
	public void Reconfigure(float gapSize)
	{
		GapSize = gapSize;
		_hasScored = false; // Reset scoring flag
		UpdatePipeConfiguration(); // Reuse existing logic
	}
	
	public bool HasScored() => _hasScored;
	
	public void MarkAsScored() => _hasScored = true;
	
	public override void _Ready()
	{
		// Get references to scene nodes
		_topPipe = GetNode<StaticBody2D>("TopPipe");
		_bottomPipe = GetNode<StaticBody2D>("BottomPipe");
		_scoreArea = GetNode<Area2D>("ScoreArea");
		
		// Cache Main reference to avoid repeated lookups
		if (!Engine.IsEditorHint())
		{
			_mainScene = GetNode<Main>("/root/main");
		}
		
		// Initialize the pipes - just set positions based on gap size
		UpdatePipeConfiguration();
		_isInitialized = true;
		_lastGapSize = GapSize;
		
		// Connect signals only in game (not in editor)
		if (!Engine.IsEditorHint())
		{
			_scoreArea.BodyEntered += OnScoreAreaEntered;
		}
	}
	
	public override void _Process(double delta)
	{
		// Only update in editor when gap size changes
		if (!Engine.IsEditorHint())
		{
			SetProcess(false); // Disable _Process in game - not needed
			return;
		}
		
		if (_isInitialized && _lastGapSize != GapSize)
		{
			UpdatePipeConfiguration();
			_lastGapSize = GapSize;
		}
	}
	
	private void UpdatePipeConfiguration()
	{
		if (_topPipe == null || _bottomPipe == null || _scoreArea == null)
			return;
			
		// Position the pipes based on gap size
		// Top pipe positioned at top edge of gap, bottom pipe at bottom edge
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
		// Disconnect all signals to prevent memory leaks
		if (!Engine.IsEditorHint())
		{
			if (_scoreArea != null && IsInstanceValid(_scoreArea))
			{
				_scoreArea.BodyEntered -= OnScoreAreaEntered;
			}
		}
		
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