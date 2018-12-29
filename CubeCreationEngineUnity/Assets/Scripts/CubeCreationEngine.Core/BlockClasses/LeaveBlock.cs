using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CubeCreationEngine.Core
{
    public class LeaveBlock : Block
    {
        public Vector2[,] leaveBlockUVs = {

        };
        public LeaveBlock(Vector3 pos, GameObject p, Material c)
        {
            bType = BlockType.LEAVES;
            parent = p;
            position = pos;
            cubeMaterial = c;
            isSolid = true;
            blockUVs = leaveBlockUVs;
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];
        }
    }
}
