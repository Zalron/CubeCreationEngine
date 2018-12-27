using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class World : MonoBehaviour
    {
        public GameObject player;
        public Material textureAtlas; // the texture that is going to be aplided to the chunks
        public static int columnHeight = 16; // the height of the world
        public static int chunkSize = 16; // the size of the chunk
        public static int worldSize = 2; // size of the world
        public static int radus = 3;
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
            foreach (KeyValuePair<string, Chunk> c in chunks)
            {
                c.Value.DrawChunk();
                yield return null;
            }
        }
        IEnumerator BuildWorld()// builds columns of chunks around the player
        {
            int posx = (int)Mathf.Floor(player.transform.position.x / chunkSize);
            int posz = (int)Mathf.Floor(player.transform.position.z / chunkSize);
            for (int x = -radus; x < radus; x++)
            {
                for (int y = 0; y < columnHeight; y++)
                {
                    for (int z = -radus; z < radus; z++)
                    {
                        Vector3 chunkPosition = new Vector3((x+posx) * chunkSize, y * chunkSize, (z + posz) * chunkSize);
                        Chunk c = new Chunk(chunkPosition, textureAtlas);
                        c.chunk.transform.parent = this.transform;
                        chunks.Add(c.chunk.name, c);
                    }
                }
            }
            foreach (KeyValuePair<string, Chunk> c in chunks)
            {
                c.Value.DrawChunk();
                yield return null;
            }
            player.SetActive(true);
        }
        void Start() // Use this for initialization
        {
            player.SetActive(false);
            chunks = new Dictionary<string, Chunk>();
            this.transform.position = Vector3.zero;
            this.transform.rotation = Quaternion.identity;
            StartCoroutine(BuildWorld());
        }
        void Update()// Update is called once per frame
        {

        }
    }
}
