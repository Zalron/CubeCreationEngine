using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCreationEngine.Core;
public class ChunkMB: MonoBehaviour
{
    Chunk owner;
    public ChunkMB(){}
    public void SetOwner(Chunk o)
    {
        owner = o;
        InvokeRepeating("SaveProgress", 10, 1000); //calls the save code in the first time after ten seconds and after that every thousand seconds

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
    void SaveProgress() // checks if this chunk has any blocks change 
    {
        if (owner.changed) //if true it will save the changes
        {
            owner.Save();
            owner.changed = false;
        }
    }
}
