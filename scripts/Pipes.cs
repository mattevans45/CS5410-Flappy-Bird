using Godot;
using System.Collections.Generic;

public partial class Pipes : Node2D
{
	private const float InitialSpawnDistance = 400.0f;
	private const float DespawnDistance = 800.0f;
	private const float ResetSpawnDistance = 800.0f;

	[Export] public PackedScene PipePairScene { get; set; }

	[Export] public float PipeSpacing { get; set; } = 600.0f;
	[Export] public float GapSize { get; set; } = 200.0f;
	[Export] public float MinHeight { get; set; } = 120.0f;
	[Export] public float MaxHeight { get; set; } = 500.0f;
	[Export] public int InitialPoolSize { get; set; } = 8;
	[Export] public float SpawnLookahead { get; set; } = 1200.0f;

	private bool _gameRunning = false;
	private Bird _bird;
	private float _nextPipeX;
	private readonly List<Node2D> _activePipes = [];

	private readonly Queue<Node2D> _pipePool = [];
	private readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		_rng.Randomize();

		for (int i = 0; i < InitialPoolSize; i++)
		{
			var pipe = CreateNewPipePair();
			if (pipe != null)
			{
				pipe.Visible = false;
				pipe.ProcessMode = ProcessModeEnum.Disabled;
				_pipePool.Enqueue(pipe);
			}
		}

		_bird = GetNode<Bird>("/root/main/Bird");
		_nextPipeX = _bird != null
			? _bird.GlobalPosition.X + InitialSpawnDistance
			: InitialSpawnDistance;
	}

	public override void _ExitTree()
	{
		_activePipes?.Clear();
		_pipePool?.Clear();
		_bird = null;

		base._ExitTree();
	}

	public override void _Process(double delta)
	{
		if (!_gameRunning || _bird == null)
			return;

		float birdX = _bird.GlobalPosition.X;

		while (_nextPipeX < birdX + SpawnLookahead)
		{
			SpawnPipePair(_nextPipeX);
			_nextPipeX += PipeSpacing;
		}

		if (_activePipes.Count > 0)
		{
			for (int i = _activePipes.Count - 1; i >= 0; i--)
			{
				float pipeX = _activePipes[i].GlobalPosition.X;
				if (pipeX < birdX - DespawnDistance)
				{
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
		var pipe = _pipePool.Count > 0
			? _pipePool.Dequeue()
			: CreateNewPipePair();

		if (pipe != null)
		{
			pipe.Visible = true;
			pipe.ProcessMode = ProcessModeEnum.Inherit;
		}

		return pipe;
	}

	private void ReturnPipeToPool(Node2D pipe)
	{
		pipe.Visible = false;
		pipe.ProcessMode = ProcessModeEnum.Disabled;
		_pipePool.Enqueue(pipe);
	}

	private void SpawnPipePair(float xPosition)
	{
		float gapCenter = _rng.RandfRange(MinHeight, MaxHeight);
		var pipePair = GetPipeFromPool();

		if (pipePair == null)
		{
			GD.PushError("Pipes: Failed to get pipe from pool!");
			return;
		}

		if (pipePair is PipePair pp)
			pp.Reconfigure(GapSize);

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
		foreach (var pipe in _activePipes)
		{
			if (pipe != null)
				ReturnPipeToPool(pipe);
		}
		_activePipes.Clear();

		_nextPipeX = _bird != null
			? _bird.GlobalPosition.X + ResetSpawnDistance
			: ResetSpawnDistance;
	}
}
