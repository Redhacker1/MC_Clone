using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using MinecraftClone.Debug_and_Logging;
using MinecraftClone.Utility;
using MinecraftClone.Utility.CoreCompatibility;
using MinecraftClone.World_CS.Blocks;
using MinecraftClone.World_CS.Generation.Chunk_Generator_cs;
using MinecraftClone.World_CS.Utility;
using Array = Godot.Collections.Array;
using Random = MinecraftClone.Utility.JavaImports.Random;

//#define Core
#if(Core)
	using System.Numerics;

#else
	using Godot;
#endif

using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace MinecraftClone.World_CS.Generation
{
	#if(Core)
	public class ChunkCs
	#else
	public class ChunkCs : Spatial
	#endif
	{
		//bool NeedsSaved;

		public Vector2 ChunkCoordinate;

		public readonly Dictionary<Vector2, ChunkCs> NeighbourChunks = new Dictionary<Vector2, ChunkCs>();
		

		static readonly Vector2 TextureAtlasSize = new Vector2(8, 4);
			
		static readonly float sizex = 1.0f / TextureAtlasSize.X;
		static readonly float sizey = 1.0f / TextureAtlasSize.Y;
		
		#if !Core
		static readonly SpatialMaterial _mat = (SpatialMaterial) GD.Load("res://assets/TextureAtlasMaterial.tres");

		readonly MeshInstance _blockMeshInstance = new MeshInstance();
		#endif
		
		
		#if Core
		Vector3 Translation;
		#endif


		static readonly Godot.Vector3[] V =
		{
			new Godot.Vector3(0, 0, 0), //0
			new Godot.Vector3(1, 0, 0), //1
			new Godot.Vector3(0, 1, 0), //2
			new Godot.Vector3(1, 1, 0), //3
			new Godot.Vector3(0, 0, 1), //4
			new Godot.Vector3(1, 0, 1), //5
			new Godot.Vector3(0, 1, 1), //6
			new Godot.Vector3(1, 1, 1)  //7
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
		static readonly BaseGenerator _generator = new ForestGenerator();

		public byte[] BlockData = new byte[(int) Dimension.X * (int) Dimension.Y * (int) Dimension.Z];
		readonly bool[,,] VisibilityMask = new bool[(int) Dimension.X, (int) Dimension.Y, (int) Dimension.Z];
		public bool ChunkDirty;


		public void InstantiateChunk(ProcWorld W, int Cx, int Cz, long Seed)
		{
			
			Translation = new Vector3(Cx * (Dimension.X), 0, Cz * Dimension.Z).CastToGodot();
			ChunkCoordinate = new Vector2(Cx, Cz);
			
			_generator.Generate(this, Cx, Cz, Seed);
			
		}
		
		public void Update()
		{

			List<Godot.Vector3> blocks = new List<Godot.Vector3>();
			List<Godot.Vector3> blocksNormals = new List<Godot.Vector3>();
			List<Godot.Vector2>  uVs = new List<Godot.Vector2>();

			//Making use of multidimensional arrays allocated on creation
			
			Stopwatch watch = Stopwatch.StartNew();
			for (int z = 0; z < Dimension.Z; z++)
			for (int y = 0; y < Dimension.Y; y++)
			for (int x = 0; x < Dimension.X; x++)
			{
				byte block = BlockData[GetFlattenedDimension(x, y, z)];
				if (block == 0) continue;
				bool[] check = check_transparent_neighbours(x, y, z);
				if (check.Contains(true))
				{
					_create_block(check, x, y, z, block, blocks, blocksNormals, uVs);	
				}
			}

			
		#if !Core
			ArrayMesh blockArrayMesh = new ArrayMesh();

			Array meshInstance = new Array();
			meshInstance.Resize((int) ArrayMesh.ArrayType.Max);
			
			meshInstance[(int)ArrayMesh.ArrayType.Vertex] = blocks.ToArray(); 
			meshInstance[(int)ArrayMesh.ArrayType.TexUv] = uVs.ToArray();
			meshInstance[(int)ArrayMesh.ArrayType.Normal] = blocksNormals.ToArray();
			blockArrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, meshInstance);
			blockArrayMesh.SurfaceSetMaterial(0, _mat);
			
			_blockMeshInstance.Mesh = blockArrayMesh;
			
		#else
			// Do Render stuff here with OpenGL
		#endif
			ConsoleLibrary.DebugPrint(watch.ElapsedMilliseconds);

		}


		public void UpdateVisMask()
		{
			for (int z = 0; z < Dimension.Z; z++)
			for (int y = 0; y < Dimension.Y; y++)
			for (int x = 0; x < Dimension.X; x++)
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
			if (X >= 0 && X < Dimension.X && Y >= 0 && Y < Dimension.Y && Z >= 0 && Z < Dimension.Z)
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
				int localX = (int) (MathHelper.Modulo((float) Math.Floor(worldCoordinates.X), Dimension.X) + 0.5);
				int localY = (int) (MathHelper.Modulo((float) Math.Floor(worldCoordinates.Y), Dimension.Y) + 0.5);
				int localZ = (int) (MathHelper.Modulo((float) Math.Floor(worldCoordinates.Z), Dimension.Z) + 0.5);

				int cx = (int) Math.Floor(worldCoordinates.X / Dimension.X);
				int cz = (int) Math.Floor(worldCoordinates.Z / Dimension.Z);
				
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

		public static void _create_block(bool[] Check, int X, int Y, int Z, byte Block, List<Godot.Vector3> Blocks, List<Godot.Vector3> BlocksNormals, List<Godot.Vector2>  UVs)
		{
			List<BlockStruct> blockTypes = BlockHelper.BlockTypes;
			Vector3 coord = new Vector3(X, Y, Z);
			if (blockTypes[Block].TagsList.Contains("Flat"))
			{
				var tempvar = blockTypes[Block].Only.CastToGodot();
				create_face(Cross1, ref coord, tempvar, Blocks, BlocksNormals, UVs);
				create_face(Cross2, ref coord, tempvar, Blocks, BlocksNormals, UVs);
				create_face(Cross3, ref coord, tempvar, Blocks, BlocksNormals, UVs);
				create_face(Cross4, ref coord, tempvar, Blocks, BlocksNormals, UVs);
			}
			else
			{
				if (Check[0]) create_face(Top, ref coord, blockTypes[Block].Top.CastToGodot(), Blocks, BlocksNormals, UVs);
				if (Check[1]) create_face(Bottom, ref coord, blockTypes[Block].Bottom.CastToGodot(), Blocks, BlocksNormals, UVs);
				if (Check[2]) create_face(Left, ref coord, blockTypes[Block].Left.CastToGodot(), Blocks, BlocksNormals, UVs);
				if (Check[3]) create_face(Right, ref coord, blockTypes[Block].Right.CastToGodot(), Blocks, BlocksNormals, UVs);
				if (Check[4]) create_face(Back, ref coord, blockTypes[Block].Back.CastToGodot(), Blocks, BlocksNormals, UVs);
				if (Check[5]) create_face(Front, ref coord, blockTypes[Block].Front.CastToGodot(), Blocks, BlocksNormals, UVs);
			}
		}
		
		static void create_face(IReadOnlyList<int> I, ref Vector3 offset, Godot.Vector2 TextureAtlasOffset, List<Godot.Vector3> Blocks, List<Godot.Vector3> BlocksNormals, List<Godot.Vector2>  UVs)
		{

			Godot.Vector3 a = V[I[0]] + offset.CastToGodot();
			Godot.Vector3 b = V[I[1]] + offset.CastToGodot();
			Godot.Vector3 c = V[I[2]] + offset.CastToGodot();
			Godot.Vector3 d = V[I[3]] + offset.CastToGodot();

			Godot.Vector2 uvOffset = new Godot.Vector2(
				TextureAtlasOffset.x / TextureAtlasSize.X,
				TextureAtlasOffset.y / TextureAtlasSize.Y
			);

			// the f means float, there is another type called double it defaults to that has better accuracy at the cost of being larger to store, but vector3 does not use it.
			Godot.Vector2 uvB = new Godot.Vector2(uvOffset.x, sizey + uvOffset.y);
			Godot.Vector2 uvC = new Godot.Vector2(sizex, sizey) + uvOffset;
			Godot.Vector2 uvD = new Godot.Vector2(sizex + uvOffset.x, uvOffset.y);
			
			Blocks.AddRange(new[] {a, b, c, a, c, d});

			UVs.AddRange(new[] {uvOffset, uvB, uvC, uvOffset, uvC, uvD});

			BlocksNormals.AddRange(NormalGenerate(a, b, c, d));
		}
		
		static IEnumerable<Godot.Vector3> NormalGenerate(Godot.Vector3 A, Godot.Vector3 B, Godot.Vector3 C, Godot.Vector3 D)
		{
			// HACK: Actually calculate normals as this only works for cubes

			Godot.Vector3 qr = C - A;
			Godot.Vector3 qs = B - A;

			Godot.Vector3 normal = new Godot.Vector3((qr.y * qs.z) - (qr.z * qs.y),(qr.z * qs.x) - (qr.x * qs.z), (qr.x * qs.y) - (qr.y * qs.x) );

			return new[] {normal, normal, normal, normal, normal, normal};

		}
		
		public static int GetFlattenedDimension(int X, int Y, int Z)
		{
			return X + Y * (int)Dimension.Z + Z * (int)Dimension.Y * (int)Dimension.X;
		}
		
		bool is_block_transparent(int X, int Y, int Z)
		{
			if (X < 0 || X >= Dimension.X || Z < 0 || Z >= Dimension.Z)
			{
				
				int cx = (int) Math.Floor(X / Dimension.X);
				int cz = (int) Math.Floor(Z / Dimension.X);

				int bx = (int) (MathHelper.Modulo((float) Math.Floor((double) X), Dimension.X));
				int bz = (int) (MathHelper.Modulo((float) Math.Floor((double) Z), Dimension.X));

				Vector2 cpos = new Vector2(cx, cz);


				if (ProcWorld.instance.LoadedChunks.ContainsKey(cpos))
				{
					return ProcWorld.instance.LoadedChunks[cpos].VisibilityMask[bx, Y, bz];
				}
				return false;
			}

			if (Y < 0 || Y >= Dimension.Y)
			{
				return false;	
			}

			return VisibilityMask[X,Y,Z];
		}
	}
}
