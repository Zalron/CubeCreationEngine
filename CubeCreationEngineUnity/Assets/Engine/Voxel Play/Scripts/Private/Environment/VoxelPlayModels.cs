using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {
		
		public IEnumerator ModelPlaceWithDuration (Vector3 position, ModelDefinition model, float buildDuration, int rotationDegrees = 0, float colorBrightness = 1f, bool fitTerrain = false, VoxelPlayModelBuildEvent callback = null) {

			if (OnModelBuildStart != null) {
				OnModelBuildStart (model, position);
			}
			int currentIndex = 0;
			int len = model.bits.Length - 1;
			float startTime = Time.time;
			float t = 0;
			WaitForEndOfFrame w = new WaitForEndOfFrame ();
			while (t < 1f) {
				t = (Time.time - startTime) / buildDuration;
				if (t >= 1f) {
					t = 1f;
				}
				int lastIndex = (int)(len * t);
				if (lastIndex >= currentIndex) {
					ModelPlace (position, model, rotationDegrees, colorBrightness, fitTerrain, null, currentIndex, lastIndex);
					currentIndex = lastIndex + 1;
				}
				yield return w;
			}
			if (callback != null) {
				callback (model, position);
			}
			if (OnModelBuildEnd != null) {
				OnModelBuildEnd (model, position);
			}

		}

	}



}
