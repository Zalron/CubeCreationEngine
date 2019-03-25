using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		// Chunk Creation helpers for non-geometry shaders
		static Vector3[] faceVerticesForward = new Vector3[] {
			new Vector3 (0.5f, -0.5f, 0.5f),
			new Vector3 (0.5f, 0.5f, 0.5f),
			new Vector3 (-0.5f, -0.5f, 0.5f),
			new Vector3 (-0.5f, 0.5f, 0.5f)
		};
		static Vector3[] faceVerticesBack = new Vector3[] {
			new Vector3 (-0.5f, -0.5f, -0.5f),
			new Vector3 (-0.5f, 0.5f, -0.5f),
			new Vector3 (0.5f, -0.5f, -0.5f),
			new Vector3 (0.5f, 0.5f, -0.5f)
		};
		static Vector3[] faceVerticesLeft = new Vector3[] {
			new Vector3 (-0.5f, -0.5f, 0.5f),
			new Vector3 (-0.5f, 0.5f, 0.5f),
			new Vector3 (-0.5f, -0.5f, -0.5f),
			new Vector3 (-0.5f, 0.5f, -0.5f)
		};
		static Vector3[] faceVerticesRight = new Vector3[] {
			new Vector3 (0.5f, -0.5f, -0.5f),
			new Vector3 (0.5f, 0.5f, -0.5f),
			new Vector3 (0.5f, -0.5f, 0.5f),
			new Vector3 (0.5f, 0.5f, 0.5f)
		};
		//		static Vector3[] faceVerticesTop = new Vector3[] {
		//			new Vector3 (-0.5f, 0.5f, 0.5f),
		//			new Vector3 (0.5f, 0.5f, 0.5f),
		//			new Vector3 (-0.5f, 0.5f, -0.5f),
		//			new Vector3 (0.5f, 0.5f, -0.5f)
		//		};
		static Vector3[] faceVerticesTop = new Vector3[] {
			new Vector3 (-0.5f, 0.5f, -0.5f),
			new Vector3 (-0.5f, 0.5f, 0.5f),
			new Vector3 (0.5f, 0.5f, -0.5f),
			new Vector3 (0.5f, 0.5f, 0.5f),
		};
		static Vector3[] faceVerticesTopFlipped = new Vector3[] { // water surface from below
			new Vector3 (-0.5f, 0.5f, -0.5f),
			new Vector3 (0.5f, 0.5f, -0.5f),
			new Vector3 (-0.5f, 0.5f, 0.5f),
			new Vector3 (0.5f, 0.5f, 0.5f),
		};
		//		static Vector3[] faceVerticesBottom = new Vector3[] {
		//			new Vector3 (-0.5f, -0.5f, -0.5f),
		//			new Vector3 (0.5f, -0.5f, -0.5f),
		//			new Vector3 (-0.5f, -0.5f, 0.5f),
		//			new Vector3 (0.5f, -0.5f, 0.5f)
		//		};
		static Vector3[] faceVerticesBottom = new Vector3[] {
			new Vector3 (0.5f, -0.5f, -0.5f),
			new Vector3 (0.5f, -0.5f, 0.5f),
			new Vector3 (-0.5f, -0.5f, -0.5f),
			new Vector3 (-0.5f, -0.5f, 0.5f)
		};
		static Vector3[] faceVerticesCross1 = new Vector3[] {
			new Vector3 (-0.5f, -0.5f, 0.5f),
			new Vector3 (-0.5f, 0.5f, 0.5f),
			new Vector3 (0.5f, -0.5f, -0.5f),
			new Vector3 (0.5f, 0.5f, -0.5f)
		};
		static Vector3[] faceVerticesCross2 = new Vector3[] {
			new Vector3 (-0.5f, -0.5f, -0.5f),
			new Vector3 (-0.5f, 0.5f, -0.5f),
			new Vector3 (0.5f, -0.5f, 0.5f),
			new Vector3 (0.5f, 0.5f, 0.5f)
		};
		static Vector3[] normalsBack = new Vector3[] {
			Misc.vector3back, Misc.vector3back, Misc.vector3back, Misc.vector3back
		};
		static Vector3[] normalsForward = new Vector3[] {
			Misc.vector3forward, Misc.vector3forward, Misc.vector3forward, Misc.vector3forward
		};
		static Vector3[] normalsLeft = new Vector3[] {
			Misc.vector3left, Misc.vector3left, Misc.vector3left, Misc.vector3left
		};
		static Vector3[] normalsRight = new Vector3[] {
			Misc.vector3right, Misc.vector3right, Misc.vector3right, Misc.vector3right
		};
		static Vector3[] normalsUp = new Vector3[] {
			Misc.vector3up, Misc.vector3up, Misc.vector3up, Misc.vector3up
		};
		static Vector3[] normalsDown = new Vector3[] {
			Misc.vector3down, Misc.vector3down, Misc.vector3down, Misc.vector3down
		};

		VoxelPlayGreedyMesher greedyNoAO, greedyCutout;


		/// <summary>
		/// Generates chunk mesh. Also computes lightmap if needed.
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		/// <param name="forceClearLightmap">If set to <c>true</c> force clear lightmap.</param>
		void GenerateMeshData_Triangle (int jobIndex) {

			VoxelChunk chunk = meshJobs [jobIndex].chunk;
			tempChunkVertices = meshJobs [jobIndex].vertices;
			tempChunkUV0 = meshJobs [jobIndex].uv0;
			tempChunkColors32 = meshJobs [jobIndex].colors;
			tempChunkNormals = meshJobs [jobIndex].normals;
			meshColliderVertices = meshJobs [jobIndex].colliderVertices;
			meshColliderIndices = meshJobs [jobIndex].colliderIndices;
			opaqueIndices = meshJobs [jobIndex].opaqueIndices;
			waterIndices = meshJobs [jobIndex].waterIndices;
			transpIndices = meshJobs [jobIndex].transpIndices;
			cutoutIndices = meshJobs [jobIndex].cutoutIndices;
			cutxssIndices = meshJobs [jobIndex].cutxssIndices;
			navMeshVertices = meshJobs [jobIndex].navMeshVertices;
			navMeshIndices = meshJobs [jobIndex].navMeshIndices;
			mivs = meshJobs [jobIndex].mivs;

			opaqueIndices.Clear ();
			cutoutIndices.Clear ();
			waterIndices.Clear ();
			cutxssIndices.Clear ();
			transpIndices.Clear ();
			tempChunkVertices.Clear ();
			tempChunkUV0.Clear ();
			tempChunkColors32.Clear ();
			tempChunkNormals.Clear ();
			mivs.Clear ();

			if (enableColliders) {
				meshColliderIndices.Clear ();
				meshColliderVertices.Clear ();
				if (enableNavMesh) {
					navMeshIndices.Clear ();
					navMeshVertices.Clear ();
				}
			}

			int opaqueVoxelsCount = 0;
			int cutoutVoxelsCount = 0;
			int waterVoxelsCount = 0;
			int transpVoxelsCount = 0;
			int cutxssVoxelsCount = 0;

			int chunkVoxelCount = 0;
			Color32 tintColor = Misc.color32White;
			Vector3 pos = Misc.vector3zero;

			Voxel[] voxels = chunk.voxels;

			const int V_ONE_Y_ROW = 18 * 18;
			const int V_ONE_Z_ROW = 18;
			ModelInVoxel miv = new ModelInVoxel ();

			int voxelSignature = 0;
			int voxelIndex = 0;
			for (int y = 0; y < 16; y++) {
				int vy = (y + 1) * 18 * 18;
				for (int z = 0; z < 16; z++) {
					int vyz = vy + (z + 1) * 18;
					for (int x = 0; x < 16; x++, voxelIndex++) {
						voxels [voxelIndex].lightMesh = voxels [voxelIndex].light;
						if (voxels [voxelIndex].hasContent != 1)
							continue;
						
						// If voxel is surrounded by material, don't render
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
						int v1b = chunk_middle_back_middle [middle_back_middle].opaque;
						int v1f = chunk_middle_forward_middle [middle_forward_middle].opaque;
						int v1u = chunk_top_middle_middle [top_middle_middle].opaque;
						int v1d = chunk_bottom_middle_middle [bottom_middle_middle].opaque;
						int v1l = chunk_middle_middle_left [middle_middle_left].opaque;
						int v1r = chunk_middle_middle_right [middle_middle_right].opaque;
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


						pos.x = x - 7.5f;
						pos.y = y - 7.5f;
						pos.z = z - 7.5f;

						chunkVoxelCount++;
						voxelSignature += voxelIndex;

						VoxelDefinition type = voxelDefinitions [voxels [voxelIndex].typeIndex];
						tintColor.r = voxels [voxelIndex].red;
						tintColor.g = voxels [voxelIndex].green;
						tintColor.b = voxels [voxelIndex].blue;

						switch (type.renderType) {
						case RenderType.Water:
							{
								int occ;
								int foam = 0;
								const int noflow = (1 << 8); // vertical flow
								waterVoxelsCount++;

								// Get corners heights
								int light = voxels [voxelIndex].light << 13;
								int flow = noflow;
								int hf = chunk_middle_forward_middle [middle_forward_middle].GetWaterLevel ();
								int hb = chunk_middle_back_middle [middle_back_middle].GetWaterLevel ();
								int hr = chunk_middle_middle_right [middle_middle_right].GetWaterLevel ();
								int hl = chunk_middle_middle_left [middle_middle_left].GetWaterLevel ();
								int th = chunk_top_middle_middle [top_middle_middle].GetWaterLevel ();
								int wh = voxels [voxelIndex].GetWaterLevel ();

								int corner_height_fr, corner_height_br, corner_height_fl, corner_height_bl;
								int hfr = 0, hbr = 0, hbl = 0, hfl = 0;
								// If there's water on top, full size
								if (th > 0) {
									corner_height_fr = corner_height_br = corner_height_fl = corner_height_bl = 15;
								} else {
									hfr = corner_height_fr = chunk_middle_forward_right [middle_forward_right].GetWaterLevel ();
									hbr = corner_height_br = chunk_middle_back_right [middle_back_right].GetWaterLevel ();
									hbl = corner_height_bl = chunk_middle_back_left [middle_back_left].GetWaterLevel ();
									hfl = corner_height_fl = chunk_middle_forward_left [middle_forward_left].GetWaterLevel ();

									int tf = chunk_top_forward_middle [top_forward_middle].GetWaterLevel ();
									int tfr = chunk_top_forward_right [top_forward_right].GetWaterLevel ();
									int tr = chunk_top_middle_right [top_middle_right].GetWaterLevel ();
									int tbr = chunk_top_back_right [top_back_right].GetWaterLevel ();
									int tb = chunk_top_back_middle [top_back_middle].GetWaterLevel ();
									int tbl = chunk_top_back_left [top_back_left].GetWaterLevel ();
									int tl = chunk_top_middle_left [top_middle_left].GetWaterLevel ();
									int tfl = chunk_top_forward_left [top_forward_left].GetWaterLevel ();

									// forward right corner
									if (tf * hf + tfr * corner_height_fr + tr * hr > 0) {
										corner_height_fr = 15;
									} else {
										corner_height_fr = wh > corner_height_fr ? wh : corner_height_fr;
										if (hf > corner_height_fr)
											corner_height_fr = hf;
										if (hr > corner_height_fr)
											corner_height_fr = hr;
									}
									// bottom right corner
									if (tr * hr + tbr * corner_height_br + tb * hb > 0) {
										corner_height_br = 15;
									} else {
										corner_height_br = wh > corner_height_br ? wh : corner_height_br;
										if (hr > corner_height_br)
											corner_height_br = hr;
										if (hb > corner_height_br)
											corner_height_br = hb;
									}
									// bottom left corner
									if (tb * hb + tbl * corner_height_bl + tl * hl > 0) {
										corner_height_bl = 15;
									} else {
										corner_height_bl = wh > corner_height_bl ? wh : corner_height_bl;
										if (hb > corner_height_bl)
											corner_height_bl = hb;
										if (hl > corner_height_bl)
											corner_height_bl = hl;
									}
									// forward left corner
									if (tl * hl + tfl * corner_height_fl + tf * hf > 0) {
										corner_height_fl = 15;
									} else {
										corner_height_fl = wh > corner_height_fl ? wh : corner_height_fl;
										if (hl > corner_height_fl)
											corner_height_fl = hl;
										if (hf > corner_height_fl)
											corner_height_fl = hf;
									}

									// flow
									int fx = corner_height_fr + corner_height_br - corner_height_fl - corner_height_bl;
									if (fx < 0)
										flow = 2 << 10;
									else if (fx == 0)
										flow = 1 << 10;
									else
										flow = 0;
									
									int fz = corner_height_fl + corner_height_fr - corner_height_bl - corner_height_br;
									if (fz > 0)
										flow += 2 << 8;
									else if (fz == 0)
										flow += 1 << 8;
								}
								pos.y -= 0.5f;

								// back face
								occ = chunk_middle_back_middle [middle_back_middle].hasContent;
								if (occ == 1) {
									// 0 means that face is visible
									if (hb == 0) {
										foam = 1;
									}
								} else {
									AddFaceWater (faceVerticesBack, normalsBack, pos, waterIndices, type.textureIndexSide, light + noflow, 0, corner_height_bl, 0, corner_height_br, tintColor);
								}

								// front face
								occ = chunk_middle_forward_middle [middle_forward_middle].hasContent;
								if (occ == 1) {
									if (hf == 0) {
										foam |= 2;
									}
								} else {
									AddFaceWater (faceVerticesForward, normalsForward, pos, waterIndices, type.textureIndexSide, light + noflow, 0, corner_height_fr, 0, corner_height_fl, tintColor);
								}

								// left face
								occ = chunk_middle_middle_left [middle_middle_left].hasContent;
								if (occ == 1) {
									if (hl == 0) {
										foam |= 4;
									}
								} else {
									AddFaceWater (faceVerticesLeft, normalsLeft, pos, waterIndices, type.textureIndexSide, light + noflow, 0, corner_height_fl, 0, corner_height_bl, tintColor);
								}

								// right face
								occ = chunk_middle_middle_right [middle_middle_right].hasContent;
								if (occ == 1) {
									if (hr == 0) {
										foam |= 8;
									}
								} else {
									AddFaceWater (faceVerticesRight, normalsRight, pos, waterIndices, type.textureIndexSide, light + noflow, 0, corner_height_br, 0, corner_height_fr, tintColor);
								}

								// top (hide only if water level is full or voxel on top is water)
								occ = chunk_top_middle_middle [top_middle_middle].hasContent;
								if (occ != 1 || (wh < 15 && th == 0)) {
									if (type.showFoam) {
										// corner foam
										if (hbl == 0) {
											foam |= chunk_middle_back_left [middle_back_left].hasContent << 4;
										}
										if (hfl == 0) {
											foam |= chunk_middle_forward_left [middle_forward_left].hasContent << 5;
										}
										if (hfr == 0) {
											foam |= chunk_middle_forward_right [middle_forward_right].hasContent << 6;
										}
										if (hbr == 0) {
											foam |= chunk_middle_back_right [middle_back_right].hasContent << 7;
										}
									} else {
										foam = 0;
									}
									AddFaceWater (faceVerticesTop, normalsUp, pos, waterIndices, type.textureIndexSide, light + foam + flow, corner_height_bl, corner_height_fl, corner_height_br, corner_height_fr, tintColor);
									AddFaceWater (faceVerticesTopFlipped, normalsUp, pos, waterIndices, type.textureIndexSide, light + foam + flow, corner_height_bl, corner_height_fl, corner_height_br, corner_height_fr, tintColor);
								}

								// bottom
								occ = chunk_bottom_middle_middle [bottom_middle_middle].hasContent;
								if (occ != 1) {
									AddFaceWater (faceVerticesBottom, normalsDown, pos, waterIndices, type.textureIndexSide, light + noflow, 0, 0, 0, 0, tintColor);
								}
							}
							break;
						case RenderType.CutoutCross:
							{
								cutxssVoxelsCount++;
								float light = voxels [voxelIndex].light / 15f;
								float random = WorldRand.GetValue (pos);
								light *= 1f + (random - 0.45f) * type.colorVariation;
								AddFaceVegetation (faceVerticesCross1, pos, cutxssIndices, type.textureIndexSide, light, tintColor);
								AddFaceVegetation (faceVerticesCross2, pos, cutxssIndices, type.textureIndexSide, light, tintColor);
							}
							break;
						case RenderType.OpaqueNoAO:
							{
								opaqueVoxelsCount++;

								// back face
								if (v1b < FULL_OPAQUE) {
									greedyNoAO.AddQuad (FaceDirection.Back, x, y, z, ref Misc.vector3back, ref tintColor, 1f, type.textureIndexSide);
								}
								// forward face
								if (v1f < FULL_OPAQUE) {
									greedyNoAO.AddQuad (FaceDirection.Forward, x, y, z, ref Misc.vector3forward, ref tintColor, 1f, type.textureIndexSide);
								}
								// left face
								if (v1l < FULL_OPAQUE) {
									greedyNoAO.AddQuad (FaceDirection.Left, z, y, x, ref Misc.vector3left, ref tintColor, 1f, type.textureIndexSide);
								}
								// right face
								if (v1r < FULL_OPAQUE) {
									greedyNoAO.AddQuad (FaceDirection.Right, z, y, x, ref Misc.vector3right, ref tintColor, 1f, type.textureIndexSide);
								}
								// top face
								if (v1u < FULL_OPAQUE) {
									greedyNoAO.AddQuad (FaceDirection.Top, x, z, y, ref Misc.vector3up, ref tintColor, 1f, type.textureIndexTop);
								}
								// bottom face
								if (v1d < FULL_OPAQUE) {
									greedyNoAO.AddQuad (FaceDirection.Bottom, x, z, y, ref Misc.vector3down, ref tintColor, 1f, type.textureIndexBottom);
								}
							}
							break;
						case RenderType.Transp6tex:
							{
								transpVoxelsCount++;

								int rotationIndex = voxels [voxelIndex].GetTextureRotation ();
								float light = voxels [voxelIndex].light / 15f;
								int typeIndex = voxels [voxelIndex].typeIndex;

								// back face
								if (v1b != FULL_OPAQUE && chunk_middle_back_middle [middle_back_middle].typeIndex != typeIndex) {
									AddFaceTransparent (faceVerticesBack, normalsBack, pos, transpIndices, type.textureSideIndices [rotationIndex].back, light, tintColor);
									if (enableColliders) {
										greedyCollider.AddQuad (FaceDirection.Back, x, y, z);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Back, x, y, z);
										}
									}
								}

								// front
								if (v1f != FULL_OPAQUE && chunk_middle_forward_middle [middle_forward_middle].typeIndex != typeIndex) {
									AddFaceTransparent (faceVerticesForward, normalsForward, pos, transpIndices, type.textureSideIndices [rotationIndex].forward, light, tintColor);
									if (enableColliders) {
										greedyCollider.AddQuad (FaceDirection.Forward, x, y, z);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Forward, x, y, z);
										}
									}
								}

								// top
								if (v1u != FULL_OPAQUE && chunk_top_middle_middle [top_middle_middle].typeIndex != typeIndex) {
									AddFaceTransparent (faceVerticesTop, normalsUp, pos, transpIndices, type.textureIndexTop, light, tintColor);
									if (enableColliders) {
										greedyCollider.AddQuad (FaceDirection.Top, x, z, y);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Top, x, z, y);
										}
									}
								}

								// down
								if (v1d != FULL_OPAQUE && chunk_bottom_middle_middle [bottom_middle_middle].typeIndex != typeIndex) {
									AddFaceTransparent (faceVerticesBottom, normalsDown, pos, transpIndices, type.textureIndexBottom, light, tintColor);
									if (enableColliders) {
										greedyCollider.AddQuad (FaceDirection.Bottom, x, z, y);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Bottom, x, z, y);
										}
									}
								}

								// left
								if (v1l != FULL_OPAQUE && chunk_middle_middle_left [middle_middle_left].typeIndex != typeIndex) {
									AddFaceTransparent (faceVerticesLeft, normalsLeft, pos, transpIndices, type.textureSideIndices [rotationIndex].left, light, tintColor);
									if (enableColliders) {
										greedyCollider.AddQuad (FaceDirection.Left, z, y, x);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Left, z, y, y);
										}
									}
								}
								// right
								if (v1r != FULL_OPAQUE && chunk_middle_middle_right [middle_middle_right].typeIndex != typeIndex) {
									AddFaceTransparent (faceVerticesRight, normalsRight, pos, transpIndices, type.textureSideIndices [rotationIndex].right, light, tintColor);
									if (enableColliders) {
										greedyCollider.AddQuad (FaceDirection.Right, z, y, x);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Right, z, y, y);
										}
									}
								}
							}
							break;
						default: //case RenderType.Custom:
							miv.vd = type;
							miv.voxelIndex = voxelIndex;
							mivs.Add (miv);
							break;
						case RenderType.Empty:
							{
								// back face
								if (v1b < FULL_OPAQUE) {												
									if (enableColliders) {
										greedyCollider.AddQuad (FaceDirection.Back, x, y, z);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Back, x, y, z);
										}
									}
								}
								// forward face
								if (v1f < FULL_OPAQUE) {
									if (enableColliders) {
										greedyCollider.AddQuad (FaceDirection.Forward, x, y, z);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Forward, x, y, z);
										}
									}
								}
								// left face
								if (v1l < FULL_OPAQUE) {
									if (enableColliders) {
										greedyCollider.AddQuad (FaceDirection.Left, z, y, x);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Left, z, y, y);
										}
									}
								}
								// right face
								if (v1r < FULL_OPAQUE) {
									if (enableColliders) {
										greedyCollider.AddQuad (FaceDirection.Right, z, y, x);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Right, z, y, y);
										}
									}
								}
								// top face
								if (v1u < FULL_OPAQUE) {
									if (enableColliders) {
										greedyCollider.AddQuad (FaceDirection.Top, x, z, y);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Top, x, z, y);
										}
									}
								}
								// bottom face
								if (v1d < FULL_OPAQUE) {
									if (enableColliders) {
										greedyCollider.AddQuad (FaceDirection.Bottom, x, z, y);
									}
								}
							}
							break;
						case RenderType.Opaque:
						case RenderType.Opaque6tex:
						case RenderType.Cutout: // Opaque & Cutout


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
								// Opaque / Cutout with AO

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

								float l0, l1, l2, l3;

								List<int> indices;
								bool denseTreeCheck = false;
								int windAnimation = 0;
								float aoBase = 1f / (4f * 15f); // 4 light factors per vertex
								if (type.renderType != RenderType.Cutout) {
									opaqueVoxelsCount++;
									indices = opaqueIndices;
								} else {
									cutoutVoxelsCount++;
									indices = cutoutIndices;
									denseTreeCheck = denseTrees;
									float random = WorldRand.GetValue (pos);
									aoBase *= 1f + (random - 0.45f) * type.colorVariation;
									if (type.windAnimation)
										windAnimation = 65536;
								}
								bool addCollider = enableColliders && voxels [voxelIndex].opaque > 5;
								int rotationIndex = voxels [voxelIndex].GetTextureRotation ();

								// back face
								if (chunk_middle_back_middle [middle_back_middle].opaque < FULL_OPAQUE || denseTreeCheck) {
									// Vertex 0
									l0 = (lb + v0b + v0bl + v1bl) * aoBase;
									// Vertex 2
									l1 = (lb + v1bl + v2bl + v2b) * aoBase;
									// Vertex 1
									l2 = (lb + v0b + v0br + v1br) * aoBase;
									// Vertex 3
									l3 = (lb + v2b + v2br + v1br) * aoBase;
									AddFaceWithAO (faceVerticesBack, normalsBack, pos, indices, type.textureSideIndices [rotationIndex].back + windAnimation, l0, l1, l2, l3, true, tintColor);
									if (addCollider) {
										greedyCollider.AddQuad (FaceDirection.Back, x, y, z);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Back, x, y, z);
										}
									}
								}
								// forward face
								if (chunk_middle_forward_middle [middle_forward_middle].opaque < FULL_OPAQUE || denseTreeCheck) {
									// Vertex 5
									l0 = (lf + v0f + v0fr + v1fr) * aoBase;
									// Vertex 6
									l1 = (lf + v1fr + v2fr + v2f) * aoBase;
									// Vertex 4
									l2 = (lf + v0f + v0fl + v1fl) * aoBase;
									// Vertex 7
									l3 = (lf + v2f + v2fl + v1fl) * aoBase;
									AddFaceWithAO (faceVerticesForward, normalsForward, pos, indices, type.textureSideIndices [rotationIndex].forward + windAnimation, l0, l1, l2, l3, true, tintColor);
									if (addCollider) {
										greedyCollider.AddQuad (FaceDirection.Forward, x, y, z);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Forward, x, y, z);
										}
									}
								}
								// left face
								if (chunk_middle_middle_left [middle_middle_left].opaque < FULL_OPAQUE || denseTreeCheck) {
									// Vertex 4
									l0 = (ll + v1fl + v0l + v0fl) * aoBase;
									// Vertex 7
									l1 = (ll + v2l + v2fl + v1fl) * aoBase;
									// Vertex 0
									l2 = (ll + v1bl + v0l + v0bl) * aoBase;
									// Vertex 2
									l3 = (ll + v2l + v2bl + v1bl) * aoBase;

									AddFaceWithAO (faceVerticesLeft, normalsLeft, pos, indices, type.textureSideIndices [rotationIndex].left + windAnimation, l0, l1, l2, l3, true, tintColor);
									if (addCollider) {
										greedyCollider.AddQuad (FaceDirection.Left, z, y, x);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Left, z, y, x);
										}
									}
								}
								// right face
								if (chunk_middle_middle_right [middle_middle_right].opaque < FULL_OPAQUE || denseTreeCheck) {
									// Vertex 1
									l0 = (lr + v1br + v0r + v0br) * aoBase;
									// Vertex 3
									l1 = (lr + v2r + v2br + v1br) * aoBase;
									// Vertex 5
									l2 = (lr + v1fr + v0r + v0fr) * aoBase;
									// Vertex 6
									l3 = (lr + v2r + v2fr + v1fr) * aoBase;
									AddFaceWithAO (faceVerticesRight, normalsRight, pos, indices, type.textureSideIndices [rotationIndex].right + windAnimation, l0, l1, l2, l3, true, tintColor);
									if (addCollider) {
										greedyCollider.AddQuad (FaceDirection.Right, z, y, x);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Right, z, y, x);
										}
									}
								}
								// top face
								if (chunk_top_middle_middle [top_middle_middle].opaque < FULL_OPAQUE || denseTreeCheck) {
									// Top face
									// Vertex 2
									l0 = (lu + v2b + v2bl + v2l) * aoBase;
									// Vertex 7
									l1 = (lu + v2l + v2fl + v2f) * aoBase;
									// Vvertex 3
									l2 = (lu + v2b + v2br + v2r) * aoBase;
									// Vertex 6
									l3 = (lu + v2r + v2fr + v2f) * aoBase;
									AddFaceWithAO (faceVerticesTop, normalsUp, pos, indices, type.textureIndexTop + windAnimation, l0, l1, l2, l3, true, tintColor);
									if (addCollider) {
										greedyCollider.AddQuad (FaceDirection.Top, x, z, y);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Top, x, z, y);
										}
									}
								}
								// bottom face
								if (chunk_bottom_middle_middle [bottom_middle_middle].opaque < FULL_OPAQUE || denseTreeCheck) {
									// Vertex 1
									l0 = (ld + v0b + v0r + v0br) * aoBase;
									// Vertex 5
									l1 = (ld + v0f + v0r + v0fr) * aoBase;
									// Vertex 0
									l2 = (ld + v0b + v0l + v0bl) * aoBase;
									// Vertex 4
									l3 = (ld + v0f + v0l + v0fl) * aoBase;
									AddFaceWithAO (faceVerticesBottom, normalsDown, pos, indices, type.textureIndexBottom + windAnimation, l0, l1, l2, l3, true, tintColor);
									if (addCollider) {
										greedyCollider.AddQuad (FaceDirection.Bottom, x, z, y);
										// no NavMesh for bottom faces
									}
								}
							} else {
								// Opaque / Cutout without AO
								bool denseTreeCheck = false;
								float aoBase = 1f / 15f;
								bool isOpaqueType = type.renderType != RenderType.Cutout;
								int windAnimation = 0;
								if (isOpaqueType) {
									opaqueVoxelsCount++;
								} else {
									cutoutVoxelsCount++;
									denseTreeCheck = denseTrees;
									if (type.windAnimation)
										windAnimation = 65536;
									float random = WorldRand.GetValue (pos);
									aoBase *= 1f + (random - 0.45f) * type.colorVariation;
								}
								bool addCollider = enableColliders && voxels [voxelIndex].opaque > 5;
								int rotationIndex = voxels [voxelIndex].GetTextureRotation ();

								// back face
								if (v1b < FULL_OPAQUE || denseTreeCheck) {												
									float backFaceGI = lb * aoBase;
									if (isOpaqueType) {
										greedyNoAO.AddQuad (FaceDirection.Back, x, y, z, ref Misc.vector3back, ref tintColor, backFaceGI, type.textureSideIndices [rotationIndex].back + windAnimation);
									} else {
										greedyCutout.AddQuad (FaceDirection.Back, x, y, z, ref Misc.vector3back, ref tintColor, backFaceGI, type.textureSideIndices [rotationIndex].back + windAnimation);
									}
									if (addCollider) {
										greedyCollider.AddQuad (FaceDirection.Back, x, y, z);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Back, x, y, z);
										}
									}
								}
								// forward face
								if (v1f < FULL_OPAQUE || denseTreeCheck) {
									float frontFaceGI = lf * aoBase;
									if (isOpaqueType) {
										greedyNoAO.AddQuad (FaceDirection.Forward, x, y, z, ref Misc.vector3forward, ref tintColor, frontFaceGI, type.textureSideIndices [rotationIndex].forward + windAnimation);
									} else {
										greedyCutout.AddQuad (FaceDirection.Forward, x, y, z, ref Misc.vector3forward, ref tintColor, frontFaceGI, type.textureSideIndices [rotationIndex].forward + windAnimation);
									}
									if (addCollider) {
										greedyCollider.AddQuad (FaceDirection.Forward, x, y, z);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Forward, x, y, z);
										}
									}
								}
								// left face
								if (v1l < FULL_OPAQUE || denseTreeCheck) {
									float leftFaceGI = ll * aoBase;
									if (isOpaqueType) {
										greedyNoAO.AddQuad (FaceDirection.Left, z, y, x, ref Misc.vector3left, ref tintColor, leftFaceGI, type.textureSideIndices [rotationIndex].left + windAnimation);
									} else {
										greedyCutout.AddQuad (FaceDirection.Left, z, y, x, ref Misc.vector3left, ref tintColor, leftFaceGI, type.textureSideIndices [rotationIndex].left);
									}
									if (addCollider) {
										greedyCollider.AddQuad (FaceDirection.Left, z, y, x);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Left, z, y, y);
										}
									}
								}
								// right face
								if (v1r < FULL_OPAQUE || denseTreeCheck) {
									float rightFaceGI = lr * aoBase;
									if (isOpaqueType) {
										greedyNoAO.AddQuad (FaceDirection.Right, z, y, x, ref Misc.vector3right, ref tintColor, rightFaceGI, type.textureSideIndices [rotationIndex].right + windAnimation);
									} else {
										greedyCutout.AddQuad (FaceDirection.Right, z, y, x, ref Misc.vector3right, ref tintColor, rightFaceGI, type.textureSideIndices [rotationIndex].right + windAnimation);
									}
									if (addCollider) {
										greedyCollider.AddQuad (FaceDirection.Right, z, y, x);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Right, z, y, y);
										}
									}
								}
								// top face
								if (v1u < FULL_OPAQUE || denseTreeCheck) {
									// Top face
									float topFaceGI = lu * aoBase;
									if (isOpaqueType) {
										greedyNoAO.AddQuad (FaceDirection.Top, x, z, y, ref Misc.vector3up, ref tintColor, topFaceGI, type.textureIndexTop + windAnimation);
									} else {
										greedyCutout.AddQuad (FaceDirection.Top, x, z, y, ref Misc.vector3up, ref tintColor, topFaceGI, type.textureIndexTop + windAnimation);
									}
									if (addCollider) {
										greedyCollider.AddQuad (FaceDirection.Top, x, z, y);
										if (enableNavMesh && type.navigatable) {
											greedyNavMesh.AddQuad (FaceDirection.Top, x, z, y);
										}
									}
								}
								// bottom face
								if (v1d < FULL_OPAQUE || denseTreeCheck) {
									float bottomFaceGI = ld * aoBase;
									if (isOpaqueType) {
										greedyNoAO.AddQuad (FaceDirection.Bottom, x, z, y, ref Misc.vector3down, ref tintColor, bottomFaceGI, type.textureIndexBottom + windAnimation);
									} else {
										greedyCutout.AddQuad (FaceDirection.Bottom, x, z, y, ref Misc.vector3down, ref tintColor, bottomFaceGI, type.textureIndexBottom + windAnimation);
									}
									if (addCollider) {
										greedyCollider.AddQuad (FaceDirection.Bottom, x, z, y);
									}
								}
							}
							break;
						}
					}
				}
			}

			meshJobs [jobIndex].chunk = chunk;
			meshJobs [jobIndex].totalVisibleVoxels = chunkVoxelCount;
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

			greedyNoAO.FlushTriangles (tempChunkVertices, opaqueIndices, tempChunkUV0, tempChunkNormals, enableTinting ? tempChunkColors32 : null);
			greedyCutout.FlushTriangles (tempChunkVertices, cutoutIndices, tempChunkUV0, tempChunkNormals, enableTinting ? tempChunkColors32 : null);

			meshJobs [jobIndex].vertices = tempChunkVertices;
			meshJobs [jobIndex].uv0 = tempChunkUV0;
			meshJobs [jobIndex].colors = tempChunkColors32;
			meshJobs [jobIndex].normals = tempChunkNormals;

			int matVoxelIndex = 0;
			int subMeshCount = 0;
			meshJobs [jobIndex].opaqueVoxelsCount = opaqueVoxelsCount;
			if (opaqueVoxelsCount > 0) {
				subMeshCount++;
				matVoxelIndex++;
			}
			meshJobs [jobIndex].cutoutVoxelsCount = cutoutVoxelsCount;
			if (cutoutVoxelsCount > 0) {
				subMeshCount++;
				matVoxelIndex += 2;
			}
			meshJobs [jobIndex].waterVoxelsCount = waterVoxelsCount;
			if (waterVoxelsCount > 0) {
				subMeshCount++;
				matVoxelIndex += 4;
			}
			meshJobs [jobIndex].cutxssVoxelsCount = cutxssVoxelsCount;
			if (cutxssVoxelsCount > 0) {
				subMeshCount++;
				matVoxelIndex += 8;
			}
			meshJobs [jobIndex].transpVoxelsCount = transpVoxelsCount;
			if (transpVoxelsCount > 0) {
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

		void AddFaceWithAO (Vector3[] faceVertices, Vector3[] normals, Vector3 pos, List<int> indices, int textureIndex, float w0, float w1, float w2, float w3, bool flipAO, Color32 tintColor) {
			int index = tempChunkVertices.Count;
			Vector3 vertPos;
			for (int v = 0; v < 4; v++) {
				vertPos.x = faceVertices [v].x + pos.x;
				vertPos.y = faceVertices [v].y + pos.y;
				vertPos.z = faceVertices [v].z + pos.z;
				tempChunkVertices.Add (vertPos);
				tempChunkNormals.Add (normals [v]);
			}

			if (flipAO && w0 + w3 > w1 + w2) {
				indices.Add (index);
				indices.Add (index + 1);
				indices.Add (index + 3);
				indices.Add (index + 3);
				indices.Add (index + 2);
				indices.Add (index + 0);
			} else {
				indices.Add (index);
				indices.Add (index + 1);
				indices.Add (index + 2);
				indices.Add (index + 3);
				indices.Add (index + 2);
				indices.Add (index + 1);
			}

			Vector4 v4 = new Vector4 (0, 0, textureIndex, w0);
			tempChunkUV0.Add (v4);
			v4.y = 1f;
			v4.w = w1;
			tempChunkUV0.Add (v4);
			v4.x = 1f;
			v4.y = 0;
			v4.w = w2;
			tempChunkUV0.Add (v4);
			v4.y = 1f;
			v4.w = w3;
			tempChunkUV0.Add (v4);
			if (enableTinting) {
				tempChunkColors32.Add (tintColor);
				tempChunkColors32.Add (tintColor);
				tempChunkColors32.Add (tintColor);
				tempChunkColors32.Add (tintColor);
			}
		}

		void AddFaceWater (Vector3[] faceVertices, Vector3[] normals, Vector3 pos, List<int> indices, int textureIndex, int w, int h0, int h1, int h2, int h3, Color32 tintColor) {
			int index = tempChunkVertices.Count;
			Vector3 vertPos;
			// vertices
			vertPos.x = faceVertices [0].x + pos.x;
			vertPos.y = h0 / 15f + pos.y;
			vertPos.z = faceVertices [0].z + pos.z;
			tempChunkVertices.Add (vertPos);
			tempChunkNormals.Add (normals [0]);
			vertPos.x = faceVertices [1].x + pos.x;
			vertPos.y = h1 / 15f + pos.y;
			vertPos.z = faceVertices [1].z + pos.z;
			tempChunkVertices.Add (vertPos);
			tempChunkNormals.Add (normals [1]);
			vertPos.x = faceVertices [2].x + pos.x;
			vertPos.y = h2 / 15f + pos.y;
			vertPos.z = faceVertices [2].z + pos.z;
			tempChunkVertices.Add (vertPos);
			tempChunkNormals.Add (normals [2]);
			vertPos.x = faceVertices [3].x + pos.x;
			vertPos.y = h3 / 15f + pos.y;
			vertPos.z = faceVertices [3].z + pos.z;
			tempChunkVertices.Add (vertPos);
			tempChunkNormals.Add (normals [3]);
			// indices
			indices.Add (index);
			indices.Add (index + 1);
			indices.Add (index + 2);
			indices.Add (index + 3);
			indices.Add (index + 2);
			indices.Add (index + 1);
			Vector4 v4 = new Vector4 (0f, 0f, textureIndex, w);
			tempChunkUV0.Add (v4);
			v4.y = 1f;
			tempChunkUV0.Add (v4);
			v4.x = 1f;
			v4.y = 0f;
			tempChunkUV0.Add (v4);
			v4.y = 1f;
			tempChunkUV0.Add (v4);
			if (enableTinting) {
				tempChunkColors32.Add (tintColor);
				tempChunkColors32.Add (tintColor);
				tempChunkColors32.Add (tintColor);
				tempChunkColors32.Add (tintColor);
			}
		}

		void AddFaceVegetation (Vector3[] faceVertices, Vector3 pos, List<int> indices, int textureIndex, float w, Color32 tintColor) {
			int index = tempChunkVertices.Count;

			// Add random displacement and elevation
			Vector3 aux = pos;
			float random = WorldRand.GetValue (aux);
			pos.x += random * 0.5f - 0.25f;
			aux.x += 1f;
			random = WorldRand.GetValue (aux);
			pos.z += random * 0.5f - 0.25f;
			pos.y -= random * 0.1f;
			for (int v = 0; v < 4; v++) {
				aux.x = faceVertices [v].x + pos.x;
				aux.y = faceVertices [v].y + pos.y;
				aux.z = faceVertices [v].z + pos.z;
				tempChunkVertices.Add (aux);
				tempChunkNormals.Add (Misc.vector3zero);
			}
			indices.Add (index);
			indices.Add (index + 1);
			indices.Add (index + 2);
			indices.Add (index + 3);
			indices.Add (index + 2);
			indices.Add (index + 1);
			Vector4 v4 = new Vector4 (0, 0, textureIndex, w);
			tempChunkUV0.Add (v4);
			v4.y = 1f;
			tempChunkUV0.Add (v4);
			v4.x = 1f;
			v4.y = 0f;
			tempChunkUV0.Add (v4);
			v4.y = 1f;
			tempChunkUV0.Add (v4);
			if (enableTinting) {
				tempChunkColors32.Add (tintColor);
				tempChunkColors32.Add (tintColor);
				tempChunkColors32.Add (tintColor);
				tempChunkColors32.Add (tintColor);
			}
		}


		void AddFaceTransparent (Vector3[] faceVertices, Vector3[] normals, Vector3 pos, List<int> indices, int textureIndex, float light, Color32 tintColor) {
			int index = tempChunkVertices.Count;
			Vector3 vertPos;
			for (int v = 0; v < 4; v++) {
				vertPos.x = faceVertices [v].x + pos.x;
				vertPos.y = faceVertices [v].y + pos.y;
				vertPos.z = faceVertices [v].z + pos.z;
				tempChunkVertices.Add (vertPos);
				tempChunkNormals.Add (normals [v]);
			}

			indices.Add (index);
			indices.Add (index + 1);
			indices.Add (index + 3);
			indices.Add (index + 3);
			indices.Add (index + 2);
			indices.Add (index + 0);

			Vector4 v4 = new Vector4 (0, 0, textureIndex, light);
			tempChunkUV0.Add (v4);
			v4.y = 1f;
			tempChunkUV0.Add (v4);
			v4.x = 1f;
			v4.y = 0;
			tempChunkUV0.Add (v4);
			v4.y = 1f;
			tempChunkUV0.Add (v4);
			if (enableTinting) {
				tempChunkColors32.Add (tintColor);
				tempChunkColors32.Add (tintColor);
				tempChunkColors32.Add (tintColor);
				tempChunkColors32.Add (tintColor);
			}
		}

		/// <summary>
		/// Creates a gameobject with geometry and materials based on the triangle renderer but lmited to one voxel
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		/// <param name="voxelIndex">Voxel index.</param>
		GameObject MakeDynamicCubeFromVoxel (VoxelChunk chunk, int voxelIndex) {
			VoxelDefinition type = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];
			Color32 tintColor = chunk.voxels [voxelIndex].color;

			Mesh mesh = null;
			if (type.dynamicMeshes == null) {
				type.dynamicMeshes = new Dictionary<Color, Mesh> ();
			} else {
				type.dynamicMeshes.TryGetValue (tintColor, out mesh);
			}

			if (mesh == null) {
				// Create cube mesh procedurally
				tempVertices.Clear ();
				tempNormals.Clear ();
				tempUVs.Clear ();
				tempColors.Clear ();
				tempIndicesPos = 0;

				AddFace (faceVerticesBack, normalsBack, type.textureIndexSide, tintColor);
				AddFace (faceVerticesForward, normalsForward, type.textureIndexSide, tintColor);
				AddFace (faceVerticesLeft, normalsLeft, type.textureIndexSide, tintColor);
				AddFace (faceVerticesRight, normalsRight, type.textureIndexSide, tintColor);
				AddFace (faceVerticesTop, normalsUp, type.textureIndexTop, tintColor);
				AddFace (faceVerticesBottom, normalsDown, type.textureIndexBottom, tintColor);

				mesh = new Mesh ();
				mesh.SetVertices (tempVertices);
				mesh.SetUVs (0, tempUVs);
				mesh.SetNormals (tempNormals);
				if (enableTinting) {
					mesh.SetColors (tempColors);
				}
				mesh.triangles = tempIndices;
				type.dynamicMeshes [tintColor] = mesh;
			}

			GameObject obj = new GameObject ("DynamicVoxelTemplate");
			obj.SetActive (false);

			MeshFilter mf = obj.AddComponent<MeshFilter> ();
			mf.mesh = mesh;

			MeshRenderer mr = obj.AddComponent<MeshRenderer> ();
			mr.sharedMaterial = type.renderType == RenderType.Cutout ? matTriangleCutout : matTriangleOpaque;

			BoxCollider collider = obj.AddComponent<BoxCollider> ();
			collider.size = Misc.vector3one * 0.98f;

			obj.transform.SetParent (worldRoot, false);

			return obj;

		}



		void AddFace (Vector3[] faceVertices, Vector3[] normals, int textureIndex, Color32 tintColor) {
			int index = tempVertices.Count;
			for (int v = 0; v < 4; v++) {
				tempVertices.Add (faceVertices [v]);
				tempNormals.Add (normals [v]);
			}

			tempIndices [tempIndicesPos++] = index;
			tempIndices [tempIndicesPos++] = index + 1;
			tempIndices [tempIndicesPos++] = index + 3;
			tempIndices [tempIndicesPos++] = index + 3;
			tempIndices [tempIndicesPos++] = index + 2;
			tempIndices [tempIndicesPos++] = index + 0;

			Vector4 v4 = new Vector4 (0, 0, textureIndex, 1f);
			tempUVs.Add (v4);
			v4.y = 1f;
			tempUVs.Add (v4);
			v4.x = 1f;
			v4.y = 0;
			tempUVs.Add (v4);
			v4.y = 1f;
			tempUVs.Add (v4);
			if (enableTinting) {
				tempColors.Add (tintColor);
				tempColors.Add (tintColor);
				tempColors.Add (tintColor);
				tempColors.Add (tintColor);
			}
		}


	}
}
