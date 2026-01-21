using Godot;
using System;

public partial class BackgroundController : Parallax2D
{
	// Array of background texture paths (these are full backgrounds, not sheets)
	private static readonly string[] BackgroundPaths = new[]
	{
		"res://assets/Background/Background1.png",
		"res://assets/Background/Background2.png",
		"res://assets/Background/Background3.png",
		"res://assets/Background/Background4.png",
		"res://assets/Background/Background5.png",
		"res://assets/Background/Background6.png",
		"res://assets/Background/Background7.png",
		"res://assets/Background/Background8.png",
		"res://assets/Background/Background9.png"
	};
	
	private Sprite2D _sprite;
	private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();
	private int _currentBackgroundIndex = -1;
	
	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_rng.Randomize();
		
		// Set initial random background
		ChangeBackground();
	}
	
	public void ChangeBackground()
	{
		// Get sprite reference if not already set
		if (_sprite == null)
		{
			_sprite = GetNode<Sprite2D>("Sprite2D");
		}
		
		if (_sprite == null || BackgroundPaths.Length == 0)
		{
			GD.PrintErr("BackgroundController: Sprite2D not found or no backgrounds available");
			return;
		}
		
		// Pick a different background than the current one
		int newIndex;
		do
		{
			newIndex = _rng.RandiRange(0, BackgroundPaths.Length - 1);
		} while (newIndex == _currentBackgroundIndex && BackgroundPaths.Length > 1);
		
		_currentBackgroundIndex = newIndex;
		
		// Load and apply the new texture
		var texture = GD.Load<Texture2D>(BackgroundPaths[_currentBackgroundIndex]);
		if (texture != null)
		{
			_sprite.Texture = texture;
			
			// Disable region for regular backgrounds (they're full images, not sprite sheets)
			_sprite.RegionEnabled = false;
			
			// Scale 256x256 backgrounds to fit properly
			// x: 1024 / 256 = 4.0 to match repeat width
			// y: Scale to fill screen height, accounting for Parallax2D scale.y = 2.7
			_sprite.Position = new Vector2(0, 0);
			_sprite.Scale = new Vector2(4.0f, 2.7f);
			_sprite.Centered = false;
		}
	}
	
	public override void _ExitTree()
	{
		// Clear sprite reference to allow garbage collection
		_sprite = null;
		
		base._ExitTree();
	}
}
