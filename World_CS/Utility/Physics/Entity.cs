using System;
using System.Collections.Generic;
using Godot;
using MinecraftClone.World_CS.Generation;

namespace MinecraftClone.World_CS.Utility.Physics
{
public abstract class Entity: Spatial
    {
        public Vector3 Pos = new Vector3(10, 10, 10);
        public Vector3 PosDelta;
        public AABB AABB;
        public double XRotation;
        public double YRotation;
        public bool OnGround;
        public double AABBWidth = .9f;
        protected double AABBHeight = 1.95f;
        public double EyeOffset = 1.6f;

        public ProcWorld Level;

        public virtual void Move(Vector3 a)
        {
            if (Level == null)
            {
                Level = WorldScript._pw;
                return;
            }

            Vector3 _a = new Vector3(a.x, a.y, a.z);

            Vector3 o = new Vector3(_a.x, _a.y, _a.z);

            List<AABB> aabbs = Level.Get_aabbs(0, AABB.Expand(_a));

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
            

            OnGround = Math.Abs(o.y - _a.y) > double.Epsilon && o.y < 0;

            if (Math.Abs(o.x - _a.x) > double.Epsilon) PosDelta.x = 0;
            if (Math.Abs(o.y - _a.y) > double.Epsilon) PosDelta.y = 0;
            if (Math.Abs(o.z - _a.z) > double.Epsilon) PosDelta.z = 0;

            Pos.x = (AABB.MinLoc.x + AABB.MaxLoc.x) / 2.0f;
            Pos.y = (float) (AABB.MinLoc.y + EyeOffset);
            Pos.z = (AABB.MinLoc.z + AABB.MaxLoc.z) / 2.0f;

            Translation = new Vector3(Pos.x, Pos.y, Pos.z);
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
            double w = AABBWidth / 2.0f;
            double h = AABBHeight / 2.0f;
            
            AABB = new AABB(new Vector3((float) (pos.x - w), (float) (pos.y - h), (float) (pos.z - w)), new Vector3((float) (pos.x + w), (float) (pos.y + h), (float) (pos.z + w)));
            Translation = pos;
        }

        public virtual void Rotate(double rotX, double rotY)
        {
            XRotation = (XRotation - rotX * 0.15);
            YRotation = ((YRotation + rotY * 0.15) % 360.0);
            if (XRotation < -90.0f) XRotation = -90.0f;
            if (XRotation > 90.0f) XRotation = 90.0f;
        }
    }
}