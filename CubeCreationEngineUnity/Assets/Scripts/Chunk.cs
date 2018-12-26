using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    enum Cubeside // an enum to handle all of the sides of the cubes
    {
        BOTTOM, TOP, LEFT, RIGHT, FRONT, BACK
    }
    public enum BlockType
    {
        GRASS, DIRT, STONE
    }
    public Material cubeMaterial;
    public BlockType bType;
    Vector2[,] blockUVs = {
        { new Vector2 (0.125f,0.375f), new Vector2 (0.1875f,0.375f), new Vector2 (0.125f,0.4375f), new Vector2 (0.1875f, 0.4375f)}, //Grass Top
        { new Vector2 (0.1875f,0.9375f), new Vector2 (0.25f,0.9375f), new Vector2 (0.1875f,1.0f), new Vector2 (0.25f,1.0f)}, //Grass Sides
        { new Vector2 (0.125f,0.9375f), new Vector2 (0.1875f,0.9375f), new Vector2 (0.125f,1.0f), new Vector2 (0.1875f,1.0f)}, //Dirt 
        { new Vector2 (0,0.875f), new Vector2 (0.0625f,0.875f), new Vector2 (0,0.9375f), new Vector2 (0.0625f,0.9375f)} //Stone
    };
    void CreateQuad(Cubeside side) // the function to create the cubes
    {
        // creates the mesh component on the object
        Mesh mesh = new Mesh();
        mesh.name = "ScriptedMesh" + side.ToString();
        // initialises all of the quad variables
        Vector3[] verts = new Vector3[4];
        Vector3[] normals = new Vector3[4];
        Vector2[] uvs = new Vector2[4];
        int[] triangles = new int[6];
        //all possible UVs
        Vector2 uv00;
        Vector2 uv10;
        Vector2 uv01;
        Vector2 uv11;
        // assigning the texture from the atlas
        if (bType == BlockType.GRASS && side == Cubeside.TOP)
        {
            uv00 = blockUVs[0, 0];
            uv10 = blockUVs[0, 1];
            uv01 = blockUVs[0, 2];
            uv11 = blockUVs[0, 3];
        }
        else if (bType == BlockType.GRASS && side == Cubeside.BOTTOM)
        {
            uv00 = blockUVs[(int)(BlockType.DIRT + 1), 0];
            uv10 = blockUVs[(int)(BlockType.DIRT + 1), 1];
            uv01 = blockUVs[(int)(BlockType.DIRT + 1), 2];
            uv11 = blockUVs[(int)(BlockType.DIRT + 1), 3];
        }
        else
        {
            uv00 = blockUVs[(int)(bType + 1), 0];
            uv10 = blockUVs[(int)(bType + 1), 1];
            uv01 = blockUVs[(int)(bType + 1), 2];
            uv11 = blockUVs[(int)(bType + 1), 3];
        }
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
                verts = new Vector3[] {p0, p1, p2, p3};
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
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        // Creates the required components in the gameobject that this script is attached to, to show the created quad
        GameObject quad = new GameObject("Quads");
        quad.transform.parent = this.gameObject.transform;
        MeshFilter meshFilter = (MeshFilter)quad.AddComponent(typeof(MeshFilter));
        meshFilter.mesh = mesh;
        MeshRenderer renderer = quad.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        renderer.material = cubeMaterial;
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
    void CreateCube() // Creates the cube by creating the quads 
    {
        CreateQuad(Cubeside.FRONT);
        CreateQuad(Cubeside.BACK);
        CreateQuad(Cubeside.TOP);
        CreateQuad(Cubeside.BOTTOM);
        CreateQuad(Cubeside.LEFT);
        CreateQuad(Cubeside.RIGHT);
        CombineQuads();
    }
	void Start () // Use this for initialization
    {
        CreateCube();
	}
	void Update () // Update is called once per frame
    {
		
	}
}
