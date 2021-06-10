using Godot;

namespace MinecraftClone.World_CS.Utility.Physics
{
    public struct Vec3
    {
        public double X;
        public double Y;
        public double Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public Vec3(System.Numerics.Vector3 vector3)
        {
            X = vector3.X;
            Y = vector3.Y;
            Z = vector3.Z;
        }
        
        public Vec3(Godot.Vector3 vector3)
        {
            X = vector3.x;
            Y = vector3.y;
            Z = vector3.z;
        }
        
        public Vec3(Vec3 vector3)
        {
            X = vector3.X;
            Y = vector3.Y;
            Z = vector3.Z;
        }
        
        public Vec3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public Vec3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        
        
        public static Vec3 operator+ (Vec3 left, Vec3 Right)
        {
            return new Vec3(left.X + Right.X, left.Y + Right.Z, left.Z + Right.Z);
        }

        public static Vec3 operator- (Vec3 left, Vec3 Right)
        {
            return new Vec3(left.X - Right.X, left.Y - Right.Z, left.Z - Right.Z);
        }
        
        public static Vec3 operator* (Vec3 left, Vec3 Right)
        {
            return new Vec3(left.X * Right.X, left.Y * Right.Z, left.Z * Right.Z);
        }

        public static implicit operator Vector3(Vec3 vector)
        {
            return new Vector3((float) vector.X, (float) vector.Y, (float) vector.Z);
        }
        
        public static implicit operator System.Numerics.Vector3(Vec3 vector)
        {
            return new System.Numerics.Vector3((float) vector.X, (float) vector.Y, (float) vector.Z);
        }
        
        public static implicit operator Vec3(Vector3 vector)
        {
            return new Vec3(vector.x, vector.y, vector.z);
        }
        
        public static implicit operator Vec3(System.Numerics.Vector3 vector)
        {
            return new Vec3(vector.X, vector.Y, vector.Z);
        }
    }
}