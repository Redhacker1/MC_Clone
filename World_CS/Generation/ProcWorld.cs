using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MinecraftClone.Debug_and_Logging;
using MinecraftClone.World_CS.Blocks;
using MinecraftClone.World_CS.Generation.Noise;
using MinecraftClone.World_CS.Utility;
using MinecraftClone.World_CS.Utility.IO;
using MinecraftClone.World_CS.Utility.Threading;
using Thread = System.Threading.Thread;

namespace MinecraftClone.World_CS.Generation
{
	public class ProcWorld : Spatial
	{

		ThreadPoolClass Threads = new ThreadPoolClass();
		
		//
		int LoadRadius = 16;

		readonly object _chunkMutex = new object(); 
		
		public WorldData World;

		public WorldScript Initializer;

		public readonly Dictionary<Vector2, ChunkCs> LoadedChunks = new Dictionary<Vector2, ChunkCs>();

		public readonly NoiseUtil HeightNoise = new NoiseUtil();
		public readonly object Mutex = new object();

		bool _bKillThread;
		Vector2 _chunkPos; 
		Vector2 _currentChunkPos;
		int _currentLoadRadius;
		Vector2 _lastChunk;

		Vector2 _newChunkPos;

		Thread _terrainThread;

		public override void _Ready()
		{
			
			ConsoleLibrary.DebugPrint("Starting procworld");
			
			ConsoleLibrary.DebugPrint("Preparing Threadpool");
			// Starts the threadpool;
			Threads.InitializePool();
			Threads.IgniteThreadPool();
			
			ConsoleLibrary.DebugPrint("Registering Blocks");
			// Sets the blocks used in the base game up.
			BlockHelper.RegisterBaseBlocks();
			
			ConsoleLibrary.DebugPrint("Creating noise");
			// Sets Noise settings
			HeightNoise.SetFractalOctaves(100); 
			HeightNoise.SetFractalGain(100);

			ConsoleLibrary.DebugPrint("Creating Terrain Gen thread");
			// Preparing static terrain thread 
			_terrainThread = new Thread(_thread_gen);
			_terrainThread.Start();

			ConsoleLibrary.DebugPrint("Binding Console Commands");
			// Console Binds
			ConsoleLibrary.BindCommand("reload_chunks", "reloads all currently loaded chunks", "reload_chunks", ReloadChunks, false);
		}

		void _thread_gen()
		{
			ConsoleLibrary.DebugPrint("ThreadGen Thread Running");
			while (!_bKillThread)
			{
				bool PlayerPosUpdated = _newChunkPos != _chunkPos;

				_chunkPos = _newChunkPos;

				_currentChunkPos = _newChunkPos;

				if (PlayerPosUpdated)
				{
					enforce_render_distance(_currentChunkPos);
					_lastChunk = _load_chunk((int) _currentChunkPos.x, (int) _currentChunkPos.y);
					_currentLoadRadius = 1;
				}
				else
				{
					// Load next chunk based on the position of the last one
					Vector2 DeltaPos = _lastChunk - _currentChunkPos;
					// Only have player chunk
					if (DeltaPos == Vector2.Zero)
					{
						// Move down one
						_lastChunk = _load_chunk((int) _lastChunk.x, (int) _lastChunk.y + 1);
					}
					else if (DeltaPos.x < DeltaPos.y)
					{
						// Either go right or up
						// Prioritize going right
						if ((DeltaPos.y == _currentLoadRadius) & (-DeltaPos.x != _currentLoadRadius))
						{
							//Go right
							_lastChunk = _load_chunk((int)_lastChunk.x - 1, (int) _lastChunk.y);
						}
						// Either moving in constant x or we just reached bottom right. Addendum by donovan: this looping on the X axis has happened to me actually
						else if ((-DeltaPos.x == _currentLoadRadius) | (-DeltaPos.x == DeltaPos.y))
						{
							// Go up
							_lastChunk = _load_chunk((int) _lastChunk.x, (int) _lastChunk.y - 1);
						}
						else
						{
							// We increment here idk why
							if (_currentLoadRadius < LoadRadius)
							{
								_currentLoadRadius++;
							}
						}
					}
					else
					{
						//Either go left or down
						//Prioritize going left
						if ((-DeltaPos.y == _currentLoadRadius) & (DeltaPos.x != _currentLoadRadius))
						{
							//Go left
							_lastChunk = _load_chunk((int) _lastChunk.x + 1, (int) _lastChunk.y);	
						}
						else if ((DeltaPos.x == _currentLoadRadius) | (DeltaPos.x == -DeltaPos.y))
						{
							// Go down
							// Stop the last one where we'd go over the limit
							if (DeltaPos.y < LoadRadius)
							{
								_lastChunk = _load_chunk((int) _lastChunk.x, (int) _lastChunk.y + 1);
							}
						}
					}
				}
			}
		}

		Vector2 _load_chunk(int Cx, int Cz)
		{
			Vector2 Cpos = new Vector2(Cx, Cz);
			bool LoadChunk;
			lock (_chunkMutex)
			{
				LoadChunk = !LoadedChunks.ContainsKey(Cpos);
			}

			if (LoadChunk)
			{
				ChunkCs C;
				if (SaveFileHandler.ChunkExists(World, Cpos))
				{
					C = SaveFileHandler.GetChunkData(this ,World, Cpos, out _);
				}
				else
				{
					C = new ChunkCs();
					C.Generate(this, Cx, Cz, Vector3.Zero);	
				}
				lock (_chunkMutex)
				{
					LoadedChunks[Cpos] = C;
				}
				CallDeferred("add_child", C);
				_update_chunk(Cx, Cz);
			}
			return Cpos;
		}

		public string ReloadChunks(params string[] args)
		{
			ChunkCs[] Chunks = LoadedChunks.Values.ToArray();

			foreach (ChunkCs chunk in Chunks)
			{
				if (chunk != null)
				{
					Threads.AddRequest(() =>
					{
						chunk?.Update();
						return null;
					});
				}
			}
			return $"{Chunks.Length} Chunks sent to threadpool for processing...";
		}
		
		
		

		public void change_block(int Cx, int Cz, int Bx, int By, int Bz, byte T)
		{
			ChunkCs C;
			lock (_chunkMutex)
			{
				C = LoadedChunks[new Vector2(Cx, Cz)];
			}

			if (C.BlockData[ChunkCs.GetFlattenedDimension(Bx, By, Bz)] != T)
			{
				ConsoleLibrary.DebugPrint($"Changed block at {Bx} {By} {Bz} in chunk {Cx}, {Cz}");
				C?._set_block_data(Bx,By,Bz,T, UpdateSurrounding: true);
				_update_chunk(Cx, Cz);
			}
		}

		Vector2 _update_chunk(int Cx, int Cz)
		{
			Vector2 Cpos = new Vector2(Cx, Cz);
			Threads.AddRequest(()=>
			{
				if (LoadedChunks.ContainsKey(Cpos))
				{
					LoadedChunks[Cpos]?.Update();	
				}
				return null;
			});

			return Cpos;
		}

		void enforce_render_distance(Vector2 CurrentChunkPos)
		{
			lock (_chunkMutex)
			{
				List<Vector2> KeyList = new List<Vector2>(LoadedChunks.Keys);
				foreach (Vector2 Location in KeyList)
					if (Math.Abs(Location.x - CurrentChunkPos.x) > LoadRadius ||
						Math.Abs(Location.y - CurrentChunkPos.y) > LoadRadius)
						_unloadChunk((int) Location.x, (int) Location.y);
			}
		}

		void _unloadChunk(int Cx, int Cz)
		{
			Vector2 Cpos = new Vector2(Cx, Cz);
			lock (_chunkMutex)
			{
				if (LoadedChunks.ContainsKey(Cpos))
				{
					lock (_chunkMutex)
					{
						SaveFileHandler.WriteChunkData(LoadedChunks[Cpos].BlockData, LoadedChunks[Cpos].ChunkCoordinate, World);
						LoadedChunks[Cpos].QueueFree();
						LoadedChunks.Remove(Cpos);
					}	
				}
			}

		}

		public void update_player_pos(Vector2 NewPos)
		{
			_newChunkPos = NewPos;
		}

		void kill_thread()
		{
			_bKillThread = true;
		}
		
		public void SaveAndQuit()
		{
			if (GetTree() != null)
			{
				GetTree().Paused = true;	
			}
			lock (_chunkMutex)
            {
	            ConsoleLibrary.DebugPrint("Saving Chunks");


	            foreach (KeyValuePair<Vector2, ChunkCs> Chunk in LoadedChunks)
	            {
		            if (Chunk.Value.ChunkDirty)
		            {
			            Threads.AddRequest(() =>
			            {
				            SaveFileHandler.WriteChunkData(Chunk.Value.BlockData,
					            Chunk.Value.ChunkCoordinate, World);
				            return null; 
			            });	
		            }
	            }
	            
	            
	            // Hack: this needs to be corrected.
	            while (Threads.AllThreadsIdle() != true)
	            {
		            
	            }
	            CallDeferred("kill_thread");
            }

			if (GetTree() != null)
			{
				GetTree().Paused = false;
			}
		}
	}
}
