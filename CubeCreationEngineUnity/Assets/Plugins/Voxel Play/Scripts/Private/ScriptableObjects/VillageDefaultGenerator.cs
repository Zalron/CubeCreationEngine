using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	[CreateAssetMenu(menuName = "Voxel Play/Detail Generators/Village Generator", fileName = "VillageGenerator", order = 102)]
	public class VillageDefaultGenerator : VoxelPlayDetailGenerator {

		public ModelDefinition[] buildings;

		struct BuildingStatus {
			public float height;
			public bool placementStatus;
		}


		VoxelPlayEnvironment env;
		// x,y,z chunk position  w cached terrain height
		Dictionary<Vector3, BuildingStatus> buildingPositions;

		/// <summary>
		/// Initialization method. Called by Voxel Play at startup.
		/// </summary>
		public override void Init() {
			env = VoxelPlayEnvironment.instance;
			buildingPositions = new Dictionary<Vector3, BuildingStatus>(100);

			// Fill models with empty blocks so they clear any terrain or vegetation inside them when placing on the world
			if (buildings != null && buildings.Length > 0) {
				for (int k = 0; k < buildings.Length; k++) {
					env.ModelFillInside(buildings[k]);
				}
			}
		}


		/// <summary>
		/// Called by Voxel Play to inform that player has moved onto another chunk so new detail can start generating
		/// </summary>
		/// <param name="currentPosition">Current player position.</param>
		/// <param name="checkOnlyBorders">True means the player has moved to next chunk. False means player position is completely new and all chunks in
		/// range should be checked for detail in this call.</param>
		/// <param name="position">Position.</param>
		public override void ExploreArea(Vector3 position, bool checkOnlyBorders) {
			int explorationRange = env.visibleChunksDistance + 10;
			int minz = -explorationRange;
			int maxz = +explorationRange;
			int minx = -explorationRange;
			int maxx = +explorationRange;
			Vector3 pos = position;
			for (int z = minz; z <= maxz; z++) {
				for (int x = minx; x < maxx; x++) {
					if (checkOnlyBorders && z > minz && z < maxz && x > minx && x < maxx) continue;
					pos.x = position.x + x * 16;
					pos.z = position.z + z * 16;
					pos = env.GetChunkPosition(pos);
					if (WorldRand.GetValue(pos) > 0.98f) {
						BuildingStatus bs;
						if (!buildingPositions.TryGetValue(pos, out bs)) {
							float h = env.GetTerrainHeight(pos, false);
							if (h > env.waterLevel) {
								bs.height = h;
								bs.placementStatus = false;

								// No trees on this chunk
								VoxelChunk chunk;
								env.GetChunk(pos, out chunk, false);
								if (chunk != null) {
									chunk.allowTrees = false;
								}
							} else {
								bs.placementStatus = true;
							}
							buildingPositions[pos] = bs;
						}
					}
				}
			}
		}


		/// <summary>
		/// Fills the given chunk with detail. Filled voxels won't be replaced by the terrain generator.
		/// Use Voxel.Empty to fill with void.
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		public override void AddDetail(VoxelChunk chunk) {

			// if chunk is within distance any village center, render the village
			BuildingStatus bs;
			if (buildingPositions.TryGetValue(chunk.position, out bs)) {
				if (!bs.placementStatus) {
					bs.placementStatus = true;
					buildingPositions[chunk.position] = bs;
					Vector3 pos = chunk.position;
					pos.y = bs.height;
					ModelDefinition buildingModel = buildings[Random.Range(0, buildings.Length)];
					env.ModelPlace(pos, buildingModel, 360, Random.value * 0.2f + 0.8f, true);
				}
			}
		}




	}

}