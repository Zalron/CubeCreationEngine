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

		void LoadSaveFileFormat_1_2(TextReader sr, bool preservePlayerPosition = false) {
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
					if (wordData.Length < 6)
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
					// Water level (only for transparent)
					byte waterLevel = 15;
					if (wordData.Length >= 7 && voxelDefinition.renderType == RenderType.Water)  {
						waterLevel = byte.Parse(wordData[6]);
					}
					for (int i = 0; i < repetitions; i++) {
						chunk.voxels[voxelIndex + i].Set(voxelDefinition, new Color32(r, g, b, 255));
						if (voxelDefinition.renderType == RenderType.Water) chunk.voxels [voxelIndex + i].SetWaterLevel(waterLevel);
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


		void SaveGameFormat_1_2(TextWriter sw) {

			// Build a table with all voxel definitions used in modified chunks
			InitSaveGameStructs();
			VoxelDefinition last = null;
			int count = 0;
			foreach (KeyValuePair<int, CachedChunk>kv in cachedChunks) {
				VoxelChunk chunk = kv.Value.chunk;
				if (chunk != null && chunk.modified) {
					for (int k = 0; k < chunk.voxels.Length; k++) {
						VoxelDefinition vd = chunk.voxels[k].type;
						if (vd == null || vd == last)
							continue;
						last = vd;
						if (!saveVoxelDefinitionsDict.ContainsKey(vd)) {
							saveVoxelDefinitionsDict[vd] = count++;
							saveVoxelDefinitionsList.Add(vd.name);
						}
					}
				}
			}

			// Header
			sw.WriteLine(SAVE_FILE_CURRENT_FORMAT);
			// Character controller transform position
			sw.WriteLine(characterController.transform.position.x + "," + characterController.transform.position.y + "," + characterController.transform.position.z);
			// Character controller transform rotation
			sw.WriteLine(characterController.transform.rotation.eulerAngles.x + "," + characterController.transform.rotation.eulerAngles.y + "," + characterController.transform.rotation.eulerAngles.z);
			// Character controller's camera local rotation
			sw.WriteLine(cameraMain.transform.localRotation.eulerAngles.x + "," + cameraMain.transform.localRotation.eulerAngles.y + "," + cameraMain.transform.localRotation.eulerAngles.z);
			// Add voxel definitions table
			int vdCount = saveVoxelDefinitionsList.Count;
			sw.WriteLine(vdCount);
			for (int k = 0; k < vdCount; k++) {
				sw.WriteLine(saveVoxelDefinitionsList[k]);
			}
			// Add modified chunks
			foreach (KeyValuePair<int, CachedChunk>kv in cachedChunks) {
				VoxelChunk chunk = kv.Value.chunk;
				if (chunk != null && chunk.modified) {
					WriteChunkData1_2(sw, chunk);
				}
			}
		}

		void WriteChunkData1_2(TextWriter sw, VoxelChunk chunk) {
			// Chunk position
			sw.WriteLine(chunk.position.x + "," + chunk.position.y + "," + chunk.position.z);
			if (sb == null)
				sb = new StringBuilder();
			else
				sb.Length = 0;
			int k = 0;
			int numWords = 0;
			// Compute voxels
			while (k < chunk.voxels.Length) {
				if (chunk.voxels[k].hasContent == 1) {
					int voxelIndex = k;
					VoxelDefinition voxelDefinition = chunk.voxels[k].type;
					int voxelDefinitionIndex;
					if (!saveVoxelDefinitionsDict.TryGetValue(voxelDefinition, out voxelDefinitionIndex))
						continue;
					Color32 tintColor = chunk.voxels[k].color;
					int waterLevel = 15;
					if (voxelDefinition.renderType == RenderType.Water) {
						waterLevel = chunk.voxels [k].GetWaterLevel();
					}
					int repetitions = 1;
					k++;
					while (k < chunk.voxels.Length && chunk.voxels[k].type == voxelDefinition && chunk.voxels[k].color.r == tintColor.r && chunk.voxels[k].color.g == tintColor.g && chunk.voxels[k].color.b == tintColor.b && chunk.voxels[k].GetWaterLevel() == waterLevel) {
						repetitions++;
						k++;
					}
					if (numWords > 0)
						sb.AppendLine();
					sb.Append(voxelDefinitionIndex);
					sb.Append(",");
					sb.Append(tintColor.r);
					sb.Append(",");
					sb.Append(tintColor.g);
					sb.Append(",");
					sb.Append(tintColor.b);
					sb.Append(",");
					sb.Append(voxelIndex);
					sb.Append(",");
					sb.Append(repetitions);
					if (voxelDefinition.renderType == RenderType.Water) {
						sb.Append (",");
						sb.Append (waterLevel);
					}
					numWords++;
				} else {
					k++;
				}
			}

			// Write number of voxels
			sw.WriteLine(numWords);
			if (numWords > 0) {
				// Write voxels data
				sw.WriteLine(sb.ToString());
			}

			// Write number of light sources
			int lightCount = chunk.lightSources != null ? chunk.lightSources.Count : 0;
			sb.Length = 0;
			sb.Append(lightCount.ToString());
			if (lightCount > 0) {
				for (k = 0; k < lightCount; k++) {
					VoxelHitInfo hi = chunk.lightSources[k].hitInfo;
					sb.AppendLine();
					sb.AppendLine(hi.voxelIndex.ToString());
					sb.Append(hi.voxelCenter.x);
					sb.Append(",");
					sb.Append(hi.voxelCenter.y);
					sb.Append(",");
					sb.AppendLine(hi.voxelCenter.z.ToString());
					sb.Append(hi.normal.x);
					sb.Append(",");
					sb.Append(hi.normal.y);
					sb.Append(",");
					sb.Append(hi.normal.z);
				}
			}
			sw.WriteLine(sb.ToString());
		}

	}



}
