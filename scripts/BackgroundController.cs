using Godot;

public partial class BackgroundController : Parallax2D
{
	[Export] public Texture2D[] BackgroundTextures { get; set; }

	private Sprite2D _sprite;
	private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();
	private int _currentBackgroundIndex = -1;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_rng.Randomize();

		SetBackground();
	}

	public void SetBackground()
	{
		if (_sprite == null || BackgroundTextures == null || BackgroundTextures.Length == 0)
		{
			GD.PrintErr("BackgroundController: Sprite2D not found or no backgrounds assigned");
			return;
		}

		// Select random background different from current
		int newIndex;
		do
		{
			newIndex = _rng.RandiRange(0, BackgroundTextures.Length - 1);
		} while (newIndex == _currentBackgroundIndex && BackgroundTextures.Length > 1);

		_currentBackgroundIndex = newIndex;
		var texture = BackgroundTextures[_currentBackgroundIndex];

		if (texture == null)
			return;

		_sprite.Texture = texture;
		_sprite.RegionEnabled = false;
		_sprite.Position = Vector2.Zero;
		_sprite.Scale = new Vector2(4.0f, 2.7f);
		_sprite.Centered = false;
	}

	public override void _ExitTree()
	{
		_sprite = null;
		base._ExitTree();
	}
}
