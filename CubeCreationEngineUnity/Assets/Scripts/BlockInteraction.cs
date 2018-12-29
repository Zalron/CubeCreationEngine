using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCreationEngine.Core;
namespace CubeCreationEngine.Player
{
    public class BlockInteraction : MonoBehaviour
    {
        public Block.BlockType pBType;
        public GameObject cam;
        public int playerReach = 10;
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown("1"))
            {
                pBType = Block.BlockType.WATER;
            }
            if (Input.GetKeyDown("2"))
            {
                pBType = Block.BlockType.STONE;
            }
            if (Input.GetKeyDown("3"))
            {
                pBType = Block.BlockType.DIRT;
            }
            if (Input.GetKeyDown("4"))
            {
                pBType = Block.BlockType.GRASS;
            }
            if (Input.GetKeyDown("5"))
            {
                pBType = Block.BlockType.SAND;
            }
            if (Input.GetMouseButtonDown(0)|| Input.GetMouseButtonDown(1)) // checks for right or left mouse click
            {
                RaycastHit hit;
                //for mouse clicking
                //Ray ray = Camera.main,ScreenPointToRay(Input.mousePosition);
                //if(Physics.Raycast(ray,out hit,10))
                //{
                // for cross hairs
                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, playerReach)) // forward along the vector that the camera is looking at
                {
                    Chunk hitc;
                    if (!World.chunks.TryGetValue(hit.collider.gameObject.name, out hitc)) // if nothing was hit return to the start
                    {
                        return;
                    }
                    Vector3 hitBlock;
                    if (Input.GetMouseButtonDown(0)) // when left clicking
                    {
                        hitBlock = hit.point - hit.normal / 2.0f; // calculating a point inside the block that the player clicked on
                    }
                    else // when right clicking
                    {
                        hitBlock = hit.point + hit.normal / 2.0f; // calculating a point outside the block that the player clicked on
                    }
                    Block b = World.GetWorldBlock(hitBlock);
                    // Debug.Log(b.position);
                    hitc = b.owner;
                    //int x = (int)(Mathf.Round(hitBlock.x) - hit.collider.gameObject.transform.position.x);
                    //int y = (int)(Mathf.Round(hitBlock.y) - hit.collider.gameObject.transform.position.y);
                    //int z = (int)(Mathf.Round(hitBlock.z) - hit.collider.gameObject.transform.position.z);
                    bool update = false;
                    if (Input.GetMouseButtonDown(0)) // when left clicking it calls hitblock
                    {
                        update = b.HitBlock();
                    }
                    else // when right clicking it calls buildblock
                    {
                        update = b.BuildBlock(pBType);
                    }
                    if (update)
                    {
                        hitc.changed = true;
                        List<string> updates = new List<string>(); // get the neighbouting chunks blocks coordinates
                        float thisChunkx = hitc.chunk.transform.position.x;
                        float thisChunky = hitc.chunk.transform.position.y;
                        float thisChunkz = hitc.chunk.transform.position.z;
                        //updates.Add(hit.collider.gameObject.name);
                        //checks if neighbouring blocks at the edge of the chunks 
                        if (b.position.x == 0) // using the blocks position now
                        {
                            updates.Add(World.BuildChunkName(new Vector3(thisChunkx - World.chunkSize, thisChunky, thisChunkz)));
                        }
                        if (b.position.x == World.chunkSize - 1)
                        {
                            updates.Add(World.BuildChunkName(new Vector3(thisChunkx + World.chunkSize, thisChunky, thisChunkz)));
                        }
                        if (b.position.z == 0)
                        {
                            updates.Add(World.BuildChunkName(new Vector3(thisChunkx, thisChunky, thisChunkz - World.chunkSize)));
                        }
                        if (b.position.z == World.chunkSize - 1)
                        {
                            updates.Add(World.BuildChunkName(new Vector3(thisChunkx, thisChunky, thisChunkz + World.chunkSize)));
                        }
                        if (b.position.y == 0)
                        {
                            updates.Add(World.BuildChunkName(new Vector3(thisChunkx, thisChunky - World.chunkSize, thisChunkz)));
                        }
                        if (b.position.y == World.chunkSize - 1)
                        {
                            updates.Add(World.BuildChunkName(new Vector3(thisChunkx, thisChunky + World.chunkSize, thisChunkz)));
                        }
                        foreach (string cname in updates)
                        {
                            Chunk c;
                            if (World.chunks.TryGetValue(cname, out c)) // checking for the block in the chunk name to destroy the block
                            {
                                //c.chunkData[x, y, z].SetType(Block.BlockType.AIR); // setting the chunk data at the coordinated position to air
                                c.Redraw();
                            }
                        }
                    }
                }
            }
        }
    }
}
