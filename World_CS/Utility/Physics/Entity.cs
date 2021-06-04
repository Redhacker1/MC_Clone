using System;
using Godot;
using MinecraftClone.World_CS.Generation;

namespace MinecraftClone.World_CS.Utility.Physics
{
public abstract class Entity: Spatial
    {
        public Vector3 Pos = new Vector3(10, 10, 10);
        public Vector3 PosDelta = new Vector3();
        float Lasttick;
        public AABB AABB;
        public float XRotation = 0;
        public float YRotation = 0;
        public bool OnGround = false;
        public bool InWater = false;
        protected float aabbWidth = 0.6f;
        protected float aabbHeight = 1.8f;
        public float eyeOffset = 1.6f;

        public ProcWorld level;

        //public abstract void Tick();

        public override void _Process(float delta)
        {
            base._Process(delta);
            Lasttick = delta;
        }

        public virtual void Move(Vector3 a)
        {
            if (level == null)
            {
                level = WorldScript._pw;
                return;
            }

            Vector3 _a = new Vector3(a.x, a.y, a.z);

            InWater = false;

            Vector3 o = new Vector3(_a.x, _a.y, _a.z);

            var aabbs = level.Get_aabbs(0, AABB.Expand(_a));

            foreach (AABB aabb in aabbs)
            {
                _a.y = (float)aabb.ClipYCollide(AABB, _a.y);
            }
            AABB.Move(new Vector3(0, _a.y, 0));
            foreach (AABB aabb in aabbs)
            {
                _a.x = (float)aabb.ClipXCollide(AABB, _a.x);
            }
            AABB.Move(new Vector3(_a.x, 0, 0));
            foreach (AABB aabb in aabbs)
            {
                _a.z = (float)aabb.ClipZCollide(AABB, _a.z);
            }
            AABB.Move(new Vector3(0, 0, _a.z));
            

            OnGround = o.y != _a.y && o.y < 0;

            if (o.x != _a.x) PosDelta.x = 0;
            if (o.y != _a.y) PosDelta.y = 0;
            if (o.z != _a.z) PosDelta.z = 0;

            Pos.x = (AABB.A.x + AABB.B.x) / 2.0f;
            Pos.y = AABB.A.y + eyeOffset;
            Pos.z = (AABB.A.z + AABB.B.z) / 2.0f;

            Translation = new Godot.Vector3(Pos.x, Pos.y, Pos.z);
        }

        public virtual void MoveRelative(float dx, float dz, float speed)
        {
            float dist = dx * dx + dz * dz;
            if (dist < 0.01f) return;

            dist = speed / (float)Math.Sqrt(dist);
            double sin = (float)Math.Sin(MathHelper.DegreesToRadians(YRotation));
            double cos = (float)Math.Cos(MathHelper.DegreesToRadians(YRotation));

            PosDelta.x += (dx *= dist) * (float)cos - (dz *= dist) * (float)sin;
            PosDelta.z += dz * (float)cos + dx * (float)sin;
        }

        public virtual void SetPos(Vector3 pos)
        {
            Pos = pos;
            float w = aabbWidth / 2.0f;
            float h = aabbHeight / 2.0f;
            AABB = new AABB(new Vector3(pos.x - w, pos.y - h, pos.z - w), new Vector3(pos.x + w, pos.y + h, pos.z + w));
            Translation = pos;
        }

        public virtual void Rotate(float rotX, float rotY)
        {
            XRotation = (float)(XRotation - rotX * 0.15);
            YRotation = (float)((YRotation + rotY * 0.15) % 360.0);
            if (XRotation < -90.0f) XRotation = -90.0f;
            if (XRotation > 90.0f) XRotation = 90.0f;
        }
    }
}