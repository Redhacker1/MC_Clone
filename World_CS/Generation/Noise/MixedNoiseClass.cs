using System.CodeDom;

namespace MinecraftClone.World_CS.Generation.Noise
{
    public class MixedNoiseClass
    {
        readonly int _iterations = 0;

        readonly NoiseUtil[] _noiseFilters;

        public MixedNoiseClass(int iterations, NoiseUtil noise)
        {
            this._iterations = iterations;
            _noiseFilters = new NoiseUtil[iterations];
            long seed = noise.GetSeed();

            for (int i = 0; i < iterations; i++)
            {
                _noiseFilters[i] = new NoiseUtil(noise);
                _noiseFilters[i].SetSeed(seed * (i + 1));
            }
        }
        
        public MixedNoiseClass(NoiseUtil[] noiseFilters)
        {
            _iterations = noiseFilters.Length;
            this._noiseFilters = noiseFilters;
        }

        public float GetMixedNoiseSimplex(float x, float y, float z)
        {
            float iterationResults = 0;
            for (int i = 0; i < _iterations; i++)
            {
                iterationResults += _noiseFilters[i].GetSimplexFractal(x,y,z);
            }
            return iterationResults / _iterations;
        }
        
        public float GetMixedNoiseSimplex(float x, float y)
        {
            float iterationResults = 0;

            for (int i = 0; i < _iterations; i++)
            {
                iterationResults += _noiseFilters[i].GetSimplexFractal(x,y);
            }
            return iterationResults / _iterations;
        }
    }
}