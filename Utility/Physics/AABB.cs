using Godot;
using MinecraftClone.Utility.CoreCompatibility;
using Vector3 =  System.Numerics.Vector3;

namespace MinecraftClone.Utility.Physics
{
    public class AABB
    {
        const double epsilon = 0.0f; // This is NOT an epsilon, I must have removed this comment but whoever did this was dumb.
        public Vector3 MinLoc { get; protected set; } // Minimum Location value.
        public Vector3 MaxLoc { get; protected set; } // Max location value.

        public AABB(Vector3 minLoc, Vector3 maxLoc)
        {
            MinLoc = minLoc;
            MaxLoc = maxLoc;
        }

        public void DrawDebug()
        {
            // TODO: Find a good way to identify these 

            WorldScript.lines.Drawline(MinLoc.CastToGodot(), MaxLoc.CastToGodot(), Colors.Red);
        
            WorldScript.lines.Drawline(new Vector3(MinLoc.X, MaxLoc.Y, MaxLoc.Z).CastToGodot(), MaxLoc.CastToGodot(), Colors.Black); 
            
            WorldScript.lines.Drawline(new Vector3(MaxLoc.X, MinLoc.Y, MinLoc.Z).CastToGodot(), MinLoc.CastToGodot(), Colors.Black);
            
            WorldScript.lines.Drawline(new Vector3(MaxLoc.X, MinLoc.Y, MaxLoc.Z).CastToGodot(), MaxLoc.CastToGodot(), Colors.Black);
            
            WorldScript.lines.Drawline(new Vector3(MinLoc.X, MaxLoc.Y, MinLoc.Z).CastToGodot(), MinLoc.CastToGodot(), Colors.Black);
            
            WorldScript.lines.Drawline(new Vector3(MaxLoc.X, MinLoc.Y, MinLoc.Z).CastToGodot(), MinLoc.CastToGodot(), Colors.Black);
            
            WorldScript.lines.Drawline(new Vector3(MaxLoc.X, MinLoc.Y, MaxLoc.Z).CastToGodot(), MaxLoc.CastToGodot(), Colors.Black);
            
            
            
        }

        public AABB Expand(Vector3 size)
        {
            Vector3 minLoc = MinLoc;
            Vector3 maxLoc = MaxLoc;

            if (size.X < 0.0f) minLoc.X += size.X;
            if (size.X > 0.0f) maxLoc.X += size.X;
            if (size.Y < 0.0f) minLoc.Y += size.Y;
            if (size.Y > 0.0f) maxLoc.Y += size.Y;
            if (size.Z < 0.0f) minLoc.Z += size.Z;
            if (size.Z > 0.0f) maxLoc.Z += size.Z;

            return new AABB(minLoc, maxLoc);
        }

        public AABB Grow(Vector3 size)
        {
            Vector3 minLoc = MinLoc - size;
            Vector3 maxLoc = MaxLoc + size;
            return new AABB(minLoc, maxLoc);
        }

        public double ClipXCollide(AABB c, double xa)
        {
            double max;
            if (c.MaxLoc.Y <= MinLoc.Y || c.MinLoc.Y >= MaxLoc.Y) return xa;
            if (c.MaxLoc.Z <= MinLoc.Z || c.MinLoc.Z >= MaxLoc.Z) return xa;

            if (xa > 0.0f && c.MaxLoc.X <= MinLoc.X && (max = MinLoc.X - c.MaxLoc.X - epsilon) < xa) xa = max;
            if (xa < 0.0f && c.MinLoc.X >= MaxLoc.X && (max = MaxLoc.X - c.MinLoc.X + epsilon) > xa) xa = max;

            return xa;
        }

        public double ClipYCollide(AABB c, double ya)
        {
            double max;
            if (c.MaxLoc.X <= MinLoc.X || c.MinLoc.X >= MaxLoc.X) return ya;
            if (c.MaxLoc.Z <= MinLoc.Z || c.MinLoc.Z >= MaxLoc.Z) return ya;

            if (ya > 0.0f && c.MaxLoc.Y <= MinLoc.Y && (max = MinLoc.Y - c.MaxLoc.Y - epsilon) < ya) ya = max;
            if (ya < 0.0f && c.MinLoc.Y >= MaxLoc.Y && (max = MaxLoc.Y - c.MinLoc.Y + epsilon) > ya) ya = max;

            return ya;
        }

        public double ClipZCollide(AABB c, double za)
        {
            double max;
            if (c.MaxLoc.X <= MinLoc.X || c.MinLoc.X >= MaxLoc.X) return za;
            if (c.MaxLoc.Y <= MinLoc.Y || c.MinLoc.Y >= MaxLoc.Y) return za;

            if (za > 0.0f && c.MaxLoc.Z <= MinLoc.Z && (max = MinLoc.Z - c.MaxLoc.Z - epsilon) < za) za = max;
            if (za < 0.0f && c.MinLoc.Z >= MaxLoc.Z && (max = MaxLoc.Z - c.MinLoc.Z + epsilon) > za) za = max;

            return za;
        }

        public bool Intersects(AABB c)
        {
            return !(MaxLoc.X <= c.MinLoc.X || MaxLoc.Y <= c.MinLoc.Y ||
                     MaxLoc.Z <= c.MinLoc.Z || MinLoc.X >= c.MaxLoc.X ||
                     MinLoc.Y >= c.MaxLoc.Y || MinLoc.Z >= c.MaxLoc.Z);
        }

        public void Move(Vector3 a)
        {
            MinLoc += a;
            MaxLoc += a;
        }
    }
}
