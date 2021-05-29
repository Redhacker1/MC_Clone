using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using MinecraftClone.Debug_and_Logging;
using MinecraftClone.World_CS.Blocks;
using MinecraftClone.World_CS.Generation.Chunk_Generator_cs;

namespace MinecraftClone.World_CS.Generation
{
	public class ChunkCs : StaticBody
	{
		const int GenHeight = 60;
		const int BlockOffset = 0;
		
		public bool ChunkDirty = true;

		public Vector2 ChunkCoordinate;

		public readonly Dictionary<Vector2, ChunkCs> NeighbourChunks = new Dictionary<Vector2, ChunkCs>();

		static readonly RandomNumberGenerator Rng = new RandomNumberGenerator();

		static readonly Vector2 TextureAtlasSize = new Vector2(8, 4);

		readonly SpatialMaterial _mat = (SpatialMaterial) GD.Load("res://assets/TextureAtlasMaterial.tres");

		readonly MeshInstance _blockMeshInstance = new MeshInstance();
		readonly MeshInstance _foliageMeshInstance = new MeshInstance();

		readonly ConcavePolygonShape _collisionShapeClass = new ConcavePolygonShape();
		readonly CollisionShape _shape = new CollisionShape();


		static readonly Vector3[] V =
		{
			new Vector3(0, 0, 0), //0
			new Vector3(1, 0, 0), //1
			new Vector3(0, 1, 0), //2
			new Vector3(1, 1, 0), //3
			new Vector3(0, 0, 1), //4
			new Vector3(1, 0, 1), //5
			new Vector3(0, 1, 1), //6
			new Vector3(1, 1, 1) //7
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

		public ProcWorld World;
		public int Seed;
		

		public void Generate(ProcWorld W, float Cx, float Cz, Vector3 PosOffset)
		{
			
			Translation = new Vector3(Cx * (Dimension.x), 0, Cz * Dimension.x)  - PosOffset;
			ChunkCoordinate = new Vector2(Cx, Cz);

			World = W;
			Seed = (int) (Cx * 1000 + Cz);
			
			Rng.Seed = (ulong) Seed;

			int[,] GroundHeight = new int[(int)Dimension.x, (int)Dimension.x];

			for (int X = 0; X < Dimension.x; X++)
			{
				for (int Z = 0; Z < Dimension.x; Z++)
				{
					float HNoise = Mathf.Clamp((1f + World.HeightNoise.GetSimplex(X + Translation.x, Z + Translation.z))/2,  0, 1);
					int YHeight = (int) (HNoise * (GenHeight - 1) + 1) + BlockOffset;
					GroundHeight[X,Z] = YHeight;
					_generator.generate_surface(this ,GroundHeight[X,Z], X, Z);
					_generator.GenerateTopsoil(this ,GroundHeight[X,Z], X, Z);	
				}
			}
			_generator.generate_details(this, Rng, GroundHeight);
			_generator.Generate_Caves(this,Seed, GroundHeight);
		}

		// HUGE HACK: This restricts the method to only running on one thread at a time, this will make threadpooling this impossible later
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Update()
		{
			
			List<Vector3> Blocks = new List<Vector3>();
			List<Vector3> BlocksNormals = new List<Vector3>();
			List<Vector2>  UVs = new List<Vector2>();
			List<Vector3> CollisionData = new List<Vector3>();
			
			ArrayMesh BlockArrayMesh = new ArrayMesh();

			//Making use of multidimensional arrays allocated on creation, should speed up this process significantly
			for (int X = 0; X < Dimension.x; X++)
			for (int Y = 0; Y < Dimension.y; Y++)
			for (int Z = 0; Z < Dimension.z; Z++)
			{
				byte Block = BlockData[GetFlattenedDimension(X, Y, Z)];
				if (Block != 0)
				{
					bool[] Check = check_transparent_neighbours(X, Y, Z);
					if (Check.Contains(true))
					{
						_create_block(Check, X, Y, Z, Block, Blocks, BlocksNormals, UVs, CollisionData);	
					}
				}
			}
			
			Godot.Collections.Array MeshInstance = new Godot.Collections.Array();
			MeshInstance.Resize((int) ArrayMesh.ArrayType.Max);
			
			MeshInstance[(int)ArrayMesh.ArrayType.Vertex] = Blocks.ToArray();
			MeshInstance[(int)ArrayMesh.ArrayType.TexUv] = UVs.ToArray();
			MeshInstance[(int) ArrayMesh.ArrayType.Normal] = BlocksNormals.ToArray();
			BlockArrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshInstance);
			BlockArrayMesh.SurfaceSetMaterial(0, _mat);
			
			_blockMeshInstance.Mesh = BlockArrayMesh;


			PhysicsServer.ShapeSetData(_collisionShapeClass.GetRid(),CollisionData.ToArray());

			//ConsoleLibrary.DebugPrint(stopwatch.ElapsedMilliseconds.ToString());
			
		}
		
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			_mat.AlbedoTexture.Flags = 2;
			
			AddChild(_foliageMeshInstance);
			AddChild(_blockMeshInstance);
			AddChild(_shape);
			_shape.Shape = _collisionShapeClass;
		}

		public void _set_block_data(int X, int Y, int Z, byte B, bool Overwrite = true, bool UpdateSurrounding = false)
		{
			if (X >= 0 && X < Dimension.x && Y >= 0 && Y < Dimension.y && Z >= 0 && Z < Dimension.z)
			{
				if (Overwrite || BlockData[GetFlattenedDimension(X, Y, Z)] == 0)
				{
					BlockData[GetFlattenedDimension(X, Y, Z)] = B;
					ChunkDirty = true;
				}
			}
			else
			{
				//GD.Print("External Chunk Write");
				Vector3 WorldCoordinates = new Vector3(X + Translation.x, Y, Z + Translation.z);
				int LocalX = (int) (Mathf.PosMod(Mathf.Floor(WorldCoordinates.x), Dimension.x) + 0.5);
				int LocalY = (int) (Mathf.PosMod(Mathf.Floor(WorldCoordinates.y), Dimension.y) + 0.5);
				int LocalZ = (int) (Mathf.PosMod(Mathf.Floor(WorldCoordinates.z), Dimension.z) + 0.5);

				int Cx = (int) Mathf.Floor(WorldCoordinates.x / Dimension.x);
				int Cz = (int) Mathf.Floor(WorldCoordinates.z / Dimension.z);
				
				Vector2 ChunkKey = new Vector2(Cx, Cz);
				if (NeighbourChunks.ContainsKey(ChunkKey))
				{
					NeighbourChunks[ChunkKey]._set_block_data(LocalX, LocalY, LocalZ, B, Overwrite);
				}
				else if(World.LoadedChunks.ContainsKey(ChunkKey))
				{
					ChunkCs CurrentChunk = World.LoadedChunks[ChunkKey];
					NeighbourChunks[ChunkKey] = CurrentChunk;
					CurrentChunk?._set_block_data(LocalX,LocalY,LocalZ,B,Overwrite);
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

		public static void _create_block(bool[] Check, int X, int Y, int Z, byte Block, List<Vector3> Blocks, List<Vector3> BlocksNormals, List<Vector2>  UVs, List<Vector3> CollisionData)
		{
			bool NoCollision = BlockHelper.block_have_collision(Block);
			List<BlockStruct> BlockTypes = BlockHelper.BlockTypes;
			if (BlockTypes[Block].TagsList.Contains("Flat"))
			{
				create_face(Cross1, X, Y, Z, BlockTypes[Block].Only, NoCollision, Blocks, BlocksNormals, UVs, CollisionData);
				create_face(Cross2, X, Y, Z, BlockTypes[Block].Only, NoCollision, Blocks, BlocksNormals, UVs, CollisionData);
				create_face(Cross3, X, Y, Z, BlockTypes[Block].Only, NoCollision, Blocks, BlocksNormals, UVs, CollisionData);
				create_face(Cross4, X, Y, Z, BlockTypes[Block].Only, NoCollision, Blocks, BlocksNormals, UVs, CollisionData);
			}
			else
			{
				if (Check[0]) create_face(Top, X, Y, Z, BlockTypes[Block].Top, NoCollision, Blocks, BlocksNormals, UVs, CollisionData);
				if (Check[1]) create_face(Bottom, X, Y, Z, BlockTypes[Block].Bottom, NoCollision, Blocks, BlocksNormals, UVs, CollisionData);
				if (Check[2]) create_face(Left, X, Y, Z, BlockTypes[Block].Left, NoCollision, Blocks, BlocksNormals, UVs, CollisionData);
				if (Check[3]) create_face(Right, X, Y, Z, BlockTypes[Block].Right, NoCollision, Blocks, BlocksNormals, UVs, CollisionData);
				if (Check[4]) create_face(Back, X, Y, Z, BlockTypes[Block].Back, NoCollision, Blocks, BlocksNormals, UVs, CollisionData);
				if (Check[5]) create_face(Front, X, Y, Z, BlockTypes[Block].Front, NoCollision, Blocks, BlocksNormals, UVs, CollisionData);
			}
		}

		static void create_face(IReadOnlyList<int> I, int X, int Y, int Z, Vector2 TextureAtlasOffset,
			bool NoCollision, List<Vector3> Blocks, List<Vector3> BlocksNormals, List<Vector2>  UVs, List<Vector3> CollisionData)
		{
			Vector3 Offset = new Vector3(X, Y, Z);

			Vector3 A = V[I[0]] + Offset;
			Vector3 B = V[I[1]] + Offset;
			Vector3 C = V[I[2]] + Offset;
			Vector3 D = V[I[3]] + Offset;

			Vector2 UvOffset = new Vector2(
				TextureAtlasOffset.x / TextureAtlasSize.x,
				TextureAtlasOffset.y / TextureAtlasSize.y
			);

			// the f means float, there is another type called double it defaults to that has better accuracy at the cost of being larger to store, but vector3 does not use it.
			Vector2 UvA = new Vector2(0f, 0f) + UvOffset;
			Vector2 UvB = new Vector2(0, 1.0f / TextureAtlasSize.y) + UvOffset;
			Vector2 UvC = new Vector2(1.0f / TextureAtlasSize.x, 1.0f / TextureAtlasSize.y) + UvOffset;
			Vector2 UvD = new Vector2(1.0f / TextureAtlasSize.x, 0) + UvOffset;
			
			Blocks.AddRange(new[] {A, B, C});
			Blocks.AddRange(new[] {A, C, D});

			UVs.AddRange(new[] {UvA, UvB, UvC});
			UVs.AddRange(new[] {UvA, UvC, UvD});
				
			BlocksNormals.AddRange(NormalGenerate(A,B,C));
			BlocksNormals.AddRange(NormalGenerate(A,B,C));
			
			if (!NoCollision)
			{
				CollisionData?.AddRange(new List<Vector3>() {A, B, C});
				CollisionData?.AddRange(new List<Vector3>() {A, C, D});
			}
		}
		
		static IEnumerable<Vector3> NormalGenerate(Vector3 A, Vector3 B, Vector3 C)
		{
			// HACK: Actually calculate normals this only works for cubes
			Plane Vertexplane = new Plane(A, B, C);
			return new[] {Vertexplane.Normal, Vertexplane.Normal, Vertexplane.Normal};

		}


		public static int GetFlattenedDimension(int X, int Y, int Z)
		{
			return X + Y * (int)Dimension[0] + Z * (int)Dimension[1] * (int)Dimension[2];
		}

		bool is_block_transparent(int X, int Y, int Z)
		{
			if (X < 0 || X >= Dimension.x || Z < 0 || Z >= Dimension.z || Y < 0 || Y >= Dimension.y)
			{
				return true;
			}

			return BlockHelper.BlockTypes[BlockData[GetFlattenedDimension(X, Y, Z)]].Transparent;
		}
	}
}
