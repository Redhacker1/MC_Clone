using Godot;
using MinecraftClone.World_CS.Generation.Noise;

namespace MinecraftClone.World_CS.Generation.Chunk_Generator_cs
{
	internal class ForestGenerator : BaseGenerator
	{
		public override void generate_surface(ChunkCs Chunk, int Height, int X, int Z)
		{
			for (int I = 0; I < Height; I++)
			{
				Chunk._set_block_data(X,I,Z, 10);
			}
		}

		public override void GenerateTopsoil(ChunkCs Chunk, int Height, int X, int Z)
		{
			NoiseUtil HeightNoise = new NoiseUtil();
			HeightNoise.SetSeed(Chunk.Seed);
			HeightNoise.SetFractalOctaves(100);

            int Depth = (int) Mathf.Lerp(1,6,Mathf.Abs(HeightNoise.GetSimplex(X + Chunk.ChunkCoordinate.x,Chunk.Seed,Z + Chunk.ChunkCoordinate.y)));
            //GD.Print(depth);

            for (int I = 0; I < Depth; I++)
			{
				if (I == 0)
				{
					Chunk._set_block_data(X,Height, Z, 4);	
				}
				else
				{
					Chunk._set_block_data(X,Height - I, Z,4);	
				}
			}
		}

		public override void generate_details(ChunkCs Chunk, RandomNumberGenerator Rng, int[,] GroundHeight, bool CheckingInterChunkGen = true)
		{

			const int treeWidth = 2;

			for (int NTree = 0; NTree < Rng.RandiRange(2, 8); NTree++)
			{
				int PosX = Rng.RandiRange(treeWidth, (int) ChunkCs.Dimension.x - treeWidth - 1);
				int PosZ = Rng.RandiRange(treeWidth, (int) ChunkCs.Dimension.x - treeWidth - 1);
				int TreeHeight = Rng.RandiRange(4, 8);
				
				for (int I = 0; I < TreeHeight; I++)
				{

					int X = PosX;
					int Z = PosZ;
					
					int Y = GroundHeight[X,Z] + I;
					
					// 6 is BID for logs
					Chunk._set_block_data(X, Y, Z, 6);
				}
				int MinY = Rng.RandiRange(-2, -1);

				int MaxY = Rng.RandiRange(2, 4);

				for (int Dy = MinY; Dy < MaxY; Dy++)
				{
					int LeafWidth = treeWidth;
					if (Dy == MinY || Dy == MaxY - 1) LeafWidth -= 1;
					for (int Dx = -LeafWidth; Dx < LeafWidth + 1; Dx++)
					{
						for (int Dz = -LeafWidth; Dz < LeafWidth + 1; Dz++)
						{
							int Lx = PosX + Dx;
							int Ly = GroundHeight[PosX,PosZ] + TreeHeight + Dy;
							int Lz = PosZ + Dz;
							
							
							// 5 is block ID for leaves
							Chunk._set_block_data(Lx, Ly, Lz, 5, false);
						}

						if (Dy == MinY || Dy == MaxY - 1) LeafWidth -= 1;
					}
				}

				for (int NShrub = 0; NShrub < Rng.RandiRange(6, 10); NShrub++)
				{
					int X = Rng.RandiRange(0, (int)ChunkCs.Dimension.x - 1);
					int Z = Rng.RandiRange(0, (int)ChunkCs.Dimension.x - 1);
					int Y = GroundHeight[X,Z];
					
					// 11 is block ID for tall grass
					if (!IsBlockAir(Chunk, X, Y, Z))
					{
						Chunk._set_block_data(X, Y, Z, 11, false);	
					}
				}

				for (int NFlower = 0; NFlower < Rng.RandiRange(4, 6); NFlower++)
				{
					int X = Rng.RandiRange(0, (int)ChunkCs.Dimension.x - 1);
					int Z = Rng.RandiRange(0, (int)ChunkCs.Dimension.x - 1);
					int Y = GroundHeight[X,Z];
					
					// 3 is BID for flower
					if (!IsBlockAir(Chunk, X, Y, Z))
					{
						Chunk._set_block_data(X, Y, Z, 3, false);
					}
				}
			}
		}
	}
}
