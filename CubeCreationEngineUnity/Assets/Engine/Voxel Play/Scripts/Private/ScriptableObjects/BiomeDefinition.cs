using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	[System.Serializable]
	public struct BiomeZone {
		[Range (0, 1f)]
		public float elevationMin;
		[Range (0, 1f)]
		public float elevationMax;
		[Range (0, 1f)]
		public float moistureMin;
		[Range (0, 1f)]
		public float moistureMax;
	}

	[System.Serializable]
	public struct BiomeTree {
		public ModelDefinition tree;
		public float probability;
	}

	[System.Serializable]
	public struct BiomeVegetation {
		public VoxelDefinition vegetation;
		public float probability;
	}

	[System.Serializable]
	public struct BiomeOre {
		public VoxelDefinition ore;
		[Range (0, 1)]
		public float probabilityMin;
		[Range (0, 1)]
		public float probabilityMax;
		public int depthMin;
		public int depthMax;
	}


	[CreateAssetMenu (menuName = "Voxel Play/Biome Definition", fileName = "BiomeDefinition", order = 100)]
	public partial class BiomeDefinition : ScriptableObject {
		public VoxelDefinition voxelTop;
		public VoxelDefinition voxelDirt;
		[Range (0, 0.05f)]
		public float treeDensity = 0.02f;
		public BiomeTree[] trees;
		public float vegetationDensity = 0.05f;
		public BiomeVegetation[] vegetation;
		public BiomeZone[] zones;
		public BiomeOre[] ores;
	}

}