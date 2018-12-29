using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class RedStoneBlock : Block
    {
        public Vector2[,] redStoneBlockUVs = {

        };
        public RedStoneBlock(Vector3 pos, GameObject p, Material c)
        {
            bType = BlockType.SAND;
            parent = p;
            position = pos;
            cubeMaterial = c;
            isSolid = true;
            blockUVs = redStoneBlockUVs;
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];


        }
    }
}

