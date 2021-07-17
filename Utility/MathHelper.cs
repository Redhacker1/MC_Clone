using System;

namespace MinecraftClone.Utility
{
    public static class MathHelper
    {
        public static double DegreesToRadians(double YRotation)
        {
            return YRotation * (Math.PI / 180);
        }
        
        public static float DegreesToRadians(float YRotation)
        {
            return (float) (YRotation * (Math.PI / 180));
        }
        
        public static float Round(float value)
        {
            return (float) Math.Floor(value + .5f);
        }
        public static double Round(double value)
        {
            return Math.Floor(value + .5);
        }
        
        
        public static int Modulo( int value, int m) {
            int mod = value % m;
            if (mod < 0) {
                mod += m;
            }
            return mod;
        }
        
        public static float Modulo( float value, float m) {
            float mod = value % m;
            if (mod < 0) {
                mod += m;
            }
            return mod;
        }
        
        public static double Modulo( double value, double m) {
            double mod = value % m;
            if (mod < 0) {
                mod += m;
            }
            return mod;
        }
        
        public static int Clamp( int n, int min, int max ) {
            if( n < min ) return min;
            if( n > max ) return max;
            return n;
        }
        
        public static float Clamp( float n, float min, float max ) {
            if( n < min ) return min;
            if( n > max ) return max;
            return n;
        }
        
        public static double Clamp( double n, double min, double max ) {
            if( n < min ) return min;
            if( n > max ) return max;
            return n;
        }
        

        public static float Lerp(float v0, float v1, float t) {
            return (1 - t) * v0 + t * v1;
        }
        
        public static double Lerp(double v0, double v1, double t) {
            return (1 - t) * v0 + t * v1;
        }
    }
}