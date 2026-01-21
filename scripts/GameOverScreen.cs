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
		
		// Update labels
		if (_scoreLabel != null)
		{
			_scoreLabel.Text = $"Score: {score}";
		}
		
		if (_highScoreLabel != null)
		{
			_highScoreLabel.Text = $"Best: {highScore}";
		}
		
		// Show new record indicator if applicable
		if (_newRecordLabel != null)
		{
			_newRecordLabel.Visible = isNewRecord;
		}
		
		// Play appear animation
		if (_animationPlayer != null)
		{
			_animationPlayer.Play("appear");
		}
		
		// Start blink animation after appear finishes
		if (_blinkPlayer != null)
		{
			// Disconnect old timer if exists
			if (_blinkDelayTimer != null)
			{
				_blinkDelayTimer.Timeout -= StartBlinkAnimation;
			}
			
			// Delay blink animation to start after appear
			_blinkDelayTimer = GetTree().CreateTimer(0.5);
			_blinkDelayTimer.Timeout += StartBlinkAnimation;
		}
	}
	
	private void StartBlinkAnimation()
	{
		if (_blinkPlayer != null)
		{
			_blinkPlayer.Play("blink");
		}
	}
	
	public new void Hide()
	{
		Visible = false;
		
		// Reset animations
		if (_animationPlayer != null)
		{
			_animationPlayer.Play("RESET");
		}
		
		if (_blinkPlayer != null)
		{
			_blinkPlayer.Stop();
		}
	}
	
	public override void _ExitTree()
	{
		// Disconnect timer to prevent memory leak
		if (_blinkDelayTimer != null)
		{
			_blinkDelayTimer.Timeout -= StartBlinkAnimation;
		}
		
		base._ExitTree();
	}
}
