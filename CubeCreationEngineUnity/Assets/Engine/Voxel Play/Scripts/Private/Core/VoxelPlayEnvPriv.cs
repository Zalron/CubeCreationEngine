using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxelPlay {

	public delegate void VoxelHitEvent (VoxelChunk chunk, int voxelIndex, ref int damage);
	public delegate void VoxelEvent (VoxelChunk chunk, int voxelIndex);
	public delegate void VoxelPlaceEvent (Vector3 position, VoxelChunk chunk, int voxelIndex, ref VoxelDefinition voxelDefinition, ref Color32 tintColor);
	public delegate void VoxelDropItemEvent (VoxelChunk chunk, int voxelIndex, out bool canBeCollected);
	public delegate void VoxelClickEvent (VoxelChunk chunk, int voxelIndex, int buttonIndex);
	public delegate void VoxelChunkBeforeCreationEvent (Vector3 chunkCenter, out bool overrideDefaultContents, Voxel[] voxels, out bool isAboveSurface);
	public delegate void VoxelChunkEvent (VoxelChunk chunk);
	public delegate void VoxelChunkUnloadEvent (VoxelChunk chunk, ref bool canUnload);
	public delegate void VoxelTorchEvent (VoxelChunk chunk, LightSource lightSource);
	public delegate void VoxelPlayEvent ();
	public delegate void VoxelLightRefreshEvent ();
	public delegate void RepaintActionEvent ();
	public delegate void VoxelPlayModelBuildEvent (ModelDefinition model, Vector3 position);

	[ExecuteInEditMode]
	public partial class VoxelPlayEnvironment : MonoBehaviour {

		public delegate void AfterInitCallback ();

		[NonSerialized]
		public bool initialized, applicationIsPlaying;

		#if UNITY_EDITOR
		public event RepaintActionEvent WantRepaintInspector;

		long lastCameraMoveTime, lastInspectorUpdateTime;
		#endif

		const string VOXELPLAY_WORLD_ROOT = "Voxel Play World";

		const string SKW_VOXELPLAY_USE_AO = "VOXELPLAY_USE_AO";
		const string SKW_VOXELPLAY_USE_TINTING = "VOXELPLAY_USE_TINTING";
		const string SKW_VOXELPLAY_USE_OUTLINE = "VOXELPLAY_USE_OUTLINE";
		const string SKW_VOXELPLAY_USE_PARALLAX = "VOXELPLAY_USE_PARALLAX";
		const string SKW_VOXELPLAY_GLOBAL_USE_FOG = "VOXELPLAY_GLOBAL_USE_FOG";
		const string SKW_VOXELPLAY_AA_TEXELS = "VOXELPLAY_USE_AA";
		const string SKW_VOXELPLAY_USE_NORMAL = "VOXELPLAY_USE_NORMAL";
		const string SKW_VOXELPLAY_USE_PIXEL_LIGHTS = "VOXELPLAY_PIXEL_LIGHTS";
		const string SKW_VOXELPLAY_TRANSP_BLING = "VOXELPLAY_TRANSP_BLING";

		[NonSerialized]
		public System.Diagnostics.Stopwatch stopWatch;
		Vector3 lastCamPos, currentCamPos;
		Quaternion lastCamRot, currentCamRot;
		bool _cameraHasMoved, shouldCheckChunksInFrustum;
		Material skyboxEarth, skyboxEarthSimplified, skyboxSpace, skyboxEarthNightCube, skyboxEarthDayNightCube, skyboxMaterial;
		Camera sceneCam;
		Collider characterControllerCollider;
		float lastTimeOfDay, lastAzimuth;
		Material modelHighlightMat;

		/// <summary>
		/// The transform of the world root where all objects created by Voxel Play are placed
		/// </summary>
		[NonSerialized]
		public Transform worldRoot;

		/// <summary>
		/// Stores the last message send to ShowMessage() method
		/// </summary>
		[NonSerialized]
		public string lastMessage = "";

		[NonSerialized]
		public bool isMobilePlatform;

		[NonSerialized]
		internal bool draftModeActive;

		[NonSerialized] public int STAGE = 0;

		List<VoxelChunk> tempChunks;
		Collider[] tempColliders;

		int[] neighbourOffsets = new int[] { 
			0, 1, 0, 
			1, 0, 0,
			-1, 0, 0,
			0, 0, 1,
			0, 0, -1,
			0, -1, 0
		};

		#region Gameloop events

		void OnEnable () {
			if (!initialized) {
				#if UNITY_EDITOR
				CheckTintingScriptingSupport ();
				#endif
				BootEngine ();
			}
		}

		void Update () {
			if (initialized) {
				input.Update ();
			}
		}


		void LateUpdate () {
			if (applicationIsPlaying && initialized) {
				DoWork ();
				ProcessThreadMessages ();
			}
		}

		void OnDisable () {
			StopGenerationThread ();
		}

		void OnDestroy () {
			#if UNITY_EDITOR
			EditorApplication.update -= UpdateInEditor;
			#endif
			DisposeAll ();
		}

		#endregion


		#region Initialization and disposal

		void BootEngine () {
			Init (MayLoadGame);
		}

		void MayLoadGame () {
			if (applicationIsPlaying || (!applicationIsPlaying && renderInEditor)) {
				if (loadSavedGame)
					LoadGameBinary (true);
			}
		}


		public void Init (AfterInitCallback callback = null) {
			initialized = false;
			sceneCam = null;
			applicationIsPlaying = Application.isPlaying;
			tempChunks = new List<VoxelChunk> ();
			tempColliders = new Collider[1];
			InitMainThreading ();

#if UNITY_ANDROID || UNITY_IOS
			isMobilePlatform = true;
#elif UNITY_WEBGL
			isMobilePlatform = false;
			#if UNITY_EDITOR
			if (PlayerSettings.WebGL.memorySize<2000) PlayerSettings.WebGL.memorySize = 2000;
			#endif
#else
			isMobilePlatform = Application.isMobilePlatform;
			#endif

#if UNITY_WEBGL
            effectiveMultithreadGeneration = false;
#else
			effectiveMultithreadGeneration = multiThreadGeneration;
#endif

			// Init camera and Sun references
			if (cameraMain == null) {
				cameraMain = Camera.main;
				if (cameraMain == null) {
					cameraMain = FindObjectOfType<Camera> ();
				}
				if (cameraMain == null) {
					Debug.LogError ("Voxel Play: No camera found!");
					return;
				}
			}

			// Cache player collider
			if (characterController == null) {
				characterController = FindObjectOfType<VoxelPlayCharacterControllerBase> ();
			}
			if (characterController != null) {
				characterControllerCollider = characterController.GetComponent<CharacterController> ();
				if (characterControllerCollider == null) {
					characterControllerCollider = characterController.GetComponent<Collider> ();
				}
			}

			#if UNITY_EDITOR
			if (cameraMain.actualRenderingPath != RenderingPath.Forward) {
				Debug.LogWarning ("Voxel Play works better with Forward Rendering path.");
			}
			if (!isMobilePlatform && QualitySettings.antiAliasing < 2) {
				Debug.LogWarning ("Voxel Play looks better with MSAA enabled (x2 minimum to enable crosshair).");
			}
			#endif

			if (isMobilePlatform && Application.isPlaying) {
				cameraMain.farClipPlane = Mathf.Min (400, _visibleChunksDistance * 16);
			}

			if (UICanvasPrefab == null) {
				UICanvasPrefab = Resources.Load<GameObject> ("VoxelPlay/UI/Voxel Play UI Canvas");
			}
			if (crosshairPrefab == null) {
				crosshairPrefab = Resources.Load<GameObject> ("VoxelPlay/UI/crosshair");
			}
			if (crosshairTexture == null) {
				crosshairTexture = Resources.Load<Texture2D> ("VoxelPlay/UI/crosshairTexture");
			}

			#if UNITY_EDITOR
			input = new KeyboardMouseController ();
			#else
			if (isMobilePlatform) {
			input = new DualTouchController ();
			} else {
			input = new KeyboardMouseController ();
			}
			#endif

			stopWatch = new System.Diagnostics.Stopwatch ();

			#if UNITY_EDITOR
			lastInspectorUpdateTime = 0;
			lastCameraMoveTime = 0;
			#endif

			if (!enableBuildMode)
				buildMode = false;

			if (applicationIsPlaying || (!applicationIsPlaying && renderInEditor)) {
				stopWatch.Start ();
				if (cachedChunks == null || chunksPool == null) {
					LoadWorldInt ();
					if (applicationIsPlaying) {
						StartCoroutine (WarmChunks (callback));
					} else {
						WarmChunksEditor (callback);
					}
				}
			} else {
				UpdateAmbientProperties ();
			}

		}

		IEnumerator WarmChunks (AfterInitCallback callback) {
			WaitForEndOfFrame w = new WaitForEndOfFrame ();
			int required = maxChunks;
#if UNITY_EDITOR
			required = prewarmChunksInEditor;
#endif

			if (world != null) {
				while (chunksPoolLoadIndex < maxChunks) {
					try {
						for (int k = 0; k < 100; k++) {
							ReserveChunkMemory ();
						}
					} catch (Exception ex) {
						ShowExceptionMessage (ex);
						break;
					}
					if (enableLoadingPanel) {
						float progress = (float)(chunksPoolLoadIndex + 1) / required;
						VoxelPlayUI.instance.ToggleInitializationPanel (true, loadingText, progress);
					}
					yield return w;
					if (chunksPoolLoadIndex > required)
						break;
				}
			}

			InitEnd (callback);
		}

		void WarmChunksEditor (AfterInitCallback callback) {
			int required = 1000;
			if (world != null) {
				while (chunksPoolLoadIndex < maxChunks) {
					for (int k = 0; k < 100; k++) {
						ReserveChunkMemory ();
					}
					if (chunksPoolLoadIndex > required)
						break;
				}
			}
			InitEnd (callback);
		}



		void InitEnd (AfterInitCallback callback) {
			initialized = true;

			if (applicationIsPlaying) {
				GC.Collect ();
			}

			input.Init ();

			ComputeFirstReusableChunk ();

			if (applicationIsPlaying) {
				if (OnInitialized != null) {
					OnInitialized ();
				}
			}

			#if UNITY_EDITOR
			EditorApplication.update -= UpdateInEditor;
			if (renderInEditor && !applicationIsPlaying) {
				EditorApplication.update += UpdateInEditor;
			}
			#endif

			if (callback != null) {
				callback ();
			}

			if (initialWaitTime > 0 && Application.isPlaying) {
				input.enabled = false;
				StartCoroutine (DoWaitTime ());
			} else {
				EndWaitTime ();
			}
		}

		IEnumerator DoWaitTime () {
			WaitForSeconds w = new WaitForSeconds (0.2f);
			float start = Time.time;
			while (Time.time - start < initialWaitTime) {
				float progress = (Time.time - start) / initialWaitTime;
				if (progress > 1f) {
					progress = 1f;
				}
				VoxelPlayUI.instance.ToggleInitializationPanel (true, initialWaitText, progress);
				yield return w;
			}
			EndWaitTime ();
		}

		void EndWaitTime () {
			input.enabled = true;
			VoxelPlayUI.instance.ToggleInitializationPanel (false);
			ShowMessage (welcomeMessage, welcomeMessageDuration, true);
		}

		/// <summary>
		/// Destroyes everything and initializes world
		/// </summary>
		void LoadWorldInt () {
			DisposeAll ();

			if (world == null) {
				if (Application.isPlaying) {
					world = ScriptableObject.CreateInstance<WorldDefinition> ();
					Debug.LogWarning ("World Definition asset missing in Voxel Play Environment. Assigning a temporary asset.");
				} else {
					return;
				}
			}

			// Create world root
			if (worldRoot == null) {
				GameObject wr = GameObject.Find (VOXELPLAY_WORLD_ROOT);
				if (wr == null) {
					wr = new GameObject (VOXELPLAY_WORLD_ROOT);
					wr.transform.position = Misc.vector3zero;
				}
				worldRoot = wr.transform;
			}

			WorldRand.Randomize (world.seed);
			Physics.gravity = new Vector3 (0, world.gravity, 0);
			InitSaveGameStructs ();
			InitWater ();
			InitSky ();
			InitRenderer ();
			InitTrees ();
			InitVegetation ();
			LoadWorldTextures ();
			InitItems ();
			InitNavMesh ();
			InitChunkManager ();
			lastCamPos.y += 0.0001f; // forces check chunks in frustum
			InitClouds ();
			InitPhysics ();
			InitTileRules ();
			UpdateMaterialProperties ();
			SetBuildMode (buildMode);
		}


		#endregion

		#region Master rendering

		void DoWork () {
			try {
				STAGE = 1;
				CheckCamera ();

#if UNITY_EDITOR
				if (WantRepaintInspector != null) { // update inspector stats
					long elapsed = stopWatch.ElapsedMilliseconds - lastInspectorUpdateTime;
					if (elapsed > 1000) {
						WantRepaintInspector ();
						lastInspectorUpdateTime = stopWatch.ElapsedMilliseconds;
					}
				}
				if (!applicationIsPlaying) {
					if (renderInEditorLowPriority) {
						if (_cameraHasMoved) {
							lastCameraMoveTime = stopWatch.ElapsedMilliseconds;
						}
						long elapsed = stopWatch.ElapsedMilliseconds - lastCameraMoveTime;
						if (elapsed < 1000) {
							return;
						} else {
							// After 1 second of inactivity, rendering resumes but we need to inform that the camera probably has moved so the frustum planes get recalculated
							_cameraHasMoved = true;
							shouldCheckChunksInFrustum = true;
						}
					}
				}
#endif

				// main world creation & rendering cycle
				long currentTime = stopWatch.ElapsedMilliseconds;
				#if UNITY_EDITOR
				long availableTime = (!applicationIsPlaying && renderInEditorLowPriority) ? (long)(maxCPUTimePerFrame * 0.7f) : maxCPUTimePerFrame;
				long creationMaxTime = currentTime + (long)(availableTime * 0.8f); // Max 80% of frame time to populate stuff
				#else
				long availableTime = maxCPUTimePerFrame;
				long creationMaxTime = (long)(currentTime + availableTime * 0.8f);
				#endif

				if (applicationIsPlaying) {
					STAGE = 10;
					UpdateNavMesh ();
					DoLightEffects ();
					CheckSystemKeys ();

					if (requireTextureArrayUpdate) {
						STAGE = 11;
						LoadWorldTextures ();
					}
				}
				CheckCustomSunRotation ();

				// Content generators - only process new chunks if there's no GPU upload or collider creation jobs
				if (meshJobMeshUploadIndex == meshJobMeshDataGenerationReadyIndex) {
					STAGE = 3;
					CheckChunksInRange (creationMaxTime);
					STAGE = 4;
					CheckTreeRequests (creationMaxTime);
					STAGE = 5;
					CheckVegetationRequests (creationMaxTime);
					STAGE = 21;
					DoDetailWork (creationMaxTime);
					if (!effectiveMultithreadGeneration) {
						STAGE = 22;
						GenerateChunkMeshDataInMainThread (currentTime + availableTime);
					}
				}

				STAGE = 23;
				UpdateWaterFlood ();

				// Check which chunks need to be refreshed (either lightmap or content)
				STAGE = 9;
				CheckRenderChunkQueue (currentTime + availableTime);

				STAGE = 31;
				// Signal which chunk meshes need to be rebuilt, send them to generation thread and upload any ready mesh
				UpdateAndNotifyChunkChanges ();

				// After chunk mesh upload has completed, process any pending light update for particles
				UpdateParticlesLight ();

				STAGE = 41;
				// Render instanced objects
				instancingManager.Render (currentCamPos, _visibleChunksDistance, frustumPlanesNormals, frustumPlanesDistances);

				// end cycle
			} catch (Exception ex) {
				ShowExceptionMessage (ex);
			}
			STAGE = 0;
		}

		void ShowExceptionMessage (Exception ex) {
			string msg = "<color=yellow>Critical (STAGE=" + STAGE + ")</color>: " + ex.Message + "\n" + ex.StackTrace;
			ShowMessage (msg);
			Debug.LogError (msg);
		}

		Camera currentCamera {
			get {
				if (applicationIsPlaying) {
					return cameraMain;
				}

#if UNITY_EDITOR
				// In Editor camera (SceneCam)
				if (sceneCam == null) {
					Camera[] cam = SceneView.GetAllSceneCameras ();
					if (cam != null) {
						for (int k = 0; k < cam.Length; k++) {
							if (cam [k] == Camera.current) {
								sceneCam = Camera.current;
								break;
							}
						}
					}
				}

#endif

				if (Camera.current == null) {
					if (sceneCam == null) {
						return cameraMain;
					} else {
						return sceneCam;
					}
				} else {
					return Camera.current;
				}
			}
		}

		void CheckCamera () {

			Camera cam = currentCamera;

			if (cam == null)
				return;

			currentCamPos = cam.transform.position;
			currentCamRot = cam.transform.rotation;
			_cameraHasMoved = (lastCamPos != currentCamPos || lastCamRot != currentCamRot);
			if (_cameraHasMoved) {
				lastCamPos = currentCamPos;
				lastCamRot = currentCamRot;
				if (frustumPlanes == null) {
					frustumPlanes = new Plane[6];
					frustumPlanesDistances = new float[6];
					frustumPlanesNormals = new Vector3[6];
				}
#if UNITY_IOS || UNITY_WEBGL
				#if UNITY_2017_3_OR_NEWER
                GeometryUtility.CalculateFrustumPlanes(cam.projectionMatrix * cam.worldToCameraMatrix, frustumPlanes);
				#else
				frustumPlanes = GeometryUtility.CalculateFrustumPlanes (cam.projectionMatrix * cam.worldToCameraMatrix);
				#endif
#else
				GeometryUtilityNonAlloc.CalculateFrustumPlanes (frustumPlanes, cam.projectionMatrix * cam.worldToCameraMatrix);
#endif
				for (int k = 0; k < 6; k++) {
					frustumPlanesDistances [k] = frustumPlanes [k].distance;
					frustumPlanesNormals [k] = frustumPlanes [k].normal;
				}

				if (frustumCorners == null || frustumCorners.Length != 4) {
					frustumCorners = new Vector3[4];
				}
				cam.CalculateFrustumCorners (Misc.rectFullViewport, cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
				for (int i = 0; i < 4; i++) {
					frustumCorners [i] = cam.transform.position + cam.transform.TransformVector (frustumCorners [i]);
				}
				shouldCheckChunksInFrustum = true;
			}
		}

		void DoLightEffects () {

			Shader.SetGlobalFloat ("_VPEmissionIntensity", world.emissionMinIntensity + Mathf.PingPong (Time.time * world.emissionAnimationSpeed, (world.emissionMaxIntensity - world.emissionMinIntensity)));

			if (world.dayCycleSpeed == 0 || sun == null)
				return;
			Transform t = sun.transform;
			if (applicationIsPlaying) {
				t.Rotate (new Vector3 (1f, 0.2f, 0) * Time.deltaTime * world.dayCycleSpeed);
			}
		}

		void CheckCustomSunRotation () {
			if (world.setTimeAndAzimuth && sun != null) {
				if (world.timeOfDay != lastTimeOfDay || world.azimuth != lastAzimuth) {
					lastTimeOfDay = world.timeOfDay;
					lastAzimuth = world.azimuth;
					Vector3 r;
					r.x = 360 * (world.timeOfDay / 24f) + 270;
					r.y = world.azimuth;
					r.z = 0;
					sun.transform.eulerAngles = r;
				}
			}
		}

		public void UpdateInEditor () {
			if (!applicationIsPlaying) {
				if (!initialized) {
					BootEngine ();
				}
				DoWork ();
			}
		}

		void UpdateParticlesLight () {
			if (particlePool == null)
				return;
			for (int k = 0; k < particlePool.Length; k++) {
				if (!particlePool [k].used)
					continue;
				Renderer renderer = particlePool [k].renderer;
				if (Time.time > particlePool [k].destructionTime || renderer == null) {
					ReleaseParticle (k);
					continue;
				}
				if (!globalIllumination)
					continue;
				Vector3 currentPos = renderer.transform.position;
				int cx = (int)currentPos.x;
				int cy = (int)currentPos.y;
				int cz = (int)currentPos.z;
				if (shouldUpdateParticlesLighting || cx != particlePool [k].lastX || cy != particlePool [k].lastY || cz != particlePool [k].lastZ) {
					float voxelLight = GetVoxelLight (currentPos);
					renderer.sharedMaterial.SetFloat ("_VoxelLight", voxelLight);
					particlePool [k].lastX = cx;
					particlePool [k].lastY = cy;
					particlePool [k].lastZ = cz;
				}
			}
			shouldUpdateParticlesLighting = false;
		}


		void ReleaseParticle (int k) {
			particlePool [k].used = false;
			if (particlePool [k].renderer != null) {
				particlePool [k].rigidBody.isKinematic = true;
				particlePool [k].renderer.enabled = false;
				particlePool [k].item.enabled = false;
				particlePool [k].renderer.transform.position += new Vector3 (1000, 1000, 1000);
			}
			particlePool [k].lastX = int.MinValue;
		}

		#endregion


		#region Special keys handling

		void CheckSystemKeys () {
			if (enableConsole) {
				if (Input.GetKey (KeyCode.LeftControl)) {
					if (Input.GetKeyDown (KeyCode.F3)) {
						if (LoadGameBinary (false)) {
							ShowMessage ("<color=green>Game loaded successfully.</color>");
						}
					} else if (Input.GetKeyDown (KeyCode.F4)) {
						SaveGameBinary ();
						ShowMessage ("<color=green>Game saved. Press <color=yellow>Control + F3</color> to load.</color>");
					}
				}
			}
		}

		#endregion


		#region Editor helpers

		#if UNITY_EDITOR
		void CheckEditorTintColor () {
			if (!enableTinting) {
				Debug.Log ("Option enableTinting is disabled. To use colored voxels, please enable the option in the VoxelPlayEnvironment component inspector.");
			}
		}

		void CheckTintingScriptingSupport () {
			if (Application.isPlaying)
				return;

#if UNITY_2018_3_OR_NEWER
            // Do not execute if gameobject is prefab
            if (PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab)
                return;
            if (PrefabUtility.GetPrefabInstanceStatus(gameObject) == PrefabInstanceStatus.Connected)
                return;

#else
			// Do not execute if gameobject is prefab
			if (PrefabUtility.GetPrefabType (gameObject) == PrefabType.Prefab)
				return;
			if (PrefabUtility.GetPrefabParent (gameObject) == null && PrefabUtility.GetPrefabObject (gameObject) != null)
				return;
#endif

			if ((Voxel.supportsTinting && !enableTinting) || (!Voxel.supportsTinting && enableTinting)) {
				UpdateTintingCodeMacro ();
			}
		}

		public void SetShaderOptionValue (string option, string file, bool state) {
			string[] res = Directory.GetFiles (Application.dataPath, file, SearchOption.AllDirectories);
			string path = null;
			for (int k = 0; k < res.Length; k++) {
				if (res [k].Contains ("Voxel Play")) {
					path = res [k];
					break;
				}
			}
			if (path == null) {
				Debug.LogError (file + " could not be found!");
				return;
			}

			string[] code = File.ReadAllLines (path, System.Text.Encoding.UTF8);
			string searchToken = "#define " + option;
			for (int k = 0; k < code.Length; k++) {
				if (code [k].Contains (searchToken)) {
					if (state) {
						code [k] = "#define " + option;
					} else {
						code [k] = "//#define " + option;
					}
					File.WriteAllLines (path, code, System.Text.Encoding.UTF8);
					break;
				}
			}
		}

		public void SetShaderOptionValue (string option, string file, string value) {
			string[] res = Directory.GetFiles (Application.dataPath, file, SearchOption.AllDirectories);
			string path = null;
			for (int k = 0; k < res.Length; k++) {
				if (res [k].Contains ("Voxel Play")) {
					path = res [k];
					break;
				}
			}
			if (path == null) {
				Debug.LogError (file + " could not be found!");
				return;
			}

			string[] code = File.ReadAllLines (path, System.Text.Encoding.UTF8);
			string searchToken = "#define " + option;
			for (int k = 0; k < code.Length; k++) {
				if (code [k].Contains (searchToken)) {
					code [k] = "#define " + option + " " + value;
					File.WriteAllLines (path, code, System.Text.Encoding.UTF8);
					break;
				}
			}
		}


		public void UpdateTintingCodeMacro () {
			SetShaderOptionValue ("USES_TINTING", "Voxel.cs", enableTinting);
			AssetDatabase.Refresh ();
		}


		#endif

		#endregion

		#region Misc functions

		public List<T> GetList<T> (int proposedSize) {
			return new List<T> (lowMemoryMode ? 4 : proposedSize);
		}

		#endregion

	}




}
