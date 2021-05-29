using Godot;
using MinecraftClone.Debug_and_Logging;
using MinecraftClone.Player_CS;
using MinecraftClone.World_CS.Blocks;
using MinecraftClone.World_CS.Generation;
using MinecraftClone.World_CS.Utility.IO;

namespace MinecraftClone.World_CS.Utility
{
	public class WorldScript : Node
	{
		MeshInstance _blockOutline;
		Vector2 _chunkPos;

		int _chunkX = 1;
		int _chunkZ = 1;
		Player _player;

		ProcWorld _pw;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			BlockHelper.RegisterBaseBlocks();
			WorldManager.FindWorlds();
			WorldData worldPath = WorldManager.CreateWorld();
			_player = GetNode<KinematicBody>("Player") as Player;
			ConsoleLibrary.DebugPrint("CREATING WORLD");
			_pw = new ProcWorld {World = worldPath};

			AddChild(_pw);
			Connect("tree_exiting", this, "_on_WorldScript_tree_exiting");
			
			_player.World = _pw;
			_player.GameManager = this;
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
			if (_player == null || _pw?.Mutex == null) return;
			_chunkX = (int) Mathf.Floor(_player.Translation.x / ChunkCs.Dimension.x);
			_chunkZ = (int) Mathf.Floor(_player.Translation.z / ChunkCs.Dimension.x);

			Vector2 newChunkPos = new Vector2(_chunkX, _chunkZ);

			if (newChunkPos == _chunkPos) return;
			_chunkPos = newChunkPos;
			_pw.update_player_pos(_chunkPos);
		}
	}
}