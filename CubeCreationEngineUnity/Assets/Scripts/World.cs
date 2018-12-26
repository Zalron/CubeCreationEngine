﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public Material textureAtlas; // the texture that is going to be aplided to the chunks
    public static int columnHeight = 16; // the height of the world
    public static int chunkSize = 16; // the size of the chunk
    public static Dictionary<string, Chunk> chunks; // a disctionary of all of the chunks
    public static string BuildChunkName(Vector3 v) // assigning a name to a chunk
    {
        return (int)v.x + "_" + (int)v.y + "_" + (int)v.z;
    }
    IEnumerator BuildChunkColumn()// builds columns of chunks
    {
        for (int i = 0; i < columnHeight; i++)
        {
            Vector3 chunkPosition = new Vector3(this.transform.position.x, i * chunkSize, this.transform.position.z);
            Chunk c = new Chunk(chunkPosition, textureAtlas);
            c.chunk.transform.parent = this.transform;
            chunks.Add(c.chunk.name, c);
        }
        foreach (KeyValuePair<string,Chunk> c in chunks)
        {
            c.Value.DrawChunk();
            yield return null;
        }
    }
	void Start () // Use this for initialization
    {
        chunks = new Dictionary<string, Chunk>();
        this.transform.position = Vector3.zero;
        this.transform.rotation = Quaternion.identity;
        StartCoroutine(BuildChunkColumn());
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
