﻿using System;
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
				buttons [(int)InputButtonNames.Button1] = InputButtonState.Down;
			} else if (Input.GetMouseButtonUp (0)) {
				buttons [(int)InputButtonNames.Button1] = InputButtonState.Up;
			} else if (Input.GetMouseButton(0)) {
				buttons [(int)InputButtonNames.Button1] = InputButtonState.Pressed;
			}
			// Right mouse button
			if (Input.GetMouseButtonDown (1)) {
				buttons [(int)InputButtonNames.Button2] = InputButtonState.Down;
			} else if (Input.GetMouseButtonUp (1)) {
				buttons [(int)InputButtonNames.Button2] = InputButtonState.Up;
			} else if (Input.GetMouseButton (1)) {
				buttons [(int)InputButtonNames.Button2] = InputButtonState.Pressed;
			}
			// Middle mouse button
			if (Input.GetMouseButtonDown (2)) {
				buttons [(int)InputButtonNames.MiddleButton] = InputButtonState.Down;
			} else if (Input.GetMouseButtonUp (2)) {
				buttons [(int)InputButtonNames.MiddleButton] = InputButtonState.Up;
			} else if (Input.GetMouseButton (2)) {
				buttons [(int)InputButtonNames.MiddleButton] = InputButtonState.Pressed;
			}
			// Jump key
			ReadButtonState(InputButtonNames.Jump, "Jump");
			ReadKeyState (InputButtonNames.Up, KeyCode.E);
			ReadKeyState (InputButtonNames.Down, KeyCode.Q);
			ReadKeyState (InputButtonNames.LeftControl, KeyCode.LeftControl);
			ReadKeyState (InputButtonNames.LeftShift, KeyCode.LeftShift);
			ReadKeyState (InputButtonNames.LeftAlt, KeyCode.LeftAlt);
			ReadKeyState (InputButtonNames.Build, KeyCode.B);
			ReadKeyState (InputButtonNames.Fly, KeyCode.F);
			ReadKeyState (InputButtonNames.Crouch, KeyCode.C);
			ReadKeyState (InputButtonNames.Inventory, KeyCode.Tab);
			ReadKeyState (InputButtonNames.Light, KeyCode.L);
			ReadKeyState (InputButtonNames.ThrowItem, KeyCode.G);
			ReadKeyState (InputButtonNames.Action, KeyCode.T);
		}

	
	}



}