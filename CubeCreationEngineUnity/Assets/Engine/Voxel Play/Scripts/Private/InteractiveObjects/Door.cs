using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

namespace VoxelPlay {
	
	public class Door : VoxelPlayInteractiveObject {

		public float speed = 50f;

		public AudioClip sound;


		[NonSerialized]
		public bool isOpen;

		bool shown;
		WaitForEndOfFrame nextFrame;
		bool rotating;
		float targetRotation;
		float baseRotation;

		public override void OnStart () {
			nextFrame = new WaitForEndOfFrame ();
			baseRotation = transform.eulerAngles.y + 360;
		}

		public override void OnPlayerApproach () {
			if (!shown) {
				env.ShowMessage ("<color=green>Press </color><color=yellow>T</color> to open/close this door.");
				shown = true;
			}
		}

		public override void OnPlayerGoesAway () {
		}

		public override void OnPlayerAction () {
			if (speed <= 0)
				return;

			float openRotation = customTag.Equals ("left") ? -90 : 90;
			isOpen = !isOpen;
			if (isOpen && sound != null) {
				AudioSource.PlayClipAtPoint (sound, transform.position);
			}
			targetRotation = isOpen ? baseRotation + openRotation : baseRotation;
			if (!rotating) {
				rotating = true;
				StartCoroutine (RotateDoor ());
			}
		}

		IEnumerator RotateDoor () {

			for (;;) {
				float angY = transform.rotation.eulerAngles.y + 360f;
				float direction = targetRotation > angY ? 1 : -1;
				float incY = speed * Time.deltaTime * direction;
				bool ends = false;
				if (incY > 0 && angY + incY > targetRotation) {
					incY = targetRotation - angY;
					ends = true;
				} else if (incY < 0 && angY + incY < targetRotation) {
					incY = targetRotation - angY;
					ends = true;
				}
				angY += incY;
				transform.eulerAngles = new Vector3 (0, angY, 0);
				if (ends) {
					if (!isOpen && sound != null) {
						AudioSource.PlayClipAtPoint (sound, transform.position);
					}
					rotating = false;
					yield break;
				}
				yield return nextFrame;
			}
		
		}
	
	}

}