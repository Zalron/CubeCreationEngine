using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCreationEngine.Core;
namespace CubeCreationEngine.Player
{
    public class BlockInteraction : MonoBehaviour
    {
        public GameObject cam;
        public int playerReach = 10;
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                //for mouse clicking
                //Ray ray = Camera.main,ScreenPointToRay(Input.mousePosition);
                //if(Physics.Raycast(ray,out hit,10))
                //{
                // for cross hairs
                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, playerReach)) // forward along the vector that the camera is looking at
                {
                    Vector3 hitBlock = hit.point - hit.normal / 2.0f; // calculating a point inside the block that the player clicked on
                    int x = (int)(Mathf.Round(hitBlock.x) - hit.collider.gameObject.transform.position.x);
                    int y = (int)(Mathf.Round(hitBlock.y) - hit.collider.gameObject.transform.position.y);
                    int z = (int)(Mathf.Round(hitBlock.z) - hit.collider.gameObject.transform.position.z);
                    List<string> updates = new List<string>(); // get the neighbouting chunks blocks coordinates
                    float thisChunkx = hit.collider.gameObject.transform.position.x;
                    float thisChunky = hit.collider.gameObject.transform.position.y;
                    float thisChunkz = hit.collider.gameObject.transform.position.z;
                    updates.Add(hit.collider.gameObject.name); // 
                    //checks if neighbouring blocks at the edge of the chunks 
                    if (x == 0 )
                    {
                        updates.Add(World.BuildChunkName(new Vector3(thisChunkx - World.chunkSize, thisChunky, thisChunkz)));
                    }
                    if (x == World.chunkSize - 1) 
                    {
                        updates.Add(World.BuildChunkName(new Vector3(thisChunkx + World.chunkSize, thisChunky , thisChunkz)));
                    }
                    if (z == 0)
                    {
                        updates.Add(World.BuildChunkName(new Vector3(thisChunkx, thisChunky, thisChunkz - World.chunkSize)));
                    }
                    if (z == World.chunkSize - 1)
                    {
                        updates.Add(World.BuildChunkName(new Vector3(thisChunkx, thisChunky, thisChunkz + World.chunkSize)));
                    }
                    if (y == 0)
                    {
                        updates.Add(World.BuildChunkName(new Vector3(thisChunkx, thisChunky - World.chunkSize, thisChunkz)));
                    }
                    if (y == World.chunkSize - 1)
                    {
                        updates.Add(World.BuildChunkName(new Vector3(thisChunkx, thisChunky + World.chunkSize, thisChunkz)));
                    }
                    foreach (string cname in updates)
                    {
                        Chunk c;
                        if (World.chunks.TryGetValue(cname, out c)) // checking for the block in the chunk name to destroy the block
                        {
                            DestroyImmediate(c.chunk.GetComponent<MeshFilter>());
                            DestroyImmediate(c.chunk.GetComponent<MeshRenderer>());
                            DestroyImmediate(c.chunk.GetComponent<Collider>());
                            c.chunkData[x, y, z].SetType(Block.BlockType.AIR); // setting the chunk data at the coordinated position to air
                            c.DrawChunk();
                        }
                    }
                   
                }
            }
        }
    }
}
