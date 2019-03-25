using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace VoxelPlay {

	public partial class VoxelPlayFirstPersonController :  VoxelPlayCharacterControllerBase {

		GameObject voxelHighlightBuilder = null;

		#if UNITY_EDITOR

		[Header ("Builder"), Tooltip ("Assign a VoxelPlay Model definition asset to edit it")]
		public ModelDefinition loadModel;

		[Tooltip ("Default constructor size")]
		public int constructorSize = 16;

		GameObject grid;
		const string GRID_NAME = "Voxel Play Builder Grid";
		static Vector3 buildingPosition = Misc.vector3one * 1608f;
		Vector3 beforeConstructorPosition;
		bool beforeOrbitMode, beforeFreeMode, beforeEnableColliders;
		GameObject constructorCanvas;
		ModelDefinition prevModel;
		int sizeX, sizeY, sizeZ;

		void ToggleConstructor () {
			env.constructorMode = !env.constructorMode;
			if (constructorCanvas == null) {
				constructorCanvas = Instantiate<GameObject> (Resources.Load<GameObject> ("VoxelPlay/UI/CanvasConstructor"));
				constructorCanvas.transform.SetParent (env.worldRoot, false);
			}
			if (env.constructorMode) {
				env.ShowMessage ("<color=green>Entered </color><color=yellow>The Constructor</color><color=green>. Press <color=white>V</color> again to exit.</color>");
				constructorCanvas.SetActive (true);
			} else {
				env.ShowMessage ("<color=green>Back to normal world. Press <color=white>B</color> to cancel <color=yellow>Build Mode</color>.</color>");
				constructorCanvas.SetActive (false);
			}
			UpdateConstructorMode ();
		}

		void RefreshConstructorCanvas () {
			string modelName = loadModel == null ? "<New Model>" : loadModel.name;
			constructorCanvas.transform.Find ("Panel/ModelName").GetComponent<Text> ().text = modelName;
		}

		void UpdateConstructorMode () {
			if (env.constructorMode) {
				GetModelSize ();
				if (grid == null) {
					grid = Instantiate<GameObject> (Resources.Load<GameObject> ("VoxelPlay/Prefabs/Grid"));
					grid.name = GRID_NAME;
				} else {
					grid.SetActive (true);
				}
				grid.transform.localScale = new Vector3 (sizeX, sizeY, sizeZ);
				grid.transform.position = buildingPosition + new Vector3 (0, sizeY / 2, 0);
				grid.GetComponent<Renderer> ().sharedMaterial.SetFloat ("_Size", constructorSize);
				ClearConstructionArea ();
				beforeOrbitMode = orbitMode;
				beforeFreeMode = freeMode;
				beforeConstructorPosition = transform.position;
				beforeEnableColliders = env.enableColliders;
				env.enableColliders = false;
				orbitMode = false;
				freeMode = false;
				MoveTo (grid.transform.position);
				isFlying = true;
				limitBounds = new Bounds (grid.transform.position, new Vector3 (sizeX - 1, sizeY - 1, sizeZ - 1));
				limitBoundsEnabled = true;
				UpdateVoxelHighlight ();
				voxelHighlightBuilder.SetActive (true);
				RefreshConstructorCanvas ();
				Selection.activeGameObject = gameObject;
			} else {
				if (grid != null) {
					grid.SetActive (false);
				}
				if (voxelHighlightBuilder != null) {
					voxelHighlightBuilder.SetActive (false);
				}
				isFlying = false;
				limitBoundsEnabled = false;
				MoveTo (beforeConstructorPosition);
				orbitMode = beforeOrbitMode;
				freeMode = beforeFreeMode;
				env.enableColliders = beforeEnableColliders;
			}
		}

		void UpdateConstructor () {
												
			if (!env.buildMode)
				return;

			if (Input.GetKeyDown (KeyCode.V)) {
				ToggleConstructor ();
			}

			if (!env.constructorMode)
				return;

			if (loadModel != null && prevModel != loadModel) {
				prevModel = loadModel;
				LoadModel ();
			}

			UpdateVoxelHighlight ();

			if (Input.GetKeyDown (KeyCode.F5)) {
				LoadModel ();
			} else if (Input.GetKeyDown (KeyCode.F6)) {
				SaveModel ();
			} else if (Input.GetKey (KeyCode.LeftControl)) {
				if (Input.GetKeyDown (KeyCode.A)) {
					DisplaceModel (-1, 0, 0);
				} else if (Input.GetKeyDown (KeyCode.D)) {
					DisplaceModel (1, 0, 0);
				} else if (Input.GetKeyDown (KeyCode.S)) {
					DisplaceModel (0, 0, -1);
				} else if (Input.GetKeyDown (KeyCode.W)) {
					DisplaceModel (0, 0, 1);
				} else if (Input.GetKeyDown (KeyCode.Q)) {
					DisplaceModel (0, -1, 0);
				} else if (Input.GetKeyDown (KeyCode.E)) {
					DisplaceModel (0, 1, 0);
				}
			}

		}

		void UpdateVoxelHighlight () {
			if (voxelHighlightBuilder == null) {
				voxelHighlightBuilder = Instantiate<GameObject> (Resources.Load<GameObject> ("VoxelPlay/Prefabs/VoxelHighlight"));
			}

			Vector3 rawPos;
			if (crosshairOnBlock) {
				rawPos = crosshairHitInfo.voxelCenter + crosshairHitInfo.normal;
			} else {
				rawPos = m_Camera.transform.position + m_Camera.transform.forward * 4f;
			}

			// Bound check
			for (int i = 0; i < 50; i++) {
				if (limitBounds.Contains (rawPos))
					break;
				rawPos -= m_Camera.transform.forward * 0.1f;
			}
																
			rawPos.x = FastMath.FloorToInt (rawPos.x) + 0.5f;
			if (rawPos.x > limitBounds.max.x)
				rawPos.x = limitBounds.max.x - 0.5f;
			if (rawPos.x < limitBounds.min.x)
				rawPos.x = limitBounds.min.x + 0.5f;
			rawPos.y = FastMath.FloorToInt (rawPos.y) + 0.5f;
			if (rawPos.y > limitBounds.max.y)
				rawPos.y = limitBounds.max.y - 0.5f;
			if (rawPos.y < limitBounds.min.y)
				rawPos.y = limitBounds.min.y + 0.5f;
			rawPos.z = FastMath.FloorToInt (rawPos.z) + 0.5f;
			if (rawPos.z > limitBounds.max.z)
				rawPos.z = limitBounds.max.z - 0.5f;
			if (rawPos.z < limitBounds.min.z)
				rawPos.z = limitBounds.min.z + 0.5f;
			voxelHighlightBuilder.transform.position = rawPos;
		}

		void LoadModel () {
			if (loadModel == null) {
				DisplayDialog ("Load Model", "Please assign an existing Model Definition file to the loadModel property of Voxel Play FPS Controller in the inspector.", "Ok");
				return;
			}

			if (!DisplayDialog ("Load Model?", "Discard any change and load model?", "Yes", "No"))
				return;

			GetModelSize ();

			// Clear building location
			ClearConstructionArea ();

			// Loads model content
			Vector3 basePosition = buildingPosition;
			for (int k = 0; k < loadModel.bits.Length; k++) {
				Vector3 pos = basePosition - new Vector3 (loadModel.offsetX, loadModel.offsetY, loadModel.offsetZ); // ignore offset
				env.ModelPlace (pos, loadModel, 0, 1f, false);
			}
		}

		void ClearConstructionArea () {
			for (int y = 0; y < sizeY; y++) {
				for (int z = 0; z < sizeZ; z++) {
					for (int x = 0; x < sizeX; x++) {
						env.VoxelDestroy (buildingPosition + new Vector3 (x - sizeX / 2, y, z - sizeZ / 2));
					}
				}
			}
		}

		void SaveModel () {

			string assetPathAndName = AssetDatabase.GetAssetPath (loadModel);
			string modeFilename = assetPathAndName == "" ? "Assets/NewModelDefinition.asset" : assetPathAndName;
				
			if (!DisplayDialog ("Save Model?", "Save current model to file " + modeFilename + "? (Existing file will be replaced)", "Yes", "No"))
				return;

			bool isNew = false;
			if (loadModel == null) {
				loadModel = ScriptableObject.CreateInstance<ModelDefinition> ();
				isNew = true;
			}
			List<ModelBit> bits = new List<ModelBit> ();
			for (int y = 0; y < sizeY; y++) {
				for (int z = 1; z < sizeZ; z++) {
					for (int x = 0; x < sizeX; x++) {
						Voxel voxel = env.GetVoxel (buildingPosition + new Vector3 (x - sizeX / 2, y, z - sizeZ / 2));
						if (voxel.hasContent == 1) {
							int k = y * sizeZ * sizeX + z * sizeX + x;
							ModelBit bit = new ModelBit ();
							bit.voxelIndex = k;
							bit.voxelDefinition = voxel.type;
							bit.color = voxel.color;
							bits.Add (bit);
						}
					}
				}
			}
			loadModel.bits = bits.ToArray ();

			if (assetPathAndName == "") {
				assetPathAndName = AssetDatabase.GenerateUniqueAssetPath ("Assets/NewModelDefinition.asset");
				AssetDatabase.CreateAsset (loadModel, assetPathAndName);
			}
			EditorUtility.SetDirty (loadModel);
			AssetDatabase.SaveAssets ();
			AssetDatabase.Refresh ();

			env.ReloadTextures ();

			RefreshConstructorCanvas ();

			if (isNew) {
				EditorUtility.FocusProjectWindow ();
				Selection.activeObject = loadModel;
				DisplayDialog ("Save Model", "New model file created successfully in " + assetPathAndName + ".", "Ok");
			}
		}

		void GetModelSize () {
			if (loadModel == null) {
				sizeX = sizeY = sizeZ = constructorSize;
			} else {
				sizeX = loadModel.sizeX;
				sizeY = loadModel.sizeY;
				sizeZ = loadModel.sizeZ;
			}
		}

		void DisplaceModel (int dx, int dy, int dz) {
			VoxelChunk chunk;
			if (!env.GetChunk (buildingPosition, out chunk, false)) {
				Debug.Log ("Unexpected error: chunk not found.");
				return;
			}
			Voxel[] newContents = new Voxel[sizeY * sizeZ * sizeX];
			int ny, nz, nx;
			for (int y = 0; y < sizeY; y++) {
				ny = y + dy;
				if (ny >= sizeY)
					ny -= sizeY;
				else if (ny < 0)
					ny += sizeY;
				for (int z = 0; z < sizeZ; z++) {
					nz = z + dz;
					if (nz >= sizeZ)
						nz -= sizeZ;
					else if (nz < 0)
						nz += sizeZ;
					for (int x = 0; x < sizeX; x++) {
						Voxel voxel = env.GetVoxel (buildingPosition + new Vector3 (x - sizeX / 2, y, z - sizeZ / 2));
						if (voxel.hasContent == 1) {
							nx = x + dx;
							if (nx >= sizeX)
								nx -= sizeX;
							else if (nx < 0)
								nx += sizeX;
							newContents [ny * sizeY * sizeZ + nz * sizeX + nx] = voxel;
						}
					}
				}
			}

			// Replace voxels
			ClearConstructionArea ();
			for (int y = 0; y < sizeY; y++) {
				for (int z = 0; z < sizeZ; z++) {
					for (int x = 0; x < sizeX; x++) {
						int voxelIndex = y * sizeY * sizeZ + z * sizeX + x;
						if (!newContents [voxelIndex].isEmpty) {
							env.VoxelPlace (buildingPosition + new Vector3 (x - sizeX / 2, y, z - sizeZ / 2), newContents [voxelIndex]);
						}
					}
				}
			}
		}

		bool DisplayDialog (string title, string message, string ok, string cancel = null) {
			mouseLook.SetCursorLock (false);
			bool res = EditorUtility.DisplayDialog (title, message, ok, cancel);
			mouseLook.SetCursorLock (true);
			return res;
		}

		#endif

	}
}
