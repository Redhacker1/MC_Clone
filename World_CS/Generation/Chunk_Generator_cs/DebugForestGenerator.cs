using MinecraftClone.World_CS.Utility.JavaImports;

namespace MinecraftClone.World_CS.Generation.Chunk_Generator_cs
{
	internal class DebugForestGenerator : ForestGenerator
    {
	    public override void generate_details(ChunkCs Chunk, Random Rng, int[,] GroundHeight, bool CheckingInterChunkGen = true)
		{

			const int treeWidth = 2;
			const int posX = 0;
			const int posZ = 0;
			int TreeHeight = Rng.NextInt(4, 8);
				
			for (int I = 0; I < TreeHeight; I++)
			{
				int Y = GroundHeight[posX,posZ] + I;
					
				// 6 is BID for logs
				Chunk._set_block_data(posX, Y, posZ, 6);
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
						int Lx = posX + Dx;
						int Ly = GroundHeight[posX,posZ] + TreeHeight + Dy;
						int Lz = posZ + Dz;
							
							
						// 5 is block ID for leaves
						Chunk._set_block_data(Lx, Ly, Lz, 5, false);
					}

					if (Dy == MinY || Dy == MaxY - 1) LeafWidth -= 1;
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