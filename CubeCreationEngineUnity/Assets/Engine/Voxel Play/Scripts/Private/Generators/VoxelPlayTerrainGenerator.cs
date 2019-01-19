using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public abstract class VoxelPlayTerrainGenerator : ScriptableObject {

		/// <summary>
		/// The maximum height allowed by the terrain generator (usually equals to 255)
		/// </summary>
		[Header ("Terrain Parameters")]
		public float maxHeight = 255;

		public float minHeight = -32;

		[Range (0, 1f), Tooltip ("Sea level (altitude)")]
		public float seaLevel = 0.1f;


		protected const int ONE_Y_ROW = 16 * 16;
		protected const int ONE_Z_ROW = 16;

		[NonSerialized]
		protected VoxelPlayEnvironment env;

		[NonSerialized]
		protected WorldDefinition world;

		/// <summary>
		/// Resets any cached data and reload info
		/// </summary>
		public abstract void Init ();

		/// <summary>
		/// Gets the altitude and moisture.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="z">The z coordinate.</param>
		/// <param name="altitude">Altitude (0..1) range.</param>
		/// <param name="moisture">Moisture (0..1) range.</param>
		public abstract void GetHeightAndMoisture (float x, float z, out float altitude, out float moisture);

		/// <summary>
		/// Paints the terrain inside the chunk defined by its central "position"
		/// </summary>
		/// <returns><c>true</c>, if terrain was painted, <c>false</c> otherwise.</returns>
		/// <param name="position">Central position of the chunk.</param>
		public abstract bool PaintChunk (VoxelChunk chunk);


		public void Initialize () {
			env = VoxelPlayEnvironment.instance;
			if (env == null)
				return;
			world = env.world;
			if (world == null)
				return;
			env.waterLevel = (int)(seaLevel * maxHeight);
			Init ();
		}

	}

}