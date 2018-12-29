using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class BedrockBlock : Block
    {
        public Vector2[,] bedrockBlockUVs = {

        };
        public BedrockBlock(Vector3 pos, GameObject p, Material c)
        {
            bType = BlockType.BEDROCK;
            parent = p;
            position = pos;
            cubeMaterial = c;
            isSolid = true;
            blockUVs = bedrockBlockUVs;
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];
        }
    }
}
