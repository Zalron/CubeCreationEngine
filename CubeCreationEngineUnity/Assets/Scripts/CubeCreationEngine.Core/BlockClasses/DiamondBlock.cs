using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class DiamondBlock : Block
    {
        public Vector2[,] diamondBlockUVs = {

        };
        public DiamondBlock(Vector3 pos, GameObject p, Material c)
        {
            bType = BlockType.DIAMOND;
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
