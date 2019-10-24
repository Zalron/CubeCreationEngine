using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Text;
using System.Globalization;

namespace VoxelPlay
{

	public partial class VoxelPlayEnvironment : MonoBehaviour
	{

		void LoadSaveBinaryFileFormat_4 (BinaryReader br, bool preservePlayerPosition = false)
		{
			// Character controller transform position
			Vector3 v = DecodeVector3Binary (br);
			if (!preservePlayerPosition) {
				characterController.transform.position = v;
			}
			// Character controller transform rotation
			Vector3 angles = DecodeVector3Binary (br);
			if (!preservePlayerPosition) {
				characterController.transform.rotation = Quaternion.Euler (angles);
			}
			// Character controller's camera local rotation
			angles = DecodeVector3Binary (br);
			if (!preservePlayerPosition) {
				cameraMain.transform.localRotation = Quaternion.Euler (angles);
				// Pass initial rotation to mouseLook script
				characterController.GetComponent<VoxelPlayFirstPersonController> ().mouseLook.Init (characterController.transform, cameraMain.transform, null);
			}

			// Read voxel definition table
			InitSaveGameStructs ();
			int vdCount = br.ReadInt16 (); 
			for (int k = 0; k < vdCount; k++) {
				saveVoxelDefinitionsList.Add (br.ReadString ());
			}

			int numChunks = br.ReadInt32 ();
			VoxelDefinition voxelDefinition = defaultVoxel;
			int prevVdIndex = -1;
			Color32 voxelColor = Misc.color32White;
			for (int c = 0; c < numChunks; c++) {
				// Read chunks
				// Get chunk position
				Vector3 chunkPosition = DecodeVector3Binary (br);
				VoxelChunk chunk = GetChunkUnpopulated (chunkPosition);
				byte isAboveSurface = br.ReadByte ();
				chunk.isAboveSurface = isAboveSurface == 1;
				chunk.back = chunk.bottom = chunk.left = chunk.right = chunk.forward = chunk.top = null;
				chunk.allowTrees = false;
				chunk.modified = true;
				chunk.isPopulated = true;
				chunk.voxelSignature = chunk.lightmapSignature = -1;
				chunk.renderState = ChunkRenderState.Pending;
				SetChunkOctreeIsDirty (chunkPosition, false);
				ChunkClearFast (chunk);
				// Read voxels
				int numWords = br.ReadInt16 ();
				for (int k = 0; k < numWords; k++) {
					// Voxel definition
					int vdIndex = br.ReadInt16 ();
					if (prevVdIndex != vdIndex) {
						if (vdIndex >= 0 && vdIndex < vdCount) {
							voxelDefinition = GetVoxelDefinition (saveVoxelDefinitionsList [vdIndex]);
							prevVdIndex = vdIndex;
						}
					}
					// RGB
					voxelColor.r = br.ReadByte ();
					voxelColor.g = br.ReadByte ();
					voxelColor.b = br.ReadByte ();
					// Voxel index
					int voxelIndex = br.ReadInt16 ();
					// Repetitions
					int repetitions = br.ReadInt16 ();

					if (voxelDefinition == null) {
						continue;
					}

					// Water level (only for transparent)
					byte waterLevel = 15;
					if (voxelDefinition.renderType == RenderType.Water) {
						waterLevel = br.ReadByte ();
					} if (voxelDefinition.renderType == RenderType.Custom) {
						byte hasCustomRotation = br.ReadByte ();
						if (hasCustomRotation == 1) {
							Vector3 voxelAngles = DecodeVector3Binary(br);
                            delayedVoxelCustomRotations.Add(GetVoxelPosition(chunkPosition, voxelIndex), voxelAngles);
						}
					}
					for (int i = 0; i < repetitions; i++) {
						chunk.voxels [voxelIndex + i].Set (voxelDefinition, voxelColor);
						if (voxelDefinition.renderType == RenderType.Water)
							chunk.voxels [voxelIndex + i].SetWaterLevel(waterLevel);
					}
				}
				// Read light sources
				int lightCount = br.ReadInt16 ();
				VoxelHitInfo hitInfo = new VoxelHitInfo ();
				for (int k = 0; k < lightCount; k++) {
					// Voxel index
					hitInfo.voxelIndex = br.ReadInt16 ();
					// Voxel center
					hitInfo.voxelCenter = GetVoxelPosition (chunkPosition, hitInfo.voxelIndex);
					// Normal
					hitInfo.normal = DecodeVector3Binary (br);
					hitInfo.chunk = chunk;
					TorchAttach (hitInfo);
				}
			}
		}

	}



}
