using Godot;
using System;

public partial class StartScreen : Control
{
	private Label _highScoreLabel;
	private AnimationPlayer _animationPlayer;
	private AnimationPlayer _blinkPlayer;
	
	public override void _Ready()
	{
		_highScoreLabel = GetNode<Label>("HighScoreLabel");
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_blinkPlayer = GetNode<AnimationPlayer>("BlinkPlayer");
		
		// Start animations
		if (_animationPlayer != null)
		{
			_animationPlayer.Play("idle");
		}
		
		if (_blinkPlayer != null)
		{
			_blinkPlayer.Play("blink");
		}
	}
	
	public void UpdateHighScore(int highScore)
	{
		if (_highScoreLabel != null)
		{
			_highScoreLabel.Text = $"High Score: {highScore}";
		}
	}
	
	public new void Hide()
	{
		Visible = false;
	}
	
	public new void Show()
	{
		Visible = true;
		
		// Restart animations when shown
		if (_animationPlayer != null && !_animationPlayer.IsPlaying())
		{
			_animationPlayer.Play("idle");
		}
		
		if (_blinkPlayer != null && !_blinkPlayer.IsPlaying())
		{
			_blinkPlayer.Play("blink");
		}
	}
	
	public override void _ExitTree()
	{
		// Clear references to allow garbage collection
		_highScoreLabel = null;
		_animationPlayer = null;
		_blinkPlayer = null;
		
		base._ExitTree();
	}
}
