using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {
				
	public struct Vector3i {
		public int x, y, z;

		public Vector3i(int x, int y, int z) {
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public Vector3i(Vector3 v) {
			FastMath.FloorToInt (v.x, v.y, v.z, out x, out y, out z);
		}

	}

}