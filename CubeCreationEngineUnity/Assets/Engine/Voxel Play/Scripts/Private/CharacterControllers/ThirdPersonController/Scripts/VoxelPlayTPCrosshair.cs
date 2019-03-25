using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {


	public partial class VoxelPlayThirdPersonController : VoxelPlayCharacterControllerBase {

		[Header ("Crosshair")]
		public bool crosshairEnabled = true;
		public float crosshairScale = 0.1f;
		public float targetAnimationSpeed = 0.75f;
		public float targetAnimationScale = 0.2f;
		[Tooltip("Max distance from character to selection")]
		public float crosshairMaxDistance = 30f;
		public Color crosshairOnTargetColor = Color.yellow;
		public Color crosshairNormalColor = Color.white;
		[Tooltip ("Crosshair will change over a reachable voxel.")]
		public bool changeOnBlock = true;
		[Tooltip ("When enabled, crosshair colors invert according to background color to enhance visibility. This feature uses GrabPass which can be expensive on mobile.")]
		public bool autoInvertColors = true;

		[Header ("Voxel Highlight")]
		public bool voxelHighlight = true;
		public Color voxelHighlightColor = Color.yellow;
		[Range (1f, 100f)]
		public float voxelHighlightEdge = 20f;


		Transform crosshair;
		const string CROSSHAIR_NAME = "Crosshair";
		Material crosshairMat;

		void InitCrosshair () {
			if (env.crosshairPrefab == null) {
				Debug.LogError ("Crosshair prefab not assigned to this world.");
				return;
			} 
			GameObject obj = Instantiate<GameObject> (env.crosshairPrefab);
			obj.name = CROSSHAIR_NAME;
			if (autoInvertColors) {
				crosshairMat = Resources.Load<Material> ("VoxelPlay/Materials/VP Crosshair");
			} else {
				crosshairMat = Resources.Load<Material> ("VoxelPlay/Materials/VP Crosshair No GrabPass");
			}
			crosshairMat = Instantiate<Material> (crosshairMat);
			obj.GetComponent<Renderer> ().sharedMaterial = crosshairMat;
			if (env.crosshairTexture != null) {
				crosshairMat.mainTexture = env.crosshairTexture;
			}
			crosshair = obj.transform;
			crosshair.SetParent (m_Camera.transform, false);
			ResetCrosshairPosition ();

		}

		void ResetCrosshairPosition () {
			UpdateCrosshairScreenPosition ();
			crosshair.localRotation = Misc.quaternionZero;
			crosshair.localScale = Misc.vector3one * crosshairScale;
			crosshairMat.color = crosshairNormalColor;
			env.VoxelHighlight (false);
		}

		void UpdateCrosshairScreenPosition () {
			Vector3 scrPos = input.screenPos;
			scrPos.z = m_Camera.nearClipPlane + 0.001f;
			Vector3 newPosition = m_Camera.ScreenToWorldPoint (scrPos);
			crosshair.position = newPosition;
		}


		void LateUpdate () {

			if (env == null || !env.applicationIsPlaying || !crosshairEnabled)
				return;

			UpdateCrosshairScreenPosition ();

			Ray ray = m_Camera.ScreenPointToRay (input.screenPos);
			VoxelHitInfo hitInfo;

			// Check if there's a voxel in range
			crosshairOnBlock = env.RayCast (ray, out hitInfo) && hitInfo.voxelIndex >= 0; 
			if (!input.GetButton (InputButtonNames.Button1) || crosshairHitInfo.GetVoxelNow().isEmpty) {
				crosshairHitInfo = hitInfo;
			}
			if (crosshairOnBlock) {
				crosshairOnBlock = FastVector.SqrDistance (ref crosshairHitInfo.voxelCenter, ref curPos) < crosshairMaxDistance * crosshairMaxDistance;
				if (!crosshairOnBlock) {
					crosshairHitInfo.Clear ();
				}
			}
			if (changeOnBlock) {
				if (!crosshairOnBlock) {
					ResetCrosshairPosition ();
					return;
				}
				// Puts crosshair over the voxel but do it only if crosshair won't disappear because of the angle or it's switching from orbit to free mode (or viceversa)
				float d = Vector3.Dot (ray.direction, crosshairHitInfo.normal);
				if (d < -0.2f) {
					crosshair.position = hitInfo.point;
					crosshair.LookAt (hitInfo.point + crosshairHitInfo.normal);
				} else {
					crosshair.localRotation = Misc.quaternionZero;
				}
				crosshairMat.color = crosshairOnTargetColor;

			}
			if (crosshairOnBlock) {
				crosshair.localScale = Misc.vector3one * (crosshairScale * (1f - targetAnimationScale * 0.5f + Mathf.PingPong (Time.time * targetAnimationSpeed, targetAnimationScale)));
				env.VoxelHighlight (crosshairHitInfo, voxelHighlightColor, voxelHighlightEdge);
			}
		}

	}




}

