using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public Material cubeMaterial;
    public Block[,,] chunkData;
    IEnumerator BuildChunk(int sizeX, int sizeY, int sizeZ) // Creating the chunks asynchronous to the normal unity logic
    {
        // Declaring the chunkData array
        chunkData = new Block[sizeX, sizeY, sizeZ];
        //Creating the blocks
        for (int z = 0; z<sizeZ; z++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    Vector3 pos = new Vector3(x, y, z);
                    chunkData[x,y,z]= new Block(Block.BlockType.DIRT, pos, this.gameObject, cubeMaterial);
                }
            }
        }
        //Drawing the blocks
        for (int z = 0; z < sizeZ; z++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    chunkData[x,y,z].Draw();
                    yield return null;
                }
            }
        }
        CombineQuads();
    }
    void CombineQuads() // Combines all of the meshs into one object
    {
        // Combine all children meshs
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            i++;
        }
        // Create a new mesh on the parent object
        MeshFilter mf = (MeshFilter)this.gameObject.AddComponent(typeof(MeshFilter));
        mf.mesh = new Mesh();
        // Add combined meshes on children as the parent's mesh
        mf.mesh.CombineMeshes(combine);
        // Creates a renderer for the parent
        MeshRenderer renderer = this.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        renderer.material = cubeMaterial;
        // Deletes all of the uncombined children
        foreach (Transform quad in this.transform)
        {
            Destroy(quad.gameObject);
        }
    }
	void Start () // Use this for initialization
    {
        StartCoroutine(BuildChunk(4, 4, 4));
	}
	void Update () // Update is called once per frame
    {
		
	}
}
