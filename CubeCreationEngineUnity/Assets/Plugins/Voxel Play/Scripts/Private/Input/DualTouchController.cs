using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelPlay
{

	public class DualTouchController : VoxelPlayInputController
	{

		public float dragThreshold = 10f;
		public float rotationSpeed = 0.1f;
		public float alpha = 0.7f;
		public float fadeInSpeed = 2f;

		GameObject touchControls;
		bool dragged;
		Rect buttonJumpRect, buttonCrouchRect, buttonBuildRect, buttonInventoryRect;
		CanvasGroup canvasGroup;
		float startTime;
		bool leftTouched;
		bool pressingFire;
		float pressTime, liftTime;
		Vector3 leftTouchPos;

		protected override bool Initialize ()
		{
			touchControls = GameObject.Instantiate<GameObject> (Resources.Load<GameObject> ("VoxelPlay/UI/CanvasTouch"));
			touchControls.name = "VoxelPlay Touch Interface";
			canvasGroup = touchControls.GetComponent<CanvasGroup> ();
			canvasGroup.alpha = 0;
			buttonBuildRect = GetRect (touchControls.transform.Find ("ButtonBuild"));
			buttonJumpRect = GetRect (touchControls.transform.Find ("ButtonJump"));
			buttonCrouchRect = GetRect (touchControls.transform.Find ("ButtonCrouch"));
			Transform tInventory = touchControls.transform.Find ("ButtonInventory");
			buttonInventoryRect = GetRect (tInventory);
			tInventory.gameObject.SetActive (false);
			startTime = Time.time;
			return true;
		}

		protected override void UpdateInputState ()
		{
			if (canvasGroup.alpha < alpha) {
				float t = (Time.time - startTime) / fadeInSpeed;
				if (t > alpha)
					t = alpha;
				canvasGroup.alpha = t;
			}

			screenPos = Input.mousePosition;
			focused = true;

			leftTouched = false;
			int touchCount = Input.touchCount;
			for (int k = 0; k < touchCount; k++) {
				ManageTouch (k);
			}
			if (!leftTouched) {
				horizontalAxis = verticalAxis = 0;
			}
		}

		void ManageTouch (int touchIndex)
		{
			Touch t = Input.touches [touchIndex];

			if (t.position.x < Screen.width / 2) {
				leftTouched = true;
				if (t.phase == TouchPhase.Began) {
					leftTouchPos = t.position;
				} else if (t.phase == TouchPhase.Moved) {
					horizontalAxis = t.position.x - leftTouchPos.x;
					verticalAxis = t.position.y - leftTouchPos.y;
				} else if (t.phase == TouchPhase.Ended) {
					leftTouched = false;
				}
			} else {
				if (t.phase == TouchPhase.Began) {
					dragged = false;
					if (buttonBuildRect.Contains (t.position)) {
						buttons [(int)INPUT_BUTTON_NAMES.Button2] = INPUT_BUTTON_STATE.Down;
						return;
					}
					if (buttonJumpRect.Contains (t.position)) {
						buttons [(int)INPUT_BUTTON_NAMES.Jump] = INPUT_BUTTON_STATE.Down;
						return;
					}
					if (buttonCrouchRect.Contains (t.position)) {
						buttons [(int)INPUT_BUTTON_NAMES.Crouch] = INPUT_BUTTON_STATE.Down;
						return;
					}
					if (buttonInventoryRect.Contains (t.position)) {
						buttons [(int)INPUT_BUTTON_NAMES.Inventory] = INPUT_BUTTON_STATE.Down;
						return;
					}
					buttons [(int)INPUT_BUTTON_NAMES.Button1] = INPUT_BUTTON_STATE.Down;
					pressTime = Time.time;
					pressingFire = (Time.time - liftTime) < 0.3f;
				} else if (t.phase == TouchPhase.Moved) {
					float deltaX = t.deltaPosition.x;
					if (deltaX > 0) {
						deltaX -= dragThreshold;
						if (deltaX < 0)
							deltaX = 0;
					} else if (deltaX < 0) {
						deltaX += dragThreshold;
						if (deltaX > 0)
							deltaX = 0;
					}
					deltaX *= rotationSpeed;
					mouseX = mouseX * 0.9f + deltaX * 0.1f;

					float deltaY = t.deltaPosition.y;
					if (deltaY > 0) {
						deltaY -= dragThreshold;
						if (deltaY < 0)
							deltaY = 0;
					} else if (deltaY < 0) {
						deltaY += dragThreshold;
						if (deltaY > 0)
							deltaY = 0;
					}
					deltaY *= rotationSpeed;
					mouseY = mouseY * 0.9f + deltaY * 0.1f;
					dragged = mouseX != 0 || mouseY != 0;
				} else if (t.phase == TouchPhase.Ended) {
					mouseX = mouseY = 0;
					pressingFire = false;
					if (!dragged && Time.time - pressTime < 0.3f) {
						buttons [(int)INPUT_BUTTON_NAMES.Button1] = INPUT_BUTTON_STATE.Pressed;
						liftTime = Time.time;
					}
				} else {
					dragged = false;
				}
				if (pressingFire) {
					buttons [(int)INPUT_BUTTON_NAMES.Button1] = INPUT_BUTTON_STATE.Pressed;
				}

			}
		}

	
		Rect GetRect (Transform t)
		{
			RectTransform rt = t.GetComponent<RectTransform> ();
			return new Rect (Mathf.RoundToInt (rt.anchorMin.x * Screen.width), Mathf.RoundToInt (rt.anchorMin.y * Screen.height), Mathf.RoundToInt ((rt.anchorMax.x - rt.anchorMin.x) * Screen.width), Mathf.RoundToInt ((rt.anchorMax.y - rt.anchorMin.y) * Screen.height));
		}

	}



}
