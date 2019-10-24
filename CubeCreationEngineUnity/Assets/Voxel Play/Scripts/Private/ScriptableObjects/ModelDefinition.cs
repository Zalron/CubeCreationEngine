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
		public float rotation;

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
		public int offsetX;
		/// <summary>
		/// Offset of the model with respect to the placement position (axis Y);
		/// </summary>
		public int offsetY;
		/// <summary>
		/// Offset of the model with respect to the placement position (axis Z);
		/// </summary>
		public int offsetZ;

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




		public Vector3 size {
			get {
				return new Vector3 (sizeX, sizeY, sizeZ);
			}
		}

		public Vector3 offset {
			get {
				return new Vector3 (offsetX, offsetY, offsetZ);
			}
		}

        Bounds _bounds;

        /// <summary>
        /// The real boundary of visible voxels within the model definition
        /// </summary>
        public Bounds bounds {
            get {
                return _bounds;
            }
        }


        void OnEnable() {
            ComputeFinalColors();
            ComputeBounds();
        }


        public void ComputeFinalColors() {
            if (bits == null) return;
            for (int k = 0; k < bits.Length; k++) {
                Color32 color = bits[k].color;
                if (color.r == 0 && color.g == 0 && color.b == 0) {
                    color = Misc.color32White;
                }
                if (bits[k].voxelDefinition != null) {
                    color = color.MultiplyRGB(bits[k].voxelDefinition.tintColor);
                }
                bits[k].finalColor = color;
            }
        }


        public void ComputeBounds() {
            if (bits == null) return;
            int minX, minZ, minY;
            int maxX, maxZ, maxY;
            minX = minZ = minY = int.MaxValue;
            maxX = maxZ = maxY = int.MinValue;

            int modelOneYRow = sizeZ * sizeX;
            int modelOneZRow = sizeX;

            for (int b = 0; b < bits.Length; b++) {
                if (bits[b].isEmpty) continue;
                int bitIndex = bits[b].voxelIndex;
                int py = bitIndex / modelOneYRow;
                int remy = bitIndex - py * modelOneYRow;
                int pz = remy / modelOneZRow;
                int px = remy - pz * modelOneZRow;

                if (px < minX) minX = px;
                if (px > maxX) maxX = px;
                if (py < minY) minY = py;
                if (py > maxY) maxY = py;
                if (pz < minZ) minZ = pz;
                if (pz > maxZ) maxZ = pz;
            }

            Vector3 size = new Vector3(maxX - minX + 1, maxY - minY + 1, maxZ - minZ + 1);
            Vector3 center = new Vector3((maxX + minX) * 0.5f, (maxY + minY) * 0.5f, (maxZ + minZ) * 0.5f);
            _bounds = new Bounds (center, size);
        }
    }
}