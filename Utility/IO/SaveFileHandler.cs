using System.Collections.Generic;
using System.Linq;

using Vector2 = System.Numerics.Vector2;
using MinecraftClone.World_CS.Generation;
using File = System.IO.File;

namespace MinecraftClone.Utility.IO
{
    public static class SaveFileHandler
    {
        // Possible Save Handlers
        static readonly List<BaseFileHandler> ValidFormats = new List<BaseFileHandler>();
        
        // TODO: Save files in reigon file
        static readonly BaseFileHandler DefaultSaveFileFormat = new ChunkFilesV1();
        
        
        public static void WriteChunkData(byte[] blocks, Vector2 chunkCoords, WorldData world, bool optimizeSave = true)
        {
            foreach (BaseFileHandler format in ValidFormats)
            {
                if (format.ChunkExists(world, chunkCoords) && format != DefaultSaveFileFormat)
                {
                    File.Delete(format.GetFilename(chunkCoords, world, true));
                    File.Delete(format.GetFilename(chunkCoords, world, false));
                }
            }
            DefaultSaveFileFormat.WriteChunkData(blocks,chunkCoords,world,optimizeSave);
        }

        public static bool ChunkExists(WorldData world, Vector2 location)
        {
            if (DefaultSaveFileFormat.ChunkExists(world, location) == false)
            {
                return ValidFormats.Any(format => format.ChunkExists(world, location));   
            }
            return true;
        }

        public static ChunkCs GetChunkData(ProcWorld worldref ,WorldData world, Vector2 location, out bool chunkExists)
        {
            ChunkCs data = DefaultSaveFileFormat.GetChunkData(world, location, out chunkExists);
            if (data == null)
            {
                foreach (BaseFileHandler format in ValidFormats)
                {
                    data = format.GetChunkData(world, location, out chunkExists);
                    if (data != null)
                    {
                        return data;
                    }
                }   
            }
            
            return data;
        }
    }
}