using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	[CreateAssetMenu (menuName = "Voxel Play/Terrain Generators/Unity Terrain Generator", fileName = "UnityTerrainGenerator", order = 102)]
	public class UnityTerrainGenerator : VoxelPlayTerrainGenerator {
		public enum TERRAIN_RESOURCE_ACTION {
			Create,
			Assigned,
			Ignore
		}

		[Serializable]
		public struct TerrainVoxelDefinitionMapping {
			public Texture2D preview;
			public int dirtWith;
			public float blendPower;
			public VoxelDefinition top, dirt;
			public TERRAIN_RESOURCE_ACTION action;
			public float smoothPower;
		}

		[Serializable]
		public struct VegetationVoxelDefinitionMapping {
			public Texture2D preview;
			public VoxelDefinition vd;
			public TERRAIN_RESOURCE_ACTION action;
		}

		[Serializable]
		public struct TerrainModelDefinitionMapping {
			public Texture2D preview;
			public ModelDefinition md;
			public TERRAIN_RESOURCE_ACTION action;
			public float smoothPower;
		}

		public TerrainVoxelDefinitionMapping[] splatSettings;
		public VegetationVoxelDefinitionMapping[] detailSettings;
		public TerrainModelDefinitionMapping[] treeSettings;
		public TerrainData terrainData;
		[Range (0, 1)]
		public float vegetationDensity = 1f;
		public VoxelDefinition waterVoxel;

		struct DetailLayerInfo {
			public int[,] detailLayer;
		}

		DetailLayerInfo[] detailLayers;

		struct TerrainHeightInfo {
			public float altitude;
			public VoxelDefinition terrainVoxelTop, terrainVoxelDirt, vegetationVoxel;
			public ModelDefinition treeModel;
		}

		TerrainHeightInfo[] heights;


		public override void Init () {

			if (splatSettings == null || splatSettings.Length == 0) {
				splatSettings = new TerrainVoxelDefinitionMapping[64];
			}
			if (detailSettings == null || detailSettings.Length == 0) {
				detailSettings = new VegetationVoxelDefinitionMapping[64];
			}
			if (treeSettings == null || treeSettings.Length == 0) {
				treeSettings = new TerrainModelDefinitionMapping[32];
			}
			if (detailLayers == null || detailLayers.Length < 32) {
				detailLayers = new DetailLayerInfo[32];
			}
			if (waterVoxel == null) {
				waterVoxel = Resources.Load<VoxelDefinition> ("VoxelPlay/Defaults/Water/VoxelWaterSea");
			}

			#if UNITY_EDITOR
			if (world != null && world.terrainGenerator == null) {
				world.terrainGenerator = this;
			}
			if (terrainData == null) {
				Terrain activeTerrain = Terrain.activeTerrain;
				if (activeTerrain != null) {
					terrainData = activeTerrain.terrainData;
					ExamineTerrainData ();
				}
			}

			#endif

			if (terrainData == null)
				return;
			maxHeight = terrainData.size.y;

			int th = terrainData.heightmapHeight;
			int tw = terrainData.heightmapWidth;
			int len = tw * th;
			if (heights == null || heights.Length != len) {
				heights = new TerrainHeightInfo[len];
			}

			float[,,] heightInfo = terrainData.GetAlphamaps (0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
			int detailLayerCount = terrainData.detailPrototypes.Length;
			for (int d = 0; d < detailLayerCount; d++) {
				detailLayers [d].detailLayer = terrainData.GetDetailLayer (0, 0, terrainData.detailWidth, terrainData.detailHeight, d);
			}
			int i = 0;
			int alphaMapsLayerCount = heightInfo.GetUpperBound (2);
			int currentDetailLayer = 0;
			int vegDensity = (int)(16 * (1f - vegetationDensity));
			for (int y = 0; y < th; y++) {
				int alphamapY = y * terrainData.alphamapHeight / th;
				int detailY = y * terrainData.detailHeight / th;
				for (int x = 0; x < tw; x++,i++) {
					int alphamapX = x * terrainData.alphamapWidth / tw;
					heights [i].altitude = terrainData.GetHeight (x, y) / maxHeight;
					float maxBlend = -1;
					for (int a = 0; a <= alphaMapsLayerCount; a++) {
						float alphamapValue = heightInfo [alphamapY, alphamapX, a];
						if (alphamapValue > maxBlend) {
							maxBlend = alphamapValue;
							heights [i].terrainVoxelTop = splatSettings [a].top;
							heights [i].terrainVoxelDirt = splatSettings [a].dirt;
							if (maxBlend >= 1f)
								break;
						}
					}

					if (detailLayerCount > 0) {
						for (int v = 0; v < detailLayerCount; v++) {
							currentDetailLayer++;
							if (currentDetailLayer >= detailLayerCount) {
								currentDetailLayer = 0;
							}
							if (detailSettings [currentDetailLayer].vd != null) {
								int detailX = x * terrainData.detailWidth / tw;
								int o = detailLayers [currentDetailLayer].detailLayer [detailY, detailX];
								if (o > vegDensity) {
									heights [i].vegetationVoxel = detailSettings [currentDetailLayer].vd;
									break;
								}
							}
						}
					}
				}
			}

			float sx = terrainData.size.x;
			float sz = terrainData.size.z;
			for (int t = 0; t < terrainData.treeInstances.Length; t++) {
				TreeInstance ti = terrainData.treeInstances [t];
				int hindex = GetHeightIndex (ti.position.x * sx - sx / 2, ti.position.z * sz - sz / 2);
				heights [hindex].treeModel = treeSettings [ti.prototypeIndex].md;
			}
		}

		public void ExamineTerrainData () {
			#if UNITY_EDITOR
			if (terrainData == null)
				return;
#if UNITY_2018_3_OR_NEWER
            for (int k = 0; k < terrainData.terrainLayers.Length; k++) {
                splatSettings[k].preview = TextureTools.GetSolidTexture(terrainData.terrainLayers[k].diffuseTexture);
#else
            for (int k = 0; k < terrainData.splatPrototypes.Length; k++) {
				splatSettings [k].preview = TextureTools.GetSolidTexture (terrainData.splatPrototypes [k].texture);
#endif
                if (splatSettings [k].dirtWith == 0) {
					splatSettings [k].dirtWith = (k + 1);
					splatSettings [k].blendPower = 0.5f;
				}
				if (splatSettings [k].preview == null) {
					splatSettings [k].action = TERRAIN_RESOURCE_ACTION.Ignore;
				} else if ((splatSettings [k].top == null || splatSettings [k].dirt == null) && splatSettings [k].action == TERRAIN_RESOURCE_ACTION.Assigned) {
					splatSettings [k].action = TERRAIN_RESOURCE_ACTION.Create;
				}
			}
			for (int k = 0; k < terrainData.treePrototypes.Length; k++) {
				treeSettings [k].preview = UnityEditor.AssetPreview.GetAssetPreview (terrainData.treePrototypes [k].prefab);
				if (treeSettings [k].preview == null) {
					treeSettings [k].action = TERRAIN_RESOURCE_ACTION.Ignore;
				} else if (treeSettings [k].md == null && treeSettings [k].action == TERRAIN_RESOURCE_ACTION.Assigned) {
					treeSettings [k].action = TERRAIN_RESOURCE_ACTION.Create;
				}
			}
			for (int k = 0; k < terrainData.detailPrototypes.Length; k++) {
				if (terrainData.detailPrototypes [k].prototype != null) {
					detailSettings [k].preview = UnityEditor.AssetPreview.GetAssetPreview (terrainData.detailPrototypes [k].prototype);
				} else {
					detailSettings [k].preview = terrainData.detailPrototypes [k].prototypeTexture;
				}
				if (detailSettings [k].preview == null) {
					detailSettings [k].action = TERRAIN_RESOURCE_ACTION.Ignore;
				} else if (detailSettings [k].vd == null && detailSettings [k].action == TERRAIN_RESOURCE_ACTION.Assigned) {
					detailSettings [k].action = TERRAIN_RESOURCE_ACTION.Create;
				}
			}
			UnityEditor.EditorUtility.SetDirty (this);
#endif
        }

		int GetHeightIndex (float x, float z) {
			int w = terrainData.heightmapWidth;
			int h = terrainData.heightmapHeight;

			float sx = terrainData.size.x;
			float sz = terrainData.size.z;

			float fx = w / sx;
			float fz = h / sz;

			int tx = (int)((x + sx / 2) * fx);
			if (tx < 0)
				tx = 0;
			else if (tx >= w)
				tx = w - 1;
			int ty = (int)((z + sz / 2) * fz);
			if (ty < 0)
				ty = 0;
			else if (ty >= h)
				ty = h - 1;
			return ty * w + tx;
		}

		/// <summary>
		/// Gets the altitude and moisture (in 0-1 range).
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="z">The z coordinate.</param>
		/// <param name="altitude">Altitude.</param>
		/// <param name="moisture">Moisture.</param>
		public override void GetHeightAndMoisture (float x, float z, out float altitude, out float moisture) {

			if (!env.applicationIsPlaying) {
				altitude = 0;
				moisture = 0;
				return;
			}

			int heightIndex = GetHeightIndex (x, z);
			altitude = heights [heightIndex].altitude;
			moisture = 0;
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

			for (int z = 0; z < 16; z++) {
				pos.z = position.z + z;
				int arrayZIndex = z * ONE_Z_ROW;
				for (int x = 0; x < 16; x++) {
					pos.x = position.x + x;
					HeightMapInfo heightMapInfo = env.GetHeightMapInfoFast (pos.x, pos.z);

					float groundLevel = heightMapInfo.groundLevel;
					float surfaceLevel = waterLevel > groundLevel ? waterLevel : groundLevel;
					if (surfaceLevel < position.y) {
						// position is above terrain or water
						isAboveSurface = true;
						continue;
					}

					int hindex = GetHeightIndex (pos.x, pos.z);
					VoxelDefinition vd = heights [hindex].terrainVoxelTop;
					if ((object)vd == null)
						continue;
					
					int y = (int)(surfaceLevel - position.y);
					if (y > 15)
						y = 15;
					pos.y = position.y + y;

					// Place voxels
					int voxelIndex = y * ONE_Y_ROW + arrayZIndex + x;
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
							// surface => draw voxel top, vegetation and trees
										voxels [voxelIndex].Set (vd);
#if UNITY_EDITOR
							if (!env.draftModeActive) {
#endif
								// Check tree probability
								if (pos.y > waterLevel) {
									ModelDefinition treeModel = heights [hindex].treeModel;
									if (env.enableTrees && treeModel != null) {
										env.RequestTreeCreation (chunk, pos, treeModel);
									} else if (env.enableVegetation) {
										VoxelDefinition vegetation = heights [hindex].vegetationVoxel;
										if (vegetation != null) {
											if (voxelIndex >= 15 * ONE_Y_ROW) {
												env.RequestVegetationCreation (chunk.top, voxelIndex - ONE_Y_ROW * 15, vegetation);
											} else {
												voxels [voxelIndex + ONE_Y_ROW].Set (vegetation);
											}
											env.vegetationCreated++;
										}
									}
								}
#if UNITY_EDITOR
							}
#endif
						}
						voxelIndex -= ONE_Y_ROW;
					}

					// Continue filling down
					vd = heights [hindex].terrainVoxelDirt;
					while (voxelIndex >= 0) {
						if (voxels [voxelIndex].hasContent == 0) {
							voxels [voxelIndex].SetFast (vd, 15, 1);
						}
						voxelIndex -= ONE_Y_ROW;
					}
					if (placeBedrock) {
						voxels [voxelIndex + ONE_Y_ROW].Set (world.bedrockVoxel);
					}
					hasContent = true;
				}
			}

			chunk.isAboveSurface = isAboveSurface;
			return hasContent;
		}



	}

}