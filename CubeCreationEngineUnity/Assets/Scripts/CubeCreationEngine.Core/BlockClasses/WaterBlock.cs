using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CubeCreationEngine.Core
{
    public class WaterBlock : Block
    {
        public Vector2[,] WaterBlockUVs = {

        };
        public WaterBlock(Vector3 pos, GameObject p, Material c)
        {
            bType = BlockType.WATER;
            parent = p;
            position = pos;
            cubeMaterial = c;
            isSolid = false;
            blockUVs = WaterBlockUVs;
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];


        }
    }
}
