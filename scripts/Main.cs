using Godot;
using System;

public partial class Main : Node2D
{
	// Game state
	private bool _gameRunning = false;
	private bool _gameOver = false;
	private int _score = 0;
	private int _highScore = 0;

	[Export] public float GameOverDelay { get; set; } = 1.0f;

	// Scene references
	private Bird _bird;
	private Floor _floor;
	private Pipes _pipes;
	private BackgroundController _background;
	private Label _scoreLabel;
	private StartScreen _startScreen;
	private GameOverScreen _gameOverScreen;
	private Button _muteButton;

	// Audio
	private AudioStreamPlayer _flapSound;
	private AudioStreamPlayer _hitSound;
	private AudioStreamPlayer _scoreSound;
	private AudioStreamPlayer _startScreenMusic;
	private AudioStreamPlayer _gameOverSound;
	private float _originalFlapVolume;
	private float _originalHitVolume;
	private float _originalScoreVolume;
	private float _originalStartMusicVolume;
	private float _originalGameOverVolume;

	// Mute settings
	private bool _isMuted = false;
	[Export] public Texture2D IconMute { get; set; }
	[Export] public Texture2D IconUnmute { get; set; }

	// Timers
	private SceneTreeTimer _gameOverTimer;

	// Save file path
	private const string SaveFilePath = "user://highscore.save";

	public override void _Ready()
	{
		_bird = GetNode<Bird>("Bird");
		_floor = GetNode<Floor>("Floor");
		_pipes = GetNode<Pipes>("Pipes");
		_background = GetNode<Parallax2D>("Background") as BackgroundController;
		_scoreLabel = GetNode<Label>("UI/ScoreLabel");
		_startScreen = GetNode<StartScreen>("UI/StartScreen");
		_gameOverScreen = GetNode<GameOverScreen>("UI/GameOverScreen");
		_muteButton = GetNode<Button>("UI/StartScreen/MuteButton");

		_flapSound = GetNode<AudioStreamPlayer>("Sounds/FlapSound");
		_hitSound = GetNode<AudioStreamPlayer>("Sounds/HitSound");
		_scoreSound = GetNode<AudioStreamPlayer>("Sounds/ScoreSound");
		_startScreenMusic = GetNode<AudioStreamPlayer>("Sounds/StartScreenMusic");
		_gameOverSound = GetNode<AudioStreamPlayer>("Sounds/GameOverSound");

		StoreOriginalVolumes();

		LoadHighScore();

		// Initialize UI
		UpdateScoreDisplay();
		_startScreen?.UpdateHighScore(_highScore);
		_startScreen?.Show();
		_gameOverScreen?.Hide();
		// Play start screen music
		_startScreenMusic?.Play();

		// Set initial mute button icon and connect event
		if (_muteButton != null)
		{
			UpdateMuteButtonIcon();
			_muteButton.Pressed += OnMuteButtonPressed;
		}
	}

	public override void _ExitTree()
	{
		// Disconnect events
		if (_gameOverTimer != null)
		{
			_gameOverTimer.Timeout -= ShowGameOverScreen;
		}

		if (_muteButton != null)
		{
			_muteButton.Pressed -= OnMuteButtonPressed;
		}

		// Clear references to allow garbage collection
		_bird = null;
		_floor = null;
		_pipes = null;
		_background = null;
		_scoreLabel = null;
		_startScreen = null;
		_gameOverScreen = null;
		_muteButton = null;
		_flapSound = null;
		_hitSound = null;
		_scoreSound = null;
		_startScreenMusic = null;
		_gameOverSound = null;
		_gameOverTimer = null;

		base._ExitTree();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsLeftMouseClick(@event))
			return;

		if (TryRestartGame())
			return;

		if (TryStartGame())
			return;

		TryFlapBird();
	}

	private static bool IsLeftMouseClick(InputEvent @event)
	{
		return @event is InputEventMouseButton mouseEvent
			&& mouseEvent.ButtonIndex == MouseButton.Left
			&& mouseEvent.Pressed;
	}

	private bool TryRestartGame()
	{
		if (_gameOver && _gameOverScreen?.Visible == true)
		{
			ResetGame();
			GetViewport().SetInputAsHandled();
			return true;
		}
		return false;
	}

	private bool TryStartGame()
	{
		if (!_gameRunning && !_gameOver && _startScreen?.Visible == true)
		{
			StartGame();
			_bird?.Flap();
			PlayFlapSound();
			GetViewport().SetInputAsHandled();
			return true;
		}
		return false;
	}

	private bool TryFlapBird()
	{
		if (_gameRunning && !_gameOver && _bird != null && !_bird.IsDead() && _bird.IsFlying())
		{
			_bird.Flap();
			PlayFlapSound();
			GetViewport().SetInputAsHandled();
			return true;
		}
		return false;
	}

	private void StartGame()
	{
		_gameRunning = true;
		_gameOver = false;
		_score = 0;
		UpdateScoreDisplay();

		_startScreen?.Hide();

		_startScreenMusic?.Stop();

		_bird?.StartFlying();

		_floor?.StartScrolling();
		_pipes?.StartSpawning();
	}

	public void GameOver()
	{
		if (_gameOver) return;

		_gameOver = true;
		_gameRunning = false;


		PlayHitSound();
		_floor?.StopScrolling();
		_pipes?.StopSpawning();
		bool isNewRecord = _score > _highScore;
		if (isNewRecord)
		{
			_highScore = _score;
			SaveHighScore();
		}
		if (_gameOverTimer != null)
		{
			_gameOverTimer.Timeout -= ShowGameOverScreen;
		}
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

		_gameOverSound?.Play();
	}

	private void ResetGame()
	{
		_background?.SetBackground();
		_gameOverScreen?.Hide();
		_startScreen?.UpdateHighScore(_highScore);
		_startScreen?.Show();
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

	private void StoreOriginalVolumes()
	{
		_originalFlapVolume = _flapSound.VolumeDb;
		_originalHitVolume = _hitSound.VolumeDb;
		_originalScoreVolume = _scoreSound.VolumeDb;
		_originalStartMusicVolume = _startScreenMusic.VolumeDb;
		_originalGameOverVolume = _gameOverSound.VolumeDb;
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
		ApplyMuteSettings();
		UpdateMuteButtonIcon();
	}

	private void ApplyMuteSettings()
	{
		SetSoundVolume(_flapSound, _originalFlapVolume);
		SetSoundVolume(_hitSound, _originalHitVolume);
		SetSoundVolume(_scoreSound, _originalScoreVolume);
		SetSoundVolume(_startScreenMusic, _originalStartMusicVolume);
		SetSoundVolume(_gameOverSound, _originalGameOverVolume);
	}

	private void UpdateMuteButtonIcon()
	{
		if (_muteButton != null)
		{
			_muteButton.Icon = _isMuted ? IconMute : IconUnmute;
		}
	}

	private void SetSoundVolume(AudioStreamPlayer sound, float originalVolume)
	{
		if (sound != null)
		{
			sound.VolumeDb = _isMuted ? -80f : originalVolume;
		}
	}
}
