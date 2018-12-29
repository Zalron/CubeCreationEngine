using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class AirBlock : Block
    {
        public Vector2[,] airBlockUVs = {

        };
        public AirBlock(Vector3 pos, GameObject p, Material c)
        {
            bType = BlockType.AIR;
            parent = p;
            position = pos;
            isSolid = false;
            blockUVs = airBlockUVs;
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];


        }
    }
}
