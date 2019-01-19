using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace VoxelPlay {

	public delegate float SDF (Vector3 worldPosition);

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		public WorldDefinition world;
		public bool enableBuildMode = true;
		public bool constructorMode;
		public bool buildMode;
		public bool editorDraftMode;
		public bool renderInEditor;
		public bool renderInEditorLowPriority = true;
		public VoxelPlayCharacterControllerBase characterController;
		public bool enableConsole = true;
		public bool showConsole;
		public bool enableInventory = true;
		public bool enableDebugWindow = true;
		public bool showFPS;
		public string welcomeMessage = "<color=green>Welcome to <color=white>Voxel Play</color>! Press<color=yellow> F1 </color>for console commands.</color>";
		public float welcomeMessageDuration = 5f;
		public GameObject UICanvasPrefab;
		public GameObject crosshairPrefab;
		public Texture2D crosshairTexture;
		public Color consoleBackgroundColor = new Color (0, 0, 0, 82f / 255f);
		public bool enableStatusBar = true;
		public Color statusBarBackgroundColor = new Color (0, 0, 0, 192 / 255f);
		public int layerParticles = 2;
		public int layerVoxels = 1;
		public bool enableLoadingPanel = true;

		public bool loadSavedGame;
		public string saveFilename = "save0001";

		public bool globalIllumination = true;

		[Range (0f, 1f)]
		public float ambientLight = 0.2f;

		[Range (0f, 1f)]
		public float daylightShadowAtten = 0.65f;

		public bool enableSmoothLighting = true;

		public bool enableReliefMapping = false;

		[Range (0f, 0.2f)]
		public float reliefStrength = 0.05f;
		[Range (2, 100)]
		public int reliefIterations = 10;
		[Range (0, 10)]
		public int reliefIterationsBinarySearch = 5;

		public bool enableNormalMap = false;

		public bool enableFogSkyBlending = true;

		public int textureSize = 64;

		public int maxChunks = 16000;

		public bool hqFiltering = true;
		[Range (0, 2f)]
		public float mipMapBias = 1;

		public bool doubleSidedGlass = true;

		[NonSerialized]
		public Camera cameraMain;

		/// <summary>
		/// Minimum recommended chunk pool size based on visible distance
		/// </summary>
		public int maxChunksRecommended {
			get {
				int dxz = _visibleChunksDistance * 2 + 1;
				int dy = Mathf.Min (_visibleChunksDistance, 8) * 2 + 1;
				return Mathf.Max (3000, dxz * dxz * dy * 2);
			}
		}

		public int prewarmChunksInEditor = 5000;

		public bool enableTinting = false;

		public bool enableOutline = false;

		public Color outlineColor = new Color (1, 1, 1, 0.5f);

		[Range (0, 1f)]
		public float outlineThreshold = 0.49f;

		[Range (1, 25)]
		[SerializeField]
		int _visibleChunksDistance = 10;

		public int visibleChunksDistance {
			get { return _visibleChunksDistance; }
			set {
				if (_visibleChunksDistance != value) {
					_visibleChunksDistance = value;
					lastCamPos.y += 0.0001f; // forces check chunks in frustum
					InitOctrees ();
				}
			}
		}

		public bool adjustCameraFarClip = true;

		public bool useGeometryShaders = true;
		public bool usePixelLights = true;
		public bool enableShadows = true;
		public bool shadowsOnWater = false;
		[Range (1, 8)] public int forceChunkDistance = 3;
		public long maxCPUTimePerFrame = 30;
		public int maxChunksPerFrame = 50;
		public int maxTreesPerFrame = 10;
		public int maxBushesPerFrame = 10;
		public bool multiThreadGeneration = true;
		public bool onlyGenerateInFrustum = true;
		public bool onlyRenderInFrustum = true;

		public bool enableColliders = true;
		public bool enableNavMesh = false;
		public bool hideChunksInHierarchy = true;

		public bool enableTrees = true;
		public bool denseTrees = true;
		public bool enableVegetation = true;

		public Light sun;
		[Range (0, 1)]
		public float fogAmount = 0.5f;
		public bool fogUseCameraFarClip = true;
		public float fogDistance = 300;
		[Range (0, 1)]
		public float fogFallOff = 0.8f;
		public bool enableClouds = true;

		/// <summary>
		/// Default build sound.
		/// </summary>
		public AudioClip defaultBuildSound;

		/// <summary>
		/// Default pick up sound.
		/// </summary>
		public AudioClip defaultPickupSound;

		/// <summary>
		/// Default pick up sound.
		/// </summary>
		public AudioClip defaultDestructionSound;

		/// <summary>
		/// Default impact/hit sound.
		/// </summary>
		public AudioClip defaultImpactSound;

		[Tooltip ("Assumed voxel when the voxel definition is missing or placing colors directly on the positions")]
		public VoxelDefinition defaultVoxel;

		#region Public useful state fields

		[NonSerialized, HideInInspector]
		public VoxelHitInfo lastHitInfo;

		public bool cameraHasMoved {
			get { return _cameraHasMoved; }
		}

		[NonSerialized]
		public int chunksCreated, chunksUsed, chunksInRenderQueueCount, chunksDrawn;
		[NonSerialized]
		public int voxelsCreatedCount;
		[NonSerialized]
		public int treesInCreationQueueCount, treesCreated;
		[NonSerialized]
		public int vegetationInCreationQueueCount, vegetationCreated;

		#endregion


		#region Public Events

		/// <summary>
		/// Triggered after a voxel receives damage
		/// </summary>
		public event VoxelHitEvent OnVoxelDamaged;

		/// <summary>
		/// Triggered just before a voxel is destroyed
		/// </summary>
		public event VoxelEvent OnVoxelBeforeDestroyed;

		/// <summary>
		/// Triggered after a voxel is destroyed
		/// </summary>
		public event VoxelEvent OnVoxelDestroyed;

		/// <summary>
		/// Triggered just before a recoverable voxel is created.
		/// </summary>
		public event VoxelDropItemEvent OnVoxelBeforeDropItem;

		/// <summary>
		/// Triggered when clicking on a voxel.
		/// </summary>
		public event VoxelClickEvent OnVoxelClick;

		/// <summary>
		/// Triggered after the contents of a chunk changes (ie. placing a new voxel)
		/// </summary>
		public event VoxelChunkEvent OnChunkChanged;

		/// <summary>
		/// Triggered after a torch is placed
		/// </summary>
		public event VoxelTorchEvent OnTorchAttached;

		/// <summary>
		/// Triggered after a torch is removed
		/// </summary>
		public event VoxelTorchEvent OnTorchDetached;

		/// <summary>
		/// Triggered after a saved game is loaded
		/// </summary>
		public event VoxelPlayEvent OnGameLoaded;

		/// <summary>
		/// Triggered after Voxel Play has finished loading and initializing stuff
		/// </summary>
		public event VoxelPlayEvent OnInitialized;

		/// <summary>
		/// Triggered just before the chunk is filled with default contents (terrain, etc.)
		/// Set overrideDefaultContents to fill in the voxels array with your own content (voxel array is a linear array of 18x18x18 voxels)
		/// </summary>
		public event VoxelChunkBeforeCreationEvent OnChunkBeforeCreate;

		/// <summary>
		/// Triggered just after the chunk has been filled with default contents (terrain, etc.)
		/// </summary>
		public event VoxelChunkEvent OnChunkAfterCreate;

		/// <summary>
		/// Triggered just after the chunk has been rendered for the first time
		/// </summary>
		public event VoxelChunkEvent OnChunkAfterFirstRender;

		/// <summary>
		/// Triggered when chunk mesh is refreshed (updated and uploaded to the GPU)
		/// </summary>
		public event VoxelChunkEvent OnChunkRender;

		/// <summary>
		/// Triggered when requesting a refresh of the light buffers
		/// </summary>
		public event VoxelLightRefreshEvent OnLightRefreshRequest;

		#endregion



		#region Public API

		static VoxelPlayEnvironment _instance;

		/// <summary>
		/// An empty voxel
		/// </summary>
		public Voxel Empty;


		/// <summary>
		/// Returns the singleton instance of Voxel Play API.
		/// </summary>
		/// <value>The instance.</value>
		public static VoxelPlayEnvironment instance {
			get {
				if (_instance == null) {
					_instance = FindObjectOfType<VoxelPlayEnvironment> ();
					if (_instance == null) {
						VoxelPlayEnvironment[] vv = Resources.FindObjectsOfTypeAll<VoxelPlayEnvironment> ();
						for (int k = 0; k < vv.Length; k++) {
							if (vv [k].hideFlags != HideFlags.HideInHierarchy) {
								_instance = vv [k];
								break;
							}
						}
					}
				}
				return _instance;
			}
		}

		/// <summary>
		/// The default value for the light amount on a clear voxel. If global illumination is enabled, this value is 0 (dark). If it's disabled, then this value is 15 (so it does not darken the voxel).
		/// </summary>
		/// <value>The no light value.</value>
		public byte noLightValue {
			get {
				return effectiveGlobalIllumination ? (byte)0 : (byte)15;
			}
		}

		/// <summary>
		/// Gets the GameObject of the player
		/// </summary>
		/// <value>The player game object.</value>
		public GameObject playerGameObject {
			get {
				if (characterController != null) {
					return characterController.gameObject;
				} else {
					return cameraMain.gameObject;
				}
			}
		}

		/// <summary>
		/// Destroyes everything and reloads current assigned world
		/// </summary>
		public void ReloadWorld () {
			if (!applicationIsPlaying && !renderInEditor)
				return;

			LoadWorldInt ();
			initialized = true;
			DoWork ();
			// Refresh behaviours lighting
			VoxelPlayBehaviour[] bh = FindObjectsOfType<VoxelPlayBehaviour> ();
			for (int k = 0; k < bh.Length; k++) {
				bh [k].UpdateLighting ();
			}
		}


		/// <summary>
		/// Clears all chunks in the world and initializes all structures
		/// </summary>
		public void DestroyAllVoxels () {
			LoadWorldInt ();
			WarmChunks (null);
			initialized = true;
		}


		/// <summary>
		/// Issues a redraw command on all chunks
		/// </summary>
		public void Redraw (bool reloadWorldTextures = false) {
			if (reloadWorldTextures) {
				LoadWorldTextures ();
			}
			UpdateMaterialProperties ();
			if (cachedChunks != null) {
				foreach (KeyValuePair<int, CachedChunk> kv in cachedChunks) {
					CachedChunk cc = kv.Value;
					if (cc != null && cc.chunk != null) {
						ChunkRequestRefresh (cc.chunk, true, true);
					}
				}
			}
		}

		/// <summary>
		/// Toggles on/off chunks visibility
		/// </summary>
		public bool ChunksToggle () {
			if (chunksRoot != null) {
				chunksRoot.gameObject.SetActive (!chunksRoot.gameObject.activeSelf);
				return chunksRoot.gameObject.activeSelf;
			}
			return false;
		}

		/// <summary>
		/// Toggles chunks visibility
		/// </summary>
		public void ChunksToggle (bool visible) {
			if (chunksRoot != null) {
				chunksRoot.gameObject.SetActive (visible);
			}
		}

		/// <summary>
		/// Casts a ray and applies given damage to any voxel impacted in the direction.
		/// </summary>
		/// <returns><c>true</c>, if voxel was hit, <c>false</c> otherwise.</returns>
		/// <param name="ray">Ray.</param>
		/// <param name="damage">Damage.</param>
		/// <param name="maxDistance">Max distance of ray.</param>
		public bool RayHit (Ray ray, int damage, float maxDistance = 0, int damageRadius = 1) {
			return RayHit (ray.origin, ray.direction, damage, maxDistance, damageRadius);
		}

		/// <summary>
		/// Casts a ray and applies given damage to any voxel impacted in the direction.
		/// </summary>
		/// <returns><c>true</c>, if voxel was hit, <c>false</c> otherwise.</returns>
		/// <param name="ray">Ray.</param>
		/// <param name="damage">Damage.</param>
		/// <param name="hitInfo">VoxelHitInfo structure with additional details.</param>
		/// <param name="maxDistance">Max distance of ray.</param>
		public bool RayHit (Ray ray, int damage, out VoxelHitInfo hitInfo, float maxDistance = 0, int damageRadius = 1) {
			return RayHit (ray.origin, ray.direction, damage, out hitInfo, maxDistance, damageRadius);
		}

		/// <summary>
		/// Casts a ray and applies given damage to any voxel impacted in the direction.
		/// </summary>
		/// <returns><c>true</c>, if voxel was hit, <c>false</c> otherwise.</returns>
		/// <param name="ray">Ray.</param>
		/// <param name="damage">Damage.</param>
		/// <param name="maxDistance">Max distance of ray.</param>
		public bool RayHit (Vector3 origin, Vector3 direction, int damage, float maxDistance = 0, int damageRadius = 1) {
			VoxelHitInfo hitInfo;
			bool impact = HitVoxelFast (origin, direction, damage, out hitInfo, maxDistance, damageRadius);
			return impact;

		}

		/// <summary>
		/// Casts a ray and applies given damage to any voxel impacted in the direction.
		/// </summary>
		/// <returns><c>true</c>, if voxel was hit, <c>false</c> otherwise.</returns>
		/// <param name="ray">Ray.</param>
		/// <param name="damage">Damage.</param>
		/// <param name="hitInfo">VoxelHitInfo structure with additional details.</param>
		/// <param name="maxDistance">Max distance of ray.</param>
		public bool RayHit (Vector3 origin, Vector3 direction, int damage, out VoxelHitInfo hitInfo, float maxDistance = 0, int damageRadius = 1) {
			return HitVoxelFast (origin, direction, damage, out hitInfo, maxDistance, damageRadius);
		}


		/// <summary>
		/// Raycasts in the direction of the ray from ray's origin.
		/// </summary>
		/// <returns><c>true</c>, if a voxel was hit, <c>false</c> otherwise.</returns>
		/// <param name="ray">Ray.</param>
		/// <param name="hitInfo">Hit info.</param>
		/// <param name="maxDistance">Max distance.</param>
		/// <param name="minOpaque">Optionally limit the rayhit to voxels with certain opaque factor (15 = solid/full opaque, 3 = cutout, 2 = water, 0 = grass).</param>
		public bool RayCast (Ray ray, out VoxelHitInfo hitInfo, float maxDistance = 0, byte minOpaque = 0) {
			return RayCastFast (ray.origin, ray.direction, out hitInfo, maxDistance, false, minOpaque);
		}

		/// <summary>
		/// Raycasts from a given origin and direction.
		/// </summary>
		/// <returns><c>true</c>, if a voxel was hit, <c>false</c> otherwise.</returns>
		/// <param name="ray">Ray.</param>
		/// <param name="hitInfo">Hit info.</param>
		/// <param name="maxDistance">Max distance.</param>
		/// <param name="minOpaque">Optionally limit the rayhit to voxels with certain opaque factor (15 = solid/full opaque, 3 = cutout, 2 = water, 0 = grass).</param>
		public bool RayCast (Vector3 origin, Vector3 direction, out VoxelHitInfo hitInfo, float maxDistance = 0, byte minOpaque = 0) {
			return RayCastFast (origin, direction, out hitInfo, maxDistance, false, minOpaque);
		}

		/// <summary>
		/// Gets the highest voxel position under a given location
		/// </summary>
		public float GetHeight (Vector3 position) {
			VoxelHitInfo hitInfo;
			RayCastFast (new Vector3 (position.x, 1000, position.z), Misc.vector3down, out hitInfo, 0, true, 0, ColliderTypes.OnlyVoxels);
			return hitInfo.point.y;
		}

		/// <summary>
		/// Returns the voxel chunk where the player is located
		/// </summary>
		public VoxelChunk GetCurrentChunk () {
			if (cameraMain == null)
				return null;
			return GetChunkOrCreate (cameraMain.transform.position);
		}

		/// <summary>
		/// Returns true if water is found at a given position (only X/Z values are considered)
		/// </summary>
		/// <returns><c>true</c> if this instance is water at position; otherwise, <c>false</c>.</returns>
		public bool IsWaterAtPosition (Vector3 position) {
			if (heightMapCache == null)
				return false;
			float groundLevel = GetHeightMapInfoFast (position.x, position.z).groundLevel;
			if (waterLevel > groundLevel) {
				return true;
			} else {
				return false;
			}
		}

		[NonSerialized]
		public bool enableWaterFlood = true;

		/// <summary>
		/// Returns true if there a solid block at a position
		/// </summary>
		/// <returns><c>true</c> if this instance is water at position; otherwise, <c>false</c>.</returns>
		public bool IsWallAtPosition (Vector3 position) {
			VoxelChunk chunk;
			int voxelIndex;
			if (GetVoxelIndex (position, out chunk, out voxelIndex)) {
				return chunk.voxels [voxelIndex].opaque == FULL_OPAQUE;
			}
			return false;
		}

		/// <summary>
		/// Returns true if position is empty (no voxels)
		/// </summary>
		public bool IsEmptyAtPosition (Vector3 position) {
			VoxelChunk chunk;
			int voxelIndex;
			if (GetVoxelIndex (position, out chunk, out voxelIndex)) {
				return chunk.voxels [voxelIndex].hasContent != 1;
			}
			return false;
		}


		/// <summary>
		/// Returns the chunk at a given position
		/// </summary>
		/// <returns><c>true</c>, if chunk was gotten, <c>false</c> otherwise.</returns>
		/// <param name="position">Position.</param>
		/// <param name="chunk">Chunk.</param>
		/// <param name="forceCreation">If set to <c>true</c> force creation.</param>
		public bool GetChunk (Vector3 position, out VoxelChunk chunk, bool forceCreation = false) {
			int chunkX = FastMath.FloorToInt (position.x / 16);
			int chunkY = FastMath.FloorToInt (position.y / 16);
			int chunkZ = FastMath.FloorToInt (position.z / 16);
			return GetChunkFast (chunkX, chunkY, chunkZ, out chunk, forceCreation);
		}


		/// <summary>
		/// Returns a list of created chunks. 
		/// </summary>
		/// <param name="chunks">User provided list for returning the chunks.</param>
		/// <param name="onlyModified">Returns only chunks marked with modified flag (those modified by the user).</param>
		public void GetChunks (List<VoxelChunk> chunks, bool onlyModified = false) {
			chunks.Clear ();
			for (int k = 0; k < chunksPoolLoadIndex; k++) {
				if (chunksPool [k].isPopulated && (!onlyModified || (onlyModified && chunksPool [k].modified))) {
					chunks.Add (chunksPool [k]);
				}
			}
		}


		/// <summary>
		/// Returns the chunk at a given position without invoking the terrain generator (the chunk should be empty but only if it's got before it has rendered)
		/// You can use chunk.isPopulated to query if terrain has rendered into this chunk or not
		/// </summary>
		/// <returns><c>true</c>, if chunk was gotten, <c>false</c> otherwise.</returns>
		/// <param name="position">Position.</param>
		/// <param name="chunk">Chunk.</param>
		public VoxelChunk GetChunkUnpopulated (Vector3 position) {
			int chunkX = FastMath.FloorToInt (position.x / 16);
			int chunkY = FastMath.FloorToInt (position.y / 16);
			int chunkZ = FastMath.FloorToInt (position.z / 16);
			VoxelChunk chunk;
			STAGE = 201;
			GetChunkFast (chunkX, chunkY, chunkZ, out chunk, false);
			if ((object)chunk == null) {
				STAGE = 202;
				int hash = GetChunkHash (chunkX, chunkY, chunkZ);
				chunk = CreateChunk (hash, chunkX, chunkY, chunkZ, true, false);
			}
			return chunk;
		}




		/// <summary>
		/// Returns the chunk at a given position without invoking the terrain generator (the chunk should be empty but only if it's got before it has rendered)
		/// You can use chunk.isPopulated to query if terrain has rendered into this chunk or not
		/// </summary>
		/// <returns><c>true</c>, if chunk was gotten, <c>false</c> otherwise.</returns>
		/// <param name="chunkX">X position of chunk / 16. Use FastMath.FloorToInt(chunk.position.x/16)</param>
		/// <param name="chunkY">Y position of chunk / 16.</param>
		/// <param name="chunkZ">Z position of chunk / 16.</param>
		public VoxelChunk GetChunkUnpopulated (int chunkX, int chunkY, int chunkZ) {
			VoxelChunk chunk;
			STAGE = 201;
			GetChunkFast (chunkX, chunkY, chunkZ, out chunk, false);
			if ((object)chunk == null) {
				STAGE = 202;
				int hash = GetChunkHash (chunkX, chunkY, chunkZ);
				chunk = CreateChunk (hash, chunkX, chunkY, chunkZ, true, false);
			}
			return chunk;
		}


		/// <summary>
		/// Returns the chunk position that encloses a given position
		/// </summary>
		/// <param name="position">Position.</param>
		public Vector3 GetChunkPosition (Vector3 position) {
			int x = FastMath.FloorToInt (position.x / 16) * 16 + 8;
			int y = FastMath.FloorToInt (position.y / 16) * 16 + 8;
			int z = FastMath.FloorToInt (position.z / 16) * 16 + 8;
			return new Vector3 (x, y, z);
		}


		/// <summary>
		/// Gets the voxel at a given position. Returns Voxel.Empty if no voxel found.
		/// </summary>
		/// <returns>The voxel.</returns>
		/// <param name="position">Position.</param>
		/// <param name="createChunkIfNotExists">If set to <c>true</c> create chunk if not exists.</param>
		/// <param name="onlyRenderedVoxels">If set to <c>true</c> the voxel will only be returned if it's rendered. If you're calling GetVoxel as part of a spawning logic, pass true as it will ensure the voxel returned also has the collider in place so your spawned stuff won't fall down.</param>
		public Voxel GetVoxel (Vector3 position, bool createChunkIfNotExists = true, bool onlyRenderedVoxels = false) {
			int chunkX = FastMath.FloorToInt (position.x / 16);
			int chunkY = FastMath.FloorToInt (position.y / 16);
			int chunkZ = FastMath.FloorToInt (position.z / 16);
			VoxelChunk chunk;
			GetChunkFast (chunkX, chunkY, chunkZ, out chunk, createChunkIfNotExists);
			if (chunk != null && (!onlyRenderedVoxels || onlyRenderedVoxels && chunk.renderState == ChunkRenderState.RenderingComplete)) {
				Voxel[] voxels = chunk.voxels;
				int px = (int)(position.x - chunkX * 16); // FastMath.FloorToInt (position.x) - chunkX * 16;
				int py = (int)(position.y - chunkY * 16); // FastMath.FloorToInt (position.y) - chunkY * 16;
				int pz = (int)(position.z - chunkZ * 16); // FastMath.FloorToInt (position.z) - chunkZ * 16;
				int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
				return voxels [voxelIndex];
			}
			return Voxel.Empty;
		}


		/// <summary>
		/// Returns in indices list all visible voxels inside a volume defined by boxMin and boxMax
		/// </summary>
		/// <returns>Count of all visible voxel indices.</returns>
		/// <param name="boxMin">Bottom/left/back or minimum corner of the enclosing box.</param>
		/// <param name="boxMax">Top/right/forward or maximum corner of the enclosing box.</param>
		/// <param name="indices">A list of indices provided by the user to write to.</param>
		/// <param name="minOpaque">Minimum opaque value for a voxel to be considered. Water has an opaque factor of 2, cutout = 3, grass = 0.</param>
		/// <param name="hasContents">Defaults to 1 which will return existing voxels. Pass a 0 to retrieve positions without voxels. Pass -1 to ignore this filter.</param> 
		public int GetVoxelIndices (Vector3 boxMin, Vector3 boxMax, List<VoxelIndex> indices, byte minOpaque = 0, int hasContents = 1) {
			Vector3 position, voxelPosition;
			VoxelIndex index = new VoxelIndex ();
			indices.Clear ();

			Vector3 chunkMinPos = GetChunkPosition (boxMin);
			Vector3 chunkMaxPos = GetChunkPosition (boxMax);
			Vector3 center = (boxMax - boxMin) * 0.5f;

			bool createChunkIfNotExists = hasContents != 1;

			if (hasContents == 0)
				minOpaque = 0;

			for (float y = chunkMinPos.y; y <= chunkMaxPos.y; y += 16) {
				position.y = y;
				for (float z = chunkMinPos.z; z <= chunkMaxPos.z; z += 16) {
					position.z = z;
					for (float x = chunkMinPos.x; x <= chunkMaxPos.x; x += 16) {
						position.x = x;
						VoxelChunk chunk;
						if (GetChunk (position, out chunk, createChunkIfNotExists)) {
							for (int v = 0; v < chunk.voxels.Length; v++) {
								if (chunk.voxels [v].opaque >= minOpaque && (hasContents == -1 || chunk.voxels [v].hasContent == hasContents)) {
									int py = v / ONE_Y_ROW;
									voxelPosition.y = chunk.position.y - 7.5f + py;
									if (voxelPosition.y >= boxMin.y && voxelPosition.y < boxMax.y) {
										int remy = v - py * ONE_Y_ROW;
										int pz = remy / ONE_Z_ROW;
										voxelPosition.z = chunk.position.z - 7.5f + pz;
										if (voxelPosition.z >= boxMin.z && voxelPosition.z < boxMax.z) {
											int px = remy - pz * ONE_Z_ROW;
											voxelPosition.x = chunk.position.x - 7.5f + px;
											if (voxelPosition.x >= boxMin.x && voxelPosition.x < boxMax.x) {
												index.chunk = chunk;
												index.voxelIndex = v;
												index.position = voxelPosition;
												index.sqrDistance = FastVector.SqrDistance (ref voxelPosition, ref center);
												indices.Add (index);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return indices.Count;
		}


		/// <summary>
		/// Returns in indices list all visible voxels inside a volume defined by boxMin and boxMax
		/// </summary>
		/// <returns>Count of all visible voxel indices.</returns>
		/// <param name="boxMin">Bottom/left/back or minimum corner of the enclosing box.</param>
		/// <param name="boxMax">Top/right/forward or maximum corner of the enclosing box.</param>
		/// <param name="indices">A list of indices provided by the user to write to.</param>
		/// <param name="sdf">A delegate for a method that accepts a world space position and returns a negative value if that position is contained inside an user-defined volume.</param>
		public int GetVoxelIndices (Vector3 boxMin, Vector3 boxMax, List<VoxelIndex> indices, SDF sdf) {
			Vector3 position, voxelPosition;
			VoxelIndex index = new VoxelIndex ();
			indices.Clear ();

			Vector3 chunkMinPos = GetChunkPosition (boxMin);
			Vector3 chunkMaxPos = GetChunkPosition (boxMax);

			for (float y = chunkMinPos.y; y <= chunkMaxPos.y; y += 16) {
				position.y = y;
				for (float z = chunkMinPos.z; z <= chunkMaxPos.z; z += 16) {
					position.z = z;
					for (float x = chunkMinPos.x; x <= chunkMaxPos.x; x += 16) {
						position.x = x;
						VoxelChunk chunk;
						if (GetChunk (position, out chunk, true)) {
							for (int v = 0; v < chunk.voxels.Length; v++) {
								int py = v / ONE_Y_ROW;
								voxelPosition.y = chunk.position.y - 7.5f + py;
								if (voxelPosition.y >= boxMin.y && voxelPosition.y < boxMax.y) {
									int remy = v - py * ONE_Y_ROW;
									int pz = remy / ONE_Z_ROW;
									voxelPosition.z = chunk.position.z - 7.5f + pz;
									if (voxelPosition.z >= boxMin.z && voxelPosition.z < boxMax.z) {
										int px = remy - pz * ONE_Z_ROW;
										voxelPosition.x = chunk.position.x - 7.5f + px;
										if (voxelPosition.x >= boxMin.x && voxelPosition.x < boxMax.x) {
											if (sdf (voxelPosition) < 0) {
												index.chunk = chunk;
												index.voxelIndex = v;
												index.position = voxelPosition;
												indices.Add (index);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return indices.Count;
		}


		/// <summary>
		/// Returns in indices list all visible voxels inside a sphere
		/// </summary>
		/// <returns>Count of all visible voxel indices.</returns>
		/// <param name="center">Center of sphere.</param>
		/// <param name="radius">Radius of the sphere.</param>
		/// <param name="indices">A list of indices provided by the user to write to.</param>
		/// <param name="minOpaque">Minimum opaque value for a voxel to be considered. Water has opaque = 2, cutout = 3, grass = 0, solid = 15.</param>
		/// <param name="hasContents">Defaults to 1 which will return existing voxels. Pass a 0 to retrieve positions without voxels.</param>
		public int GetVoxelIndices (Vector3 center, float radius, List<VoxelIndex> indices, byte minOpaque = 0, byte hasContents = 1) {
			Vector3 position, voxelPosition;
			VoxelIndex index = new VoxelIndex ();
			indices.Clear ();

			center.x = Mathf.FloorToInt (center.x) + 0.5f;
			center.y = Mathf.FloorToInt (center.y) + 0.5f;
			center.z = Mathf.FloorToInt (center.z) + 0.5f;
			Vector3 boxMin = center - Misc.vector3one * (radius + 1f);
			Vector3 boxMax = center + Misc.vector3one * (radius + 1f);
			Vector3 chunkMinPos = GetChunkPosition (boxMin);
			Vector3 chunkMaxPos = GetChunkPosition (boxMax);
			float radiusSqr = radius * radius;

			if (hasContents == 0)
				minOpaque = 0;
			
			for (float y = chunkMinPos.y; y <= chunkMaxPos.y; y += 16) {
				position.y = y;
				for (float z = chunkMinPos.z; z <= chunkMaxPos.z; z += 16) {
					position.z = z;
					for (float x = chunkMinPos.x; x <= chunkMaxPos.x; x += 16) {
						position.x = x;
						VoxelChunk chunk;
						if (GetChunk (position, out chunk, false)) {
							for (int v = 0; v < chunk.voxels.Length; v++) {
								if (chunk.voxels [v].hasContent == hasContents && chunk.voxels [v].opaque >= minOpaque) {
									int py = v / ONE_Y_ROW;
									voxelPosition.y = chunk.position.y - 7.5f + py;
									if (voxelPosition.y >= boxMin.y && voxelPosition.y <= boxMax.y) {
										int remy = v - py * ONE_Y_ROW;
										int pz = remy / ONE_Z_ROW;
										voxelPosition.z = chunk.position.z - 7.5f + pz;
										if (voxelPosition.z >= boxMin.z && voxelPosition.z <= boxMax.z) {
											int px = remy - pz * ONE_Z_ROW;
											voxelPosition.x = chunk.position.x - 7.5f + px;
											if (voxelPosition.x >= boxMin.x && voxelPosition.x <= boxMax.x) {
												float dist = FastVector.SqrDistance (ref voxelPosition, ref center);
												if (dist <= radiusSqr) {
													index.chunk = chunk;
													index.voxelIndex = v;
													index.position = voxelPosition;
													index.sqrDistance = dist;
													indices.Add (index);
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return indices.Count;
		}


		/// <summary>
		/// Returns a copy of all voxels in a given volume
		/// </summary>
		/// <param name="boxMin">Bottom/left/back or minimum corner of the enclosing box.</param>
		/// <param name="boxMax">Top/right/forward or maximum corner of the enclosing box.</param>
		/// <param name="voxels">User provided 3-dimensional array of voxels (y/z/x). You must allocate enough space before calling this method.</param> 
		public void GetVoxels (Vector3 boxMin, Vector3 boxMax, Voxel[,,] voxels) {
			Vector3 position;

			Vector3 chunkMinPos = GetChunkPosition (boxMin);
			Vector3 chunkMaxPos = GetChunkPosition (boxMax);

			int minY = FastMath.FloorToInt (boxMin.y);
			int minZ = FastMath.FloorToInt (boxMin.z);
			int minX = FastMath.FloorToInt (boxMin.x);
			int maxY = FastMath.FloorToInt (boxMax.y);
			int maxZ = FastMath.FloorToInt (boxMax.z);
			int maxX = FastMath.FloorToInt (boxMax.x);
			int sizeY = maxY - minY;
			int sizeZ = maxZ - minZ;
			int sizeX = maxX - minX;
			int msizeY = voxels.GetUpperBound (0);
			int msizeZ = voxels.GetUpperBound (1);
			int msizeX = voxels.GetUpperBound (2);
			if (msizeY < sizeY || msizeZ < sizeZ || msizeX < sizeX) {
				Debug.LogError ("Voxels array size does not match volume size. Expected size: [" + sizeY + ", " + sizeZ + ", " + sizeX + "]");
				return;
			}

			for (float y = chunkMinPos.y; y <= chunkMaxPos.y; y += 16) {
				position.y = y;
				for (float z = chunkMinPos.z; z <= chunkMaxPos.z; z += 16) {
					position.z = z;
					for (float x = chunkMinPos.x; x <= chunkMaxPos.x; x += 16) {
						position.x = x;
						VoxelChunk chunk;
						if (GetChunk (position, out chunk, false)) {
							int chunkMinY = FastMath.FloorToInt (chunk.position.y) - 8;
							int chunkMinZ = FastMath.FloorToInt (chunk.position.z) - 8;
							int chunkMinX = FastMath.FloorToInt (chunk.position.x) - 8;
							for (int vy = 0; vy < 16; vy++) {
								int wy = chunkMinY + vy;
								if (wy < minY || wy > maxY)
									continue;
								int my = wy - minY;
								int voxelIndexY = vy * ONE_Y_ROW;
								for (int vz = 0; vz < 16; vz++) {
									int wz = chunkMinZ + vz;
									if (wz < minZ || wz > maxZ)
										continue;
									int mz = wz - minZ;
									int voxelIndex = voxelIndexY + vz * ONE_Z_ROW;
									for (int vx = 0; vx < 16; vx++, voxelIndex++) {
										int wx = chunkMinX + vx;
										if (wx < minX || wx > maxX)
											continue;
										int mx = wx - minX;
										voxels [my, mz, mx] = chunk.voxels [voxelIndex];
									}
								}
							}
						} else {
							int chunkMinY = Mathf.FloorToInt (y) - 8;
							int chunkMinZ = Mathf.FloorToInt (z) - 8;
							int chunkMinX = Mathf.FloorToInt (x) - 8;
							int voxelIndex = 0;
							for (int vy = 0; vy < 16; vy++) {
								int wy = chunkMinY + vy;
								if (wy < minY || wy > maxY)
									continue;
								int my = wy - minY;
								for (int vz = 0; vz < 16; vz++) {
									int wz = chunkMinZ + vz;
									if (wz < minZ || wz > maxZ)
										continue;
									int mz = wz - minZ;
									for (int vx = 0; vx < 16; vx++, voxelIndex++) {
										int wx = chunkMinX + vx;
										if (wx < minX || wx > maxX)
											continue;
										int mx = wx - minX;
										voxels [my, mz, mx] = Voxel.Empty;
									}
								}
							}
						}
					}
				}
			}
		}


		/// <summary>
		/// Replaces voxels
		/// </summary>
		/// <param name="boxMin">Bottom/left/back or minimum corner of the enclosing box.</param>
		/// <param name="boxMax">Top/right/forward or maximum corner of the enclosing box.</param>
		/// <param name="voxels">User provided 3-dimensional array of voxels (y/z/x). You must allocate enough space before calling this method.</param> 
		/// <param name="ignoreEmptyVoxels">Set to true to only place voxels from the array which are not empty</param>
		public void VoxelPlace (Vector3 boxMin, Vector3 boxMax, Voxel[,,] voxels, bool ignoreEmptyVoxels = false) {
			Vector3 position;

			Vector3 chunkMinPos = GetChunkPosition (boxMin);
			Vector3 chunkMaxPos = GetChunkPosition (boxMax);

			int minY = FastMath.FloorToInt (boxMin.y);
			int minZ = FastMath.FloorToInt (boxMin.z);
			int minX = FastMath.FloorToInt (boxMin.x);
			int maxY = FastMath.FloorToInt (boxMax.y);
			int maxZ = FastMath.FloorToInt (boxMax.z);
			int maxX = FastMath.FloorToInt (boxMax.x);
			int sizeY = maxY - minY;
			int sizeZ = maxZ - minZ;
			int sizeX = maxX - minX;
			int msizeY = voxels.GetUpperBound (0);
			int msizeZ = voxels.GetUpperBound (1);
			int msizeX = voxels.GetUpperBound (2);
			if (msizeY < sizeY || msizeZ < sizeZ || msizeX < sizeX) {
				Debug.LogError ("Voxels array size does not match volume size. Expected size: [" + sizeY + ", " + sizeZ + ", " + sizeX + "]");
				return;
			}

			for (float y = chunkMinPos.y; y <= chunkMaxPos.y; y += 16) {
				position.y = y;
				for (float z = chunkMinPos.z; z <= chunkMaxPos.z; z += 16) {
					position.z = z;
					for (float x = chunkMinPos.x; x <= chunkMaxPos.x; x += 16) {
						position.x = x;
						VoxelChunk chunk;
						if (GetChunk (position, out chunk, true)) {
							int chunkMinY = FastMath.FloorToInt (chunk.position.y) - 8;
							int chunkMinZ = FastMath.FloorToInt (chunk.position.z) - 8;
							int chunkMinX = FastMath.FloorToInt (chunk.position.x) - 8;
							for (int vy = 0; vy < 16; vy++) {
								int wy = chunkMinY + vy;
								if (wy < minY || wy > maxY)
									continue;
								int my = wy - minY;
								int voxelIndexY = vy * ONE_Y_ROW;
								for (int vz = 0; vz < 16; vz++) {
									int wz = chunkMinZ + vz;
									if (wz < minZ || wz > maxZ)
										continue;
									int mz = wz - minZ;
									int voxelIndex = voxelIndexY + vz * ONE_Z_ROW;
									for (int vx = 0; vx < 16; vx++, voxelIndex++) {
										int wx = chunkMinX + vx;
										if (wx < minX || wx > maxX)
											continue;
										int mx = wx - minX;
										if (voxels [my, mz, mx].hasContent == 1 || !ignoreEmptyVoxels) {
											chunk.voxels [voxelIndex] = voxels [my, mz, mx];
										}
									}
								}
							}
							ChunkRequestRefresh (chunk, true, true);
						}
					}
				}
			}
		}

		/// <summary>
		/// Adds a new voxel definition to the internal dictionary
		/// </summary>
		/// <param name="vd">Vd.</param>
		public void AddVoxelDefinition (VoxelDefinition vd) {
            if (vd == null) return;
			// Check if voxelType is not added
			if (vd.index <= 0) {
				sessionUserVoxels.Add (vd);
				requireTextureArrayUpdate = true;
			}
		}

		/// <summary>
		/// Gets the voxel definition by name
		/// </summary>
		/// <returns>The voxel definition.</returns>
		public VoxelDefinition GetVoxelDefinition (string name) {
			VoxelDefinition vd;
			voxelDefinitionsDict.TryGetValue (name, out vd);
			return vd;
		}


		/// <summary>
		/// Gets the voxel definition by index
		/// </summary>
		/// <returns>The voxel definition.</returns>
		/// <param name="index">Index.</param>
		public VoxelDefinition GetVoxelDefinition (int index) {
			if (index >= 0 && index < voxelDefinitionsCount) {
				return voxelDefinitions [index];
			}
			return null;
		}


		/// <summary>
		/// Gets a VoxelIndex struct that locates a voxel position in world space
		/// </summary>
		/// <returns>The voxel index.</returns>
		/// <param name="position">Position in world space.</param>
		/// <param name="createChunkIfNotExists">Pass true to force the chunk creation if it does not exist at that position. Defaults to false.</param>
		public VoxelIndex GetVoxelIndex (Vector3 position, bool createChunkIfNotExists = false) {
			VoxelIndex index = new VoxelIndex ();
			GetVoxelIndex (position, out index.chunk, out index.voxelIndex, createChunkIfNotExists);
			return index;
		}

		/// <summary>
		/// Gets the chunk position and voxelIndex corresponding to a given world position (note that the chunk might not exists yet)
		/// </summary>
		/// <param name="position">World position.</param>
		/// <param name="chunk">Chunk position.</param>
		/// <param name="index">Voxel index in the chunk.voxels array</param>
		public void GetVoxelIndex (Vector3 position, out Vector3 chunkPosition, out int voxelIndex) {
			int chunkX, chunkY, chunkZ;
			chunkX = FastMath.FloorToInt (position.x / 16f) * 16;
			chunkY = FastMath.FloorToInt (position.y / 16f) * 16;
			chunkZ = FastMath.FloorToInt (position.z / 16f) * 16;
			chunkPosition.x = chunkX + 8;
			chunkPosition.y = chunkY + 8;
			chunkPosition.z = chunkZ + 8;
			int px = (int)(position.x - chunkX);
			int py = (int)(position.y - chunkY);
			int pz = (int)(position.z - chunkZ);
			voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
		}

		/// <summary>
		/// Gets the chunk and array index of the voxel at a given position
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="chunk">Chunk.</param>
		/// <param name="index">Voxel index in the chunk.voxels array</param>
		public bool GetVoxelIndex (Vector3 position, out VoxelChunk chunk, out int voxelIndex, bool createChunkIfNotExists = true) {
			int chunkX, chunkY, chunkZ;
			chunkX = FastMath.FloorToInt (position.x / 16f);
			chunkY = FastMath.FloorToInt (position.y / 16f);
			chunkZ = FastMath.FloorToInt (position.z / 16f);
			if (GetChunkFast (chunkX, chunkY, chunkZ, out chunk, createChunkIfNotExists)) {
				int py = (int)(position.y - chunkY * 16);
				int pz = (int)(position.z - chunkZ * 16);
				int px = (int)(position.x - chunkX * 16);
				voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
				return true;
			} else {
				voxelIndex = 0;
				return false;
			}
		}

		/// <summary>
		/// Gets the index of a voxel by its local x,y,z positions inside a voxel
		/// </summary>
		/// <returns>The voxel index.</returns>
		public int GetVoxelIndex (int px, int py, int pz) {
			return py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
		}


		/// <summary>
		/// Gets the chunk and voxel index corresponding to certain offset to another chunk/voxel index. Useful to get a safe reference to voxels on top of others, etc.
		/// </summary>
		/// <returns><c>true</c>, if voxel index was gotten, <c>false</c> otherwise.</returns>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		/// <param name="offsetX">Offset x.</param>
		/// <param name="offsetY">Offset y.</param>
		/// <param name="offsetZ">Offset z.</param>
		/// <param name="otherChunk">Other chunk.</param>
		/// <param name="otherVoxelIndex">Other voxel index.</param>
		/// <param name="createChunkIfNotExists">If set to <c>true</c> create chunk if not exists.</param>
		public bool GetVoxelIndex (VoxelChunk chunk, int voxelIndex, int offsetX, int offsetY, int offsetZ, out VoxelChunk otherChunk, out int otherVoxelIndex, bool createChunkIfNotExists = false) {
			int chunkX, chunkY, chunkZ;

			int px, py, pz;
			GetVoxelChunkCoordinates (voxelIndex, out px, out py, out pz);
			Vector3 position;
			position.x = chunk.position.x - 7.5f + px + offsetX;
			position.y = chunk.position.y - 7.5f + py + offsetY;
			position.z = chunk.position.z - 7.5f + pz + offsetZ;
			chunkX = FastMath.FloorToInt (position.x / 16f);
			chunkY = FastMath.FloorToInt (position.y / 16f);
			chunkZ = FastMath.FloorToInt (position.z / 16f);
			if (GetChunkFast (chunkX, chunkY, chunkZ, out otherChunk, createChunkIfNotExists)) {
				py = (int)(position.y - chunkY * 16); // FastMath.FloorToInt (position.y) - chunkY * 16;
				pz = (int)(position.z - chunkZ * 16); // FastMath.FloorToInt (position.z) - chunkZ * 16;
				px = (int)(position.x - chunkX * 16); // FastMath.FloorToInt (position.x) - chunkX * 16;
				otherVoxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
				return true;
			} else {
				otherVoxelIndex = 0;
				return false;
			}
		}

		/// <summary>
		/// Gets the voxel position in world space coordinates.
		/// </summary>
		/// <returns>The voxel position.</returns>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		public Vector3 GetVoxelPosition (VoxelChunk chunk, int voxelIndex) {
			int px, py, pz;
			GetVoxelChunkCoordinates (voxelIndex, out px, out py, out pz);
			Vector3 position;
			position.x = chunk.position.x - 7.5f + px;
			position.y = chunk.position.y - 7.5f + py;
			position.z = chunk.position.z - 7.5f + pz;
			return position;
		}



		/// <summary>
		/// Gets the corresponding voxel position in world space coordinates (the voxel position is exactly the center of the voxel).
		/// </summary>
		/// <returns>The voxel position.</returns>
		/// <param name="position">Any position in world space coordinates.</param>
		public Vector3 GetVoxelPosition (Vector3 position) {
			position.x = Mathf.Floor (position.x) + 0.5f;
			position.y = Mathf.Floor (position.y) + 0.5f;
			position.z = Mathf.Floor (position.z) + 0.5f;
			return position;
		}



		/// <summary>
		/// Gets the voxel local position inside the chunk
		/// </summary>
		/// <returns>The voxel position.</returns>
		/// <param name="position">Position relative to the chunk center</param>
		public Vector3 GetVoxelChunkPosition (int voxelIndex) {
			int px, py, pz;
			GetVoxelChunkCoordinates (voxelIndex, out px, out py, out pz);
			Vector3 position;
			position.x = -7.5f + px;
			position.y = -7.5f + py;
			position.z = -7.5f + pz;
			return position;
		}


		/// <summary>
		/// Gets the voxel position in world space coordinates.
		/// </summary>
		/// <returns>The voxel position.</returns>
		/// <param name="chunkPosition">Chunk position.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		public Vector3 GetVoxelPosition (Vector3 chunkPosition, int voxelIndex) {
			int px, py, pz;
			GetVoxelChunkCoordinates (voxelIndex, out px, out py, out pz);
			Vector3 position;
			position.x = chunkPosition.x - 7.5f + px;
			position.y = chunkPosition.y - 7.5f + py;
			position.z = chunkPosition.z - 7.5f + pz;
			return position;
		}

		/// <summary>
		/// Given a voxel index, returns its x, y, z position inside the chunk
		/// </summary>
		/// <param name="voxelIndex">Voxel index.</param>
		/// <param name="px">Px.</param>
		/// <param name="py">Py.</param>
		/// <param name="pz">Pz.</param>
		[MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
		public void GetVoxelChunkCoordinates (int voxelIndex, out int px, out int py, out int pz) {
			py = voxelIndex / ONE_Y_ROW;
			int remy = voxelIndex - py * ONE_Y_ROW;
			pz = remy / ONE_Z_ROW;
			px = remy - pz * ONE_Z_ROW;
		}

		/// <summary>
		/// Returns true if the specified voxel has content and is visible from any of the 6 surrounding faces
		/// </summary>
		/// <returns><c>true</c>, if voxel is visible, <c>false</c> otherwise.</returns>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		public bool GetVoxelVisibility (VoxelChunk chunk, int voxelIndex) {
			if (chunk == null || chunk.voxels [voxelIndex].hasContent != 1)
				return false;
			
			int px, py, pz;
			GetVoxelChunkCoordinates (voxelIndex, out px, out py, out pz);

			for (int o = 0; o < 6 * 3; o += 3) {
				VoxelChunk otherChunk = chunk;
				int ox = px + neighbourOffsets [o];
				int oy = py + neighbourOffsets [o + 1];
				int oz = pz + neighbourOffsets [o + 2];
				if (ox < 0) {
					otherChunk = chunk.left;
					ox = 15;
					if (otherChunk == null)
						return true;
				} else if (ox >= 16) {
					ox = 0;
					otherChunk = chunk.right;
					if (otherChunk == null)
						return true;
				}
				if (oy < 0) {
					otherChunk = chunk.bottom;
					oy = 15;
					if (otherChunk == null)
						return true;
				} else if (oy >= 16) {
					oy = 0;
					otherChunk = chunk.top;
					if (otherChunk == null)
						return true;
				}
				if (oz < 0) {
					otherChunk = chunk.back;
					oz = 15;
					if (otherChunk == null)
						return true;
				} else if (oz >= 16) {
					oy = 0;
					otherChunk = chunk.forward;
					if (otherChunk == null)
						return true;
				}
				int otherIndex = GetVoxelIndex (ox, oy, oz);
				if (otherChunk.voxels [otherIndex].hasContent != 1)
					return true;
			}

			return false;
		}


		/// <summary>
		/// Requests a refresh of a given chunk. The chunk mesh will be recreated and the lightmap will be computed again.
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		public void ChunkRedraw (VoxelChunk chunk, bool includeNeighbours = false) {
			if (includeNeighbours) {
				RefreshNineChunks (chunk);
			} else {
				ChunkRequestRefresh (chunk, true, true);
			}
		}

		/// <summary>
		/// Gets the voxel under a given position. Returns Voxel.Empty if no voxel found.
		/// </summary>
		/// <returns>The voxel under.</returns>
		/// <param name="position">Position.</param>
		public Voxel GetVoxelUnder (Vector3 position, bool includeWater = false) {
			VoxelHitInfo hitinfo;
			byte minOpaque = includeWater ? (byte)255 : (byte)0;
			if (RayCastFast (position, Misc.vector3down, out hitinfo, 0, false, minOpaque, ColliderTypes.OnlyVoxels)) {
				return hitinfo.chunk.voxels [hitinfo.voxelIndex];
			}
			return Voxel.Empty;
		}


		/// <summary>
		/// Gets the voxel under a given position. Returns Voxel.Empty if no voxel found.
		/// </summary>
		/// <returns>The voxel under.</returns>
		/// <param name="position">Position.</param>
		public VoxelIndex GetVoxelUnderIndex (Vector3 position, bool includeWater = false) {
			VoxelIndex index = new VoxelIndex ();
			VoxelHitInfo hitinfo;
			byte minOpaque = includeWater ? (byte)255 : (byte)0;
			if (RayCastFast (position, Misc.vector3down, out hitinfo, 0, false, minOpaque, ColliderTypes.OnlyVoxels)) {
				index.chunk = hitinfo.chunk;
				index.voxelIndex = hitinfo.voxelIndex;
				index.position = hitinfo.point;
				index.sqrDistance = hitinfo.sqrDistance;
			}
			return index;
		}


		/// <summary>
		/// Changes/set the tint color of one voxel
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		/// <param name="color">Color.</param>
		public void VoxelSetColor (VoxelChunk chunk, int voxelIndex, Color32 color) {
			if (chunk != null) {
				#if UNITY_EDITOR
				CheckEditorTintColor ();
				#endif
				chunk.voxels [voxelIndex].color = color;
				chunk.modified = true;
				ChunkRequestRefresh (chunk, false, true);
			}

		}

		/// <summary>
		/// Places a new voxel on a givel position in world space coordinates. Optionally plays a sound.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="voxelType">Voxel.</param>
		/// <param name="playSound">If set to <c>true</c> play sound.</param>
		public void VoxelPlace (Vector3 position, VoxelDefinition voxelType, bool playSound = false) {
			VoxelPlace (position, voxelType, playSound, voxelType.tintColor);
		}

		/// <summary>
		/// Places a new voxel on a givel position in world space coordinates. Optionally plays a sound.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="voxelType">Voxel.</param>
		/// <param name="tintColor">Tint color.</param>
		/// <param name="playSound">If set to <c>true</c> play sound.</param>
		public void VoxelPlace (Vector3 position, VoxelDefinition voxelType, Color tintColor, bool playSound = false) {
			VoxelPlace (position, voxelType, playSound, tintColor);
		}

		/// <summary>
		/// Places a default voxel on a givel position in world space coordinates with specified color. Optionally plays a sound.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="tintColor">The tint color for the voxel.</param>
		/// <param name="playSound">If set to <c>true</c> play sound when placing the voxel.</param>
		public void VoxelPlace (Vector3 position, Color tintColor, bool playSound = false) {
			VoxelPlace (position, defaultVoxel, playSound, tintColor);
		}


		/// <summary>
		/// Places a default voxel on an existing chunk with specified color. Optionally plays a sound.
		/// </summary>
		/// <param name="chunk">The chunk object.</param>
		/// <param name="voxelIndex">The index of the voxel.</param>
		/// <param name="tintColor">The tint color for the voxel.</param>
		/// <param name="playSound">If set to <c>true</c> play sound when placing the voxel.</param>
		public void VoxelPlace (VoxelChunk chunk, int voxelIndex, Color tintColor, bool playSound = false) {
			Vector3 position = GetVoxelPosition (chunk, voxelIndex);
			VoxelPlace (position, defaultVoxel, playSound, tintColor);
		}

		/// <summary>
		/// Places a list of voxels within a given chunk. List of voxels is given by a list of ModelBit structs.
		/// </summary>
		/// <param name="chunk">The chunk object.</param>
		/// <param name="voxels">The list of voxels to insert into the chunk.</param>
		/// <param name="indices">Optional user-provided list. Will contain the list of visible voxels if provided.</param>
		public void VoxelPlace (VoxelChunk chunk, List<ModelBit> voxels) {
			ModelPlace (chunk, voxels);
		}


		/// <summary>
		/// Places a voxel at a given position.
		/// </summary>
		/// <param name="chunk">The chunk object.</param>
		/// <param name="voxels">The list of voxels to insert into the chunk.</param>
		/// /// <param name="playSound">If set to <c>true</c> play sound when placing the voxel.</param>
		public void VoxelPlace (Vector3 position, Voxel voxel, bool playSound = false) {
			VoxelPlace (position, voxelDefinitions [voxel.typeIndex], voxel.color, playSound);
		}


		/// <summary>
		/// Places a new voxel on a givel position in world space coordinates. Optionally plays a sound.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="voxelType">Voxel.</param>
		/// <param name="playSound">If set to <c>true</c> play sound when placing the voxel.</param>
		/// <param name="tintColor">The tint color for the voxel.</param>
		/// <param name="amount">Only used to place a specific amount of water-like voxels (0-1).</param>
		/// <param name="rotation">Rotation turns. Can be 0, 1, 2 or 3 and represents clockwise 90-degree step rotations.</param>
		public void VoxelPlace (Vector3 position, VoxelDefinition voxelType, bool playSound, Color tintColor, float amount = 1f, int rotation = 0) {
			if (voxelType == null) {
				return;
			}

			#if UNITY_EDITOR
			if (!enableTinting && tintColor != Color.white) {
				Debug.Log ("Option enableTinting is disabled. To use colored voxels, please enable the option in the VoxelPlayEnvironment component inspector.");
			}
			#endif

			// Check if voxelType is known
			if (voxelType.index <= 0) {
				sessionUserVoxels.Add (voxelType);
				requireTextureArrayUpdate = true;
			}

			if (playSound) {
				PlayBuildSound (voxelType.buildSound, position);
			}

			VoxelChunk chunk;
			int voxelIndex;
			VoxelPlaceFast (position, voxelType, out chunk, out voxelIndex, tintColor, amount, rotation);
		}



		/// <summary>
		/// Returns true if a voxel placed on a given position would overlap any collider.
		/// </summary>
		public bool VoxelOverlaps (Vector3 position, VoxelDefinition type, Quaternion rotation, int layerMask = -1) {
			// Check if the voxel will overlap any collider then 
			if (type.renderType == RenderType.Custom && type.modelUsesCollider) {
				Bounds meshBounds = type.mesh.bounds;
				Vector3 extents = meshBounds.extents;
				FastVector.Multiply (ref extents, ref type.scale, 0.9f);
				Quaternion rot = type.GetRotation (position) * rotation;
				position += rot * (meshBounds.center + type.GetOffset (position));
				return Physics.OverlapBoxNonAlloc (position, extents, tempColliders, rot, layerMask, QueryTriggerInteraction.Ignore) > 0;
			} 
			return Physics.OverlapBoxNonAlloc (position, new Vector3 (0.45f, 0.45f, 0.45f), tempColliders, Misc.quaternionZero, layerMask, QueryTriggerInteraction.Ignore) > 0;
		}


		/// <summary>
		/// Puts lots of voxels in the given positions. Takes care of informing neighbour chunks.
		/// </summary>
		/// <param name="positions">Positions.</param>
		/// <param name="voxelType">Voxel type.</param>
		/// <param name="tintColor">Tint color.</param>
		/// <param name="chunks">Optionally provide a chunks list which will be filled with modified chunks.</param>
		public void VoxelPlace (List<Vector3> positions, VoxelDefinition voxelType, Color tintColor, List<VoxelChunk> modifiedChunks = null) {

			VoxelChunk chunk, lastChunk = null;
			int voxelIndex;
			int count = positions.Count;

			if (modifiedChunks == null) {
				if (voxelPlaceFastAffectedChunks == null) {
					voxelPlaceFastAffectedChunks = new List<VoxelChunk> ();
				} else {
					voxelPlaceFastAffectedChunks.Clear ();
				}
				modifiedChunks = voxelPlaceFastAffectedChunks;
			}

			if (voxelType == null) {
				for (int k = 0; k < count; k++) {
					VoxelSingleClear (positions [k], out chunk, out voxelIndex);
					if (lastChunk != chunk) {
						lastChunk = chunk;
						if (!modifiedChunks.Contains (chunk)) {
							modifiedChunks.Add (chunk);
							chunk.modified = true;
						}
					}
				}
			} else {
				for (int k = 0; k < count; k++) {
					VoxelSingleSet (positions [k], voxelType, out chunk, out voxelIndex, tintColor);
					if (lastChunk != chunk) {
						lastChunk = chunk;
						if (!modifiedChunks.Contains (chunk)) {
							modifiedChunks.Add (chunk);
							chunk.modified = true;
						}
					}
				}
			}

			int mainChunksCount = modifiedChunks.Count;
			for (int k = 0; k < mainChunksCount; k++) {
				ChunkRequestRefresh (modifiedChunks [k], true, true);
				// Triggers event
				if (OnChunkChanged != null) {
					OnChunkChanged (modifiedChunks [k]);
				}
			}
		}



		/// <summary>
		/// Puts lots of voxels in the given positions. Takes care of informing neighbour chunks.
		/// </summary>
		/// <param name="indices">Array of voxel indices for placing.</param>
		/// <param name="voxelType">Voxel type.</param>
		/// <param name="tintColor">Tint color.</param>
		/// <param name="modifiedChunks">Optionally return the list of modified chunks</param>
		public void VoxelPlace (List<VoxelIndex> indices, VoxelDefinition voxelType, Color tintColor, List<VoxelChunk> modifiedChunks = null) {

			VoxelChunk lastChunk = null;
			int count = indices.Count;

			if (modifiedChunks == null) {
				if (voxelPlaceFastAffectedChunks == null) {
					voxelPlaceFastAffectedChunks = new List<VoxelChunk> ();
				} else {
					voxelPlaceFastAffectedChunks.Clear ();
				}
				modifiedChunks = voxelPlaceFastAffectedChunks;
			}

			if (voxelType == null) {
				byte light = noLightValue;
				for (int k = 0; k < count; k++) {
					VoxelIndex vi = indices [k];
					vi.chunk.voxels [vi.voxelIndex].Clear (light);
					if (lastChunk != vi.chunk) {
						lastChunk = vi.chunk;
						if (!modifiedChunks.Contains (lastChunk)) {
							modifiedChunks.Add (lastChunk);
							lastChunk.modified = true;
						}
					}
				}
			} else {
				for (int k = 0; k < count; k++) {
					VoxelIndex vi = indices [k];
					vi.chunk.voxels [vi.voxelIndex].Set (voxelType, tintColor);
					if (lastChunk != vi.chunk) {
						lastChunk = vi.chunk;
						if (!modifiedChunks.Contains (lastChunk)) {
							modifiedChunks.Add (lastChunk);
							lastChunk.modified = true;
						}
					}
				}
			}

			int mainChunksCount = modifiedChunks.Count;
			for (int k = 0; k < mainChunksCount; k++) {
				ChunkRequestRefresh (modifiedChunks [k], true, true);
				// Triggers event
				if (OnChunkChanged != null) {
					OnChunkChanged (modifiedChunks [k]);
				}
			}
		}


		/// <summary>
		/// Fills an area with same voxel definition and optional tint color
		/// </summary>
		/// <param name="boxMin">Box minimum.</param>
		/// <param name="boxMax">Box max.</param>
		/// <param name="voxelType">Voxel type.</param>
		/// <param name="tintColor">Tint color.</param>
		/// <param name="modifiedChunks">Optionally return the list of modified chunks</param>
		public void VoxelPlace (Vector3 boxMin, Vector3 boxMax, VoxelDefinition voxelType, Color tintColor, List<VoxelChunk> modifiedChunks = null) {
			GetVoxelIndices (boxMin, boxMax, tempVoxelIndices, 0, -1);
			VoxelPlace (tempVoxelIndices, voxelType, tintColor, modifiedChunks);
		}


		/// <summary>
		/// Creates an optimized voxel game object from an array of colors
		/// </summary>
		/// <returns>The create game object.</returns>
		/// <param name="colors">Colors in Y/Z/X distribution.</param>
		/// <param name="sizeX">Size x.</param>
		/// <param name="sizeY">Size y.</param>
		/// <param name="sizeZ">Size z.</param>
		public GameObject VoxelCreateGameObject (Color32[] colors, int sizeX, int sizeY, int sizeZ) {
			return VoxelPlayConverter.GenerateVoxelObject (colors, sizeX, sizeY, sizeZ, Misc.vector3zero, Misc.vector3one);
		}

		/// <summary>
		/// Creates an optimized voxel game object from an array of colors
		/// </summary>
		/// <returns>The create game object.</returns>
		/// <param name="colors">Colors in Y/Z/X distribution.</param>
		/// <param name="sizeX">Size x.</param>
		/// <param name="sizeY">Size y.</param>
		/// <param name="sizeZ">Size z.</param>
		/// <param name="offset">Mesh offset.</param>
		/// <param name="scale">Mesh scale.</param>
		public GameObject VoxelCreateGameObject (Color32[] colors, int sizeX, int sizeY, int sizeZ, Vector3 offset, Vector3 scale) {
			return VoxelPlayConverter.GenerateVoxelObject (colors, sizeX, sizeY, sizeZ, offset, scale);
		}


		/// <summary>
		/// Adds a highlight effect to a voxel at a given position. If there's no voxel at that position this method returns false.
		/// </summary>
		/// <returns><c>true</c>, if highlight was voxeled, <c>false</c> otherwise.</returns>
		/// <param name="hitInfo">A voxelHitInfo struct with information about the location of the highlighted voxel.</param>
		public bool VoxelHighlight (VoxelHitInfo hitInfo, Color color, float edgeWidth = 0f) {

			if (voxelHighlightGO == null) {
				voxelHighlightGO = Instantiate<GameObject> (Resources.Load<GameObject> ("VoxelPlay/Prefabs/VoxelHighlightEdges"));
			}
			Material mat = voxelHighlightGO.GetComponent<Renderer> ().sharedMaterial;
			mat.color = color;
			if (edgeWidth > 0f) {
				mat.SetFloat ("_Width", 1f / edgeWidth);
			}
			if (hitInfo.placeholder != null) {
				voxelHighlightGO.transform.SetParent (hitInfo.placeholder.transform, false);
				voxelHighlightGO.transform.localScale = hitInfo.placeholder.bounds.size;
				if (hitInfo.placeholder.modelMeshRenderer != null) {
					voxelHighlightGO.transform.position = hitInfo.placeholder.modelMeshRenderer.bounds.center;
				} else {
					voxelHighlightGO.transform.localPosition = hitInfo.placeholder.bounds.center;
				}
				if (hitInfo.placeholder.modelInstance != null) {
					voxelHighlightGO.transform.localRotation = hitInfo.placeholder.modelInstance.transform.localRotation;
				} else {
					voxelHighlightGO.transform.localRotation = Misc.quaternionZero;
				}
			} else {
				voxelHighlightGO.transform.SetParent (worldRoot, false);
				voxelHighlightGO.transform.position = hitInfo.voxelCenter;
				voxelHighlightGO.transform.localScale = Misc.vector3one;
				voxelHighlightGO.transform.localRotation = Misc.quaternionZero;

				// Adapt box highlight to voxel contents
				if (hitInfo.chunk != null && hitInfo.voxel.typeIndex > 0) {
					// water?
					int waterLevel = hitInfo.voxel.GetWaterLevel ();
					if (waterLevel > 0) {
						// adapt to water level
						float ly = waterLevel / 15f;
						voxelHighlightGO.transform.localScale = new Vector3 (1, ly, 1);
						voxelHighlightGO.transform.position = new Vector3 (hitInfo.voxelCenter.x, hitInfo.voxelCenter.y - 0.5f + ly * 0.5f, hitInfo.voxelCenter.z);
					} else {
						VoxelDefinition vd = voxelDefinitions [hitInfo.voxel.typeIndex];
						if (vd.gpuInstancing && vd.renderType == RenderType.Custom) {
							// instanced mesh ?
							Bounds bounds = vd.mesh.bounds;
							Quaternion rotation = vd.GetRotation (hitInfo.voxelCenter);
							// User rotation
							float rot = hitInfo.chunk.voxels [hitInfo.voxelIndex].GetTextureRotationDegrees ();
							if (rot != 0) {
								rotation *= Quaternion.Euler (0, rot, 0);
							}
							// Custom position
							voxelHighlightGO.transform.position = hitInfo.voxelCenter + rotation * (bounds.center + vd.GetOffset (hitInfo.voxelCenter));
							Vector3 size = bounds.size;
							FastVector.Multiply (ref size, ref vd.scale);
							voxelHighlightGO.transform.localScale = size;
							voxelHighlightGO.transform.localRotation = rotation;
						} else if (vd.renderType == RenderType.CutoutCross) {
							// grass?
							Vector3 pos = hitInfo.voxelCenter - hitInfo.chunk.position;
							Vector3 aux = pos;
							float random = WorldRand.GetValue (pos);
							pos.x += random * 0.5f - 0.25f;
							aux.x += 1f;
							random = WorldRand.GetValue (aux);
							pos.z += random * 0.5f - 0.25f;
							float offsetY = random * 0.1f;
							pos.y -= offsetY * 0.5f + 0.5f - vd.scale.y * 0.5f;
							voxelHighlightGO.transform.position = hitInfo.chunk.position + pos;
							Vector3 adjustedScale = vd.scale;
							adjustedScale.y -= offsetY;
							voxelHighlightGO.transform.localScale = adjustedScale;
						}
					}
				}
			}
			voxelHighlightGO.SetActive (true);
			return true;
		}

		/// <summary>
		/// Shows/hides current voxel highlight
		/// </summary>
		/// <returns><c>true</c>, if highlight was voxeled, <c>false</c> otherwise.</returns>
		/// <param name="visible">If set to <c>true</c> visible.</param>
		public void VoxelHighlight (bool visible) {
			if (voxelHighlightGO == null)
				return;
			voxelHighlightGO.SetActive (visible);
		}


		/// <summary>
		/// Damages a voxel.
		/// </summary>
		/// <returns><c>true</c>, if voxel was hit, <c>false</c> otherwise.</returns>
		/// <param name="position">Position.</param>
		public bool VoxelDamage (Vector3 position, int damage, bool playSound = false) {
			VoxelHitInfo hitInfo;
			VoxelChunk chunk;
			int voxelIndex;
			if (!GetVoxelIndex (position, out chunk, out voxelIndex, false) || chunk.voxels [voxelIndex].hasContent != 1)
				return false;
			bool impact = HitVoxelFast (position, Misc.vector3down, damage, out hitInfo, 1, 1, false, playSound, false);
			return impact;
		}


		/// <summary>
		/// Simulates an explosion at a given position damaging every voxel within radius.
		/// </summary>
		/// <returns><c>int</c>, the number of damaged voxels<c>false</c> otherwise.</returns>
		/// <param name="origin">Explosion origin.</param>
		/// <param name="damage">Maximm damage.</param>
		/// <param name="radius">Radius.</param>
		/// <param name="attenuateDamageWithDistance">If set to <c>true</c> damage will be reduced with distance.</param>
		/// <param name="addParticles">If set to <c>true</c> damage particles will be added.</param>
		public int VoxelDamage (Vector3 origin, int damage, int radius, bool attenuateDamageWithDistance, bool addParticles = true, List<VoxelIndex> damagedVoxels = null) {
			return DamageAreaFast (origin, radius, damage, attenuateDamageWithDistance, addParticles, damagedVoxels);
		}


		/// <summary>
		/// Clears a voxel.
		/// </summary>
		/// <returns><c>true</c>, if voxel was destroyed, <c>false</c> otherwise.</returns>
		/// <param name="position">Position.</param>
		public bool VoxelDestroy (VoxelChunk chunk, int voxelIndex) {
			if (chunk == null)
				return false;
			if (chunk.voxels [voxelIndex].hasContent == 1) {
				VoxelDestroyFast (chunk, voxelIndex);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Clears a voxel.
		/// </summary>
		/// <returns><c>true</c>, if voxel was destroyed, <c>false</c> otherwise.</returns>
		/// <param name="position">Position.</param>
		/// <param name="createChunkIfNotExists">Pass true if you need to make sure the terrain has been generated before destroying this voxel.</param>
		public bool VoxelDestroy (Vector3 position, bool createChunkIfNotExists = false) {
			VoxelChunk chunk;
			int voxelIndex;
			if (!GetVoxelIndex (position, out chunk, out voxelIndex, createChunkIfNotExists)) {
				return false;
			}
			if (chunk.voxels [voxelIndex].hasContent == 1) {
				VoxelDestroyFast (chunk, voxelIndex);
				return true;
			}
			return false;
		}


		/// <summary>
		/// Makes all voxels with "willCollapse" flag on top of certain position to collapse and fall
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="amount">Maximum number of voxels to collapse.</param>
		/// <param name="voxelIndices">If voxelIndices is provided, it will be filled with the collapsing voxels.</param>
		/// <param name="consolidateDelay">If consolidateDelay is greater than 0, collapsed voxels will be either destroyed or converted back to normal voxels after 'duration' in seconds.</param>
		public void VoxelCollapse (Vector3 position, int amount, List<VoxelIndex> voxelIndices = null, float consolidateDelay = 0) {
			int count = GetCrumblyVoxelIndices (position, amount, voxelIndices);
			if (count > 0) {
				VoxelGetDynamic (tempVoxelIndices, true, consolidateDelay);
			}
		}

		/// <summary>
		/// Returns true if a given voxel is dynamic
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		public bool VoxelIsDynamic (VoxelChunk chunk, int voxelIndex) {
			VoxelPlaceholder placeHolder = GetVoxelPlaceholder (chunk, voxelIndex, true);
			if (placeHolder == null)
				return false;

			if (placeHolder.modelMeshFilter == null)
				return false;

			return true;
		}


		/// <summary>
		/// Converts a dynamic voxel back to normal voxel. This operation will result in voxel being destroyed if it the current voxel position is already occupied by another voxel.
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		public bool VoxelCancelDynamic (VoxelChunk chunk, int voxelIndex) {

			// If no voxel there cancel
			if (chunk == null || chunk.voxels [voxelIndex].hasContent != 1)
				return false;

			// If it a dynamic voxel?
			VoxelPlaceholder placeholder = GetVoxelPlaceholder (chunk, voxelIndex, false);
			if (placeholder == null)
				return false;

			if (placeholder.modelMeshFilter == null)
				return false;

			placeholder.CancelDynamic ();
			return true;
		}


		/// <summary>
		/// Converts a dynamic voxel back to normal voxel. This operation will result in voxel being destroyed if it the current voxel position is already occupied by another voxel.
		/// </summary>
		/// <param name="placeholder">Placeholder.</param>
		public bool VoxelCancelDynamic (VoxelPlaceholder placeholder) {

			// No model instance? Return
			if (placeholder == null || placeholder.modelInstance == null)
				return false;

			// Check if voxel is of dynamic type
			VoxelChunk chunk = placeholder.chunk;
			if (chunk == null)
				return false;
			
			int voxelIndex = placeholder.voxelIndex;
			if (voxelIndex < 0 || chunk.voxels [voxelIndex].hasContent != 1)
				return false;

			VoxelDefinition voxelType = chunk.voxels [voxelIndex].type.staticDefinition;
			if (voxelType != null) {
				Color voxelColor = chunk.voxels [voxelIndex].color;

				// Removes old voxel from chunk
				VoxelDestroyFastSingle (chunk, voxelIndex);

				// Places the voxel if destination target is empty
				VoxelChunk targetChunk;
				int targetVoxelIndex;
				Vector3 targetPosition = placeholder.transform.position;
				if (GetVoxelIndex (targetPosition, out targetChunk, out targetVoxelIndex, false)) {
					if (targetChunk.voxels [targetVoxelIndex].opaque < 3) {
						targetChunk.voxels [targetVoxelIndex].Set (voxelType, voxelColor);
						targetChunk.modified = true;
						RefreshNineChunks (targetChunk);
					}
				}
			}

			// Destroy old placeholder after 1 seconds, hoping the normal mesh has already been rendered in place
			placeholder.modelMeshFilter = null;
			placeholder.modelTemplate = null;
			if (placeholder.modelInstance != null) {
				Destroy (placeholder.modelInstance);
			}
			return true;
		}



		/// <summary>
		/// Converts a list of voxels into dynamic gameobjects.
		/// </summary>
		/// <param name="voxelIndices">Voxel indices.</param>
		/// <param name="addRigidbody">If set to <c>true</c> add rigidbody.</param>
		/// <param name="duration">If duration is greater than 0, voxel will be converted back to normal voxel after 'duration' seconds.</param>
		public void VoxelGetDynamic (List<VoxelIndex> voxelIndices, bool addRigidbody = false, float duration = 0) {

			VoxelChunk lastChunk = null;
			tempChunks.Clear ();
			int count = voxelIndices.Count;
			for (int k = 0; k < count; k++) {
				VoxelIndex vi = voxelIndices [k];
				VoxelChunk chunk = vi.chunk;
				GameObject obj = VoxelSetDynamic (chunk, vi.voxelIndex, addRigidbody, duration);
				if (obj == null)
					continue;
				if (lastChunk != chunk) {
					lastChunk = chunk;
					if (!tempChunks.Contains (chunk)) {
						tempChunks.Add (chunk);
					}
				}
			}

			// Refresh & notify
			count = tempChunks.Count;
			for (int k = 0; k < count; k++) {
				VoxelChunk chunk = tempChunks [k];
				// Update chunk
				chunk.modified = true;
				ChunkRequestRefresh (chunk, true, true);

				// Triggers event
				if (OnChunkChanged != null) {
					OnChunkChanged (chunk);
				}
			}
		}


		/// <summary>
		/// Converts a voxel into a dynamic gameobject. If voxel has already been converted, it just returns the reference to the gameobject.
		/// </summary>
		/// <returns>The get dynamic.</returns>
		/// <param name="position">Voxel position in world space.</param>
		/// <param name="addRigidbody">If set to <c>true</c> add rigidbody.</param>
		/// <param name="duration">If duration is greater than 0, voxel will be either destroyed or converted back to normal voxel after 'duration' seconds.</param>
		public GameObject VoxelGetDynamic (Vector3 position, bool addRigidbody = false, float duration = 0) {
			VoxelChunk chunk;
			int voxelIndex;
			if (!GetVoxelIndex (position, out chunk, out voxelIndex))
				return null;
			return VoxelGetDynamic (chunk, voxelIndex, addRigidbody, duration);
		}


		/// <summary>
		/// Converts a voxel into a dynamic gameobject. If voxel has already been converted, it just returns the reference to the gameobject.
		/// </summary>
		/// <returns>The get dynamic.</returns>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		/// <param name="addRigidbody">If set to <c>true</c> add rigidbody.</param>
		/// <param name="duration">If duration is greater than 0, voxel will be either destroyed or converted back to normal voxel after 'duration' seconds.</param>
		public GameObject VoxelGetDynamic (VoxelChunk chunk, int voxelIndex, bool addRigidbody = false, float duration = 0) {

			if (chunk == null || chunk.voxels [voxelIndex].hasContent == 0)
				return null;

			VoxelDefinition vd = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];
			if (!vd.renderType.supportsDynamic ()) {
				#if UNITY_EDITOR
				Debug.LogError ("Only opaque, transparent, opaque-no-AO and cutout voxel types can be converted to dynamic voxels.");
				#endif
				return null;
			}

			GameObject obj = VoxelSetDynamic (chunk, voxelIndex, addRigidbody, duration);
			if (obj == null)
				return null;

			// If voxel is already custom-type, then just returns the placeholder gameobject
			if (vd.renderType == RenderType.Custom)
				return obj;
			
			chunk.modified = true;
			ChunkRequestRefresh (chunk, true, true);

			// Triggers event
			if (OnChunkChanged != null) {
				OnChunkChanged (chunk);
			}

			return obj;
		}


		/// <summary>
		/// Creates a recoverable voxel and throws it at given position, direction and strength
		/// </summary>
		/// <param name="position">Position in world space.</param>
		/// <param name="direction">Direction.</param>
		/// <param name="voxelType">Voxel definition.</param>
		public void VoxelThrow (Vector3 position, Vector3 direction, float velocity, VoxelDefinition voxelType, Color32 color) {
			GameObject voxelGO = CreateRecoverableVoxel (position, voxelType, color, GetVoxelLight (position));
			if (voxelGO == null)
				return;
			Rigidbody rb = voxelGO.GetComponent<Rigidbody> ();
			if (rb == null)
				return;
			rb.velocity = direction * velocity;
		}

		/// <summary>
		/// Rotates a voxel
		/// </summary>
		/// <param name="position">Voxel position in world space.</param>
		/// <param name="angleX">Angle x.</param>
		/// <param name="angleY">Angle y.</param>
		/// <param name="angleZ">Angle z.</param>
		public void VoxelRotate (Vector3 position, float angleX, float angleY, float angleZ) {
			VoxelChunk chunk;
			int voxelIndex;
			if (!GetVoxelIndex (position, out chunk, out voxelIndex, false)) {
				return;
			}
			VoxelRotate (chunk, voxelIndex, angleX, angleY, angleZ);
		}

		/// <summary>
		/// Rotates a voxel
		/// </summary>
		/// <param name="chunk">Chunk of the voxel.</param>
		/// <param name="voxelIndex">Index of the voxel in the chunk.</param>
		/// <param name="angleX">Angle x.</param>
		/// <param name="angleY">Angle y.</param>
		/// <param name="angleZ">Angle z.</param>
		public void VoxelRotate (VoxelChunk chunk, int voxelIndex, float angleX, float angleY, float angleZ) {
			GameObject obj = VoxelGetDynamic (chunk, voxelIndex);
			if (obj != null) {
				obj.transform.Rotate (angleX, angleY, angleZ);
				chunk.modified = true;
			}
		}

		/// <summary>
		/// Sets the rotation of a voxel. Voxel will be converted to dynamic first.
		/// </summary>
		/// <param name="chunk">Chunk of the voxel.</param>
		/// <param name="voxelIndex">Index of the voxel in the chunk.</param>
		/// <param name="angleX">Angle x.</param>
		/// <param name="angleY">Angle y.</param>
		/// <param name="angleZ">Angle z.</param>
		public void VoxelSetRotation (VoxelChunk chunk, int voxelIndex, float angleX, float angleY, float angleZ) {
			GameObject obj = VoxelGetDynamic (chunk, voxelIndex);
			if (obj != null) {
				obj.transform.localRotation = Quaternion.Euler (angleX, angleY, angleZ);
				chunk.modified = true;
			}
		}


		/// <summary>
		/// Sets the rotation of the side textures of a voxel.
		/// </summary>
		/// <param name="position">Voxel position in world space.</param>
		/// <param name="rotation">Rotation turns. Can be 0, 1, 2 or 3 and represents clockwise 90-degree step rotations.</param>
		public bool VoxelSetTexturesRotation (Vector3 position, int rotation) {
			VoxelChunk chunk;
			int voxelIndex;
			if (!GetVoxelIndex (position, out chunk, out voxelIndex, false)) {
				return false;
			}
			return VoxelSetTexturesRotation (chunk, voxelIndex, rotation);
		}


		/// <summary>
		/// Sets the rotation of the side textures of a voxel.
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		/// <param name="rotation">Rotation turns. Can be 0, 1, 2 or 3 and represents clockwise 90-degree step rotations.</param>
		public bool VoxelSetTexturesRotation (VoxelChunk chunk, int voxelIndex, int rotation) {
			if (chunk == null || voxelIndex < 0)
				return false;

			VoxelDefinition vd = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];
			if (vd.allowsTextureRotation && vd.renderType.supportsTextureRotation ()) {
				int currentRotation = chunk.voxels [voxelIndex].GetTextureRotation ();
				if (currentRotation != rotation) {
					chunk.voxels [voxelIndex].SetTextureRotation (rotation);
					chunk.modified = true;
					ChunkRequestRefresh (chunk, false, true);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns the current rotation for the side textures of a voxel
		/// </summary>
		/// <returns>The get texture rotation.</returns>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		public int VoxelGetTexturesRotation (VoxelChunk chunk, int voxelIndex) {
			if (chunk != null && voxelIndex >= 0) {
				return chunk.voxels [voxelIndex].GetTextureRotation ();
			}
			return 0;
		}

		/// <summary>
		/// Rotates the side textures of a voxel
		/// </summary>
		/// <param name="position">Voxel position in world space.</param>
		/// <param name="rotation">Turns. Each turn represent a 90º degree rotation.</param>
		public bool VoxelRotateTextures (Vector3 position, int rotation) {
			VoxelChunk chunk;
			int voxelIndex;
			if (!GetVoxelIndex (position, out chunk, out voxelIndex, false)) {
				return false;
			}
			return VoxelRotateTextures (chunk, voxelIndex, rotation);
		}

		/// <summary>
		/// Rotates the side textures of a voxel
		/// </summary>
		/// <param name="chunk">Chunk of the voxel.</param>
		/// <param name="voxelIndex">Index of the voxel in the chunk.</param>
		/// <param name="rotation">Turns (0, 1, 2 or 3). Each turn represent a 90º degree rotation. Can be positive or negative.</param>
		public bool VoxelRotateTextures (VoxelChunk chunk, int voxelIndex, int rotation) {
			if (chunk == null || voxelIndex < 0 || !voxelDefinitions [chunk.voxels [voxelIndex].typeIndex].renderType.supportsTextureRotation ())
				return false;

			int currentRotation = chunk.voxels [voxelIndex].GetTextureRotation ();
			currentRotation = (currentRotation + rotation + 128000) % 4;
			chunk.voxels [voxelIndex].SetTextureRotation (currentRotation);
			chunk.modified = true;
			ChunkRequestRefresh (chunk, false, true);
			return true;
		}



		/// <summary>
		/// Clears a chunk given a world space position.
		/// </summary>
		/// <param name="position">Position.</param>
		public bool ChunkDestroy (Vector3 position) {
			VoxelChunk chunk;
			GetChunk (position, out chunk, false);
			return ChunkDestroy (chunk);
		}


		/// <summary>
		/// Clears a chunk given a world space position.
		/// </summary>
		/// <param name="position">Position.</param>
		public bool ChunkDestroy (VoxelChunk chunk) {
			if (chunk == null)
				return false;
			ChunkClearFast (chunk);
			chunk.modified = true;

			// Refresh rendering
			UpdateChunkRR (chunk);

			// Triggers event
			if (OnChunkChanged != null) {
				OnChunkChanged (chunk);
			}
			return true;
		}


		/// <summary>
		/// Returns true if the underline NavMEs
		/// </summary>
		/// <returns><c>true</c>, if has nav mesh ready was chunked, <c>false</c> otherwise.</returns>
		/// <param name="chunk">Chunk.</param>
		public bool ChunkHasNavMeshReady (VoxelChunk chunk) {
			return chunk.navMeshSourceIndex >= 0 && !navMeshHasNewData && !navMeshIsUpdating;
		}


		/// <summary>
		/// Returns true if chunk is within camera frustum
		/// </summary>
		public bool ChunkIsInFrustum (VoxelChunk chunk) {
			Vector3 boundsMin;
			boundsMin.x = chunk.position.x - 8;
			boundsMin.y = chunk.position.y - 8;
			boundsMin.z = chunk.position.z - 8;
			Vector3 boundsMax;
			boundsMax.x = chunk.position.x + 8;
			boundsMax.y = chunk.position.y + 8;
			boundsMax.z = chunk.position.z + 8;
			chunk.visibleInFrustum = GeometryUtilityNonAlloc.TestPlanesAABB (frustumPlanesNormals, frustumPlanesDistances, ref boundsMin, ref boundsMax);
			return chunk.visibleInFrustum;
		}


		/// <summary>
		/// Ensures chunks within given bounds are created
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="chunkExtents">Distance in chunk units (each chunk is 16 world units)</param>
		/// <param name="renderChunks">If set to <c>true</c> enable chunk rendering.</param>
		public void ChunkCheckArea (Vector3 position, Vector3 chunkExtents, bool renderChunks = false) {
			int chunkX = FastMath.FloorToInt (position.x / 16f);
			int chunkY = FastMath.FloorToInt (position.y / 16f);
			int chunkZ = FastMath.FloorToInt (position.z / 16f);
			int xmin = chunkX - (int)chunkExtents.x;
			int ymin = chunkY - (int)chunkExtents.y;
			int zmin = chunkZ - (int)chunkExtents.z;
			int xmax = chunkX + (int)chunkExtents.x;
			int ymax = chunkY + (int)chunkExtents.y;
			int zmax = chunkZ + (int)chunkExtents.z;

			for (int x = xmin; x <= xmax; x++) {
				int x00 = WORLD_SIZE_DEPTH * WORLD_SIZE_HEIGHT * (x + WORLD_SIZE_WIDTH);
				for (int y = ymin; y <= ymax; y++) {
					int y00 = WORLD_SIZE_DEPTH * (y + WORLD_SIZE_HEIGHT);
					int h00 = x00 + y00;
					for (int z = zmin; z <= zmax; z++) {
						int hash = h00 + z;
						CachedChunk cachedChunk;
						if (cachedChunks.TryGetValue (hash, out cachedChunk)) {
							VoxelChunk chunk = cachedChunk.chunk;
							if (chunk == null)
								continue;
							if (chunk.isPopulated) {
								if (renderChunks && chunk.renderState != ChunkRenderState.RenderingComplete) {
									ChunkRequestRefresh (chunk, false, true, true);
								}
								continue;
							}
						}
						VoxelChunk newChunk = CreateChunk (hash, x, y, z, false);
						if (renderChunks) {
							ChunkRequestRefresh (newChunk, false, true, true);
						}
					}
				}
			}
		}


		/// <summary>
		/// Gets the terrain height under a given position, optionally including water
		/// </summary>
		public float GetTerrainHeight (Vector3 position, bool includeWater = false) {

			if ((object)heightMapCache == null)
				return 0;
			float groundLevel = GetHeightMapInfoFast (position.x, position.z).groundLevel;
			if (includeWater && waterLevel > groundLevel) {
				return waterLevel + 0.9f;
			} else {
				return groundLevel + 1f;
			}
		}

		/// <summary>
		/// Gets info about the terrain on a given position
		/// </summary>
		public HeightMapInfo GetTerrainInfo (Vector3 position) {
			return GetHeightMapInfoFast (position.x, position.z);
		}


		/// <summary>
		/// Gets the computed light amount at a given position in 0..1 range
		/// </summary>
		/// <returns>The light intensity.</returns>
		public float GetVoxelLight (Vector3 position) {
			VoxelChunk chunk;
			int voxelIndex;
			return GetVoxelLight (position, out chunk, out voxelIndex);
		}


		/// <summary>
		/// Gets the computed light amount at a given position in 0..1 range
		/// </summary>
		/// <returns>The light intensity at the voxel position..</returns>
		public float GetVoxelLight (Vector3 position, out VoxelChunk chunk, out int voxelIndex) {
			chunk = null;
			voxelIndex = 0;
			if (!globalIllumination) {
				return 1f;
			}

			if (GetVoxelIndex (position, out chunk, out voxelIndex, false)) {
				if (chunk.voxels [voxelIndex].opaque < FULL_OPAQUE) {
					return chunk.voxels [voxelIndex].lightMesh / 15f;
				}
				// voxel has contents try to retrieve light information from nearby voxels
				int nearby = voxelIndex + ONE_Y_ROW;
				if (nearby < chunk.voxels.Length && chunk.voxels [nearby].opaque < FULL_OPAQUE) {
					return chunk.voxels [nearby].lightMesh / 15f;
				}
				nearby = voxelIndex - ONE_Y_ROW;
				if (nearby >= 0 && chunk.voxels [nearby].opaque < FULL_OPAQUE) {
					return chunk.voxels [nearby].lightMesh / 15f;
				}
				return chunk.voxels [voxelIndex].lightMesh / 15f;
			}

			// Estimate light by height
			float height = GetTerrainHeight (position, false);
			if (height >= position.y) { // is below surface, we assume a lightIntensity of 0
				return 0;
			}
			return 1f;
		}


		/// <summary>
		/// Creates a placeholder for a given voxel.
		/// </summary>
		/// <returns>The voxel placeholder.</returns>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		/// <param name="createIfNotExists">If set to <c>true</c> create if not exists.</param>
		public VoxelPlaceholder GetVoxelPlaceholder (VoxelChunk chunk, int voxelIndex, bool createIfNotExists = true) {
			if (chunk == null)
				return null;

			bool phArrayCreated = (object)chunk.placeholders != null;

			if (phArrayCreated) {
				int count = chunk.placeholders.Count;
				for (int k = 0; k < count; k++) {
					VoxelPlaceholder ph = chunk.placeholders [k];
					if (ph.voxelIndex == voxelIndex) {
						return  ph;
					}
				}
			}

			// Create placeholder
			if (createIfNotExists) {
				if (!phArrayCreated) {
					chunk.placeholders = new List<VoxelPlaceholder> ();
				}
				GameObject placeholderGO = new GameObject ("Voxel Placeholder");
				placeholderGO.transform.SetParent (chunk.transform, false);
				placeholderGO.transform.localPosition = GetVoxelChunkPosition (voxelIndex);
				VoxelPlaceholder placeholder = placeholderGO.AddComponent<VoxelPlaceholder> ();
				placeholder.chunk = chunk;
				placeholder.voxelIndex = voxelIndex;
				placeholder.bounds.center = Misc.vector3zero;
				placeholder.bounds.size = Misc.vector3one;
				if (chunk.voxels [voxelIndex].hasContent == 1) {
					placeholder.resistancePointsLeft = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex].resistancePoints;
					VoxelDefinition voxelDefinition = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];

					// Custom rotation
					Vector3 customSavedAngles;
					Vector3 position = placeholderGO.transform.localPosition + chunk.position;
					if (saveVoxelCustomRotations.TryGetValue (position, out customSavedAngles)) {
						// From saved game
						saveVoxelCustomRotations.Remove (position);
						placeholder.transform.localRotation = Quaternion.Euler (customSavedAngles);
					} else {
						placeholder.transform.localRotation = voxelDefinition.GetRotation (position);
					}

					// Bounds for highlighting
					Mesh mesh = voxelDefinition.mesh;
					if (mesh != null) {
						Bounds bounds = mesh.bounds;
						bounds.size = new Vector3 (bounds.size.x * voxelDefinition.scale.x, bounds.size.y * voxelDefinition.scale.y, bounds.size.z * voxelDefinition.scale.z);
						placeholder.bounds = bounds;
					}

					if (!voxelDefinition.isDynamic) {
						// User rotation
						float rot = chunk.voxels [voxelIndex].GetTextureRotationDegrees ();
						if (rot != 0) {
							placeholder.transform.localRotation *= Quaternion.Euler (0, rot, 0);
						}

						// Custom position
						placeholderGO.transform.localPosition += placeholder.transform.TransformDirection (voxelDefinition.GetOffset (position));
					}
				} 
				chunk.placeholders.Add (placeholder);
				return placeholder;
			} 

			return null;
		}

		/// <summary>
		/// Destroys a voxel placeholder
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		public void VoxelPlaceholderDestroy (VoxelChunk chunk, int voxelIndex) {
			
			if (chunk == null)
				return;

			bool phArrayCreated = (object)chunk.placeholders != null;

			if (phArrayCreated) {
				int count = chunk.placeholders.Count;
				for (int k = 0; k < count; k++) {
					VoxelPlaceholder ph = chunk.placeholders [k];
					if (ph.voxelIndex == voxelIndex) {
						DestroyImmediate (ph.gameObject);
						chunk.placeholders.RemoveAt (k);
						return;
					}
				}
			}
		}


		/// <summary>
		/// Array with all available item definitions in current world
		/// </summary>
		[NonSerialized]
		public List<InventoryItem> allItems;


		/// <summary>
		/// Places a model defined by a matrix of colors in the world at the given position. Colors with zero alpha will be skipped.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="colors">3-dimensional array of colors (y/z/x).</param>
		public void ModelPlace (Vector3 position, Color[,,] colors) {
			#if UNITY_EDITOR
			CheckEditorTintColor ();
			#endif

			Vector3 pos;
			VoxelChunk lastChunk = null;
			int maxY = colors.GetUpperBound (0) + 1;
			int maxZ = colors.GetUpperBound (1) + 1;
			int maxX = colors.GetUpperBound (2) + 1;
			int halfZ = maxZ / 2;
			int halfX = maxX / 2;
			VoxelDefinition vd = defaultVoxel;
			for (int y = 0; y < maxY; y++) {
				pos.y = position.y + y;
				for (int z = 0; z < maxZ; z++) {
					pos.z = position.z + z - halfZ;
					for (int x = 0; x < maxX; x++) {
						Color32 color = colors [y, z, x];
						if (color.a == 0)
							continue;
						pos.x = position.x + x - halfX;
						VoxelChunk chunk;
						int voxelIndex;
						if (GetVoxelIndex (pos, out chunk, out voxelIndex)) {
							chunk.voxels [voxelIndex].Set (vd, color);
							if (chunk != lastChunk) {
								lastChunk = chunk;
								chunk.modified = true;
								if (!lastChunk.inqueue) {
									ChunkRequestRefresh (lastChunk, true, true);
								}
							}
						}
					}
				}
			}
		}


		/// <summary>
		/// Places a model defined by a matrix of colors in the world at the given position. Colors with zero alpha will be skipped.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="voxels">3-dimensional array of voxel definitions (y/z/x).</param>
		/// <param name="colors">3-dimensional array of colors (y/z/x).</param>
		public void ModelPlace (Vector3 position, VoxelDefinition[,,] voxels, Color[,,] colors = null) {
			Vector3 pos;
			VoxelChunk lastChunk = null;
			int maxY = voxels.GetUpperBound (0) + 1;
			int maxZ = voxels.GetUpperBound (1) + 1;
			int maxX = voxels.GetUpperBound (2) + 1;
			int halfZ = maxZ / 2;
			int halfX = maxX / 2;
			bool hasColors = colors != null;
			if (hasColors) {
				#if UNITY_EDITOR
				CheckEditorTintColor ();
				#endif
				if (colors.GetUpperBound (0) != voxels.GetUpperBound (0) || colors.GetUpperBound (1) != voxels.GetUpperBound (1) || colors.GetUpperBound (2) != voxels.GetUpperBound (2)) {
					Debug.LogError ("Colors array dimensions must match those of voxels array.");
					return;
				}
			}
			for (int y = 0; y < maxY; y++) {
				pos.y = position.y + y;
				for (int z = 0; z < maxZ; z++) {
					pos.z = position.z + z - halfZ;
					for (int x = 0; x < maxX; x++) {
						VoxelDefinition vd = voxels [y, z, x];
						if (vd != null) {
							pos.x = position.x + x - halfX;
							VoxelChunk chunk;
							int voxelIndex;
							if (GetVoxelIndex (pos, out chunk, out voxelIndex)) {
								if (hasColors) {
									chunk.voxels [voxelIndex].Set (vd, colors [y, z, x]);
								} else {
									chunk.voxels [voxelIndex].Set (vd);
								}
								if (chunk != lastChunk) {
									lastChunk = chunk;
									chunk.modified = true;
									if (!lastChunk.inqueue) {
										ChunkRequestRefresh (lastChunk, true, true);
									}
								}
							}
						}
					}
				}
			}
		}


		/// <summary>
		/// Places a list of voxels given by a list of ModelBits within a given chunk
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="bits">List of voxels described by a list of modelbit structs</param>
		/// <param name="indices">Optional user-provided list. Will contain the list of visible voxels if provided.</param>
		public void ModelPlace (VoxelChunk chunk, List<ModelBit>bits) {
			int count = bits.Count;
			for (int b = 0; b < count; b++) {
				int bitIndex = bits [b].voxelIndex;
				VoxelDefinition vd = bits [b].voxelDefinition ?? defaultVoxel;
				chunk.voxels [bitIndex].Set (vd, bits [b].colorOrDefault);
			}
			chunk.modified = true;
			ChunkRequestRefresh (chunk, true, true);
		}


		/// <summary>
		/// Places a model in the world at the given position
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="model">Model Definition.</param>
		/// <param name="rotationDegrees">0, 90, 180 or 270 degree rotation. A value of 360 means random rotation.</param>
		/// <param name="colorBrightness">Optionally pass a color brightness value. This value is multiplied by the voxel color.</param>
		/// <param name="fitTerrain">If set to true, vegetation and trees are prevented and some space is flattened around the model.</param>
		/// <param name="indices">Optional user-provided list. If provided, it will contain the indices and positions of all visible voxels in the model</param>
		public void ModelPlace (Vector3 position, ModelDefinition model, int rotationDegrees = 0, float colorBrightness = 1f, bool fitTerrain = false, List<VoxelIndex> indices = null) {

			if (model == null)
				return;

			Vector3 pos;
			int modelOneYRow = model.sizeZ * model.sizeX;
			int modelOneZRow = model.sizeX;
			int halfSizeX = model.sizeX / 2;
			int halfSizeZ = model.sizeZ / 2;

			if (rotationDegrees == 360) {
				switch (UnityEngine.Random.Range (0, 4)) {
				case 0:
					rotationDegrees = 90;
					break;
				case 1:
					rotationDegrees = 180;
					break;
				case 2:
					rotationDegrees = 270;
					break;
				}
			}

			bool indicesProvided = indices != null;
			if (indicesProvided)
				indices.Clear ();
			VoxelIndex index = new VoxelIndex ();
			VoxelChunk lastChunk = null;
			int tmp;
			for (int b = 0; b < model.bits.Length; b++) {
				int bitIndex = model.bits [b].voxelIndex;
				int py = bitIndex / modelOneYRow;
				int remy = bitIndex - py * modelOneYRow;
				int pz = remy / modelOneZRow;
				int px = remy - pz * modelOneZRow;
				switch (rotationDegrees) {
				case 90:
					tmp = px;
					px = halfSizeZ - pz;
					pz = tmp - halfSizeX;
					break;
				case 180:
					tmp = px;
					px = pz - halfSizeZ;
					pz = tmp - halfSizeX;
					break;
				case 270:
					tmp = px;
					px = pz - halfSizeZ;
					pz = halfSizeX - tmp;
					break;
				default:
					px -= halfSizeX;
					pz -= halfSizeZ;
					break;
				}

				pos.x = position.x + model.offsetX + px;
				pos.y = position.y + model.offsetY + py;
				pos.z = position.z + model.offsetZ + pz;

				VoxelChunk chunk;
				int voxelIndex;
				if (GetVoxelIndex (pos, out chunk, out voxelIndex)) {
					Color32 color = model.bits [b].colorOrDefault;
					VoxelDefinition vd = model.bits [b].voxelDefinition ?? defaultVoxel;
					bool emptyVoxel = model.bits [b].isEmpty;
					if (emptyVoxel) {
						chunk.voxels [voxelIndex] = Voxel.Empty;
					} else {
						if (colorBrightness != 1f) {
							color.r = (byte)(color.r * colorBrightness);
							color.g = (byte)(color.g * colorBrightness);
							color.b = (byte)(color.b * colorBrightness);
						}
						chunk.voxels [voxelIndex].Set (vd, color);
						if (indicesProvided) {
							index.chunk = chunk;
							index.voxelIndex = voxelIndex;
							index.position = pos;
							indices.Add (index);
						}
					}

					// Prevent tree population
					chunk.allowTrees = false;
					chunk.modified = true;

					if (fitTerrain && !emptyVoxel) {
						// Fill beneath row 1
						if (py == 0) {
							Vector3 under = pos;
							under.y -= 1;
							for (int k = 0; k < 100; k++, under.y--) {
								VoxelChunk lowChunk;
								int vindex;
								GetVoxelIndex (under, out lowChunk, out vindex, false);
								if (lowChunk != null && lowChunk.voxels [vindex].opaque < FULL_OPAQUE) {
									lowChunk.voxels [vindex].Set (vd, color);
									if (lowChunk != lastChunk) {
										lastChunk = lowChunk;
										if (!lastChunk.inqueue) {
											ChunkRequestRefresh (lastChunk, true, true);
										}
									}
								} else {
									break;
								}
							}
						}
					}

					if (chunk != lastChunk) {
						lastChunk = chunk;
						if (!lastChunk.inqueue) {
							lastChunk.MarkAsInconclusive ();
							ChunkRequestRefresh (lastChunk, true, true);
						}
					}
				}
			}
		}


		/// <summary>
		/// Fills empty voxels inside the model with Empty blocks so they clear any other existing voxel when placing the model
		/// </summary>
		/// <param name="model">Model.</param>
		public void ModelFillInside (ModelDefinition model) {

			if (model == null)
				return;
			int sx = model.sizeX;
			int sy = model.sizeY;
			int sz = model.sizeZ;
			Voxel[] voxels = new Voxel[sy * sz * sx];

			// Load model inside the temporary voxel array
			for (int k = 0; k < model.bits.Length; k++) {
				int voxelIndex = model.bits [k].voxelIndex;
				voxels [voxelIndex].hasContent = 1;
			}

			// Fill inside
			List<ModelBit> newBits = new List<ModelBit> (model.bits);
			ModelBit empty = new ModelBit ();
			empty.isEmpty = true;
			for (int z = 0; z < sz; z++) {
				for (int x = 0; x < sx; x++) {
					int miny = -1;
					int maxy = sy;
					for (int y = 0; y < sy; y++) {
						int voxelIndex = y * sz * sx + z * sx + x;
						if (voxels [voxelIndex].hasContent == 1) {
							if (miny < 0)
								miny = y;
							else
								maxy = y;
						}
					}
					if (miny >= 0) {
						miny++;
						maxy--;
						for (int y = miny; y < maxy; y++) {
							int voxelIndex = y * sz * sx + z * sx + x;
							if (voxels [voxelIndex].hasContent != 1) {
								empty.voxelIndex = voxelIndex;
								newBits.Add (empty);
							}
						}
					}
				}
			}
			model.bits = newBits.ToArray ();
		}


		/// <summary>
		/// Reloads all world textures
		/// </summary>
		public void ReloadTextures () {
			LoadWorldTextures ();
		}


		/// <summary>
		/// Sets or cancel build mode
		/// </summary>
		/// <param name="buildMode">If set to <c>true</c> build mode.</param>
		public void SetBuildMode (bool buildMode) {
			if (!enableBuildMode)
				return;
			this.buildMode = buildMode;

			// select first item
			VoxelPlayPlayer.instance.selectedItemIndex = 0;

			// refresh inventory
			VoxelPlayUI.instance.RefreshInventoryContents ();
		}

		/// <summary>
		/// Shows a custom message into the status text.
		/// </summary>
		/// <param name="txt">Text.</param>
		public void ShowMessage (string txt, float displayDuration = 4, bool flashEffect = false) {
			if (lastMessage == txt)
				return;
			lastMessage = txt;

			ExecuteInMainThread (delegate () {
				VoxelPlayUI.instance.AddMessage (txt, displayDuration, flashEffect);
			});
		}


		/// <summary>
		/// Get an item from the allItems array of a given category and voxel type
		/// </summary>
		/// <returns>The item of requested category and type.</returns>
		/// <param name="category">Category.</param>
		/// <param name="voxelType">Voxel type.</param>
		public ItemDefinition GetItemByType (ItemCategory category, VoxelDefinition voxelType = null) {
			if (allItems == null)
				return null;
			int allItemsCount = allItems.Count;
			for (int k = 0; k < allItemsCount; k++) {
				if (allItems [k].item.category == category && (allItems [k].item.voxelType == voxelType || voxelType == null)) {
					return allItems [k].item;
				}
			}
			return null;
		}


		/// <summary>
		/// Adds a torch.
		/// </summary>
		/// <param name="hitInfo">Information about the hit position where the torch should be attached.</param>
		public GameObject TorchAttach (VoxelHitInfo hitInfo) {
			return TorchAttachInt (hitInfo);
		}

		/// <summary>
		/// Removes an existing torch.
		/// </summary>
		/// <param name="chunk">Chunk where the torch is currently attached.</param>
		/// <param name="gameObject">Game object of the torch itself.</param>
		public void TorchDetach (VoxelChunk chunk, GameObject gameObject) {
			TorchDetachInt (chunk, gameObject);
		}


		/// <summary>
		/// Forces an update of the light buffers. Useful if you place point lights manually in the scene.
		/// </summary>
		public void UpdateLights () {
			if (OnLightRefreshRequest != null) {
				OnLightRefreshRequest ();
			}
		}

		/// <summary>
		/// Voxel Play Input manager reference.
		/// </summary>
		[NonSerialized]
		public VoxelPlayInputController input;

		#endregion

	}

}