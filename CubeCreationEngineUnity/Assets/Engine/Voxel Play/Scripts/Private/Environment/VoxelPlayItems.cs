using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay
{
				
	public partial class VoxelPlayEnvironment : MonoBehaviour
	{

		const string TORCH_NAME = "Torch";
		ItemDefinition torchDefinition;

		/// <summary>
		/// Initializes the array of available items "allItems" with items defined at world level plus all the terrain voxels
		/// </summary>
		void InitItems ()
		{
			int worldItemsCount = world.items!=null ? world.items.Length : 0;
			if (allItems == null) {
				allItems = new List<InventoryItem> (voxelDefinitionsCount + worldItemsCount);
			} else {
				allItems.Clear ();
			}

			// Add world items
			for (int k = 0; k < worldItemsCount; k++) {
				InventoryItem item;
				item.item = world.items [k];
				item.quantity = 999999;
				allItems.Add (item);
			}

			// Add any player item that's not listed in world items
			VoxelPlayPlayer player = VoxelPlayPlayer.instance;
			if (player != null && player.items!=null) {
				int playerItemCount = player.playerItems.Count;
				for (int k = 0; k < playerItemCount; k++) {
					if (!allItems.Contains (player.playerItems [k])) {
						InventoryItem item;
						item.item = player.playerItems [k].item;
						item.quantity = 999999;
						allItems.Add (item);
					}
				}
			}

			// Add voxel definitions as inventory items
			for (int k = 0; k < voxelDefinitionsCount; k++) {
				if (voxelDefinitions [k].hidden)
					continue;
				ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition> ();
				item.category = ItemCategory.Voxel;
				item.icon = voxelDefinitions [k].textureThumbnailSide;
				item.title = voxelDefinitions [k].title;
				item.voxelType = voxelDefinitions [k];
				InventoryItem i;
				i.item = item;
				i.quantity = 999999;
				allItems.Add (i);
			}
		}


		/// <summary>
		/// Adds a torch.
		/// </summary>
		/// <param name="position">Position of the light gameobject. This is the center of the gameObject.</param>
		/// <param name="voxelLightPosition">Position of the voxel containing the light. This is used for building the lightmap. While 'position' can be on the wall of another voxel, 'voxelLightPosition' would be slightly off so it's containined inside the voxel that holds the light (the wall belongs to the next voxel).</param>
		GameObject TorchAttachInt (VoxelHitInfo hitInfo)
		{

			// Placeholder for attaching the torch
			VoxelPlaceholder placeHolder = GetVoxelPlaceholder (hitInfo.chunk, hitInfo.voxelIndex, true);
			if (placeHolder == null)
				return null;

			// Position of the voxel containing the "light" of the torch
			Vector3 voxelLightPosition = hitInfo.voxelCenter + hitInfo.normal;

			VoxelChunk chunk;
			int voxelIndex;

			if (!GetVoxelIndex (voxelLightPosition, out chunk, out voxelIndex))
				return null;

			// Make sure the voxel exists (has not been removed just before this call) and is solid 
			if (chunk.voxels [hitInfo.voxelIndex].hasContent != 1 || chunk.voxels [hitInfo.voxelIndex].opaque < FULL_OPAQUE)
				return null;

			// Updates current chunk
			if (chunk.lightSources == null) {
				chunk.lightSources = new List<LightSource> ();
			} else {
				// Restriction 2: no second torch on the same voxel wall
				// Position in world space where the torch will be attached
				Vector3 wallPosition = hitInfo.voxelCenter + hitInfo.normal * 0.5f;
				int count = chunk.lightSources.Count;
				for (int k = 0; k < count; k++) {
					if (chunk.lightSources [k].gameObject.transform.position == wallPosition)
						return null;
				}
			}

			// Load & instantiate torch prefab
			if (torchDefinition == null) {
				// Get an inventory item with name Torch
				for (int k = 0; k < world.items.Length; k++) {
					if (world.items [k].category == ItemCategory.Torch) {
						torchDefinition = world.items [k];
						break;
					}
				}
			}
			if (torchDefinition == null)
				return null;

			GameObject torch = Instantiate<GameObject> (torchDefinition.prefab);
			torch.name = TORCH_NAME;

			// Parent the torch gameobject to the voxel placeholder
			torch.transform.SetParent (placeHolder.transform, false);

			// Position torch on the wall
			torch.transform.position = chunk.transform.position + GetVoxelChunkPosition(hitInfo.voxelIndex) + hitInfo.normal * 0.5f;

			// Rotate torch according to wall normal
			if (hitInfo.normal.y == -1) { // downwards
				torch.transform.Rotate (180f, 0, 0);
			} else if (hitInfo.normal.y == 0) { // side wall
				torch.transform.Rotate (hitInfo.normal.z * 40f, 0, hitInfo.normal.x * -40f);
			}

			Item itemInfo = torch.AddComponent<Item> ();
			itemInfo.type = torchDefinition;
			itemInfo.resitancePointsLeft = torchDefinition.resistancePoints;

			// Add light source to chunk
			LightSource lightSource;
			lightSource.gameObject = torch;
			lightSource.voxelIndex = voxelIndex;
			lightSource.hitInfo = hitInfo;
			chunk.lightSources.Add (lightSource);
			chunk.modified = true;

			// Add script to remove light source from chunk when torch or voxel is destroyed
			LightSourceRemoval sr = torch.AddComponent<LightSourceRemoval> ();
			sr.env = this;
			sr.chunk = chunk;

			Light pointLight = torch.GetComponentInChildren<Light> ();
			if (pointLight != null) {
				pointLight.enabled = true;
			}

			// Make torch collider ignore player's collider to avoid collisions
			if (characterController != null) {
				Physics.IgnoreCollision (torch.GetComponent<Collider> (),  characterControllerCollider);
			}

			// Trigger torch event
			if (!isLoadingGame && OnTorchAttached != null) {
				OnTorchAttached (chunk, lightSource);
			}

			return torch;
		}

		void TorchDetachInt (VoxelChunk chunk, GameObject gameObject)
		{
			if (chunk.lightSources == null)
				return;
			int count = chunk.lightSources.Count;
			for (int k = 0; k < count; k++) {
				if (chunk.lightSources [k].gameObject == gameObject) {
					if (OnTorchDetached != null) {
						OnTorchDetached (chunk, chunk.lightSources [k]);
					}
					chunk.lightSources.RemoveAt (k);
					chunk.modified = true;
					return;
				}
			}
		}


	}

}
