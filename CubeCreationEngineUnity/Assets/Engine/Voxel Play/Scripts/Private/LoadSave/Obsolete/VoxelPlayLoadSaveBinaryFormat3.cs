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

		void LoadSaveBinaryFileFormat_3 (BinaryReader br, bool preservePlayerPosition = false)
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
				int chunkX, chunkY, chunkZ;
				// Get chunk position
				Vector3 chunkPosition = DecodeVector3Binary (br);
				GetChunkCoordinates (chunkPosition, out chunkX, out chunkY, out chunkZ);
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

// Preserved and commented for reference purposes
//
//		void SaveGameBinaryFormat_3 (BinaryWriter bw)
//		{
//
//			// Build a table with all voxel definitions used in modified chunks
//			InitSaveGameStructs ();
//			VoxelDefinition last = null;
//			int count = 0;
//			int numChunks = 0;
//			foreach (KeyValuePair<int, CachedChunk>kv in cachedChunks) {
//				VoxelChunk chunk = kv.Value.chunk;
//				if (chunk != null && chunk.modified) {
//					numChunks++;
//					for (int k = 0; k < chunk.voxels.Length; k++) {
//						VoxelDefinition vd = chunk.voxels [k].type;
//						if (vd == null || vd == last || vd.isDynamic)
//							continue;
//						last = vd;
//						if (!saveVoxelDefinitionDict.ContainsKey (vd)) {
//							saveVoxelDefinitionDict [vd] = count++;
//							saveVoxelDefinitionList.Add (vd.name);
//						}
//					}
//				}
//			}
//
//			// Header
//			bw.Write (SAVE_FILE_CURRENT_FORMAT);
//			// Character controller transform position
//			bw.Write (characterController.transform.position.x);
//			bw.Write (characterController.transform.position.y);
//			bw.Write (characterController.transform.position.z);
//			// Character controller transform rotation
//			bw.Write (characterController.transform.rotation.eulerAngles.x);
//			bw.Write (characterController.transform.rotation.eulerAngles.y);
//			bw.Write (characterController.transform.rotation.eulerAngles.z);
//			// Character controller's camera local rotation
//			bw.Write (cameraMain.transform.localRotation.eulerAngles.x);
//			bw.Write (cameraMain.transform.localRotation.eulerAngles.y);
//			bw.Write (cameraMain.transform.localRotation.eulerAngles.z);
//			// Add voxel definitions table
//			int vdCount = saveVoxelDefinitionList.Count;
//			bw.Write ((Int16)vdCount);
//			for (int k = 0; k < vdCount; k++) {
//				bw.Write (saveVoxelDefinitionList [k]);
//			}
//			// Add modified chunks
//			bw.Write (numChunks);
//			foreach (KeyValuePair<int, CachedChunk>kv in cachedChunks) {
//				VoxelChunk chunk = kv.Value.chunk;
//				if (chunk != null && chunk.modified) {
//					WriteChunkData1_3 (bw, chunk);
//				}
//			}
//		}
//
//		void WriteChunkData1_3 (BinaryWriter bw, VoxelChunk chunk)
//		{
//			// Chunk position
//			bw.Write (chunk.position.x);
//			bw.Write (chunk.position.y);
//			bw.Write (chunk.position.z);
//			bw.Write (chunk.isAboveSurface ? (byte)1 : (byte)0);
//
//			int voxelDefinitionIndex = 0;
//			VoxelDefinition prevVD = null;
//
//
//			// Count voxels words
//			int k = 0;
//			int numWords = 0;
//			while (k < chunk.voxels.Length) {
//				if (chunk.voxels [k].hasContent == 1) {
//					VoxelDefinition voxelDefinition = chunk.voxels [k].type;
//					if (voxelDefinition.isDynamic) {
//						k++;
//						continue;
//					}
//					if (voxelDefinition != prevVD) {
//						if (!saveVoxelDefinitionDict.TryGetValue (voxelDefinition, out voxelDefinitionIndex)) {
//							k++;
//							continue;
//						}
//						prevVD = voxelDefinition;
//					}
//					Color32 tintColor = chunk.voxels [k].color;
//					byte waterLevel = 15;
//					if (voxelDefinition.renderType == RenderType.Transparent) {
//						waterLevel = chunk.voxels [k].GetWaterLevel();
//					}
//					k++;
//					while (k < chunk.voxels.Length && chunk.voxels [k].type == voxelDefinition && chunk.voxels [k].color.r == tintColor.r && chunk.voxels [k].color.g == tintColor.g && chunk.voxels [k].color.b == tintColor.b && chunk.voxels [k].GetWaterLevel() == waterLevel) {
//						k++;
//					}
//					numWords++;
//				} else {
//					k++;
//				}
//			}
//			bw.Write ((Int16)numWords);
//
//			// Write voxels
//			k = 0;
//			while (k < chunk.voxels.Length) {
//				if (chunk.voxels [k].hasContent == 1) {
//					int voxelIndex = k;
//					VoxelDefinition voxelDefinition = chunk.voxels [k].type;
//					if (voxelDefinition.isDynamic) {
//						k++;
//						continue;
//					}
//					if (voxelDefinition != prevVD) {
//						if (!saveVoxelDefinitionDict.TryGetValue (voxelDefinition, out voxelDefinitionIndex)) {
//							k++;
//							continue;
//						}
//						prevVD = voxelDefinition;
//					}
//					Color32 tintColor = chunk.voxels [k].color;
//					byte waterLevel = 15;
//					if (voxelDefinition.renderType == RenderType.Transparent) {
//						waterLevel = chunk.voxels [k].GetWaterLevel();
//					}
//					int repetitions = 1;
//					k++;
//					while (k < chunk.voxels.Length && chunk.voxels [k].type == voxelDefinition && chunk.voxels [k].color.r == tintColor.r && chunk.voxels [k].color.g == tintColor.g && chunk.voxels [k].color.b == tintColor.b && chunk.voxels [k].GetWaterLevel() == waterLevel) {
//						repetitions++;
//						k++;
//					}
//					bw.Write ((Int16)voxelDefinitionIndex);
//					bw.Write (tintColor.r);
//					bw.Write (tintColor.g);
//					bw.Write (tintColor.b);
//					bw.Write ((Int16)voxelIndex);
//					bw.Write ((Int16)repetitions);
//					if (voxelDefinition.renderType == RenderType.Transparent) {
//						bw.Write (waterLevel);
//					}
//				} else {
//					k++;
//				}
//			}
//
//			// Write number of light sources
//			int lightCount = chunk.lightSources != null ? chunk.lightSources.Count : 0;
//			bw.Write ((Int16)lightCount);
//			if (lightCount > 0) {
//				for (k = 0; k < lightCount; k++) {
//					VoxelHitInfo hi = chunk.lightSources [k].hitInfo;
//					bw.Write ((Int16)hi.voxelIndex);
//					bw.Write (hi.normal.x);
//					bw.Write (hi.normal.y);
//					bw.Write (hi.normal.z);
//				}
//			}
//		}

	}



}
