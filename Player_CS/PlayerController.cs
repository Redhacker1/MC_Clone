using Godot;

namespace MinecraftClone.Player_CS
{
    public class PlayerController
    {
        Player pawn;
        Vector3 _velocity;
        public PlayerController(Player PawnReference)
        {
            pawn = PawnReference;
        }

        public void pmove(float delta)
        {
            //float xa = 0.0f, ya = 0.0f;

            Basis cameraBaseBasis = pawn.Transform.basis;
            Vector3 direction = new Vector3();
            
            
            if (Input.IsActionPressed("forward"))
            {
                direction -= cameraBaseBasis.z;
            }
            if (Input.IsActionPressed("backward"))
            {
                direction += cameraBaseBasis.z;
            }
                
            if (Input.IsActionPressed("left"))
            {
                direction -= cameraBaseBasis.x;
            }
                
            if (Input.IsActionPressed("right"))
            {
                direction += cameraBaseBasis.x;
            }


            if (Input.IsActionPressed("jump") && pawn.OnGround)
            {
                _velocity.y = .2f;
            }
            _velocity.x = direction.x * Player.Speed * delta;
            _velocity.z = direction.z * Player.Speed * delta;
            _velocity.y = _velocity.y - .01f;
            pawn.MoveRelative(_velocity.x, _velocity.z, Player.Speed);
            pawn.Move(_velocity);
        }
    }
}