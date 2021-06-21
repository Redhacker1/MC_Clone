using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Godot;
using MinecraftClone.World_CS.Generation;
using File = System.IO.File;
using Path = System.IO.Path;

namespace MinecraftClone.World_CS.Utility.IO
{
    public class ChunkFilesV1 : BaseFileHandler
    {

        
        public override ChunkCs GetChunkData(WorldData world, Vector2 location, out bool chunkExists)
        {
            bool compressed = false;
            // TODO Come up with newer and shorter filename structure that will work when I batch chunks together
            string filename = GetFilename(location,world,false);
            string filePath = Path.Combine(world.Directory, filename);
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(world.Directory,GetFilename(location,world,true));
                if (!File.Exists(filePath))
                {
                    chunkExists = false;
                    return null;
                }
                compressed = true;
            }
            FileStream fs = File.OpenRead(filePath);

            DeflateStream compressor = null;
            BinaryReader fileReader = new BinaryReader(fs);
            if (compressed)
            {
                compressor = new DeflateStream(fs, CompressionMode.Decompress);
                fileReader = new BinaryReader(compressor);
            }


            SaveInfo saveData = new SaveInfo();
            
            // TODO: Version number can be shared in grouped chunks
            saveData.VersionNumber = fileReader.ReadByte();
            
            saveData.BlockSize = fileReader.ReadByte();
            saveData.BiomeId = fileReader.ReadByte();
            
            short blockIds = fileReader.ReadInt16();

            Dictionary<byte, byte> blockDict = new Dictionary<byte, byte>();
            for (int i = 0; i < blockIds; i++)
            {
                // Block ID
                byte bid = fileReader.ReadByte();
                
                // Serialized ID
                byte sid = fileReader.ReadByte();
                
                blockDict.Add(sid, bid);
            }

            int x = fileReader.ReadInt32();
            int y = fileReader.ReadInt32();

            saveData.Location = new Vector2(x, y);
            
            // TODO: Add chunk dimensions to file format and have it calculate this value automatically
            byte[] serializedBlockData = fileReader.ReadBytes(16 * 16 * 384);
            saveData.ChunkBlocks = new byte[16 * 16 * 384];

            for (int i = 0; i <  serializedBlockData.Length; i++)
            {
                saveData.ChunkBlocks[i] = blockDict[serializedBlockData[i]];
            }

            if (compressed)
            {
                compressor.Close();    
            }
            
            fs.Close();

            ChunkCs referencedChunk = new ChunkCs
            {
                BlockData = saveData.ChunkBlocks,
                ChunkCoordinate = saveData.Location,
                Translation = new Vector3(ChunkCs.Dimension.x * saveData.Location.x, 0, ChunkCs.Dimension.x * saveData.Location.y)
            };
            


            chunkExists = true;
            return referencedChunk;
        }

        public void WriteChunkData_new(byte[] blocks, Vector2 chunkCoords, WorldData world, bool optimizeSave = true)
        {
            SaveInfo saveData = SerializeChunkData(blocks,chunkCoords, world, optimizeSave);
            
            var chunkdat = new MemoryStream();
            
            BinaryWriter fileWriter = new BinaryWriter(chunkdat);

            fileWriter.Write(saveData.VersionNumber);
            fileWriter.Write(saveData.BlockSize);
            fileWriter.Write(saveData.BiomeId);

            fileWriter.Write((short)saveData.BlockIdWriter.Count);
            foreach (KeyValuePair<byte, byte> blockIdPair in saveData.BlockIdWriter)
            {
                fileWriter.Write(blockIdPair.Key);
                fileWriter.Write(blockIdPair.Value);
            }
            
            
            fileWriter.Write((int)saveData.Location.x);
            fileWriter.Write((int)saveData.Location.y);

            foreach (byte block in saveData.ChunkBlocks)
            {
                fileWriter.Write(saveData.BlockIdWriter[block]);
            }


            FileStream fs = new FileStream(Path.Combine(world.Directory, GetFilename(chunkCoords, world, optimizeSave)), FileMode.Create);

            
            if (optimizeSave)
            {

                string uncompressedPath = Path.Combine(world.Directory, $"{world.Name}_x_{(int)saveData.Location.x}-y_{(int)saveData.Location.y}.cdat");
                if (File.Exists(uncompressedPath))
                {
                    File.Delete(uncompressedPath);
                }

                var cdat = Compress(chunkdat.ToArray());
                fs.Write(cdat, 0, cdat.Length);
            }
            else
            {
                fs.Write(chunkdat.ToArray(), 0, (int)chunkdat.Length);
            }
            
            fileWriter.Close();
            chunkdat.Close();
            fs.Close();


        }
        
        public override void WriteChunkData(byte[] blocks, Vector2 chunkCoords, WorldData world, bool optimizeSave = true)
        {
            WriteChunkData_new(blocks, chunkCoords, world, optimizeSave);
            /*SaveInfo saveData = SerializeChunkData(blocks,chunkCoords, world, optimizeSave);
            string filename;

            filename = GetFilename(chunkCoords, world, optimizeSave);


            string filePath = Path.Combine(world.Directory, filename);

            FileStream fs;
            if (!File.Exists(filePath))
            {
                fs = File.Create(filePath);   
            }
            else
            {
                fs = File.OpenWrite(filePath);
                fs.SetLength(0);
            }

            DeflateStream compressor = null;
            BinaryWriter fileWriter = new BinaryWriter(fs);
            if (optimizeSave)
            {
                compressor = new DeflateStream(fs,CompressionLevel.Fastest, true);
                fileWriter = new BinaryWriter(compressor);
                
                string uncompressedPath = Path.Combine(world.Directory, $"{world.Name}_x_{(int)saveData.Location.x}-y_{(int)saveData.Location.y}.cdat");
                if (File.Exists(uncompressedPath))
                {
                    File.Delete(uncompressedPath);
                }
            }

            fileWriter.Write(saveData.VersionNumber);
            fileWriter.Write(saveData.BlockSize);
            fileWriter.Write(saveData.BiomeId);

            fileWriter.Write((short)saveData.BlockIdWriter.Count);
            foreach (KeyValuePair<byte, byte> blockIdPair in saveData.BlockIdWriter)
            {
                fileWriter.Write(blockIdPair.Key);
                fileWriter.Write(blockIdPair.Value);
            }
            
            
            fileWriter.Write((int)saveData.Location.x);
            fileWriter.Write((int)saveData.Location.y);

            foreach (byte block in saveData.ChunkBlocks)
            {
                fileWriter.Write(saveData.BlockIdWriter[block]);
            }
            
            fileWriter.Flush();

            if (optimizeSave)
            { 
                compressor.Close();   
            }
            fs.Close();
            
            */
        }
        
        public static byte[] Compress(byte[] data)
        {
            byte[] compressArray = null;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
                {
                    deflateStream.Write(data, 0, data.Length);
                }
                compressArray = memoryStream.ToArray();
            }
            return compressArray;
        }

    }
}