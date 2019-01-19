using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay.GPUInstancing {

	public class VoxelPlayInstancingRendererManager {
		Material defaultInstancingMaterial;
		FastIndexedList<VoxelChunk, InstancedChunk> instancedChunks;
		FastList<BatchedMesh> batchedMeshes;
		public bool rebuild;
		VoxelPlayEnvironment env;
		int lastRebuildFrame;

		public  VoxelPlayInstancingRendererManager (VoxelPlayEnvironment env) {
			this.env = env;
			defaultInstancingMaterial = Resources.Load<Material> ("VoxelPlay/Materials/VP Model VertexLit");
			instancedChunks = new FastIndexedList<VoxelChunk, InstancedChunk> ();
			batchedMeshes = new FastList<BatchedMesh> ();
		}

		public void ClearChunk (VoxelChunk chunk) {
			InstancedChunk instancedChunk;
			if (instancedChunks.TryGetValue (chunk, out instancedChunk)) {
				instancedChunk.Clear ();
				rebuild = true;
			}
		}

		public void AddVoxel (VoxelChunk chunk, int voxelIndex, Vector3 position, Quaternion rotation, Vector3 scale) {

			VoxelDefinition voxelDefinition = env.voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];

			// Ensure there're batches for this voxel definition
			if (voxelDefinition.batchedIndex < 0) {
				BatchedMesh batchedMesh = new BatchedMesh (voxelDefinition);
				Material material = voxelDefinition.material;
				if (material == null) {
					material = defaultInstancingMaterial;
				}
				batchedMesh.material = material;
				voxelDefinition.batchedIndex = batchedMeshes.Add (batchedMesh);
			}

			// Add chunk and voxel to the rendering lists
			InstancedChunk instancedChunk;
			if (!instancedChunks.TryGetValue (chunk, out instancedChunk)) {
				instancedChunk = new InstancedChunk (chunk);
				instancedChunks.Add (chunk, instancedChunk);
			}
			InstancedVoxel instancedVoxel = new InstancedVoxel ();
			instancedVoxel.voxelDefinition = voxelDefinition;
			instancedVoxel.position = position;
			instancedVoxel.rotation = rotation;
			instancedVoxel.scale = scale;
			instancedVoxel.color = chunk.voxels [voxelIndex].color;
			instancedVoxel.light = chunk.voxels [voxelIndex].lightMesh / 15f;
			instancedChunk.instancedVoxels.Add (instancedVoxel);
			rebuild = true;
		}

		void RebuildZoneRenderingLists (Vector3 observerPos, float visibleDistance) {
			// rebuild batch lists to be used in the rendering loop
			for (int k = 0; k < batchedMeshes.count; k++) {
				BatchedMesh batchedMesh = batchedMeshes.values [k];
				batchedMesh.batches.Clear ();
			}

			float cullDistance = (visibleDistance * 16) * (visibleDistance * 16);

			for (int j = 0; j <= instancedChunks.lastIndex; j++) {
				InstancedChunk instancedChunk = instancedChunks.values [j];
				if (instancedChunk == null)
					continue;
				// check if chunk is in area
				Vector3 chunkCenter = instancedChunk.chunk.position;
				if (FastVector.SqrDistance (ref chunkCenter, ref observerPos) > cullDistance)
					continue;
					
				// add instances to batch
				InstancedVoxel[] voxels = instancedChunk.instancedVoxels.values;
				for (int i = 0; i < instancedChunk.instancedVoxels.count; i++) {
					VoxelDefinition vd = voxels [i].voxelDefinition;
					BatchedMesh batchedMesh = batchedMeshes.values [vd.batchedIndex];
					Batch batch = batchedMesh.batches.last;
					if (batch == null || batch.instancesCount >= Batch.MAX_INSTANCES) {
						batch = batchedMesh.batches.FetchDirty ();
						if (batch == null) {
							batch = new Batch ();
							batchedMesh.batches.Add (batch);
						} else {
							batch.Init ();
						}
					}
					int pos = batch.instancesCount++;
					batch.matrices [pos].SetTRS (voxels [i].position, voxels [i].rotation, voxels [i].scale);
					batch.color [pos].x = voxels [i].color.r / 255f;
					batch.color [pos].y = voxels [i].color.g / 255f;
					batch.color [pos].z = voxels [i].color.b / 255f;
					batch.color [pos].w = 1f;
					batch.light [pos] = voxels [i].light;
				}
			}

			for (int k = 0; k < batchedMeshes.count; k++) {
				BatchedMesh batchedMesh = batchedMeshes.values [k];
				for (int j = 0; j < batchedMesh.batches.count; j++) {
					Batch batch = batchedMesh.batches.values [j];
					batch.materialPropertyBlock.SetVectorArray ("_Color", batch.color);
					batch.materialPropertyBlock.SetFloatArray ("_VoxelLight", batch.light);
				}
			}
		}

		public void Render (Vector3 observerPos, float visibleDistance) {
			if (rebuild) {
				if (!Application.isPlaying || Time.frameCount - lastRebuildFrame > 10) {
					lastRebuildFrame = Time.frameCount;
					RebuildZoneRenderingLists (observerPos, visibleDistance);
					rebuild = false;
				}
			}
			for (int k = 0; k < batchedMeshes.count; k++) {
				BatchedMesh batchedMesh = batchedMeshes.values [k];
				VoxelDefinition vd = batchedMesh.voxelDefinition;
				Mesh mesh = vd.mesh;
				Material material = batchedMesh.material;
				ShadowCastingMode shadowCastingMode = vd.castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
				for (int j = 0; j < batchedMesh.batches.count; j++) {
					Batch batch = batchedMesh.batches.values [j];
					Graphics.DrawMeshInstanced (mesh, 0, material, batch.matrices, batch.instancesCount, batch.materialPropertyBlock, shadowCastingMode, vd.receiveShadows, env.layerVoxels);
				}
			}
		}


	}
}
