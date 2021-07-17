using System;
using Godot;
using MinecraftClone.Debug_and_Logging;
using MinecraftClone.Player_CS;
using MinecraftClone.Utility.IO;
using MinecraftClone.World_CS.Blocks;
using MinecraftClone.World_CS.Generation;
using MinecraftClone.World_CS.Utility.Debug;
using Path = System.IO.Path;
using Vector2 = System.Numerics.Vector2;

namespace MinecraftClone.Utility
{
	//[Tool]
	public class WorldScript : Node
	{
		Vector2 _chunkPos;

		int _chunkX = 1;
		int _chunkZ = 1;
		Player _player;

		public static DebugLines lines = new DebugLines();

		public Logger Logger = new Logger(Path.Combine(OS.GetExecutablePath().GetBaseDir(),"Logs"), "DebugFile", ConsoleLibrary.DebugPrint);

		static public ProcWorld _pw;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			AddChild(lines);
			BlockHelper.RegisterBaseBlocks();
			WorldManager.FindWorlds();
			WorldData worldPath = WorldManager.CreateWorld();
			_player = GetNode<Node>("Player") as Player;
			_player.Level = _pw;

			if (!Engine.EditorHint)
			{
				ConsoleLibrary.DebugPrint("CREATING WORLD");	
			}
			else
			{
				GD.Print("CREATING WORLD (Editor)");
			}
			_pw = new ProcWorld(1337) {World = worldPath};

			AddChild(_pw);
			Connect("tree_exiting", this, "_on_WorldScript_tree_exiting");
			
			_player.World = _pw;
			//_player.GameManager = this;
		}

		void _on_WorldScript_tree_exiting()
		{
			ConsoleLibrary.DebugPrint("Kill map loading thread");
			if (_pw != null)
			{
				_pw.SaveAndQuit();
				ConsoleLibrary.DebugPrint("Finished");
			}
		}
		

		//  // Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(float delta)
		{
			if (_player == null) return;
			_chunkX = (int) Math.Floor(_player.Pos.X / ChunkCs.Dimension.X);
			_chunkZ = (int) Math.Floor(_player.Pos.Z / ChunkCs.Dimension.Z);

			Vector2 newChunkPos = new Vector2(_chunkX, _chunkZ);

			if (newChunkPos == _chunkPos) return;
			ConsoleLibrary.DebugPrint("Chunk Updated");
			_chunkPos = newChunkPos;
			_pw.update_player_pos(_chunkPos);
		}
	}
}
