using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MinecraftClone.World_CS.Blocks
{
    public static class BlockHelper
    {
        public static readonly List<BlockStruct> BlockTypes = new List<BlockStruct>();
        public static readonly List<string> IdToString = new List<string>();
        public static readonly Dictionary<string, byte> StringToId = new Dictionary<string, byte>();

        public static void RegisterBaseBlocks()
        {
            List<string> airTags = new List<string> {"Air", "Transparent"};
            RegisterBlock(new Vector2(-1, -1), airTags, "Air", 0);

            List<string> dirtTags = new List<string>();
            RegisterBlock(new Vector2(1, 0), dirtTags, "Dirt", 1);

            List<string> cactusTags = new List<string>();
            RegisterBlock(new Vector2(3, 2), cactusTags, "Cactus", 2);

            List<string> flowerTags = new List<string> {"Flat", "Transparent", "No Collision"};
            RegisterBlock(new Vector2(2, 3), flowerTags, "Flower", 3);

            string[] grassTags = { };
            Vector2[] grassSides =
            {
                new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 1),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1)
            };
            RegisterBlock(grassSides, grassTags, "Grass", 4);

            List<string> leafTags = new List<string> {"Transparent"};
            RegisterBlock(new Vector2(2, 1), leafTags, "Leaf", 5);

            string[] logTags = { };
            Vector2[] logSides =
            {
                new Vector2(3, 0), new Vector2(3, 0), new Vector2(2, 0),
                new Vector2(2, 0), new Vector2(2, 0), new Vector2(2, 0)
            };
            RegisterBlock(logSides, logTags, "Log", 6);

            List<string> pineLeafTags = new List<string> {"Transparent"};
            RegisterBlock(new Vector2(3, 1), pineLeafTags, "Pine Leaf block", 7);

            List<string> sandTags = new List<string>();
            RegisterBlock(new Vector2(2, 2), sandTags, "Sand", 8);

            List<string> snowTags = new List<string>();
            RegisterBlock(new Vector2(1, 2), snowTags, "Snow", 9);

            List<string> stoneTags = new List<string>();
            RegisterBlock(new Vector2(0, 0), stoneTags, "Stone", 10);

            List<string> tallGrassTags = new List<string> {"Flat", "Transparent", "No Collision"};
            RegisterBlock(new Vector2(1, 3), tallGrassTags, "Tall Grass", 11);

            List<string> woodTags = new List<string>();
            RegisterBlock(new Vector2(4, 0), woodTags, "Wood", 12);
            
            List<string> borderTags = new List<string> {"Unbreakable"};
            RegisterBlock(new Vector2(4, 2), borderTags, "Barrier", 13);
            RegisterBlock(new Vector2(5, 0), borderTags, "Barrier_02", 14);
        }

        static void RegisterBlock(Vector2[] sides, ICollection<string> tags, string name, byte id)
        {
            if (sides == null) throw new ArgumentNullException(nameof(sides));
            BlockStruct block = new BlockStruct {Transparent = false, NoCollision = false};
            if (tags.Contains("Transparent", StringComparer.CurrentCultureIgnoreCase))
            {
                block.Transparent = true;
                tags.Remove("Transparent");
            }

            if (tags.Contains("air", StringComparer.CurrentCultureIgnoreCase))
            {
                tags.Remove("Air");
                tags.Remove("air");
                block.Air = true;
            }
            if (tags.Contains("No Collision", StringComparer.CurrentCultureIgnoreCase))
            {
                block.Transparent = true;
                tags.Remove("No Collision");
            }
            if (tags.Contains("Flat", StringComparer.CurrentCultureIgnoreCase))
            {
                block.Only = sides[0];
            }
            else
            {
                block.Top = sides[0];
                block.Bottom = sides[1];
                block.Left = sides[2];
                block.Right = sides[3];
                block.Front = sides[4];
                block.Back = sides[5];
            }

            block.TagsList = tags.ToList();
            BlockTypes.Insert(id, block);
            StringToId[name] = id;
            IdToString.Insert(id, name);

        }

        static void RegisterBlock(Vector2 sides, ICollection<string> tags, string name, byte id)
        {
            BlockStruct block = new BlockStruct();
            if (tags.Contains("Transparent"))
            {
                block.Transparent = true;
                tags.Remove("Transparent");
            }
            if (tags.Contains("No Collision"))
            {
                block.NoCollision = true;
                tags.Remove("No Collision");
            }
            if (tags.Contains("Flat"))
            {
                block.Only = sides;
            }
            else
            {
                block.Top = sides;
                block.Bottom = sides;
                block.Left = sides;
                block.Right = sides;
                block.Front = sides;
                block.Back = sides;
            }

            block.TagsList = tags.ToList();
            BlockTypes.Insert(id, block);
            StringToId[name] = id;
            IdToString.Insert(id, name);

        }

        public static bool block_have_collision(string blockType)
        {
            return BlockTypes[StringToId[blockType]].NoCollision;
        }
        public static bool block_have_collision(byte blockType)
        {
            return BlockTypes[blockType].NoCollision;
        }
    }
}