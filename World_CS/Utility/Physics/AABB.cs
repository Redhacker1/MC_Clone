using Godot;

namespace MinecraftClone.World_CS.Utility.Physics
{
    public class AABB
    {
        private const double epsilon = 0.0f;
        public Vector3 A { get; protected set; }
        public Vector3 B { get; protected set; }

        public AABB(Vector3 a, Vector3 b)
        {
            A = a;
            B = b;
        }

        public AABB Expand(Vector3 size)
        {
            Vector3 a = new Vector3(A);
            Vector3 b = new Vector3(B);

            if (size.x < 0.0f) a.x += size.x;
            if (size.x > 0.0f) b.x += size.x;
            if (size.y < 0.0f) a.y += size.y;
            if (size.y > 0.0f) b.y += size.y;
            if (size.z < 0.0f) a.z += size.z;
            if (size.z > 0.0f) b.z += size.z;

            return new AABB(a, b);
        }

        public AABB Grow(Vector3 size)
        {
            Vector3 a = A - size;
            Vector3 b = B + size;
            return new AABB(a, b);
        }

        public double ClipXCollide(AABB c, double xa)
        {
            double max;
            if (c.B.y <= this.A.y || c.A.y >= this.B.y) return xa;
            if (c.B.z <= this.A.z || c.A.z >= this.B.z) return xa;

            if (xa > 0.0f && c.B.x <= this.A.x && (max = this.A.x - c.B.x - epsilon) < xa) xa = max;
            if (xa < 0.0f && c.A.x >= this.B.x && (max = this.B.x - c.A.x + epsilon) > xa) xa = max;

            return xa;
        }

        public double ClipYCollide(AABB c, double ya)
        {
            double max;
            if (c.B.x <= this.A.x || c.A.x >= this.B.x) return ya;
            if (c.B.z <= this.A.z || c.A.z >= this.B.z) return ya;

            if (ya > 0.0f && c.B.y <= this.A.y && (max = this.A.y - c.B.y - epsilon) < ya) ya = max;
            if (ya < 0.0f && c.A.y >= this.B.y && (max = this.B.y - c.A.y + epsilon) > ya) ya = max;

            return ya;
        }

        public double ClipZCollide(AABB c, double za)
        {
            double max;
            if (c.B.x <= this.A.x || c.A.x >= this.B.x) return za;
            if (c.B.y <= this.A.y || c.A.y >= this.B.y) return za;

            if (za > 0.0f && c.B.z <= this.A.z && (max = this.A.z - c.B.z - epsilon) < za) za = max;
            if (za < 0.0f && c.A.z >= this.B.z && (max = this.B.z - c.A.z + epsilon) > za) za = max;

            return za;
        }

        public bool Intersects(AABB c)
        {
            if (c.B.x <= this.A.x || c.A.x >= this.B.x) return false;
            if (c.B.y <= this.A.y || c.A.y >= this.B.y) return false;

            return !(c.B.z <= this.A.z) && !(c.A.z >= this.B.z);
        }

        public void Move(Vector3 a)
        {
            A += a;
            B += a;
        }
    }
}
