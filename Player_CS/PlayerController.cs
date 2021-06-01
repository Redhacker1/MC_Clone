using Godot;

namespace MinecraftClone.Player_CS
{
    public class PlayerController
    {
        Player pawn;
        PlayerController(Player PawnReference)
        {
            pawn = PawnReference;
        }
        
        public Vector3 GetWishdir()
        {
            Vector3 direction = new Vector3();
            
            
            if (Input.IsActionPressed("forward"))
            {
                direction.z -= 1;
            }
            if (Input.IsActionPressed("backward"))
            {
                direction.z += 1;
            }
                
            if (Input.IsActionPressed("left"))
            {
                direction.x -= 1;
            }
                
            if (Input.IsActionPressed("right"))
            {
                direction.x += 1;
            }

            direction *= pawn.Rotation;
            
            return direction;
        }
    }
}