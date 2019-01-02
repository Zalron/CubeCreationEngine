using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay.GPUInstancing {

	struct InstancedVoxel {
		public VoxelDefinition voxelDefinition;
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 scale;
		public Color32 color;
		public float light;
	}

}
