using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		const int ONE_Y_ROW = 16 * 16;
		const int ONE_Z_ROW = 16;
		const string CHUNKS_ROOT = "Chunks Root";

		// Optimization support
		VoxelChunk lastChunkFetch;
		int lastChunkFetchX, lastChunkFetchY, lastChunkFetchZ;
		object lockLastChunkFetch = new object ();

		#region Chunk functions

		void GetChunkCoordinates (Vector3 position, out int chunkX, out int chunkY, out int chunkZ) {
			chunkX = FastMath.FloorToInt (position.x / 16);
			chunkY = FastMath.FloorToInt (position.y / 16);
			chunkZ = FastMath.FloorToInt (position.z / 16);
		}

		int GetChunkHash (int chunkX, int chunkY, int chunkZ) {
			int x00 = WORLD_SIZE_DEPTH * WORLD_SIZE_HEIGHT * (chunkX + WORLD_SIZE_WIDTH);
			int y00 = WORLD_SIZE_DEPTH * (chunkY + WORLD_SIZE_HEIGHT);
			return x00 + y00 + chunkZ;
		}

		/// <summary>
		/// Gets the chunk if exits or create it if forceCreation is set to true.
		/// </summary>
		/// <returns><c>true</c>, if chunk fast was gotten, <c>false</c> otherwise.</returns>
		/// <param name="chunkX">Chunk x.</param>
		/// <param name="chunkY">Chunk y.</param>
		/// <param name="chunkZ">Chunk z.</param>
		/// <param name="chunk">Chunk.</param>
		/// <param name="createIfNotAvailable">If set to <c>true</c> force creation if chunk doesn't exist.</param>
		bool GetChunkFast (int chunkX, int chunkY, int chunkZ, out VoxelChunk chunk, bool createIfNotAvailable = false) {
			lock (lockLastChunkFetch) {
				if (lastChunkFetchX == chunkX && lastChunkFetchY == chunkY && lastChunkFetchZ == chunkZ && (object)lastChunkFetch != null) {
					chunk = lastChunkFetch;
					return true;
				}
			}
			int hash = GetChunkHash (chunkX, chunkY, chunkZ);
			STAGE = 501;
			CachedChunk cachedChunk;
			cachedChunks.TryGetValue (hash, out cachedChunk);
			bool exists = (object)cachedChunk != null;
			if (createIfNotAvailable) {
				if (!exists) {
					STAGE = 502;
					// not yet created, create it
					cachedChunk = new CachedChunk ();
					cachedChunk.chunk = CreateChunk (hash, chunkX, chunkY, chunkZ, false);
					exists = true;
				}
				if ((object)cachedChunk.chunk == null) { // chunk is really empty, create it with empty space
					STAGE = 503;
					cachedChunk.chunk = CreateChunk (hash, chunkX, chunkY, chunkZ, true);
				}
			}
			STAGE = 0;
			if (exists) {
				chunk = cachedChunk.chunk;
				lock (lockLastChunkFetch) {
					lastChunkFetchX = chunkX;
					lastChunkFetchY = chunkY;
					lastChunkFetchZ = chunkZ;
					lastChunkFetch = chunk;
				}
				return chunk != null;
			} else {
				chunk = null;
				return false;
			}
		}


		VoxelChunk GetChunkOrCreate (Vector3 position) {
			int x = FastMath.FloorToInt (position.x / 16);
			int y = FastMath.FloorToInt (position.y / 16);
			int z = FastMath.FloorToInt (position.z / 16);
			VoxelChunk chunk;
			GetChunkFast (x, y, z, out chunk, true);
			return chunk;
		}


		VoxelChunk GetChunkOrCreate (int chunkX, int chunkY, int chunkZ) {
			VoxelChunk chunk;
			GetChunkFast (chunkX, chunkY, chunkZ, out chunk, true);
			return chunk;
		}

		VoxelChunk GetChunkIfExists (int hash) {
			CachedChunk cachedChunk;
			if (cachedChunks.TryGetValue (hash, out cachedChunk)) {
				return cachedChunk.chunk;
			}
			return null;
		}


		/// <summary>
		/// Creates the chunk.
		/// </summary>
		/// <returns>The chunk.</returns>
		/// <param name="hash">Hash.</param>
		/// <param name="chunkX">Chunk x.</param>
		/// <param name="chunkY">Chunk y.</param>
		/// <param name="chunkZ">Chunk z.</param>
		/// <param name="createEmptyChunk">If set to <c>true</c> create empty chunk.</param>
		/// <param name="complete">If set to <c>true</c> detail generators will fire as well as OnChunkCreated event. Chunk will be marked as populated and a refresh will be triggered if within view distance.</param>
		VoxelChunk CreateChunk (int hash, int chunkX, int chunkY, int chunkZ, bool createEmptyChunk, bool complete = true) {

			STAGE = 101;
			Vector3 position;
			position.x = chunkX * 16 + 8;
			position.y = chunkY * 16 + 8;
			position.z = chunkZ * 16 + 8;

			STAGE = 102;
			CachedChunk cachedChunk;
			// Create entry in the dictionary
			if (!cachedChunks.TryGetValue (hash, out cachedChunk)) {
				cachedChunk = new CachedChunk ();
				cachedChunks [hash] = cachedChunk;
			}

			STAGE = 103;
			VoxelChunk chunk;
			if ((object)cachedChunk.chunk == null) {
				// Fetch a new entry in the chunks pool
				if (chunksPoolFetchNew) {
					chunksPoolFetchNew = false;
					FetchNewChunkIndex (position);
				}
				chunk = chunksPool [chunksPoolCurrentIndex];
			} else {
				chunk = cachedChunk.chunk;
			}

			// Paint voxels
			bool chunkHasContents = false;
			chunk.position = position;

			STAGE = 104;
			if (createEmptyChunk) {
				chunk.isAboveSurface = CheckIfChunkAboveTerrain (position);
				if (chunk.isAboveSurface && effectiveGlobalIllumination) {
					chunk.ClearLightmap (15);
				}
			} else {
				if (OnChunkBeforeCreate != null) {
					// allows a external function to fill the contents of this new chunk
					bool isAboveSurface = true;
					OnChunkBeforeCreate (position, out chunkHasContents, chunk.voxels, out isAboveSurface);
					chunk.isAboveSurface = isAboveSurface;
				}
				if (!chunkHasContents) {
					if (position.y < CLOUDS_SPECIAL_ALTITUDE) {
						chunkHasContents = world.terrainGenerator.PaintChunk (chunk);
					}
					if (!chunkHasContents) {
						chunk.isAboveSurface = true;
					}
				}
			}

			STAGE = 105;

			if (chunkHasContents || createEmptyChunk) {
				// Create gameobject
				if (!effectiveGlobalIllumination) {
					chunk.ClearLightmap (15);
				}
				chunksPoolFetchNew = true;
				chunksCreated++;

				cachedChunk.chunk = chunk;

				if (complete) {
					chunk.isPopulated = true;

					// Check for detail generators
					if (worldHasDetailGenerators) {
						for (int d = 0; d < world.detailGenerators.Length; d++) {
							if (world.detailGenerators [d].enabled) {
								world.detailGenerators [d].AddDetail (chunk);
							}
						}
					}

					if (chunkHasContents) {
						// if chunk is near camera, request a render refresh
						bool sendRefresh = (chunkX >= visible_xmin && chunkX <= visible_xmax && chunkZ >= visible_zmin && chunkZ <= visible_zmax && chunkY >= visible_ymin && chunkY <= visible_ymax);
						if (sendRefresh) {
							ChunkRequestRefresh (chunk, false, true);
						}

						// Handle special case for chunks that have no lighting (ie. underground) so neigbhours need to be updated 
						if ((chunk.inconclusiveNeighbours & CHUNK_IS_INCONCLUSIVE) != 0) {
							VoxelChunk nchunk = chunk.bottom;
							if (nchunk != null && (nchunk.inconclusiveNeighbours & CHUNK_TOP) != 0) {
								ChunkRequestRefresh (nchunk, false, true);
							}
							nchunk = chunk.top;
							if (nchunk != null && (nchunk.inconclusiveNeighbours & CHUNK_BOTTOM) != 0) {
								ChunkRequestRefresh (nchunk, false, true);
							}
							nchunk = chunk.left;
							if (nchunk != null && (nchunk.inconclusiveNeighbours & CHUNK_RIGHT) != 0) {
								ChunkRequestRefresh (nchunk, false, true);
							}
							nchunk = chunk.right;
							if (nchunk != null && (nchunk.inconclusiveNeighbours & CHUNK_LEFT) != 0) {
								ChunkRequestRefresh (nchunk, false, true);
							}
							nchunk = chunk.back;
							if (nchunk != null && (nchunk.inconclusiveNeighbours & CHUNK_FORWARD) != 0) {
								ChunkRequestRefresh (nchunk, false, true);
							}
							nchunk = chunk.forward;
							if (nchunk != null && (nchunk.inconclusiveNeighbours & CHUNK_BACK) != 0) {
								ChunkRequestRefresh (nchunk, false, true);
							}
						}


					} else {
						chunk.renderState = CHUNK_RENDER_STATE.RenderingComplete;
					}

					if ((object)OnChunkAfterCreate != null) {
						OnChunkAfterCreate (chunk);
					}
				}

				STAGE = 0;
				return chunk;
			} else {
				chunk.renderState = CHUNK_RENDER_STATE.RenderingComplete;
				STAGE = 0;
				return null;
			}
		}

		bool CheckIfChunkAboveTerrain (Vector3 position) {

			position.y += 7;
			if (position.y < waterLevel && waterLevel > 0) {
				return false;
			}

			position.x -= 8;
			position.z -= 8;
			Vector3 pos = position;

			for (int z = 0; z < 16; z++) {
				pos.z = position.z + z;
				for (int x = 0; x < 16; x++) {
					pos.x = position.x + x;
					float groundLevel = GetHeightMapInfoFast (pos.x, pos.z).groundLevel;
					float surfaceLevel = waterLevel > groundLevel ? waterLevel : groundLevel;
					if (position.y >= surfaceLevel) {
						// chunk is above terrain or water
						return true;
					}
				}
			}

			return false;
		}


		void RefreshNineChunks (VoxelChunk chunk) {
			if (chunk == null)
				return;

			Vector3 position = chunk.position;
			int chunkX = FastMath.FloorToInt (position.x / 16f);
			int chunkY = FastMath.FloorToInt (position.y / 16f);
			int chunkZ = FastMath.FloorToInt (position.z / 16f);

			VoxelChunk neighbour;
			for (int y = -1; y <= 1; y++) {
				for (int z = -1; z <= 1; z++) {
					for (int x = -1; x <= 1; x++) {
						if (y == 0 && x == 0 && z == 0)
							continue;
						GetChunkFast (chunkX + x, chunkY + y, chunkZ + z, out neighbour);
						if (neighbour != null && neighbour != chunk) {
							ChunkRequestRefresh (neighbour, true, false);
						}
					}
				}
			}

			UpdateChunkRR (chunk);
		}


		void RebuildNeighbours(VoxelChunk chunk, int voxelIndex) {
			int px, py, pz;
			GetVoxelChunkCoordinates (voxelIndex, out px, out py, out pz);
			if (px == 0 && chunk.left != null)
				ChunkRequestRefresh (chunk.left, false, true);
			else if (px == 15 && chunk.right != null)
				ChunkRequestRefresh (chunk.right, false, true);
			if (py == 0 && chunk.bottom != null)
				ChunkRequestRefresh (chunk.bottom, false, true);
			else if (py == 15 && chunk.top != null)
				ChunkRequestRefresh (chunk.top, false, true);
			if (pz == 0 && chunk.back != null)
				ChunkRequestRefresh (chunk.back, false, true);
			else if (pz == 15 && chunk.forward != null)
				ChunkRequestRefresh (chunk.forward, false, true);
		}


		/// <summary>
		/// Clears a chunk
		/// </summary>
		void ChunkClearFast (VoxelChunk chunk) {
			chunk.ClearVoxels (noLightValue);

		}


		public bool GetChunkNavMeshIsReady (VoxelChunk chunk) {
			return chunk.navMeshSourceIndex >= 0;
		}

		#endregion

	}



}
