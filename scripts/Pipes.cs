using Godot;
using System;

public partial class Pipes : Node2D
{
	// Constants for spawn/despawn distances
	private const float InitialSpawnDistance = 400.0f;
	private const float DespawnDistance = 800.0f;
	private const float ResetSpawnDistance = 800.0f;
	
	[Export] public PackedScene PipePairScene { get; set; }
	
	[Export] public float PipeSpacing { get; set; } = 600.0f; // Distance between pipe pairs
	[Export] public float GapSize { get; set; } = 200.0f; // Vertical gap between top and bottom pipes
	[Export] public float MinHeight { get; set; } = 120.0f; // Minimum height for bottom of gap
	[Export] public float MaxHeight { get; set; } = 500.0f; // Maximum height for bottom of gap
	[Export] public int InitialPoolSize { get; set; } = 8;
	[Export] public float SpawnLookahead { get; set; } = 1200.0f;
	
	private bool _gameRunning = false;
	private Bird _bird;
	private float _nextPipeX; // X position for next pipe to spawn (initialized in _Ready)
	private System.Collections.Generic.List<Node2D> _activePipes = new System.Collections.Generic.List<Node2D>();
	
	// Object pool for reusing pipe pairs
	private System.Collections.Generic.Queue<Node2D> _pipePool = new System.Collections.Generic.Queue<Node2D>();
	private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();
	
	public override void _Ready()
	{
		// Initialize and randomize the RNG
		_rng.Randomize();
		
		// Pre-instantiate pool of pipes
		for (int i = 0; i < InitialPoolSize; i++)
		{
			var pipe = CreateNewPipePair();
			pipe.Visible = false;
			pipe.ProcessMode = ProcessModeEnum.Disabled;
			_pipePool.Enqueue(pipe);
		}
		
		// Get reference to bird
		_bird = GetNode<Bird>("/root/main/Bird");
		
		// Initialize spawn position relative to bird's starting position
		if (_bird != null)
		{
			_nextPipeX = _bird.GlobalPosition.X + InitialSpawnDistance;
		}
		else
		{
			_nextPipeX = InitialSpawnDistance;
		}
	}
	
	public override void _ExitTree()
	{
		// Clear collections to allow garbage collection
		_activePipes?.Clear();
		_pipePool?.Clear();
		_bird = null;
		
		base._ExitTree();
	}
	
	public override void _Process(double delta)
	{
		if (!_gameRunning || _bird == null)
			return;
		
		// Cache bird position to avoid repeated property access
		float birdX = _bird.GlobalPosition.X;
		
		// Spawn new pipes ahead of the bird
		while (_nextPipeX < birdX + SpawnLookahead)
		{
			SpawnPipePair(_nextPipeX);
			_nextPipeX += PipeSpacing;
		}
		
		// Remove pipes that are far behind the bird (check only when we have pipes)
		if (_activePipes.Count > 0)
		{
			for (int i = _activePipes.Count - 1; i >= 0; i--)
			{
				float pipeX = _activePipes[i].GlobalPosition.X;
				// Despawn when pipe is fully off-screen
				if (pipeX < birdX - DespawnDistance)
				{
					// Return to pool instead of destroying
					ReturnPipeToPool(_activePipes[i]);
					_activePipes.RemoveAt(i);
				}
			}
		}
	}
	
	private Node2D CreateNewPipePair()
	{
		if (PipePairScene == null)
		{
			GD.PushError("Pipes: PipePairScene not assigned in editor!");
			return null;
		}
		
		var pipePair = PipePairScene.Instantiate<Node2D>();
		AddChild(pipePair);
		return pipePair;
	}
	
	private Node2D GetPipeFromPool()
	{
		Node2D pipe;
		
		if (_pipePool.Count > 0)
		{
			// Reuse from pool
			pipe = _pipePool.Dequeue();
		}
		else
		{
			// Create new if pool is empty
			pipe = CreateNewPipePair();
		}
		
		// Activate the pipe
		pipe.Visible = true;
		pipe.ProcessMode = ProcessModeEnum.Inherit;
		
		return pipe;
	}
	
	private void ReturnPipeToPool(Node2D pipe)
	{
		// Deactivate and return to pool (signal stays connected, flag resets in Reconfigure)
		pipe.Visible = false;
		pipe.ProcessMode = ProcessModeEnum.Disabled;
		_pipePool.Enqueue(pipe);
	}
	
	private void SpawnPipePair(float xPosition)
	{
		// Random gap position using cached RNG
		float gapCenter = _rng.RandfRange(MinHeight, MaxHeight);
		
		// Get pipe from pool instead of instantiating
		Node2D pipePair = GetPipeFromPool();
		
		// Set the gap size before adding to scene tree
		if (pipePair is PipePair pp)
		{
			pp.Reconfigure(GapSize);
		}
		
		pipePair.Position = new Vector2(xPosition, gapCenter);
		_activePipes.Add(pipePair);
	}

	
	public void StartSpawning()
	{
		_gameRunning = true;
	}
	
	public void StopSpawning()
	{
		_gameRunning = false;
	}
	
	public void ResetPipes()
	{
		// Return all active pipes to pool instead of destroying
		foreach (var pipe in _activePipes)
		{
			if (pipe != null)
			{
				ReturnPipeToPool(pipe);
			}
		}
		_activePipes.Clear();
		
		// Reset spawn position
		if (_bird != null)
		{
			_nextPipeX = _bird.GlobalPosition.X + ResetSpawnDistance;
		}
		else
		{
			_nextPipeX = ResetSpawnDistance;
		}
	}
}
