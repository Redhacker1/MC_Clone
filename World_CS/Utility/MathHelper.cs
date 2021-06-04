using System;

namespace MinecraftClone.World_CS.Utility
{
    public static class MathHelper
    {
        public static double DegreesToRadians(double YRotation)
        {
            return YRotation * (Math.PI / 180);
        }
    }
}