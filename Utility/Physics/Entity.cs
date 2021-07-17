using System;
using System.Collections.Generic;
using Godot;
using MinecraftClone.Utility.CoreCompatibility;
using Vector3 = System.Numerics.Vector3;
using MinecraftClone.World_CS.Generation;

namespace MinecraftClone.Utility.Physics
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

            Vector3 _a = new Vector3(a.X, a.Y, a.Z);

            Vector3 o = new Vector3(_a.X, _a.Y, _a.Z);

            List<AABB> aabbs = Level.Get_aabbs(0, AABB.Expand(_a));

            foreach (AABB aabb in aabbs)
            {
                _a.Y = (float)aabb.ClipYCollide(AABB, _a.Y);
            }
            AABB.Move(new Vector3(0, _a.Y, 0));
            foreach (AABB aabb in aabbs)
            {
                _a.X = (float)aabb.ClipXCollide(AABB, _a.X);
            }
            AABB.Move(new Vector3(_a.X, 0, 0));
            foreach (AABB aabb in aabbs)
            {
                _a.Z = (float)aabb.ClipZCollide(AABB, _a.Z);
            }
            AABB.Move(new Vector3(0, 0, _a.Z));
            

            OnGround = Math.Abs(o.Y - _a.Y) > double.Epsilon && o.Y < 0;

            if (Math.Abs(o.X - _a.X) > double.Epsilon) PosDelta.X = 0;
            if (Math.Abs(o.Y - _a.Y) > double.Epsilon) PosDelta.Y = 0;
            if (Math.Abs(o.Z - _a.Z) > double.Epsilon) PosDelta.Z = 0;

            Pos.X = (AABB.MinLoc.X + AABB.MaxLoc.X) / 2.0f;
            Pos.Y = (float) (AABB.MinLoc.Y + EyeOffset);
            Pos.Z = (AABB.MinLoc.Z + AABB.MaxLoc.Z) / 2.0f;

            Translation = new Vector3(Pos.X, Pos.Y, Pos.Z).CastToGodot();
        }

        public virtual void MoveRelative(float dx, float dz, float speed)
        {
            float dist = dx * dx + dz * dz;
            if (dist < 0.01f) return;

            dist = speed / (float)Math.Sqrt(dist);
            double sin = (float)Math.Sin(MathHelper.DegreesToRadians(YRotation));
            double cos = (float)Math.Cos(MathHelper.DegreesToRadians(YRotation));

            PosDelta.X += (dx *= dist) * (float)cos - (dz *= dist) * (float)sin;
            PosDelta.Z += dz * (float)cos + dx * (float)sin;
        }

        public virtual void SetPos(Vector3 pos)
        {
            Pos = pos;
            double w = AABBWidth / 2.0f;
            double h = AABBHeight / 2.0f;
            
            AABB = new AABB(new Vector3((float) (pos.X - w), (float) (pos.Y - h), (float) (pos.Z - w)), new Vector3((float) (pos.X + w), (float) (pos.Y + h), (float) (pos.Z + w)));
            Translation = pos.CastToGodot();
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