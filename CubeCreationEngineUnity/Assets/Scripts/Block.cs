using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class Block
    {
        enum Cubeside { BOTTOM, TOP, LEFT, RIGHT, FRONT, BACK }// an enum to handle all of the sides of the cubes
        public enum BlockType // an enum that declares all of the block types in the game
        {
            GRASS,
            DIRT,
            WATER,
            STONE,
            SAND,
            GOLD,
            BEDROCK,
            REDSTONE,
            DIAMOND,
            NOCRACK,
            CRACK1,
            CRACK2,
            CRACK3,
            CRACK4,
            AIR
        }
        public BlockType bType;
        public bool isSolid;
        public Chunk owner; // the chunk that the block belongs to
        GameObject parent;
        public Vector3 position;
        public BlockType health; // set to the maxium health for each block which is set at SetType to NoCrack and adds with every punch 
        public int currentHealth; // the current health of the block
        // the amount of hits that the block can take
        // -1 is indistructible
        // 0 isn't a interactible block no value
        public int[] blockHealthMax = { 3, 3, 8, 4, 2, 4, -1, 4, 4, 0, 0, 0, 0, 0, 0 };
        // all of the uv variables
        Vector2[,] blockUVs = {
        {new Vector2( 0.125f, 0.375f ), new Vector2( 0.1875f, 0.375f),new Vector2( 0.125f, 0.4375f ),new Vector2( 0.1875f, 0.4375f )}, /*GRASS TOP*/
		{new Vector2( 0.1875f, 0.9375f ), new Vector2( 0.25f, 0.9375f),new Vector2( 0.1875f, 1.0f ),new Vector2( 0.25f, 1.0f )}, /*GRASS SIDE*/
		{new Vector2( 0.125f, 0.9375f ), new Vector2( 0.1875f, 0.9375f),new Vector2( 0.125f, 1.0f ),new Vector2( 0.1875f, 1.0f )}, /*DIRT*/
        {new Vector2( 0.875f, 0.125f ), new Vector2( 0.9375f, 0.125f),new Vector2( 0.875f, 0.1875f ),new Vector2( 0.9375f, 0.1875f )}, /*WATER*/
		{new Vector2( 0, 0.875f ), new Vector2( 0.0625f, 0.875f),new Vector2( 0, 0.9375f ),new Vector2( 0.0625f, 0.9375f )}, /*STONE*/
        {new Vector2( 0.125f, 0.875f ), new Vector2( 0.1875f, 0.875f),new Vector2( 0.125f, 0.9375f ),new Vector2( 0.1875f, 0.9375f )}, /*SAND*/
        {new Vector2( 0, 0.8125f ), new Vector2( 0.0625f, 0.8125f),new Vector2( 0, 0.875f ),new Vector2( 0.0625f, 0.0875f )}, /*GOLD*/
		{new Vector2( 0.3125f, 0.8125f ), new Vector2( 0.375f, 0.8125f),new Vector2( 0.3125f, 0.875f ),new Vector2( 0.375f, 0.875f )}, /*BEDROCK*/	
		{new Vector2( 0.1875f, 0.75f ), new Vector2( 0.25f, 0.75f), new Vector2( 0.1875f, 0.8125f ),new Vector2( 0.25f, 0.8125f )}, /*REDSTONE*/
		{new Vector2( 0.125f, 0.75f ), new Vector2( 0.1875f, 0.75f),new Vector2( 0.125f, 0.8125f ),new Vector2( 0.1875f, 0.8125f )}, /*DIAMOND*/
		{new Vector2( 0.6875f, 0f ), new Vector2( 0.75f, 0f),new Vector2( 0.6875f, 0.0625f ),new Vector2( 0.75f, 0.0625f )}, /*NOCRACK*/
		{new Vector2(0f,0f),  new Vector2(0.0625f,0f), new Vector2(0f,0.0625f), new Vector2(0.0625f,0.0625f)}, /*CRACK1*/
 		{new Vector2(0.0625f,0f),  new Vector2(0.125f,0f),new Vector2(0.0625f,0.0625f), new Vector2(0.125f,0.0625f)}, /*CRACK2*/
 		{new Vector2(0.125f,0f),  new Vector2(0.1875f,0f),new Vector2(0.125f,0.0625f), new Vector2(0.1875f,0.0625f)}, /*CRACK3*/
 		{new Vector2(0.1875f,0f),  new Vector2(0.25f,0f),new Vector2(0.1875f,0.0625f), new Vector2(0.25f,0.0625f)} /*CRACK4*/
        };
        public Block(BlockType b, Vector3 pos, GameObject p, Chunk o) // A constructor for the blocks 
        {
            bType = b;
            owner = o;
            parent = p;
            position = pos;
            if (bType == BlockType.AIR||bType == BlockType.WATER)
            {
                isSolid = false;
            }
            else
            {
                isSolid = true;
            }
            if (bType == BlockType.WATER)
            {
                parent = owner.fluid.gameObject;
            }
            else
            {
                parent = owner.chunk.gameObject;
            }
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];
        }
        public void SetType(BlockType b)
        {
            bType = b;
            if (bType == BlockType.AIR || bType == BlockType.WATER)
            {
                isSolid = false;
            }
            else
            {
                isSolid = true;
            }
            if (bType == BlockType.WATER)
            {
                parent = owner.fluid.gameObject;
            }
            else
            {
                parent = owner.chunk.gameObject;
            }
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];
        }
        public void Reset()
        {
            health = BlockType.NOCRACK;
            currentHealth = blockHealthMax[(int)bType];
            owner.Redraw();
        }
        public bool BuildBlock(BlockType b) // builds a block in the space you are pointing at from the variable set on the player object
        {
            //SetType(b);
            if (b == BlockType.WATER)
            {
                owner.mb.StartCoroutine(owner.mb.Flow(this, BlockType.WATER, blockHealthMax[(int)BlockType.WATER], 10));
            }
            else if(b == BlockType.SAND)
            {
                owner.mb.StartCoroutine(owner.mb.Drop(this, BlockType.SAND, 20));
            }
            else
            {
                SetType(b);
                owner.Redraw();
            }
            return true;
        }
        public bool HitBlock() // checking if the block is indistructable and taking away the health of the block until it is destroyed
        {
            if (currentHealth == -1)
            {
                return false;
            }
            currentHealth--;
            health++; // updating
            if (currentHealth == (blockHealthMax[(int)bType]-1) && World.doesBlocksHeal == true)
            {
                owner.mb.StartCoroutine(owner.mb.HealBlock(position));
            }
            if (currentHealth <= 0)
            {
                bType = BlockType.AIR;
                isSolid = false;
                health = BlockType.NOCRACK;
                owner.Redraw();
                owner.UpdateChunk();
                return true;
            }
            owner.Redraw();
            return false;
        }
        void CreateQuad(Cubeside side) // the function to create the cubes
        {
            // creates the mesh component on the object
            Mesh mesh = new Mesh();
            mesh.name = "ScriptedMesh" + side.ToString();
            // initialises all of the quad variables
            Vector3[] verts = new Vector3[4];
            Vector3[] normals = new Vector3[4];
            Vector2[] uvs = new Vector2[4];
            List<Vector2> suvs = new List<Vector2>();
            int[] triangles = new int[6];
            //all possible UVs
            Vector2 uv00;
            Vector2 uv10;
            Vector2 uv01;
            Vector2 uv11;
            // assigning the textures from the atlas
            if (bType == BlockType.GRASS && side == Cubeside.TOP) // grass
            {
                uv00 = blockUVs[0,0];
                uv10 = blockUVs[0,1];
                uv01 = blockUVs[0,2];
                uv11 = blockUVs[0,3];
            }
            else if (bType == BlockType.GRASS && side == Cubeside.BOTTOM)
            {
                uv00 = blockUVs[(int)(BlockType.DIRT+1), 0];
                uv10 = blockUVs[(int)(BlockType.DIRT+1), 1];
                uv01 = blockUVs[(int)(BlockType.DIRT+1), 2];
                uv11 = blockUVs[(int)(BlockType.DIRT+1), 3];
            }
            else
            {
                uv00 = blockUVs[(int)(bType+1), 0];
                uv10 = blockUVs[(int)(bType+1), 1];
                uv01 = blockUVs[(int)(bType+1), 2];
                uv11 = blockUVs[(int)(bType+1), 3];
            }
            // sets cracked uvs
            suvs.Add(blockUVs[(int)(health + 1), 3]);
            suvs.Add(blockUVs[(int)(health + 1), 2]);
            suvs.Add(blockUVs[(int)(health + 1), 0]);
            suvs.Add(blockUVs[(int)(health + 1), 1]);
            //all possible Verts
            Vector3 p0 = new Vector3(-0.5f, -0.5f, 0.5f);
            Vector3 p1 = new Vector3(0.5f, -0.5f, 0.5f);
            Vector3 p2 = new Vector3(0.5f, -0.5f, -0.5f);
            Vector3 p3 = new Vector3(-0.5f, -0.5f, -0.5f);
            Vector3 p4 = new Vector3(-0.5f, 0.5f, 0.5f);
            Vector3 p5 = new Vector3(0.5f, 0.5f, 0.5f);
            Vector3 p6 = new Vector3(0.5f, 0.5f, -0.5f);
            Vector3 p7 = new Vector3(-0.5f, 0.5f, -0.5f);
            switch (side) // dealing with assigning the raw quad data with the enum
            {
                case Cubeside.BOTTOM:
                    verts = new Vector3[] { p0, p1, p2, p3 };
                    normals = new Vector3[] { Vector3.down, Vector3.down, Vector3.down, Vector3.down };
                    uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                    triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                    break;
                case Cubeside.TOP:
                    verts = new Vector3[] { p7, p6, p5, p4 };
                    normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                    uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                    triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                    break;
                case Cubeside.LEFT:
                    verts = new Vector3[] { p7, p4, p0, p3 };
                    normals = new Vector3[] { Vector3.left, Vector3.left, Vector3.left, Vector3.left };
                    uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                    triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                    break;
                case Cubeside.RIGHT:
                    verts = new Vector3[] { p5, p6, p2, p1 };
                    normals = new Vector3[] { Vector3.right, Vector3.right, Vector3.right, Vector3.right };
                    uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                    triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                    break;
                case Cubeside.FRONT:
                    verts = new Vector3[] { p4, p5, p1, p0 };
                    normals = new Vector3[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
                    uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                    triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                    break;
                case Cubeside.BACK:
                    verts = new Vector3[] { p6, p7, p3, p2 };
                    normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
                    uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                    triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                    break;
            }
            // Sets all of the calucated faces into the mesh component
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.SetUVs(1, suvs);
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            // Creates the required components in the gameobject that this script is attached to, to show the created quad
            GameObject quad = new GameObject("Quads");
            quad.transform.position = position;
            quad.transform.parent = parent.transform;
            MeshFilter meshFilter = (MeshFilter)quad.AddComponent(typeof(MeshFilter));
            meshFilter.mesh = mesh;
        }
        int ConvertBlockIndexToLocal(int i) // converts the block index from other chunk into a index for this chunk
        {
            if (i == -1)
            {
                i = World.chunkSize - 1;
            }
            else if (i == World.chunkSize)
            {
                i = 0;
            }
            return i;
        }
        public Block GetBlock(int x, int y, int z) // most of the code from HasSolidNeighbour but HasSolidNeighbour calls this function
        {
            Block[,,] chunks;
            if (x < 0 || x >= World.chunkSize || y < 0 || y >= World.chunkSize || z < 0 || z >= World.chunkSize) // checking for solid neighbour in other chunks
            {
                Vector3 neighbourChunkPos = this.parent.transform.position + new Vector3((x - (int)position.x) * World.chunkSize, (y - (int)position.y) * World.chunkSize, (z - (int)position.z) * World.chunkSize);
                string nName = World.BuildChunkName(neighbourChunkPos);
                x = ConvertBlockIndexToLocal(x);
                y = ConvertBlockIndexToLocal(y);
                z = ConvertBlockIndexToLocal(z);
                Chunk nChunk;
                if (World.chunks.TryGetValue(nName, out nChunk))
                {
                    chunks = nChunk.chunkData;
                }
                else
                {
                    return null;
                }
            }
            // checking for solid neighbour in this chunk
            else
            {
                chunks = owner.chunkData;
            }
            return chunks[x, y, z];
        }
        public bool HasSolidNeighbour(int x, int y, int z) // checks if the block as a solid neighbour so the engine dosn't draw unnecessary faces
        {
            try
            {
                Block b = GetBlock(x, y, z);
                if (b != null)
                {
                    return (b.isSolid || b.bType == bType); // checking if the neighbour is solid or if the neighbour is the same block (used for water drawing)
                }
            }
            catch (System.IndexOutOfRangeException) { }
            return false;
        }
        public void Draw() // Draws the cube by creating the quads 
        {
            if (bType == BlockType.AIR)
            {
                return;
            }
            if (!HasSolidNeighbour((int)position.x, (int)position.y, (int)position.z + 1))
            {
                CreateQuad(Cubeside.FRONT);
            }
            if (!HasSolidNeighbour((int)position.x, (int)position.y, (int)position.z - 1))
            {
                CreateQuad(Cubeside.BACK);
            }
            if (!HasSolidNeighbour((int)position.x, (int)position.y + 1, (int)position.z))
            {
                CreateQuad(Cubeside.TOP);
            }
            if (!HasSolidNeighbour((int)position.x, (int)position.y - 1, (int)position.z))
            {
                CreateQuad(Cubeside.BOTTOM);
            }
            if (!HasSolidNeighbour((int)position.x - 1, (int)position.y, (int)position.z))
            {
                CreateQuad(Cubeside.LEFT);
            }
            if (!HasSolidNeighbour((int)position.x + 1, (int)position.y, (int)position.z))
            {
                CreateQuad(Cubeside.RIGHT);
            }
            //&& bType != BlockType.WATER
        }
    }
}
