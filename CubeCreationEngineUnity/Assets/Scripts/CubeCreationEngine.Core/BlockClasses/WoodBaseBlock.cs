using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class WoodBaseBlock : Block
    {
        public Vector2[,] woodBaseBlockUVs = {

        };
        public WoodBaseBlock(Vector3 pos, GameObject p, Material c)
        {
            bType = BlockType.WOODBASE;
            parent = p;
            position = pos;
            cubeMaterial = c;
            isSolid = true;
            blockUVs = woodBaseBlockUVs;
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];


        }
    }
}
