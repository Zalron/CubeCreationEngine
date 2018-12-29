using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class StoneBlock : Block
    {
        public Vector2[,] woodBaseBlockUVs = {

        };
        public StoneBlock(Vector3 pos, GameObject p, Material c)
        {
            bType = BlockType.STONE;
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
