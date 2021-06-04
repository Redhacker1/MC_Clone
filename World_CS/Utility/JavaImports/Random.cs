using System;

namespace MinecraftClone.World_CS.Utility.JavaImports
{
    public class Random
    {
        long _seed;
        double _nextNextGaussian;
        bool _haveNextNextGaussian;

        public Random()
        {
            _seed = DateTime.Now.Ticks;
        }

        public Random(long Seed)
        {
            SetSeed(Seed);
        }

        public void SetSeed(long Seed)
        {
            _seed = (Seed ^ 0x5DEECE66DL) & ((1L << 48) - 1);
            _haveNextNextGaussian = false;
        }

        protected int Next(int Bits)
        {
            _seed = (_seed * 0x5DEECE66DL + 0xBL) & ((1L << 48) - 1);

            return (int)((ulong)_seed >> (48 - Bits));
        }

        public void NextBytes(byte[] Bytes)
        {
            for (int I = 0; I < Bytes.Length;)
            {
                for (int Rnd = NextInt(), N = Math.Min(Bytes.Length - I, 4); N-- > 0; Rnd >>= 8)
                {
                    Bytes[I++] = (byte)Rnd;
                }
            }
        }

        public int NextInt()
        {
            return Next(32);
        }

        public int NextInt(int N)
        {
            if (N <= 0) throw new ArgumentException("n must be a positive non-zero integer");

            if ((N & -N) == N) return (int)((N * (long)Next(31)) >> 31); // Bound is a power of two

            int Bits, Val;

            do
            {
                Bits = Next(31);
                Val = Bits % N;
            } while (Bits - Val + (N - 1) < 0);

            return Val;
        }

        public long NextLong()
        {
            return ((long)Next(32) << 32) + Next(32);
        }

        public bool NextBoolean()
        {
            return Next(1) != 0;
        }

        public float NextFloat()
        {
            return Next(24) / ((float)(1 << 24));
        }

        public double NextDouble()
        {
            return (((long)Next(26) << 27) + Next(27)) / (double)(1L << 53);
        }
        
        public int NextInt(int Min, int Max)
        {
            return NextInt(Max - Min + 1) + Min;
        }
        

        public double NextGaussian()
        {
            if (_haveNextNextGaussian)
            {
                _haveNextNextGaussian = false;
                return _nextNextGaussian;
            }

            double V1, V2, S; 
            do
            {
                V1 = 2 * NextDouble() - 1;
                V2 = 2 * NextDouble() - 1;
                S = V1 * V1 + V2 * V2;
            } while (S >= 1 || S == 0);
            double Multiplier = Math.Sqrt(-2 * Math.Log(S) / S);
            _nextNextGaussian = V2 * Multiplier;
            _haveNextNextGaussian = true;
            return V1 * Multiplier;
        }
    }
}