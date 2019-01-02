using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public enum VOXELPLAY_SKYBOX {
		UserDefined = 0,
		Earth = 1,
		Space = 2,
		EarthSimplified = 3,
		EarthNightCubemap = 4,
		EarthDayNightCubemap = 5
	}


	[CreateAssetMenu (menuName = "Voxel Play/World Definition", fileName = "WorldDefinition", order = 103)]
	public partial class WorldDefinition : ScriptableObject {

		public int seed;

		public BiomeDefinition[] biomes;

		[Tooltip("Default biome used if no biome matches altitude/moisture at a given position. Optional.")]
		public BiomeDefinition defaultBiome;

		[Header ("Terrain")]
		public VoxelPlayTerrainGenerator terrainGenerator;

		[Tooltip ("Used by terrain generator to set a hard limit in chunks at minimum height")]
		public VoxelDefinition bedrockVoxel;

		[Header ("Detail")]
		public VoxelPlayDetailGenerator[] detailGenerators;

		[Header ("Sky & Lighting")]
		public VOXELPLAY_SKYBOX skyboxDesktop = VOXELPLAY_SKYBOX.Earth;
		public VOXELPLAY_SKYBOX skyboxMobile = VOXELPLAY_SKYBOX.EarthSimplified;
		public Texture skyboxDayCubemap, skyboxNightCubemap;

		[Range (-10, 10)]
		public float dayCycleSpeed = 1f;

		public bool setTimeAndAzimuth;

		[Range (0, 24)]
		public float timeOfDay = 0f;

		[Range(0,360)]
		public float azimuth = 15f;

		[Range (0, 2f)]
		public float exposure = 1f;

		[Tooltip ("Used to create clouds")]
		public VoxelDefinition cloudVoxel;

		[Range (0, 255)]
		public int cloudCoverage = 110;

		[Range (0, 255)]
		public int cloudAltitude = 150;

		public Color skyTint = new Color (0.52f, 0.5f, 1f);

		public float lightScattering = 1f;
		public float lightIntensityMultiplier = 1f;

		[Header ("FX")]
		[Tooltip("Duration for the emission animation on certain materials")]
		public float emissionAnimationSpeed = 0.5f;
		public float emissionMinIntensity = 0.5f;
		public float emissionMaxIntensity = 1.2f;

		[Tooltip("Duration for the voxel damage cracks")]
		public float damageDuration = 3f;
		public Texture2D[] voxelDamageTextures;
		public float gravity = -9.8f;

		[Tooltip("When set to true, voxel types with 'Trigger Collapse' will fall along nearby voxels marked with 'Will Collapse' flag")]
		public bool collapseOnDestroy = true;

		[Tooltip("The maximum number of voxels that can fall at the same time")]
		public int collapseAmount = 50;

		[Tooltip("Delay for consolidating collapsed voxels into normal voxels. A value of zero keeps dynamic voxels in the scene. Note that consolidation takes place when chunk is not in frustum to avoid visual glitches.")]
		public int consolidateDelay = 5;

		[Header ("Additional Objects")]
		public VoxelDefinition[] moreVoxels;
		public ItemDefinition[] items;

	}

}