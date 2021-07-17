using System.Collections.Generic;
using Godot;

namespace MinecraftClone.World_CS.Utility.Debug
{
    public class DebugLines : Node2D
    {


        class DebugLine
        {
            public readonly Vector3 Start;
            public readonly Vector3 End;
            public readonly Color LineColor;
            public float drawtime;

            public DebugLine(Vector3 start, Vector3 end, Color color, float Time)
            {
                Start = start;
                End = end;
                LineColor = color;
                drawtime = Time;
            }
        }


        readonly List<DebugLine> lines = new List<DebugLine>();
        bool RemovedLine;
        
        public override void _Process(float delta)
        {
            base._Process(delta);

            for (int linenumber = 0; linenumber < lines.Count; linenumber++)
            {
                lines[linenumber].drawtime -= delta;
            }

            if (lines.Count > 0 || RemovedLine)
            {
                Update(); // Calls draw
            }
            
        }
        
        public override void _Draw()
        {
            base._Draw();
            Camera cam = GetViewport().GetCamera();

            for (int i = 0; i < lines.Count; i++)
            {
                var ScreenPointStart = cam.UnprojectPosition(lines[i].Start);
                var ScreenPointEnd = cam.UnprojectPosition(lines[i].End);
                
                //Dont draw if either start or end is considered behind the camera
                // this causes the line to not be drawn sometimes, however avoids an issue where the
                // line is drawn incorrectly

                if ((cam.IsPositionBehind(lines[i].Start) || cam.IsPositionBehind(lines[i].End)) == false)
                {
                    DrawLine(ScreenPointStart, ScreenPointEnd, lines[i].LineColor, 2);
                }
            }
            
            
            // Remove lines that have timed out.
            int count = lines.Count - 1;

            while (count >= 0)
            {
                if (lines[count].drawtime < 0.0f)
                {
                    lines.RemoveAt(count);
                    RemovedLine = true;
                }

                count -= 1;
            }

        }

        public void DrawBlock(int X, int Y, int Z, float delta)
        {
            Vector3 MinLoc = new Vector3(X - 1, Y - 1, Z - 1);
            Vector3 MaxLoc = new Vector3(X, Y, Z);
            
            Drawline(MinLoc, MaxLoc, Colors.Red);
        
            
            Drawline(new Vector3(MaxLoc.x, MinLoc.y, MaxLoc.z), MaxLoc, Colors.Black,delta);
            Drawline(new Vector3(MaxLoc.x, MinLoc.y, MaxLoc.z), MaxLoc, Colors.Black,delta);
            Drawline(new Vector3(MinLoc.x, MaxLoc.y, MaxLoc.z), MaxLoc, Colors.Black,delta);
            Drawline(new Vector3(MinLoc.x, MaxLoc.y, MaxLoc.z), MaxLoc, Colors.Black,delta);
            Drawline(new Vector3(MaxLoc.x, MinLoc.y, MaxLoc.z), MaxLoc, Colors.Black,delta);

            
            
            Drawline(new Vector3(MinLoc.x, MaxLoc.y, MinLoc.z), MinLoc, Colors.Black,delta);
            Drawline(new Vector3(MinLoc.x, MaxLoc.y, MinLoc.z), MinLoc, Colors.Black,delta);
            Drawline(new Vector3(MaxLoc.x, MinLoc.y, MinLoc.z), MinLoc, Colors.Black,delta);
            Drawline(new Vector3(MaxLoc.x, MinLoc.y, MinLoc.z), MinLoc, Colors.Black,delta);
            Drawline(new Vector3(MinLoc.x, MaxLoc.y, MinLoc.z), MinLoc, Colors.Black,delta);
            
            
            Drawline(new Vector3(MaxLoc.x, MinLoc.y, MaxLoc.z), new Vector3(MaxLoc.x, MinLoc.y, MinLoc.z), Colors.Black,delta);
            Drawline(new Vector3(MinLoc.x, MaxLoc.y, MinLoc.z), new Vector3(MinLoc.x, MaxLoc.y, MaxLoc.z), Colors.Black,delta);
            Drawline(new Vector3(MinLoc.x, MinLoc.y, MaxLoc.z), MinLoc, Colors.Black,delta);
            Drawline(new Vector3(MaxLoc.x, MaxLoc.y, MinLoc.z), MaxLoc, Colors.Black,delta);


            Drawline(new Vector3(MinLoc.x, MaxLoc.y, MinLoc.z), new Vector3(MaxLoc.x, MaxLoc.y, MinLoc.z), Colors.Black, delta);
            Drawline(new Vector3(MaxLoc.x, MinLoc.y, MaxLoc.z), new Vector3(MinLoc.x, MinLoc.y, MaxLoc.z), Colors.Black, delta);
            
            Drawline(new Vector3(MaxLoc.x, MinLoc.y, MinLoc.z), new Vector3(MaxLoc.x, MaxLoc.y, MinLoc.z), Colors.Black, delta);
            Drawline(new Vector3(MinLoc.x, MaxLoc.y, MaxLoc.z), new Vector3(MinLoc.x, MinLoc.y, MaxLoc.z), Colors.Black,delta);
        }

        public void Drawline(Vector3 start, Vector3 end, Color color, float time = 0.0f)
        {
            lines.Add(new DebugLine(start, end, color, time));
        }

        public void DrawRay(Vector3 start, Vector3 ray, Color color, float time =  0.0f)
        {
            lines.Add(new DebugLine(start, start + ray, color, time));
        }

        public void DrawDot(Vector3 location, Color color, float time = 0.0f)
        {
            Drawline(location, new Vector3(.01f, .01f,.01f) + location, color, time);
        }
        
    }
}