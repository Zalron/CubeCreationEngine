using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class SandBlock : Block
    {
        public Vector2[,] sandBlockUVs = {

        };
        public SandBlock(Vector3 pos, GameObject p, Material c)
        {
            bType = BlockType.SAND;
            parent = p;
            position = pos;
            cubeMaterial = c;
            isSolid = true;
            blockUVs = sandBlockUVs;
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];


        }
    }
}
