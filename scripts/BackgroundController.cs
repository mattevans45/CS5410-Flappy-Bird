using Godot;
using System;

public partial class BackgroundController : Parallax2D
{
	// Array of background textures (assign in editor)
	[Export] public Texture2D[] BackgroundTextures { get; set; }
	
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
		
		if (_sprite == null || BackgroundTextures == null || BackgroundTextures.Length == 0)
		{
			GD.PrintErr("BackgroundController: Sprite2D not found or no backgrounds assigned in editor");
			return;
		}
		
		// Pick a different background than the current one
		int newIndex;
		do
		{
			newIndex = _rng.RandiRange(0, BackgroundTextures.Length - 1);
		} while (newIndex == _currentBackgroundIndex && BackgroundTextures.Length > 1);
		
		_currentBackgroundIndex = newIndex;
		
		// Apply the texture (already pre-loaded)
		var texture = BackgroundTextures[_currentBackgroundIndex];
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
