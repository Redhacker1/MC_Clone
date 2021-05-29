using Godot;

namespace MinecraftClone.Scenes
{
    [Tool]
    public class AutoSizer : Control
    {
        // Declare member variables here. Examples:
        // private int a = 2;
        // private string b = "text";

        public Vector2 ScaleSize = new Vector2(.9f, .9f);
        public Vector2 AnchorLocation = new Vector2(.3f, .3f);
        

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            GetTree().Root.Connect("size_changed", this, "ChangeScreenSize");
            ChangeScreenSize();
        }

        void ChangeScreenSize()
        {
            RectSize = ((Control) GetParent()).RectSize * ScaleSize;
            AnchorRight = ((Control) GetParent()).RectSize.x * AnchorLocation.x;
            AnchorTop = ((Control) GetParent()).RectSize.y * -AnchorLocation.y;
        }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
    }
}
