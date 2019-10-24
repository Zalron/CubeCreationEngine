using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay
{

    public class MeshingThreadGeom : MeshingThread
    {

        /// <summary>
        /// Uses geometry-shader-based materials and mesh to render the chunk
        /// </summary>
        public override void GenerateMeshData ()
        {
            int jobIndex = meshJobMeshDataGenerationIndex;

            for (int j = 0; j < meshJobs [jobIndex].buffers.Length; j++) {
                tempGeoIndices [j].Clear ();
                meshJobs [jobIndex].buffers [j].indicesCount = 0;
            }

            VoxelChunk chunk = meshJobs [jobIndex].chunk;
            List<Vector3> tempChunkVertices = meshJobs [jobIndex].vertices;
            List<Vector4> tempChunkUV0 = meshJobs [jobIndex].uv0;
            List<Vector4> tempChunkUV2 = meshJobs [jobIndex].uv2;
            List<Color32> tempChunkColors32 = meshJobs [jobIndex].colors;
            List<Vector3> meshColliderVertices = meshJobs [jobIndex].colliderVertices;
            List<int> meshColliderIndices = meshJobs [jobIndex].colliderIndices;
            List<Vector3> navMeshVertices = meshJobs [jobIndex].navMeshVertices;
            List<int> navMeshIndices = meshJobs [jobIndex].navMeshIndices;
            FastList<ModelInVoxel> mivs = meshJobs [jobIndex].mivs;


            tempChunkVertices.Clear ();
            tempChunkUV0.Clear ();
            tempChunkColors32.Clear ();
            if (enableColliders) {
                meshColliderIndices.Clear ();
                meshColliderVertices.Clear ();
                if (enableNavMesh) {
                    navMeshIndices.Clear ();
                    navMeshVertices.Clear ();
                }
            }
            mivs.Clear ();

            Voxel [] voxels = chunk.voxels;
            Vector4 uvAux, uv2Aux;
            tempChunkUV2.Clear ();

            int chunkUVIndex = -1;
            int chunkVoxelCount = 0;
            Color32 tintColor = Misc.color32White;
            Vector3 pos = Misc.vector3zero;
            ModelInVoxel miv = new ModelInVoxel ();

            const int V_ONE_Y_ROW = 18 * 18;
            const int V_ONE_Z_ROW = 18;

            int voxelIndex = 0;
            int voxelSignature = 1;
            for (int y = 0; y < 16; y++) {
                int vy = (y + 1) * 18 * 18;
                for (int z = 0; z < 16; z++) {
                    int vyz = vy + (z + 1) * 18;
                    for (int x = 0; x < 16; x++, voxelIndex++) {
                        voxels [voxelIndex].lightMesh = voxels [voxelIndex].light;
                        if (voxels [voxelIndex].hasContent != 1)
                            continue;

                        int vxyz = vyz + x + 1;

                        int vindex = vxyz - 1;
                        Voxel [] chunk_middle_middle_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_middle_left = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz + 1;
                        Voxel [] chunk_middle_middle_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_middle_right = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz + V_ONE_Y_ROW;
                        Voxel [] chunk_top_middle_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_middle_middle = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz - V_ONE_Y_ROW;
                        Voxel [] chunk_bottom_middle_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_middle_middle = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz + V_ONE_Z_ROW;
                        Voxel [] chunk_middle_forward_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_forward_middle = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz - V_ONE_Z_ROW;
                        Voxel [] chunk_middle_back_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_back_middle = virtualChunk [vindex].voxelIndex;

                        // If voxel is surrounded by material, don't render
                        int v1u = chunk_top_middle_middle [top_middle_middle].opaque;
                        int v1f = chunk_middle_forward_middle [middle_forward_middle].opaque;
                        int v1b = chunk_middle_back_middle [middle_back_middle].opaque;
                        int v1l = chunk_middle_middle_left [middle_middle_left].opaque;
                        int v1r = chunk_middle_middle_right [middle_middle_right].opaque;
                        int v1d = chunk_bottom_middle_middle [bottom_middle_middle].opaque;
                        if (v1u + v1f + v1b + v1l + v1r + v1d == 90) // 90 = 15 * 6
                            continue;

                        // top
                        vindex = vxyz + V_ONE_Y_ROW + V_ONE_Z_ROW - 1;
                        Voxel [] chunk_top_forward_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_forward_left = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_top_forward_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_forward_middle = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_top_forward_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_forward_right = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz + V_ONE_Y_ROW - 1;
                        Voxel [] chunk_top_middle_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_middle_left = virtualChunk [vindex].voxelIndex;

                        vindex += 2;
                        Voxel [] chunk_top_middle_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_middle_right = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz + V_ONE_Y_ROW - V_ONE_Z_ROW - 1;
                        Voxel [] chunk_top_back_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_back_left = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_top_back_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_back_middle = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_top_back_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_back_right = virtualChunk [vindex].voxelIndex;

                        // middle
                        vindex = vxyz + V_ONE_Z_ROW - 1;
                        Voxel [] chunk_middle_forward_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_forward_left = virtualChunk [vindex].voxelIndex;

                        vindex += 2;
                        Voxel [] chunk_middle_forward_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_forward_right = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz - V_ONE_Z_ROW - 1;
                        Voxel [] chunk_middle_back_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_back_left = virtualChunk [vindex].voxelIndex;

                        vindex += 2;
                        Voxel [] chunk_middle_back_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_back_right = virtualChunk [vindex].voxelIndex;

                        // bottom
                        vindex = vxyz - V_ONE_Y_ROW + V_ONE_Z_ROW - 1;
                        Voxel [] chunk_bottom_forward_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_forward_left = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_bottom_forward_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_forward_middle = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_bottom_forward_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_forward_right = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz - V_ONE_Y_ROW - 1;
                        Voxel [] chunk_bottom_middle_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_middle_left = virtualChunk [vindex].voxelIndex;

                        vindex += 2;
                        Voxel [] chunk_bottom_middle_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_middle_right = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz - V_ONE_Y_ROW - V_ONE_Z_ROW - 1;
                        Voxel [] chunk_bottom_back_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_back_left = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_bottom_back_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_back_middle = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_bottom_back_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_back_right = virtualChunk [vindex].voxelIndex;


                        pos.y = y - 7.5f;
                        pos.z = z - 7.5f;
                        pos.x = x - 7.5f;

                        voxelSignature += voxelIndex;
                        chunkVoxelCount++;

                        VoxelDefinition type = env.voxelDefinitions [voxels [voxelIndex].typeIndex];
                        FastFixedBuffer<int> indices = tempGeoIndices [type.materialBufferIndex];

                        uvAux.x = type.textureIndexSide;
                        uvAux.y = type.textureIndexTop;
                        uvAux.z = type.textureIndexBottom;
                        uvAux.w = 0;

                        int occlusionX = 0;
                        int occlusionY = 0;
                        int occlusionZ = 0;
                        int occlusionW = 0;
                        int occ = 0;

                        RenderType rt = type.renderType;
                        switch (rt) {
                        case RenderType.Water: {
                                ++chunkUVIndex;
                                indices.Add (chunkUVIndex);

                                uvAux.w = voxels [voxelIndex].light / 15f;

                                int hf = chunk_middle_forward_middle [middle_forward_middle].GetWaterLevel ();
                                int hb = chunk_middle_back_middle [middle_back_middle].GetWaterLevel ();
                                int hr = chunk_middle_middle_right [middle_middle_right].GetWaterLevel ();
                                int hl = chunk_middle_middle_left [middle_middle_left].GetWaterLevel ();

                                // compute water neighbours and account for foam
                                // back
                                occ = chunk_middle_back_middle [middle_back_middle].hasContent;
                                if (occ == 1) {
                                    // occlusionX bit = 0 means that face is visible
                                    occlusionX |= 1;
                                    if (hb == 0) {
                                        occlusionY |= 1;
                                    }
                                }

                                // front
                                occ = chunk_middle_forward_middle [middle_forward_middle].hasContent;
                                if (occ == 1) {
                                    occlusionX |= (1 << 1);
                                    if (hf == 0) {
                                        occlusionY |= 2;
                                    }
                                }

                                int wh = voxels [voxelIndex].GetWaterLevel ();
                                int th = chunk_top_middle_middle [top_middle_middle].GetWaterLevel ();

                                // top (hide only if water level is full or voxel on top is water)
                                if (wh == 15 || th > 0) {
                                    occlusionX += ((chunk_top_middle_middle [top_middle_middle].hasContent & 1) << 2);
                                }

                                // down
                                occlusionX += ((chunk_bottom_middle_middle [bottom_middle_middle].hasContent & 1) << 3);

                                // left
                                occ = chunk_middle_middle_left [middle_middle_left].hasContent;
                                if (occ == 1) {
                                    occlusionX |= (1 << 4);
                                    if (hl == 0) {
                                        occlusionY |= 4;
                                    }
                                }
                                // right
                                occ = chunk_middle_middle_right [middle_middle_right].hasContent;
                                if (occ == 1) {
                                    occlusionX += (1 << 5);
                                    if (hr == 0) {
                                        occlusionY |= 8;
                                    }
                                }

                                // If there's water on top, full size
                                if (th > 0) {
                                    occlusionW = 15 + (15 << 4) + (15 << 8) + (15 << 12); // full height
                                    occlusionY += (1 << 8) + (1 << 10); // neutral/no flow
                                } else {
                                    // Get corners heights
                                    int hfr = chunk_middle_forward_right [middle_forward_right].GetWaterLevel ();
                                    int hbr = chunk_middle_back_right [middle_back_right].GetWaterLevel ();
                                    int hbl = chunk_middle_back_left [middle_back_left].GetWaterLevel ();
                                    int hfl = chunk_middle_forward_left [middle_forward_left].GetWaterLevel ();

                                    // corner foam
                                    if (type.showFoam) {
                                        if (hbl == 0) {
                                            occlusionY |= chunk_middle_back_left [middle_back_left].hasContent << 4;
                                        }
                                        if (hfl == 0) {
                                            occlusionY |= chunk_middle_forward_left [middle_forward_left].hasContent << 5;
                                        }
                                        if (hfr == 0) {
                                            occlusionY |= chunk_middle_forward_right [middle_forward_right].hasContent << 6;
                                        }
                                        if (hbr == 0) {
                                            occlusionY |= chunk_middle_back_right [middle_back_right].hasContent << 7;
                                        }
                                    }

                                    int tf = chunk_top_forward_middle [top_forward_middle].GetWaterLevel ();
                                    int tfr = chunk_top_forward_right [top_forward_right].GetWaterLevel ();
                                    int tr = chunk_top_middle_right [top_middle_right].GetWaterLevel ();
                                    int tbr = chunk_top_back_right [top_back_right].GetWaterLevel ();
                                    int tb = chunk_top_back_middle [top_back_middle].GetWaterLevel ();
                                    int tbl = chunk_top_back_left [top_back_left].GetWaterLevel ();
                                    int tl = chunk_top_middle_left [top_middle_left].GetWaterLevel ();
                                    int tfl = chunk_top_forward_left [top_forward_left].GetWaterLevel ();

                                    // forward right corner
                                    if (tf * hf + tfr * hfr + tr * hr > 0) {
                                        hfr = 15;
                                    } else {
                                        hfr = wh > hfr ? wh : hfr;
                                        if (hf > hfr)
                                            hfr = hf;
                                        if (hr > hfr)
                                            hfr = hr;
                                    }
                                    // bottom right corner
                                    if (tr * hr + tbr * hbr + tb * hb > 0) {
                                        hbr = 15;
                                    } else {
                                        hbr = wh > hbr ? wh : hbr;
                                        if (hr > hbr)
                                            hbr = hr;
                                        if (hb > hbr)
                                            hbr = hb;
                                    }
                                    // bottom left corner
                                    if (tb * hb + tbl * hbl + tl * hl > 0) {
                                        hbl = 15;
                                    } else {
                                        hbl = wh > hbl ? wh : hbl;
                                        if (hb > hbl)
                                            hbl = hb;
                                        if (hl > hbl)
                                            hbl = hl;
                                    }
                                    // forward left corner
                                    if (tl * hl + tfl * hfl + tf * hf > 0) {
                                        hfl = 15;
                                    } else {
                                        hfl = wh > hfl ? wh : hfl;
                                        if (hl > hfl)
                                            hfl = hl;
                                        if (hf > hfl)
                                            hfl = hf;
                                    }
                                    occlusionW = hfr + (hbr << 4) + (hbl << 8) + (hfl << 12);

                                    // flow
                                    int fx = hfr + hbr - hfl - hbl;
                                    if (fx > 0)
                                        fx = 2;
                                    else if (fx < 0)
                                        fx = 0;
                                    else
                                        fx = 1;
                                    int fz = hfl + hfr - hbl - hbr;
                                    if (fz > 0)
                                        fz = 2;
                                    else if (fz < 0)
                                        fz = 0;
                                    else
                                        fz = 1;

                                    occlusionY += (fx << 8) + (fz << 10);
                                }

                                if (!type.showFoam) {
                                    occlusionY &= 0xFF00;
                                }
                                pos.y -= 0.5f;
                                tempChunkVertices.Add (pos);
                            }
                            break;
                        case RenderType.CutoutCross: {
                                ++chunkUVIndex;
                                indices.Add (chunkUVIndex);
                                uvAux.w = voxels [voxelIndex].light / 15f;
                                Vector3 aux = pos;
                                float random = WorldRand.GetValue (pos);
                                uvAux.w *= 1f + (random - 0.45f) * type.colorVariation; // * (1f + random * 0.3f;  // adds color variation
                                pos.x += random * 0.5f - 0.25f;
                                aux.x += 1f;
                                random = WorldRand.GetValue (aux);
                                pos.z += random * 0.5f - 0.25f;
                                pos.y -= random * 0.1f;
                                tempChunkVertices.Add (pos);
                            }
                            break;
                        case RenderType.OpaqueNoAO:
                            ++chunkUVIndex;
                            tempChunkVertices.Add (pos);

                            v1b = (v1b + 1) >> 4;   // substitutes a comparisson with FULL_OPAQUE (15)
                            v1f = (v1f + 1) >> 4;
                            v1u = (v1u + 1) >> 4;
                            v1d = (v1d + 1) >> 4;
                            v1l = (v1l + 1) >> 4;
                            v1r = (v1r + 1) >> 4;
                            uvAux.w = v1b + (v1f << 1) + (v1u << 2) + (v1d << 3) + (v1l << 4) + (v1r << 5);
                            indices.Add (chunkUVIndex);
                            break;
                        case RenderType.Transp6tex: {
                                ++chunkUVIndex;
                                indices.Add (chunkUVIndex);
                                int rotation = voxels [voxelIndex].GetTextureRotation ();
                                uvAux = type.textureSideIndices [rotation].xyzw;
                                uvAux.w = voxels [voxelIndex].light;

                                // Avoid overlapping faces of same glass material
                                int typeIndex = voxels [voxelIndex].typeIndex;

                                // back
                                if (v1b == FULL_OPAQUE || chunk_middle_back_middle [middle_back_middle].typeIndex == typeIndex) {
                                    // occlusionX bit = 0 means that face is visible
                                    occlusionX |= 1;
                                }

                                // front
                                if (v1f == FULL_OPAQUE || chunk_middle_forward_middle [middle_forward_middle].typeIndex == typeIndex) {
                                    occlusionX |= 2;
                                }

                                // up
                                if (v1u == FULL_OPAQUE || chunk_top_middle_middle [top_middle_middle].typeIndex == typeIndex) {
                                    occlusionX |= 4;
                                }

                                // down
                                if (v1d == FULL_OPAQUE || chunk_bottom_middle_middle [bottom_middle_middle].typeIndex == typeIndex) {
                                    occlusionX |= 8;
                                }

                                // left
                                if (v1l == FULL_OPAQUE || chunk_middle_middle_left [middle_middle_left].typeIndex == typeIndex) {
                                    occlusionX |= 16;
                                }
                                // right
                                if (v1r == FULL_OPAQUE || chunk_middle_middle_right [middle_middle_right].typeIndex == typeIndex) {
                                    occlusionX |= 32;
                                }
                                tempChunkVertices.Add (pos);

                                // Pass custom alpha
                                occlusionY = (int)(255 * type.alpha);

                                // Add collider data
                                if (enableColliders) {
                                    if (v1b == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Back, x, y, z);
                                    }
                                    if (v1f == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Forward, x, y, z);
                                    }
                                    if (v1u == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Top, x, z, y);
                                    }
                                    if (v1d == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Bottom, x, z, y);
                                    }
                                    if (v1l == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Left, z, y, x);
                                    }
                                    if (v1r == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Right, z, y, x);
                                    }
                                    if (enableNavMesh && type.navigatable) {
                                        if (v1b == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Back, x, y, z);
                                        }
                                        if (v1f == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Forward, x, y, z);
                                        }
                                        if (v1u == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Top, x, z, y);
                                        }
                                        if (v1l == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Left, z, y, x);
                                        }
                                        if (v1r == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Right, z, y, x);
                                        }
                                    }
                                }

                            }
                            break;
                        default: //case RenderType.Custom:
                            miv.voxelIndex = voxelIndex;
                            miv.vd = type;
                            mivs.Add (miv);
                            continue;
                        case RenderType.Empty: {
                                v1b = (v1b + 1) >> 4;   // substitutes a comparison with FULL_OPAQUE (15)
                                v1f = (v1f + 1) >> 4;
                                v1u = (v1u + 1) >> 4;
                                v1d = (v1d + 1) >> 4;
                                v1l = (v1l + 1) >> 4;
                                v1r = (v1r + 1) >> 4;

                                // Add collider data
                                if (enableColliders) {
                                    if (v1b == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Back, x, y, z);
                                    }
                                    if (v1f == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Forward, x, y, z);
                                    }
                                    if (v1u == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Top, x, z, y);
                                    }
                                    if (v1d == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Bottom, x, z, y);
                                    }
                                    if (v1l == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Left, z, y, x);
                                    }
                                    if (v1r == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Right, z, y, x);
                                    }
                                    if (enableNavMesh && type.navigatable) {
                                        if (v1b == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Back, x, y, z);
                                        }
                                        if (v1f == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Forward, x, y, z);
                                        }
                                        if (v1u == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Top, x, z, y);
                                        }
                                        if (v1l == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Left, z, y, x);
                                        }
                                        if (v1r == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Right, z, y, x);
                                        }
                                    }
                                }
                            }
                            continue; // loop
                        case RenderType.Opaque:
                        case RenderType.Opaque6tex:
                        case RenderType.Cutout: // Opaque & Cutout
                            ++chunkUVIndex;
                            indices.Add (chunkUVIndex);

                            tempChunkVertices.Add (pos);

                            if (rt != RenderType.Cutout) {
                                int rotation = voxels [voxelIndex].GetTextureRotation ();
                                uvAux = type.textureSideIndices [rotation].xyzw;
                            }

                            if (denseTrees && rt == RenderType.Cutout) {
                                uvAux.w = 0;
                            } else {
                                v1b = (v1b + 1) >> 4;   // substitutes a comparisson with FULL_OPAQUE (15)
                                v1f = (v1f + 1) >> 4;
                                v1u = (v1u + 1) >> 4;
                                v1d = (v1d + 1) >> 4;
                                v1l = (v1l + 1) >> 4;
                                v1r = (v1r + 1) >> 4;
                                uvAux.w = v1b + (v1f << 1) + (v1u << 2) + (v1d << 3) + (v1l << 4) + (v1r << 5);

                                // Add collider data
                                if (enableColliders && rt != RenderType.Cutout) {
                                    if (v1b == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Back, x, y, z);
                                    }
                                    if (v1f == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Forward, x, y, z);
                                    }
                                    if (v1u == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Top, x, z, y);
                                    }
                                    if (v1d == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Bottom, x, z, y);
                                    }
                                    if (v1l == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Left, z, y, x);
                                    }
                                    if (v1r == 0) {
                                        greedyCollider.AddQuad (FaceDirection.Right, z, y, x);
                                    }
                                    if (enableNavMesh && type.navigatable) {
                                        if (v1b == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Back, x, y, z);
                                        }
                                        if (v1f == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Forward, x, y, z);
                                        }
                                        if (v1u == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Top, x, z, y);
                                        }
                                        if (v1l == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Left, z, y, x);
                                        }
                                        if (v1r == 0) {
                                            greedyNavMesh.AddQuad (FaceDirection.Right, z, y, x);
                                        }
                                    }
                                }
                            }
                            if (rt == RenderType.Cutout) {
                                // Add color variation
                                float random = WorldRand.GetValue (pos);
                                int r = (int)(255f * (1f + (random - 0.45f) * type.colorVariation));
                                uvAux.w += (r << 6);
                                if (type.windAnimation) {
                                    uvAux.w += 65536; //1 << 16;
                                }
                            }

                            int lu = chunk_top_middle_middle [top_middle_middle].light;
                            int ll = chunk_middle_middle_left [middle_middle_left].light;
                            int lf = chunk_middle_forward_middle [middle_forward_middle].light;
                            int lr = chunk_middle_middle_right [middle_middle_right].light;
                            int lb = chunk_middle_back_middle [middle_back_middle].light;
                            int ld = chunk_bottom_middle_middle [bottom_middle_middle].light;

#if UNITY_EDITOR
                            if (env.enableSmoothLighting && !env.draftModeActive) {
#else
							if (env.enableSmoothLighting) {
#endif
                                int v2r = chunk_top_middle_right [top_middle_right].light;
                                int v2br = chunk_top_back_right [top_back_right].light;
                                int v2b = chunk_top_back_middle [top_back_middle].light;
                                int v2bl = chunk_top_back_left [top_back_left].light;
                                int v2l = chunk_top_middle_left [top_middle_left].light;
                                int v2fl = chunk_top_forward_left [top_forward_left].light;
                                int v2f = chunk_top_forward_middle [top_forward_middle].light;
                                int v2fr = chunk_top_forward_right [top_forward_right].light;

                                int v1fr = chunk_middle_forward_right [middle_forward_right].light;
                                int v1br = chunk_middle_back_right [middle_back_right].light;
                                int v1bl = chunk_middle_back_left [middle_back_left].light;
                                int v1fl = chunk_middle_forward_left [middle_forward_left].light;

                                int v0r = chunk_bottom_middle_right [bottom_middle_right].light;
                                int v0br = chunk_bottom_back_right [bottom_back_right].light;
                                int v0b = chunk_bottom_back_middle [bottom_back_middle].light;
                                int v0bl = chunk_bottom_back_left [bottom_back_left].light;
                                int v0l = chunk_bottom_middle_left [bottom_middle_left].light;
                                int v0fl = chunk_bottom_forward_left [bottom_forward_left].light;
                                int v0f = chunk_bottom_forward_middle [bottom_forward_middle].light;
                                int v0fr = chunk_bottom_forward_right [bottom_forward_right].light;

                                // Backwards face
                                // Vertex 0
                                occ = ((lb + v0b + v0bl + v1bl) >> 2);
                                occlusionX += occ;
                                // Vertex 1
                                occ = ((lb + v0b + v0br + v1br) >> 2);
                                occlusionX += occ << 4;
                                // Vertex 2
                                occ = ((lb + v1bl + v2bl + v2b) >> 2);
                                occlusionX += occ << 8;
                                // Vertex 3
                                occ = ((lb + v2b + v2br + v1br) >> 2);
                                occlusionX += occ << 12;

                                // Forward face
                                // Vertex 5
                                occ = ((lf + v0f + v0fr + v1fr) >> 2);
                                occlusionX += occ << 16;
                                // Vertex 4
                                occ = ((lf + v0f + v0fl + v1fl) >> 2);
                                occlusionX += occ << 20;
                                // Vertex 6
                                occ = ((lf + v1fr + v2fr + v2f) >> 2);
                                occlusionY += occ;
                                // Vertex 7
                                occ = ((lf + v2f + v2fl + v1fl) >> 2);
                                occlusionY += occ << 4;

                                // Top face
                                // Vertex 2
                                occ = ((lu + v2b + v2bl + v2l) >> 2);
                                occlusionY += occ << 8;
                                // Vertex 3
                                occ = ((lu + v2b + v2br + v2r) >> 2);
                                occlusionY += occ << 12;
                                // Vertex 7
                                occ = ((lu + v2l + v2fl + v2f)) >> 2;
                                occlusionY += occ << 16;
                                // Vertex 6
                                occ = ((lu + v2r + v2fr + v2f) >> 2);
                                occlusionY += occ << 20;

                                // Left face
                                // Vertex 0
                                occ = ((ll + v1bl + v0l + v0bl) >> 2);
                                occlusionZ += occ;
                                // Vertex 4
                                occ = ((ll + v1fl + v0l + v0fl) >> 2);
                                occlusionZ += occ << 4;
                                // Vertex 2
                                occ = ((ll + v2l + v2bl + v1bl) >> 2);
                                occlusionZ += occ << 8;
                                // Vertex 7
                                occ = ((ll + v2l + v2fl + v1fl) >> 2);
                                occlusionZ += occ << 12;

                                // Right face
                                // Vertex 1
                                occ = ((lr + v1br + v0r + v0br) >> 2);
                                occlusionZ += occ << 16;
                                // Vertex 5
                                occ = ((lr + v1fr + v0r + v0fr) >> 2);
                                occlusionZ += occ << 20;
                                // Vertex 3
                                occ = ((lr + v2r + v2br + v1br) >> 2);
                                occlusionW += occ;
                                // Vertex 6
                                occ = ((lr + v2r + v2fr + v1fr) >> 2);
                                occlusionW += occ << 4;

                                // Bottom face
                                // Vertex 0
                                occ = ((ld + v0b + v0l + v0bl) >> 2);
                                occlusionW += occ << 8;
                                // Vertex 1
                                occ = ((ld + v0b + v0r + v0br) >> 2);
                                occlusionW += occ << 12;
                                // Vertex 4
                                occ = ((ld + v0f + v0l + v0fl) >> 2);
                                occlusionW += occ << 16;
                                // Vertex 5
                                occ = ((ld + v0f + v0r + v0fr) >> 2);
                                occlusionW += occ << 20;

                            } else {
                                occlusionX = lb; // back
                                occlusionX += lf << 4; // forward
                                occlusionX += lu << 8; // top
                                occlusionX += ll << 12; // // left
                                occlusionX += lr << 16; // right
                                occlusionX += ld << 20; // bottom
                            }
                            break;
                        }
                        tempChunkUV0.Add (uvAux);
                        uv2Aux.x = occlusionX;
                        uv2Aux.y = occlusionY;
                        uv2Aux.z = occlusionZ;
                        uv2Aux.w = occlusionW;
                        tempChunkUV2.Add (uv2Aux);

                        if (enableTinting) {
                            tintColor.r = voxels [voxelIndex].red;
                            tintColor.g = voxels [voxelIndex].green;
                            tintColor.b = voxels [voxelIndex].blue;
                            tempChunkColors32.Add (tintColor);
                        }
                    }
                }
            }

            meshJobs [jobIndex].chunk = chunk;

            meshJobs [jobIndex].totalVoxels = chunkVoxelCount;

            if (chunkVoxelCount == 0) {
                return;
            }

            meshJobs [jobIndex].needsColliderRebuild = (voxelSignature != chunk.voxelSignature);
            if (enableColliders) {
                if (meshJobs [jobIndex].needsColliderRebuild) {
                    greedyCollider.FlushTriangles (meshColliderVertices, meshColliderIndices);
                    if (enableNavMesh) {
                        greedyNavMesh.FlushTriangles (navMeshVertices, navMeshIndices);
                    }
                } else {
                    greedyCollider.Clear ();
                    greedyNavMesh.Clear ();
                }
            }
            chunk.voxelSignature = voxelSignature;

            // job index 0 lists are used as generic buffers but we need to convert them to array to use SetIndices API :(
            int subMeshCount = 0;
            for (int k = 0; k < VoxelPlayEnvironment.MAX_MATERIALS_PER_CHUNK; k++) {
                if (tempGeoIndices [k].count > 0) {
                    subMeshCount++;
                    meshJobs [jobIndex].buffers [k].indicesArray = tempGeoIndices [k].ToArray ();
                    meshJobs [jobIndex].buffers [k].indicesCount = meshJobs [jobIndex].buffers [k].indicesArray.Length;
                }
            }
            meshJobs [jobIndex].subMeshCount = subMeshCount;

            meshJobs [jobIndex].mivs = mivs;

        }


    }



}
