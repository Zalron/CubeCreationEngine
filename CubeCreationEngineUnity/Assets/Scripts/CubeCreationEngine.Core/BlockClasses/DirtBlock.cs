using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class DirtBlock : Block
    {
        public Vector2[,] dirtBlockUVs = {
        {new Vector2( 0.125f, 0.9375f ), new Vector2( 0.1875f, 0.9375f),new Vector2( 0.125f, 1.0f ),new Vector2( 0.1875f, 1.0f )}, /*DIRT*/ 
        {new Vector2( 0.125f, 0.9375f ), new Vector2( 0.1875f, 0.9375f),new Vector2( 0.125f, 1.0f ),new Vector2( 0.1875f, 1.0f )}, /*DIRT*/ 
        {new Vector2( 0.125f, 0.9375f ), new Vector2( 0.1875f, 0.9375f),new Vector2( 0.125f, 1.0f ),new Vector2( 0.1875f, 1.0f )} /*DIRT*/ 
        };
        public DirtBlock(Vector3 pos, GameObject p, Material c)
        {
            bType = BlockType.DIRT;
            parent = p;
            position = pos;
            cubeMaterial = c;
            isSolid = true;
            blockUVs = dirtBlockUVs;
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];
        }

    }
}

