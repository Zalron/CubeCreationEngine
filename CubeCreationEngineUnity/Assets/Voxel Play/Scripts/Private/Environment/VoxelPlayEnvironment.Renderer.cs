//#define USES_SEE_THROUGH

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using VoxelPlay.GPURendering;
using VoxelPlay.GPURendering.Instancing;
using VoxelPlay.GPURendering.InstancingIndirect;
using VoxelPlay.GPULighting;

namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        public static bool supportsSeeThrough {
            get {
#if USES_SEE_THROUGH
				return true;
#else
                return false;
#endif
            }
        }


        struct RenderingMaterial
        {
            public Material material;
            public bool usesTextureArray;
        }


        public const int MESH_JOBS_TOTAL_POOL_SIZE_PC = 2000;
        public const int MESH_JOBS_TOTAL_POOL_SIZE_MOBILE = 128;
        public const int MAX_MATERIALS_PER_CHUNK = 16;

        /* cube coords
		
		7+------+6
		/.   3 /|
		2+------+ |
		|4.....|.+5
		|/     |/
		0+------+1
		
		*/

        public const int INDICES_BUFFER_OPAQUE = 0;
        public const int INDICES_BUFFER_CUTXSS = 1;
        public const int INDICES_BUFFER_CUTOUT = 2;
        public const int INDICES_BUFFER_WATER = 3;
        public const int INDICES_BUFFER_TRANSP = 4;
        public const int INDICES_BUFFER_OPNOAO = 5;

        // Opaque and cutout triangle are always loaded as well regardless of geometry shaders used. They're needed when a dynamic voxel is created since dynamic voxels do not use geometry shaders.
        // When "Use Geometry Shaders" is disabled, INDICES_BUFFER_OPAQUE_TRIANGLE equals to the index above. If geometry shaders are enabled, the index will be 6 and 7 respectively.
        int INDICES_BUFFER_OPAQUE_TRIANGLE = INDICES_BUFFER_OPAQUE;
        int INDICES_BUFFER_CUTOUT_TRIANGLE = INDICES_BUFFER_CUTOUT;


        // Unconclusive neighbours
        const byte CHUNK_TOP = 1;
        const byte CHUNK_BOTTOM = 2;
        const byte CHUNK_LEFT = 4;
        const byte CHUNK_RIGHT = 8;
        const byte CHUNK_BACK = 16;
        const byte CHUNK_FORWARD = 32;
        const byte CHUNK_IS_INCONCLUSIVE = 128;

        byte [] inconclusiveNeighbourTable = new byte [] {
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



        // Chunk Rendering
        [NonSerialized]
        public bool geometryShadersActive;
        bool effectiveMultithreadGeneration;

        [NonSerialized]
        public VirtualVoxel [] virtualChunk;

        [NonSerialized]
        public Voxel [] emptyChunkUnderground, emptyChunkAboveTerrain;

        // Materials
        RenderingMaterial [] renderingMaterials;
        Dictionary<int, Material []> materialsDict;
        List<Color32> modelMeshColors;
        Material matDynamicCutout, matDynamicOpaque;

        /// Each material has an index power of 2 which is combined with other materials to create a multi-material chunk mesh
        Dictionary<Material, int> materialIndices;
        int lastBufferIndex;

        // Multi-thread support
        MeshingThread [] meshingThreads;
        bool generationThreadsRunning;
        private readonly object seeThroughLock = new object ();

        // Instancing
        IGPUInstancingRenderer instancedRenderer;


        #region Renderer initialization

        void InitRenderer ()
        {

            draftModeActive = !applicationIsPlaying && editorRenderDetail == EditorRenderDetail.Draft;

#if UNITY_WEBGL
			geometryShadersActive = false;
#else
            geometryShadersActive = useGeometryShaders && !isMobilePlatform && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Metal;
#endif

            // Init materials
            matDynamicOpaque = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Dynamic Opaque"));
            matDynamicCutout = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Dynamic Cutout"));
            
            Material matTriangleOpaque = Instantiate<Material> (RenderType.Opaque.GetDefaultMaterial (this, false));
            Material matTriangleCutout = Instantiate<Material> (RenderType.Cutout.GetDefaultMaterial (this, false));
            Material matTerrainOpaque = geometryShadersActive ? Instantiate<Material> (RenderType.Opaque.GetDefaultMaterial (this)) : matTriangleOpaque;
            Material matTerrainCutout = geometryShadersActive ? Instantiate<Material> (RenderType.Cutout.GetDefaultMaterial (this)) : matTriangleCutout;
            Material matTerrainOpNoAO = Instantiate<Material> (RenderType.OpaqueNoAO.GetDefaultMaterial (this));
            Material matTerrainWater = Instantiate<Material> (RenderType.Water.GetDefaultMaterial (this));
            Material matTerrainCutxss = Instantiate<Material> (RenderType.CutoutCross.GetDefaultMaterial (this));
            Material matTerrainTransp = Instantiate<Material> (RenderType.Transp6tex.GetDefaultMaterial (this));

            // Init system arrays and structures
            if (materialsDict == null) {
                materialsDict = new Dictionary<int, Material []> ();
            } else {
                materialsDict.Clear ();
            }

            // Assign materials to rendering buffers
            renderingMaterials = new RenderingMaterial [MAX_MATERIALS_PER_CHUNK];
            renderingMaterials [INDICES_BUFFER_OPAQUE] = new RenderingMaterial { material = matTerrainOpaque, usesTextureArray = true };
            renderingMaterials [INDICES_BUFFER_CUTOUT] = new RenderingMaterial { material = matTerrainCutout, usesTextureArray = true };
            renderingMaterials [INDICES_BUFFER_CUTXSS] = new RenderingMaterial { material = matTerrainCutxss, usesTextureArray = true };
            renderingMaterials [INDICES_BUFFER_WATER] = new RenderingMaterial { material = matTerrainWater, usesTextureArray = true };
            renderingMaterials [INDICES_BUFFER_TRANSP] = new RenderingMaterial { material = matTerrainTransp, usesTextureArray = true };
            renderingMaterials [INDICES_BUFFER_OPNOAO] = new RenderingMaterial { material = matTerrainOpNoAO, usesTextureArray = true };

            if (materialIndices == null) {
                materialIndices = new Dictionary<Material, int> ();
            } else {
                materialIndices.Clear ();
            }
            materialIndices [matTerrainOpaque] = INDICES_BUFFER_OPAQUE;
            materialIndices [matTerrainCutout] = INDICES_BUFFER_CUTOUT;
            materialIndices [matTerrainCutxss] = INDICES_BUFFER_CUTXSS;
            materialIndices [matTerrainWater] = INDICES_BUFFER_WATER;
            materialIndices [matTerrainTransp] = INDICES_BUFFER_TRANSP;
            materialIndices [matTerrainOpNoAO] = INDICES_BUFFER_OPNOAO;

            // Triangle opaque and cutout are always loaded because dynamic voxels requires them
            if (useGeometryShaders) {
                INDICES_BUFFER_OPAQUE_TRIANGLE = 6;
                INDICES_BUFFER_CUTOUT_TRIANGLE = 7;
                lastBufferIndex = 7;
            } else {
                INDICES_BUFFER_OPAQUE_TRIANGLE = INDICES_BUFFER_OPAQUE;
                INDICES_BUFFER_CUTOUT_TRIANGLE = INDICES_BUFFER_CUTOUT;
                lastBufferIndex = 5;
            }
            renderingMaterials [INDICES_BUFFER_OPAQUE_TRIANGLE] = new RenderingMaterial { material = matTriangleOpaque, usesTextureArray = true };
            renderingMaterials [INDICES_BUFFER_CUTOUT_TRIANGLE] = new RenderingMaterial { material = matTriangleCutout, usesTextureArray = true };


            modelMeshColors = new List<Color32> (128);

            InitTempVertices ();
            InitSeeThrough ();
            InitMeshingThreads ();

            Voxel.Empty.light = noLightValue;

            if (delayedVoxelCustomRotations == null) {
                delayedVoxelCustomRotations = new Dictionary<Vector3, Vector3> ();
            } else {
                delayedVoxelCustomRotations.Clear ();
            }

            if (useComputeBuffers) {
                instancedRenderer = new GPUInstancingIndirectRenderer (this);
            } else {
                instancedRenderer = new GPUInstancingRenderer (this);
            }

            VoxelPlayLightManager lightManager = currentCamera.GetComponent<VoxelPlayLightManager> ();
            if (lightManager == null) {
                currentCamera.gameObject.AddComponent<VoxelPlayLightManager> ();
            } else {
                lightManager.enabled = true;
            }

            if (realisticWater) {
                currentCamera.depthTextureMode |= DepthTextureMode.Depth;
                currentCamera.forceIntoRenderTexture = true;
            }

            StartGenerationThreads ();

        }

        void InitMeshingThreads ()
        {
            InitVirtualChunk ();
            int maxThreads = effectiveMultithreadGeneration ? SystemInfo.processorCount - 1 : 1;
            if (maxThreads < 1) maxThreads = 1;
            meshingThreads = new MeshingThread [maxThreads];
            bool geoMeshing = geometryShadersActive && !isMobilePlatform;
            int poolSize;
            if (geoMeshing) {
                poolSize = MESH_JOBS_TOTAL_POOL_SIZE_PC / maxThreads;
            } else {
                poolSize = MESH_JOBS_TOTAL_POOL_SIZE_MOBILE / maxThreads;
            }
            for (int k = 0; k < meshingThreads.Length; k++) {
                if (geoMeshing) {
                    // Geometry shader-based meshing
                    meshingThreads [k] = new MeshingThreadGeom ();
                } else {
                    // Classic triangle-baed meshing
                    meshingThreads [k] = new MeshingThreadTriangle ();
                }
                meshingThreads [k].Init (k, poolSize, this);
            }
        }

        void StartGenerationThreads ()
        {
            if (effectiveMultithreadGeneration) {
                generationThreadsRunning = true;
                for (int k = 0; k < meshingThreads.Length; k++) {
                    MeshingThread thread = meshingThreads [k];
                    thread.waitEvent = new AutoResetEvent (false);
                    thread.meshGenerationThread = new Thread (() => GenerateChunkMeshDataInBackgroundThread (thread));
                    thread.meshGenerationThread.Start ();
                }
            }
        }

        void StopGenerationThreads ()
        {
            generationThreadsRunning = false;
            if (meshingThreads == null) return;
            for (int k = 0; k < meshingThreads.Length; k++) {
                MeshingThread meshingThread = meshingThreads [k];
                if (meshingThread != null && meshingThread.meshGenerationThread != null) {
                    meshingThread.waitEvent.Set ();
                }
            }
            for (int t = 0; t < meshingThreads.Length; t++) {
                MeshingThread meshingThread = meshingThreads [t];
                if (meshingThread != null && meshingThread.meshGenerationThread != null) {
                    for (int k = 0; k < 100; k++) {
                        bool wait = false;
                        if (meshingThread.meshGenerationThread.IsAlive)
                            wait = true;
                        if (!wait)
                            break;
                        Thread.Sleep (10);
                    }
                }
            }
        }


        void InitVirtualChunk ()
        {
            emptyChunkUnderground = new Voxel [16 * 16 * 16];
            emptyChunkAboveTerrain = new Voxel [16 * 16 * 16];
            for (int k = 0; k < emptyChunkAboveTerrain.Length; k++) {
                emptyChunkAboveTerrain [k].light = (byte)15;
                emptyChunkUnderground [k].hasContent = 1;
                emptyChunkUnderground [k].opaque = FULL_OPAQUE;
            }

            virtualChunk = new VirtualVoxel [18 * 18 * 18];

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

        #endregion


        #region Rendering


        public void UpdateMaterialProperties ()
        {

            NotifyCameraMove();
            if (renderingMaterials == null || renderingMaterials.Length == 0)
                return;

            ToggleMaterialKeyword (SKW_VOXELPLAY_USE_AO, !draftModeActive && enableSmoothLighting);
            ToggleMaterialKeyword (SKW_VOXELPLAY_TRANSP_BLING, transparentBling);
            ToggleMaterialKeyword (SKW_VOXELPLAY_AA_TEXELS, hqFiltering && !enableReliefMapping);

            if (enableFogSkyBlending && !draftModeActive) {
                Shader.EnableKeyword (SKW_VOXELPLAY_GLOBAL_USE_FOG);
            } else {
                Shader.DisableKeyword (SKW_VOXELPLAY_GLOBAL_USE_FOG);
            }

            for (int k = 0; k < renderingMaterials.Length; k++) {
                Material mat = renderingMaterials [k].material;
                if (mat != null) {
                    UpdateOutlinePropertiesMat (mat);
                    UpdateNormalMapPropertiesMat (mat);
                    UpdateParallaxPropertiesMat (mat);
                    UpdatePixelLightsPropertiesMat (mat);
                }
            }

            UpdateOutlinePropertiesMat (matDynamicOpaque);
            UpdateNormalMapPropertiesMat (matDynamicOpaque);
            UpdateParallaxPropertiesMat (matDynamicOpaque);
            UpdatePixelLightsPropertiesMat (matDynamicOpaque);
            UpdateOutlinePropertiesMat (matDynamicCutout);
            UpdateNormalMapPropertiesMat (matDynamicCutout);
            UpdateParallaxPropertiesMat (matDynamicCutout);
            UpdatePixelLightsPropertiesMat (matDynamicCutout);

            UpdateRealisticWaterMat ();
            UpdateAmbientProperties ();

            if (OnSettingsChanged != null) {
                OnSettingsChanged ();
            }
        }

        void ToggleMaterialKeyword (string keyword, bool enabled)
        {
            if (renderingMaterials == null)
                return;

            for (int k = 0; k < renderingMaterials.Length; k++) {
                Material mat = renderingMaterials [k].material;
                if (mat != null) {
                    if (enabled && !mat.IsKeywordEnabled (keyword)) {
                        mat.EnableKeyword (keyword);
                    } else if (!enabled && mat.IsKeywordEnabled (keyword)) {
                        mat.DisableKeyword (keyword);
                    }
                }
            }
        }

        void UpdateAmbientProperties ()
        {

            if (world == null)
                return;

            if (cameraMain != null) {
                if (adjustCameraFarClip && distanceAnchor == cameraMain.transform) {
                    cameraMain.farClipPlane = visibleChunksDistance * 16f;
                }
                float thisFogDistance = fogUseCameraFarClip ? cameraMain.farClipPlane : fogDistance;
                float thisFogStart = thisFogDistance * fogFallOff;
                Vector3 fogData = new Vector3 (thisFogStart * thisFogStart, thisFogDistance * thisFogDistance - thisFogStart * thisFogStart, 0);
                Shader.SetGlobalVector ("_VPFogData", fogData);
            }

            // Global sky & global uniforms
            Shader.SetGlobalColor ("_VPSkyTint", world.skyTint);
            Shader.SetGlobalFloat ("_VPFogAmount", fogAmount);
            Shader.SetGlobalFloat ("_VPExposure", world.exposure);
            Shader.SetGlobalFloat ("_VPAmbientLight", ambientLight);
            Shader.SetGlobalFloat ("_VPDaylightShadowAtten", daylightShadowAtten);
            Shader.SetGlobalFloat ("_VPGrassWindSpeed", world.grassWindSpeed * 0.01f);
            Shader.SetGlobalFloat ("_VPTreeWindSpeed", world.treeWindSpeed * 0.005f);

            // Update skybox material
            VoxelPlaySkybox worldSkybox = isMobilePlatform ? world.skyboxMobile : world.skyboxDesktop;

            if (worldSkybox != VoxelPlaySkybox.UserDefined) {
                if (skyboxMaterial != RenderSettings.skybox || RenderSettings.skybox == null) {
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

        void UpdateRealisticWaterMat ()
        {
            // Update realistic water properties
            if (realisticWater) {
                Material waterMat = renderingMaterials [INDICES_BUFFER_WATER].material;
                if (waterMat != null) {
                    waterMat.SetColor ("_WaterColor", world.waterColor);
                    waterMat.SetColor ("_UnderWaterFogColor", world.underWaterFogColor);

                    waterMat.SetColor ("_FoamColor", world.foamColor);
                    waterMat.SetFloat ("_WaveScale", world.waveScale * world.waveAmplitude);
                    waterMat.SetFloat ("_WaveSpeed", world.waveSpeed * world.waveAmplitude);
                    waterMat.SetFloat ("_WaveAmplitude", world.waveAmplitude);
                    waterMat.SetFloat ("_SpecularIntensity", world.specularIntensity);
                    waterMat.SetFloat ("_SpecularPower", world.specularPower);
                    waterMat.SetFloat ("_RefractionDistortion", world.refractionDistortion * world.waveAmplitude);
                    waterMat.SetFloat ("_Fresnel", 1f - world.fresnel);
                    waterMat.SetFloat ("_NormalStrength", world.normalStrength * world.waveAmplitude);
                    waterMat.SetVector ("_OceanWave", new Vector3 (world.oceanWaveThreshold, world.oceanWaveIntensity, 0));
                }
            }
        }

        void UpdateOutlinePropertiesMat (Material mat)
        {
            if (enableOutline) {
                mat.EnableKeyword (SKW_VOXELPLAY_USE_OUTLINE);
                mat.SetColor ("_OutlineColor", outlineColor);
                mat.SetFloat ("_OutlineThreshold", hqFiltering ? outlineThreshold * 10f : outlineThreshold);
            } else {
                mat.DisableKeyword (SKW_VOXELPLAY_USE_OUTLINE);
            }
        }

        void UpdateParallaxPropertiesMat (Material mat)
        {
            if (enableReliefMapping) {
                mat.EnableKeyword (SKW_VOXELPLAY_USE_PARALLAX);
                mat.SetFloat ("_VPParallaxStrength", reliefStrength);
                mat.SetInt ("_VPParallaxIterations", reliefIterations);
                mat.SetInt ("_VPParallaxIterationsBinarySearch", reliefIterationsBinarySearch);
            } else {
                mat.DisableKeyword (SKW_VOXELPLAY_USE_PARALLAX);
            }
        }

        void UpdateNormalMapPropertiesMat (Material mat)
        {
            if (enableNormalMap) {
                mat.EnableKeyword (SKW_VOXELPLAY_USE_NORMAL);
            } else {
                mat.DisableKeyword (SKW_VOXELPLAY_USE_NORMAL);
            }
        }

        void UpdatePixelLightsPropertiesMat (Material mat)
        {
            if (usePixelLights) {
                mat.EnableKeyword (SKW_VOXELPLAY_USE_PIXEL_LIGHTS);
            } else {
                mat.DisableKeyword (SKW_VOXELPLAY_USE_PIXEL_LIGHTS);
            }
        }


        bool CreateChunkMeshJob (VoxelChunk chunk)
        {
            int threadId = chunk.poolIndex % meshingThreads.Length;
            return meshingThreads [threadId].CreateChunkMeshJob (chunk, generationThreadsRunning);
        }

        void GenerateChunkMeshDataInBackgroundThread (MeshingThread thread)
        {
            try {
                while (generationThreadsRunning) {
                    bool idle;
                    lock (thread.indicesUpdating) {
                        idle = thread.meshJobMeshDataGenerationIndex == thread.meshJobMeshLastIndex;
                    }
                    if (idle) {
                        thread.waitEvent.WaitOne ();
                        continue;
                    }
                    GenerateChunkMeshDataOneJob (thread);
                    lock (thread.indicesUpdating) {
                        thread.meshJobMeshDataGenerationReadyIndex = thread.meshJobMeshDataGenerationIndex;
                    }
                }
            } catch (Exception ex) {
                ShowExceptionMessage (ex);
            }
        }



        void GenerateChunkMeshDataInMainThread (long endTime)
        {

            long elapsed;
            MeshingThread thread = meshingThreads [0];
            do {
                if (thread.meshJobMeshDataGenerationIndex == thread.meshJobMeshLastIndex)
                    return;
                GenerateChunkMeshDataOneJob (thread);
                thread.meshJobMeshDataGenerationReadyIndex = thread.meshJobMeshDataGenerationIndex;
                elapsed = stopWatch.ElapsedMilliseconds;
            } while (elapsed < endTime);
        }


        void GenerateChunkMeshDataOneJob (MeshingThread thread)
        {
            thread.meshJobMeshDataGenerationIndex++;
            if (thread.meshJobMeshDataGenerationIndex >= thread.meshJobs.Length) {
                thread.meshJobMeshDataGenerationIndex = 0;
            }

            VoxelChunk chunk = thread.meshJobs [thread.meshJobMeshDataGenerationIndex].chunk;
            Voxel [] [] chunk9 = thread.chunk9;
            chunk9 [13] = chunk.voxels;
            Voxel [] emptyChunk = chunk.isAboveSurface ? emptyChunkAboveTerrain : emptyChunkUnderground;
            int chunkX, chunkY, chunkZ;
            FastMath.FloorToInt (chunk.position.x / 16f, chunk.position.y / 16f, chunk.position.z / 16f, out chunkX, out chunkY, out chunkZ);

            // Reset bit field; inconclusive neighbours are those neighbours which are undefined when an adjacent chunk is rendered. We mark it so then it's finally rendered, we re-render the adjacent chunk. This is required if the new chunk can 
            // break holes on the edge of the chunk while no lighting is entering the chunk or global illumination is disabled (yes, it's an edge case but must be addressed to avoid gaps in those cases).
            chunk.inconclusiveNeighbours = 0; //(byte)(chunk.inconclusiveNeighbours & ~CHUNK_IS_INCONCLUSIVE);

            VoxelChunk [] neighbourChunks = thread.neighbourChunks;
            neighbourChunks [13] = chunk;
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
                        neighbourChunks [c] = neighbour;
                    }
                }
            }
#if USES_SEE_THROUGH
			lock (seeThroughLock) {

				// Hide voxels marked as hidden
				for (int c = 0; c < neighbourChunks.Length; c++) {
					ToggleHiddenVoxels (neighbourChunks [c], false);	
				}
#endif

            thread.GenerateMeshData ();

#if USES_SEE_THROUGH
				// Reactivate hidden voxels
				for (int c = 0; c < neighbourChunks.Length; c++) {
					ToggleHiddenVoxels (neighbourChunks [c], true);	
				}
			}
#endif
        }

        void UploadMeshData (MeshingThread thread, int jobIndex)
        {
            MeshJobData [] meshJobs = thread.meshJobs;
            VoxelChunk chunk = meshJobs [jobIndex].chunk;

            // Update collider?
            if (enableColliders && meshJobs [jobIndex].needsColliderRebuild) {
                int colliderVerticesCount = meshJobs [jobIndex].colliderVertices.Count;
                Mesh colliderMesh = chunk.mc.sharedMesh;
#if UNITY_EDITOR
                if (!applicationIsPlaying && editorRenderDetail != EditorRenderDetail.StandardPlusColliders) {
                    colliderVerticesCount = 0;
                }
#endif
                if (colliderVerticesCount == 0) {
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

            // Update mesh?
            if (meshJobs [jobIndex].totalVoxels == 0) {
                if (chunk.mf.sharedMesh != null) {
                    chunk.mf.sharedMesh.Clear (false);
                }
                chunk.renderState = ChunkRenderState.RenderingComplete;
                return;
            }

            // Create mesh
            Mesh mesh = chunk.mf.sharedMesh;
#if !UNITY_EDITOR
			if (isMobilePlatform) {
                if (mesh != null) {
                    DestroyImmediate(mesh);
                }
				mesh = new Mesh (); // on mobile will be released mesh data upon uploading to the GPU so the mesh is no longer readable; need to recreate it everytime the chunk is rendered
			} else {
				if (mesh == null) {
					mesh = new Mesh ();
				} else {
					mesh.Clear ();
				}
			}
#else
            if (mesh == null) {
                mesh = new Mesh ();
                chunksDrawn++;
            } else {
                voxelsCreatedCount -= chunk.totalVisibleVoxelsCount;
                mesh.Clear ();
            }
            chunk.totalVisibleVoxelsCount = meshJobs [jobIndex].totalVoxels;
            voxelsCreatedCount += chunk.totalVisibleVoxelsCount;
#endif

            // Assign materials and submeshes
            mesh.subMeshCount = meshJobs [jobIndex].subMeshCount;
            if (mesh.subMeshCount > 0) {

                // Vertices
                mesh.SetVertices (meshJobs [jobIndex].vertices);

                // UVs, normals, colors
                mesh.SetUVs (0, meshJobs [jobIndex].uv0);
                if (geometryShadersActive) {
                    mesh.SetUVs (1, meshJobs [jobIndex].uv2);
                } else {
                    mesh.SetNormals (meshJobs [jobIndex].normals);
                }
                if (enableTinting) {
                    mesh.SetColors (meshJobs [jobIndex].colors);
                }

                int subMeshIndex = -1;
                int matIndex = 0;

                if (geometryShadersActive) {
                    for (int k = 0; k < MAX_MATERIALS_PER_CHUNK; k++) {
                        if (meshJobs [jobIndex].buffers [k].indicesCount > 0) {
                            subMeshIndex++;
                            mesh.SetIndices (meshJobs [jobIndex].buffers [k].indicesArray, MeshTopology.Points, subMeshIndex, false);
                            matIndex += 1 << k;
                        }
                    }
                } else {
                    for (int k = 0; k < MAX_MATERIALS_PER_CHUNK; k++) {
                        if (meshJobs [jobIndex].buffers [k].indicesCount > 0) {
                            subMeshIndex++;
                            mesh.SetTriangles (meshJobs [jobIndex].buffers [k].indices, subMeshIndex, false);
                            matIndex += 1 << k;
                        }
                    }
                }

                // Compute material array
                Material [] matArray;
                if (!materialsDict.TryGetValue (matIndex, out matArray)) {
                    matArray = new Material [mesh.subMeshCount];
                    for (int k = 0, j = 0; k < MAX_MATERIALS_PER_CHUNK; k++) {
                        if (meshJobs [jobIndex].buffers [k].indicesCount > 0) {
                            matArray [j++] = renderingMaterials [k].material;
                        }
                    }
                    materialsDict [matIndex] = matArray;
                }
                chunk.mr.sharedMaterials = matArray;

                if (chunk.isCloud) {
                    mesh.bounds = new Bounds (Misc.vector3zero, new Vector4 (64, 32, 64));
                } else if (enableCurvature) {
                    mesh.bounds = Misc.bounds16Stretched;
                } else {
                    mesh.bounds = Misc.bounds16;
                }

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


        void RenderModelsInVoxels (VoxelChunk chunk, FastList<ModelInVoxel> mivs)
        {

            instancedRenderer.ClearChunk (chunk);

            // deactivate all models in this chunk
            // we need to iterate the placeholders list entirely to address the case when the voxel is not using GPU instancing. In this case the gameobject renderer needs to be disabled 
            // and we need to do this way because mivs won't contain the custom voxel since it may be termporarily converted to a transparent voxels due to see-through effect
            if (chunk.placeholders != null) {
                int count = chunk.placeholders.Count;
                for (int k = 0; k < count; k++) {
                    if (chunk.placeholders.entries [k].key >= 0) {
                        VoxelPlaceholder placeHolder = chunk.placeholders.entries [k].value;
                        if (placeHolder != null && placeHolder.modelMeshRenderer != null) {
                            placeHolder.modelMeshRenderer.enabled = false;
                        }
                    }
                }
            }

            Quaternion rotation = Misc.quaternionZero;
            Vector3 position;

            for (int k = 0; k < mivs.count; k++) {
                ModelInVoxel miv = mivs.values [k];
                VoxelDefinition voxelDefinition = miv.vd;

                bool createGO = voxelDefinition.createGameObject || !voxelDefinition.gpuInstancing;

                if (VoxelIsHidden (chunk, miv.voxelIndex)) {
                    continue;
                }

                if (createGO) {
                    VoxelPlaceholder placeholder = GetVoxelPlaceholder (chunk, miv.voxelIndex, true);
                    bool createModel = true;
                    if (placeholder.modelInstance != null) {
                        if (placeholder.modelTemplate != voxelDefinition.prefab) {
                            DestroyImmediate (placeholder.modelInstance);
                            placeholder.originalMeshColors32 = null;
                            placeholder.lastMivTintColor = Misc.color32White;
                        } else {
                            createModel = false;
                        }
                    }

                    if (createModel || placeholder.modelMeshFilter == null || placeholder.modelMeshFilter.sharedMesh == null || placeholder.modelMeshRenderer == null) {
                        if (voxelDefinition.prefab == null)
                            continue;
                        placeholder.modelTemplate = voxelDefinition.prefab;
                        placeholder.modelInstance = Instantiate (voxelDefinition.prefab);
                        placeholder.modelInstance.name = "DynamicVoxelInstance";
                        // Note: placeHolder.modelInstance layer must be different from layerVoxels to allow dynamic voxels collide with terrain. So don't set its layer to layer voxels
                        placeholder.modelMeshRenderer = placeholder.modelInstance.GetComponentInChildren<MeshRenderer> ();
                        if (voxelDefinition.gpuInstancing) {
                            if (placeholder.modelMeshRenderer != null) {
                                placeholder.modelMeshRenderer.enabled = false;
                            }
                        } else {
                            placeholder.modelMeshFilter = placeholder.modelInstance.GetComponentInChildren<MeshFilter> ();
                        }

                        // Parent model to the placeholder
                        Transform tModel = placeholder.modelInstance.transform;
                        tModel.SetParent (placeholder.transform, false);
                        tModel.transform.localPosition = Misc.vector3zero;
                        tModel.transform.localScale = voxelDefinition.scale;

                    } else {
                        placeholder.modelMeshRenderer.enabled = true;
                    }

                    if (voxelDefinition.gpuInstancing) {
                        rotation = placeholder.transform.localRotation;
                    } else {
                        // Adjust lighting
                        if (effectiveGlobalIllumination || chunk.voxels [miv.voxelIndex].isColored) {
                            // Update mesh colors
                            MeshFilter mf = placeholder.modelMeshFilter;
                            if (mf != null) {
                                Mesh mesh = mf.sharedMesh;
                                if (mesh != null) {
                                    float voxelLight = chunk.voxels [miv.voxelIndex].lightMesh / 15f;
                                    Color32 tintColor = chunk.voxels [miv.voxelIndex].color;
                                    tintColor.r = (byte)(tintColor.r * voxelLight);
                                    tintColor.g = (byte)(tintColor.g * voxelLight);
                                    tintColor.b = (byte)(tintColor.b * voxelLight);
                                    if (placeholder.lastMivTintColor.r != tintColor.r || placeholder.lastMivTintColor.g != tintColor.g || placeholder.lastMivTintColor.b != tintColor.b) {
                                        Color32 [] colors32 = placeholder.originalMeshColors32;
                                        if (colors32 == null) {
                                            colors32 = mesh.colors32;
                                            if (colors32 == null) {
                                                colors32 = new Color32 [0]; // no color info
                                            }
                                            placeholder.originalMeshColors32 = colors32;
                                            mesh = Instantiate<Mesh> (mesh);
                                            mesh.hideFlags = HideFlags.DontSave;
                                            mf.sharedMesh = mesh;
                                        }
                                        modelMeshColors.Clear ();
                                        int vertexCount = mesh.vertexCount;
                                        if (colors32.Length == 0) {
                                            for (int c = 0; c < vertexCount; c++) {
                                                modelMeshColors.Add (tintColor);
                                            }
                                        } else {
                                            for (int c = 0; c < vertexCount; c++) {
                                                Color32 color = tintColor.MultiplyRGB (colors32 [c]);
                                                modelMeshColors.Add (color);
                                            }
                                        }
                                        mesh.SetColors (modelMeshColors);
                                        mesh.UploadMeshData (false);
                                        placeholder.lastMivTintColor = tintColor;
                                    }
                                }
                            }
                        }
                    }
                    if (!placeholder.modelInstance.gameObject.activeSelf) {
                        placeholder.modelInstance.gameObject.SetActive (true);
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
                    position += rotation * voxelDefinition.GetOffset (position);
                }

                if (voxelDefinition.gpuInstancing) {
                    instancedRenderer.AddVoxel (chunk, miv.voxelIndex, position, rotation, voxelDefinition.scale);
                }

            }
        }

        #endregion

    }



}
