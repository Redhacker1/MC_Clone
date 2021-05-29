using System.Collections.Generic;
using Godot;

namespace MinecraftClone.World_CS.Blocks
{
	public struct BlockStruct
	{
		public bool Transparent;
		public bool NoCollision;
		public Vector2 Top;
		public Vector2 Bottom;
		public Vector2 Left;
		public Vector2 Right;
		public Vector2 Front;
		public Vector2 Back;
		public Vector2 Only;
		public List<string> TagsList;
	}
}
