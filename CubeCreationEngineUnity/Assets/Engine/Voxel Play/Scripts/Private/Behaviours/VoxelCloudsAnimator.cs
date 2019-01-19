using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public class VoxelCloudsAnimator : MonoBehaviour {

		[NonSerialized]
		public List<VoxelChunk> cloudChunks;

		int cloudCount;
		int cloudIndex;

		void Start() {
			cloudCount = cloudChunks.Count;
		}

		void LateUpdate() {
			if (cloudChunks != null && cloudChunks[cloudIndex] != null) {
				transform.position += Misc.vector3left * Time.deltaTime;
				Transform cloudTransform = cloudChunks[cloudIndex].transform;
				Vector3 refPos = VoxelPlayEnvironment.instance.cameraMain != null ? VoxelPlayEnvironment.instance.cameraMain.transform.position : Misc.vector3zero;
				if (cloudTransform.position.x < refPos.x - 512) {
					cloudTransform.position += Misc.vector3right * 1024;
				}
				cloudChunks[cloudIndex].position = cloudTransform.position;
			}
			cloudIndex++;
			if (cloudIndex >= cloudCount)
				cloudIndex = 0;
		}


	}

}