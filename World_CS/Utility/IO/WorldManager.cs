using System.Collections.Generic;
using Godot;
using Directory = System.IO.Directory;
using Environment = System.Environment;
using File = System.IO.File;
using Path = System.IO.Path;

namespace MinecraftClone.World_CS.Utility.IO
{

    public struct WorldData
    {
        public string Directory;
        public string Name;

        public WorldData(string name, string directory)
        {
            Name = name.Replace(".wdat", "");
            Directory = directory;
        }
    }
    public static class WorldManager
    {
        public static readonly string WorldDir = Path.Combine(Environment.CurrentDirectory, "World");

        static Dictionary<string, WorldData> _worlds = FindWorlds();
        public static Dictionary<string, WorldData> FindWorlds()
        {
            Dictionary<string, WorldData> worlds = new Dictionary<string, WorldData>();

            GD.Print(WorldDir);
            if (Directory.Exists(WorldDir))
            {
                foreach (string path in Directory.GetDirectories(WorldDir))
                {
                    foreach (string file in Directory.GetFiles(path))
                    {
                        if (Path.GetExtension(file) == ".wdat")
                        {
                            WorldData world = new WorldData(
                                Path.GetFileNameWithoutExtension(path).Replace(path, ""), 
                                file
                                );
                            world.Name = Path.GetFileNameWithoutExtension(path).Replace(path, "");
                            world.Directory = path;
                            
                            worlds.Add(world.Name, world);
                        }
                    }
                }

                _worlds = worlds;
                return worlds;
            }

            Directory.CreateDirectory(WorldDir);
            return new Dictionary<string, WorldData>();
        }

        public static bool WorldExists(string worldName = "New World")
        {
            foreach (string directories in Directory.GetDirectories(WorldDir))
            {
                if (File.Exists(Path.Combine(directories, worldName + ".wdat")))
                {
                    return true;
                }
            }

            return false;
        }

        public static WorldData CreateWorld(string worldname = "New World")
        {
            if (!WorldExists(worldname))
            {

                string dir = Path.Combine(WorldDir, worldname);
                
                Directory.CreateDirectory(dir);
                File.Create(Path.Combine(dir, worldname + ".wdat")).Close();

                return new WorldData(worldname, dir);
            }

            return _worlds[worldname];
        }
    }
}