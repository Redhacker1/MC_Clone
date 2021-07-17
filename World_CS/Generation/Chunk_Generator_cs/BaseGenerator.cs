using System;
using Godot;
using MinecraftClone.Utility;
using MinecraftClone.World_CS.Generation.Noise;
using MinecraftClone.World_CS.Utility;
using Random = MinecraftClone.Utility.JavaImports.Random;

namespace MinecraftClone.World_CS.Generation.Chunk_Generator_cs
{
    internal class BaseGenerator
    {
        internal const int MinTerrainHeight = 8;

        internal const int MaxTerrainCaveHeight = 20;
        
        internal int GenHeight = 60;


        public int[,] GenerateHeightmap(int X, int Z, long seed)
        {
            NoiseUtil noise = new NoiseUtil(seed);
            noise.SetFractalOctaves(1);
            //noise.SetFrequency(0.0001f);
            MixedNoiseClass HeightNoise = new MixedNoiseClass(3, noise);

            int[,] groundHeight = new int[(int)ChunkCs.Dimension.X, (int)ChunkCs.Dimension.Z];

            for (int x = 0; x < ChunkCs.Dimension.X; x++)
            {
                for (int z = 0; z < ChunkCs.Dimension.Z; z++)
                {
                    float hNoise = MathHelper.Clamp(((1f + HeightNoise.GetMixedNoiseSimplex(x + X, z + Z)))/2,  0, 1);
                    int yHeight = (int) (hNoise * (GenHeight - 1) + 1);
                    
                    groundHeight[x,z] = yHeight;
                }
            }

            return groundHeight;
            
        }

        public void Generate(ChunkCs  chunk, int X, int Z, long Seed)
        {
            X *= (int)ChunkCs.Dimension.X;
            Z *= (int)ChunkCs.Dimension.Z;
            
            int[,] surfaceheight = GenerateHeightmap(X,Z,Seed);

            for (int x = 0; x < ChunkCs.Dimension.X; x++)
            {
                for (int z = 0; z < ChunkCs.Dimension.Z; z++)
                {
                    generate_surface(chunk ,surfaceheight[x,z], x, z); 
                    GenerateTopsoil(chunk,surfaceheight[x,z], x, z, Seed);	
                }
            }
            
            Generate_Caves(chunk, Seed, surfaceheight, X, Z);
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

        public virtual void Generate_Caves(ChunkCs Chunk, long Seed, int[,] Height, int LocX, int LocZ)
        {
            NoiseUtil Noisegen = new NoiseUtil();
            Noisegen.SetSeed(Seed);
			
            Noisegen.SetFractalOctaves(100);

            for (int Z = 0; Z < ChunkCs.Dimension.Z; Z++)
            {
                for (int X = 0; X < ChunkCs.Dimension.X; X++)
                {
                    for (int Y = 0 + MinTerrainHeight; Y == Height[X, Z]; Y++)
                    {
                        float CaveNoise = Math.Abs(Noisegen.GetSimplex(X + LocX, Y, Z + LocZ));

                        if (CaveNoise <= .3f)
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