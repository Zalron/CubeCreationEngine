using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelPlay
{

	public class KeyboardMouseController : VoxelPlayInputController
	{

		protected override void UpdateInputState ()
		{

			screenPos = Input.mousePosition;
			focused = screenPos.x >= 0 && screenPos.x < Screen.width && screenPos.y >= 0 && screenPos.y < Screen.height;

			mouseX =  Input.GetAxis ("Mouse X");
			mouseY =  Input.GetAxis ("Mouse Y");
			mouseScrollWheel = Input.GetAxis ("Mouse ScrollWheel");
			horizontalAxis = Input.GetAxis ("Horizontal");
			verticalAxis = Input.GetAxis ("Vertical");

			// Left mouse button
			if (Input.GetMouseButtonDown (0)) {
				buttons [(int)INPUT_BUTTON_NAMES.Button1] = INPUT_BUTTON_STATE.Down;
			} else if (Input.GetMouseButtonUp (0)) {
				buttons [(int)INPUT_BUTTON_NAMES.Button1] = INPUT_BUTTON_STATE.Up;
			} else if (Input.GetMouseButton(0)) {
				buttons [(int)INPUT_BUTTON_NAMES.Button1] = INPUT_BUTTON_STATE.Pressed;
			}
			// Right mouse button
			if (Input.GetMouseButtonDown (1)) {
				buttons [(int)INPUT_BUTTON_NAMES.Button2] = INPUT_BUTTON_STATE.Down;
			} else if (Input.GetMouseButtonUp (1)) {
				buttons [(int)INPUT_BUTTON_NAMES.Button2] = INPUT_BUTTON_STATE.Up;
			} else if (Input.GetMouseButton (1)) {
				buttons [(int)INPUT_BUTTON_NAMES.Button2] = INPUT_BUTTON_STATE.Pressed;
			}
			// Middle mouse button
			if (Input.GetMouseButtonDown (2)) {
				buttons [(int)INPUT_BUTTON_NAMES.MiddleButton] = INPUT_BUTTON_STATE.Down;
			} else if (Input.GetMouseButtonUp (2)) {
				buttons [(int)INPUT_BUTTON_NAMES.MiddleButton] = INPUT_BUTTON_STATE.Up;
			} else if (Input.GetMouseButton (2)) {
				buttons [(int)INPUT_BUTTON_NAMES.MiddleButton] = INPUT_BUTTON_STATE.Pressed;
			}
			// Jump key
			ReadButtonState(INPUT_BUTTON_NAMES.Jump, "Jump");
			ReadKeyState (INPUT_BUTTON_NAMES.Up, KeyCode.E);
			ReadKeyState (INPUT_BUTTON_NAMES.Down, KeyCode.Q);
			ReadKeyState (INPUT_BUTTON_NAMES.LeftControl, KeyCode.LeftControl);
			ReadKeyState (INPUT_BUTTON_NAMES.LeftShift, KeyCode.LeftShift);
			ReadKeyState (INPUT_BUTTON_NAMES.LeftAlt, KeyCode.LeftAlt);
			ReadKeyState (INPUT_BUTTON_NAMES.Build, KeyCode.B);
			ReadKeyState (INPUT_BUTTON_NAMES.Fly, KeyCode.F);
			ReadKeyState (INPUT_BUTTON_NAMES.Crouch, KeyCode.C);
			ReadKeyState (INPUT_BUTTON_NAMES.Inventory, KeyCode.Tab);
			ReadKeyState (INPUT_BUTTON_NAMES.Light, KeyCode.L);
			ReadKeyState (INPUT_BUTTON_NAMES.ThrowItem, KeyCode.G);
			ReadKeyState (INPUT_BUTTON_NAMES.Action, KeyCode.T);
		}

	
	}



}
