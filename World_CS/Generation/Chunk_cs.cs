using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using MinecraftClone.World_CS.Blocks;
using MinecraftClone.World_CS.Generation.Chunk_Generator_cs;
using Array = Godot.Collections.Array;
using Random = MinecraftClone.World_CS.Utility.JavaImports.Random;

namespace MinecraftClone.World_CS.Generation
{
	public class ChunkCs : Spatial
	{
		//bool NeedsSaved;

		public Vector2 ChunkCoordinate;

		public readonly Dictionary<Vector2, ChunkCs> NeighbourChunks = new Dictionary<Vector2, ChunkCs>();

		static readonly Random Rng = new Random();

		static readonly Vector2 TextureAtlasSize = new Vector2(8, 4);

		readonly SpatialMaterial _mat = (SpatialMaterial) GD.Load("res://assets/TextureAtlasMaterial.tres");

		readonly MeshInstance _blockMeshInstance = new MeshInstance();


		static readonly Vector3[] V =
		{
			new Vector3(0, 0, 0), //0
			new Vector3(1, 0, 0), //1
			new Vector3(0, 1, 0), //2
			new Vector3(1, 1, 0), //3
			new Vector3(0, 0, 1), //4
			new Vector3(1, 0, 1), //5
			new Vector3(0, 1, 1), //6
			new Vector3(1, 1, 1)  //7
		};

		static readonly int[] Top = {2, 3, 7, 6};
		static readonly int[] Bottom = {0, 4, 5, 1};
		static readonly int[] Left = {6, 4, 0, 2};
		static readonly int[] Right = {3, 1, 5, 7};
		static readonly int[] Front = {7, 5, 4, 6};
		static readonly int[] Back = {2, 0, 1, 3};
		static readonly int[] Cross1 = {3, 1, 4, 6};
		static readonly int[] Cross2 = {7, 5, 0, 2};
		static readonly int[] Cross3 = {6, 4, 1, 3};
		static readonly int[] Cross4 = {2, 0, 5, 7};

		public static readonly Vector3 Dimension = new Vector3(16, 384, 16);

		readonly BaseGenerator _generator = new ForestGenerator();

		public byte[] BlockData = new byte[(int) Dimension.x * (int) Dimension.y * (int) Dimension.z];
		public readonly bool[,,] VisibilityMask = new bool[(int) Dimension.x, (int) Dimension.y, (int) Dimension.z];
		public bool ChunkDirty;


		public void InstantiateChunk(ProcWorld W, int Cx, int Cz, Vector3 PosOffset, long Seed)
		{
			
			Translation = new Vector3(Cx * (Dimension.x), 0, Cz * Dimension.x)  - PosOffset;
			ChunkCoordinate = new Vector2(Cx, Cz);
			
			_generator.Generate(this, Cx, Cz, Seed);
			
		}
		
		public void Update()
		{
			Stopwatch watch = Stopwatch.StartNew();

			List<Vector3> blocks = new List<Vector3>();
			List<Vector3> blocksNormals = new List<Vector3>();
			List<Vector2>  uVs = new List<Vector2>();

			ArrayMesh blockArrayMesh = new ArrayMesh();
			
			
			//Making use of multidimensional arrays allocated on creation
			for (int z = 0; z < Dimension.z; z++)
			for (int y = 0; y < Dimension.y; y++)
			for (int x = 0; x < Dimension.x; x++)
			{
				byte block = BlockData[GetFlattenedDimension(x, y, z)];
				if (block == 0) continue;
				bool[] check = check_transparent_neighbours(x, y, z);
				if (check.Contains(true))
				{
					_create_block(check, x, y, z, block, blocks, blocksNormals, uVs);	
				}
			}

			Array meshInstance = new Array();
			meshInstance.Resize((int) ArrayMesh.ArrayType.Max);
			
			meshInstance[(int)ArrayMesh.ArrayType.Vertex] = blocks.ToArray(); 
			meshInstance[(int)ArrayMesh.ArrayType.TexUv] = uVs.ToArray();
			meshInstance[(int) ArrayMesh.ArrayType.Normal] = blocksNormals.ToArray();
			blockArrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, meshInstance);
			blockArrayMesh.SurfaceSetMaterial(0, _mat);
			
			_blockMeshInstance.Mesh = blockArrayMesh;
			
			//ConsoleLibrary.DebugPrint(watch.ElapsedMilliseconds);


		}


		public void UpdateVisMask()
		{
			for (int z = 0; z < Dimension.z; z++)
			for (int y = 0; y < Dimension.y; y++)
			for (int x = 0; x < Dimension.x; x++)
			{
				VisibilityMask[x,y,z] = BlockHelper.BlockTypes[BlockData[GetFlattenedDimension(x,y,z)]].Transparent;
			}
		}
		
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			_mat.AlbedoTexture.Flags = 2;
			
			AddChild(_blockMeshInstance);

		}

		public void _set_block_data(int X, int Y, int Z, byte B, bool Overwrite = true, bool UpdateSurrounding = false)
		{
			if (X >= 0 && X < Dimension.x && Y >= 0 && Y < Dimension.y && Z >= 0 && Z < Dimension.z)
			{
				if (!Overwrite && BlockData[GetFlattenedDimension(X, Y, Z)] != 0) return;
				BlockData[GetFlattenedDimension(X, Y, Z)] = B;

				VisibilityMask[X,Y,Z] = BlockHelper.BlockTypes[B].Transparent;
				ChunkDirty = true;
				//NeedsSaved = true;
			}
			else
			{
				//GD.Print("External Chunk Write");
				Vector3 worldCoordinates = new Vector3(X + Translation.x, Y, Z + Translation.z);
				int localX = (int) (Mathf.PosMod((float) Math.Floor(worldCoordinates.x), Dimension.x) + 0.5);
				int localY = (int) (Mathf.PosMod((float) Math.Floor(worldCoordinates.y), Dimension.y) + 0.5);
				int localZ = (int) (Mathf.PosMod((float) Math.Floor(worldCoordinates.z), Dimension.z) + 0.5);

				int cx = (int) Mathf.Floor(worldCoordinates.x / Dimension.x);
				int cz = (int) Mathf.Floor(worldCoordinates.z / Dimension.z);
				
				Vector2 chunkKey = new Vector2(cx, cz);
				if (NeighbourChunks.ContainsKey(chunkKey))
				{
					NeighbourChunks[chunkKey]._set_block_data(localX, localY, localZ, B, Overwrite);
				}
				else if(ProcWorld.instance.LoadedChunks.ContainsKey(chunkKey))
				{
					ChunkCs currentChunk = ProcWorld.instance.LoadedChunks[chunkKey];
					NeighbourChunks[chunkKey] = currentChunk;
					currentChunk?._set_block_data(localX,localY,localZ,B,Overwrite);
				}
			}
		}

		bool[] check_transparent_neighbours(int X, int Y, int Z)
		{
			return new[]
			{
				is_block_transparent(X, Y + 1, Z), is_block_transparent(X, Y - 1, Z), is_block_transparent(X - 1, Y, Z),
				is_block_transparent(X + 1, Y, Z), is_block_transparent(X, Y, Z - 1), is_block_transparent(X, Y, Z + 1)
			};
		}

		public static void _create_block(bool[] Check, int X, int Y, int Z, byte Block, List<Vector3> Blocks, List<Vector3> BlocksNormals, List<Vector2>  UVs)
		{
			List<BlockStruct> blockTypes = BlockHelper.BlockTypes;
			if (blockTypes[Block].TagsList.Contains("Flat"))
			{
				create_face(Cross1, X, Y, Z, blockTypes[Block].Only, Blocks, BlocksNormals, UVs);
				create_face(Cross2, X, Y, Z, blockTypes[Block].Only, Blocks, BlocksNormals, UVs);
				create_face(Cross3, X, Y, Z, blockTypes[Block].Only, Blocks, BlocksNormals, UVs);
				create_face(Cross4, X, Y, Z, blockTypes[Block].Only, Blocks, BlocksNormals, UVs);
			}
			else
			{
				if (Check[0]) create_face(Top, X, Y, Z, blockTypes[Block].Top, Blocks, BlocksNormals, UVs);
				if (Check[1]) create_face(Bottom, X, Y, Z, blockTypes[Block].Bottom, Blocks, BlocksNormals, UVs);
				if (Check[2]) create_face(Left, X, Y, Z, blockTypes[Block].Left, Blocks, BlocksNormals, UVs);
				if (Check[3]) create_face(Right, X, Y, Z, blockTypes[Block].Right, Blocks, BlocksNormals, UVs);
				if (Check[4]) create_face(Back, X, Y, Z, blockTypes[Block].Back, Blocks, BlocksNormals, UVs);
				if (Check[5]) create_face(Front, X, Y, Z, blockTypes[Block].Front, Blocks, BlocksNormals, UVs);
			}
		}

		static void create_face(IReadOnlyList<int> I, int X, int Y, int Z, Vector2 TextureAtlasOffset, List<Vector3> Blocks, List<Vector3> BlocksNormals, List<Vector2>  UVs)
		{
			Vector3 offset = new Vector3(X, Y, Z);

			Vector3 a = V[I[0]] + offset;
			Vector3 b = V[I[1]] + offset;
			Vector3 c = V[I[2]] + offset;
			Vector3 d = V[I[3]] + offset;

			Vector2 uvOffset = new Vector2(
				TextureAtlasOffset.x / TextureAtlasSize.x,
				TextureAtlasOffset.y / TextureAtlasSize.y
			);

			// the f means float, there is another type called double it defaults to that has better accuracy at the cost of being larger to store, but vector3 does not use it.
			Vector2 uvA = new Vector2(0f, 0f) + uvOffset;
			Vector2 uvB = new Vector2(0, 1.0f / TextureAtlasSize.y) + uvOffset;
			Vector2 uvC = new Vector2(1.0f / TextureAtlasSize.x, 1.0f / TextureAtlasSize.y) + uvOffset;
			Vector2 uvD = new Vector2(1.0f / TextureAtlasSize.x, 0) + uvOffset;
			
			Blocks.AddRange(new[] {a, b, c, a, c, d});

			UVs.AddRange(new[] {uvA, uvB, uvC, uvA, uvC, uvD});

			BlocksNormals.AddRange(NormalGenerate(a,b,c));
			BlocksNormals.AddRange(NormalGenerate(a,c,d));
		}
		
		static IEnumerable<Vector3> NormalGenerate(Vector3 A, Vector3 B, Vector3 C)
		{
			// HACK: Actually calculate normals as this only works for cubes

			Vector3 qr = C - A;
			Vector3 qs = B - A;

			Vector3 normal = new Vector3((qr.y * qs.z) - (qr.z * qs.y),(qr.z * qs.x) - (qr.x * qs.z), (qr.x * qs.y) - (qr.y * qs.x) );

			return new[] {normal, normal, normal};

		}


		public static int GetFlattenedDimension(int X, int Y, int Z)
		{
			return X + Y * (int)Dimension[0] + Z * (int)Dimension[1] * (int)Dimension[2];
		}

		bool is_block_transparent(int X, int Y, int Z)
		{
			if (X < 0 || X >= Dimension.x || Z < 0 || Z >= Dimension.z)
			{
				
				int cx = (int) Math.Floor(X / Dimension.x);
				int cz = (int) Math.Floor(Z / Dimension.x);

				int bx = (int) (Mathf.PosMod((float) Math.Floor((double)X), Dimension.x));
				int by = (int) (Mathf.PosMod((float) Math.Floor((double)Y), Dimension.y));
				int bz = (int) (Mathf.PosMod((float) Math.Floor((double)Z), Dimension.x));

				Vector2 cpos = new Vector2(cx, cz);


				if (ProcWorld.instance.LoadedChunks.ContainsKey(cpos))
				{
					return ProcWorld.instance.LoadedChunks[cpos].VisibilityMask[bx, by, bz];
				}
				return true;
			}

			if (Y < 0 || Y >= Dimension.y)
			{
				return true;	
			}

			return VisibilityMask[X,Y,Z];
		}
	}
}
