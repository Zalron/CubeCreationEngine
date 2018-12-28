using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCreationEngine.Core;
public class ChunkMB: MonoBehaviour
{
    Chunk owner;
    public ChunkMB()
    {
    }
    public void SetOwner(Chunk o)
    {
        owner = o;
        //InvokeRepeating("SaveProgress", 10, 1000);

    }

    public IEnumerator HealBlock(Vector3 bpos) // heals the block after three seconds
    {
        yield return new WaitForSeconds(3);
        int x = (int)bpos.x;
        int y = (int)bpos.y;
        int z = (int)bpos.z;

        if (owner.chunkData[x, y, z].bType != Block.BlockType.AIR)// if the block is not already destroyed then reset the block 
        {
            owner.chunkData[x, y, z].Reset();
        }
    }

    //void SaveProgress()
    //{
    //    if (owner.changed)
    //    {
    //        owner.Save();
    //        owner.changed = false;
    //    }
    //}
}
