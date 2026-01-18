using Godot;
using System;

[Tool]
public partial class PipePair : Node2D
{
	[Export] public float GapSize { get; set; } = 200.0f;
	[Export] public float PipeHeight { get; set; } = 1000.0f;
	[Export] public Vector2 ScoreAreaPosition { get; set; } = new Vector2(140, 0);
	[Export] public Vector2 ScoreAreaSize { get; set; } = new Vector2(10, 200);
	[Export] public string PipeTexturePath { get; set; } = "res://assets/Tiles/Style 1/PipeStyle1.png";
	[Export] public Vector2 PipeScale { get; set; } = new Vector2(4, 4.44f);
	
	private StaticBody2D _topPipe;
	private StaticBody2D _bottomPipe;
	private Area2D _scoreArea;
	private Texture2D _preloadedTexture; // Store pre-loaded texture
	private bool _hasScored = false; // Track if this pipe has already been scored
	
	// Track last values to detect changes in editor
	private float _lastGapSize;
	private float _lastPipeHeight;
	private Vector2 _lastScoreAreaPosition;
	private bool _isInitialized = false;
	
	// Call this before adding to scene tree to set custom gap size
	public void SetGapSize(float gapSize)
	{
		GapSize = gapSize;
	}
	
	// Call this to set pre-loaded texture and avoid loading in _Ready
	public void SetPreloadedTexture(Texture2D texture)
	{
		_preloadedTexture = texture;
	}
	
	// Call this to reconfigure the pipe when reusing from pool
	public void Reconfigure(float gapSize)
	{
		GapSize = gapSize;
		_hasScored = false; // Reset scoring flag
		
		// Reposition pipes
		if (_topPipe != null && _bottomPipe != null)
		{
			_topPipe.Position = new Vector2(0, -GapSize / 2);
			_bottomPipe.Position = new Vector2(0, GapSize / 2);
		}
		
		// Update score area collision shape
		if (_scoreArea != null)
		{
			var scoreCollision = _scoreArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (scoreCollision?.Shape is RectangleShape2D scoreShape)
			{
				scoreShape.Size = new Vector2(10, GapSize);
			}
			// Position stays at right edge (96)
		}
	}
	
	public bool HasScored() => _hasScored;
	
	public void MarkAsScored() => _hasScored = true;
	
	public override void _Ready()
	{
		// Get references to scene nodes
		_topPipe = GetNode<StaticBody2D>("TopPipe");
		_bottomPipe = GetNode<StaticBody2D>("BottomPipe");
		_scoreArea = GetNode<Area2D>("ScoreArea");
		
		// Initialize the pipes
		UpdatePipeConfiguration();
		_isInitialized = true;
		_lastGapSize = GapSize;
		_lastPipeHeight = PipeHeight;
		_lastScoreAreaPosition = ScoreAreaPosition;
		
		// Connect signals only in game (not in editor)
		if (!Engine.IsEditorHint())
		{
			_scoreArea.BodyEntered += OnScoreAreaEntered;
			
			// Connect hit areas
			var topHitArea = _topPipe.GetNodeOrNull<Area2D>("HitArea");
			var bottomHitArea = _bottomPipe.GetNodeOrNull<Area2D>("HitArea");
			
			if (topHitArea != null)
				topHitArea.BodyEntered += OnPipeHit;
			
			if (bottomHitArea != null)
				bottomHitArea.BodyEntered += OnPipeHit;
		}
	}
	
	public override void _Process(double delta)
	{
		// Only update in editor when values change
		if (Engine.IsEditorHint() && _isInitialized)
		{
			if (_lastGapSize != GapSize || _lastPipeHeight != PipeHeight || _lastScoreAreaPosition != ScoreAreaPosition)
			{
				UpdatePipeConfiguration();
				_lastGapSize = GapSize;
				_lastPipeHeight = PipeHeight;
				_lastScoreAreaPosition = ScoreAreaPosition;
			}
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
		
		// Configure sprites - they need region enabled and proper height
		ConfigureTopPipeSprite();
		ConfigureBottomPipeSprite();
		
		// Update collision shapes to match pipe heights
		UpdateCollisionShape(_topPipe, PipeHeight);
		UpdateCollisionShape(_bottomPipe, PipeHeight);
		
		// Update score area collision shape size and position
		var scoreCollision = _scoreArea.GetNode<CollisionShape2D>("CollisionShape2D");
		if (scoreCollision?.Shape is RectangleShape2D scoreShape)
		{
			scoreShape.Size = ScoreAreaSize;
			scoreCollision.Position = Vector2.Zero; // Relative to ScoreArea
		}
		
		// Position score area
		_scoreArea.Position = ScoreAreaPosition;
	}
	
	public override void _ExitTree()
	{
		// Disconnect signal to prevent memory leak (only if connected in game)
		if (!Engine.IsEditorHint() && _scoreArea != null && IsInstanceValid(_scoreArea))
		{
			_scoreArea.BodyEntered -= OnScoreAreaEntered;
		}
		
		base._ExitTree();
	}
	
	private void OnScoreAreaEntered(Node2D body)
	{
		if (body is Bird && !_hasScored)
		{
			// Only count if game is still running
			var main = GetNode<Main>("/root/main");
			if (main.IsGameRunning())
			{
				_hasScored = true;
				main.IncrementScore();
			}
		}
	}
	
	private void OnPipeHit(Node2D body)
	{
		if (body is Bird bird)
		{
			bird.OnPipeHit();
		}
	}
	
	private void ConfigureTopPipeSprite()
	{
		var sprite = _topPipe.GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite != null)
		{
			// Use pre-loaded texture if available, otherwise load from path
			if (_preloadedTexture != null)
			{
				sprite.Texture = _preloadedTexture;
			}
			else
			{
				var baseTexture = GD.Load<Texture2D>(PipeTexturePath);
				if (baseTexture != null)
				{
					sprite.Texture = baseTexture;
				}
			}
			
			// Top pipe is flipped and not centered
			// It needs to extend UPWARD from the pipe position
			sprite.RegionEnabled = true;
			
			// Use ONLY the green pipe's repeatable section (200 pixels for less stretching)
			sprite.RegionRect = new Rect2(0, 0, 32, 225);
			
			// Adjust scale to stretch this region to fill the pipe height
			sprite.Scale = new Vector2(PipeScale.X, PipeHeight / 225);
			
			// Since it's flipped and not centered, position it to extend upward
			sprite.Position = new Vector2(0, -PipeHeight);
			
			// GD.Print($"TopPipe Sprite - Position: {sprite.Position}, Scale: {sprite.Scale}, FlipV: {sprite.FlipV}, Region: {sprite.RegionRect}");
		}
}
	
private void ConfigureBottomPipeSprite()
{
	var sprite = _bottomPipe.GetNodeOrNull<Sprite2D>("Sprite2D");
	if (sprite != null)
	{
		// Use pre-loaded texture if available, otherwise load from path
		if (_preloadedTexture != null)
		{
			sprite.Texture = _preloadedTexture;
		}
		else
		{
			var baseTexture = GD.Load<Texture2D>(PipeTexturePath);
			if (baseTexture != null)
			{
				sprite.Texture = baseTexture;
			}
		}
		
		// Bottom pipe extends downward from the pipe position
		sprite.RegionEnabled = true;
		
		// Use a larger region to minimize stretching (225 pixels)
		sprite.RegionRect = new Rect2(0, 0, 32, 225);
		
		// Adjust scale - use exported scale values
		sprite.Scale = new Vector2(PipeScale.X, PipeHeight / 225);
		
		// Not centered, so starts at 0,0 and extends downward
		sprite.Position = Vector2.Zero;
		
		// GD.Print($"BottomPipe Sprite - Position: {sprite.Position}, Scale: {sprite.Scale}, Region: {sprite.RegionRect}");
	}
}
	private void UpdateCollisionShape(StaticBody2D pipe, float height)
	{
		var collision = pipe.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collision?.Shape is RectangleShape2D shape)
		{
			// Pipe width: 32 (base texture) * 4 (scale) = 128 pixels
			shape.Size = new Vector2(128, height);
			
			// Position collision at the center of the pipe
			// For top pipe: extends from 0 to -height, so center is at -height/2
			// For bottom pipe: extends from 0 to +height, so center is at +height/2
			if (pipe.Name == "TopPipe")
			{
				collision.Position = new Vector2(64, -height / 2);
			}
			else
			{
				collision.Position = new Vector2(64, height / 2);
			}
			
			// GD.Print($"{pipe.Name} Collision - Position: {collision.Position}, Size: {shape.Size}");
		}
	}
}