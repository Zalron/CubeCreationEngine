using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace VoxelPlay {

	public enum CHUNK_RENDER_STATE : byte {
		Pending,
		RenderingRequested,
		RenderingComplete
	}

	public class VoxelChunk : MonoBehaviour {

		/// <summary>
		/// Voxels definition
		/// </summary>
		[NonSerialized]
		public Voxel[] voxels;

		/// <summary>
		/// Chunk center position. A 16x16x16 chunk starts at position-8 and ends on position+8
		/// </summary>
		[NonSerialized, HideInInspector] public Vector3 position;

		/// <summary>
		/// If the chunk is visible in frustum. This value is stored for internal optimization purposes and could not reflect the current state, call ChunkIsInFrustum() instead if you want to know if a chunk is within camera frustum.
		/// </summary>
		[NonSerialized, HideInInspector] public bool visibleInFrustum;

		[NonSerialized, HideInInspector] public int frustumCheckIteration;

		[NonSerialized, HideInInspector] public int lightmapSignature = -1;

		[NonSerialized, HideInInspector] public int voxelSignature;

		[NonSerialized, HideInInspector] public MeshFilter mf;

		[NonSerialized, HideInInspector] public MeshRenderer mr;

		[NonSerialized, HideInInspector] public MeshCollider mc;

		[NonSerialized, HideInInspector] public bool allowTrees = true;

		[NonSerialized, HideInInspector] public int navMeshSourceIndex = -1;

		[NonSerialized, HideInInspector] public Mesh navMesh;

		/// <summary>
		/// A flag that specified if this chunk is being hit by day light from above
		/// </summary>
		[NonSerialized, HideInInspector] public bool isAboveSurface;

		/// <summary>
		/// A flag that specifies that the chunk mesh needs to be rebuilt when it gets refreshed
		/// </summary>
		/// <value><c>true</c> if needs mesh rebuild; otherwise, <c>false</c>.</value>
		[NonSerialized, HideInInspector] public bool needsMeshRebuild;


		/// <summary>
		/// A flag that specifies that the chunk collider mesh needs to be rebuilt
		/// </summary>
		/// <value><c>true</c> if needs mesh rebuild; otherwise, <c>false</c>.</value>
		[NonSerialized, HideInInspector] public bool needsColliderRebuild;

		/// <summary>
		/// If chunk has been filled/populated with voxels. It might not been rendered yet.
		/// </summary>
		[NonSerialized, HideInInspector] public bool isPopulated;

		/// <summary>
		/// Chunk is pending rendering (in queue)
		/// </summary>
		/// <value><c>true</c> if inqueue; otherwise, <c>false</c>.</value>
		[NonSerialized, HideInInspector] public bool inqueue;

		/// <summary>
		/// If this chunk can be reused, or it's a special chunk that needs to stay as it's
		/// </summary>
		/// <value><c>true</c> if can be reused; otherwise, <c>false</c>.</value>
		[NonSerialized, HideInInspector]  public bool cannotBeReused;

		/// <summary>
		/// Chunk has been modified in game
		/// </summary>
		[NonSerialized, HideInInspector] public bool modified;

		/// <summary>
		/// Returns true if the chunk has been rendered at least once (and it might have no visible contents)
		/// </summary>
		[NonSerialized, HideInInspector] public CHUNK_RENDER_STATE renderState = CHUNK_RENDER_STATE.Pending;

		/// <summary>
		/// Chunk has been rendered and uploaded to the GPU?
		/// </summary>
		public bool isRendered { get { return renderState == CHUNK_RENDER_STATE.RenderingComplete; } }

		[NonSerialized, HideInInspector]
		public byte inconclusiveNeighbours;


		/// <summary>
		/// The frame number where this chunk is rendered. Used for optimization.
		/// </summary>
		[NonSerialized, HideInInspector]
		public int renderingFrame;


		[NonSerialized]
		public List<LightSource> lightSources;


		[NonSerialized]
		public List<VoxelPlaceholder> placeholders;

		VoxelChunk _top;
		public VoxelChunk top {
			get {
				if (_top == null) {
					VoxelPlayEnvironment.instance.GetChunk (position + new Vector3 (0, 16, 0), out _top, false);
					if (_top != null)
						_top._bottom = this;
				}
				return _top;
			}
			set {
				_top = value;
			}
		}

		VoxelChunk _bottom;
		public VoxelChunk bottom {
			get {
				if (_bottom == null) {
					VoxelPlayEnvironment.instance.GetChunk (position + new Vector3 (0, -16, 0), out _bottom, false);
					if (_bottom != null)
						_bottom._top = this;
				}
				return _bottom;
			}
			set {
				_bottom = value;
			}
		}

		VoxelChunk _left;
		public VoxelChunk left {
			get {
				if (_left == null) {
					VoxelPlayEnvironment.instance.GetChunk (position + new Vector3 (-16, 0, 0), out _left, false);
					if (_left != null)
						_left._right = this;
				}
				return _left;
			}
			set {
				_left = value;
			}

		}

		VoxelChunk _right;
		public VoxelChunk right {
			get {
				if (_right == null) {
					VoxelPlayEnvironment.instance.GetChunk (position + new Vector3 (16, 0, 0), out _right, false);
					if (_right != null)
						_right._left = this;
				}
				return _right;
			}
			set {
				_right = value;
			}

		}

		VoxelChunk _forward;
		public VoxelChunk forward {
			get {
				if (_forward == null) {
					VoxelPlayEnvironment.instance.GetChunk (position + new Vector3 (0, 0, 16), out _forward, false);
					if (_forward != null)
						_forward._back = this;
				}
				return _forward;
			}
			set {
				_forward = value;
			}

		}

		VoxelChunk _back;
		public VoxelChunk back {
			get {
				if (_back == null) {
					VoxelPlayEnvironment.instance.GetChunk (position + new Vector3 (0, 0, -16), out _back, false);
					if (_back != null)
						_back.forward = this;
				}
				return _back;
			}
			set {
				_back = value;
			}

		}

		/// <summary>
		/// Clears the lightmap of this chunk or initializes it with a value
		/// </summary>
		public void ClearLightmap (byte value = 0) {
			for (int k = 0; k < voxels.Length; k++) {
				voxels [k].light = value;
			}
		}

		/// <summary>
		/// Removes all existing voxels in this chunk.
		/// </summary>
		public void ClearVoxels (byte light) {
			if (lightSources != null) {
				int lightSourcesCount = lightSources.Count;
				for (int k = 0; k < lightSourcesCount; k++) {
					if (lightSources [k].gameObject != null) {
						DestroyImmediate (lightSources [k].gameObject);
					}
				}
				lightSources.Clear ();
			}
			if (placeholders != null) {
				int phCount = placeholders.Count;
				for (int k = 0; k < phCount; k++) {
					VoxelPlaceholder ph = placeholders [k];
					if (ph != null) {
						DestroyImmediate (ph.gameObject);
					}
				}
				placeholders.Clear ();
			}
			Voxel.Clear (voxels, light);
		}

		/// <summary>
		/// Returns true if this voxel contains a given position in world space
		/// </summary>
		/// <param name="position">Position.</param>
		public bool Contains (Vector3 position) {
			Vector3 min = mr.bounds.min;
			Vector3 max = mr.bounds.max;
			// check internal bound, non-inclusive
			return position.x > min.x && position.x < max.x && position.y > min.y && position.y < max.y && position.z > min.z && position.z < max.z;
		}


		/// <summary>
		/// Clears chunk state before returning it to the pool. This method is called when this chunk is reused.
		/// </summary>
		public void PrepareForReuse (byte light) {
			isAboveSurface = false;
			needsMeshRebuild = false;
			isPopulated = false;
			inqueue = false;
			modified = false;
			renderState = CHUNK_RENDER_STATE.Pending;
			allowTrees = true;
			lightmapSignature = -1;
			frustumCheckIteration = 0;
			navMesh = null;
			navMeshSourceIndex = -1;
			inconclusiveNeighbours = 0;
			renderingFrame = -1;

			if (_left != null) {
				_left.right = null;
				_left = null;
			}
			if (_right != null) {
				_right.left = null;
				_right = null;
			}
			if (_forward != null) {
				_forward.back = null;
				_forward = null;
			}
			if (_back != null) {
				_back.forward = null;
				_back = null;
			}
			if (_top != null) {
				_top.bottom = null;
				_top = null;
			}
			if (_bottom != null) {
				_bottom.top = null;
				_bottom = null;
			}
			ClearVoxels (light);
			mr.enabled = false;
		}

		/// <summary>
		/// Marks this chunk as inconclusive which means some neighbour may need to be rendered again due some special changes in this chunk (eg. drawing holes on this chunk edges)
		/// </summary>
		public void MarkAsInconclusive() {
			inconclusiveNeighbours |= 128;

		}


	}


}