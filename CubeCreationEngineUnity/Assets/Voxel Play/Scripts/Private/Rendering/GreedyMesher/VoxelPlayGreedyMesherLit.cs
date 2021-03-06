﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

    public class VoxelPlayGreedyMesherLit
    {
        VoxelPlayGreedySliceLit [] slicesFull;

        public VoxelPlayGreedyMesherLit ()
        {
            slicesFull = new VoxelPlayGreedySliceLit [16 * 6];
            for (int k = 0; k < slicesFull.Length; k++) {
                slicesFull [k] = new VoxelPlayGreedySliceLit ();
            }
        }


        public void AddQuad (FaceDirection direction, int x, int y, int slice, ref Color32 color, float light, int textureIndex)
        {
            int index = (int)direction * 16 + slice;
            slicesFull [index].AddQuad (x, y, ref color, light, textureIndex);
        }


        public void FlushTriangles (List<Vector3> vertices, List<int> indices, List<Vector4> uv0, List<Vector3> normals, List<Color32> colors)
        {
            for (int d = 0; d < 6; d++) {
                for (int s = 0; s < 16; s++) {
                    slicesFull [d * 16 + s].FlushTriangles ((FaceDirection)d, s, vertices, indices, uv0, normals, colors);
                }
            }
        }


        public void Clear ()
        {
            for (int k = 0; k < slicesFull.Length; k++) {
                slicesFull [k].Clear ();
            }
        }

    }
}