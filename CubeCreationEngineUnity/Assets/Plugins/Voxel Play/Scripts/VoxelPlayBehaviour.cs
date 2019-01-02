// Voxel Play 
// Created by Ramiro Oliva (Kronnect)

// Voxel Play Behaviour - attach this script to any moving object that should receive voxel global illumination

using System;
using UnityEngine;
using System.Collections;

namespace VoxelPlay {
				
	public class VoxelPlayBehaviour : MonoBehaviour {

		[Tooltip("Enable this property to adjust material lighting based on voxel global illumination")]
		public bool enableVoxelLight = true;

		[Tooltip("Moves this gameobject to the surface of the terrain if it falls below or crosses a solid voxel")]
		public bool forceUnstuck = true;

		VoxelPlayEnvironment env;
		int lastX, lastY, lastZ;
		Vector3 lastPosition;
		Material mat;
		bool useMaterialColor;
		Color normalMatColor;

		void Start() {
			env = VoxelPlayEnvironment.instance;
			if (env == null) {
				DestroyImmediate(this);
				return;
			}
			CharacterController cc = GetComponent<CharacterController>();
			if (cc != null) {
				cc.skinWidth = 0.08f;
				cc.radius = 0.4f;
			}
			lastPosition = transform.position;
			lastX = int.MaxValue;

			MeshRenderer mr = GetComponent<MeshRenderer>();
			if (mr != null) {
				mat = mr.sharedMaterial;
				useMaterialColor = !mat.name.Contains("VP Model");
				if (useMaterialColor) {
					mat = Instantiate(mat) as Material;
					mat.hideFlags = HideFlags.DontSave;
					mr.sharedMaterial = mat;
					normalMatColor = mat.color;
				}
				UpdateLighting();
			}
		}

		void LateUpdate() {
			// Check if position has changed since previous
			Vector3 position = transform.position;

			int x = FastMath.FloorToInt(position.x);
			int y = FastMath.FloorToInt(position.y);
			int z = FastMath.FloorToInt(position.z);

			if (lastX == x && lastY == y && lastZ == z)
				return;

			lastPosition = position;
			lastX = x;
			lastY = y;
			lastZ = z;
	
			if (enableVoxelLight) {
				UpdateLighting();
			}

			if (forceUnstuck) {
				Vector3 pos = transform.position;
				pos.y += 0.1f;
				if (env.CheckCollision (pos)) {
					float deltaY = FastMath.FloorToInt (pos.y) + 1f - pos.y;
					pos.y += deltaY + 0.01f;
					transform.position = pos;
					lastX--;
				}
			}

		}


		public void UpdateLighting() {
			if (mat != null) {
				Vector3 pos = lastPosition;
				// center of voxel
				pos.x += 0.5f;
				pos.y += 0.5f;
				pos.z += 0.5f;
				float light = env.GetVoxelLight(pos);
				if (useMaterialColor) {
					Color newColor = new Color(normalMatColor.r * light, normalMatColor.g * light, normalMatColor.b * light, normalMatColor.a);
					mat.color = newColor;
				}
				else {
					mat.SetFloat("_VoxelLight", env.GetVoxelLight(pos));
				}
			}
		}

	}
}