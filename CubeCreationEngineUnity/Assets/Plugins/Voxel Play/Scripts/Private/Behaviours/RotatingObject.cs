using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public class RotatingObject : MonoBehaviour {

		public float speed = 10f;

		void Update () {
			transform.Rotate (Misc.vector3forward * (Time.deltaTime * speed));
		}

	}

}