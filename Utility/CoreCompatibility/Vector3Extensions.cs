using System;
using System.Numerics;

namespace MinecraftClone.Utility.CoreCompatibility
{
    public static class Vector3Extension
    {
        public static Godot.Vector3 CastToGodot (this Vector3 vector3)
        {
            return new Godot.Vector3(vector3.X, vector3.Y, vector3.Z);
        }
        
        public static Vector3 CastToCore (this Godot.Vector3 vector3)
        {
            return new Vector3(vector3.x, vector3.y, vector3.z);
        }
        
        
        public static Godot.Vector3 CastToGodot (this Godot.Vector3 vector3)
        {
            return vector3;
        }
        
        public static Vector3 CastToCore (this Vector3 vector3)
        {
            return vector3;
        }
        
        public static Vector3 Floor (this Vector3 vector3)
        {
            return new Vector3((float) Math.Floor(vector3.X),(float) Math.Floor(vector3.Y), (float) Math.Floor(vector3.Z));
        }
        
        
        public static Godot.Vector2 CastToGodot (this Vector2 vector3)
        {
            return new Godot.Vector2(vector3.X, vector3.Y);
        }
        
        
    }
}