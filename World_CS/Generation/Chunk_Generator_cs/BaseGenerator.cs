using System;
using Godot;
using MinecraftClone.World_CS.Generation.Noise;

namespace MinecraftClone.World_CS.Generation.Chunk_Generator_cs
{
    internal class BaseGenerator
    {
        internal const int MinTerrainHeight = 8;

        internal const int MaxTerrainCaveHeight = 20;

        public virtual void generate_surface(ChunkCs Chunk,int Height, int X, int Z)
        {
            Chunk._set_block_data(X,0,Z, 0);   
        }

        public virtual void GenerateTopsoil(ChunkCs Chunk, int Height, int X, int Z)
        {
            
        }

        public virtual void generate_details(ChunkCs Chunk, RandomNumberGenerator Rng, int[,] GroundHeight, bool CheckingInterChunkGen = true)
        {
        }

        public virtual void Generate_Caves(ChunkCs Chunk, long Seed, int[,] Height)
        {
            NoiseUtil Noisegen = new NoiseUtil();
            Noisegen.SetSeed((int)Seed + (int)(Chunk.ChunkCoordinate.x + Chunk.ChunkCoordinate.y));
			
            Noisegen.SetFractalOctaves(1000);

            for (int X = 0; X < ChunkCs.Dimension.x; X++)
            {
                for (int Z = 0; Z < ChunkCs.Dimension.x; Z++)
                {
                    for (int Y = 0 + MinTerrainHeight; Y < Height[X, Z] - MaxTerrainCaveHeight; Y++)
                    {
                        float CaveNoise = Math.Abs(Noisegen.GetSimplex(X, Y, Z));

                        if (CaveNoise <= .3)
                        {
                            Chunk._set_block_data(X,Y,Z,0);
                        }
                    }
                }
            }
        }
        
        internal static bool IsBlockAir(ChunkCs Chunk, int X, int Y, int Z)
        {
            return false; //chunk.BlockData[ChunkCs.GetFlattenedDimension(x, y - 1, z)] == 0;
        }


    }
}