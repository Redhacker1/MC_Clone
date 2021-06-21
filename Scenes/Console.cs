using System.Text;
using Godot;
using MinecraftClone.Debug_and_Logging;

namespace MinecraftClone.Scenes
{
	public class Console : Control
	{
		StringBuilder _scrollback = new StringBuilder();
		// Declare member variables here. Examples:
		// private int a = 2;
		// private string b = "text";

		// Called when the node enters the scene tree for the first time.
		TextEdit _consoleBox;
		LineEdit _cmdInputBox;
	
		public override void _Ready()
		{
			ConsoleLibrary.InitConsole(WriteToTextbox);
			_consoleBox = GetNode("VSplitContainer/ConsoleHistory") as TextEdit;
			_cmdInputBox = GetNode("VSplitContainer/HSplitContainer/LineEdit") as LineEdit;
			
			_cmdInputBox?.Connect("text_entered", this, "CommandEntered");
			_consoleBox.Text = "lol";
		}

		public void CommandEntered(string newText)
		{
			_cmdInputBox.Text = string.Empty;
			ConsoleLibrary.SendCommand(newText);
		}

		void WriteToTextbox(string newText)
		{
			_consoleBox.ScrollVertical = double.PositiveInfinity;
			_consoleBox.Text = newText;
			_consoleBox.ScrollVertical = double.PositiveInfinity;
		}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
	}
}
