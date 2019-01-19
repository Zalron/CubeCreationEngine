using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{
	public enum FaceDirection {
		Top,
		Bottom,
		Left,
		Right,
		Forward,
		Back
	}

	public class VoxelPlayGreedyMesher
	{
		VoxelPlayGreedySlice[] slices;
		VoxelPlayGreedySliceFullVertexData[] slicesFull;
		bool useFullVertexData;

		public VoxelPlayGreedyMesher (bool useFullVertexData = false) 
		{
			this.useFullVertexData = useFullVertexData;
			if (useFullVertexData) {
				slicesFull = new VoxelPlayGreedySliceFullVertexData[16 * 6];
				for (int k = 0; k < slicesFull.Length; k++) {
					slicesFull [k] = new VoxelPlayGreedySliceFullVertexData ();
				}

			} else {
				slices = new VoxelPlayGreedySlice[16 * 6];
				for (int k = 0; k < slices.Length; k++) {
					slices [k] = new VoxelPlayGreedySlice ();
				}
			}
		}


		public void AddQuad (FaceDirection direction, int x, int y, int slice)
		{
			int index = (int)direction * 16 + slice;
			slices [index].AddQuad (x, y);
		}

		public void AddQuad (FaceDirection direction, int x, int y, int slice, ref Vector3 normal, ref Color32 color, float light, int textureIndex)
		{
			int index = (int)direction * 16 + slice;
			slicesFull [index].AddQuad(x, y, ref normal, ref color, light, textureIndex);
		}


		public void FlushTriangles (List<Vector3> vertices, List<int>indices)
		{
			for (int d=0;d<6;d++) {
				for (int s = 0; s < 16; s++) {
					slices [d * 16 + s].FlushTriangles ((FaceDirection)d, s, vertices, indices);
				}
			}
		}

		public void FlushTriangles (List<Vector3> vertices, List<int>indices, List<Vector4>uv0, List<Vector3>normals, List<Color32>colors)
		{
			for (int d=0;d<6;d++) {
				for (int s = 0; s < 16; s++) {
					slicesFull [d * 16 + s].FlushTriangles ((FaceDirection)d, s, vertices, indices, uv0, normals, colors);
				}
			}
		}


		public void Clear() {
			if (useFullVertexData) {
				for (int k = 0; k < slicesFull.Length; k++) {
					slicesFull [k].Clear ();
				}
			} else {
				for (int k = 0; k < slices.Length; k++) {
					slices [k].Clear ();
				}
			}
		}

	}
}