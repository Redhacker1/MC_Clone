using System.CodeDom;

namespace MinecraftClone.World_CS.Generation.Noise
{
    public class MixedNoiseClass
    {
        int iterations = 0;
        float frequency;
        int Octaves;
        
        
        
        long seed;
        NoiseUtil noise;

        public MixedNoiseClass(int iterations, NoiseUtil noise)
        {
            this.iterations = iterations;
            this.noise = noise;
        }

        public float GetMixedNoiseSimplex(float x, float y, float z)
        {
            float iterationResults = 0;
            for (int i = 0; i < iterations; i++)
            {
                noise.SetSeed(seed * (i+1));

                iterationResults += noise.GetSimplexFractal(x,y,z);
            }
            noise.SetSeed(seed);
            return iterationResults / iterations;
        }
        
        public float GetMixedNoiseSimplex(float x, float y)
        {
            float iterationResults = 0;

            for (int i = 0; i < iterations; i++)
            {
                noise.SetSeed(seed * (i+1));
                iterationResults += noise.GetSimplexFractal(x,y);
            }
            noise.SetSeed(seed);

            return iterationResults / iterations;
        }
    }
}