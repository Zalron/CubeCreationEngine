using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Realtime.Messaging.Internal;

namespace CubeCreationEngine.Core
{
    public class World : MonoBehaviour
    {
        public GameObject player;
        public Material textureAtlas; // the texture that is going to be aplided to the chunks
        public static int columnHeight = 16; // the height of the world
        public static int chunkSize = 16; // the size of the chunk
        public static int worldSize = 2; // size of the world
        public static int radius = 4;
        public static ConcurrentDictionary<string, Chunk> chunks; // a dictionary of all of the chunks
        bool firstbuild = true;
        public static string BuildChunkName(Vector3 v) // assigning a name to a chunk
        {
            return (int)v.x + "_" + (int)v.y + "_" + (int)v.z;
        }
        void BuildChunkAt(int x, int y, int z)// builds chunks
        {
            Vector3 chunkPosition = new Vector3(x*chunkSize, y * chunkSize, z * chunkSize);
            string n = BuildChunkName(chunkPosition);
            Chunk c;
            if (!chunks.TryGetValue(n, out c)) // checks if the chunks has already been generated
            {
                c = new Chunk(chunkPosition, textureAtlas);
                c.chunk.transform.parent = this.transform;
                chunks.TryAdd(c.chunk.name, c);
            }
        }
        IEnumerator BuildRecursiveWorld(int x, int y, int z, int radius)// builds chunks around the player
        {
            if (radius <= 0)
            {
                yield break;
            }
            BuildChunkAt(x, y, z - 1);
            StartCoroutine(BuildRecursiveWorld(x, y, z - 1, radius-1));
            yield return null;
        }
        IEnumerator DrawChunks() // looping through the dictionary and drawing the chunks that needed to be drawn
        {
            foreach (KeyValuePair<string, Chunk> c in chunks)
            {
                if (c.Value.status == Chunk.ChunkStatus.DRAW)
                {
                    c.Value.DrawChunk();
                }
            }
            yield return null;
        }
        void Start() // Use this for initialization
        {
            Vector3 ppos = player.transform.position;
            player.transform.position = new Vector3(ppos.x, Utilities.GenerateDirtHeight(ppos.x, ppos.z) + 1, ppos.z); // setting the player height to the chunk height = 1
            player.SetActive(false);
            firstbuild = true;
            chunks = new ConcurrentDictionary<string, Chunk>();
            this.transform.position = Vector3.zero;
            this.transform.rotation = Quaternion.identity;
            // build starting chunk
            BuildChunkAt((int)(player.transform.position.x / chunkSize), (int)(player.transform.position.y / chunkSize), (int)(player.transform.position.z / chunkSize));
            // draw it
            StartCoroutine(DrawChunks());
            // creates a bigger world
            StartCoroutine(BuildRecursiveWorld((int)(player.transform.position.x / chunkSize), (int)(player.transform.position.y / chunkSize), (int)(player.transform.position.z / chunkSize), radius));
        }
        void Update()// Update is called once per frame
        {
            if (!player.activeSelf)
            {
                player.SetActive(true);
                firstbuild = false;
            }
            StartCoroutine(DrawChunks());
        }
    }
}
