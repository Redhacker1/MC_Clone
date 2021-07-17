using System;
using Godot;
using MinecraftClone.Utility.CoreCompatibility;
using Vector3 = System.Numerics.Vector3;
using MinecraftClone.World_CS.Generation;

namespace MinecraftClone.Utility.Physics
{
    public static class Raycast
    {

        public static HitResult CastToPoint(Vector3 StartDir, Vector3 EndLocation, float debugtime)
        {
            Vector3 delta = EndLocation - StartDir;
            Vector3 deltaNormal = Vector3.Normalize(delta);

            int lineLength = (int) Math.Floor(delta.Length());

            return CastInDirection(StartDir, deltaNormal, debugtime, lineLength);
        }
        
        public static HitResult CastInDirection(Vector3 Origin, Vector3 Direction, float debugtime, int MaxVoxeldistance = 100)
        {
            HitResult outResult = new HitResult();
            Vector3 position = Origin + new Vector3(.5f, .5f, .5f);
            Vector3 direction = Direction;


            Vector3 sign = new Vector3
            {
                X = direction.X > 0 ? 1 : 0,
                Y = direction.Y > 0 ? 1 : 0,
                Z = direction.Z > 0 ? 1 : 0
            }; // dda stuff



            for (int i = 0; i < MaxVoxeldistance; ++i)
            {
                Vector3 tvec = ((position + sign).Floor() - position) / direction;
                
                float t = Math.Min(tvec.X, Math.Min(tvec.Y, tvec.Z));

                position += direction * (t + 0.001f); // +0.001 is an epsilon value so that you dont get precision issues
                //position.Floor();


                byte id = 0;
                // Get the position at the current ray position. HACK: this prevents it from working in editor, however it crashes in editor.
                if (Engine.EditorHint == false)
                {
                    id = ProcWorld.instance.GetBlockIdFromWorldPos((int) position.X, (int) position.Y, (int) position.Z);    
                }
                
                // TODO: Add Collision masks to allow for more selective picking of blocks in the world.
                if (id != 0) // 0 here, just means air. This statement just says that you've hit a block IF the current ray march step *isnt* air.
                {
                    Vector3 normal = new Vector3();
                    
                    
                    normal.X = (t == tvec.X) ? 1 : 0;
                    if (sign.X == 1)
                    {
                        normal.X = -normal.X;
                    }
                    
                    normal.Y = (t == tvec.Y) ? 1 : 0;
                    if (sign.Y == 1)
                    {
                        normal.Y = -normal.Y;
                    }
                    
                    normal.Z = (t == tvec.Z) ? 1 : 0;
                    if (sign.Z == 1)
                    {
                        normal.Z = -normal.Z;
                    }

                    outResult.Normal = normal;
                    outResult.Hit = true;
                    
                    break;
                }
                outResult.Location = position.Floor();
                
                
            }


            return outResult;
        }
    }
}