using System;
using Godot;
using MinecraftClone.World_CS.Generation.Noise;
using Random = MinecraftClone.World_CS.Utility.JavaImports.Random;

namespace MinecraftClone.World_CS.Generation.Chunk_Generator_cs
{
	internal class ForestGenerator : BaseGenerator
	{
		public override void generate_surface(ChunkCs Chunk, int Height, int X, int Z)
		{
			for (int Y = 0; Y < Height; Y++)
			{
				//GD.Print($"{X},{Y},{Z}");
				Chunk._set_block_data(X,Y,Z, 10);
			}
		}

		public override void GenerateTopsoil(ChunkCs Chunk, int Height, int X, int Z, long seed)
		{	NoiseUtil HeightNoise = new NoiseUtil();
			HeightNoise.SetSeed(seed);
			HeightNoise.SetFractalOctaves(100);


			float noise = HeightNoise.GetSimplex(X + Chunk.ChunkCoordinate.x, seed, Z + Chunk.ChunkCoordinate.y);
			noise /= 2;

            int Depth = (int) Mathf.Lerp(1,6,Math.Abs(noise));

            for (int I = 0; I < Depth; I++)
			{
				if (I == 0)
				{
					Chunk._set_block_data(X,Height - 1, Z, 4);	
				}
				else
				{
					Chunk._set_block_data(X,(Height - 1) - I, Z,4);	
				}
			}
		}

		public override void generate_details(ChunkCs Chunk, Random Rng, int[,] GroundHeight, bool CheckingInterChunkGen = true)
		{

			const int treeWidth = 2;

			for (int NTree = 0; NTree < Rng.NextInt(2, 8); NTree++)
			{
				int PosX = Rng.NextInt(treeWidth, (int) ChunkCs.Dimension.x - treeWidth - 1);
				int PosZ = Rng.NextInt(treeWidth, (int) ChunkCs.Dimension.x - treeWidth - 1);
				int TreeHeight = Rng.NextInt(4, 8);
				
				for (int I = 0; I < TreeHeight; I++)
				{

					int X = PosX;
					int Z = PosZ;
					
					int Y = GroundHeight[X,Z] + I;
					
					// 6 is BID for logs
					Chunk._set_block_data(X, Y, Z, 6);
				}
				int MinY = Rng.NextInt(-2, -1);

				int MaxY = Rng.NextInt(2, 4);

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

				for (int NShrub = 0; NShrub < Rng.NextInt(6, 10); NShrub++)
				{
					int X = Rng.NextInt(0, (int)ChunkCs.Dimension.x - 1);
					int Z = Rng.NextInt(0, (int)ChunkCs.Dimension.x - 1);
					int Y = GroundHeight[X,Z];
					
					// 11 is block ID for tall grass
					if (!IsBlockAir(Chunk, X, Y, Z))
					{
						Chunk._set_block_data(X, Y, Z, 11, false);	
					}
				}

				for (int NFlower = 0; NFlower < Rng.NextInt(4, 6); NFlower++)
				{
					int X = Rng.NextInt(0, (int)ChunkCs.Dimension.x - 1);
					int Z = Rng.NextInt(0, (int)ChunkCs.Dimension.x - 1);
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
