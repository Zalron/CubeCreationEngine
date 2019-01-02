using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay
{

	public partial class VoxelPlayEnvironment : MonoBehaviour
	{

		/// <summary>
		/// Uses geometry-shader-based materials and mesh to render the chunk
		/// </summary>
		void GenerateMeshData_Geo (int jobIndex)
		{

			VoxelChunk chunk = meshJobs [jobIndex].chunk;
			tempChunkVertices = meshJobs [jobIndex].vertices;
			tempChunkUV0 = meshJobs [jobIndex].uv0;
			tempChunkUV2 = meshJobs [jobIndex].uv2;
			tempChunkColors32 = meshJobs [jobIndex].colors;
			meshColliderVertices = meshJobs [jobIndex].colliderVertices;
			meshColliderIndices = meshJobs [jobIndex].colliderIndices;
			navMeshVertices = meshJobs [jobIndex].navMeshVertices;
			navMeshIndices = meshJobs [jobIndex].navMeshIndices;
			mivs = meshJobs [jobIndex].mivs;

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
			opaqueIndices.Clear ();
			cutoutIndices.Clear ();
			waterIndices.Clear ();
			cutxssIndices.Clear ();
			opNoAOIndices.Clear ();	
			transpIndices.Clear ();
			mivs.Clear ();

			int opaqueVoxelsCount = 0;
			int cutoutVoxelsCount = 0;
			int waterVoxelsCount = 0;
			int cutxssVoxelsCount = 0;
			int opNoAOVoxelsCount = 0;
			int transpVoxelsCount = 0;
			Voxel[] voxels = chunk.voxels;
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
			int voxelSignature = 0;
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
						Voxel[] chunk_middle_middle_left = chunk9 [virtualChunk [vindex].chunk9Index];
						int middle_middle_left = virtualChunk [vindex].voxelIndex;

						vindex = vxyz + 1;
						Voxel[] chunk_middle_middle_right = chunk9 [virtualChunk [vindex].chunk9Index];
						int middle_middle_right = virtualChunk [vindex].voxelIndex;

						vindex = vxyz + V_ONE_Y_ROW;
						Voxel[] chunk_top_middle_middle = chunk9 [virtualChunk [vindex].chunk9Index];
						int top_middle_middle = virtualChunk [vindex].voxelIndex;

						vindex = vxyz - V_ONE_Y_ROW;
						Voxel[] chunk_bottom_middle_middle = chunk9 [virtualChunk [vindex].chunk9Index];
						int bottom_middle_middle = virtualChunk [vindex].voxelIndex;

						vindex = vxyz + V_ONE_Z_ROW;
						Voxel[] chunk_middle_forward_middle = chunk9 [virtualChunk [vindex].chunk9Index];
						int middle_forward_middle = virtualChunk [vindex].voxelIndex;

						vindex = vxyz - V_ONE_Z_ROW;
						Voxel[] chunk_middle_back_middle = chunk9 [virtualChunk [vindex].chunk9Index];
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
						Voxel[] chunk_top_forward_left = chunk9 [virtualChunk [vindex].chunk9Index];
						int top_forward_left = virtualChunk [vindex].voxelIndex;

						vindex++;
						Voxel[] chunk_top_forward_middle = chunk9 [virtualChunk [vindex].chunk9Index];
						int top_forward_middle = virtualChunk [vindex].voxelIndex;

						vindex++;
						Voxel[] chunk_top_forward_right = chunk9 [virtualChunk [vindex].chunk9Index];
						int top_forward_right = virtualChunk [vindex].voxelIndex;

						vindex = vxyz + V_ONE_Y_ROW - 1;
						Voxel[] chunk_top_middle_left = chunk9 [virtualChunk [vindex].chunk9Index];
						int top_middle_left = virtualChunk [vindex].voxelIndex;

						vindex += 2;
						Voxel[] chunk_top_middle_right = chunk9 [virtualChunk [vindex].chunk9Index];
						int top_middle_right = virtualChunk [vindex].voxelIndex;

						vindex = vxyz + V_ONE_Y_ROW - V_ONE_Z_ROW - 1;
						Voxel[] chunk_top_back_left = chunk9 [virtualChunk [vindex].chunk9Index];
						int top_back_left = virtualChunk [vindex].voxelIndex;

						vindex++;
						Voxel[] chunk_top_back_middle = chunk9 [virtualChunk [vindex].chunk9Index];
						int top_back_middle = virtualChunk [vindex].voxelIndex;

						vindex++;
						Voxel[] chunk_top_back_right = chunk9 [virtualChunk [vindex].chunk9Index];
						int top_back_right = virtualChunk [vindex].voxelIndex;

						// middle
						vindex = vxyz + V_ONE_Z_ROW - 1;
						Voxel[] chunk_middle_forward_left = chunk9 [virtualChunk [vindex].chunk9Index];
						int middle_forward_left = virtualChunk [vindex].voxelIndex;

						vindex += 2;
						Voxel[] chunk_middle_forward_right = chunk9 [virtualChunk [vindex].chunk9Index];
						int middle_forward_right = virtualChunk [vindex].voxelIndex;

						vindex = vxyz - V_ONE_Z_ROW - 1;
						Voxel[] chunk_middle_back_left = chunk9 [virtualChunk [vindex].chunk9Index];
						int middle_back_left = virtualChunk [vindex].voxelIndex;

						vindex += 2;
						Voxel[] chunk_middle_back_right = chunk9 [virtualChunk [vindex].chunk9Index];
						int middle_back_right = virtualChunk [vindex].voxelIndex;

						// bottom
						vindex = vxyz - V_ONE_Y_ROW + V_ONE_Z_ROW - 1;
						Voxel[] chunk_bottom_forward_left = chunk9 [virtualChunk [vindex].chunk9Index];
						int bottom_forward_left = virtualChunk [vindex].voxelIndex;

						vindex++;
						Voxel[] chunk_bottom_forward_middle = chunk9 [virtualChunk [vindex].chunk9Index];
						int bottom_forward_middle = virtualChunk [vindex].voxelIndex;

						vindex++;
						Voxel[] chunk_bottom_forward_right = chunk9 [virtualChunk [vindex].chunk9Index];
						int bottom_forward_right = virtualChunk [vindex].voxelIndex;

						vindex = vxyz - V_ONE_Y_ROW - 1;
						Voxel[] chunk_bottom_middle_left = chunk9 [virtualChunk [vindex].chunk9Index];
						int bottom_middle_left = virtualChunk [vindex].voxelIndex;

						vindex += 2;
						Voxel[] chunk_bottom_middle_right = chunk9 [virtualChunk [vindex].chunk9Index];
						int bottom_middle_right = virtualChunk [vindex].voxelIndex;

						vindex = vxyz - V_ONE_Y_ROW - V_ONE_Z_ROW - 1;
						Voxel[] chunk_bottom_back_left = chunk9 [virtualChunk [vindex].chunk9Index];
						int bottom_back_left = virtualChunk [vindex].voxelIndex;

						vindex++;
						Voxel[] chunk_bottom_back_middle = chunk9 [virtualChunk [vindex].chunk9Index];
						int bottom_back_middle = virtualChunk [vindex].voxelIndex;

						vindex++;
						Voxel[] chunk_bottom_back_right = chunk9 [virtualChunk [vindex].chunk9Index];
						int bottom_back_right = virtualChunk [vindex].voxelIndex;

		
						pos.y = y - 7.5f;
						pos.z = z - 7.5f;
						pos.x = x - 7.5f;

						voxelSignature += voxelIndex;
						chunkVoxelCount++;

						VoxelDefinition type = voxelDefinitions [voxels [voxelIndex].typeIndex];
		
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
						case RenderType.Water:
							{
								++chunkUVIndex;
								waterVoxelsCount++;

								waterIndices.Add (chunkUVIndex);
		
								uvAux.w = voxels [voxelIndex].light / 15f;
		
								int hf = chunk_middle_forward_middle [middle_forward_middle].GetWaterLevel ();
								int hb = chunk_middle_back_middle [middle_back_middle].GetWaterLevel ();
								int hr = chunk_middle_middle_right [middle_middle_right].GetWaterLevel ();
								int hl = chunk_middle_middle_left [middle_middle_left].GetWaterLevel ();

								// compute water neighbours and account for foam
								// back
								occ = chunk_middle_back_middle [middle_back_middle].hasContent;
								if (occ != 0) {
									// occlusionX bit = 0 means that face is visible
									occlusionX |= 1;
									if (hb == 0) {
										occlusionY += 1;
									}
								}
											
								// front
								occ = chunk_middle_forward_middle [middle_forward_middle].hasContent;
								if (occ != 0) {
									occlusionX |= (1 << 1);
									if (hf == 0) {
										occlusionY += 2;
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
								if (occ != 0) {
									occlusionX |= (1 << 4);
									if (hl == 0) {
										occlusionY += 4;
									}
								}
								// right
								occ = chunk_middle_middle_right [middle_middle_right].hasContent;
								if (occ != 0) {
									occlusionX += (1 << 5);
									if (hr == 0) {
										occlusionY += 8;
									}
								}

								// if there's something on top, no foam
//								if (chunk_top_middle_middle [top_middle_middle].hasContent == 1 && wh < 15) {
//									if (chunk_top_back_middle [top_back_middle].hasContent != 1 || chunk_top_forward_middle [top_forward_middle].hasContent != 1 ||
//									    chunk_top_middle_left [top_middle_left].hasContent != 1 || chunk_top_middle_right [top_middle_right].hasContent != 1) {
//										occlusionY = 0;
//									}
//								}

								// Get corners heights
								// If there's water on top, full size
								if (th > 0) {
									occlusionW = 15 + (15 << 4) + (15 << 8) + (15 << 12); // full height
									occlusionY += (1 << 4) + (1 << 6);	// neutral/no flow
								} else {
									int hfr = chunk_middle_forward_right [middle_forward_right].GetWaterLevel ();
									int hbr = chunk_middle_back_right [middle_back_right].GetWaterLevel ();
									int hbl = chunk_middle_back_left [middle_back_left].GetWaterLevel ();
									int hfl = chunk_middle_forward_left [middle_forward_left].GetWaterLevel ();

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

									occlusionY += (fx << 4) + (fz << 6);
								}
								pos.y -= 0.5f;
								tempChunkVertices.Add (pos);
							}
							break;
						case RenderType.CutoutCross:
							{
								++chunkUVIndex;
								cutxssVoxelsCount++;
								cutxssIndices.Add (chunkUVIndex);
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
							opNoAOVoxelsCount++;
							tempChunkVertices.Add (pos);

							v1b = (v1b + 1) >> 4;	// substitutes a comparisson with FULL_OPAQUE (15)
							v1f = (v1f + 1) >> 4;
							v1u = (v1u + 1) >> 4;
							v1d = (v1d + 1) >> 4;
							v1l = (v1l + 1) >> 4;
							v1r = (v1r + 1) >> 4;
							uvAux.w = v1b + (v1f << 1) + (v1u << 2) + (v1d << 3) + (v1l << 4) + (v1r << 5);
							opNoAOIndices.Add (chunkUVIndex);
							break;
						case RenderType.Transp6tex:
							{
								++chunkUVIndex;
								transpVoxelsCount++;
								transpIndices.Add (chunkUVIndex);
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

								// Add collider data
								if (enableColliders) {
									if (v1b == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Back, x, y, z);
									}
									if (v1f == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Forward, x, y, z);
									}
									if (v1u == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Top, x, z, y);
									}
									if (v1d == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Bottom, x, z, y);
									}
									if (v1l == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Left, z, y, x);
									}
									if (v1r == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Right, z, y, x);
									}
									if (enableNavMesh && type.navigatable) {
										if (v1b == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Back, x, y, z);
										}
										if (v1f == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Forward, x, y, z);
										}
										if (v1u == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Top, x, z, y);
										}
										if (v1l == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Left, z, y, x);
										}
										if (v1r == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Right, z, y, x);
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
						case RenderType.Empty:
							{
								v1b = (v1b + 1) >> 4;	// substitutes a comparison with FULL_OPAQUE (15)
								v1f = (v1f + 1) >> 4;
								v1u = (v1u + 1) >> 4;
								v1d = (v1d + 1) >> 4;
								v1l = (v1l + 1) >> 4;
								v1r = (v1r + 1) >> 4;

								// Add collider data
								if (enableColliders) {
									if (v1b == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Back, x, y, z);
									}
									if (v1f == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Forward, x, y, z);
									}
									if (v1u == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Top, x, z, y);
									}
									if (v1d == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Bottom, x, z, y);
									}
									if (v1l == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Left, z, y, x);
									}
									if (v1r == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Right, z, y, x);
									}
									if (enableNavMesh && type.navigatable) {
										if (v1b == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Back, x, y, z);
										}
										if (v1f == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Forward, x, y, z);
										}
										if (v1u == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Top, x, z, y);
										}
										if (v1l == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Left, z, y, x);
										}
										if (v1r == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Right, z, y, x);
										}
									}
								}
							}
							continue; // loop
						case RenderType.Opaque:
						case RenderType.Opaque6tex:
						case RenderType.Cutout: // Opaque & Cutout
							++chunkUVIndex;
							tempChunkVertices.Add (pos);

							if (rt == RenderType.Cutout) {
								cutoutVoxelsCount++;
								cutoutIndices.Add (chunkUVIndex);
							} else {
								opaqueVoxelsCount++;
								opaqueIndices.Add (chunkUVIndex);
								int rotation = voxels [voxelIndex].GetTextureRotation ();
								uvAux = type.textureSideIndices [rotation].xyzw;
							}

							if (denseTrees && rt == RenderType.Cutout) {
								uvAux.w = 0;
							} else {
								v1b = (v1b + 1) >> 4;	// substitutes a comparisson with FULL_OPAQUE (15)
								v1f = (v1f + 1) >> 4;
								v1u = (v1u + 1) >> 4;
								v1d = (v1d + 1) >> 4;
								v1l = (v1l + 1) >> 4;
								v1r = (v1r + 1) >> 4;
								uvAux.w = v1b + (v1f << 1) + (v1u << 2) + (v1d << 3) + (v1l << 4) + (v1r << 5);
		
								// Add collider data
								if (enableColliders && rt != RenderType.Cutout) {
									if (v1b == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Back, x, y, z);
									}
									if (v1f == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Forward, x, y, z);
									}
									if (v1u == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Top, x, z, y);
									}
									if (v1d == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Bottom, x, z, y);
									}
									if (v1l == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Left, z, y, x);
									}
									if (v1r == 0) {
										greedyCollider.AddQuad (FACE_DIRECTION.Right, z, y, x);
									}
									if (enableNavMesh && type.navigatable) {
										if (v1b == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Back, x, y, z);
										}
										if (v1f == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Forward, x, y, z);
										}
										if (v1u == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Top, x, z, y);
										}
										if (v1l == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Left, z, y, x);
										}
										if (v1r == 0) {
											greedyNavMesh.AddQuad (FACE_DIRECTION.Right, z, y, x);
										}
									}
								}
							}
							if (rt == RenderType.Cutout) {
								// Add color variation
								float random = WorldRand.GetValue (pos);
								int r = (int)(255f * (1f + (random - 0.45f) * type.colorVariation));
//								int r = (int)((0.8f + random * 0.4f) * 255);  // adds color variation
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
							if (enableSmoothLighting && !draftModeActive) {
								#else
							if (enableSmoothLighting) {
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
								occlusionX = lb; //chunk_middle_back_middle [middle_back_middle].light;  // back
								occlusionX += lf << 4; //chunk_middle_forward_middle [middle_forward_middle].light << 4;  // forward
								occlusionX += lu << 8; // chunk_top_middle_middle [top_middle_middle].light << 8;  // top
								occlusionX += ll << 12; // chunk_middle_middle_left [middle_middle_left].light << 12;  // left
								occlusionX += lr << 16; // chunk_middle_middle_right [middle_middle_right].light << 16;  // right
								occlusionX += ld << 20; // chunk_bottom_middle_middle [bottom_middle_middle].light << 20;  // bottom
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

			// chunkVoxelCount includes MIVs
			int nonMivsCount = chunkUVIndex + 1;
			meshJobs [jobIndex].totalVisibleVoxels = nonMivsCount;

			if (chunkVoxelCount == 0) {
				return;
			}

			voxelSignature++;
			if (voxelSignature != chunk.voxelSignature) {
				chunk.needsColliderRebuild = true;
			}

			chunk.voxelSignature = voxelSignature;

			if (enableColliders) {
				if (chunk.needsColliderRebuild) {
					greedyCollider.FlushTriangles (meshColliderVertices, meshColliderIndices);
					if (enableNavMesh) {
						greedyNavMesh.FlushTriangles (navMeshVertices, navMeshIndices);
					}
				} else {
					greedyCollider.Clear ();
					greedyNavMesh.Clear ();
				}
			}

			meshJobs [jobIndex].vertices = tempChunkVertices;
			meshJobs [jobIndex].uv0 = tempChunkUV0;
			meshJobs [jobIndex].uv2 = tempChunkUV2;
			meshJobs [jobIndex].colors = tempChunkColors32;

			int matVoxelIndex = 0;
			int subMeshCount = 0;
			meshJobs [jobIndex].opaqueVoxelsCount = opaqueVoxelsCount;
			if (opaqueVoxelsCount > 0) {
				meshJobs [jobIndex].opaqueIndicesArray = opaqueIndices.ToArray ();
				subMeshCount++;
				matVoxelIndex += 1;
			}
			meshJobs [jobIndex].cutoutVoxelsCount = cutoutVoxelsCount;
			if (cutoutVoxelsCount > 0) {
				meshJobs [jobIndex].cutoutIndicesArray = cutoutIndices.ToArray ();
				subMeshCount++;
				matVoxelIndex += 2;
			}
			meshJobs [jobIndex].waterVoxelsCount = waterVoxelsCount;
			if (waterVoxelsCount > 0) {
				meshJobs [jobIndex].waterIndicesArray = waterIndices.ToArray ();
				subMeshCount++;
				matVoxelIndex += 4;
			}
			meshJobs [jobIndex].cutxssVoxelsCount = cutxssVoxelsCount;
			if (cutxssVoxelsCount > 0) {
				meshJobs [jobIndex].cutxssIndicesArray = cutxssIndices.ToArray ();
				subMeshCount++;
				matVoxelIndex += 8;
			}
			meshJobs [jobIndex].opNoAOVoxelsCount = opNoAOVoxelsCount;
			if (opNoAOVoxelsCount > 0) {
				meshJobs [jobIndex].opNoAOIndicesArray = opNoAOIndices.ToArray ();
				subMeshCount++;
				matVoxelIndex += 16;
			}
			meshJobs [jobIndex].transpVoxelsCount = transpVoxelsCount;
			if (transpVoxelsCount > 0) {
				meshJobs [jobIndex].transpIndicesArray = transpIndices.ToArray ();
				subMeshCount++;
				matVoxelIndex += 32;
			}
			meshJobs [jobIndex].matVoxelIndex = matVoxelIndex;
			meshJobs [jobIndex].subMeshCount = subMeshCount;

			meshJobs [jobIndex].colliderVertices = meshColliderVertices;
			meshJobs [jobIndex].colliderIndices = meshColliderIndices;

			meshJobs [jobIndex].navMeshVertices = navMeshVertices;
			meshJobs [jobIndex].navMeshIndices = navMeshIndices;

			meshJobs [jobIndex].mivs = mivs;

		}


	}



}
