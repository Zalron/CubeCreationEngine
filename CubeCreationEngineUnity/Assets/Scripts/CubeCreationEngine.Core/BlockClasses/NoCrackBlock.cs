using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CubeCreationEngine.Core
{
    public class NoCrackBlock : Block
    {
        public Vector2[,] noCrackUVs = {

        };
        public NoCrackBlock(Vector3 pos, GameObject p, Material c)
        {
            bType = BlockType.NOCRACK;
            parent = p;
            position = pos;
            isSolid = false;
            blockUVs = noCrackUVs;
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];


        }
    }
}
