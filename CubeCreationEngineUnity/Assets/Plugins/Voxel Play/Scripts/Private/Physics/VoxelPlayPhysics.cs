using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//#define DEBUG_RAYCAST

namespace VoxelPlay {

	public enum COLLIDER_TYPES {
		OnlyVoxels = 0,
		AnyCollider = 1
	}


	public struct VoxelHitInfo {

		/// <summary>
		/// The world space position of the ray hit
		/// </summary>
		public Vector3 point;


		float lastSqrDistance;
		float computedDistance;

		/// <summary>
		/// Distance to the hit position
		/// </summary>
		public float distance {
			get {
				if (lastSqrDistance != sqrDistance) {
					computedDistance = Mathf.Sqrt (sqrDistance);
				}
				return computedDistance;
			}
			set {
				computedDistance = value;
				sqrDistance = computedDistance * computedDistance;
				lastSqrDistance = sqrDistance;
			}
		}


		/// <summary>
		/// Squared distance (distance * distance) to the hit position
		/// </summary>
		public float sqrDistance;

		/// <summary>
		/// The index of the voxel being hit in the chunk.voxels array
		/// </summary>
		public int voxelIndex;

		/// <summary>
		/// The chunk to which the voxel belongs to
		/// </summary>
		public VoxelChunk chunk;

		/// <summary>
		/// The center of the voxel in world space coordinates
		/// </summary>
		public Vector3 voxelCenter;

		/// <summary>
		/// The normal of the side of the voxel being hit
		/// </summary>
		public Vector3 normal;

		/// <summary>
		/// Convenience property that returns a copy of the voxel hit
		/// </summary>
		/// <value>The voxel.</value>
		public Voxel voxel;

		/// <summary>
		/// The collider of the gameobject which is hit by the ray
		/// </summary>
		public Collider collider;

		/// <summary>
		/// If the voxel hit has a placeholder attached, a reference to it is returned here
		/// </summary>
		public VoxelPlaceholder placeholder;
	}

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		[HideInInspector]
		public Transform fxRoot;

		struct ParticlePoolEntry {
			public bool used;
			public Renderer renderer;
			public Rigidbody rigidBody;
			public BoxCollider collider;
			public Item item;
			public float destructionTime;
			public int lastX, lastY, lastZ;
		}

		const string VM_FX_ROOT = "VMFX Root";
		const int MAX_PARTICLES = 500;
		const int OWNER_PARTICLE = 0;
		const int OWNER_FLOATING_VOXEL = -1;

		const string DAMAGE_INDICATOR = "DamageIndicator";
		GameObject damagedVoxelPrefab, damageParticlePrefab;
		ParticlePoolEntry[] particlePool;
		int particlePoolCurrentIndex;
		bool shouldUpdateParticlesLighting;
		List<VoxelIndex> tempVoxelIndices;
		Dictionary<Vector3, bool> tempVoxelPositions;
		int tempVoxelIndicesCount;

		void InitPhysics () {

			Transform t = transform.Find (VM_FX_ROOT);
			if (t != null) {
				DestroyImmediate (t.gameObject);
			}

			GameObject fx = new GameObject (VM_FX_ROOT);
			fx.hideFlags = HideFlags.DontSave;
			fxRoot = fx.transform;
			fxRoot.hierarchyCapacity = 100;
			fxRoot.SetParent (worldRoot, false);

			if (damageParticlePrefab == null) {
				damageParticlePrefab = Resources.Load<GameObject> ("VoxelPlay/Prefabs/DamageParticle");
			}

			if (particlePool == null) {
				particlePool = new ParticlePoolEntry[MAX_PARTICLES];
				for (int k = 0; k < MAX_PARTICLES; k++) {
					int i = GetParticleFromPool ();
					ReleaseParticle (i);
				}
			}
			particlePoolCurrentIndex = -1;
			for (int k = 0; k < MAX_PARTICLES; k++) {
				particlePool [k].used = false;
			}
			if (tempVoxelIndices == null) {
				tempVoxelIndices = new List<VoxelIndex> (100);
			} else {
				tempVoxelIndices.Clear ();
			}
			if (tempVoxelPositions == null) {
				tempVoxelPositions = new Dictionary<Vector3, bool> (100);
			} else {
				tempVoxelPositions.Clear ();
			}

			if (layerVoxels == layerParticles) {
				layerParticles = layerVoxels + 1;
			}
			Physics.IgnoreLayerCollision (layerParticles, layerParticles);
			Physics.IgnoreLayerCollision (layerVoxels, layerVoxels);
		}


		bool RayCastFast (Vector3 origin, Vector3 direction, out VoxelHitInfo hitInfo, float maxDistance = 0, bool createChunksIfNeeded = false, byte minOpaque = 0, COLLIDER_TYPES colliderTypes = COLLIDER_TYPES.AnyCollider) {

			bool voxelHit = RayCastFastVoxel (origin, direction, out hitInfo, maxDistance, createChunksIfNeeded, minOpaque);
			if (colliderTypes == COLLIDER_TYPES.OnlyVoxels) {
				return voxelHit;
			}

			if (voxelHit) {
				maxDistance = hitInfo.distance - 0.1f;
			}
			// Cast a normal raycast to detect normal gameobjects within ray
			RaycastHit hit;
			if (Physics.Raycast (origin + 0.3f * direction, direction, out hit, maxDistance - 0.3f)) {
				hitInfo.distance = hit.distance;
				hitInfo.point = hit.point;
				hitInfo.normal = hit.normal;
				hitInfo.collider = hit.collider;

				// Check if gameobject is a dynamic voxel
				if (hit.collider != null) {
					VoxelPlaceholder placeholder = hit.collider.GetComponentInParent<VoxelPlaceholder> ();
					if (placeholder != null) {
						hitInfo.chunk = placeholder.chunk;
						hitInfo.voxelIndex = placeholder.voxelIndex;
						hitInfo.voxel = placeholder.chunk.voxels [placeholder.voxelIndex];
						hitInfo.voxelCenter = placeholder.transform.position;
						hitInfo.placeholder = placeholder;
					}
				}
				return true;
			} else {
				return voxelHit;
			}

		}

		VoxelChunk RayCastFastVoxel (Vector3 origin, Vector3 direction, out VoxelHitInfo hitInfo, float maxDistance = 0, bool createChunksIfNeeded = false, byte minOpaque = 0) {

#if DEBUG_RAYCAST
												GameObject o;
#endif

			float maxDistanceSqr = maxDistance == 0 ? 1000 * 1000 : maxDistance * maxDistance;

			// Ray march throuch chunks until hit one loaded chunk
			Vector3 position = origin;
			VoxelChunk chunk = null;
			hitInfo = new VoxelHitInfo ();
			hitInfo.voxelIndex = -1;

			Vector3 viewDirSign = new Vector3 (Mathf.Sign (direction.x), Mathf.Sign (direction.y), Mathf.Sign (direction.z));
			Vector3 viewSign = (viewDirSign + Misc.vector3one) * 0.5f; // 0 = left, 1 = right

			float vxz, vzy, vxy;
			if (direction.y != 0) {
				float a = direction.x / direction.y;
				float b = direction.z / direction.y;
				vxz = Mathf.Sqrt (1f + a * a + b * b);
			} else {
				vxz = 1000000f;
			}
			if (direction.x != 0) {
				float a = direction.z / direction.x;
				float b = direction.y / direction.x;
				vzy = Mathf.Sqrt (1f + a * a + b * b);
			} else {
				vzy = 1000000f;
			}
			if (direction.z != 0) {
				float a = direction.x / direction.z;
				float b = direction.y / direction.z;
				vxy = Mathf.Sqrt (1f + a * a + b * b);
			} else {
				vxy = 1000000f;
			}

			Vector3 v3 = new Vector3 (vzy, vxz, vxy);
			Vector3 viewSign16 = viewSign * 16f;
			Vector3 viewDirSignOffset = viewDirSign * 0.002f;

			int chunkX, chunkY, chunkZ;
			int chunkCount = 0;
			float t;
			Vector3 normal = Misc.vector3zero, db;
//			bool notFirstVoxel = false;

			while (chunkCount++ < 500) { // safety counter to avoid any potential infinite loop

				// Check max distance
				float distSqr = (position.x - origin.x) * (position.x - origin.x) + (position.y - origin.y) * (position.y - origin.y) + (position.z - origin.z) * (position.z - origin.z);
				if (distSqr > maxDistanceSqr)
					return null;


#if DEBUG_RAYCAST
																o = GameObject.CreatePrimitive(PrimitiveType.Cube);
																o.transform.localScale = Misc.Vector3one * 0.15f;
																o.transform.position = position;
																DestroyImmediate(o.GetComponent<Collider>());
																o.GetComponent<Renderer>().material.color = Color.blue;
#endif

				chunkX = FastMath.FloorToInt (position.x / 16f);
				chunkY = FastMath.FloorToInt (position.y / 16f);
				chunkZ = FastMath.FloorToInt (position.z / 16f);

				chunk = null;
				if (createChunksIfNeeded) {
					chunk = GetChunkOrCreate (chunkX, chunkY, chunkZ);
				} else {
					int x00 = WORLD_SIZE_DEPTH * WORLD_SIZE_HEIGHT * (chunkX + WORLD_SIZE_WIDTH);
					int y00 = WORLD_SIZE_DEPTH * (chunkY + WORLD_SIZE_HEIGHT);
					int hash = x00 + y00 + chunkZ;
					chunk = GetChunkIfExists (hash);
				}

				chunkX *= 16;
				chunkY *= 16;
				chunkZ *= 16;

				if (chunk) {
					// Ray-march through chunk
					Voxel[] voxels = chunk.voxels;
					Vector3 inPosition = position;

					for (int k = 0; k < 64; k++) {

#if DEBUG_RAYCAST
																								o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
																								o.transform.localScale = Misc.Vector3one * 0.1f;
																								o.transform.position = inPosition;
																								DestroyImmediate(o.GetComponent<Collider>());
																								o.GetComponent<Renderer>().material.color = Color.yellow;
#endif

						// Check voxel content
						int fy = FastMath.FloorToInt (inPosition.y);
						int py = fy - chunkY;
						int fz = FastMath.FloorToInt (inPosition.z);
						int pz = fz - chunkZ;
						int fx = FastMath.FloorToInt (inPosition.x);
						int px = fx - chunkX;
						if (px < 0 || px > 15 || py < 0 || py > 15 || pz < 0 || pz > 15) {
							break;
						}

						int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
						if (voxels [voxelIndex].hasContent == 1 && !voxels [voxelIndex].type.ignoresRayCast && (minOpaque == 255 || voxels [voxelIndex].opaque >= minOpaque)) {

							VoxelDefinition vd = voxelDefinitions [voxels [voxelIndex].typeIndex];
							if (vd.renderType != RenderType.Custom || !vd.modelUsesCollider) {

								// Check max distance
								distSqr = (inPosition.x - origin.x) * (inPosition.x - origin.x) + (inPosition.y - origin.y) * (inPosition.y - origin.y) + (inPosition.z - origin.z) * (inPosition.z - origin.z);
								if (distSqr > maxDistanceSqr)
									return null;

								// Check water level or grass height
								float voxelHeight = 0;
								if (vd.renderType == RenderType.Water) {
									voxelHeight = voxels [voxelIndex].GetWaterLevel () / 15f;
								} else if (vd.renderType == RenderType.CutoutCross) {
									voxelHeight = vd.scale.y;
								}
								bool hit = true;
								Vector3 voxelCenter = new Vector3 (chunkX + px + 0.5f, chunkY + py + 0.5f, chunkZ + pz + 0.5f);
								Vector3 localHitPos = inPosition - voxelCenter;
								if (voxelHeight > 0 && voxelHeight < 1f && direction.y != 0) {
									t = localHitPos.y + 0.5f - voxelHeight;
									if (t > 0) {
										t = t * Mathf.Sqrt (1 + (direction.x * direction.x + direction.z * direction.z) / (direction.y * direction.y));
										localHitPos += t * direction;
										hit = localHitPos.x >= -0.5f && localHitPos.x <= 0.5f && localHitPos.z >= -0.5f && localHitPos.z <= 0.5f;
									}
								} 
								if (hit) {
									hitInfo = new VoxelHitInfo ();
									hitInfo.chunk = chunk;
									hitInfo.voxel = voxels [voxelIndex];
									hitInfo.point = inPosition - normal;
									hitInfo.distance = Mathf.Sqrt (distSqr);
									hitInfo.voxelIndex = voxelIndex;
									hitInfo.voxelCenter = voxelCenter;
									if (localHitPos.y >= 0.495) {
										hitInfo.normal = Misc.vector3up;
									} else if (localHitPos.y <= -0.495) {
										hitInfo.normal = Misc.vector3down;
									} else if (localHitPos.x < -0.495) {
										hitInfo.normal = Misc.vector3left;
									} else if (localHitPos.x > 0.495) {
										hitInfo.normal = Misc.vector3right;
									} else if (localHitPos.z < -0.495) {
										hitInfo.normal = Misc.vector3back;
									} else if (localHitPos.z > 0.495) {
										hitInfo.normal = Misc.vector3forward;
									}

#if DEBUG_RAYCAST
																												o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
																												o.transform.localScale = Misc.Vector3one * 0.15f;
																												o.transform.position = inPosition;
																												DestroyImmediate(o.GetComponent<Collider>());
																												o.GetComponent<Renderer>().material.color = Color.red;
#endif

									return chunk;
								}
							}
						}

						db.x = (fx + viewSign.x - inPosition.x) * v3.x;
						db.y = (fy + viewSign.y - inPosition.y) * v3.y;
						db.z = (fz + viewSign.z - inPosition.z) * v3.z;

						db.x = db.x < 0 ? -db.x : db.x;
						db.y = db.y < 0 ? -db.y : db.y;
						db.z = db.z < 0 ? -db.z : db.z;

						t = db.x;
						normal.x = viewDirSignOffset.x;
						normal.y = 0;
						normal.z = 0;
						if (db.y < t) {
							t = db.y;
							normal.x = 0;
							normal.y = viewDirSignOffset.y;
						}
						if (db.z < t) {
							t = db.z;
							normal.x = 0;
							normal.y = 0;
							normal.z = viewDirSignOffset.z;
						}

						inPosition.x += direction.x * t + normal.x;
						inPosition.y += direction.y * t + normal.y;
						inPosition.z += direction.z * t + normal.z;

//						notFirstVoxel = true;
					}
				}

				db.x = (chunkX + viewSign16.x - position.x) * v3.x;
				db.y = (chunkY + viewSign16.y - position.y) * v3.y;
				db.z = (chunkZ + viewSign16.z - position.z) * v3.z;

				db.x = db.x < 0 ? -db.x : db.x;
				db.y = db.y < 0 ? -db.y : db.y;
				db.z = db.z < 0 ? -db.z : db.z;

				t = db.x;
				normal.x = viewDirSignOffset.x;
				normal.y = 0;
				normal.z = 0;
				if (db.y < t) {
					t = db.y;
					normal.x = 0;
					normal.y = viewDirSignOffset.y;
				}
				if (db.z < t) {
					t = db.z;
					normal.x = 0;
					normal.y = 0;
					normal.z = viewDirSignOffset.z;
				}

				position.x += direction.x * t + normal.x;
				position.y += direction.y * t + normal.y;
				position.z += direction.z * t + normal.z;

//				notFirstVoxel = true;
			}
			return null;
		}

		bool HitVoxelFast (Vector3 origin, Vector3 direction, int damage, out VoxelHitInfo hitInfo, float maxDistance = 0, int damageRadius = 1, bool addParticles = true, bool playSound = true, bool allowDamageEvent = true) {

			RayCastFast (origin, direction, out hitInfo, maxDistance);
			VoxelChunk chunk = hitInfo.chunk;
			if (chunk == null || hitInfo.voxelIndex < 0) {
				lastHitInfo.chunk = null;
				lastHitInfo.voxelIndex = -1;
				return false;
			}

			lastHitInfo = hitInfo;
			DamageVoxelFast (ref hitInfo, damage, addParticles, playSound, allowDamageEvent);

			bool button1Pressed = input.GetButton (INPUT_BUTTON_NAMES.Button1);
			bool button2Pressed = input.GetButton (INPUT_BUTTON_NAMES.Button2);
			if ((button1Pressed || button2Pressed) && OnVoxelClick != null) {
				OnVoxelClick (chunk, hitInfo.voxelIndex, button1Pressed ? 0 : 1);
			}

			if (damageRadius > 1) {
				Vector3 otherPos;
				Vector3 explosionPosition = hitInfo.voxelCenter + hitInfo.normal * damageRadius;
				damageRadius--;

				for (int y = -damageRadius; y <= damageRadius; y++) {
					otherPos.y = lastHitInfo.voxelCenter.y + y;
					for (int z = -damageRadius; z <= damageRadius; z++) {
						otherPos.z = lastHitInfo.voxelCenter.z + z;
						for (int x = -damageRadius; x <= damageRadius; x++) {
							if (x == 0 && z == 0 && y == 0)
								continue;
							VoxelChunk otherChunk;
							int otherIndex;
							otherPos.x = lastHitInfo.voxelCenter.x + x;
							if (GetVoxelIndex (otherPos, out otherChunk, out otherIndex, false)) {
								if (GetVoxelVisibility (otherChunk, otherIndex)) {
									FastVector.NormalizedDirection (ref explosionPosition, ref otherPos, ref direction);
									if (RayCast (explosionPosition, direction, out hitInfo)) {
										DamageVoxelFast (ref hitInfo, damage, addParticles, playSound, allowDamageEvent);
									}
								}
							}
						}
					}
				}
			}

			return true;
		}


		int DamageAreaFast (Vector3 origin, int damage, int damageRadius = 1, bool distanceAttenuation = true, bool addParticles = true, List<VoxelIndex> results = null) {

			bool hasResults = results != null;
			if (hasResults) {
				results.Clear ();
			}
			if (damageRadius < 0 || damage < 1) {
				return 0;
			}

			int damagedCount = 0;
			Vector3 direction = Misc.vector3zero;
			VoxelHitInfo hitInfo;
			GetVoxelIndices (origin, damageRadius, tempVoxelIndices);
			int count = tempVoxelIndices.Count;
			for (int k = 0; k < count; k++) {
				VoxelIndex vi = tempVoxelIndices [k];
				VoxelChunk otherChunk = vi.chunk;
				int otherIndex = vi.voxelIndex;
				int dam = damage;
				if (distanceAttenuation && vi.sqrDistance > 1) {
					dam = (int)(damage * damageRadius * damageRadius / vi.sqrDistance);
				}
				if (dam > 0 && GetVoxelVisibility (otherChunk, otherIndex)) {
					FastVector.NormalizedDirection (ref origin, ref vi.position, ref direction);
					if (RayCastFast (origin, direction, out hitInfo, damageRadius, false, 5)) {
						int damageTaken = DamageVoxelFast (ref hitInfo, dam, addParticles, false);
						if (hasResults) {
							VoxelIndex di = vi;
							di.damageTaken = damageTaken;
							results.Add (di);
							damagedCount++;
						}
					}
				}
			}

			return damagedCount;
		}

		/// <summary>
		/// Performs the voxel damage.
		/// </summary>
		/// <returns>The actual damage taken by the voxe.</returns>
		/// <param name="hitInfo">Hit info.</param>
		/// <param name="damage">Damage.</param>
		/// <param name="addParticles">If set to <c>true</c> add particles.</param>
		int DamageVoxelFast (ref VoxelHitInfo hitInfo, int damage, bool addParticles, bool playSound, bool allowDamageEvent = true) {

			VoxelChunk chunk = hitInfo.chunk;
			if (hitInfo.voxel.typeIndex == 0)
				return 0;
			VoxelDefinition voxelType = hitInfo.voxel.type;
			byte voxelTypeResistancePoints = voxelType.resistancePoints;

			if (buildMode) {
				if (damage > 0) {
					damage = 255;
				}
			} else {
				if (voxelTypeResistancePoints == (byte)0) {
					damage = 0;
				} else if (voxelTypeResistancePoints == (byte)255) {
					if (playSound) {
						PlayImpactSound (hitInfo.voxel.type.impactSound, hitInfo.voxelCenter);
					}
					damage = 0;
				}
			}

			if (allowDamageEvent && OnVoxelDamaged != null) {
				OnVoxelDamaged (chunk, hitInfo.voxelIndex, ref damage);
			}

			if (damage == 0)
				return 0;

			// Gets ambient light near surface
			float voxelLight = GetVoxelLight (hitInfo.point + hitInfo.normal * 0.5f);

			// Get voxel damage indicator GO's name
			bool destroyed = voxelType.renderType == RenderType.CutoutCross;
			int resistancePointsLeft = 0;
			VoxelPlaceholder placeholder = null;
			if (!destroyed) {
				placeholder = GetVoxelPlaceholder (chunk, hitInfo.voxelIndex, true);
				resistancePointsLeft = placeholder.resistancePointsLeft - damage;
				if (resistancePointsLeft < 0) {
					resistancePointsLeft = 0;
					destroyed = true;
				}
				placeholder.resistancePointsLeft = resistancePointsLeft;
			}

			if (voxelType.renderType == RenderType.Empty)
				addParticles = false;
			
			int particlesAmount;
			if (destroyed) {

				// Add recoverable voxel on the scene (not for vegetation)
				if (voxelType.renderType != RenderType.Empty && voxelType.renderType != RenderType.CutoutCross && voxelType.canBeCollected && !buildMode) {
					bool create = true;
					if (OnVoxelBeforeDropItem != null) {
						OnVoxelBeforeDropItem (chunk, hitInfo.voxelIndex, out create);
					}
					if (create) {
						CreateRecoverableVoxel (hitInfo.voxelCenter, voxelDefinitions [hitInfo.voxel.typeIndex], hitInfo.voxel.color, voxelLight);
					}
				}

				// Destroy the voxel
				VoxelDestroyFast (chunk, hitInfo.voxelIndex);

				// Check if grass is on top and remove it as well
				VoxelChunk topChunk;
				int topIndex;
				if (GetVoxelIndex (hitInfo.voxelCenter + Misc.vector3up, out topChunk, out topIndex, false)) {
					if (topChunk.voxels [topIndex].typeIndex != 0 && voxelDefinitions [topChunk.voxels [topIndex].typeIndex].renderType == RenderType.CutoutCross) {
						byte light = topChunk.voxels [topIndex].lightMesh;
						topChunk.voxels [topIndex].Clear (light);
						topChunk.modified = true;
					}
				}

				// Max particles
				particlesAmount = 20;

				if (playSound) {
					PlayDestructionSound (voxelDefinitions [hitInfo.voxel.typeIndex].destructionSound, hitInfo.voxelCenter);
				}
			} else {
				// Add damage indicator
				if (placeholder == null) {
					placeholder = GetVoxelPlaceholder (chunk, hitInfo.voxelIndex, true);
				}
				if (placeholder.damageIndicator == null) {
					if (damagedVoxelPrefab == null) {
						damagedVoxelPrefab = Resources.Load<GameObject> ("VoxelPlay/Prefabs/DamagedVoxel");
					}
					GameObject g = Instantiate<GameObject> (damagedVoxelPrefab);
					g.name = DAMAGE_INDICATOR;
					Transform tDamageIndicator = g.transform;
					placeholder.damageIndicator = tDamageIndicator.GetComponent<Renderer> ();
//					if (placeholder.modelInstance != null) {
//						tDamageIndicator.SetParent (placeholder.modelInstance.transform, false);
//					} else {
						tDamageIndicator.SetParent (placeholder.transform, false);
//					}
					tDamageIndicator.localPosition = placeholder.bounds.center;
					tDamageIndicator.localScale = placeholder.bounds.size * 1.001f;
				}

				int textureIndex = FastMath.FloorToInt ((5f * resistancePointsLeft) / voxelTypeResistancePoints);
				if (world.voxelDamageTextures.Length > 0) {
					if (textureIndex >= world.voxelDamageTextures.Length) {
						textureIndex = world.voxelDamageTextures.Length - 1;
					}
					Material mi = placeholder.damageIndicatorMaterial; // gets a copy of material the first time it's used
					mi.mainTexture = world.voxelDamageTextures [textureIndex];
					mi.SetFloat ("_VoxelLight", voxelLight);
					mi.SetFloat ("_VPAmbientLight", ambientLight);
					mi.SetFloat ("_VPDaylightShadowAtten", daylightShadowAtten);
					placeholder.damageIndicator.enabled = true;
				}

				// Particle amount depending of damage
				particlesAmount = (6 - textureIndex) + 3;

				// Sets health recovery for the voxel
				placeholder.StartHealthRecovery (chunk, hitInfo.voxelIndex, world.damageDuration);

				if (playSound) {
					PlayImpactSound (voxelDefinitions [hitInfo.voxel.typeIndex].impactSound, hitInfo.voxelCenter);
				}
			}

			// Add random particles
			if (!addParticles)
				particlesAmount = 0;
			for (int k = 0; k < particlesAmount; k++) {
				int ppeIndex = GetParticleFromPool ();
				if (ppeIndex < 0)
					continue;

				// Scale of particle
				Renderer particleRenderer = particlePool [ppeIndex].renderer;
				if (destroyed) {
					if (voxelType.renderType == RenderType.CutoutCross) {   // smaller particles for vegetation
						particleRenderer.transform.localScale = Misc.vector3one * Random.Range (0.03f, 0.04f);
					} else {
						particleRenderer.transform.localScale = Misc.vector3one * Random.Range (0.04f, 0.1f);
					}
				} else {
					particleRenderer.transform.localScale = Misc.vector3one * Random.Range (0.03f, 0.06f);
				}

				// Set particle texture
				Material instanceMat = particleRenderer.sharedMaterial;
				SetParticleMaterialTextures (instanceMat, voxelDefinitions [hitInfo.voxel.typeIndex], hitInfo.voxel.color);
				instanceMat.mainTextureOffset = new Vector2 (Random.value, Random.value);
				instanceMat.mainTextureScale = Misc.vector2one * 0.05f;
				instanceMat.SetFloat ("_VoxelLight", voxelLight);
				instanceMat.SetFloat ("_VPAmbientLight", ambientLight);
				instanceMat.SetFloat ("_VPDaylightShadowAtten", daylightShadowAtten);
				instanceMat.SetFloat ("_FlashDelay", 0);

				// Set position
				Rigidbody rb = particlePool [ppeIndex].rigidBody;
				if (destroyed) {
					Vector3 expelDir = Random.insideUnitSphere;
					Vector3 pos = hitInfo.voxelCenter + expelDir;
					particleRenderer.transform.position = pos;
					rb.AddForce (expelDir * (Random.value * 125f));
				} else {
					Vector3 pos = hitInfo.point;
					Vector3 v1 = new Vector3 (-hitInfo.normal.y, hitInfo.normal.z, hitInfo.normal.x);
					Vector3 v2 = new Vector3 (-hitInfo.normal.z, hitInfo.normal.x, hitInfo.normal.y);
					Vector3 dx = (Random.value - 0.5f) * 0.7f * v1;
					Vector3 dy = (Random.value - 0.5f) * 0.7f * v2;
					particleRenderer.transform.position = pos + hitInfo.normal * 0.001f + dx + dy;
					rb.AddForce (cameraMain.transform.forward * (Random.value * -125f));
				}
				rb.AddForce (Misc.vector3up * 25f);
				rb.AddTorque (Random.onUnitSphere * 100f);
				rb.useGravity = true;

				// Self-destruct
				particlePool [ppeIndex].destructionTime = Time.time + 2.5f + Random.value;
			}

			return damage;
		}

		/// <summary>
		/// Plays impact sound at position.
		/// </summary>
		/// <param name="sound">Custom audioclip or pass null to use default impact sound defined in Voxel Play Environment component.</param>
		void PlayImpactSound (AudioClip sound, Vector3 position) {
			if (sound == null)
				sound = defaultImpactSound;
			if (sound != null) {
				AudioSource.PlayClipAtPoint (sound, position);
			}
		}

		/// <summary>
		/// Plays voxel build sound at position
		/// </summary>
		/// <param name="sound">Custom audioclip or pass null to use default build sound defined in Voxel Play Environment component.</param>
		void PlayBuildSound (AudioClip sound, Vector3 position) {
			if (sound == null)
				sound = defaultBuildSound;
			if (sound != null) {
				AudioSource.PlayClipAtPoint (sound, position);
			}
		}

		/// <summary>
		/// Plays voxel destruction sound at position
		/// </summary>
		/// <param name="sound">Custom audioclip or pass null to use default destruction sound defined in Voxel Play Environment component.</param>
		void PlayDestructionSound (AudioClip sound, Vector3 position) {
			if (sound == null)
				sound = defaultDestructionSound;
			if (sound != null) {
				AudioSource.PlayClipAtPoint (sound, position);
			}
		}

		GameObject CreateRecoverableVoxel (Vector3 position, VoxelDefinition voxelType, Color32 color, float voxelLight) {

			// Set item info
			ItemDefinition dropItem = voxelType.dropItem;
			if (dropItem == null) {
				dropItem = GetItemByType (ITEM_CATEGORY.Voxel, voxelType);
			}
			if (dropItem == null)
				return null;

			int ppeIndex = GetParticleFromPool ();
			if (ppeIndex < 0)
				return null;

			// Set collider size
			particlePool [ppeIndex].collider.size = new Vector3 (2f, 2f, 2f); // make voxel float on top of other voxels

			// Set rigidbody behaviour
			particlePool [ppeIndex].rigidBody.freezeRotation = true;

			// Set position & scale
			Renderer particleRenderer = particlePool [ppeIndex].renderer;
			particleRenderer.transform.position = position + Random.insideUnitSphere * 0.25f;
			particleRenderer.transform.localScale = new Vector3 (0.25f, 0.25f, 0.25f);

			particlePool [ppeIndex].item.type = dropItem;
			particlePool [ppeIndex].item.canPickUp = true;
			particlePool [ppeIndex].item.rb = particlePool [ppeIndex].rigidBody;
			particlePool [ppeIndex].item.resitancePointsLeft = dropItem.resistancePoints;
			particlePool [ppeIndex].item.creationTime = Time.time;
			particlePool [ppeIndex].item.quantity = voxelType.renderType == RenderType.Water ? GetVoxel (position, false).GetWaterLevel () / 15f : 1f;

			// Set particle texture
			Material instanceMat = particleRenderer.sharedMaterial;
			switch (dropItem.category) {
			case ITEM_CATEGORY.Voxel:
				VoxelDefinition dropVoxelType = dropItem.voxelType;
				if (dropVoxelType == null) {
					dropVoxelType = voxelType;
				}
				SetParticleMaterialTextures (instanceMat, dropVoxelType, color);
				break;
			default:
				SetParticleMaterialTextures (instanceMat, dropItem.icon);
				break;
			}
			instanceMat.mainTextureOffset = Misc.vector2zero;
			instanceMat.mainTextureScale = Misc.vector2one;
			instanceMat.SetFloat ("_VoxelLight", voxelLight);
			instanceMat.SetFloat ("_VPAmbientLight", ambientLight);
			instanceMat.SetFloat ("_VPDaylightShadowAtten", daylightShadowAtten);
			instanceMat.SetFloat ("_FlashDelay", 5f);

			// Self-destruct
			particlePool [ppeIndex].destructionTime = Time.time + 10f;

			return particlePool [ppeIndex].renderer.gameObject;
		}

		void SetParticleMaterialTextures (Material mat, VoxelDefinition voxelType, Color32 color) {
			if (voxelType.renderType == RenderType.CutoutCross) {
				// vegetation only uses sample colors
				mat.mainTexture = Texture2D.whiteTexture;
				mat.SetTexture ("_TexSides", Texture2D.whiteTexture);
				mat.SetTexture ("_TexBottom", Texture2D.whiteTexture);
				float r = 0.8f + Random.value * 0.4f; // color variation
				Color vegetationColor = new Color (voxelType.sampleColor.r * r, voxelType.sampleColor.g * r, voxelType.sampleColor.b * r, 1f);
				mat.SetColor ("_Color", vegetationColor);
			} else {
				mat.mainTexture = voxelType.textureThumbnailTop;
				mat.SetTexture ("_TexSides", voxelType.textureThumbnailSide);
				mat.SetTexture ("_TexBottom", voxelType.textureThumbnailBottom);
				mat.SetColor ("_Color", color);
			}
		}

		void SetParticleMaterialTextures (Material mat, Texture2D texture) {
			mat.SetTexture ("_TexSides", texture);
			mat.SetTexture ("_TexBottom", texture);
			mat.SetColor ("_Color", Misc.colorWhite);
		}


		int GetParticleFromPool () {
			int count = particlePool.Length;
			int index = -1;
			for (int k = 0; k < count; k++) {
				if (++particlePoolCurrentIndex >= particlePool.Length)
					particlePoolCurrentIndex = 0;
				if (!particlePool [particlePoolCurrentIndex].used) {
					index = particlePoolCurrentIndex;
					break;
				}
			}
			if (index < 0)
				return -1;

			Renderer particleRenderer;
			if (particlePool [index].renderer == null) {
				GameObject particle = Instantiate<GameObject> (damageParticlePrefab, fxRoot);
				particle.hideFlags = HideFlags.HideAndDontSave;
				particleRenderer = particle.GetComponent<Renderer> ();
				particleRenderer.sharedMaterial = Instantiate<Material> (particleRenderer.sharedMaterial, fxRoot);
				particleRenderer.sharedMaterial.SetFloat ("_AnimSeed", UnityEngine.Random.value * Mathf.PI);
				particlePool [index].renderer = particleRenderer;
				particlePool [index].rigidBody = particleRenderer.GetComponent<Rigidbody> ();
				particlePool [index].collider = particleRenderer.GetComponent<BoxCollider> ();
				particlePool [index].item = particleRenderer.GetComponent<Item> ();
				particlePool [index].renderer.gameObject.layer = layerParticles;
				// Ignore collisions with player
				if (characterController != null) {
					Physics.IgnoreCollision (particlePool [index].collider, characterControllerCollider);
				}
			} else {
				particleRenderer = particlePool [index].renderer;
				particlePool [index].rigidBody.isKinematic = false;
				particlePool [index].item.enabled = true;
				particleRenderer.enabled = true;
			}
			particlePool [index].rigidBody.freezeRotation = false;
			particlePool [index].rigidBody.velocity = Misc.vector3zero;
			particlePool [index].rigidBody.angularVelocity = Misc.vector3zero;
			particlePool [index].collider.size = Misc.vector3one;
			particlePool [index].used = true;
			particlePool [index].item.type = null;
			particlePool [index].item.canPickUp = false;
			particlePool [index].item.pickingUp = false;
			return index;
		}


		/// <summary>
		/// Checks if there's a voxel at given position.
		/// </summary>
		/// <returns><c>true</c>, if collision was checked, <c>false</c> otherwise.</returns>
		/// <param name="position">Position.</param>
		public bool CheckCollision (Vector3 position) {
			int x = FastMath.FloorToInt (position.x / 16);
			int y = FastMath.FloorToInt (position.y / 16);
			int z = FastMath.FloorToInt (position.z / 16);
			VoxelChunk chunk = GetChunkOrCreate (x, y, z);
			if (chunk != null) {
				Voxel[] voxels = chunk.voxels;
				int py = (int)(position.y - y * 16); // FastMath.FloorToInt (position.y) - y * 16;
				int pz = (int)(position.z - z * 16); // FastMath.FloorToInt (position.z) - z * 16;
				int px = (int)(position.x - x * 16); // FastMath.FloorToInt (position.x) - x * 16;
				int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
				return voxels [voxelIndex].hasContent == 1 && voxelDefinitions [voxels [voxelIndex].typeIndex].navigatable;
			}
			return false;
		}


	}



}
