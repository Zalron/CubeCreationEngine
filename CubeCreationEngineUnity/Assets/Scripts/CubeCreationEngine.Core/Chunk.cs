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
        public DirtBlock[,,] dirtBlockChunkData; // a three dimensional variable that stores all of the chunks blocks positions
        public AirBlock[,,] airBlockChunkData; // a three dimensional variable that stores all of the chunks blocks positions
        public StoneBlock[,,] stoneBlockChunkData; // a three dimensional variable that stores all of the chunks blocks positions
        public GrassBlock[,,] grassBlockChunkData; // a three dimensional variable that stores all of the chunks blocks positions
        public WaterBlock[,,] waterBlockChunkData; // a three dimensional variable that stores all of the chunks blocks positions
        //public DirtBlock[,,] dirtBlockChunkData; // a three dimensional variable that stores all of the chunks blocks positions
        public GameObject chunk; // the chunks gameobject 
        public GameObject fluid;
        public ChunkStatus status; 
        public float touchedTime;
        public bool treesCreated;
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
            //bd = new BlockData(dirtBlockChunkData);
            bf.Serialize(file, bd); // writing the data
            file.Close();
            //Debug.Log("Saving chunk from file: " + chunkFile);
        }
        public void UpdateChunk() // updates the chunk 
        {
            for (int z = 0; z < World.chunkSize; z++)
            {
                for (int y = 0; y < World.chunkSize; y++)
                {
                    for (int x = 0; x < World.chunkSize; x++)
                    {
                        //mb.StartCoroutine(mb.Drop(dirtBlockChunkData[x,y,z], Block.BlockType.SAND, 20));
                    }
                }
            }
        }
        void BuildChunk() // Creating the chunks asynchronous to the normal unity logic
        {
            bool dataFromFile = false;
            dataFromFile = Load(); // first we load data from a save file
            //touchedTime = Time.time;
            // Declaring the chunkData array
            //dirtBlockChunkData = new DirtBlock[World.chunkSize, World.chunkSize, World.chunkSize];
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
                        //if (dataFromFile) // before any chunks are created we check if there is a file to load data from
                        //{
                        //    chunkData[x, y, z] = new Block(bd.matrix[x, y, z], pos, chunk.gameObject, this);
                        //    continue;
                        //}
                        // generates the blocks in the chunks into a height map 
                        int surfaceHeight = Utilities.GenerateDirtHeight(worldX,worldZ);
                        int waterHeight = 490;
                        if (worldY == 0)
                        {
                            //dirtBlockChunkData[x, y, z] = new BedrockBlock(pos, chunk.gameObject, cubeMaterial);
                        }
                        else if (worldY <= Utilities.GenerateStoneHeight(worldX, worldZ))
                        {
                            if (Utilities.fBM3D(worldX, worldY, worldZ, 0.01f, 2) < 0.4f && worldY < Utilities.maxDiamondSpawnHeight)
                            {
                                //dirtBlockChunkData[x, y, z] = new DiamondBlock(pos, chunk.gameObject, cubeMaterial);
                            }
                            else if (Utilities.fBM3D(worldX, worldY, worldZ, 0.03f, 3) < 0.41f && worldY < 40)
                            {
                                //dirtBlockChunkData[x, y, z] = new RedStoneBlock(pos, chunk.gameObject, cubeMaterial);
                            }
                            else if (Utilities.fBM3D(worldX, worldY, worldZ, 0.03f, 3) < 0.41f && worldY < 80)
                            {
                                //dirtBlockChunkData[x, y, z] = new GoldBlock(pos, chunk.gameObject, cubeMaterial);
                            }
                            else
                            {
                                //dirtBlockChunkData[x, y, z] = new StoneBlock(pos, chunk.gameObject, cubeMaterial);
                            }
                        }
                        else if (worldY == surfaceHeight)
                        {
                            if (Utilities.fBM3D(worldX, worldY, worldZ, 0.4f, 2) < 0.4f)
                            {
                                //dirtBlockChunkData[x, y, z] = new WoodBaseBlock(pos, chunk.gameObject, cubeMaterial);
                            }
                            else
                            {
                                //dirtBlockChunkData[x, y, z] = new DirtBlock(pos, chunk.gameObject, cubeMaterial);
                            }
                           
                        }
                        else if (worldY < surfaceHeight)
                        {
                            //dirtBlockChunkData[x, y, z] = new GrassBlock(pos, chunk.gameObject, cubeMaterial);
                        }
                        else if (worldY == surfaceHeight && worldY <= Utilities.maxWaterSpawnHeight)
                        {
                            //dirtBlockChunkData[x, y, z] = new SandBlock(pos, chunk.gameObject, cubeMaterial);
                        }
                        else if (worldY < Utilities.maxWaterSpawnHeight)
                        {
                            waterBlockChunkData[x, y, z] = new WaterBlock(pos, fluid.gameObject, fluidMaterial);
                        }
                        else
                        {
                            airBlockChunkData[x, y, z] = new AirBlock(pos, chunk.gameObject, cubeMaterial);
                        }
                        if (airBlockChunkData[x,y,z].bType != Block.BlockType.WATER && Utilities.fBM3D(worldX, worldY, worldZ, 0.1f, 3) < 0.42f)
                        {
                            airBlockChunkData[x, y, z] = new AirBlock(pos, chunk.gameObject, cubeMaterial);
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
            if (!treesCreated)
            {
                for (int z = 0; z < World.chunkSize; z++)
                {
                    for (int y = 0; y < World.chunkSize; y++)
                    {
                        for (int x = 0; x < World.chunkSize; x++)
                        {
                            //BuildTrees(dirtBlockChunkData[x, y, z],x,y,z);
                        }
                    }
                }
                treesCreated = true;
            }
            //Drawing soild and water blocks 
            for (int z = 0; z < World.chunkSize; z++)
            {
                for (int y = 0; y < World.chunkSize; y++)
                {
                    for (int x = 0; x < World.chunkSize; x++)
                    {
                        //dirtBlockChunkData[x, y, z].Draw();
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
        void BuildTrees(Block trunk, int x, int y, int z)//builds the tree from the woodbase
        {
            if (trunk.bType != Block.BlockType.WOODBASE)
            {
                return;
            }
            Block t = trunk.GetBlock(x, y + 1, z);
            if (t != null)
            {
                t.SetType(Block.BlockType.WOOD);
                Block t1 = t.GetBlock(x, y + 2, z);
                if (t1 != null)
                {
                    t1.SetType(Block.BlockType.WOOD);
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int k = 3; k <=4; k++)
                            {
                                Block t2 = trunk.GetBlock(x + i, y + k, z + 1);
                                if (t2 != null)
                                {
                                    t2.SetType(Block.BlockType.LEAVES);
                                }
                                else
                                {
                                    return;
                                } 
                            }
                        }
                    }
                    Block t3 = t1.GetBlock(x, y + 5, z);
                    if (t3 != null)
                    {
                        t3.SetType(Block.BlockType.LEAVES);
                    }
                }
            }
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
