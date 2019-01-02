using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	[Serializable]
	public struct ModelBit {
		public int voxelIndex;
		public VoxelDefinition voxelDefinition;
		public bool isEmpty;
		public Color32 color;

		public Color32 colorOrDefault {
			get {
				if (color.r == 0 && color.g == 0 && color.b == 0 && color.a == 0) {
					return Misc.color32White;
				}
				return color;
			}
		}
	}

	[CreateAssetMenu(menuName = "Voxel Play/Model Definition", fileName = "ModelDefinition", order = 102)]
	public partial class ModelDefinition : ScriptableObject {
		/// <summary>
		/// Size of the model (axis X)
		/// </summary>
		public int sizeX = 16;
		/// <summary>
		/// Size of the model (axis Y)
		/// </summary>
		public int sizeY = 16;
		/// <summary>
		/// Size of the model (axis Z)
		/// </summary>
		public int sizeZ = 16;
		/// <summary>
		/// Offset of the model with respect to the placement position (axis X);
		/// </summary>
		public int offsetX = 0;
		/// <summary>
		/// Offset of the model with respect to the placement position (axis Y);
		/// </summary>
		public int offsetY = 0;
		/// <summary>
		/// Offset of the model with respect to the placement position (axis Z);
		/// </summary>
		public int offsetZ = 0;
		/// <summary>
		/// Array of model bits.
		/// </summary>
		public ModelBit[] bits;
	}

}