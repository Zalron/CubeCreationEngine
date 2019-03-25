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

		/// <summary>
		/// The final color combining bit tint color and voxel definition tint color
		/// </summary>
		[NonSerialized]
		public Color32 finalColor;
	}

	[CreateAssetMenu (menuName = "Voxel Play/Model Definition", fileName = "ModelDefinition", order = 102)]
	[HelpURL ("https://kronnect.freshdesk.com/support/solutions/articles/42000033382-model-definitions")]
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
		/// The duration of the build in seconds.
		/// </summary>
		public float buildDuration = 5f;

		/// <summary>
		/// Array of model bits.
		/// </summary>
		public ModelBit[] bits;

		/// <summary>
		/// Used temporarily to cache the gameobject generated from the model definition
		/// </summary>
		[NonSerialized, HideInInspector]
		public GameObject modelGameObject;


		void OnEnable () {
			ComputeFinalColors ();
		}

		public void ComputeFinalColors () {
			for (int k = 0; k < bits.Length; k++) {
				Color32 color = bits [k].color;
				if (color.r == 0 && color.g == 0 && color.b == 0) {
					color = Misc.color32White;
				}
				if (bits [k].voxelDefinition != null) {
					color = color.MultiplyRGB (bits [k].voxelDefinition.tintColor);
				}
				bits [k].finalColor = color;
			}
		}


	}

}