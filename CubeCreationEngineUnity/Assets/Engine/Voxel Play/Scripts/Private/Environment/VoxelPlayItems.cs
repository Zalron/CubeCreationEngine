using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {
				
	public partial class VoxelPlayEnvironment : MonoBehaviour {

		const string TORCH_NAME = "Torch";
		ItemDefinition torchDefinition;

		/// <summary>
		/// Dictionary lookup for the voxel definition by name
		/// </summary>
		Dictionary<string, ItemDefinition> itemDefinitionsDict;


		/// <summary>
		/// Initializes the array of available items "allItems" with items defined at world level plus all the terrain voxels
		/// </summary>
		void InitItems () {

			int worldItemsCount = world.items != null ? world.items.Length : 0;
			if (allItems == null) {
				allItems = new List<InventoryItem> (voxelDefinitionsCount + worldItemsCount);
			} else {
				allItems.Clear ();
			}
			// Init item definitions dictionary
			if (itemDefinitionsDict == null) {
				itemDefinitionsDict = new Dictionary<string, ItemDefinition> ();
			} else {
				itemDefinitionsDict.Clear ();
			}

			// Add world items
			for (int k = 0; k < worldItemsCount; k++) {
				ItemDefinition id = world.items [k];
				if (id == null)
					continue;
				if (!itemDefinitionsDict.ContainsKey (id.name)) {
					InventoryItem item;
					item.item = id;
					item.quantity = 999999;
					allItems.Add (item);

					itemDefinitionsDict[id.name] = id;
				}
			}

			// Add any player item that's not listed in world items
			VoxelPlayPlayer player = VoxelPlayPlayer.instance;
			if (player != null && player.items != null) {
				int playerItemCount = player.playerItems.Count;
				for (int k = 0; k < playerItemCount; k++) {
					InventoryItem it = player.playerItems [k];
					if (it.item!=null && !itemDefinitionsDict.ContainsKey (it.item.name)) {
						InventoryItem item;
						item.item = it.item;
						item.quantity = 999999;
						allItems.Add (item);

						itemDefinitionsDict[it.item.name] = it.item;
					}
				}
			}

			// Add any other item definition found inside Defaults
			ItemDefinition[] ids = Resources.LoadAll<ItemDefinition> ("VoxelPlay/Defaults");
			for (int k = 0; k < ids.Length; k++) {
				AddItemDefinition (ids[k]);
			}

			// Add any other item definition inside a resource directory with same name of world
			ids = Resources.LoadAll<ItemDefinition> ("Worlds/" + world.name);
			for (int k = 0; k < ids.Length; k++) {
				AddItemDefinition (ids [k]);
			}

			// Add any other item definition inside a resource directory with same name of world (if not placed into Worlds directory)
			ids = Resources.LoadAll<ItemDefinition> (world.name);
			for (int k = 0; k < ids.Length; k++) {
				AddItemDefinition (ids [k]);
			}

			// Add voxel definitions as inventory items
			for (int k = 0; k < voxelDefinitionsCount; k++) {
				if (voxelDefinitions [k].hidden)
					continue;
				ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition> ();
				item.category = ItemCategory.Voxel;
				item.icon = voxelDefinitions [k].GetIcon ();
				item.color = voxelDefinitions [k].tintColor;
				item.title = voxelDefinitions [k].title;
				item.voxelType = voxelDefinitions [k];
				item.pickupSound = voxelDefinitions [k].pickupSound;
				InventoryItem i;
				i.item = item;
				i.quantity = 999999;
				allItems.Add (i);
			}

		}

		/// <summary>
		/// Adds an item definition
		/// </summary>
		/// <returns><c>true</c>, if item definition was added, <c>false</c> otherwise.</returns>
		public bool AddItemDefinition(ItemDefinition itemDefinition) {
			if (itemDefinitionsDict.ContainsKey (itemDefinition.name))
				return false;

			InventoryItem item;
			item.item = itemDefinition;
			item.quantity = 999999;
			allItems.Add (item);

			itemDefinitionsDict [itemDefinition.name] = itemDefinition;
			return true;
		}


		/// <summary>
		/// Adds a torch.
		/// </summary>
		/// <param name="position">Position of the light gameobject. This is the center of the gameObject.</param>
		/// <param name="voxelLightPosition">Position of the voxel containing the light. This is used for building the lightmap. While 'position' can be on the wall of another voxel, 'voxelLightPosition' would be slightly off so it's containined inside the voxel that holds the light (the wall belongs to the next voxel).</param>
		GameObject TorchAttachInt (VoxelHitInfo hitInfo) {

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
				int itemCount = allItems.Count;
				for (int k = 0; k < itemCount; k++) {
					if (allItems [k].item.category == ItemCategory.Torch) {
						torchDefinition = allItems [k].item;
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
			torch.transform.position = chunk.transform.position + GetVoxelChunkPosition (hitInfo.voxelIndex) + hitInfo.normal * 0.5f;

			// Rotate torch according to wall normal
			if (hitInfo.normal.y == -1) { // downwards
				torch.transform.Rotate (180f, 0, 0);
			} else if (hitInfo.normal.y == 0) { // side wall
				torch.transform.Rotate (hitInfo.normal.z * 40f, 0, hitInfo.normal.x * -40f);
			}

			Item itemInfo = torch.AddComponent<Item> ();
			itemInfo.itemDefinition = torchDefinition;

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
			if (characterControllerCollider != null) {
				Physics.IgnoreCollision (torch.GetComponent<Collider> (), characterControllerCollider);
			}

			// Trigger torch event
			if (!isLoadingGame && OnTorchAttached != null) {
				OnTorchAttached (chunk, lightSource);
			}

			return torch;
		}

		void TorchDetachInt (VoxelChunk chunk, GameObject gameObject) {
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



		GameObject CreateRecoverableItem (Vector3 position, ItemDefinition itemDefinition, int quantity = 1) {

			if (itemDefinition == null || itemDefinition.prefab == null)
				return null;

			GameObject obj = Instantiate<GameObject> (itemDefinition.prefab);
			Item item = obj.AddComponent<Item> ();
			item.canPickUp = true;
			item.quantity = quantity;
			item.itemDefinition = itemDefinition;
			item.creationTime = Time.time;
			item.persistentItem = true;

			Collider collider = obj.GetComponent<Collider> ();
			if (collider != null && characterControllerCollider!=null) {
				Physics.IgnoreCollision (collider, characterControllerCollider);
			}

			// Set position & scale
			obj.transform.position = position + Random.insideUnitSphere * 0.25f;

			// If there's no chunk rendered at the position, disable any rigidBody until it's loaded
			Rigidbody rb = obj.GetComponent<Rigidbody>();
			if (rb != null) {
				rb.useGravity = false;
			}
			return obj;
		}




	}

}
