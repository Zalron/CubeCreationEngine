using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Text;
using System.Globalization;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		void LoadSaveFileFormat_1_1(TextReader sr, bool preservePlayerPosition = false) {
			// Character controller transform position
			Vector3 v = DecodeVector3(sr.ReadLine());
			if (!preservePlayerPosition) {
				characterController.transform.position = v;
			}
			// Character controller transform rotation
			Vector3 angles = DecodeVector3(sr.ReadLine());
			if (!preservePlayerPosition) {
				characterController.transform.rotation = Quaternion.Euler(angles);
			}
			// Character controller's camera local rotation
			angles = DecodeVector3(sr.ReadLine());
			if (!preservePlayerPosition) {
				cameraMain.transform.localRotation = Quaternion.Euler(angles);
				// Pass initial rotation to mouseLook script
				characterController.GetComponent<VoxelPlayFirstPersonController>().mouseLook.Init(characterController.transform, cameraMain.transform, null);
			}

			// Read voxel definition table
			InitSaveGameStructs();
			int vdCount = int.Parse(sr.ReadLine());
			for (int k = 0; k < vdCount; k++) {
				saveVoxelDefinitionsList.Add(sr.ReadLine());
			}

			// Read chunks
			int chunkX, chunkY, chunkZ;
			while (true) {
				string line = sr.ReadLine();
				if (line == null)
					return;
				// Get chunk position
				Vector3 chunkPosition = DecodeVector3(line);
				GetChunkCoordinates(chunkPosition, out chunkX, out chunkY, out chunkZ);
				VoxelChunk chunk = GetChunkOrCreate(chunkX, chunkY, chunkZ);
				chunk.modified = true;
				chunk.isPopulated = true;
				ChunkClearFast(chunk);
				line = sr.ReadLine();
				if (line == null)
					return;
				// Read voxels
				int numWords = int.Parse(line, CultureInfo.InvariantCulture);
				for (int k = 0; k < numWords; k++) {
					line = sr.ReadLine();
					if (line == null)
						return;
					string[] wordData = line.Split(LOAD_DATA_SEPARATOR);
					if (wordData.Length != 6)
						continue;
					// Voxel definition
					int vdIndex = int.Parse(wordData[0]);
					VoxelDefinition voxelDefinition = GetVoxelDefinition(saveVoxelDefinitionsList[vdIndex]);
					if (voxelDefinition == null) {
						voxelDefinition = defaultVoxel; // should not happen
					}
					// RGB
					byte r = byte.Parse(wordData[1]);
					byte g = byte.Parse(wordData[2]);
					byte b = byte.Parse(wordData[3]);
					// Voxel index
					int voxelIndex = int.Parse(wordData[4]);
					// Repetitions
					int repetitions = int.Parse(wordData[5]);
					for (int i = 0; i < repetitions; i++) {
						chunk.voxels[voxelIndex + i].Set(voxelDefinition, new Color32(r, g, b, 255));
					}
				}
				// Read light sources
				line = sr.ReadLine();
				if (line == null)
					return;
				int lightCount = int.Parse(line);
				VoxelHitInfo hitInfo = new VoxelHitInfo();
				for (int k = 0; k < lightCount; k++) {
					// Voxel index
					line = sr.ReadLine();
					if (line == null)
						return;
					hitInfo.voxelIndex = int.Parse(line);
					// Voxel center
					line = sr.ReadLine();
					if (line == null)
						return;
					hitInfo.voxelCenter = DecodeVector3(line);
					// Normal
					line = sr.ReadLine();
					if (line == null)
						return;
					hitInfo.normal = DecodeVector3(line);
					hitInfo.chunk = chunk;
					TorchAttach(hitInfo);
				}
			}
		}
	}



}
