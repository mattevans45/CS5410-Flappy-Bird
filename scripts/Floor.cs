using Godot;
using System;

public partial class Floor : Node2D
{
	[Export] public float FloorWidth { get; set; } = 1152.0f; // Width of one floor segment
	private float _scrollSpeed = 0.0f;  // Floor is stationary in world space
	private bool _gameRunning = false;
	private Bird _bird;
	
	// Cache segment references
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
			GD.Print("Floor: Area2D 1 collision connected");
		}
		
		if (_area2 != null)
		{
			_area2.BodyEntered += OnBodyEntered;
			GD.Print("Floor: Area2D 2 collision connected");
		}
		
		// Get reference to bird
		_bird = GetNode<Bird>("/root/main/Bird");
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
			// Calculate world positions of floor segments
			float segment1WorldX = GlobalPosition.X + _segment1.Position.X;
			float segment2WorldX = GlobalPosition.X + _segment2.Position.X;
			float birdX = _bird.GlobalPosition.X;
			
			// If segment1 is too far behind the bird, move it ahead of segment2
			if (segment1WorldX + FloorWidth < birdX - 300)
			{
				_segment1.Position = new Vector2(_segment2.Position.X + FloorWidth, _segment1.Position.Y);
			}
			
			// If segment2 is too far behind the bird, move it ahead of segment1
			if (segment2WorldX + FloorWidth < birdX - 300)
			{
				_segment2.Position = new Vector2(_segment1.Position.X + FloorWidth, _segment2.Position.Y);
			}
		}
	}
	
	private void OnBodyEntered(Node2D body)
	{
		// Check if the bird collided with the floor
		if (body is Bird bird)
		{
			// Stop the bird from falling further
			bird.StopFalling();
			
			// Notify Main scene about collision (if not already game over)
			GetNode<Main>("/root/main").GameOver();
		}
	}
	
	public void StartScrolling()
	{
		_gameRunning = true;
		GD.Print("Floor: Started scrolling");
	}
	
	public void StopScrolling()
	{
		_gameRunning = false;
		GD.Print("Floor: Stopped scrolling");
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
		
		GD.Print("Floor: Reset");
	}
	
	public void SetScrollSpeed(float speed)
	{
		_scrollSpeed = speed;
		GD.Print($"Floor: Scroll speed set to {speed}");
	}
}
