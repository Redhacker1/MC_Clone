/* TODO: This is starting to become a SuperClass with catch-all functionality, might be best to seperate it out.
	Might be best to move some of the more chunk oriented methods into the chunkCS class that do not use the chunk class statically.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MinecraftClone.Debug_and_Logging;
using MinecraftClone.World_CS.Blocks;
using MinecraftClone.World_CS.Generation.Noise;
using MinecraftClone.World_CS.Utility.Debug;
using MinecraftClone.World_CS.Utility.IO;
using MinecraftClone.World_CS.Utility.Threading;
using AABB = MinecraftClone.World_CS.Utility.Physics.AABB;
using Thread = System.Threading.Thread;

namespace MinecraftClone.World_CS.Generation
{
	public class ProcWorld : Spatial
	{
		
		ThreadPoolClass _threads = new ThreadPoolClass();
		
		// Max chunks radius comes out to (_loadRadius*2)^2 
		int _loadRadius = 16;

		readonly object _chunkMutex = new object(); 
		
		public WorldData World;

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

		public static DebugLines lines;

		public override void _Ready()
		{
			
			ConsoleLibrary.DebugPrint("Starting procworld");
			
			ConsoleLibrary.DebugPrint("Preparing Threadpool");
			// Starts the threadpool;
			_threads.InitializePool();
			_threads.IgniteThreadPool();
			
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
			
			// Add debugLines to scene
			lines = new DebugLines();
			AddChild(lines);

			ConsoleLibrary.DebugPrint("Binding Console Commands");
			// Console Binds
			ConsoleLibrary.BindCommand("reload_chunks", "reloads all currently loaded chunks", "reload_chunks", ReloadChunks, false);
			ConsoleLibrary.BindCommand("reset", "Reloads world after saving, ","reset", restart, false);
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
							if (_currentLoadRadius < _loadRadius)
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
							if (DeltaPos.y < _loadRadius)
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

				if (C != null)
				{
					AddChild(C);
				}
				C?.UpdateVisMask();
				_update_chunk(Cx, Cz);
			}
			return Cpos;
		}

		public string ReloadChunks(params string[] Args)
		{
			Vector2[] Chunks = LoadedChunks.Keys.ToArray();

			foreach (Vector2 ChunkPos in Chunks)
			{
				update_player_pos(ChunkPos);
			}
			return $"{Chunks.Length} Chunks sent to threadpool for processing...";
		}


		public List<AABB> Get_aabbs(int layer, AABB Aabb)
		{
			List<AABB> aabbs = new List<AABB>();
			Vector3 a = new Vector3(Aabb.MinLoc.x - 1, Aabb.MinLoc.y - 1, Aabb.MinLoc.z - 1);
			Vector3 b = new Vector3(Aabb.MaxLoc.x, Aabb.MaxLoc.y, Aabb.MaxLoc.z);

			for (int z = (int) a.z; z < b.z; z++)
			{
				for (int y = (int) a.y; y < b.y; y++)
				{
					for (int x = (int) a.x; x < b.x; x++)
					{
						byte block = GetBlockIdFromWorldPos(x, y, z);
						if (BlockHelper.BlockTypes[block].NoCollision || block == 0) continue;
						AABB c = new AABB(new Vector3(x, y, z), new Vector3(x + 1, y + 1, z + 1));
						aabbs.Add(c);
						
						c.DrawDebug();
					}
				}
			}
			return aabbs;
		}


		void DebugLine(Vector3 A, Vector3 B)
		{
			lines.Drawline(A, B, Colors.Red);
		}


		byte GetBlockIdFromWorldPos(int X, int Y, int Z)
		{
			
			int Cx = (int) Math.Floor(X / ChunkCs.Dimension.x);
			int Cz = (int) Math.Floor(Z / ChunkCs.Dimension.x);

			int Bx = (int) (Mathf.PosMod(X, ChunkCs.Dimension.x));
			int By = (int) (Mathf.PosMod(Y, ChunkCs.Dimension.y));
			int Bz = (int) (Mathf.PosMod(Z, ChunkCs.Dimension.x));

			if (LoadedChunks.ContainsKey(new Vector2(Cx, Cz)) && ValidPlace(Bx, Y, Bz))
			{

				return LoadedChunks[new Vector2(Cx, Cz)].BlockData[ChunkCs.GetFlattenedDimension(Bx, By, Bz)];
			}

			return 0;
		}
		
		
		/// <summary>
		/// Checks to ensure block is inside chunk, NOTE: Takes a relative position and does not account for validity of the chunk itself
		/// </summary>
		/// <param name="X"></param>
		/// <param name="Y"></param>
		/// <param name="Z"></param>
		/// <returns>whether it is safe to write or read from the block in the chunk</returns>
		static bool ValidPlace(int X, int Y, int Z)
		{
			if (X < 0 || X >= ChunkCs.Dimension.x || Z < 0 || Z >= ChunkCs.Dimension.z || Y < 0 || Y >= ChunkCs.Dimension.y)
			{
				return false;
			}

			return true;
		}
		
		
		

		public void change_block(int Cx, int Cz, int Bx, int By, int Bz, byte T)
		{
			ChunkCs c;
			lock (_chunkMutex)
			{
				c = LoadedChunks[new Vector2(Cx, Cz)];
			}

			if (c.BlockData[ChunkCs.GetFlattenedDimension(Bx, By, Bz)] == T) return;
			ConsoleLibrary.DebugPrint($"Changed block at {Bx} {By} {Bz} in chunk {Cx}, {Cz}");
			c?._set_block_data(Bx,By,Bz,T);
			_update_chunk(Cx, Cz);
		}

		Vector2 _update_chunk(int Cx, int Cz)
		{
			Vector2 Cpos = new Vector2(Cx, Cz);

			_threads.AddRequest(() =>
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
					if (Math.Abs(Location.x - CurrentChunkPos.x) > _loadRadius ||
						Math.Abs(Location.y - CurrentChunkPos.y) > _loadRadius)
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
			
			_threads.ShutDownHandler();
		}

		string restart(params string[] parameters)
		{
			// Shuts down the old threadpool and saves the game state.
			SaveAndQuit();

			if ( GetTree()  != null)
			{

				GetTree().ChangeSceneTo(GD.Load<PackedScene>("res://Scenes/Spatial.tscn"));
			}
			
			
			return "Restarting...";
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
			            _threads.AddRequest(() =>
			            {
				            SaveFileHandler.WriteChunkData(Chunk.Value.BlockData,
					            Chunk.Value.ChunkCoordinate, World);

				            return null; 
			            });	
		            }
	            }
	            
	            
	            // Hack: this needs to be corrected, probably doable with a monitor.Lock() and then a callback to evaluate the  
	            while (_threads.AllThreadsIdle() != true)
	            {
		            
	            }
	            kill_thread();
	            
	            
            }

			if (GetTree() != null)
			{
				GetTree().Paused = false;
			}
		}
	}
}
