using System;
using Godot;
using MinecraftClone.World_CS.Blocks;
using MinecraftClone.World_CS.Generation;
using MinecraftClone.World_CS.Utility;
using MinecraftClone.World_CS.Utility.Debug;
using MinecraftClone.World_CS.Utility.Physics;

namespace MinecraftClone.Player_CS
{
	[Tool]
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
		int _selectedBlockIndex = 0;

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
			else
			{
				
			}
			
		}

		public override void _Process(float delta)
		{
			var forward_cam = _fpCam.GlobalTransform.basis;
			var forward = -forward_cam.z;
			
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
					_selectedBlockIndex -= 1;
				
					if (_selectedBlockIndex < 0)
					{
						_selectedBlockIndex = BlockHelper.IdToString.Count -1;
					}
				}
				else if (Input.IsActionJustReleased("scroll_down"))
				{
					_selectedBlockIndex += 1;

					if (_selectedBlockIndex > BlockHelper.IdToString.Count - 1)
					{
						_selectedBlockIndex = 0;
					}
				}

				_selectedBlock = BlockHelper.IdToString[_selectedBlockIndex];	
			}

			if (!_paused)
			{
				//World.Get_aabbs(0, AABB);
				AABB?.DrawDebug();
			}
			

		}

		public override void _PhysicsProcess(float delta)
		{

			float cx = (float) Math.Floor((Translation.x ) / ChunkCs.Dimension.x);
			float cz = (float) Math.Floor((Translation.z) / ChunkCs.Dimension.x);
			float px = Translation.x - cx * ChunkCs.Dimension.x;
			float py = Translation.y;
			float pz = Translation.z - cz * ChunkCs.Dimension.x;
			
			
			var forward_cam = _fpCam.GlobalTransform.basis;
			var forward = -forward_cam.z;
			
			_infoLabel.Text = $"Selected block {_selectedBlock}, Chunk ({cx}, {cz}) pos ({px}, {py}, {pz}, CameraDir {forward})";
			
			
			

			if (!_paused)
			{
				HitResult result = Raycast.CastInDirection(_fpCam.GlobalTransform.origin,forward, -1, 5);

				if (!Engine.EditorHint)
				{
					_controller.Player_move(delta);	
				}

				if (result.Hit)
				{
					Vector3 pos = result.Location;
					Vector3 norm = result.Normal;

					if (!Engine.EditorHint)
					{
						if (Input.IsActionJustPressed("click"))
						{
							GD.Print("Click");
							_on_Player_destroy_block(pos, norm);
						}
						else if(Input.IsActionJustPressed("right_click"))
						{
							WorldScript.lines.DrawRay(_fpCam.Transform.origin, forward * 5, Colors.Red, delta);

							if (pos.DistanceTo(Translation) > 1.2)
							{
								int by = (int) (Mathf.PosMod(Mathf.Round(pos.y + 1), ChunkCs.Dimension.y) + .5);
								_on_Player_place_block(pos,norm, _selectedBlock);
								if (!OnGround )
								{
									Translation = new Vector3(Translation.x, by + .5f, Translation.z);   
								}
							}
						
						}	
					}
				}
			}
		}
		
		void _on_Player_destroy_block(Vector3 pos, Vector3 norm)
		{
			//pos -= norm * .5f;

			int cx = (int) Math.Floor(pos.x / ChunkCs.Dimension.x);
			int cz = (int) Math.Floor(pos.z / ChunkCs.Dimension.x);

			int bx = (int) (Mathf.PosMod((float) Math.Floor(pos.x), ChunkCs.Dimension.x) + 0.5);
			int by = (int) (Mathf.PosMod((float) Math.Floor(pos.y), ChunkCs.Dimension.y) + 0.5);
			int bz = (int) (Mathf.PosMod((float) Math.Floor(pos.z), ChunkCs.Dimension.x) + 0.5);

			World?.change_block(cx, cz, bx, by, bz, 0);
		}
		
		
		void _on_Player_place_block(Vector3 pos, Vector3 norm, string type)
		{
			pos += norm * .5f;

			int cx = (int) Mathf.Floor(pos.x / ChunkCs.Dimension.x);
			int cz = (int) Mathf.Floor(pos.z / ChunkCs.Dimension.x);

			int bx = (int) (Mathf.PosMod(Mathf.Floor(pos.x), ChunkCs.Dimension.x) + 0.5);
			int by = (int) (Mathf.PosMod(Mathf.Floor(pos.y), ChunkCs.Dimension.y) + 0.5);
			int bz = (int) (Mathf.PosMod(Mathf.Floor(pos.z), ChunkCs.Dimension.x) + 0.5);

			World?.change_block(cx, cz, bx, by, bz, BlockHelper.StringToId[type]);	
			
		}
		

		void _on_Player_highlight_block(Vector3 pos, Vector3 norm)
		{

			pos -= norm * .5f;

			double bx = Mathf.Floor(pos.x) + 0.5;
			double by = Mathf.Floor(pos.y) + 0.5;
			double bz = Mathf.Floor(pos.z) + 0.5;
			
			double px = Mathf.Floor(Translation.x) + 0.5;
			double py = Mathf.Floor(Translation.y) + 0.5;
			double pz = Mathf.Floor(Translation.z) + 0.5;
			
			
			//CubeLocation = new Vector3((float) (bx - px), (float) (by - py), (float) (bz - pz));

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
					RotateY(Mathf.Deg2Rad(-(mouseEvent.Relative.x * MouseSensitivity)));
					Camdir.x = RotationDegrees.y;
					float xDelta = mouseEvent.Relative.y * MouseSensitivity;

					if (_cameraXRotation + xDelta > -90 && _cameraXRotation + xDelta < 90)
					{
						_fpCam?.RotateX(Mathf.Deg2Rad(-xDelta));
						Camdir.y = _fpCam.RotationDegrees.x;
						_cameraXRotation += xDelta;
					}

				}
			}
		}
	}
}
