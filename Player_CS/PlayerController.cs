using Godot;
using MinecraftClone.Utility.CoreCompatibility;
using Vector3 = System.Numerics.Vector3;

namespace MinecraftClone.Player_CS
{
    public class PlayerController
    {
        readonly Player pawn;
        Vector3 _velocity;
        public PlayerController(Player PawnReference)
        {
            pawn = PawnReference;
        }

        public void Player_move(float delta)
        {

            //float xa = 0.0f, ya = 0.0f;

            Basis cameraBaseBasis = pawn.Transform.basis;
            Vector3 direction = new Vector3();
            
            
            if (Input.IsActionPressed("forward"))
            {
                direction -= cameraBaseBasis.z.CastToCore();
            }
            if (Input.IsActionPressed("backward"))
            {
                direction += cameraBaseBasis.z.CastToCore();
            }
                
            if (Input.IsActionPressed("left"))
            {
                direction -= cameraBaseBasis.x.CastToCore();
            }
                
            if (Input.IsActionPressed("right"))
            {
                direction += cameraBaseBasis.x.CastToCore();
            }
            
            if (!pawn.OnGround)
            {
                _velocity.Y -= .2f * delta;   
            }
            else
            {
                _velocity.Y = 0;
            }


            if (Input.IsActionPressed("jump") && pawn.OnGround)
            {
                _velocity.Y = 6f * delta;
            }
            _velocity.X = direction.X * Player.Speed * delta;
            _velocity.Z = direction.Z * Player.Speed * delta;

            pawn.Pos = _velocity;
            pawn.MoveRelative(_velocity.X, _velocity.Z, Player.Speed);
            pawn.Move(_velocity);
        }
    }
}