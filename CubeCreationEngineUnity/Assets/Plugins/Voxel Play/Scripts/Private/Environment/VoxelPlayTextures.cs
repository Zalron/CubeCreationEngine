using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		const int TEXTURE_ARRAY_INDEX_MASK = 1023;
		// 9 bits for texture indexing, bit 10 = has normal map, bit 11 = has displacement map

		struct WorldTexture {
			public Color32[] colorsAndEmission;
			public Color32[] normalsAndElevation;
		}


		/// <summary>
		/// List containing all world textures availables
		/// </summary>
		List<WorldTexture> worldTextures;

		/// <summary>
		/// Dictionary for fast texture search
		/// </summary>
		Dictionary<Texture2D, int> worldTexturesDict;

		/// <summary>
		/// Dictionary lookup for the voxel definition by name
		/// </summary>
		Dictionary<string, VoxelDefinition> voxelDefinitionsDict;

		/// <summary>
		/// Set to true if the texture array needs to be recreated (ie. new voxel definitions have been added)
		/// </summary>
		bool requireTextureArrayUpdate;

		/// <summary>
		/// Temporary/session voxels added by users at runtime
		/// </summary>
		List<VoxelDefinition> sessionUserVoxels;

		Color32[] defaultMapColors;

		void AddVoxelTextures (VoxelDefinition vd) {

			if (vd == null)
				return;

			if (vd.index > 0 && vd.index < voxelDefinitionsCount && voxelDefinitions [vd.index] == vd)
				return; // already added

			// Resize voxel definitions array?
			if (voxelDefinitionsCount >= voxelDefinitions.Length) {
				VoxelDefinition[] resized = new VoxelDefinition[voxelDefinitions.Length * 2];
				for (int k = 0; k < voxelDefinitionsCount; k++) {
					resized [k] = voxelDefinitions [k];
				}
				voxelDefinitions = resized;
			}

			voxelDefinitions [voxelDefinitionsCount] = vd;
			vd.index = (short)voxelDefinitionsCount;
			voxelDefinitionsCount++;
			voxelDefinitionsDict [vd.name] = vd;

			// Autofix certain non supported properties
			if (vd.navigatable) vd.navigatable = vd.renderType.supportsNavigation();

			// Check if custom model has collider
			vd.modelUsesCollider = false;
			if (vd.renderType == RenderType.Custom && vd.model!=null) {
				if (vd.gpuInstancing) {
					if (vd.createGameObject) {
						vd.modelUsesCollider = vd.model.GetComponent<Collider> () != null;
					}
				} else {
					vd.modelUsesCollider = vd.model.GetComponent<Collider> () != null;
				}
			}

			bool supportsEmission = vd.renderType.supportsEmission ();
			vd.textureIndexTop = AddTexture (vd.textureTop, supportsEmission ? vd.textureTopEmission : null, vd.textureTopNRM, vd.textureTopDISP);
			vd.textureIndexSide = AddTexture (vd.textureSide, supportsEmission ? vd.textureSideEmission : null, vd.textureSideNRM, vd.textureSideDISP);
			vd.textureIndexBottom = AddTexture (vd.textureBottom, supportsEmission ? vd.textureBottomEmission : null, vd.textureBottomNRM, vd.textureBottomDISP);
			vd.textureSideIndices = new TextureRotationIndices[4];
			if (vd.renderType.numberOfTextures () == 6) {
				int textureIndexRight = AddTexture (vd.textureRight, supportsEmission ? vd.textureRightEmission : null, vd.textureRightNRM, vd.textureRightDISP);
				int textureIndexForward = AddTexture (vd.textureForward, supportsEmission ? vd.textureForwardEmission : null, vd.textureForwardNRM, vd.textureForwardDISP);
				int textureIndexLeft = AddTexture (vd.textureLeft, supportsEmission ? vd.textureLeftEmission : null, vd.textureLeftNRM, vd.textureLeftDISP);

				// Rotated texture indices. In geometry shaders, lower 12 bits are combined with upper bits to define texture indices (2 texture indices per field)
				// x = side + forward, y = top + right, z = bottom + left
				if (effectiveUseGeometryShaders) {
					vd.textureSideIndices [0] = new TextureRotationIndices {
						xyzw = new Vector4 (vd.textureIndexSide + (textureIndexForward << 12), vd.textureIndexTop + (textureIndexRight << 12), vd.textureIndexBottom + (textureIndexLeft << 12), 0) 
					};
					vd.textureSideIndices [1] = new TextureRotationIndices {
						xyzw = new Vector4 (textureIndexRight + (textureIndexLeft << 12), vd.textureIndexTop + (textureIndexForward << 12), vd.textureIndexBottom + (vd.textureIndexSide << 12), 0)
					};
					vd.textureSideIndices [2] = new TextureRotationIndices {
						xyzw = new Vector4 (textureIndexForward + (vd.textureIndexSide << 12), vd.textureIndexTop + (textureIndexLeft << 12), vd.textureIndexBottom + (textureIndexRight << 12), 0)
					};
					vd.textureSideIndices [3] = new TextureRotationIndices {
						xyzw = new Vector4 (textureIndexLeft + (textureIndexLeft << 12), vd.textureIndexTop + (vd.textureIndexSide << 12), vd.textureIndexBottom + (textureIndexForward << 12), 0)
					};
				} else {
					vd.textureSideIndices [0] = new TextureRotationIndices {
						forward = textureIndexForward, right = textureIndexRight, back = vd.textureIndexSide, left = textureIndexLeft
					};
					vd.textureSideIndices [1] = new TextureRotationIndices {
						forward = textureIndexLeft, right = textureIndexForward, back = textureIndexRight, left = textureIndexForward
					};
					vd.textureSideIndices [2] = new TextureRotationIndices {
						forward = vd.textureIndexSide, right = textureIndexLeft, back = textureIndexForward, left = textureIndexRight
					};
					vd.textureSideIndices [3] = new TextureRotationIndices {
						forward = textureIndexRight, right = vd.textureIndexSide, back = textureIndexLeft, left = vd.textureIndexSide
					};
				}
			} else {
				if (effectiveUseGeometryShaders) {
					vd.textureSideIndices [0] = vd.textureSideIndices [1] = vd.textureSideIndices [2] = vd.textureSideIndices [3] = new TextureRotationIndices {
						xyzw = new Vector4 (vd.textureIndexSide + (vd.textureIndexSide << 12), vd.textureIndexTop + (vd.textureIndexSide << 12), vd.textureIndexBottom + (vd.textureIndexSide << 12), 0) 
					};
				} else {
					vd.textureSideIndices [0] = vd.textureSideIndices [1] = vd.textureSideIndices [2] = vd.textureSideIndices [3] = new TextureRotationIndices {
						forward = vd.textureIndexSide, right = vd.textureIndexSide, back = vd.textureIndexSide, left = vd.textureIndexSide
					};
				}
			}

			if (vd.renderType == RenderType.CutoutCross && vd.sampleColor.a == 0) {
				AnalyzeGrassTexture (vd, vd.textureSide);
			}

			GetVoxelThumbnails (vd);
        }

		/// <summary>
		/// Returns the index in the texture list and the full index (index in the list + some flags specifying existence of normal/displacement maps)
		/// </summary>
		int AddTexture (Texture2D texAlbedo, Texture2D texEmission, Texture2D texNRM, Texture2D texDISP) { 
			int index = 0;
			if (texAlbedo == null || worldTexturesDict.TryGetValue (texAlbedo, out index)) {
				return index;
			}

			// Add entry to dictionary
			index = worldTextures.Count;
			worldTexturesDict [texAlbedo] = index;

			// Albedo + Emission mask
			WorldTexture wt = new WorldTexture ();
			wt.colorsAndEmission = CombineAlbedoAndEmission (texAlbedo, texEmission);
			worldTextures.Add (wt);

			// Normal + Elevation Map
			if (enableNormalMap || enableReliefMapping) {
				WorldTexture wextra = new WorldTexture ();
				wextra.normalsAndElevation = CombineNormalsAndElevation (texNRM, texDISP);
				worldTextures.Add (wextra);
			}

			return index;
		}


		Color32[] CombineAlbedoAndEmission (Texture2D albedoMap, Texture2D emissionMap) {
			Color32[] mapColors;
			if (albedoMap.width != textureSize) {
				albedoMap = Instantiate (albedoMap) as Texture2D;
				albedoMap.hideFlags = HideFlags.DontSave;
				TextureTools.Scale (albedoMap, textureSize, textureSize, FilterMode.Point);
				mapColors = albedoMap.GetPixels32 ();
				DestroyImmediate (albedoMap);
			} else {
				mapColors = albedoMap.GetPixels32 ();
			}
			if (emissionMap == null) {
				return mapColors;
			}
			Color32[] emissionColors;
			if (emissionMap.width != textureSize) {
				emissionMap = Instantiate (emissionMap) as Texture2D;
				emissionMap.hideFlags = HideFlags.DontSave;
				TextureTools.Scale (emissionMap, textureSize, textureSize, FilterMode.Point);
				emissionColors = emissionMap.GetPixels32 ();
				DestroyImmediate (emissionMap);
			} else {
				emissionColors = emissionMap.GetPixels32 ();
			}
			for (int k = 0; k < mapColors.Length; k++) {
				mapColors [k].a = (byte)(255 - emissionColors [k].r);
			}
			return mapColors;
		}


		Color32[] CombineNormalsAndElevation (Texture2D normalMap, Texture2D elevationMap) {
			if (elevationMap == null && normalMap == null) {
				return GetDefaultMapColors ();
			}
			Color32[] normalMapColors, elevationMapColors;
			if (normalMap == null) {
				normalMapColors = GetDefaultMapColors ();
			} else if (normalMap.width != textureSize) {
				normalMap = Instantiate (normalMap) as Texture2D;
				normalMap.hideFlags = HideFlags.DontSave;
				TextureTools.Scale (normalMap, textureSize, textureSize, FilterMode.Point);
				normalMapColors = normalMap.GetPixels32 ();
				DestroyImmediate (normalMap);
			} else {
				normalMapColors = normalMap.GetPixels32 ();
			}
			if (elevationMap == null) {
				elevationMapColors = GetDefaultMapColors ();
			} else if (elevationMap.width != textureSize) {
				elevationMap = Instantiate (elevationMap) as Texture2D;
				elevationMap.hideFlags = HideFlags.DontSave;
				TextureTools.Scale (elevationMap, textureSize, textureSize, FilterMode.Point);
				elevationMapColors = elevationMap.GetPixels32 ();
				DestroyImmediate (elevationMap);
			} else {
				elevationMapColors = elevationMap.GetPixels32 ();
			}
			for (int k = 0; k < normalMapColors.Length; k++) {
				normalMapColors [k].a = elevationMapColors [k].r;	// copy elevation into alpha channel of normal map to save 1 texture slot in texture array and optimize cache
			}
			return normalMapColors;
		}

		Color32[] GetDefaultMapColors () {
			int len = textureSize * textureSize;
			if (defaultMapColors != null && defaultMapColors.Length == len) {
				return defaultMapColors;
			}
			defaultMapColors = new Color32[len];
			Color32 color = new Color32 (0, 0, 255, 255);
			defaultMapColors.Fill<Color32> (color);
			return defaultMapColors;
		}

		void AnalyzeGrassTexture (VoxelDefinition vd, Texture2D tex) {
            if (tex==null)
            {
                Debug.Log("AnalyzeGrassTexture: texture not found for " + vd.name);
                return;
            }
            // get sample color (random pixel from texture raw data)
            Color[] colors = tex.GetPixels ();
			int tw = tex.width;
			int th = tex.height;
			int pos = 4 * tw + tw * 3 / 4;
			if (pos >= colors.Length)
				pos = colors.Length - 1;
			for (int k = pos; k > 0; k--) {
				if (colors [k].a > 0.5f) {
					vd.sampleColor = colors [k];
					break;
				}
			}
			// get grass dimensions
			int xmin, xmax, ymin, ymax;
			xmin = tw;
			xmax = 0;
			ymin = th;
			ymax = 0;
			for (int y = 0; y < th; y++) {
				int yy = y * tw;
				for (int x = 0; x < tw; x++) {
					if (colors [yy + x].a > 0.5f) {
						if (x < xmin)
							xmin = x;
						if (x > xmax)
							xmax = x;
						if (y < ymin)
							ymin = y;
						if (y > ymax)
							ymax = y;
					}
				}
			}
			float w = (xmax - xmin + 1f) / tw;
			float h = (ymax - ymin + 1f) / th;
			vd.scale = new Vector3 (w, h, w);
		}

		void GetVoxelThumbnails (VoxelDefinition vd) {
			if (vd.textureTop != null) {
				vd.textureThumbnailTop = Instantiate (vd.textureTop) as Texture2D;
				vd.textureThumbnailTop.hideFlags = HideFlags.DontSave;
				TextureTools.Scale (vd.textureThumbnailTop, 64, 64, FilterMode.Point);
			}
			if (vd.textureSide != null) {
				vd.textureThumbnailSide = Instantiate (vd.textureSide) as Texture2D;
				vd.textureThumbnailSide.hideFlags = HideFlags.DontSave;
				TextureTools.Scale (vd.textureThumbnailSide, 64, 64, FilterMode.Point);
			}
			if (vd.textureBottom != null) {
				vd.textureThumbnailBottom = Instantiate (vd.textureBottom) as Texture2D;
				vd.textureThumbnailBottom.hideFlags = HideFlags.DontSave;
				TextureTools.Scale (vd.textureThumbnailBottom, 64, 64, FilterMode.Point);
			}
		}


		void LoadWorldTextures () {

			requireTextureArrayUpdate = false;

			// Init texture array
			if (worldTextures == null) {
				worldTextures = new List<WorldTexture> ();
			} else {
				worldTextures.Clear ();
			}
			if (worldTexturesDict == null) {
				worldTexturesDict = new Dictionary<Texture2D, int> ();
			} else {
				worldTexturesDict.Clear ();
			}

			// Clear definitions
			if (voxelDefinitions != null) {
				// Voxel Definitions no longer are added to the dictionary, clear the index field.
				for (int k = 0; k < voxelDefinitionsCount; k++) {
					if (voxelDefinitions [k] != null) {
						voxelDefinitions [k].index = 0;
					}
				}
			} else {
				voxelDefinitions = new VoxelDefinition[128];
			}
			voxelDefinitionsCount = 0;
			if (voxelDefinitionsDict == null) {
				voxelDefinitionsDict = new Dictionary<string, VoxelDefinition> ();
			} else {
				voxelDefinitionsDict.Clear ();
			}
			if (sessionUserVoxels == null) {
				sessionUserVoxels = new List<VoxelDefinition> ();
			}
			// The null voxel definition
			VoxelDefinition nullVoxelDefinition = ScriptableObject.CreateInstance<VoxelDefinition> ();
			nullVoxelDefinition.hidden = true;
			nullVoxelDefinition.canBeCollected = false;
			nullVoxelDefinition.name = "Null";
			AddVoxelTextures (nullVoxelDefinition);

			// Check default voxel
			if (defaultVoxel == null) {
				defaultVoxel = Resources.Load<VoxelDefinition> ("VoxelPlay/Defaults/DefaultVoxel");
			}
			AddVoxelTextures (defaultVoxel);

			// Add all biome textures
			if (world.biomes != null) {
				for (int k = 0; k < world.biomes.Length; k++) {
					BiomeDefinition biome = world.biomes [k];
					if (biome == null)
						continue;
					AddVoxelTextures (biome.voxelTop);
					biome.voxelTop.biomeDirtCounterpart = biome.voxelDirt;
					AddVoxelTextures (biome.voxelDirt);
					if (biome.vegetation != null) {
						for (int v = 0; v < biome.vegetation.Length; v++) {
							AddVoxelTextures (biome.vegetation [v].vegetation);
						}
					}
					if (biome.trees != null) {
						for (int t = 0; t < biome.trees.Length; t++) {
							ModelDefinition tree = biome.trees [t].tree;
							if (tree == null)
								continue;
							for (int b = 0; b < tree.bits.Length; b++) {
								AddVoxelTextures (tree.bits [b].voxelDefinition);
							}
						}
					}
					if (biome.ores != null) {
						for (int v = 0; v < biome.ores.Length; v++) {
							AddVoxelTextures (biome.ores [v].ore);
						}
					}
				}
			}

			// Special voxels
			if (world.cloudVoxel == null) {
				world.cloudVoxel = Resources.Load<VoxelDefinition> ("VoxelPlay/Defaults/VoxelCloud");
			}
			AddVoxelTextures (world.cloudVoxel);
			AddVoxelTextures (world.bedrockVoxel);

			// Add additional world voxels
			if (world.moreVoxels != null) {
				for (int k = 0; k < world.moreVoxels.Length; k++) {
					AddVoxelTextures (world.moreVoxels [k]);
				}
			}

			// Add any other voxel found inside Defaults
			VoxelDefinition[] vdd = Resources.LoadAll<VoxelDefinition> ("VoxelPlay/Defaults");
			for (int k = 0; k < vdd.Length; k++) {
				AddVoxelTextures (vdd [k]);
			}

			// Add any other voxel found inside World directory
			vdd = Resources.LoadAll<VoxelDefinition> ("Worlds/" + world.name);
			for (int k = 0; k < vdd.Length; k++) {
				AddVoxelTextures (vdd [k]);
			}

			// Add any other voxel found inside a resource directory with same name of world
			vdd = Resources.LoadAll<VoxelDefinition> (world.name);
			for (int k = 0; k < vdd.Length; k++) {
				AddVoxelTextures (vdd [k]);
			}

			// Add user provided voxels during playtime
			int count = sessionUserVoxels.Count;
			for (int k = 0; k < count; k++) {
				AddVoxelTextures (sessionUserVoxels [k]);
			}

			// Unload textures (doesn't work at runtime on PC! this commented section should be removed)
//			#if !UNITY_EDITOR
//			for (int k = 0; k < voxelDefinitionsCount; k++) {
//				VoxelDefinition vd = voxelDefinitions [k];
//				if (vd.index == 0)
//					continue;
//				if (vd.textureTop != null)
//					Resources.UnloadAsset (vd.textureTop);
//				if (vd.textureTopEmission != null)
//					Resources.UnloadAsset (vd.textureTopEmission);
//				if (vd.textureTopNRM != null)
//					Resources.UnloadAsset (vd.textureTopNRM);
//				if (vd.textureTopDISP != null)
//					Resources.UnloadAsset (vd.textureTopDISP);
//				if (vd.textureLeft != null)
//					Resources.UnloadAsset (vd.textureLeft);
//				if (vd.textureLeftEmission != null)
//					Resources.UnloadAsset (vd.textureLeftEmission);
//				if (vd.textureLeftNRM != null)
//					Resources.UnloadAsset (vd.textureLeftNRM);
//				if (vd.textureLeftDISP != null)
//					Resources.UnloadAsset (vd.textureLeftDISP);
//				if (vd.textureRight != null)
//					Resources.UnloadAsset (vd.textureRight);
//				if (vd.textureRightEmission != null)
//					Resources.UnloadAsset (vd.textureRightEmission);
//				if (vd.textureRightNRM != null)
//					Resources.UnloadAsset (vd.textureRightNRM);
//				if (vd.textureRightDISP != null)
//					Resources.UnloadAsset (vd.textureRightDISP);
//				if (vd.textureBottom != null)
//					Resources.UnloadAsset (vd.textureBottom);
//				if (vd.textureBottomEmission != null)
//					Resources.UnloadAsset (vd.textureBottomEmission);
//				if (vd.textureBottomNRM != null)
//					Resources.UnloadAsset (vd.textureBottomNRM);
//				if (vd.textureBottomDISP != null)
//					Resources.UnloadAsset (vd.textureBottomDISP);
//				if (vd.textureSide != null)
//					Resources.UnloadAsset (vd.textureSide);
//				if (vd.textureSideEmission != null)
//					Resources.UnloadAsset (vd.textureSideEmission);
//				if (vd.textureSideNRM != null)
//					Resources.UnloadAsset (vd.textureSideNRM);
//				if (vd.textureSideDISP != null)
//					Resources.UnloadAsset (vd.textureSideDISP);
//				if (vd.textureForward != null)
//					Resources.UnloadAsset (vd.textureForward);
//				if (vd.textureForwardEmission != null)
//					Resources.UnloadAsset (vd.textureForwardEmission);
//				if (vd.textureForwardNRM != null)
//					Resources.UnloadAsset (vd.textureForwardNRM);
//				if (vd.textureForwardDISP != null)
//					Resources.UnloadAsset (vd.textureForwardDISP);
//			}
//			#endif

			// Create array texture
			int textureCount = worldTextures.Count;
			if (textureCount > 0) {
				Texture2DArray pointFilterTextureArray = new Texture2DArray (textureSize, textureSize, textureCount, TextureFormat.ARGB32, hqFiltering);
				pointFilterTextureArray.wrapMode = enableReliefMapping ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
				pointFilterTextureArray.filterMode = hqFiltering ? FilterMode.Bilinear : FilterMode.Point;
				pointFilterTextureArray.mipMapBias = -mipMapBias;
				for (int k = 0; k < textureCount; k++) {
					if (worldTextures [k].colorsAndEmission != null) {
						pointFilterTextureArray.SetPixels32 (worldTextures [k].colorsAndEmission, k);
					} else if (worldTextures [k].normalsAndElevation != null) {
						pointFilterTextureArray.SetPixels32 (worldTextures [k].normalsAndElevation, k);
					}
				}
				worldTextures.Clear ();

				pointFilterTextureArray.Apply (hqFiltering, true);
				if (matTerrainOpaque != null) {
					matTerrainOpaque.SetTexture ("_MainTex", pointFilterTextureArray);
				}
				if (matTerrainCutout != null) {
					matTerrainCutout.SetTexture ("_MainTex", pointFilterTextureArray);
				}
				if (matTerrainWater != null) {
					matTerrainWater.SetTexture ("_MainTex", pointFilterTextureArray);
				}
				if (matTerrainTransp != null) {
					matTerrainTransp.SetTexture ("_MainTex", pointFilterTextureArray);
				}
				if (matTerrainCutxss != null) {
					matTerrainCutxss.SetTexture ("_MainTex", pointFilterTextureArray);
				}
				if (matTerrainOpNoAO != null) {
					matTerrainOpNoAO.SetTexture ("_MainTex", pointFilterTextureArray);
				}
				if (matTriangleOpaque != null) {
					matTriangleOpaque.SetTexture ("_MainTex", pointFilterTextureArray);
				}
				if (matTriangleCutout != null) {
					matTriangleCutout.SetTexture ("_MainTex", pointFilterTextureArray);
				}
			}
		}

		[System.Obsolete ("Use GetVoxelDefinition(string name) instead.")]
		VoxelDefinition GetVoxelDefinitionByName (string name) {
			VoxelDefinition vd;
			voxelDefinitionsDict.TryGetValue (name, out vd);
			return vd;
		}
				

	}


}
