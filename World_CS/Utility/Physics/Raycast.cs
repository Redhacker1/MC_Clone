using System;
using Godot;
using MinecraftClone.World_CS.Generation;

namespace MinecraftClone.World_CS.Utility.Physics
{
    public static class Raycast
    {

        public static HitResult CastToPoint(Vector3 StartDir, Vector3 EndLocation, float debugtime)
        {
            Vector3 delta = EndLocation - StartDir;
            Vector3 deltaNormal = delta.Normalized();

            int lineLength = (int) Math.Floor(delta.Length());
            bool res = true;

            return CastInDirection(StartDir, deltaNormal, debugtime, lineLength);
        }
        
        public static HitResult CastInDirection(Vector3 Origin, Vector3 Direction, float debugtime, int MaxVoxeldistance = 100)
        {
            HitResult outResult = new HitResult();
            Vector3 position = Origin.Floor();
            Vector3 direction = Direction;
            
            int max = 50; // block reach. The higher this value is, the further the player can modify blocks

            Vector3 sign = new Vector3(); // dda stuff

            for (int i = 0; i < 3; ++i)
                sign[i] = direction[i] > 0 ? 1 : 0;
            
            for (int i = 0; i < MaxVoxeldistance; ++i)
            {
                Vector3 tvec = ((position.Floor() + sign.Floor()) - position) / direction;
                
                float t = Math.Min(tvec.x, Math.Min(tvec.y, tvec.z));

                position += direction * (t + 0.001f); // +0.001 is an epsilon value so that you dont get precision issues
                //position.Floor();


                byte id = 0;
                // Get the position at the current ray position. HACK: this prevents it from working in editor, however it crashes in editor.
                if (Engine.EditorHint == false)
                {
                    id = ProcWorld.instance.GetBlockIdFromWorldPos((int) position.x, (int) position.y, (int) position.z);    
                }
                
                // TODO: Add Collision masks to allow for more selective picking of blocks in the world.
                if (id != 0) // 0 here, just means air. This statement just says that you've hit a block IF the current ray march step *isnt* air.
                {
                    Vector3 normal = new Vector3();
                    for (int j = 0; j < 3; ++j)
                    {
                        normal[j] = (t == tvec[j]) ? 1 : 0;

                        if (sign[j] == 1)
                        {
                            normal[j] = -normal[j];
                        }
                    }

                    outResult.Normal = normal;
                    outResult.Location = position;
                    outResult.Hit = true;
                    
                    break;
                }
                
                
            }


            return outResult;
        }
    }
}