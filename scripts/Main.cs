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
	[Export] public float GameOverDelay { get; set; } = 1.0f;
	
	// References to game objects
	private Bird _bird;
	private Floor _floor;
	private Pipes _pipes;
	private BackgroundController _background;
	private Label _scoreLabel;
	private StartScreen _startScreen;
	private GameOverScreen _gameOverScreen;
	
	// Sound effects
	private AudioStreamPlayer _flapSound;
	private AudioStreamPlayer _hitSound;
	private AudioStreamPlayer _scoreSound;
	
	// Timer reference for cleanup
	private SceneTreeTimer _gameOverTimer;
	
	// High score file path
	private const string SaveFilePath = "user://highscore.save";
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Get references to game objects
		_bird = GetNode<Bird>("Bird");
		_floor = GetNode<Floor>("Floor");
		_pipes = GetNode<Pipes>("Pipes");
		_background = GetNode<Parallax2D>("Background") as BackgroundController;
		_scoreLabel = GetNode<Label>("UI/ScoreLabel");
		_startScreen = GetNode<StartScreen>("UI/StartScreen");
		_gameOverScreen = GetNode<GameOverScreen>("UI/GameOverScreen");
		
		// Get sound effects
		_flapSound = GetNode<AudioStreamPlayer>("Sounds/FlapSound");
		_hitSound = GetNode<AudioStreamPlayer>("Sounds/HitSound");
		_scoreSound = GetNode<AudioStreamPlayer>("Sounds/ScoreSound");
		
		// Load high score
		LoadHighScore();
		
		// Initialize UI
		UpdateScoreDisplay();
		_startScreen?.UpdateHighScore(_highScore);
		_startScreen?.Show();
		_gameOverScreen?.Hide();
	}

	public override void _ExitTree()
	{
		// Disconnect timer to prevent memory leak
		if (_gameOverTimer != null)
		{
			_gameOverTimer.Timeout -= ShowGameOverScreen;
		}
		
		base._ExitTree();
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
			else if (!_gameRunning && !_gameOver && _startScreen != null && _startScreen.Visible)
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
		
		// Hide start screen
		_startScreen?.Hide();
		
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
		
		// Check for new high score
		bool isNewRecord = _score > _highScore;
		if (isNewRecord)
		{
			_highScore = _score;
			SaveHighScore();
		}
		
		// Disconnect old timer if exists
		if (_gameOverTimer != null)
		{
			_gameOverTimer.Timeout -= ShowGameOverScreen;
		}
		
		// Show game over screen after a short delay
		_gameOverTimer = GetTree().CreateTimer(GameOverDelay);
		_gameOverTimer.Timeout += ShowGameOverScreen;
	}
	
	public void IncrementScore()
	{
		if (_gameRunning && !_gameOver)
		{
			_score++;
			UpdateScoreDisplay();
			PlayScoreSound();
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
		bool isNewRecord = _score == _highScore && _score > 0;
		_gameOverScreen?.ShowGameOver(_score, _highScore, isNewRecord);
	}
	
	private void ResetGame()
	{
		// Change background to a new random one
		_background?.ChangeBackground();
		
		// Hide game over screen, show start screen
		_gameOverScreen?.Hide();
		_startScreen?.UpdateHighScore(_highScore);
		_startScreen?.Show();
		
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
		if (!FileAccess.FileExists(SaveFilePath))
			return;
		
		try
		{
			using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Read);
			if (file != null)
			{
				_highScore = (int)file.Get32();
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning($"Failed to load high score: {ex.Message}");
			_highScore = 0;
		}
	}
	
	private void SaveHighScore()
	{
		try
		{
			using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Write);
			file?.Store32((uint)_highScore);
		}
		catch (Exception ex)
		{
			GD.PushError($"Failed to save high score: {ex.Message}");
		}
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
