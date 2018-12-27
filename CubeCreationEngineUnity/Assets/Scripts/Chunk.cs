using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class Chunk
    {
        public Material cubeMaterial;
        public Block[,,] chunkData;
        public GameObject chunk;
        void BuildChunk() // Creating the chunks asynchronous to the normal unity logic
        {
            // Declaring the chunkData array
            chunkData = new Block[World.chunkSize, World.chunkSize, World.chunkSize];
            //Creating the blocks
            for (int z = 0; z < World.chunkSize; z++)
            {
                for (int y = 0; y < World.chunkSize; y++)
                {
                    for (int x = 0; x < World.chunkSize; x++)
                    {
                        Vector3 pos = new Vector3(x, y, z);
                        // declaring the worldx/y/z variable so that the blocks know where they are in the world
                        int worldX = (int)(x + chunk.transform.position.x);
                        int worldY = (int)(y + chunk.transform.position.y);
                        int worldZ = (int)(z + chunk.transform.position.z);
                        // generates the blocks in the chunks into a height map 
                        if (Utilities.fBM3D(worldX, worldY ,worldZ, Utilities.caveSmooth, Utilities.caveOctaves) < 0.42f)
                        {
                            chunkData[x, y, z] = new Block(Block.BlockType.AIR, pos, chunk.gameObject, this);
                        }
                        else if (worldY <= Utilities.GenerateStoneHeight(worldX,worldZ))
                        {
                            if (Utilities.fBM3D(worldX, worldY, worldZ, 0.01f, 2) < Utilities.DiamondChance && worldY < Utilities.DiamondSpawnHeight)
                            {
                                chunkData[x, y, z] = new Block(Block.BlockType.DIAMOND, pos, chunk.gameObject, this);
                            }
                            else
                            {
                                chunkData[x, y, z] = new Block(Block.BlockType.STONE, pos, chunk.gameObject, this);
                            }
                        }
                        else if (worldY == Utilities.GenerateDirtHeight(worldX, worldZ))
                        {
                            chunkData[x, y, z] = new Block(Block.BlockType.GRASS, pos, chunk.gameObject, this);
                        }
                        else if (worldY < Utilities.GenerateDirtHeight(worldX, worldZ))
                        {
                            chunkData[x, y, z] = new Block(Block.BlockType.DIRT, pos, chunk.gameObject, this);
                        }
                        else
                        {
                            chunkData[x, y, z] = new Block(Block.BlockType.AIR, pos, chunk.gameObject, this);
                        }
                    }
                }
            }
        }
        public void DrawChunk()
        {
            //Drawing the blocks
            for (int z = 0; z < World.chunkSize; z++)
            {
                for (int y = 0; y < World.chunkSize; y++)
                {
                    for (int x = 0; x < World.chunkSize; x++)
                    {
                        chunkData[x, y, z].Draw();
                    }
                }
            }
            CombineQuads();
            // creating and adding a meshcollider component to the individual chunks
            MeshCollider collider = chunk.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
            collider.sharedMesh = chunk.transform.GetComponent<MeshFilter>().mesh;
        }
        public Chunk(Vector3 position, Material c) // constructor for the chunks
        {
            chunk = new GameObject(World.BuildChunkName(position));
            chunk.transform.position = position;
            cubeMaterial = c;
            BuildChunk();
        }
        void CombineQuads() // Combines all of the meshs into one object
        {
            // Combine all children meshs
            MeshFilter[] meshFilters = chunk.GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            int i = 0;
            while (i < meshFilters.Length)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                i++;
            }
            // Create a new mesh on the parent object
            MeshFilter mf = (MeshFilter)chunk.AddComponent(typeof(MeshFilter));
            mf.mesh = new Mesh();
            // Add combined meshes on children as the parent's mesh
            mf.mesh.CombineMeshes(combine);
            // Creates a renderer for the parent
            MeshRenderer renderer = chunk.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            renderer.material = cubeMaterial;
            // Deletes all of the uncombined children
            foreach (Transform quad in chunk.transform)
            {
                GameObject.Destroy(quad.gameObject);
            }
        }
    }
}
