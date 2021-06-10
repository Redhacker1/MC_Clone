namespace MinecraftClone.World_CS.Utility.Physics
{
    public struct Prisim
    {
        Prisim(AABB bounding_box)
        {
           var a =  bounding_box.A.x + bounding_box.A.z;
        }
    }
}