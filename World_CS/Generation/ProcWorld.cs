/* TODO: This is starting to become a SuperClass with catch-all functionality, might be best to separate it out.
	Might be best to move some of the more chunk oriented methods into the chunkCS class that do not use the chunk class statically.
 */

//#define Core

#if Core
	// Dependencies used in .net Core exclusively
	using MinecraftClone.Utility.Physics;
	using System.Threading;
	using System.Numerics;
#else
	// Dependencies used in Godot standard library exclusively
	using AABB = MinecraftClone.Utility.Physics.AABB;
	using Thread = System.Threading.Thread;
	using Godot;
#endif
	// Dependencies used Regardless
	using  Vector2 = System.Numerics.Vector2;
	using Vector3 = System.Numerics.Vector3;
	using Random = MinecraftClone.Utility.JavaImports.Random;
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using MinecraftClone.Debug_and_Logging;
	using MinecraftClone.Utility;
	using MinecraftClone.Utility.CoreCompatibility;
	using MinecraftClone.Utility.IO;
	using MinecraftClone.Utility.Threading;
	using MinecraftClone.World_CS.Blocks;
	using MinecraftClone.World_CS.Utility;

namespace MinecraftClone.World_CS.Generation
{
	#if Core
	public class ProcWorld
	#else
	public class ProcWorld : Spatial
	#endif
	{

		public static ProcWorld instance;

		readonly ThreadPoolClass _threads = new ThreadPoolClass();
		
		// Max chunks radius comes out to (_loadRadius*2)^2 
		readonly int _loadRadius = 16;

		public static Random WorldRandom;
		public static long WorldSeed;
		
		public WorldData World;

		public readonly ConcurrentDictionary<Vector2, ChunkCs> LoadedChunks = new ConcurrentDictionary<Vector2, ChunkCs>();

		bool _bKillThread;
		Vector2 _chunkPos; 
		Vector2 _currentChunkPos;
		int _currentLoadRadius;
		Vector2 _lastChunk;

		Vector2 _newChunkPos;
		

		Thread _terrainThread;

		public ProcWorld(long seed)
		{
			WorldSeed = seed;
			WorldRandom = new Random(seed); 
		}
		
		#if Core
		public void _Ready()
		#else
		public override void _Ready()
		#endif
		{
			if (instance != null)
				return;
			instance = this;

			ConsoleLibrary.DebugPrint("Starting procworld");
			
			ConsoleLibrary.DebugPrint("Preparing Threadpool");
			// Starts the threadpool;
			_threads.InitializePool();
			_threads.IgniteThreadPool();
			
			ConsoleLibrary.DebugPrint("Registering Blocks");
			// Sets the blocks used in the base game up.
			BlockHelper.RegisterBaseBlocks();
			
			ConsoleLibrary.DebugPrint("Creating Terrain Gen thread");
			// Preparing static terrain thread 
			_terrainThread = new Thread(_thread_gen);
			_terrainThread.Start();

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
					_lastChunk = _load_chunk((int) _currentChunkPos.X, (int) _currentChunkPos.Y);
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
						_lastChunk = _load_chunk((int) _lastChunk.X, (int) _lastChunk.Y + 1);
					}
					else if (DeltaPos.X < DeltaPos.Y)
					{
						// Either go right or up
						// Prioritize going right
						if ((DeltaPos.Y == _currentLoadRadius) & (-DeltaPos.X != _currentLoadRadius))
						{
							//Go right
							_lastChunk = _load_chunk((int)_lastChunk.X - 1, (int) _lastChunk.Y);
						}
						// Either moving in constant x or we just reached bottom right. Addendum by donovan: this looping on the X axis has happened to me actually
						else if ((-DeltaPos.X == _currentLoadRadius) | (-DeltaPos.X == DeltaPos.Y))
						{
							// Go up
							_lastChunk = _load_chunk((int) _lastChunk.X, (int) _lastChunk.Y - 1);
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
						if ((-DeltaPos.Y == _currentLoadRadius) & (DeltaPos.X != _currentLoadRadius))
						{
							//Go left
							_lastChunk = _load_chunk((int) _lastChunk.X + 1, (int) _lastChunk.Y);	
						}
						else if ((DeltaPos.X == _currentLoadRadius) | (DeltaPos.X == -DeltaPos.Y))
						{
							// Go down
							// Stop the last one where we'd go over the limit
							if (DeltaPos.Y < _loadRadius)
							{
								_lastChunk = _load_chunk((int) _lastChunk.X, (int) _lastChunk.Y + 1);
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
			LoadChunk = !LoadedChunks.ContainsKey(Cpos);

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
					C.InstantiateChunk(this, Cx, Cz, WorldSeed);	
				}
				
				LoadedChunks[Cpos] = C;

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
			IEnumerable<Vector2> Chunks = LoadedChunks.Keys;

			foreach (Vector2 ChunkPos in Chunks)
			{
				update_player_pos(ChunkPos);
			}
			return $"{Chunks.Count()} Chunks sent to threadpool for processing...";
		}


		public List<AABB> Get_aabbs(int layer, AABB Aabb)
		{
			List<AABB> aabbs = new List<AABB>();
			Vector3 a = new Vector3(Aabb.MinLoc.X - 1, Aabb.MinLoc.Y - 1, Aabb.MinLoc.Z - 1);
			Vector3 b = new Vector3(Aabb.MaxLoc.X, Aabb.MaxLoc.Y, Aabb.MaxLoc.Z);

			for (int z = (int) a.Z; z < b.Z; z++)
			{
				for (int y = (int) a.Y; y < b.Y; y++)
				{
					for (int x = (int) a.X; x < b.X; x++)
					{
						byte block = GetBlockIdFromWorldPos(x, y, z);
						if (BlockHelper.BlockTypes[block].NoCollision || block == 0) continue;
						AABB c = new AABB(new Vector3(x, y, z), new Vector3(x + 1, y + 1, z + 1));
						aabbs.Add(c);
					}
				}
			}
			return aabbs;
		}


		void DebugLine(Vector3 A, Vector3 B)
		{
			WorldScript.lines.Drawline(A.CastToGodot(), B.CastToGodot(), Colors.Red);
		}


		public byte GetBlockIdFromWorldPos(int X, int Y, int Z)
		{
			
			int cx = (int) Math.Floor(X / ChunkCs.Dimension.X);
			int cz = (int) Math.Floor(Z / ChunkCs.Dimension.X);

			int bx = (int) (MathHelper.Modulo((float) Math.Floor((double)X), ChunkCs.Dimension.X));
			int bz = (int) (MathHelper.Modulo((float) Math.Floor((double)Z), ChunkCs.Dimension.Z));

			Vector2 chunkpos = new Vector2(cx, cz);

			if (LoadedChunks.ContainsKey(chunkpos) && ValidPlace(bx, Y, bz))
			{
				return LoadedChunks[chunkpos].BlockData[ChunkCs.GetFlattenedDimension(bx, Y, bz)];
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
		public static bool ValidPlace(int X, int Y, int Z)
		{
			if (X < 0 || X >= ChunkCs.Dimension.X || Z < 0 || Z >= ChunkCs.Dimension.Z)
			{
				return false;
			}

			if(Y < 0 || Y > ChunkCs.Dimension.Y - 1)
			{
				return false;
			}

			return true;
		}
		
		
		

		public void change_block(int Cx, int Cz, int Bx, int By, int Bz, byte T)
		{
			ChunkCs c = LoadedChunks[new Vector2(Cx, Cz)];

			if (c.BlockData[ChunkCs.GetFlattenedDimension(Bx, By, Bz)] == T) return;
			ConsoleLibrary.DebugPrint($"Changed block at {Bx} {By} {Bz} in chunk {Cx}, {Cz}");
			c?._set_block_data(Bx,By,Bz,T);
			_update_chunk(Cx, Cz);
		}

		void _update_chunk(int Cx, int Cz)
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
		}

		void enforce_render_distance(Vector2 CurrentChunkPos)
		{
			List<Vector2> KeyList = new List<Vector2>(LoadedChunks.Keys);
			foreach (Vector2 Location in KeyList)
				if (Math.Abs(Location.X - CurrentChunkPos.X) > _loadRadius ||
				    Math.Abs(Location.Y - CurrentChunkPos.Y) > _loadRadius)
					_unloadChunk((int) Location.X, (int) Location.Y);
		}

		void _unloadChunk(int Cx, int Cz)
		{
			Vector2 Cpos = new Vector2(Cx, Cz);
			if (LoadedChunks.ContainsKey(Cpos))
			{
				SaveFileHandler.WriteChunkData(LoadedChunks[Cpos].BlockData, LoadedChunks[Cpos].ChunkCoordinate, World);
				LoadedChunks[Cpos].QueueFree();
				LoadedChunks.TryRemove(Cpos, out _);
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
			var tree = GetTree();
			if (tree != null)
			{
				tree.Paused = true;	
			}
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
	            
	            
			// Hack: this needs to be corrected, probably doable with a monitor.Lock() and then a callback to evaluate the END
			while (_threads.AllThreadsIdle() != true)
			{
		            
			}
			kill_thread();

			if (tree != null)
			{
				tree.Paused = false;
			}
		}
	}
}
