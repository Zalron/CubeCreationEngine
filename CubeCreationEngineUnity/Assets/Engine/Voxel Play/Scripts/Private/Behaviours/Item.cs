using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	/// <summary>
	/// This behaviour should be attached to any object in the scene that can be recovered or damaged by the player
	/// </summary>
	public class Item : MonoBehaviour {

		/// <summary>
		/// The item type represented by this object.
		/// </summary>
		[NonSerialized]
		public ItemDefinition type;

		/// <summary>
		/// Resitance points left for this item. Used for items that can be damaged on the scene (not voxels).
		/// </summary>
		[NonSerialized]
		public byte resitancePointsLeft;

		/// <summary>
		/// If this object represents an item that can be picked up by a player
		/// </summary>
		[NonSerialized]
		public bool canPickUp;

		[NonSerialized]
		public float creationTime;

		[NonSerialized]
		public float quantity = 1f;

		const float PICK_UP_START_DISTANCE_SQR = 6.5f;
		const float PICK_UP_END_DISTANCE_SQR = 0.81f;
		const float ROTATION_SPEED = 40f;

		[NonSerialized, HideInInspector]
		public Rigidbody rb;

		[NonSerialized]
		public bool pickingUp;

		void Update() {
			if (!canPickUp || type == null || rb == null)
				return;

			rb.rotation = Quaternion.Euler(Misc.vector3up * ((Time.time * ROTATION_SPEED) % 360));

			if (!pickingUp) {
				if (Time.frameCount % 10 != 0)
					return;
			}

			// Check if player is near
			Vector3 playerPosition = VoxelPlayEnvironment.instance.playerGameObject.transform.position;
			Vector3 pos = transform.position;

			float dx = playerPosition.x - pos.x;
			float dy = playerPosition.y - pos.y;
			float dz = playerPosition.z - pos.z;

			if (pickingUp) {
				pos.x += dx * 0.25f;
				pos.y += dy * 0.25f;
				pos.z += dz * 0.25f;
				rb.transform.position = pos;
			}

			if (Time.time - creationTime > 1f) { 
				float dist = dx * dx + dy * dy + dz * dz;
				if (dist < PICK_UP_END_DISTANCE_SQR) {
					VoxelPlayPlayer.instance.AddInventoryItem (type, quantity);
					VoxelPlayUI.instance.RefreshInventoryContents ();
					PlayPickupSound (type.pickupSound);
					gameObject.SetActive (false);
				} else if (dist < PICK_UP_START_DISTANCE_SQR) {
					pickingUp = true;
				}
			}
		}

		void PlayPickupSound(AudioClip sound) {
			if (sound != null) {
				VoxelPlayPlayer.instance.audioSource.PlayOneShot(sound);
			} else if (VoxelPlayEnvironment.instance.defaultPickupSound != null) {
				VoxelPlayPlayer.instance.audioSource.PlayOneShot(VoxelPlayEnvironment.instance.defaultPickupSound);
			}
		}

	}

}