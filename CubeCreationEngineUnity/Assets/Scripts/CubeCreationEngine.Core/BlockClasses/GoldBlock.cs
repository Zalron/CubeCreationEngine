using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CubeCreationEngine.Core
{
    public class GoldBlock : Block
    {
        public Vector2[,] diamondBlockUVs = {

        };
        public GoldBlock(Vector3 pos, GameObject p, Material c)
        {
            bType = BlockType.GOLD;
            parent = p;
            position = pos;
            cubeMaterial = c;
            isSolid = true;
            blockUVs = diamondBlockUVs;
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];
        }
    }
}
