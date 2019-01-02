using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		const byte FULL_OPAQUE = 15;

		[NonSerialized]
		public VoxelDefinition[] voxelDefinitions;
		public int voxelDefinitionsCount;

		List<VoxelChunk> voxelPlaceFastAffectedChunks;

		float[] collapsingOffsets = new float[] {
			0, 1, 0,
			-1, 1, -1,
			1, 1, 1,
			-1, 1, 1,
			1, 1, -1,
			1, 1, 0,
			0, 1, 1,
			-1, 1, 0,
			0, 1, -1,
			-1, 1, -1,
			1, 0, 1,
			-1, 0, 1,
			1, 0, -1,
			1, 0, 0,
			0, 0, 1,
			-1, 0, 0,
			0, 0, -1,
			-1, 0, -1
		};

		#region Voxel functions

		void VoxelDestroyFast (VoxelChunk chunk, int voxelIndex) {

			// Ensure there's content on this position
			if (chunk.voxels [voxelIndex].hasContent != 1)
				return;

			if (OnVoxelBeforeDestroyed != null) {
				OnVoxelBeforeDestroyed (chunk, voxelIndex);
			}

			// 1) Remove placeholder if exists
			VoxelPlaceholderDestroy (chunk, voxelIndex);

			// 2) Clears voxel
			bool triggerCollapse = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex].triggerCollapse;
			VoxelDestroyFastSingle (chunk, voxelIndex);

			// Update lightmap and renderers
			RefreshNineChunks (chunk);

			// Force rebuild neighbour meshes if destroyed voxel is on a border
			RebuildNeighbours(chunk, voxelIndex);

			if (OnChunkChanged != null) {
				OnChunkChanged (chunk);
			}
			if (OnVoxelDestroyed != null) {
				OnVoxelDestroyed (chunk, voxelIndex);
			}

			// Check if it was surrounded by water. If it was, add water expander
			Vector3 voxelPosition = GetVoxelPosition (chunk, voxelIndex);
			MakeSurroundingWaterExpand (chunk, voxelIndex, voxelPosition);

			// Check if voxels on top can fall
			if (triggerCollapse && world.collapseOnDestroy) {
				VoxelCollapse (voxelPosition, world.collapseAmount, null, world.consolidateDelay); 
			}
		}

		void VoxelDestroyFastSingle (VoxelChunk chunk, int voxelIndex) {
			chunk.voxels [voxelIndex].Clear (effectiveGlobalIllumination ? (byte)0 : (byte)15);
			chunk.modified = true;
		}


		/// <summary>
		/// Puts a voxel in the given position. Takes care of informing neighbour chunks.
		/// </summary>
		/// <returns>Returns the affected chunk and voxel index</returns>
		/// <param name="position">Position.</param>
		/// <param name="voxel">Voxel.</param>
		void VoxelPlaceFast (Vector3 position, VoxelDefinition voxelType, out VoxelChunk chunk, out int voxelIndex, Color32 tintColor, float amount = 1f, int rotation = 0) {
			VoxelSingleSet (position, voxelType, out chunk, out voxelIndex, tintColor);

			// Apply rotation
			if (voxelType.allowsTextureRotation) {
				chunk.voxels [voxelIndex].SetTextureRotation (rotation);
			}

			// If it's water, add flood
			if (voxelType.spreads) {
				chunk.voxels [voxelIndex].SetWaterLevel ( (Mathf.CeilToInt(amount * 15f) ) );
				AddWaterFlood (ref position, voxelType);
			}

			chunk.modified = true;

			// Update neighbours
			RefreshNineChunks (chunk);

			// Triggers event
			if (OnChunkChanged != null) {
				OnChunkChanged (chunk);
			}
		}


		/// <summary>
		/// Internal method that puts a voxel in a given position. This method does not inform to neighbours. Only used by non-contiguous structures, like trees or vegetation.
		/// For terrain or large scale buildings, use VoxelPlaceFast.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="voxelType">Voxel type.</param>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		/// <returns>True if voxel was places</returns>
		void VoxelSingleSet (Vector3 position, VoxelDefinition voxelType, out VoxelChunk chunk, out int voxelIndex) {
			VoxelSingleSet (position, voxelType, out chunk, out voxelIndex, Misc.color32White);
		}

		/// <summary>
		/// Internal method that puts a voxel in a given position. This method does not inform to neighbours. Only used by non-contiguous structures, like trees or vegetation.
		/// For terrain or large scale buildings, use VoxelPlaceFast.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="voxelType">Voxel type.</param>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		/// <returns>True if voxel was places</returns>
		void VoxelSingleSet (Vector3 position, VoxelDefinition voxelType, out VoxelChunk chunk, out int voxelIndex, Color32 tintColor) {
			if (GetVoxelIndex (position, out chunk, out voxelIndex)) {
				chunk.voxels [voxelIndex].Set (voxelType, tintColor);
			}
		}


		/// <summary>
		/// Converts a voxel into dynamic type
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		GameObject VoxelSetDynamic (VoxelChunk chunk, int voxelIndex, bool addRigidbody, float duration) {
			if (chunk == null || chunk.voxels [voxelIndex].hasContent == 0)
				return null;

			VoxelDefinition vd = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];
			if (!vd.renderType.supportsDynamic()) {
				return null;
			}

			VoxelPlaceholder placeholder = GetVoxelPlaceholder (chunk, voxelIndex, true);
			if (placeholder == null)
				return null;

			// Add rigid body
			if (addRigidbody) {
				Rigidbody rb = placeholder.GetComponent<Rigidbody> ();
				if (rb == null) {
					placeholder.rb = placeholder.gameObject.AddComponent<Rigidbody> ();
				}
			}

			// If it's a custom model ignore it as it's already a gameobject
			if (placeholder.modelMeshFilter != null)
				return placeholder.gameObject;

			VoxelDefinition vdDyn = vd.dynamicDefinition;

			if (vdDyn == null) {
				// Setup and save voxel definition
				vd.dynamicDefinition = vdDyn = ScriptableObject.CreateInstance<VoxelDefinition> ();
				vdDyn.isDynamic = true;
				vdDyn.staticDefinition = vd;
				vdDyn.renderType = RenderType.Custom;
				vdDyn.textureIndexBottom = vd.textureIndexBottom;
				vdDyn.textureIndexSide = vd.textureIndexSide;
				vdDyn.textureIndexTop = vd.textureIndexTop;
				vdDyn.textureThumbnailTop = vd.textureThumbnailTop;
				vdDyn.textureThumbnailSide = vd.textureThumbnailSide;
				vdDyn.textureThumbnailBottom = vd.textureThumbnailBottom;
				vdDyn.scale = vd.scale;
				vdDyn.offset = vd.offset;
				vdDyn.offsetRandomRange = vd.offsetRandomRange;
				vdDyn.rotation = vd.rotation;
				vdDyn.rotationRandomY = vd.rotationRandomY;
				vdDyn.sampleColor = vd.sampleColor;
				vdDyn.promotesTo = vd.promotesTo;
				vdDyn.playerDamageDelay = vd.playerDamageDelay;
				vdDyn.playerDamage = vd.playerDamage;
				vdDyn.pickupSound = vd.pickupSound;
				vdDyn.landingSound = vd.landingSound;
				vdDyn.jumpSound = vd.jumpSound;
				vdDyn.impactSound = vd.impactSound;
				vdDyn.footfalls = vd.footfalls;
				vdDyn.destructionSound = vd.destructionSound;
				vdDyn.canBeCollected = vd.canBeCollected;
				vdDyn.dropItem = GetItemByType (ITEM_CATEGORY.Voxel, vd);
				vdDyn.buildSound = vd.buildSound;
				vdDyn.navigatable = true;
				vdDyn.windAnimation = false;
				vdDyn.model = MakeDynamicCubeFromVoxel (chunk, voxelIndex);
				vdDyn.name = vd.name + " (Dynamic)";
				AddVoxelTextures (vdDyn);
			}

			// Clear any vegetation on top if voxel can be moved (has a rigidbody) to avoid floating grass block
			if (placeholder.rb != null) {
				VoxelChunk topChunk;
				int topIndex;
				if (GetVoxelIndex (chunk, voxelIndex, 0, 1, 0, out topChunk, out topIndex)) {
					if (topChunk.voxels [topIndex].hasContent == 1 && voxelDefinitions [topChunk.voxels [topIndex].typeIndex].renderType == RenderType.CutoutCross) {
						VoxelDestroyFast (topChunk, topIndex);
					}
				}
			}
			Color32 color = chunk.voxels [voxelIndex].color;
			chunk.voxels [voxelIndex].Set (vdDyn, color);

			if (duration > 0) {
				placeholder.SetCancelDynamic (duration);
			}

			// Refresh neighbours
			RebuildNeighbours(chunk, voxelIndex);

			return placeholder.gameObject;
		}

		/// <summary>
		/// Finds all voxels with "willCollapse" connected to a given position
		/// </summary>
		/// <returns>The crumbly voxel indices.</returns>
		/// <param name="position">Position.</param>
		/// <param name="results">Results.</param>
		int GetCrumblyVoxelIndices (Vector3 position, int amount, List<VoxelIndex> voxelIndices = null) {
			if (voxelIndices == null) {
				voxelIndices = tempVoxelIndices;
			}
			voxelIndices.Clear ();
			tempVoxelPositions.Clear ();
			tempVoxelIndicesCount = 0;
			GetCrumblyVoxelRecursive (position + Misc.vector3one, position, amount, voxelIndices);
			return tempVoxelIndicesCount;
		}

		void GetCrumblyVoxelRecursive (Vector3 originalPosition, Vector3 position, int amount, List<VoxelIndex> voxelIndices) {
			if (tempVoxelIndicesCount >= amount)
				return;
			
			VoxelChunk chunk;
			int voxelIndex;
			int c = 0;
			bool dummy;
			for (int k = 0; k < collapsingOffsets.Length; k += 3) {
				Vector3 pos = position;
				pos.x += collapsingOffsets [k];
				pos.y += collapsingOffsets [k + 1];
				pos.z += collapsingOffsets [k + 2];
				float dx = pos.x > originalPosition.x ? pos.x - originalPosition.x : originalPosition.x - pos.x;
				float dz = pos.z > originalPosition.z ? pos.z - originalPosition.z : originalPosition.z - pos.z;
				if (dx > 8 || dz > 8)
					continue;
				if (!tempVoxelPositions.TryGetValue (pos, out dummy)) {
					tempVoxelPositions [pos] = true;
					if (GetVoxelIndex (pos, out chunk, out voxelIndex, false) && chunk.voxels [voxelIndex].hasContent == 1 && chunk.voxels [voxelIndex].opaque >= 3 && voxelDefinitions [chunk.voxels [voxelIndex].typeIndex].willCollapse) {
						VoxelIndex vi = new VoxelIndex ();
						vi.chunk = chunk;
						vi.voxelIndex = voxelIndex;
						vi.position = pos;
						voxelIndices.Add (vi);
						tempVoxelIndicesCount++;
						c++;
						if (tempVoxelIndicesCount >= amount)
							break;
					}
				}
			}
			int lastCount = tempVoxelIndicesCount;
			for (int k = 1; k <= c; k++) {
				GetCrumblyVoxelRecursive (originalPosition, tempVoxelIndices [lastCount - k].position, amount, voxelIndices);
			}
		}


		#endregion

	}



}
