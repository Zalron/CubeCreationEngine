using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VoxelPlay
{
	
	public class HeightMapCache
	{
		FastHashSet<HeightMapInfo[]> heightSectors;

		uint lastKey;
		HeightMapInfo[] lastSector;

		public HeightMapCache ()
		{
			heightSectors = new FastHashSet<HeightMapInfo[]> (16);
		}

		public void Clear ()
		{
			heightSectors.Clear ();
			lastKey = 0;
		}

		public bool TryGetValue (int x, int z, out HeightMapInfo[] heights, out int heightIndex)
		{

			int fx = x >> 8;
			int fz = z >> 8;
			heightIndex = ((z - (fz << 8)) << 8) + (x - (fx << 8));
			uint key = (uint)(((fz + 1024) << 16) + (fx + 1024));
			if (key == lastKey && lastSector != null) {
				heights = lastSector;
			} else if (!heightSectors.TryGetValue (key, out heights)) {
				heights = new HeightMapInfo[65536];
				heightSectors.Add (key, heights);
			} 
			lastSector = heights;
			lastKey = key;
			return heights [heightIndex].groundLevel != 0;
		}

	}

}