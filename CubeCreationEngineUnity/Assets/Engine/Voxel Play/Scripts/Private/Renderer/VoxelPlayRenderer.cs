using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using VoxelPlay.GPUInstancing;
using VoxelPlay.GPULighting;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		struct VirtualVoxel {
			public int chunk9Index;
			public int voxelIndex;
		}

		struct MeshJobData {
			public VoxelChunk chunk;
			public int totalVisibleVoxels;

			public List<Vector3> vertices;
			public List<Vector4> uv0;
			public List<Vector4> uv2;
			public List<Color32> colors;
			public List<Vector3> normals;

			public int opaqueVoxelsCount;
			public int[] opaqueIndicesArray;
			public List<int> opaqueIndices;

			public int cutoutVoxelsCount;
			public int[] cutoutIndicesArray;
			public List<int> cutoutIndices;

			public int waterVoxelsCount;
			public int[] waterIndicesArray;
			public List<int> waterIndices;

			public int cutxssVoxelsCount;
			public int[] cutxssIndicesArray;
			public List<int> cutxssIndices;

			public int opNoAOVoxelsCount;
			public int[] opNoAOIndicesArray;

			public int transpVoxelsCount;
			public int[] transpIndicesArray;
			public List<int> transpIndices;

			public int subMeshCount;
			public int matVoxelIndex;
			public List<Vector3> colliderVertices;
			public List<int> colliderIndices;

			public List<int> navMeshIndices;
			public List<Vector3> navMeshVertices;

			// models in voxels
			public FastList<ModelInVoxel> mivs;
		}


		struct ModelInVoxel {
			public VoxelDefinition vd;
			public int voxelIndex;
		}


		const int MESH_JOBS_POOL_SIZE = 4000;

		/* cube coords
		
		7+------+6
		/.   3 /|
		2+------+ |
		|4.....|.+5
		|/     |/
		0+------+1
		
		*/

		VoxelPlayGreedyMesher greedyCollider, greedyNavMesh;
		MeshJobData[] meshJobs;
		volatile int meshJobMeshLastIndex;
		volatile int meshJobMeshDataGenerationIndex;
		volatile int meshJobMeshDataGenerationReadyIndex;
		volatile int meshJobMeshUploadIndex;

		List<int> opaqueIndices;
		List<int> cutoutIndices;
		List<int> waterIndices;
		List<int> cutxssIndices;
		List<int> opNoAOIndices;
		List<int> transpIndices;

		// Chunk Rendering
		bool effectiveUseGeometryShaders, effectiveMultithreadGeneration;
		int chunkRequestLast;
		Voxel[][] chunk9;
		VirtualVoxel[] virtualChunk;
		Voxel[] emptyChunkUnderground, emptyChunkAboveTerrain;

		// Unconclusive neighbours
		const byte CHUNK_TOP = 1;
		const byte CHUNK_BOTTOM = 2;
		const byte CHUNK_LEFT = 4;
		const byte CHUNK_RIGHT = 8;
		const byte CHUNK_BACK = 16;
		const byte CHUNK_FORWARD = 32;
		const byte CHUNK_IS_INCONCLUSIVE = 128;

		byte[] inconclusiveNeighbourTable = new byte[] {
			0, 0, 0,
			0, CHUNK_BOTTOM, 0,
			0, 0, 0,
			0, CHUNK_BACK, 0,
			CHUNK_LEFT, 0, CHUNK_RIGHT,
			0, CHUNK_FORWARD, 0,
			0, 0, 0,
			0, CHUNK_TOP, 0,
			0, 0, 0
		};
			
		// Voxels
		[NonSerialized]
		List<Vector3> tempChunkVertices;
		List<Vector4> tempChunkUV0;
		List<Vector4> tempChunkUV2;
		List<Color32> tempChunkColors32;
		List<Vector3> tempChunkNormals;

		// Collider support
		List<int> meshColliderIndices;
		List<Vector3> meshColliderVertices;

		// Navmesh support
		List<int> navMeshIndices;
		List<Vector3> navMeshVertices;

		// Model-in-voxel support
		FastList<ModelInVoxel> mivs;

		// Materials
		Material matTerrainOpaque, matTerrainCutout, matTerrainWater, matTerrainTransp, matTerrainCutxss, matTerrainOpNoAO;
		Material[][] matTerrainArray;
		Material matTriangleOpaque, matTriangleCutout;

		// Multi-thread support
		bool generationThreadRunning;
		AutoResetEvent waitEvent;
		Thread meshGenerationThread;
		object indicesUpdating = new object ();

		// Model-in-Voxel support
		List<Color32> modelMeshColors;

		// Dynamic Voxel mesh generation support
		List<Vector3> tempVertices;
		List<Vector3> tempNormals;
		int[] tempIndices;
		int tempIndicesPos;
		List<Vector4> tempUVs;
		List<Color32> tempColors;

		// Instancing
		VoxelPlayInstancingRendererManager instancingManager;


		#region Renderer initialization

		void InitRenderer () {
			// Init materials
			// triangle opaque & cutout must always be loaded to support custom voxel types
			matTriangleOpaque = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Triangle Opaque"));
			matTriangleCutout = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Triangle Cutout"));
			if (effectiveUseGeometryShaders) {
				matTerrainOpaque = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Geo Opaque"));
				matTerrainCutout = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Geo Cutout"));
				if (shadowsOnWater && !draftModeActive) {
					matTerrainWater = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Geo Water"));
				} else {
					matTerrainWater = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Geo Water No Shadows"));
				}
				matTerrainCutxss = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Geo Cutout Cross"));
				matTerrainOpNoAO = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Geo Opaque No AO"));
				if (doubleSidedGlass) {
					matTerrainTransp = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Geo Transp Double Sided"));
				} else {
					matTerrainTransp = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Geo Transp"));
				}
			} else {
				matTerrainOpaque = matTriangleOpaque;
				matTerrainCutout = matTriangleCutout;
				if (shadowsOnWater && !draftModeActive) {
					matTerrainWater = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Triangle Water"));
				} else {
					matTerrainWater = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Triangle Water No Shadows"));
				}
				matTerrainCutxss = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Triangle Cutout Cross"));
				if (doubleSidedGlass) {
					matTerrainTransp = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Triangle Transp Double Sided"));
				} else {
					matTerrainTransp = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Triangle Transp"));
				}
				matTerrainOpNoAO = matTerrainOpaque;
			}

			// Init system arrays and structures
			// Bit  Type
			// 1    Opaque
			// 2    Cutout
			// 3    Water
			// 4    Cutout Cross
			// 5    Opaque No AO (ie. clouds)
			// 6    Transp

			Material[] nativeMaterials = new Material[] {
				matTerrainOpaque,
				matTerrainCutout,
				matTerrainWater,
				matTerrainCutxss,
				matTerrainOpNoAO,
				matTerrainTransp
			};
			int matCount = nativeMaterials.Length;
			int matVariantCount = (int)Mathf.Pow (2, matCount);
			matTerrainArray = new Material[matVariantCount][];
			List<Material> entryMaterials = new List<Material> ();
			for (int k = 1; k < matVariantCount; k++) {
				entryMaterials.Clear ();
				for (int b = 0; b < matCount; b++) {
					if ((k & (1 << b)) != 0) {
						entryMaterials.Add (nativeMaterials [b]);
					}
				}
				matTerrainArray [k] = entryMaterials.ToArray ();
			}

			modelMeshColors = new List<Color32> (128);
			tempVertices = new List<Vector3> (36);
			tempNormals = new List<Vector3> (36);
			tempUVs = new List<Vector4> (36);
			tempIndices = new int[36];
			tempColors = new List<Color32> (36);

			InitMeshJobsPool ();

			if (effectiveUseGeometryShaders) {
				int initialCapacity = 2048;
				opaqueIndices = new List<int> (initialCapacity);
				cutoutIndices = new List<int> (initialCapacity);
				waterIndices = new List<int> (initialCapacity);
				cutxssIndices = new List<int> (initialCapacity);
				opNoAOIndices = new List<int> (initialCapacity);
				transpIndices = new List<int> (initialCapacity);
			}

			Voxel.Empty.light = noLightValue;
			InitVirtualChunk ();

			greedyCollider = new VoxelPlayGreedyMesher ();
			greedyNavMesh = new VoxelPlayGreedyMesher ();
			greedyNoAO = new VoxelPlayGreedyMesher (true);
			greedyCutout = new VoxelPlayGreedyMesher (true);

			instancingManager = new VoxelPlayInstancingRendererManager (this);
			if (currentCamera.GetComponent<VoxelPlayLightManager> () == null) {
				currentCamera.gameObject.AddComponent<VoxelPlayLightManager> ();
			}
			StartGenerationThread ();
		}

		void StartGenerationThread () {
			if (effectiveMultithreadGeneration) {
				generationThreadRunning = true;
				waitEvent = new AutoResetEvent (false);
				meshGenerationThread = new Thread (GenerateChunkMeshDataInBackgroundThread);
				meshGenerationThread.Start ();
			}
		}

		void StopGenerationThread () {
			generationThreadRunning = false;
			if (meshGenerationThread != null) {
				waitEvent.Set ();
				for (int k = 0; k < 100; k++) {
					bool wait = false;
					if (meshGenerationThread.IsAlive)
						wait = true;
					if (!wait)
						break;
					Thread.Sleep (10);
				}
			}
		}

	
		void InitVirtualChunk () {
			chunk9 = new Voxel[27][];
			emptyChunkUnderground = new Voxel[16 * 16 * 16];
			emptyChunkAboveTerrain = new Voxel[16 * 16 * 16];
			for (int k = 0; k < emptyChunkAboveTerrain.Length; k++) {
				emptyChunkAboveTerrain [k].light = (byte)15;

				emptyChunkUnderground [k].hasContent = 1;
				emptyChunkUnderground [k].opaque = FULL_OPAQUE;
			}

			virtualChunk = new VirtualVoxel[18 * 18 * 18];

			int index = 0;
			for (int y = 0; y < 18; y++) {
				for (int z = 0; z < 18; z++) {
					for (int x = 0; x < 18; x++, index++) {
						int vy = 1, vz = 1, vx = 1;
						if (y == 0) {
							vy = 0;
						} else if (y == 17) {
							vy = 2;
						} 
						if (z == 0) {
							vz = 0;
						} else if (z == 17) {
							vz = 2;
						}
						if (x == 0) {
							vx = 0;
						} else if (x == 17) {
							vx = 2;
						}
						virtualChunk [index].chunk9Index = vy * 9 + vz * 3 + vx;
						int py = (y + 15) % 16;
						int pz = (z + 15) % 16;
						int px = (x + 15) % 16;
						virtualChunk [index].voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
					}
				}
			}
		}

		void InitMeshJobsPool () {

			#if UNITY_WEBGL
			meshJobs = new MeshJobData[384];
			#else
			meshJobs = new MeshJobData[(isMobilePlatform || !effectiveUseGeometryShaders) ? 256 : MESH_JOBS_POOL_SIZE];
			#endif
			meshJobMeshLastIndex = -1;
			meshJobMeshDataGenerationIndex = -1;
			meshJobMeshDataGenerationReadyIndex = -1;
			meshJobMeshUploadIndex = -1;
			int initialCapacity = effectiveUseGeometryShaders ? 2048 : 20000;
			for (int k = 0; k < meshJobs.Length; k++) {
				meshJobs [k].vertices = new List<Vector3> (initialCapacity);
				meshJobs [k].uv0 = new List<Vector4> (initialCapacity);
				meshJobs [k].uv2 = new List<Vector4> (initialCapacity);
				meshJobs [k].colors = new List<Color32> (initialCapacity);
				meshJobs [k].normals = new List<Vector3> (initialCapacity);
				if (!effectiveUseGeometryShaders) {
					meshJobs [k].opaqueIndices = new List<int> (20000);
					meshJobs [k].cutoutIndices = new List<int> (20000);
					meshJobs [k].waterIndices = new List<int> (5000);
					meshJobs [k].cutxssIndices = new List<int> (2000);
					meshJobs [k].transpIndices = new List<int> ();
				}
				if (enableColliders) {
					meshJobs [k].colliderVertices = new List<Vector3> (2700);
					meshJobs [k].colliderIndices = new List<int> (4000);
				}
				if (enableNavMesh) {
					meshJobs [k].navMeshVertices = new List<Vector3> (2700);
					meshJobs [k].navMeshIndices = new List<int> (4000);
				}
				meshJobs [k].mivs = new FastList<ModelInVoxel> ();

			}
		}

		#endregion


		#region Rendering


		public void UpdateMaterialProperties () {

			if (matTerrainOpaque == null)
				return;

			if (draftModeActive || !enableSmoothLighting) {
				matTerrainOpaque.DisableKeyword (SKW_VOXELPLAY_USE_AO);
				matTerrainCutout.DisableKeyword (SKW_VOXELPLAY_USE_AO);
			} else {
				matTerrainOpaque.EnableKeyword (SKW_VOXELPLAY_USE_AO);
				matTerrainCutout.EnableKeyword (SKW_VOXELPLAY_USE_AO);
			}
			if (enableTinting) {
				matTerrainOpaque.EnableKeyword (SKW_VOXELPLAY_USE_TINTING);
				matTriangleOpaque.EnableKeyword (SKW_VOXELPLAY_USE_TINTING);
				matTriangleCutout.EnableKeyword (SKW_VOXELPLAY_USE_TINTING);
			} else {
				matTerrainOpaque.DisableKeyword (SKW_VOXELPLAY_USE_TINTING);
				matTriangleOpaque.DisableKeyword (SKW_VOXELPLAY_USE_TINTING);
				matTriangleCutout.DisableKeyword (SKW_VOXELPLAY_USE_TINTING);
			}
			if (enableFogSkyBlending) {
				Shader.EnableKeyword (SKW_VOXELPLAY_GLOBAL_USE_FOG);
			} else {
				Shader.DisableKeyword (SKW_VOXELPLAY_GLOBAL_USE_FOG);
			}
			if (hqFiltering && !enableReliefMapping) {
				matTerrainOpaque.EnableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTerrainCutout.EnableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTerrainCutxss.EnableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTerrainOpNoAO.EnableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTerrainWater.EnableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTriangleOpaque.EnableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTriangleCutout.EnableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTerrainTransp.EnableKeyword (SKW_VOXELPLAY_AA_TEXELS);
			} else {
				matTerrainOpaque.DisableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTerrainCutout.DisableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTerrainCutxss.DisableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTerrainOpNoAO.DisableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTerrainWater.DisableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTriangleOpaque.DisableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTriangleCutout.DisableKeyword (SKW_VOXELPLAY_AA_TEXELS);
				matTerrainTransp.DisableKeyword (SKW_VOXELPLAY_AA_TEXELS);
			}
			UpdateOutlineProperties ();
			UpdateNormalMapProperties ();
			UpdateParallaxProperties ();
			UpdatePixelLightsProperties ();

			UpdateAmbientProperties ();
		}

		void UpdateAmbientProperties() {

			if (world == null)
				return;
			
			if (adjustCameraFarClip) {
				cameraMain.farClipPlane = visibleChunksDistance * 16f;
			}
			float thisFogDistance = fogUseCameraFarClip ? cameraMain.farClipPlane : fogDistance;
			float thisFogStart = thisFogDistance * fogFallOff;
			Vector3 fogData = new Vector3 (thisFogStart * thisFogStart, thisFogDistance * thisFogDistance - thisFogStart * thisFogStart, 0);

			// Global sky uniforms
			Shader.SetGlobalColor ("_VPSkyTint", world.skyTint);
			Shader.SetGlobalVector ("_VPFogData", fogData);
			Shader.SetGlobalFloat ("_VPFogAmount", fogAmount);
			Shader.SetGlobalFloat ("_VPExposure", world.exposure);
			Shader.SetGlobalFloat ("_VPAmbientLight", ambientLight);
			Shader.SetGlobalFloat ("_VPDaylightShadowAtten", daylightShadowAtten);

			// Update skybox material
			VoxelPlaySkybox worldSkybox = isMobilePlatform ? world.skyboxMobile : world.skyboxDesktop;

			if (worldSkybox != VoxelPlaySkybox.UserDefined) {
				if (skyboxMaterial != RenderSettings.skybox) {
					switch (worldSkybox) {
					case VoxelPlaySkybox.Earth:
						if (skyboxEarth == null) {
							skyboxEarth = Resources.Load<Material> ("VoxelPlay/Materials/VP Skybox Earth");
						}
						world.skyTint = new Color (0.52f, 0.5f, 1f); // default color for this style
						skyboxMaterial = skyboxEarth;
						break;
					case VoxelPlaySkybox.EarthSimplified:
						if (skyboxEarthSimplified == null) {
							skyboxEarthSimplified = Resources.Load<Material> ("VoxelPlay/Materials/VP Skybox Earth Simplified");
						}
						world.skyTint = new Color (0.52f, 0.5f, 1f); // default color for this style
						skyboxMaterial = skyboxEarthSimplified;
						break;
					case VoxelPlaySkybox.Space:
						if (skyboxSpace == null) {
							skyboxSpace = Resources.Load<Material> ("VoxelPlay/Materials/VP Skybox Space");
						}
						world.skyTint = Color.black;
						skyboxMaterial = skyboxSpace;
						break;
					case VoxelPlaySkybox.EarthNightCubemap:
						if (skyboxEarthNightCube == null) {
							skyboxEarthNightCube = Resources.Load<Material> ("VoxelPlay/Materials/VP Skybox Earth Night Cubemap");
						}
						world.skyTint = new Color (0.52f, 0.5f, 1f); // default color for this style
						if (world.skyboxNightCubemap != null)
							skyboxEarthNightCube.SetTexture ("_NightTex", world.skyboxNightCubemap);
						skyboxMaterial = skyboxEarthNightCube;
						break;
					case VoxelPlaySkybox.EarthDayNightCubemap:
						if (skyboxEarthDayNightCube == null) {
							skyboxEarthDayNightCube = Resources.Load<Material> ("VoxelPlay/Materials/VP Skybox Earth Day Night Cubemap");
						}
						world.skyTint = new Color (0.52f, 0.5f, 1f); // default color for this style
						if (world.skyboxDayCubemap != null)
							skyboxEarthDayNightCube.SetTexture ("_DayTex", world.skyboxDayCubemap);
						if (world.skyboxNightCubemap != null)
							skyboxEarthDayNightCube.SetTexture ("_NightTex", world.skyboxNightCubemap);
						skyboxMaterial = skyboxEarthDayNightCube;
						break;
					}
					RenderSettings.skybox = skyboxMaterial;
				}
			}
		}

		public void UpdateOutlineProperties () {
			UpdateOutlinePropertiesMat (matTerrainOpaque);
			UpdateOutlinePropertiesMat (matTriangleOpaque);
		}

		void UpdateOutlinePropertiesMat (Material mat) {
			if (mat != null) {
				if (enableOutline) {
					mat.EnableKeyword (SKW_VOXELPLAY_USE_OUTLINE);
					mat.SetColor ("_OutlineColor", outlineColor);
					mat.SetFloat ("_OutlineThreshold", outlineThreshold);
				} else {
					mat.DisableKeyword (SKW_VOXELPLAY_USE_OUTLINE);
				}
			}
		}

		void UpdateParallaxProperties () {
			UpdateParallaxPropertiesMat (matTerrainOpaque);
			UpdateParallaxPropertiesMat (matTerrainCutout);
			UpdateParallaxPropertiesMat (matTerrainWater);
		}

		void UpdateParallaxPropertiesMat (Material mat) {
			if (mat != null) {
				if (enableReliefMapping) {
					mat.EnableKeyword (SKW_VOXELPLAY_USE_PARALLAX);
					mat.SetFloat ("_VPParallaxStrength", reliefStrength);
					mat.SetInt ("_VPParallaxIterations", reliefIterations);
					mat.SetInt ("_VPParallaxIterationsBinarySearch", reliefIterationsBinarySearch);
				} else {
					mat.DisableKeyword (SKW_VOXELPLAY_USE_PARALLAX);
				}
			}
		}

		void UpdateNormalMapProperties () {
			UpdateNormalMapPropertiesMat (matTerrainOpaque);
			UpdateNormalMapPropertiesMat (matTerrainCutout);
			UpdateNormalMapPropertiesMat (matTerrainWater);
		}

		void UpdateNormalMapPropertiesMat (Material mat) {
			if (mat != null) {
				if (enableNormalMap) {
					mat.EnableKeyword (SKW_VOXELPLAY_USE_NORMAL);
				} else {
					mat.DisableKeyword (SKW_VOXELPLAY_USE_NORMAL);
				}
			}
		}

		void UpdatePixelLightsProperties() {
			UpdatePixelLightsPropertiesMat (matTerrainOpaque);
			UpdatePixelLightsPropertiesMat (matTerrainCutout);
			UpdatePixelLightsPropertiesMat (matTerrainOpNoAO);
			UpdatePixelLightsPropertiesMat (matTerrainTransp);
			UpdatePixelLightsPropertiesMat (matTerrainWater);
		}

		void UpdatePixelLightsPropertiesMat (Material mat) {
			if (mat != null) {
				if (usePixelLights) {
					mat.EnableKeyword (SKW_VOXELPLAY_USE_PIXEL_LIGHTS);
				} else {
					mat.DisableKeyword (SKW_VOXELPLAY_USE_PIXEL_LIGHTS);
				}
			}
		}


		public void GetMeshJobsStatus (out int lastJobIndex, out int currentGenerationJobIndex, out int currentMeshUploadIndex) {
			lastJobIndex = meshJobMeshLastIndex;
			currentGenerationJobIndex = meshJobMeshDataGenerationReadyIndex;
			currentMeshUploadIndex = meshJobMeshUploadIndex;
		}

		bool CreateChunkMeshJob (VoxelChunk chunk) {
			int newJobIndex = meshJobMeshLastIndex + 1;
			if (newJobIndex >= meshJobs.Length) {
				newJobIndex = 0;
			}

			if (newJobIndex == meshJobMeshDataGenerationIndex || newJobIndex == meshJobMeshUploadIndex) {
				ShowMessage ("Exhausted mesh job queue!");
				return false;
			}
			lock (indicesUpdating) {
				meshJobs [newJobIndex].chunk = chunk;
				meshJobMeshLastIndex = newJobIndex;
				if (generationThreadRunning)
					waitEvent.Set ();
			}
			return true;
		}


		void GenerateChunkMeshDataInBackgroundThread () {
			try {
				while (generationThreadRunning) {
					bool idle;
					lock (indicesUpdating) {
						idle = meshJobMeshDataGenerationIndex == meshJobMeshLastIndex;
					}
					if (idle) {
						waitEvent.WaitOne ();
						//Thread.Sleep (0); 
						continue;
					}
					GenerateChunkMeshDataOneJob ();
					lock (indicesUpdating) {
						meshJobMeshDataGenerationReadyIndex = meshJobMeshDataGenerationIndex; 
					}
				}
			} catch (Exception ex) {
				ShowExceptionMessage (ex);
			}
		}


		void GenerateChunkMeshDataInMainThread (long endTime) {

			long elapsed;
			do {
				if (meshJobMeshDataGenerationIndex == meshJobMeshLastIndex)
					return;
				GenerateChunkMeshDataOneJob ();
				meshJobMeshDataGenerationReadyIndex = meshJobMeshDataGenerationIndex;
				elapsed = stopWatch.ElapsedMilliseconds;
			} while (elapsed < endTime);
		}

		void GenerateChunkMeshDataOneJob () {
			meshJobMeshDataGenerationIndex++;
			if (meshJobMeshDataGenerationIndex >= meshJobs.Length) {
				meshJobMeshDataGenerationIndex = 0;
			}

			VoxelChunk chunk = meshJobs [meshJobMeshDataGenerationIndex].chunk;
			chunk9 [13] = chunk.voxels;
			Voxel[] emptyChunk = chunk.isAboveSurface ? emptyChunkAboveTerrain : emptyChunkUnderground;
			int chunkX = FastMath.FloorToInt (chunk.position.x / 16);
			int chunkY = FastMath.FloorToInt (chunk.position.y / 16);
			int chunkZ = FastMath.FloorToInt (chunk.position.z / 16);

			// Reset bit field; inconclusive neighbours are those neighbours which are undefined when an adjacent chunk is rendered. We mark it so then it's finally rendered, we re-render the adjacent chunk. This is required if the new chunk can 
			// break holes on the edge of the chunk while no lighting is entering the chunk or global illumination is disabled (yes, it's an edge case but must be addressed to avoid gaps in those cases).
			chunk.inconclusiveNeighbours = (byte)(chunk.inconclusiveNeighbours & ~CHUNK_IS_INCONCLUSIVE);

			for (int c = 0, y = -1; y <= 1; y++) {
				int yy = chunkY + y;
				for (int z = -1; z <= 1; z++) {
					int zz = chunkZ + z;
					for (int x = -1; x <= 1; x++, c++) {
						if (y == 0 && z == 0 && x == 0)
							continue;
						int xx = chunkX + x;
						VoxelChunk neighbour;
						if (GetChunkFast (xx, yy, zz, out neighbour, false) && (neighbour.isPopulated || neighbour.isRendered)) {
							chunk9 [c] = neighbour.voxels;
						} else {
							chunk.inconclusiveNeighbours |= inconclusiveNeighbourTable [c];
							chunk9 [c] = emptyChunk;
						}
					}
				}
			}

			if (effectiveUseGeometryShaders) {
				GenerateMeshData_Geo (meshJobMeshDataGenerationIndex);
			} else {
				GenerateMeshData_Triangle (meshJobMeshDataGenerationIndex);
			}
		}

		void UploadMeshData (int jobIndex) {

			VoxelChunk chunk = meshJobs [jobIndex].chunk;
			if (meshJobs [jobIndex].totalVisibleVoxels == 0) {
				if (chunk.mf.sharedMesh != null) {
					chunk.mf.sharedMesh.Clear (false);
				}
				chunk.mc.enabled = false;
				chunk.renderState = ChunkRenderState.RenderingComplete;
				return;
			}

			// Create mesh
			#if !UNITY_EDITOR
			Mesh mesh;
			if (isMobilePlatform) {
				mesh = new Mesh (); // on mobile will be released mesh data upon uploading to the GPU so the mesh is no longer readable; need to recreate it everytime the chunk is rendered
			} else {
				mesh = chunk.mf.sharedMesh;
				if (mesh == null) {
					mesh = new Mesh ();
				} else {
					mesh.Clear ();
				}
			}
			#else
			Mesh mesh = chunk.mf.sharedMesh;
			if (mesh == null) {
				mesh = new Mesh ();
				chunksDrawn++;
			} else {
				voxelsCreatedCount -= mesh.vertexCount;
				mesh.Clear ();
			}
			voxelsCreatedCount += meshJobs [jobIndex].totalVisibleVoxels;
			#endif

			// Vertices
			mesh.SetVertices (meshJobs [jobIndex].vertices);

			// UVs, normals, colors
			mesh.SetUVs (0, meshJobs [jobIndex].uv0);
			if (effectiveUseGeometryShaders) {
				mesh.SetUVs (1, meshJobs [jobIndex].uv2);
			} else {
				mesh.SetNormals (meshJobs [jobIndex].normals);
			}
			if (enableTinting) {
				mesh.SetColors (meshJobs [jobIndex].colors);
			}

			// Assign materials and submeshes
			int subMeshIndex = -1;
			mesh.subMeshCount = meshJobs [jobIndex].subMeshCount;
			if (mesh.subMeshCount > 0) {
				if (effectiveUseGeometryShaders) {
					if (meshJobs [jobIndex].opaqueVoxelsCount > 0) {
						mesh.SetIndices (meshJobs [jobIndex].opaqueIndicesArray, MeshTopology.Points, ++subMeshIndex, false);
					}
					if (meshJobs [jobIndex].cutoutVoxelsCount > 0) {
						mesh.SetIndices (meshJobs [jobIndex].cutoutIndicesArray, MeshTopology.Points, ++subMeshIndex, false);
					}
					if (meshJobs [jobIndex].waterVoxelsCount > 0) {
						mesh.SetIndices (meshJobs [jobIndex].waterIndicesArray, MeshTopology.Points, ++subMeshIndex, false);
					}
					if (meshJobs [jobIndex].cutxssVoxelsCount > 0) {
						mesh.SetIndices (meshJobs [jobIndex].cutxssIndicesArray, MeshTopology.Points, ++subMeshIndex, false);
					}
					if (meshJobs [jobIndex].opNoAOVoxelsCount > 0) {
						mesh.SetIndices (meshJobs [jobIndex].opNoAOIndicesArray, MeshTopology.Points, ++subMeshIndex, false);
					}
					if (meshJobs [jobIndex].transpVoxelsCount > 0) {
						mesh.SetIndices (meshJobs [jobIndex].transpIndicesArray, MeshTopology.Points, ++subMeshIndex, false);
					}
				} else {
					if (meshJobs [jobIndex].opaqueVoxelsCount > 0) {
						mesh.SetTriangles (meshJobs [jobIndex].opaqueIndices, ++subMeshIndex, false);
					}
					if (meshJobs [jobIndex].cutoutVoxelsCount > 0) {
						mesh.SetTriangles (meshJobs [jobIndex].cutoutIndices, ++subMeshIndex, false);
					}
					if (meshJobs [jobIndex].waterVoxelsCount > 0) {
						mesh.SetTriangles (meshJobs [jobIndex].waterIndices, ++subMeshIndex, false);
					}
					if (meshJobs [jobIndex].cutxssVoxelsCount > 0) {
						mesh.SetTriangles (meshJobs [jobIndex].cutxssIndices, ++subMeshIndex, false);
					}
					if (meshJobs [jobIndex].transpVoxelsCount > 0) {
						mesh.SetTriangles (meshJobs [jobIndex].transpIndices, ++subMeshIndex, false);
					}
				}
				chunk.mr.sharedMaterials = matTerrainArray [meshJobs [jobIndex].matVoxelIndex];

				mesh.bounds = Misc.bounds16;

				chunk.mf.sharedMesh = mesh;

				#if !UNITY_EDITOR
				if (isMobilePlatform) {
					mesh.UploadMeshData (true);
				}
				#endif

				if (!chunk.mr.enabled) {
					chunk.mr.enabled = true;
				}
			}

			// Update collider?
			if (enableColliders && chunk.needsColliderRebuild) {
				int colliderVerticesCount = meshJobs [jobIndex].colliderVertices.Count;
				Mesh colliderMesh = chunk.mc.sharedMesh;
				if (colliderVerticesCount == 0 || !applicationIsPlaying) {
					chunk.mc.enabled = false;
				} else {
					if (colliderMesh == null) {
						colliderMesh = new Mesh ();
					} else {
						colliderMesh.Clear ();
					}
					colliderMesh.SetVertices (meshJobs [jobIndex].colliderVertices);
					colliderMesh.SetTriangles (meshJobs [jobIndex].colliderIndices, 0);
					chunk.mc.sharedMesh = colliderMesh;
					chunk.mc.enabled = true;
				}

				// Update navmesh
				if (enableNavMesh) {
					int navMeshVerticesCount = meshJobs [jobIndex].navMeshVertices.Count;
					Mesh navMesh = chunk.navMesh;
					if (navMesh == null) {
						navMesh = new Mesh ();
					} else {
						navMesh.Clear ();
					}
					navMesh.SetVertices (meshJobs [jobIndex].navMeshVertices);
					navMesh.SetTriangles (meshJobs [jobIndex].navMeshIndices, 0);
					chunk.navMesh = navMesh;
					AddChunkNavMesh (chunk);
				}
			}

			RenderModelsInVoxels (chunk, meshJobs [jobIndex].mivs);

			if (chunk.renderState != ChunkRenderState.RenderingComplete) {
				chunk.renderState = ChunkRenderState.RenderingComplete;
				if (OnChunkAfterFirstRender != null) {
					OnChunkAfterFirstRender (chunk);
				}
			}

			if (OnChunkRender != null) {
				OnChunkRender (chunk);
			}

			shouldUpdateParticlesLighting = true;
		}


		void RenderModelsInVoxels (VoxelChunk chunk, FastList<ModelInVoxel> mivs) {

			instancingManager.ClearChunk (chunk);
			Quaternion rotation = Misc.quaternionZero;
			Vector3 position;

			for (int k = 0; k < mivs.count; k++) {
				ModelInVoxel miv = mivs.values [k];
				VoxelDefinition voxelDefinition = miv.vd;

				bool createGO = voxelDefinition.createGameObject || !voxelDefinition.gpuInstancing;

				if (createGO) {
					VoxelPlaceholder placeholder = GetVoxelPlaceholder (chunk, miv.voxelIndex, true);
					bool createModel = true;
					if (placeholder.modelInstance != null) {
						if (placeholder.modelTemplate != voxelDefinition.model) {
							DestroyImmediate (placeholder.modelInstance);
						} else {
							createModel = false;
						}
					}
					MeshFilter mf;
					Mesh mesh = null;

					if (createModel || placeholder.modelMeshFilter == null || placeholder.modelMeshFilter.sharedMesh == null || placeholder.modelMeshRenderer == null) {
						if (voxelDefinition.model == null)
							continue;
						placeholder.modelTemplate = voxelDefinition.model;
						placeholder.modelInstance = Instantiate (voxelDefinition.model);
						placeholder.modelInstance.name = "DynamicVoxelInstance";
						// Note: placeHolder.modelInstance layer must be different from layerVoxels to allow dynamic voxels collide with terrain. So don't set its layer to layer voxels
						placeholder.modelMeshRenderer = placeholder.modelInstance.GetComponent<MeshRenderer> ();
						if (voxelDefinition.gpuInstancing) {
							placeholder.modelMeshRenderer.enabled = false;
						} else {
							mf = placeholder.modelMeshFilter = placeholder.modelInstance.GetComponent<MeshFilter> ();
							mesh = mf.sharedMesh = Instantiate<Mesh> (mf.sharedMesh);
							mesh.hideFlags = HideFlags.DontSave;
						}
					} else {
						mf = placeholder.modelMeshFilter;
						mesh = mf.sharedMesh;
					}

					// Parent model to the placeholder
					Transform tModel = placeholder.modelInstance.transform;
					tModel.SetParent (placeholder.transform, false);
					tModel.transform.localPosition = Misc.vector3zero;
					tModel.transform.localScale = voxelDefinition.scale;

					if (voxelDefinition.gpuInstancing) {
						rotation = placeholder.transform.localRotation;
					} else {
						// Adjust lighting
						if (effectiveGlobalIllumination || chunk.voxels [miv.voxelIndex].isColored) {
							// Update mesh colors
							float voxelLight = chunk.voxels [miv.voxelIndex].lightMesh / 15f;
							Color32 color = chunk.voxels [miv.voxelIndex].color;
							color.r = (byte)(color.r * voxelLight);
							color.g = (byte)(color.g * voxelLight);
							color.b = (byte)(color.b * voxelLight);
							modelMeshColors.Clear ();
							for (int c = 0; c < mesh.vertexCount; c++) {
								modelMeshColors.Add (color);
							}
							mesh.SetColors (modelMeshColors);
							mesh.UploadMeshData (false);
						}
					}
					if (!tModel.gameObject.activeSelf) {
						tModel.gameObject.SetActive (true);
					}
					position = placeholder.transform.position;
				} else {
					// pure gpu instancing, no gameobject

					position = GetVoxelPosition (chunk, miv.voxelIndex); 

					rotation = voxelDefinition.GetRotation (position); // deterministic rotation
					// User rotation
					float rot = chunk.voxels [miv.voxelIndex].GetTextureRotationDegrees ();
					if (rot != 0) {
						rotation *= Quaternion.Euler (0, rot, 0);
					}

					// Custom position
					position = position + rotation * voxelDefinition.GetOffset(position);
				}

				if (voxelDefinition.gpuInstancing) {
					instancingManager.AddVoxel (chunk, miv.voxelIndex, position, rotation, voxelDefinition.scale);
				}

			}
		}

		#endregion
	
	}



}
