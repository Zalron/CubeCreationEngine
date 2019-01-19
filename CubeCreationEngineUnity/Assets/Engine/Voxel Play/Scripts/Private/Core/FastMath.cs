using System;
using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;

namespace VoxelPlay {

    public static class FastMath {

		[MethodImpl(256)]
        public static int FloorToInt(float n) {
#if UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
			int i = (int)n;
			if (i>n) i--;
			return i;
#else						
            return (int)(n + 1000000f) - 1000000;
#endif
        }

	}
}