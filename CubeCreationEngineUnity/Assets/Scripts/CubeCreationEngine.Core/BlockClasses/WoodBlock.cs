using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class WoodBlock : Block
    {
        public Vector2[,] woodBlockUVs = {
        
        };
        public WoodBlock(Vector3 pos, GameObject p, Material c)
        {
            bType = BlockType.WOOD;
            parent = p;
            position = pos;
            cubeMaterial = c;
            isSolid = true;
            blockUVs = woodBlockUVs;
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];
        }
    }
}

