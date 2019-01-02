using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelPlay {


	public enum INPUT_BUTTON_NAMES {
		Button1,
		Button2,
		Jump,
		Up,
		Down,
		LeftControl,
		LeftShift,
		LeftAlt,
		Build,
		Fly,
		Crouch,
		Inventory,
		Light,
		ThrowItem,
		Action,
		MiddleButton
	}

	public enum INPUT_BUTTON_STATE {
		Idle,
		Down,
		Up,
		Pressed
	}

	public abstract class VoxelPlayInputController {
		/// <summary>
		/// Horizontal input axis
		/// </summary>
		[NonSerialized]
		public float horizontalAxis;

		/// <summary>
		/// Vertical input axis
		/// </summary>
		[NonSerialized]
		public float verticalAxis;

		/// <summary>
		/// Horizontal mouse axis
		/// </summary>
		[NonSerialized]
		public float mouseX;

		/// <summary>
		/// Vertical mouse axis
		/// </summary>
		[NonSerialized]
		public float mouseY;

		/// <summary>
		/// Vertical mouse axis
		/// </summary>
		[NonSerialized]
		public float mouseScrollWheel;

		/// <summary>
		/// Location of cursor on screen
		/// </summary>
		[NonSerialized]
		public Vector3 screenPos;

		/// <summary>
		/// If cursor is inside screen
		/// </summary>
		[NonSerialized]
		public bool focused;

		/// <summary>
		/// If input is enabled (ie. user keys can be processed). Disable if you want to take control of keyboard or mouse and want to avoid player movement.
		/// </summary>
		public bool enabled;


		[NonSerialized]
		public bool initialized;

		protected INPUT_BUTTON_STATE[] buttons;

		protected virtual bool Initialize () {
			return true;
		}

		protected abstract void UpdateInputState ();

		/// <summary>
		/// Returns true if any button or key is pressed
		/// </summary>
		public bool anyKey;


		public bool GetButton (INPUT_BUTTON_NAMES button) {
			return initialized && buttons [(int)button] == INPUT_BUTTON_STATE.Pressed;
		}

		public bool GetButtonDown (INPUT_BUTTON_NAMES button) {
			return initialized && buttons [(int)button] == INPUT_BUTTON_STATE.Down;
		}

		public bool GetButtonUp (INPUT_BUTTON_NAMES button) {
			return initialized && buttons [(int)button] == INPUT_BUTTON_STATE.Up;
		}


		public void Init () {
			int buttonCount = Enum.GetNames (typeof(INPUT_BUTTON_NAMES)).Length;
			buttons = new INPUT_BUTTON_STATE[buttonCount];
			initialized = Initialize ();
			enabled = true;
		}


		public void Update () {
			if (!initialized)
				return;
			anyKey = Input.anyKey;
			for (int k = 0; k < buttons.Length; k++) {
				buttons [k] = INPUT_BUTTON_STATE.Idle;
			}
			if (!enabled)
				return;
			UpdateInputState ();
			if (!anyKey) {
				for (int k = 0; k < buttons.Length; k++) {
					if (buttons [k] != INPUT_BUTTON_STATE.Idle) {
						anyKey = true;
						break;
					}
				}
			}
		}


		protected void ReadButtonState (INPUT_BUTTON_NAMES button, string buttonName) {
			if (Input.GetButtonDown (buttonName)) {
				buttons [(int)button] = INPUT_BUTTON_STATE.Down;
			} else if (Input.GetButtonUp (buttonName)) {
				buttons [(int)button] = INPUT_BUTTON_STATE.Up;
			} else if (Input.GetButton (buttonName)) {
				buttons [(int)button] = INPUT_BUTTON_STATE.Pressed;
			}
		}


		protected void ReadKeyState (INPUT_BUTTON_NAMES button, KeyCode keyCode) {
			if (Input.GetKeyDown (keyCode)) {
				buttons [(int)button] = INPUT_BUTTON_STATE.Down;
			} else if (Input.GetKeyUp (keyCode)) {
				buttons [(int)button] = INPUT_BUTTON_STATE.Up;
			} else if (Input.GetKey (keyCode)) {
				buttons [(int)button] = INPUT_BUTTON_STATE.Pressed;
			}
		}
	
	}



}
