using Godot;

namespace MinecraftClone.World_CS.Generation.Noise
{
	public class NoiseLib
	{
		static double Rand1D(float offset,int iterations = 1, int seed = 1337, bool lerp = false)
		{
			double result = seed;
			const float a = 328f;
			const float mod = 7829f;
			for (int i = 0; i < iterations; i++)
			{
				result = (a * result + iterations + offset) % mod;
			}
			if (lerp)
			{
				return result / mod;   
			}
			return result;
		}

		public static float Rand2D(Vector2 coords, int seed, bool lerp = false)
		{
			double noise1Dx = Rand1D(coords.x, 1, seed, lerp);
			double noise1Dy = Rand1D(coords.y, 1, seed, lerp);

			return (float) (noise1Dx / 2 + noise1Dy / 2);
		}

		public static float perlin_noise(Vector2 coord, int seed = 1337) 
		{
			Vector2 i = coord.Floor();
			Vector2 f = coord - i;
	
			// 4 corners of a rectangle surrounding our point
			// must be up to 2pi radians to allow the random vectors to face all directions
			float tl = Rand2D(i,seed) * 6.283f;
			float tr = Rand2D(i + new Vector2(1.0f, 0.0f),seed) * 6.283f;
			float bl = Rand2D(i + new Vector2(0.0f, 1.0f), seed) * 6.283f;
			float br = Rand2D(i + new Vector2(1.0f, 1.0f),seed) * 6.283f;
	
			// original unit vector = (0, 1) which points downwards
			Vector2 tlVec = new Vector2(-Mathf.Sin(tl), Mathf.Cos(tl));
			Vector2 trVec = new Vector2(-Mathf.Sin(tr), Mathf.Cos(tr));
			Vector2 blvec = new Vector2(-Mathf.Sin(bl), Mathf.Cos(bl));
			Vector2 brvec = new Vector2(-Mathf.Sin(br), Mathf.Cos(br));
	
			// getting dot product of each corner's vector and its distance vector to current point
			float tldot = tlVec.Dot(f);
			float trdot = trVec.Dot(f - new Vector2(1.0f, 0.0f));
			float bldot = blvec.Dot( f - new Vector2(0.0f, 1.0f));
			float brdot = brvec.Dot( f - new Vector2(1.0f, 1.0f));
			
			// Same as Cubic Hermine Curve
			Vector2 cubic = new Vector2(3.0f - 2.0f * f.x, 3.0f - 2.0f * f.y);
			cubic = f*f*cubic;
	
			float topmix = Lerp(tldot, trdot, cubic.x);
			float botmix = Lerp(bldot, brdot, cubic.x);
			float wholemix = Lerp(topmix, botmix, cubic.y);
	
			return Lerp(0, 1, wholemix);
		}

		public float perlin_noise(int coordX, int coordY)
		{
			return perlin_noise(new Vector2(coordX, coordY));
		}
		
		public float perlin_noise(float coordX, float coordY)
		{
			return perlin_noise(new Vector2(coordX, coordY));
		}

		public static float Lerp(float firstFloat, float secondFloat, float by)
		{
			return firstFloat * (1 - by) + secondFloat * by;
		}
	}
}
