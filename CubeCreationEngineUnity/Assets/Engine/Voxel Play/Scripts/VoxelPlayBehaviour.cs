// Voxel Play 
// Created by Ramiro Oliva (Kronnect)

// Voxel Play Behaviour - attach this script to any moving object that should receive voxel global illumination

using System;
using UnityEngine;
using System.Collections;

namespace VoxelPlay {
				
	public class VoxelPlayBehaviour : MonoBehaviour {

		public bool enableVoxelLight = true;
		public bool forceUnstuck = true;
		public bool checkNearChunks = true;
		public Vector3 chunkExtents;
		public bool renderChunks;

		VoxelPlayEnvironment env;
		int lastX, lastY, lastZ;
		int lastChunkX, lastChunkY, lastChunkZ;
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
			lastPosition = transform.position;
			lastX = int.MaxValue;

			if (enableVoxelLight) {
				MeshRenderer mr = GetComponent<MeshRenderer> ();
				if (mr != null) {
					mat = mr.sharedMaterial;
					useMaterialColor = !mat.name.Contains ("VP Model");
					if (useMaterialColor) {
						mat = Instantiate (mat) as Material;
						mat.hideFlags = HideFlags.DontSave;
						mr.sharedMaterial = mat;
						normalMatColor = mat.color;
					}
				}
				UpdateLighting ();
			}

			CheckNearChunks (transform.position);
		}

		public void Refresh() {
			lastX = int.MaxValue;
			lastChunkX = int.MaxValue;
		}

		void LateUpdate() {
			// Check if position has changed since previous
			Vector3 position = transform.position;

			int x = FastMath.FloorToInt (position.x);
			int y = FastMath.FloorToInt (position.y);
			int z = FastMath.FloorToInt (position.z);

			if (lastX == x && lastY == y && lastZ == z)
				return;

			lastPosition = position;
			lastX = x;
			lastY = y;
			lastZ = z;
	
			UpdateLighting ();
	
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

			CheckNearChunks (position);
		}

		void CheckNearChunks(Vector3 position) {
			if (!checkNearChunks)
				return;
			int chunkX = FastMath.FloorToInt (position.x / 16);
			int chunkY = FastMath.FloorToInt (position.y / 16);
			int chunkZ = FastMath.FloorToInt (position.z / 16);
			if (lastChunkX != chunkX || lastChunkY != chunkY || lastChunkZ != chunkZ) {
				lastChunkX = chunkX;
				lastChunkY = chunkY;
				lastChunkZ = chunkZ;
				env.ChunkCheckArea (position, chunkExtents, renderChunks);
			}
		}


		public void UpdateLighting() {
			if (enableVoxelLight && mat != null) {
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