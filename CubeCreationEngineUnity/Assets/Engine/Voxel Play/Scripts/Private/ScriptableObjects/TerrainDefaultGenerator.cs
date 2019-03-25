using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public enum TerrainStepType {
		SampleHeightMapTexture = 0,
		SampleRidgeNoiseFromTexture = 1,
		Constant = 100,
		Copy = 101,
		Random = 102,
		Invert = 103,
		Shift = 104,
		BeachMask = 105,
		AddAndMultiply = 200,
		MultiplyAndAdd = 201,
		Exponential = 202,
		Threshold = 203,
		FlattenOrRaise = 204,
		BlendAdditive = 300,
		BlendMultiply = 301,
		Clamp = 302,
		Select = 303,
		Fill = 304,
		Test = 305
	}

	[Serializable]
	public struct StepData {
		public bool enabled;
		public TerrainStepType operation;
		public Texture2D noiseTexture;
		[Range (0.001f, 2f)]
		public float frecuency;
		[Range (0, 1f)]
		public float noiseRangeMin;
		[Range (0, 1f)]
		public float noiseRangeMax;

		public int inputIndex0;
		public int inputIndex1;

		public float threshold, thresholdShift, thresholdParam;

		public float param, param2, param3;
		public float weight0, weight1;

		public float min, max;

		[HideInInspector, NonSerialized]
		public float[] noiseValues;
		[HideInInspector, NonSerialized]
		public int noiseTextureSize;
		[HideInInspector, NonSerialized]
		public float value;
		[HideInInspector, NonSerialized]
		public Texture2D lastTextureLoaded;
	}

	public partial class BiomeDefinition {
		[NonSerialized]
		public int biomeGeneration;
	}

	[CreateAssetMenu (menuName = "Voxel Play/Terrain Generators/Multi-Step Terrain Generator", fileName = "MultiStepTerrainGenerator", order = 101)]
	[HelpURL ("https://kronnect.freshdesk.com/support/solutions/articles/42000001906-terrain-generators")]
	public class TerrainDefaultGenerator : VoxelPlayTerrainGenerator {
		public StepData[] steps;

		[Range (0, 1f)]
		public float seaDepthMultiplier = 0.4f;
		[Range (0, 0.02f)]
		public float beachWidth = 0.001f;
		public VoxelDefinition waterVoxel;
		public VoxelDefinition shoreVoxel;

		[Header ("Underground")]
		public bool addOre;

		[Header ("Moisture Parameters")]
		public Texture2D moisture;
		[Range (0, 1f)]
		public float moistureScale = 0.2f;

		// Internal fields
		float[] moistureValues;
		int noiseMoistureTextureSize;
		float seaLevelAlignedWithInt, beachLevelAlignedWithInt;
		bool paintShore;
		HeightMapInfo[] heightChunkData;
		Texture2D lastMoistureTextureLoaded;
		int generation;

		protected override void Init () {
			seaLevelAlignedWithInt = ((int)(seaLevel * maxHeight) / maxHeight);
			beachLevelAlignedWithInt = ((int)(seaLevel * maxHeight) + 1f) / maxHeight;
			if (steps != null) {
				for (int k = 0; k < steps.Length; k++) {
					if (steps [k].noiseTexture != null) {
						bool repeated = false;
						for (int j = 0; j < k - 1; j++) {
							if (steps [k].noiseTexture == steps [k].noiseTexture) {
								steps [k].noiseValues = steps [j].noiseValues;
								steps [k].noiseTextureSize = steps [j].noiseTextureSize;
								repeated = true;
								break;
							}
						}
						if (!repeated && (steps [k].noiseTextureSize == 0 || steps [k].noiseValues == null || steps [k].lastTextureLoaded == null || steps [k].noiseTexture != steps [k].lastTextureLoaded)) {
							steps [k].lastTextureLoaded = steps [k].noiseTexture;
							steps [k].noiseValues = NoiseTools.LoadNoiseTexture (steps [k].noiseTexture, out steps [k].noiseTextureSize);
						}
					}
					// Validate references
					if (steps [k].inputIndex0 < 0 || steps [k].inputIndex0 >= steps.Length) {
						steps [k].inputIndex0 = 0;
					}
					if (steps [k].inputIndex1 < 0 || steps [k].inputIndex1 >= steps.Length) {
						steps [k].inputIndex1 = 0;
					}
				}
			}
			if (moisture != null && (noiseMoistureTextureSize == 0 || moistureValues == null || lastMoistureTextureLoaded == null || lastMoistureTextureLoaded != moisture)) {
				lastMoistureTextureLoaded = moisture;
				moistureValues = NoiseTools.LoadNoiseTexture (moisture, out noiseMoistureTextureSize);
			} 
			if (waterVoxel == null) {
				waterVoxel = Resources.Load<VoxelDefinition> ("VoxelPlay/Defaults/Water/VoxelWaterSea");
			}
			paintShore = shoreVoxel != null;

			if (heightChunkData == null) {
				heightChunkData = new HeightMapInfo[16 * 16];
			}

		
			// Ensure voxels are available
			env.AddVoxelDefinition (shoreVoxel);
			env.AddVoxelDefinition (waterVoxel);
		}

		/// <summary>
		/// Gets the altitude and moisture (in 0-1 range).
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="z">The z coordinate.</param>
		/// <param name="altitude">Altitude.</param>
		/// <param name="moisture">Moisture.</param>
		public override void GetHeightAndMoisture (float x, float z, out float altitude, out float moisture) {

			if (!isInitialized) {
				Initialize ();
			}

			bool allowBeach = true;
			altitude = 0;
			if (steps != null && steps.Length > 0) {
				float value = 0;
				for (int k = 0; k < steps.Length; k++) {
					if (steps [k].enabled) {
						switch (steps [k].operation) {
						case TerrainStepType.SampleHeightMapTexture:
							value = NoiseTools.GetNoiseValueBilinear (steps [k].noiseValues, steps [k].noiseTextureSize, x * steps [k].frecuency, z * steps [k].frecuency);
							value = value * (steps [k].noiseRangeMax - steps [k].noiseRangeMin) + steps [k].noiseRangeMin;
							break;
						case TerrainStepType.SampleRidgeNoiseFromTexture:
							value = NoiseTools.GetNoiseValueBilinear (steps [k].noiseValues, steps [k].noiseTextureSize, x * steps [k].frecuency, z * steps [k].frecuency, true);
							value = value * (steps [k].noiseRangeMax - steps [k].noiseRangeMin) + steps [k].noiseRangeMin;
							break;
						case TerrainStepType.Shift:
							value += steps [k].param;
							break;
						case TerrainStepType.BeachMask:
							{
								int i1 = steps [k].inputIndex0;
								if (steps [i1].value > steps [k].threshold) {
									allowBeach = false;
								}
							}
							break;
						case TerrainStepType.AddAndMultiply:
							value = (value + steps [k].param) * steps [k].param2;
							break;
						case TerrainStepType.MultiplyAndAdd:
							value = (value * steps [k].param) + steps [k].param2;
							break;
						case TerrainStepType.Exponential:
							if (value < 0)
								value = 0;
							value = (float)System.Math.Pow (value, steps [k].param);
							break;
						case TerrainStepType.Constant:
							value = steps [k].param;
							break;
						case TerrainStepType.Invert:
							value = 1f - value;
							break;
						case TerrainStepType.Copy:
							{
								int i1 = steps [k].inputIndex0;
								value = steps [i1].value;
							}
							break;
						case TerrainStepType.Random:
							value = WorldRand.GetValue (x, z);
							break;
						case TerrainStepType.BlendAdditive:
							{
								int i1 = steps [k].inputIndex0;
								int i2 = steps [k].inputIndex1;
								value = steps [i1].value * steps [k].weight0 + steps [i2].value * steps [k].weight1;
							}
							break;
						case TerrainStepType.BlendMultiply:
							{
								int i1 = steps [k].inputIndex0;
								int i2 = steps [k].inputIndex1;
								value = steps [i1].value * steps [i2].value;
							}
							break;
						case TerrainStepType.Threshold:
							{
								int i1 = steps [k].inputIndex0;
								if (steps [i1].value >= steps [k].threshold) {
									value = steps [i1].value + steps [k].thresholdShift;
								} else {
									value = steps [k].thresholdParam;
								}
							}
							break;
						case TerrainStepType.FlattenOrRaise:
							if (value >= steps [k].threshold) {
								value = (value - steps [k].threshold) * steps [k].thresholdParam + steps [k].threshold;
							}
							break;
						case TerrainStepType.Clamp:
							if (value < steps [k].min)
								value = steps [k].min;
							else if (value > steps [k].max)
								value = steps [k].max;
							break;
						case TerrainStepType.Select:
							{
								int i1 = steps [k].inputIndex0;
								if (steps [i1].value < steps [k].min) {
									value = steps [k].thresholdParam;
								} else if (steps [i1].value > steps [k].max) {
									value = steps [k].thresholdParam;
								} else {
									value = steps [i1].value;
								}
							}
							break;
						case TerrainStepType.Fill:
							{
								int i1 = steps [k].inputIndex0;
								if (steps [i1].value >= steps [k].min && steps [i1].value <= steps [k].max) {
									value = steps [k].thresholdParam;
								}
							}
							break;
						case TerrainStepType.Test:
							{
								int i1 = steps [k].inputIndex0;
								if (steps [i1].value >= steps [k].min && steps [i1].value <= steps [k].max) {
									value = 1f;
								} else {
									value = 0f;
								}
							}
							break;
						}
					}
					steps [k].value = value;
				}
				altitude = value;
			} else {
				altitude = -9999; // no terrain so make altitude very low so every chunk be considered above terrain for GI purposes
			}

			// Moisture
			moisture = NoiseTools.GetNoiseValueBilinear (moistureValues, noiseMoistureTextureSize, x * moistureScale, z * moistureScale);


			// Remove any potential beach
			if (altitude < beachLevelAlignedWithInt && altitude >= seaLevelAlignedWithInt) {
				// smooth terrain under Sea
				float depth = beachLevelAlignedWithInt - altitude;
				if (depth > beachWidth || !allowBeach) {
					altitude = seaLevelAlignedWithInt - 0.0001f;
				}
			}

			// Adjusts sea depth
			if (altitude < seaLevelAlignedWithInt) {
				float depth = seaLevelAlignedWithInt - altitude;
				altitude = seaLevelAlignedWithInt - 0.0001f - depth * seaDepthMultiplier;
			}

		}

		/// <summary>
		/// Paints the terrain inside the chunk defined by its central "position"
		/// </summary>
		/// <returns><c>true</c>, if terrain was painted, <c>false</c> otherwise.</returns>
		/// <param name="position">Central position of the chunk.</param>
		public override bool PaintChunk (VoxelChunk chunk) {

			Vector3 position = chunk.position;
			if (position.y + 8 < minHeight) {
				chunk.isAboveSurface = false;
				return false;
			}
			bool placeBedrock = (object)world.bedrockVoxel != null && position.y < minHeight + 8;
			position.x -= 8;
			position.y -= 8;
			position.z -= 8;
			Vector3 pos;

			int waterLevel = env.waterLevel > 0 ? env.waterLevel : -1;
			Voxel[] voxels = chunk.voxels;

			bool hasContent = false;
			bool isAboveSurface = false;
			generation++;
			env.GetHeightMapInfoFast (position.x, position.z, heightChunkData);

			// iterate 256 slice of chunk (z/x plane = 16*16 positions)
			for (int arrayIndex = 0; arrayIndex < 256; arrayIndex++) {
				float groundLevel = heightChunkData [arrayIndex].groundLevel;
				float surfaceLevel = waterLevel > groundLevel ? waterLevel : groundLevel;
				if (surfaceLevel < position.y) {
					// position is above terrain or water
					isAboveSurface = true;
					continue;
				}
				BiomeDefinition biome = heightChunkData [arrayIndex].biome;
				if ((object)biome == null) {
					biome = world.defaultBiome;
					if ((object)biome == null)
						continue;
				}

				int y = (int)(surfaceLevel - position.y);
				if (y > 15)
					y = 15;
				pos.y = position.y + y;
				pos.x = position.x + (arrayIndex & 0xF);
				pos.z = position.z + (arrayIndex >> 4);

				// Place voxels
				int voxelIndex = y * ONE_Y_ROW + arrayIndex;
				if (pos.y > groundLevel) {
					// water above terrain
					if (pos.y == surfaceLevel) {
						isAboveSurface = true;
					}
					while (pos.y > groundLevel && voxelIndex >= 0) {
						voxels [voxelIndex].Set (waterVoxel);
						voxelIndex -= ONE_Y_ROW;
						pos.y--;
					}
				} else if (pos.y == groundLevel) {
					isAboveSurface = true;
					if (voxels [voxelIndex].hasContent == 0) {
						if (paintShore && pos.y == waterLevel) {
							// this is on the shore, place a shoreVoxel
							voxels [voxelIndex].Set (shoreVoxel);
						} else {
							// we're at the surface of the biome => draw the voxel top of the biome and also check for random vegetation and trees
							voxels [voxelIndex].Set (biome.voxelTop);
#if UNITY_EDITOR
							if (!env.draftModeActive) {
#endif
								// Check tree probability
								if (pos.y > waterLevel) {
									float rn = WorldRand.GetValue (pos);
									if (env.enableTrees && biome.treeDensity > 0 && rn < biome.treeDensity && biome.trees.Length > 0) {
										// request one tree at this position
										env.RequestTreeCreation (chunk, pos, env.GetTree (biome.trees, rn / biome.treeDensity)); 
									} else if (env.enableVegetation && biome.vegetationDensity > 0 && rn < biome.vegetationDensity && biome.vegetation.Length > 0) {
										if (voxelIndex >= 15 * ONE_Y_ROW) {
											// request one vegetation voxel one position above which means the chunk above this one
											env.RequestVegetationCreation (chunk.top, voxelIndex - ONE_Y_ROW * 15, env.GetVegetation (biome, rn / biome.vegetationDensity));
										} else {
											// directly place a vegetation voxel above this voxel
											voxels [voxelIndex + ONE_Y_ROW].Set (env.GetVegetation (biome, rn / biome.vegetationDensity));
											env.vegetationCreated++;
										}
									}
								}
#if UNITY_EDITOR
							}
#endif
						}
					}
					voxelIndex -= ONE_Y_ROW;
				}

				// Continue filling down
				biome.biomeGeneration = generation;
				while (voxelIndex >= 0) {
					if (voxels [voxelIndex].hasContent == 0) {
						voxels [voxelIndex].SetFast (biome.voxelDirt, 15);
					}
					voxelIndex -= ONE_Y_ROW;
				}
				if (placeBedrock) {
					voxels [voxelIndex + ONE_Y_ROW].SetFast (world.bedrockVoxel, 15);
				}
				hasContent = true;
			}

			// Spawn random ore
			if (addOre) {
				// Check if there's any ore in this chunk (randomly)
				float noiseValue = WorldRand.GetValue (chunk.position);
				for (int b = 0; b < world.biomes.Length; b++) {
					BiomeDefinition biome = world.biomes [b];
					if (biome.biomeGeneration != generation)
						continue;
					for (int o = 0; o < biome.ores.Length; o++) {
						if (biome.ores [o].ore == null)
							continue;
						if (biome.ores [o].probabilityMin <= noiseValue && biome.ores [o].probabilityMax >= noiseValue) {
							// ore picked; determine the number of veins in this chunk
							int veinsCount = biome.ores [o].veinsCountMin + (int)(WorldRand.GetValue () * (biome.ores [o].veinsCountMax - biome.ores [o].veinsCountMin + 1f));
							for (int vein = 0; vein < veinsCount; vein++) {
								Vector3 veinPos = chunk.position;
								veinPos.x += vein;
								// Determine random vein position in the chunk
								Vector3 v = WorldRand.GetVector3 (veinPos, 16);
								int px = (int)v.x;
								int py = (int)v.y;
								int pz = (int)v.z;
								veinPos = env.GetVoxelPosition (veinPos, px, py, pz);
								int oreIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
								int veinSize = biome.ores [o].veinMinSize + (oreIndex % (biome.ores [o].veinMaxSize - biome.ores [o].veinMinSize + 1));
								// span ore vein
								SpawnOre (chunk, biome.ores [o].ore, veinPos, px, py, pz, veinSize, biome.ores [o].depthMin, biome.ores [o].depthMax);
							}
							break;
						}
					}
				}
			}

			// Finish, return
			chunk.isAboveSurface = isAboveSurface;
			return hasContent;
		}


		void SpawnOre (VoxelChunk chunk, VoxelDefinition oreDefinition, Vector3 veinPos, int px, int py, int pz, int veinSize, int minDepth, int maxDepth) {
			int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
			while (veinSize-- > 0 && voxelIndex >= 0 && voxelIndex < chunk.voxels.Length) {
				// Get height at position
				int groundLevel = heightChunkData [pz * 16 + px].groundLevel;
				int depth = (int)(groundLevel - veinPos.y);
				if (depth < minDepth || depth > maxDepth)
					return;

				// Replace voxel with ore
				if (chunk.voxels [voxelIndex].hasContent == 1) {
					chunk.voxels [voxelIndex].SetFast (oreDefinition, 15);
				}
				// Check if spawn continues
				Vector3 prevPos = veinPos;
				float v = WorldRand.GetValue (veinPos);
				int dir = (int)(v * 5);
				switch (dir) {
				case 0: // down
					veinPos.y--;
					voxelIndex -= ONE_Y_ROW;
					break;
				case 1: // right
					veinPos.x++;
					voxelIndex++;
					break;
				case 2: // back
					veinPos.z--;
					voxelIndex -= ONE_Z_ROW;
					break;
				case 3: // left
					veinPos.x--;
					voxelIndex--;
					break;
				case 4: // forward
					veinPos.z++;
					voxelIndex += ONE_Z_ROW;
					break;
				}
				if (veinPos.x == prevPos.x && veinPos.y == prevPos.y && veinPos.z == prevPos.z) {
					veinPos.y--;
					voxelIndex -= ONE_Y_ROW;
				}
			}
		}




	}

}