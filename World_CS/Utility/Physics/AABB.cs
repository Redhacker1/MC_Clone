using Godot;
using MinecraftClone.World_CS.Generation;

namespace MinecraftClone.World_CS.Utility.Physics
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

            ProcWorld.lines.Drawline(MinLoc, MaxLoc, Colors.Red);
        
            ProcWorld.lines.Drawline(new Vector3(MinLoc.x, MaxLoc.y, MaxLoc.z), MaxLoc, Colors.Black); 
            
            ProcWorld.lines.Drawline(new Vector3(MaxLoc.x, MinLoc.y, MinLoc.z), MinLoc, Colors.Black);
            
            ProcWorld.lines.Drawline(new Vector3(MaxLoc.x, MinLoc.y, MaxLoc.z), MaxLoc, Colors.Black);
            
            ProcWorld.lines.Drawline(new Vector3(MinLoc.x, MaxLoc.y, MinLoc.z), MinLoc, Colors.Black);
            
            ProcWorld.lines.Drawline(new Vector3(MaxLoc.x, MinLoc.y, MinLoc.z), MinLoc, Colors.Black);
            
            ProcWorld.lines.Drawline(new Vector3(MaxLoc.x, MinLoc.y, MaxLoc.z), MaxLoc, Colors.Black);
            
            
            
        }

        public AABB Expand(Vector3 size)
        {
            Vector3 minLoc = new Vector3(MinLoc);
            Vector3 maxLoc = new Vector3(MaxLoc);

            if (size.x < 0.0f) minLoc.x += size.x;
            if (size.x > 0.0f) maxLoc.x += size.x;
            if (size.y < 0.0f) minLoc.y += size.y;
            if (size.y > 0.0f) maxLoc.y += size.y;
            if (size.z < 0.0f) minLoc.z += size.z;
            if (size.z > 0.0f) maxLoc.z += size.z;

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
            if (c.MaxLoc.y <= MinLoc.y || c.MinLoc.y >= MaxLoc.y) return xa;
            if (c.MaxLoc.z <= MinLoc.z || c.MinLoc.z >= MaxLoc.z) return xa;

            if (xa > 0.0f && c.MaxLoc.x <= MinLoc.x && (max = MinLoc.x - c.MaxLoc.x - epsilon) < xa) xa = max;
            if (xa < 0.0f && c.MinLoc.x >= MaxLoc.x && (max = MaxLoc.x - c.MinLoc.x + epsilon) > xa) xa = max;

            return xa;
        }

        public double ClipYCollide(AABB c, double ya)
        {
            double max;
            if (c.MaxLoc.x <= MinLoc.x || c.MinLoc.x >= MaxLoc.x) return ya;
            if (c.MaxLoc.z <= MinLoc.z || c.MinLoc.z >= MaxLoc.z) return ya;

            if (ya > 0.0f && c.MaxLoc.y <= MinLoc.y && (max = MinLoc.y - c.MaxLoc.y - epsilon) < ya) ya = max;
            if (ya < 0.0f && c.MinLoc.y >= MaxLoc.y && (max = MaxLoc.y - c.MinLoc.y + epsilon) > ya) ya = max;

            return ya;
        }

        public double ClipZCollide(AABB c, double za)
        {
            double max;
            if (c.MaxLoc.x <= MinLoc.x || c.MinLoc.x >= MaxLoc.x) return za;
            if (c.MaxLoc.y <= MinLoc.y || c.MinLoc.y >= MaxLoc.y) return za;

            if (za > 0.0f && c.MaxLoc.z <= MinLoc.z && (max = MinLoc.z - c.MaxLoc.z - epsilon) < za) za = max;
            if (za < 0.0f && c.MinLoc.z >= MaxLoc.z && (max = MaxLoc.z - c.MinLoc.z + epsilon) > za) za = max;

            return za;
        }

        public bool Intersects(AABB c)
        {
            GD.Print(!(this.MaxLoc.x < c.MinLoc.x || this.MaxLoc.y < c.MinLoc.y ||
                       this.MaxLoc.z < c.MinLoc.z || this.MinLoc.x > c.MaxLoc.x ||
                       this.MinLoc.y > c.MaxLoc.y || this.MinLoc.z > c.MaxLoc.z));
            return !(this.MaxLoc.x < c.MinLoc.x || this.MaxLoc.y < c.MinLoc.y ||
                     this.MaxLoc.z < c.MinLoc.z || this.MinLoc.x > c.MaxLoc.x ||
                     this.MinLoc.y > c.MaxLoc.y || this.MinLoc.z > c.MaxLoc.z);
        }

        public void Move(Vector3 a)
        {
            MinLoc += a;
            MaxLoc += a;
        }
    }
}
