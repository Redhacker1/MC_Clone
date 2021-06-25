using System;
using System.IO;
using Godot;
using MinecraftClone.World_CS.Generation;
using MinecraftClone.World_CS.Utility.Debug;

namespace MinecraftClone.World_CS.Utility.Physics
{
    public static class Raycast
    {
        
        // op is a bool that tells if you want to place a block or remove it. 1 = place, 0 = remove
        // pos is the player's position
        // dir is the player camera's "forward" vector, there *should* be an inbuild godot function to get this. (this is the value that is passed into a function that constructs the view matrix
        static void TestLine(Vector3 pos, Vector3 dir)
        {
            Vector3 position = pos;
            Vector3 direction = dir;
            
            int max = 50; // block reach. The higher this value is, the further the player can modify blocks

            Vector3 sign = new Vector3(); // dda stuff

            for (int i = 0; i < 3; ++i)
                sign[i] = direction[i] > 0 ? 1 : 0;

            for (int i = 0; i < max; ++i)
            {
                Vector3 tvec = ((position + sign) - position) / direction;
                
                float t = Math.Min(tvec.x, Math.Min(tvec.y, tvec.z));

                position += direction * (t + 0.001f); // +0.001 is an epsilon value so that you dont get precision issues
 
                // Get the position at the current ray position
                byte id = ProcWorld.instance.GetBlockIdFromWorldPos((int) position.x, (int) position.y, (int) position.z);

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

                    return;
                }
            }
        }
        
        public static HitResult CastToPoint(Vector3 StartDir, Vector3 EndLocation, float debugtime)
        {
            float x = (float) Math.Round(StartDir.x);
            float y = (float) Math.Round(StartDir.y);
            float z = (float) Math.Round(StartDir.z);

            Vector3 delta = EndLocation - StartDir;
            Vector3 deltaNormal = delta.Normalized();

            float lineLength = delta.Length();
            bool res = true;

            return CastInDirection(StartDir, deltaNormal, debugtime, lineLength);
        }
        
        public static HitResult CastInDirection(Vector3 Origin, Vector3 Direction, float debugtime, float MaxVoxeldistance = 100)
        {
            HitResult outResult = new HitResult();
            
            Vector3 position = Origin;
            Vector3 direction = Direction;

            Vector3 sign = new Vector3(); // dda stuff

            for (int i = 0; i < 3; ++i)
                sign[i] = direction[i] > 0 ? 1 : 0;

            for (int i = 0; i < MaxVoxeldistance; ++i)
            {
                Vector3 tvec = ((position + sign) - position) / direction;
                
                float t = Math.Min(tvec.x, Math.Min(tvec.y, tvec.z));

                position += direction * (t + 0.001f); // +0.001 is an epsilon value so that you dont get precision issues
 
                // Get the position at the current ray position
                byte id = ProcWorld.instance.GetBlockIdFromWorldPos((int) position.x, (int) position.y, (int) position.z);

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

                    return outResult;
                }
            }

            return outResult;
        }


        private static int Signum(float x)
        {
            return x > 0 ? 1 : x < 0 ? -1 : 0;
        }

        static double Ceil(float s)
        {
            return s == 0f ? 1f : Math.Ceiling(s);
        }

        private static float Intbound(float s, float ds)
        {
            if (ds < 0 && Math.Round(s) == s) return 0;
            s = Mod(s, 1);
            return (float) ((ds > 0 ? Ceil(s) - s : s - Math.Floor(s)) / Math.Abs(ds));
        }

        private static float Mod(float value, float modulus)
        {
            return (value % modulus + modulus) % modulus;
        }
    }
}