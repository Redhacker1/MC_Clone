using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Godot;
using MinecraftClone.World_CS.Generation;
using File = System.IO.File;
using Path = System.IO.Path;

namespace MinecraftClone.World_CS.Utility.IO
{
    public class ReigonFile : BaseFileHandler
    {
        const int RegionSize = 32;
        new const string FileExtension = "rgn";

        static int[] GetRegionFile(int x, int y)
        {
            int rx = x / RegionSize;
            int rz = y / RegionSize;
            return new[]{rx, rz};
        }

        public override string GetFilename(Vector2 chunkCoords, WorldData world, bool compressed)
        {
            int[] xy = GetRegionFile((int)chunkCoords.x, (int)chunkCoords.y);

            if (compressed)
            {
                return $"{xy[0]}{xy[1]}.{FileExtension}_c";
            }
            return $"{xy[0]}{xy[1]}.{FileExtension}";
        }

        public override bool ChunkExists(WorldData world, Vector2 location)
        {
            if (File.Exists(GetFilename(location, world, false)))
            {
                return true;
            }
            return File.Exists(GetFilename(location, world, true));
        }

        public override ChunkCs GetChunkData(WorldData world, Vector2 location, out bool chunkExists)
        {
            long offsetAmount = (long)location.x * (long)location.y * sizeof(long);
            bool compressData = false;
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
                compressData = true;
            }
            
            FileStream fs = File.OpenRead(filePath);
            DeflateStream compressor = null;
            BinaryReader fileReader = new BinaryReader(fs);
            
            if (compressData)
            {
                compressor = new DeflateStream(fs, CompressionMode.Decompress);
                fileReader = new BinaryReader(compressor);
            }
            
            SaveInfo saveData = new SaveInfo();
            fileReader.BaseStream.Seek(offsetAmount, SeekOrigin.Begin);

            long chunkStart = fileReader.ReadInt64();
            fileReader.BaseStream.Seek(chunkStart, SeekOrigin.Begin);
            
            
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


            fileReader.Close();
            compressor?.Close();
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


        SaveInfo[] GetAllChunksFromFile(string filePath, long offset ,bool compressData)
        {
            FileStream fs = File.OpenRead(filePath);
            DeflateStream compressor;
            BinaryReader fileReader = new BinaryReader(fs);
            
            if (compressData)
            {
                compressor = new DeflateStream(fs, CompressionMode.Decompress);
                fileReader = new BinaryReader(compressor);
            }
            
            SaveInfo[] chunks = new SaveInfo[(int) Math.Pow(RegionSize, 2)];

            for(int i = 0; i < chunks.Length; i++)
            {
                fileReader.BaseStream.Seek(i * 8, SeekOrigin.Begin);
                fileReader.BaseStream.Seek(fileReader.ReadInt64(), SeekOrigin.Begin);
                SaveInfo saveData = new SaveInfo();

                // TODO: Version number can be shared in grouped chunks
                saveData.VersionNumber = fileReader.ReadByte();
            
                saveData.BlockSize = fileReader.ReadByte();
                saveData.BiomeId = fileReader.ReadByte();
            
                short blockIds = fileReader.ReadInt16();

                Dictionary<byte, byte> blockDict = new Dictionary<byte, byte>();
                for (int blockIDs = 0; blockIDs < blockIds; blockIDs++)
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

                for (int blockPos = 0; blockPos <  serializedBlockData.Length; blockPos++)
                {
                    saveData.ChunkBlocks[blockPos] = blockDict[serializedBlockData[blockPos]];
                }
                chunks[i] = saveData;
            }

            return chunks;
        }

        public override void WriteChunkData(byte[] blocks, Vector2 chunkCoords, WorldData world, bool optimizeSave = true)
        {
            SaveInfo saveData = SerializeChunkData(blocks,chunkCoords, world, optimizeSave);
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

        }
    }
}