using System;

namespace MinecraftClone.World_CS.Generation.Noise
{
// Simplex Noise for C#
// Copyright © Benjamin Ward 2019
// See LICENSE
// Simplex Noise implementation offering 1D, 2D, and 3D forms w/ values in the range of 0 to 255.
// Based on work by Heikki Törmälä (2012) and Stefan Gustavson (2006).
/// <summary>
    /// Implementation of the Perlin simplex noise, an improved Perlin noise algorithm.
    /// Based loosely on SimplexNoise1234 by Stefan Gustavson: http://staffwww.itn.liu.se/~stegu/aqsis/aqsis-newnoise/
    /// </summary>
    public class NoiseSimplex
    {
        public float[] Calc1D(int width, float scale)
        {
            float[] values = new float[width];
            for (int i = 0; i < width; i++)
                values[i] = Generate(i * scale) * 128 + 128;
            return values;
        }

        public float[,] Calc2D(int width, int height, float scale)
        {
            float[,] values = new float[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    values[i, j] = Generate(i * scale, j * scale) * 128 + 128; 
                }
            }
            return values;
        }

        public float[, ,] Calc3D(int width, int height, int length, float scale)
        {
            float[,,] values = new float[width, height, length];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int k = 0; k < length; k++)
                    {
                        values[i, j, k] = Generate(i * scale, j * scale, k * scale) * 128 + 128;
                    }
                }   
            }
            return values;
        }

        public float CalcPixel1D(int x, float scale)
        {
            return Generate(x * scale) * 128 + 128;
        }

        public float CalcPixel2D(float x, float y, float scale)
        {
            return Generate(x * scale, y * scale) * 128 + 128;
        }

        public float CalcPixel3D(int x, int y, int z, float scale)
        {
            return Generate(x * scale, y * scale, z * scale) * 128 + 128;
        }

        static NoiseSimplex()
        {
            _perm = new byte[PermOriginal.Length];
            PermOriginal.CopyTo(_perm, 0);
        }

        public int Seed
        {
            get => _seed;
            set
            {
                if (value == 0)
                {
                    _perm = new byte[PermOriginal.Length];
                    PermOriginal.CopyTo(_perm, 0);
                }
                else
                {
                    _perm = new byte[512];
                    Random random = new Random(value);
                    random.NextBytes(_perm);
                }

                _seed = value;
            }
        }

        int _seed;

        /// <summary>
        /// 1D simplex noise
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        static float Generate(float x)
        {
            int i0 = FastFloor(x);
            int i1 = i0 + 1;
            float x0 = x - i0;
            float x1 = x0 - 1.0f;

            float t0 = 1.0f - x0 * x0;
            t0 *= t0;
            float n0 = t0 * t0 * Grad(_perm[i0 & 0xff], x0);

            float t1 = 1.0f - x1 * x1;
            t1 *= t1;
            float n1 = t1 * t1 * Grad(_perm[i1 & 0xff], x1);
            // The maximum value of this noise is 8*(3/4)^4 = 2.53125
            // A factor of 0.395 scales to fit exactly within [-1,1]
            return 0.395f * (n0 + n1);
        }

        /// <summary>
        /// 2D simplex noise
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static float Generate(float x, float y)
        {
            const float f2 = 0.366025403f; // F2 = 0.5*(sqrt(3.0)-1.0)
            const float g2 = 0.211324865f; // G2 = (3.0-Math.sqrt(3.0))/6.0

            float n0, n1, n2; // Noise contributions from the three corners

            // Skew the input space to determine which simplex cell we're in
            float s = (x + y) * f2; // Hairy factor for 2D
            float xs = x + s;
            float ys = y + s;
            int i = FastFloor(xs);
            int j = FastFloor(ys);

            float t = (i + j) * g2;
            float xStart = i - t; // Unskew the cell origin back to (x,y) space
            float yStart = j - t;
            float x0 = x - xStart; // The x,y distances from the cell origin
            float y0 = y - yStart;

            // For the 2D case, the simplex shape is an equilateral triangle.
            // Determine which simplex we are in.
            int i1, j1; // Offsets for second (middle) corner of simplex in (i,j) coords
            if (x0 > y0) { i1 = 1; j1 = 0; } // lower triangle, XY order: (0,0)->(1,0)->(1,1)
            else { i1 = 0; j1 = 1; }      // upper triangle, YX order: (0,0)->(0,1)->(1,1)

            // A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y), and
            // a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y), where
            // c = (3-sqrt(3))/6

            float x1 = x0 - i1 + g2; // Offsets for middle corner in (x,y) unskewed coords
            float y1 = y0 - j1 + g2;
            float x2 = x0 - 1.0f + 2.0f * g2; // Offsets for last corner in (x,y) unskewed coords
            float y2 = y0 - 1.0f + 2.0f * g2;

            // Wrap the integer indices at 256, to avoid indexing perm[] out of bounds
            int ii = Mod(i, 256);
            int jj = Mod(j, 256);

            // Calculate the contribution from the three corners
            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 < 0.0f) n0 = 0.0f;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Grad(_perm[ii + _perm[jj]], x0, y0);
            }

            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 < 0.0f) n1 = 0.0f;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Grad(_perm[ii + i1 + _perm[jj + j1]], x1, y1);
            }

            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 < 0.0f) n2 = 0.0f;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Grad(_perm[ii + 1 + _perm[jj + 1]], x2, y2);
            }

            // Add contributions from each corner to get the final noise value.
            // The result is scaled to return values in the interval [-1,1].
            return 40.0f * (n0 + n1 + n2); // TODO: The scale factor is preliminary!
        }


        static float Generate(float x, float y, float z)
        {
            // Simple skewing factors for the 3D case
            const float f3 = 0.333333333f;
            const float g3 = 0.166666667f;

            float n0, n1, n2, n3; // Noise contributions from the four corners

            // Skew the input space to determine which simplex cell we're in
            float s = (x + y + z) * f3; // Very nice and simple skew factor for 3D
            float xs = x + s;
            float ys = y + s;
            float zs = z + s;
            int i = FastFloor(xs);
            int j = FastFloor(ys);
            int k = FastFloor(zs);

            float t = (i + j + k) * g3;
            float X0 = i - t; // Unskew the cell origin back to (x,y,z) space
            float Y0 = j - t;
            float Z0 = k - t;
            float x0 = x - X0; // The x,y,z distances from the cell origin
            float y0 = y - Y0;
            float z0 = z - Z0;

            // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
            // Determine which simplex we are in.
            int i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
            int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords

            /* This code would benefit from a backport from the GLSL version! */
            if (x0 >= y0)
            {
                if (y0 >= z0)
                { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // X Y Z order
                else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; } // X Z Y order
                else { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; } // Z X Y order
            }
            else
            { // x0<y0
                if (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; } // Z Y X order
                else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; } // Y Z X order
                else { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // Y X Z order
            }

            // A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
            // a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
            // a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where
            // c = 1/6.

            float x1 = x0 - i1 + g3; // Offsets for second corner in (x,y,z) coords
            float y1 = y0 - j1 + g3;
            float z1 = z0 - k1 + g3;
            float x2 = x0 - i2 + 2.0f * g3; // Offsets for third corner in (x,y,z) coords
            float y2 = y0 - j2 + 2.0f * g3;
            float z2 = z0 - k2 + 2.0f * g3;
            float x3 = x0 - 1.0f + 3.0f * g3; // Offsets for last corner in (x,y,z) coords
            float y3 = y0 - 1.0f + 3.0f * g3;
            float z3 = z0 - 1.0f + 3.0f * g3;

            // Wrap the integer indices at 256, to avoid indexing perm[] out of bounds
            int ii = Mod(i, 256);
            int jj = Mod(j, 256);
            int kk = Mod(k, 256);

            // Calculate the contribution from the four corners
            float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
            if (t0 < 0.0f) n0 = 0.0f;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Grad(_perm[ii + _perm[jj + _perm[kk]]], x0, y0, z0);
            }

            float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1;
            if (t1 < 0.0f) n1 = 0.0f;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Grad(_perm[ii + i1 + _perm[jj + j1 + _perm[kk + k1]]], x1, y1, z1);
            }

            float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2;
            if (t2 < 0.0f) n2 = 0.0f;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Grad(_perm[ii + i2 + _perm[jj + j2 + _perm[kk + k2]]], x2, y2, z2);
            }

            float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3;
            if (t3 < 0.0f) n3 = 0.0f;
            else
            {
                t3 *= t3;
                n3 = t3 * t3 * Grad(_perm[ii + 1 + _perm[jj + 1 + _perm[kk + 1]]], x3, y3, z3);
            }

            // Add contributions from each corner to get the final noise value.
            // The result is scaled to stay just inside [-1,1]
            return 32.0f * (n0 + n1 + n2 + n3); // TODO: The scale factor is preliminary!
        }

        static byte[] _perm;

        static readonly byte[] PermOriginal = {
            151,160,137,91,90,15,
            131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
            190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
            88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
            77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
            102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
            135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
            5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
            223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
            129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
            251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
            49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
            138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
            151,160,137,91,90,15,
            131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
            190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
            88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
            77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
            102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
            135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
            5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
            223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
            129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
            251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
            49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
            138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180 
        };

        static int FastFloor(float x)
        {
            return (x > 0) ? ((int)x) : (((int)x) - 1);
        }

        static int Mod(int x, int m)
        {
            int a = x % m;
            return a < 0 ? a + m : a;
        }

        static float Grad(int hash, float x)
        {
            int h = hash & 15;
            float grad = 1.0f + (h & 7);   // Gradient value 1.0, 2.0, ..., 8.0
            if ((h & 8) != 0) grad = -grad;         // Set a random sign for the gradient
            return (grad * x);           // Multiply the gradient with the distance
        }

        static float Grad(int hash, float x, float y)
        {
            int h = hash & 7;      // Convert low 3 bits of hash code
            float u = h < 4 ? x : y;  // into 8 simple gradient directions,
            float v = h < 4 ? y : x;  // and compute the dot product with (x,y).
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -2.0f * v : 2.0f * v);
        }

        static float Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;     // Convert low 4 bits of hash code into 12 simple
            float u = h < 8 ? x : y; // gradient directions, and compute dot product.
            float v = h < 4 ? y : h == 12 || h == 14 ? x : z; // Fix repeats at h = 12 to 15
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -v : v);
        }

        static float Grad(int hash, float x, float y, float z, float t)
        {
            int h = hash & 31;      // Convert low 5 bits of hash code into 32 simple
            float u = h < 24 ? x : y; // gradient directions, and compute dot product.
            float v = h < 16 ? y : z;
            float w = h < 8 ? z : t;
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -v : v) + ((h & 4) != 0 ? -w : w);
        }
    }
}