using Godot;
using System;

public partial class Floor : Node2D
{
	[Export] public float FloorWidth { get; set; } = 1152.0f; 
	
	private bool _gameRunning = false;
	private Bird _bird;
	private Main _mainScene; 

	private Node2D _segment1;
	private Node2D _segment2;
	private Area2D _area1;
	private Area2D _area2;
	
	public override void _Ready()
	{
		// Cache segment references
		_segment1 = GetNode<Node2D>("FloorSegment1");
		_segment2 = GetNode<Node2D>("FloorSegment2");
		
		// Get references to both Area2D nodes for collision detection
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
		
		// Get reference to bird
		_bird = GetNode<Bird>("/root/main/Bird");
		_mainScene = GetNode<Main>("/root/main");
	}
	
	public override void _ExitTree()
	{
		// Disconnect signals to prevent memory leak
		var area1 = GetNodeOrNull<Area2D>("FloorSegment1/Area2D");
		var area2 = GetNodeOrNull<Area2D>("FloorSegment2/Area2D");
		
		if (area1 != null && IsInstanceValid(area1))
		{
			area1.BodyEntered -= OnBodyEntered;
		}
		
		if (area2 != null && IsInstanceValid(area2))
		{
			area2.BodyEntered -= OnBodyEntered;
		}
		
		base._ExitTree();
	}
	
	public override void _Process(double delta)
	{
		if (_gameRunning && _bird != null && _segment1 != null && _segment2 != null)
		{
			// Cache bird position for performance
			float birdX = _bird.GlobalPosition.X;
			
			// Calculate left edge of each segment in world space
			float segment1X = GlobalPosition.X + _segment1.Position.X;
			float segment2X = GlobalPosition.X + _segment2.Position.X;
			
			// Find which segment is currently furthest right
			float rightmostSegmentX = Mathf.Max(_segment1.Position.X, _segment2.Position.X);
			
			// If segment1 has moved too far behind the bird (fully off screen left)
			if (segment1X + FloorWidth < birdX - 500)
			{
				// Move it to the right of the rightmost segment
				_segment1.Position = new Vector2(rightmostSegmentX + FloorWidth, _segment1.Position.Y);
			}
			
			// Recalculate rightmost after segment1 may have moved
			rightmostSegmentX = Mathf.Max(_segment1.Position.X, _segment2.Position.X);
			
			// If segment2 has moved too far behind the bird (fully off screen left)
			if (segment2X + FloorWidth < birdX - 500)
			{
				// Move it to the right of the rightmost segment
				_segment2.Position = new Vector2(rightmostSegmentX + FloorWidth, _segment2.Position.Y);
			}
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		// Check if the bird collided with the floor
		if (body is Bird bird && _mainScene != null)
		{
			// Stop the bird from falling further
			bird.StopFalling();
			
			// Notify Main scene about collision (if not already game over)
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
		
		// Reset floor segments to initial positions using cached references
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
