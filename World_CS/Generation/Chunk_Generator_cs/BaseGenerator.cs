using System;
using Godot;
using MinecraftClone.World_CS.Generation.Noise;
using Random = MinecraftClone.World_CS.Utility.JavaImports.Random;

namespace MinecraftClone.World_CS.Generation.Chunk_Generator_cs
{
    internal class BaseGenerator
    {
        internal const int MinTerrainHeight = 8;

        internal const int MaxTerrainCaveHeight = 20;
        
        internal int GenHeight = 60;


        public virtual int[,] GenerateHeightmap(int X, int Z, long seed)
        {
            NoiseUtil noise = new NoiseUtil(seed);
            MixedNoiseClass HeightNoise = new MixedNoiseClass(5, noise);

            int[,] groundHeight = new int[(int)ChunkCs.Dimension.x, (int)ChunkCs.Dimension.x];

            for (int x = 0; x < ChunkCs.Dimension.x; x++)
            {
                for (int z = 0; z < ChunkCs.Dimension.z; z++)
                {
                    float hNoise = Mathf.Clamp(((1f + HeightNoise.GetMixedNoiseSimplex(x + X, z + Z)))/2,  0, 1);
                    int yHeight = (int) (hNoise * (GenHeight - 1) + 1);
                    
                    groundHeight[x,z] = yHeight;
                }
            }

            return groundHeight;
            
        }

        public void Generate(ChunkCs  chunk, int X, int Z, long Seed)
        {
            X *= (int)ChunkCs.Dimension.x;
            Z *= (int)ChunkCs.Dimension.z;
            
            int[,] surfaceheight = GenerateHeightmap(X,Z,Seed);

            for (int x = 0; x < ChunkCs.Dimension.x; x++)
            {
                for (int z = 0; z < ChunkCs.Dimension.x; z++)
                {
                    generate_surface(chunk ,surfaceheight[x,z], x, z); 
                    GenerateTopsoil(chunk,surfaceheight[x,z], x, z, Seed);	
                }
            }
            
            Generate_Caves(chunk, Seed, surfaceheight);
            generate_details(chunk,ProcWorld.WorldRandom,surfaceheight);
        }

        public virtual void generate_surface(ChunkCs Chunk,int Height, int X, int Z)
        {
            Chunk._set_block_data(X,0,Z, 0);   
        }

        public virtual void GenerateTopsoil(ChunkCs Chunk, int Height, int X, int Z, long seed)
        {
            
        }

        public virtual void generate_details(ChunkCs Chunk, Random Rng, int[,] GroundHeight, bool CheckingInterChunkGen = true)
        {
        }

        public virtual void Generate_Caves(ChunkCs Chunk, long Seed, int[,] Height)
        {
            NoiseUtil Noisegen = new NoiseUtil();
            Noisegen.SetSeed((int)Seed + (int)(Chunk.ChunkCoordinate.x + Chunk.ChunkCoordinate.y));
			
            Noisegen.SetFractalOctaves(100);

            for (int Z = 0; Z < ChunkCs.Dimension.z; Z++)
            {
                for (int X = 0; X < ChunkCs.Dimension.x; X++)
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
            return false;  //Chunk.BlockData[ChunkCs.GetFlattenedDimension(X, Y - 1, Z)] == 0;
        }


    }
}