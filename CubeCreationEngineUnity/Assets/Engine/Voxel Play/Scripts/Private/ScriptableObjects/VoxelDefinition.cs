using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public enum RenderType : byte {
		Opaque = 0,
		Cutout = 1,
		Water = 2,
		CutoutCross = 3,
		OpaqueNoAO = 4,
		Custom = 5,
		Opaque6tex = 6,
		Transp6tex = 7,
		Empty = 8
	}

	public static class RenderTypeExtensions {
		public static bool supportsNavigation (this RenderType o) {
			return o == RenderType.Opaque || o == RenderType.Cutout || o == RenderType.OpaqueNoAO || o == RenderType.Opaque6tex || o == RenderType.Transp6tex || o == RenderType.Empty;
		}

		public static bool supportsWindAnimation (this RenderType o) {
			return o == RenderType.Cutout;
		}

		public static int numberOfTextures (this RenderType o) {
			if (o == RenderType.Empty)
				return 0;
			return (o == RenderType.Opaque6tex || o == RenderType.Transp6tex) ? 6 : 3;
		}

		public static bool supportsTextureRotation (this RenderType o) {
			// custom types can always be rotated
			return   o == RenderType.Opaque6tex || o == RenderType.Transp6tex || o == RenderType.Custom;
		}

		public static bool supportsEmission (this RenderType o) {
			return o == RenderType.Opaque || o == RenderType.Opaque6tex;
		}

		public static bool supportsDynamic(this RenderType o) {
			return o == RenderType.Opaque || o == RenderType.Opaque6tex || o == RenderType.OpaqueNoAO || o == RenderType.Cutout || o == RenderType.Custom || o == RenderType.Transp6tex;
		}
	}

	public struct TextureRotationIndices {
		public int forward, right, back, left;
		public Vector4 xyzw;
	}


	[CreateAssetMenu (menuName = "Voxel Play/Voxel Definition", fileName = "VoxelDefinition", order = 101)]
	[HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001917-voxels-and-voxel-definitions")]
	public partial class VoxelDefinition : ScriptableObject {

		[Tooltip ("Name to show in the UI")]
		public string title;

		public RenderType renderType = RenderType.Opaque;

		[Tooltip("Set this value to 15 to specify that this is a fully solid object that occludes other adjacent voxels.")]
		[Range(0,15)]
		public byte opaque;

		[Tooltip ("Texture of the voxel Top side")]
		public Texture2D textureTop;
		[Tooltip ("Emission map")]
		public Texture2D textureTopEmission;
		[Tooltip ("Normal map")]
		public Texture2D textureTopNRM;
		[Tooltip ("Displacement")]
		public Texture2D textureTopDISP;

		[Tooltip ("Texture for all voxel sides or Back texture if voxel has 6 textures")]
		public Texture2D textureSide;
		[Tooltip ("Emission map")]
		public Texture2D textureSideEmission;
		[Tooltip ("Normal map")]
		public Texture2D textureSideNRM;
		[Tooltip ("Displacement map")]
		public Texture2D textureSideDISP;

		[Tooltip ("Texture for voxel's Right face")]
		public Texture2D textureRight;
		[Tooltip ("Emission map")]
		public Texture2D textureRightEmission;
		[Tooltip ("Normal map")]
		public Texture2D textureRightNRM;
		[Tooltip ("Displacement map")]
		public Texture2D textureRightDISP;

		[Tooltip ("Texture for voxel's Forward face")]
		public Texture2D textureForward;
		[Tooltip ("Emission map")]
		public Texture2D textureForwardEmission;
		[Tooltip ("Normal map")]
		public Texture2D textureForwardNRM;
		[Tooltip ("Displacement map")]
		public Texture2D textureForwardDISP;

		[Tooltip ("Texture for voxel's Left face")]
		public Texture2D textureLeft;
		[Tooltip ("Emission map")]
		public Texture2D textureLeftEmission;
		[Tooltip ("Normal map")]
		public Texture2D textureLeftNRM;
		[Tooltip ("Displacement map")]
		public Texture2D textureLeftDISP;

		[Tooltip ("Texture for the voxel Bottom side")]
		public Texture2D textureBottom;
		[Tooltip ("Emission map")]
		public Texture2D textureBottomEmission;
		[Tooltip ("Normal map")]
		public Texture2D textureBottomNRM;
		[Tooltip ("Displacement map")]
		public Texture2D textureBottomDISP;

		[Tooltip ("Default tint color")]
		public Color32 tintColor = Misc.color32White;

		[Tooltip ("Grass/tree leaves color variation")]
		[Range (0, 2f)]
		public float colorVariation = 0.5f;

		[Tooltip ("Sound played when voxel is picked up")]
		public AudioClip pickupSound;

		[Tooltip ("Sound produced when this voxel is placed in the scene")]
		public AudioClip buildSound;

		[Tooltip ("Sounds of the footsteps")]
		public AudioClip[] footfalls;

		[Tooltip ("Sound produced when player jumps from this voxel")]
		public AudioClip jumpSound;

		[Tooltip ("Sound produced after jumping and landing over this voxel")]
		public AudioClip landingSound;

		[Tooltip ("Voxel hit sound")]
		public AudioClip impactSound;

		[Tooltip ("Voxel destruction sound")]
		public AudioClip destructionSound;

		[Range (0, 255)]
		[Tooltip ("Resistance points. 0 means it cannot be hit. 255 means it's indestructible.")]
		public byte resistancePoints = 15;

		// used for vegetation particles; extracted dynamically from texture
		[NonSerialized, HideInInspector]
		public Color sampleColor;

		[Tooltip ("If this voxel type can't be shown in the inventory.")]
		public bool hidden;

		[Tooltip ("If this voxel type can be collected by the user after breaking it.")]
		public bool canBeCollected = true;

		[Tooltip ("The item dropped when this voxel is destroyed. If null, a default item of the same voxel type will be used.")]
		public ItemDefinition dropItem;

		[Tooltip("Texture used for the inventory panel. If omitted, it will use the side texture.")]
		public Texture2D icon;

		[Tooltip ("If this voxel type can be included in NavMesh navigation. All opaque voxels are by default included in any generated NavMesh but you can exclude this voxel type by setting this property to false.")]
		public bool navigatable = true;

		[Tooltip ("Cutout voxel types can be animated if this property is set to true.")]
		public bool windAnimation = true;

		[Tooltip ("The model to render in this voxel space.")]
		public GameObject model;

		[Tooltip ("Use GPU instancing to render this voxel type.")]
		public bool gpuInstancing;

		[Tooltip ("If this instanced voxel can cast shadows.")]
		public bool castShadows = true;

		[Tooltip ("If the instanced voxel can receive shadows.")]
		public bool receiveShadows = true;

		[Tooltip ("When GPU instancing is enabled, the rendering will be done in GPU but you can still force the creation of a gameobject which can hold colliders or custom scripts.")]
		public bool createGameObject;

		[Tooltip ("Optional displacement of the voxel.")]
		public Vector3 offset;

		[Tooltip("If offset should be modified randomly to create variation")]
		public bool offsetRandom;

		[Tooltip("Random offset range in X/Y/Z")]
		public Vector3 offsetRandomRange;

		[Tooltip ("Optional scale of the voxel. Note that this scale is multiplied by the gameobject transform scale.")]
		public Vector3 scale = Misc.vector3one;

		[Tooltip ("Optional rotation of the voxel in degrees.")]
		public Vector3 rotation;

		[Tooltip("Random rotataion around Y-Axis")]
		public bool rotationRandomY;

		[Tooltip ("Voxel definition to use when this voxel is placed twice on the same position.")]
		public VoxelDefinition promotesTo;

		[Tooltip ("If this voxel propagates (eg. water)")]
		public bool spreads;

		[Tooltip ("If this voxel reduces its amount when it propagates (eg. sea water do not drain, acting like an infinite source of water but water in player's inventory should drain as it spreads)")]
		public bool drains = true;

		[Tooltip ("Delay for propagation in seconds")]
		public float spreadDelay;

		[Tooltip ("Additional random delay for propagation")]
		public float spreadDelayRandom;

		[ColorUsage (true)]
		public Color diveColor = new Color (0, 0.41f, 0.63f, 0.83f);

		[Range (0, 15), Tooltip ("Default block level")]
		public byte height = 13;

		[Range (0, 255), Tooltip ("Damage caused to the player")]
		public byte playerDamage;

		[Range (0, 255), Tooltip ("Time interval in seconds between damage caused to the player")]
		public float playerDamageDelay = 2f;

		[Tooltip ("If this voxel is destroyed, connected voxels on same level or upper levels with triggerCollapse flag will fall.")]
		public bool triggerCollapse;

		[Tooltip ("If a nearby voxel is destroyed, this voxel will collapse and fall.")]
		public bool willCollapse;

		[Tooltip ("Ignores raycast on this voxel type (ie. air voxels)")]
		public bool ignoresRayCast;

		[Tooltip ("If texture rotation is allowed for this voxel type.")]
		public bool allowsTextureRotation = true;

		[Tooltip ("When placing this voxel, orient it to the player facing direction. Only applies to voxels with 6 textures.")]
		public bool placeFacingPlayer = true;

		[Tooltip ("If entering this voxel raises a OnVoxelEnter event by FPS controllers.")]
		public bool triggerEnterEvent;

		[Tooltip ("If walking over this voxel raises a OnVoxelWalk event by FPS controllers.")]
		public bool triggerWalkEvent;

		public bool showFoam = true;

		/// <summary>
		/// Temporary/session-related data
		/// </summary>
		[HideInInspector, NonSerialized]
		public int textureIndexSide, textureIndexTop, textureIndexBottom;

		// indices for rotated textures
		[HideInInspector, NonSerialized]
		public TextureRotationIndices[] textureSideIndices;

		// index in voxelDefinitions list when it's added
		[NonSerialized]
		public ushort index;

		// The related dynamic voxel definition. This field is only set when a static voxel is converted to dynamic (only set once per type)
		[NonSerialized]
		public VoxelDefinition dynamicDefinition;

		// The related static voxel definition. Thsi field is set so when a dynamic voxel is converted back to static, it can know which static type belongs to
		[NonSerialized]
		public VoxelDefinition staticDefinition;

		// if this voxel definition is dynamic
		[NonSerialized]
		public bool isDynamic;

		// Used to cache dynamic voxel meshes
		[NonSerialized]
		public Dictionary<Color, Mesh> dynamicMeshes;

		[NonSerialized]
		public Texture2D textureThumbnailTop, textureThumbnailSide, textureThumbnailBottom;

		// if this voxel is used in a biome as the surface, this field points to the dirt voxel
		[NonSerialized]
		public VoxelDefinition biomeDirtCounterpart;

		// look-up index for batched mesh array in GPU instancing
		[NonSerialized]
		public int batchedIndex = -1;

		// mesh used by the prefab
		[NonSerialized]
		Mesh _mesh;
		public Mesh mesh {
			get {
				if (_mesh == null && model != null) {
					MeshFilter mf = model.GetComponent<MeshFilter> ();
					if (mf != null)
						_mesh = mf.sharedMesh;
				}
				return _mesh;
			}
		}

		// material used by the prefab
		[NonSerialized]
		Material _material;
		public Material material {
			get {
				if (_material == null && model != null) {
					MeshRenderer mr = model.GetComponent<MeshRenderer> ();
					if (mr != null)
						_material = mr.sharedMaterial;
				}
				return _material;
			}
		}

		// if the model has collider and the gameobject is being created.
		[NonSerialized]
		public bool modelUsesCollider;


		public Quaternion GetRotation(Vector3 position) {
			Vector3 rot = rotation;
			if (rotationRandomY) {
				rot.y += WorldRand.GetValue (position) * 360f;
			}
			return Quaternion.Euler (rot);
		}

		public Vector3 GetOffset(Vector3 position) {
			Vector3 shiftedOffset = offset;
			if (offsetRandom) {
				shiftedOffset += WorldRand.GetVector3 (position, offsetRandomRange, -0.5f);
			}
			return shiftedOffset;
		}


		/// <summary>
		/// The texture used for the inventory
		/// </summary>
		/// <value>The icon.</value>
		public Texture2D GetIcon() {
			if (icon != null)
				return icon;
			if (textureThumbnailSide != null) {
				return textureThumbnailSide;
			}
			return textureSide;
		}

		/// <summary>
		/// Clears temporary/session non-serializable fields
		/// </summary>
		public void Reset() {
			index = 0;
			dynamicDefinition = null;
			dynamicMeshes = null;
			batchedIndex = -1;
		}

	}

}