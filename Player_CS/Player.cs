using System;
using Godot;
using MinecraftClone.Utility;
using MinecraftClone.Utility.CoreCompatibility;
using MinecraftClone.Utility.Physics;
using MinecraftClone.World_CS.Blocks;
using MinecraftClone.World_CS.Generation;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3; 

namespace MinecraftClone.Player_CS
{
	//[Tool]
	public class Player : Entity
	{
		
		public Camera _fpCam;
		RayCast _raycast;
		Label _infoLabel;
		Control _console;
		
		Vector3 Camdir = Vector3.Zero;
		
		
		public ProcWorld World;

		const float MouseSensitivity = 0.3f;
		public const float Gravity = 9.8f;
		float _cameraXRotation;

		string _selectedBlock = string.Empty;
		
		public const int Speed = 5;
		public const int JumpVel = 5;
		byte _selectedBlockIndex;

		PlayerController _controller;

		bool _paused;


		void toggle_pause()
		{
			_paused =! _paused;
			GetTree().Paused = _paused;
			Input.SetMouseMode(_paused ? Input.MouseMode.Visible : Input.MouseMode.Captured);
		}

		public override void _Ready()
		{

			SetPos(new Vector3(Translation.x, Translation.y, Translation.z));
			_controller = new PlayerController(this);

			// Facinating
			BlockHelper.RegisterBaseBlocks();

			_selectedBlock = BlockHelper.IdToString[_selectedBlockIndex];
			_console = GetNode("CameraBase/Camera/Control") as Control;

			_fpCam = GetNode<Camera>("CameraBase/Camera");
			_raycast = GetNode<RayCast>("CameraBase/Camera/RayCast");
			_infoLabel = GetNode<Label>("CameraBase/Camera/Debug_line_01");

			if (!Engine.EditorHint)
			{
				Input.SetMouseMode(Input.MouseMode.Captured);	
			}
		}

		public override void _Process(float delta)
		{
			var forward_cam = _fpCam.GlobalTransform.basis;
			var forward = -forward_cam.z.Normalized();
			
			WorldScript.lines.DrawRay(_fpCam.Transform.origin, forward * 5, Colors.Red, delta);
			
			if (!Engine.EditorHint)
			{
				if (Input.IsActionJustPressed("pause"))
				{
					toggle_pause();
				}
				if (Input.IsActionJustPressed("console"))
				{
					_console.Visible = !_console.Visible;

					if (_console.Visible)
					{
						Input.SetMouseMode(Input.MouseMode.Visible);   
					}
					else
					{
						Input.SetMouseMode(Input.MouseMode.Captured);
					}

					_paused = _console.Visible;
				}

				if (Input.IsActionJustReleased("scroll_up"))
				{

					_selectedBlockIndex = (byte) Math.Max(0, _selectedBlockIndex - 1);
				}
				else if (Input.IsActionJustReleased("scroll_down"))
				{
					_selectedBlockIndex = (byte) Math.Min( BlockHelper.BlockTypes.Count - 1, _selectedBlockIndex + 1);
				}

				_selectedBlock = BlockHelper.IdToString[_selectedBlockIndex];	
			}


		}

		public override void _PhysicsProcess(float delta)
		{

			float cx = (float) Math.Floor((Translation.x ) / ChunkCs.Dimension.X);
			float cz = (float) Math.Floor((Translation.z) / ChunkCs.Dimension.Z);
			float px = Translation.x - cx * ChunkCs.Dimension.X;
			float py = Translation.y;
			float pz = Translation.z - cz * ChunkCs.Dimension.Z;
			
			
			var forward_cam = _fpCam.GlobalTransform.basis;
			var forward = -forward_cam.z.CastToCore();
			
			_infoLabel.Text = $"Selected block {_selectedBlock}, Chunk ({cx}, {cz}) pos ({px}, {py}, {pz}, CameraDir {forward})";
			
			
			

			if (!_paused)
			{
				HitResult result = Raycast.CastInDirection(_fpCam.GlobalTransform.origin.CastToCore(),forward, -1, 5);
				Vector3 pos = result.Location;
				
				if (!Engine.EditorHint)
				{
					_controller.Player_move(delta);
					WorldScript.lines.DrawBlock((int) pos.X, (int) pos.Y, (int) pos.Z, delta);
				}

				if (result.Hit)
				{
					Vector3 norm = result.Normal;
					
					_on_Player_highlight_block(pos, norm, delta);

					if (!Engine.EditorHint)
					{
						if (Input.IsActionJustPressed("click"))
						{
							GD.Print("Click");
							_on_Player_destroy_block(pos, norm);
						}
						else if(Input.IsActionJustPressed("right_click"))
						{
							WorldScript.lines.DrawRay(_fpCam.Transform.origin, (forward * 5).CastToGodot(), Colors.Red, delta);

							if (Vector3.Distance(pos, Translation.CastToCore()) > 1.2)
							{
								int by = (int) (MathHelper.Modulo(MathHelper.Round(pos.Y), ChunkCs.Dimension.Y) + .5);
								_on_Player_place_block(pos,norm, _selectedBlock);
								if (!OnGround )
								{
									Translation = new Vector3(Translation.x, by + .5f, Translation.z).CastToGodot();   
								}
							}
						
						}	
					}
				}
			}
		}
		
		void _on_Player_destroy_block(Vector3 pos, Vector3 norm)
		{
			pos += norm * .5f;

			int cx = (int) Math.Floor(pos.X / ChunkCs.Dimension.X);
			int cz = (int) Math.Floor(pos.Z / ChunkCs.Dimension.Z);

			int bx = (int) (MathHelper.Modulo((float) Math.Floor(pos.X), ChunkCs.Dimension.X) + .5f);
			int by = (int) (MathHelper.Modulo((float) Math.Floor(pos.Y), ChunkCs.Dimension.Y) + .5f);
			int bz = (int) (MathHelper.Modulo((float) Math.Floor(pos.Z), ChunkCs.Dimension.Z) + .5f);
			
			
			World?.change_block(cx, cz, bx, by, bz, 0);
		}
		
		
		void _on_Player_place_block(Vector3 pos, Vector3 norm, string type)
		{
			pos += norm * .5f;

			int cx = (int) Math.Floor(pos.X / ChunkCs.Dimension.X);
			int cz = (int) Math.Floor(pos.Z / ChunkCs.Dimension.Z);

			int bx = (int) (MathHelper.Modulo((float) Math.Floor(pos.X), ChunkCs.Dimension.X) + 0.5);
			int by = (int) (MathHelper.Modulo((float) Math.Floor(pos.Y), ChunkCs.Dimension.Y) + 0.5);
			int bz = (int) (MathHelper.Modulo((float) Math.Floor(pos.Z), ChunkCs.Dimension.Z) + 0.5);

			World?.change_block(cx, cz, bx, by, bz, _selectedBlockIndex);	
			
		}
		

		void _on_Player_highlight_block(Vector3 pos, Vector3 norm, float delta)
		{
			pos -= norm * .5f;
			
			int cx = (int) Math.Floor(pos.X / ChunkCs.Dimension.X);
			int cz = (int) Math.Floor(pos.Z / ChunkCs.Dimension.Z);

			int bx = (int) (MathHelper.Modulo((float) Math.Floor(pos.X), ChunkCs.Dimension.X) + 0.5);
			int by = (int) (MathHelper.Modulo((float) Math.Floor(pos.Y), ChunkCs.Dimension.Y) + 0.5);
			int bz = (int) (MathHelper.Modulo((float) Math.Floor(pos.Z), ChunkCs.Dimension.Z) + 0.5);
			
			WorldScript.lines.DrawBlock((int) (cx * ChunkCs.Dimension.X + bx), @by, (int) (cz * ChunkCs.Dimension.Z + bz), delta);
			

		}
		
		public void _on_Player_unhighlight_block()
		{
			
		}

		public override void _Input(InputEvent @event)
		{
			if (!_paused)
			{
				if (@event is InputEventMouseMotion mouseEvent)
				{
					RotateY(MathHelper.DegreesToRadians(-(mouseEvent.Relative.x * MouseSensitivity)));
					Camdir.X = RotationDegrees.y;
					float xDelta = mouseEvent.Relative.y * MouseSensitivity;

					if (_cameraXRotation + xDelta > -90 && _cameraXRotation + xDelta < 90)
					{
						_fpCam?.RotateX(MathHelper.DegreesToRadians(-xDelta));
						Camdir.Y = _fpCam.RotationDegrees.x;
						_cameraXRotation += xDelta;
					}

				}
			}
		}
	}
}
