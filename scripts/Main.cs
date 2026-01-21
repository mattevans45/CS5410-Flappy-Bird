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
	private Button _muteButton;
	
	// Sound effects
	private AudioStreamPlayer _flapSound;
	private AudioStreamPlayer _hitSound;
	private AudioStreamPlayer _scoreSound;
	private AudioStreamPlayer _startScreenMusic;
	private AudioStreamPlayer _gameOverSound;
	
	// Timer reference for cleanup
	private SceneTreeTimer _gameOverTimer;
	
	// High score file path
	private const string SaveFilePath = "user://highscore.save";
	
	// Mute state
	private bool _isMuted = false;
	[Export] public Texture2D IconMute { get; set; }
	[Export] public Texture2D IconUnmute { get; set; }
	
	// Original audio settings
	private float _originalFlapVolume;
	private float _originalHitVolume;
	private float _originalScoreVolume;
	private float _originalStartMusicVolume;
	private float _originalGameOverVolume;
	
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
		_muteButton = GetNode<Button>("UI/StartScreen/MuteButton");
		
		// Get sound effects
		_flapSound = GetNode<AudioStreamPlayer>("Sounds/FlapSound");
		_hitSound = GetNode<AudioStreamPlayer>("Sounds/HitSound");
		_scoreSound = GetNode<AudioStreamPlayer>("Sounds/ScoreSound");
		_startScreenMusic = GetNode<AudioStreamPlayer>("Sounds/StartScreenMusic");
		_gameOverSound = GetNode<AudioStreamPlayer>("Sounds/GameOverSound");
		
		// Store original audio volumes
		_originalFlapVolume = _flapSound.VolumeDb;
		_originalHitVolume = _hitSound.VolumeDb;
		_originalScoreVolume = _scoreSound.VolumeDb;
		_originalStartMusicVolume = _startScreenMusic.VolumeDb;
		_originalGameOverVolume = _gameOverSound.VolumeDb;
		
		// Load high score
		LoadHighScore();
		
		// Initialize UI
		UpdateScoreDisplay();
		_startScreen?.UpdateHighScore(_highScore);
		_startScreen?.Show();
		_gameOverScreen?.Hide();
		// Play start screen music
		_startScreenMusic?.Play();
		
		// Set initial mute button icon
		if (_muteButton != null && IconUnmute != null)
		{
			_muteButton.Icon = IconUnmute;
			_muteButton.Pressed += OnMuteButtonPressed;
		}
	}

	public override void _ExitTree()
	{
		// Disconnect timer to prevent memory leak
		if (_gameOverTimer != null)
		{
			_gameOverTimer.Timeout -= ShowGameOverScreen;
		}
		
		// Disconnect mute button event to prevent memory leak
		if (_muteButton != null)
		{
			_muteButton.Pressed -= OnMuteButtonPressed;
		}
		
		base._ExitTree();
	}
	
	public override void _UnhandledInput(InputEvent @event)
	{
		// Handle mouse button click (use _UnhandledInput so UI elements like buttons can handle input first)
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
		// Stop start screen music
		_startScreenMusic?.Stop();
		
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
		// Play game over sound (duck hunt laugh)
		_gameOverSound?.Play();
	}
	
	private void ResetGame()
	{
		// Change background to a new random one
		_background?.ChangeBackground();
		// Hide game over screen, show start screen
		_gameOverScreen?.Hide();
		_startScreen?.UpdateHighScore(_highScore);
		_startScreen?.Show();
		// Play start screen music again
		_startScreenMusic?.Play();
		
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
	
	private void PlaySound(AudioStreamPlayer sound)
	{
		if (sound != null && sound.Stream != null)
		{
			sound.Play();
		}
	}
	
	private void PlayFlapSound() => PlaySound(_flapSound);
	private void PlayHitSound() => PlaySound(_hitSound);
	private void PlayScoreSound() => PlaySound(_scoreSound);
	
	private void OnMuteButtonPressed()
	{
		_isMuted = !_isMuted;
		
		if (_isMuted)
		{
			// Mute all sounds to -80 dB (effectively silent)
			_flapSound.VolumeDb = -80f;
			_hitSound.VolumeDb = -80f;
			_scoreSound.VolumeDb = -80f;
			_startScreenMusic.VolumeDb = -80f;
			_gameOverSound.VolumeDb = -80f;
		}
		else
		{
			// Restore original volumes
			_flapSound.VolumeDb = _originalFlapVolume;
			_hitSound.VolumeDb = _originalHitVolume;
			_scoreSound.VolumeDb = _originalScoreVolume;
			_startScreenMusic.VolumeDb = _originalStartMusicVolume;
			_gameOverSound.VolumeDb = _originalGameOverVolume;
		}
		
		// Switch icon
		if (_muteButton != null)
		{
			_muteButton.Icon = _isMuted ? IconMute : IconUnmute;
		}
	}
}
