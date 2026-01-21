using Godot;
using System;

public partial class GameOverScreen : Control
{
	private Label _scoreLabel;
	private Label _highScoreLabel;
	private Label _newRecordLabel;
	private AnimationPlayer _animationPlayer;
	private AnimationPlayer _blinkPlayer;
	private SceneTreeTimer _blinkDelayTimer;

	public override void _Ready()
	{
		_scoreLabel = GetNode<Label>("Panel/VBoxContainer/ScoreContainer/ScoreLabel");
		_highScoreLabel = GetNode<Label>("Panel/VBoxContainer/HighScoreContainer/HighScoreLabel");
		_newRecordLabel = GetNode<Label>("Panel/VBoxContainer/NewRecordLabel");
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_blinkPlayer = GetNode<AnimationPlayer>("BlinkPlayer");
	}

	public void ShowGameOver(int score, int highScore, bool isNewRecord)
	{
		Visible = true;

		if (_scoreLabel != null)
			_scoreLabel.Text = $"Score: {score}";

		if (_highScoreLabel != null)
			_highScoreLabel.Text = $"Best: {highScore}";

		if (_newRecordLabel != null)
			_newRecordLabel.Visible = isNewRecord;

		_animationPlayer?.Play("appear");

		// Disconnect old timer if exists
		if (_blinkDelayTimer != null)
			_blinkDelayTimer.Timeout -= StartBlinkAnimation;

		// Delay blink animation to start after appear animation
		_blinkDelayTimer = GetTree().CreateTimer(0.5);
		_blinkDelayTimer.Timeout += StartBlinkAnimation;
	}

	private void StartBlinkAnimation()
	{
		_blinkPlayer?.Play("blink");
	}

	public new void Hide()
	{
		Visible = false;
		_animationPlayer?.Play("RESET");
		_blinkPlayer?.Stop();
	}

	public override void _ExitTree()
	{
		if (_blinkDelayTimer != null)
		{
			_blinkDelayTimer.Timeout -= StartBlinkAnimation;
		}

		// Clear references to allow garbage collection
		_scoreLabel = null;
		_highScoreLabel = null;
		_newRecordLabel = null;
		_animationPlayer = null;
		_blinkPlayer = null;
		_blinkDelayTimer = null;

		base._ExitTree();
	}
}
