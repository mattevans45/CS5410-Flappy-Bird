using Godot;
using System;

public partial class Main : Node2D
{
	// Game state variables
	private bool _gameRunning = false;
	private bool _gameOver = false;
	private int _score = 0;
	private int _highScore = 0;
	
	// Game configuration
	[Export] public float ScrollSpeed { get; set; } = 0.0f;
	[Export] public float GroundHeight { get; set; } = 100.0f;
	[Export] public float PipeDelay { get; set; } = 2.0f;
	[Export] public float PipeRangeMin { get; set; } = 100.0f;
	[Export] public float PipeRangeMax { get; set; } = 400.0f;
	[Export] public float GameOverDelay { get; set; } = 1.0f;
	
	// References to game objects
	private Bird _bird;
	private Floor _floor;
	private Pipes _pipes;
	private Label _scoreLabel;
	private Control _gameOverScreen;
	private Label _gameOverScoreLabel;
	private Label _gameOverHighScoreLabel;
	
	// Sound effects
	private AudioStreamPlayer _flapSound;
	private AudioStreamPlayer _hitSound;
	private AudioStreamPlayer _scoreSound;
	
	// High score file path
	private const string SaveFilePath = "user://highscore.save";
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Get references to game objects
		_bird = GetNode<Bird>("Bird");
		_floor = GetNode<Floor>("Floor");
		_pipes = GetNode<Pipes>("Pipes");
		_scoreLabel = GetNode<Label>("UI/ScoreLabel");
		_gameOverScreen = GetNode<Control>("UI/GameOverScreen");
		_gameOverScoreLabel = GetNode<Label>("UI/GameOverScreen/Panel/VBoxContainer/ScoreLabel");
		_gameOverHighScoreLabel = GetNode<Label>("UI/GameOverScreen/Panel/VBoxContainer/HighScoreLabel");
		
		// Get sound effects
		_flapSound = GetNode<AudioStreamPlayer>("Sounds/FlapSound");
		_hitSound = GetNode<AudioStreamPlayer>("Sounds/HitSound");
		_scoreSound = GetNode<AudioStreamPlayer>("Sounds/ScoreSound");
		
		// Load high score
		LoadHighScore();
		
		// Set scroll speed for floor
		_floor?.SetScrollSpeed(ScrollSpeed);
		
		// Initialize score display
		UpdateScoreDisplay();
		
		// Hide game over screen initially
		if (_gameOverScreen != null)
		{
			_gameOverScreen.Visible = false;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public override void _Input(InputEvent @event)
	{
		// Handle mouse button click (use _Input instead of _UnhandledInput so UI doesn't block it)
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
		{
			// Restart game if game over screen is shown
			if (_gameOver && _gameOverScreen != null && _gameOverScreen.Visible)
			{
				ResetGame();
				GetViewport().SetInputAsHandled();
			}
			// Start the game if not running and not in game over state
			else if (!_gameRunning && !_gameOver)
			{
				StartGame();
				// Also flap on the first click to start
				if (_bird != null)
				{
					_bird.Flap();
					PlayFlapSound();
				}
				GetViewport().SetInputAsHandled();
			}
			// Flap the bird if game is running and not game over
			else if (_gameRunning && !_gameOver && _bird != null && !_bird.IsDead() && _bird.IsFlying())
			{
				_bird.Flap();
				PlayFlapSound();
				GetViewport().SetInputAsHandled();
			}
		}
	}
	
	private void StartGame()
	{
		_gameRunning = true;
		_gameOver = false;
		_score = 0;
		UpdateScoreDisplay();
		
		// Start the bird flying to the right
		_bird?.StartFlying();
		
		// Start floor scrolling
		_floor?.StartScrolling();
		
		// Start pipe spawning
		_pipes?.StartSpawning();
	}
	
	public void GameOver()
	{
		if (_gameOver) return; // Prevent multiple game over calls
		
		// Set flags IMMEDIATELY to block all input
		_gameOver = true;
		_gameRunning = false;
		
		// Play hit sound
		PlayHitSound();
		
		// Stop floor scrolling
		_floor?.StopScrolling();
		
		// Stop pipe spawning
		_pipes?.StopSpawning();
		
		// Update high score if needed
		if (_score > _highScore)
		{
			_highScore = _score;
			SaveHighScore();
		}
		
		// Show game over screen after a short delay using CallDeferred
		GetTree().CreateTimer(GameOverDelay).Timeout += ShowGameOverScreen;
	}
	
	public void IncrementScore()
	{
		if (_gameRunning && !_gameOver)
		{
			_score++;
			UpdateScoreDisplay();
			PlayScoreSound();
			// GD.Print($"Score: {_score}");
		}
	}
	
	public bool IsGameRunning()
	{
		return _gameRunning && !_gameOver;
	}
	
	private void UpdateScoreDisplay()
	{
		if (_scoreLabel != null)
		{
			_scoreLabel.Text = _score.ToString();
		}
	}
	
	private void ShowGameOverScreen()
	{
		if (_gameOverScreen != null)
		{
			_gameOverScreen.Visible = true;
		}
		
		if (_gameOverScoreLabel != null)
		{
			_gameOverScoreLabel.Text = $"Score: {_score}";
		}
		
		if (_gameOverHighScoreLabel != null)
		{
			_gameOverHighScoreLabel.Text = $"High Score: {_highScore}";
		}
	}
	
	private void ResetGame()
	{
		// Hide game over screen
		if (_gameOverScreen != null)
		{
			_gameOverScreen.Visible = false;
		}
		
		// Reset game state
		_gameRunning = false;
		_gameOver = false;
		_score = 0;
		UpdateScoreDisplay();
		
		// Reset bird
		_bird?.Reset();
		
		// Reset floor
		_floor?.Reset();
		
		// Reset pipes
		_pipes?.ResetPipes();
	}
	
	private void LoadHighScore()
	{
		if (FileAccess.FileExists(SaveFilePath))
		{
			using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Read);
			if (file != null)
			{
				_highScore = (int)file.Get32();
			}
		}
	}
	
	private void SaveHighScore()
	{
		using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Write);
		file?.Store32((uint)_highScore);
	}
	
	private void PlayFlapSound()
	{
		if (_flapSound != null && _flapSound.Stream != null)
		{
			_flapSound.Play();
		}
	}
	
	private void PlayHitSound()
	{
		if (_hitSound != null && _hitSound.Stream != null)
		{
			_hitSound.Play();
		}
	}
	
	private void PlayScoreSound()
	{
		if (_scoreSound != null && _scoreSound.Stream != null)
		{
			_scoreSound.Play();
		}
	}
}
