using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    [Serializable]
    class BlockData // a Serializable class that will convert block data into binary
    {
        public Block.BlockType[,,] matrix; // a matrix that holds the block type and position in the chunk
        public BlockData() // empty constructor that is required
        {
        }
        public BlockData(Block[,,] b) // a constructor that we can pass through chunk data 
            // looping through the block data to get a matrix of the block type and position
        {
            matrix = new Block.BlockType[World.chunkSize, World.chunkSize, World.chunkSize];
            for (int z = 0; z < World.chunkSize; z++)
            {
                for (int y = 0; y < World.chunkSize; y++)
                {
                    for (int x = 0; x < World.chunkSize; x++)
                    {
                        matrix[x, y, z] = b[x, y, z].bType;
                    }
                }
            }
        }
    }
    public class Chunk
    {
        public enum ChunkStatus { DRAW, DONE, KEEP }
        public Material cubeMaterial;
        public Material fluidMaterial;
        public Block[,,] chunkData; // a three dimensional variable that stores all of the chunks blocks positions
        public GameObject chunk; // the chunks gameobject 
        public GameObject fluid;
        public ChunkStatus status; 
        public float touchedTime;
        public ChunkMB mb; //the chunks monobehaviour script
        BlockData bd; // the class that handles saving the block data
        public bool changed = false; //checks of any changes to the blocks in the chunks
        string BuildChunkFileName(Vector3 v) // builds a chunk file name for each chunk 
        {
            return Application.persistentDataPath + "/savedata/Chunk_" + (int)v.x + "_" + (int)v.y + "_" + (int)v.z + "_" + World.chunkSize + "_" + World.radius + ".dat";
        }
        bool Load()// read data from file
        {
            string chunkFile = BuildChunkFileName(chunk.transform.position); // contructs the file name
            if (File.Exists(chunkFile)) // if we have saved a File
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(chunkFile, FileMode.Open);
                bd = new BlockData();
                bd = (BlockData)bf.Deserialize(file); // putting it back into bd
                file.Close();
                //Debug.Log("Loading chunk from file: " + chunkFile);
                return true;
            }
            return false;
        }
        public void Save() // write data to file
        {
            string chunkFile = BuildChunkFileName(chunk.transform.position); // contructs the file name
            if (File.Exists(chunkFile)) // if we have saved a File
            {
                Directory.CreateDirectory(Path.GetDirectoryName(chunkFile));
            }
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(chunkFile, FileMode.OpenOrCreate);
            bd = new BlockData(chunkData);
            bf.Serialize(file, bd); // writing the data
            file.Close();
            //Debug.Log("Saving chunk from file: " + chunkFile);
        }
        void BuildChunk() // Creating the chunks asynchronous to the normal unity logic
        {
            bool dataFromFile = false;
            dataFromFile = Load(); // first we load data from a save file
            //touchedTime = Time.time;
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
                        if (dataFromFile) // before any chunks are created we check if there is a file to load data from
                        {
                            chunkData[x, y, z] = new Block(bd.matrix[x, y, z], pos, chunk.gameObject, this);
                            continue;
                        }
                        // generates the blocks in the chunks into a height map 
                        int surfaceHeight = Utilities.GenerateDirtHeight(worldX,worldZ);
                        int waterHeight = 490;
                        if (worldY == 0)
                        {
                            chunkData[x, y, z] = new Block(Block.BlockType.BEDROCK, pos, chunk.gameObject, this);
                        }
                        else if (worldY <= Utilities.GenerateStoneHeight(worldX, worldZ))
                        {
                            if (Utilities.fBM3D(worldX, worldY, worldZ, 0.01f, 2) < 0.4f && worldY < Utilities.maxDiamondSpawnHeight)
                            {
                                chunkData[x, y, z] = new Block(Block.BlockType.DIAMOND, pos, chunk.gameObject, this);
                            }
                            else if (Utilities.fBM3D(worldX, worldY, worldZ, 0.03f, 3) < 0.41f && worldY < 40)
                            {
                                chunkData[x, y, z] = new Block(Block.BlockType.REDSTONE, pos, chunk.gameObject, this);
                            }
                            else
                            {
                                chunkData[x, y, z] = new Block(Block.BlockType.STONE, pos, chunk.gameObject, this);
                            }
                        }
                        else if (worldY == surfaceHeight)
                        {
                            chunkData[x, y, z] = new Block(Block.BlockType.GRASS, pos, chunk.gameObject, this);
                        }
                        else if (worldY < surfaceHeight)
                        {
                            chunkData[x, y, z] = new Block(Block.BlockType.DIRT, pos, chunk.gameObject, this);
                        }
                        else if (worldY < Utilities.maxWaterSpawnHeight)
                        {
                            chunkData[x, y, z] = new Block(Block.BlockType.WATER, pos, fluid.gameObject, this);
                        }
                        else
                        {
                            chunkData[x, y, z] = new Block(Block.BlockType.AIR, pos, chunk.gameObject, this);
                        }
                        if (chunkData[x,y,z].bType != Block.BlockType.WATER && Utilities.fBM3D(worldX, worldY, worldZ, 0.1f, 3) < 0.42f)
                        {
                            chunkData[x, y, z] = new Block(Block.BlockType.AIR, pos, chunk.gameObject, this);
                        }
                        //if(worldY < Utilities.maxWaterSpawnHeight && chunk)
                        status = ChunkStatus.DRAW;
                    }
                }
            }
        }
        public void Redraw() //redraws the chunk texture
        {
            GameObject.DestroyImmediate(chunk.GetComponent<MeshFilter>());
            GameObject.DestroyImmediate(chunk.GetComponent<MeshRenderer>());
            GameObject.DestroyImmediate(chunk.GetComponent<Collider>());
            GameObject.DestroyImmediate(fluid.GetComponent<MeshFilter>());
            GameObject.DestroyImmediate(fluid.GetComponent<MeshRenderer>());
            GameObject.DestroyImmediate(fluid.GetComponent<Collider>());
            DrawChunk();
        }
        public void DrawChunk()
        {
            //Drawing soild and water blocks 
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
            CombineQuads(chunk.gameObject, cubeMaterial);
            // creating and adding a meshcollider component to the individual chunks
            MeshCollider collider = chunk.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
            collider.sharedMesh = chunk.transform.GetComponent<MeshFilter>().mesh;
            // creating water material but not adding in the collider 
            CombineQuads(fluid.gameObject, fluidMaterial);
            status = ChunkStatus.DONE;
        }
        public Chunk(){}
        public Chunk(Vector3 position, Material c, Material t) // constructor for the chunks
        {
            chunk = new GameObject(World.BuildChunkName(position));
            chunk.transform.position = position;
            fluid = new GameObject(World.BuildChunkName(position)+"_F");
            fluid.transform.position = position;
            mb = chunk.AddComponent<ChunkMB>();
            mb.SetOwner(this);
            cubeMaterial = c;
            fluidMaterial = t;
            BuildChunk();
        }
        void CombineQuads(GameObject o, Material m) // Combines all of the meshs into one object
        {
            // Combine all children meshs
            MeshFilter[] meshFilters = o.GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            int i = 0;
            while (i < meshFilters.Length)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                i++;
            }
            // Create a new mesh on the parent object
            MeshFilter mf = (MeshFilter)o.gameObject.AddComponent(typeof(MeshFilter));
            mf.mesh = new Mesh();
            // Add combined meshes on children as the parent's mesh
            mf.mesh.CombineMeshes(combine);
            // Creates a renderer for the parent
            MeshRenderer renderer = o.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            renderer.material = m;
            // Deletes all of the uncombined children
            foreach (Transform quad in o.transform)
            {
                GameObject.Destroy(quad.gameObject);
            }
        }
    }
}
