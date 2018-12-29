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
        public static bool doesBlocksHeal = false;
        public Material textureAtlas; // the texture that is going to be aplided to the chunks
        public Material textureAtlasFuild; // the texture that is going to be aplided to the fuilds
        public static int columnHeight = 64; // the height of the world
        public static int chunkSize = 16; // the size of the chunk
        //public static int worldSize = 2; // size of the world
        public static int radius = 5;
        public static ConcurrentDictionary<string, Chunk> chunks; // a dictionary of all of the chunks
        public static List<string> toRemove = new List<string>(); // a list to remove the chunks that are not needed from the dictionary
        public static CoroutineQueue queue;
        public static uint MaxCorourtines = 4000; // must increase with the size of the radius
        public Vector3 lastBuildPos;// store position of player
        public static bool firstbuild = true;
        public float lastBuildTime;
        public float startTime;
        public static string BuildChunkName(Vector3 v) // assigning a name to a chunk
        {
            return (int)v.x + "_" + (int)v.y + "_" + (int)v.z;
        }
        public static string BuildColumnName(Vector3 v) // assigning a name to a column
        {
            return (int)v.x + "_" + (int)v.z;
        }
        void BuildChunkAt(int x, int y, int z)// builds chunks
        {
            Vector3 chunkPosition = new Vector3(x * chunkSize, y * chunkSize, z * chunkSize);
            string n = BuildChunkName(chunkPosition);
            Chunk c;
            if (!chunks.TryGetValue(n, out c)) // checks if the chunks has already been generated
            {
                c = new Chunk(chunkPosition, textureAtlas, textureAtlasFuild);
                c.chunk.transform.parent = this.transform;
                c.fluid.transform.parent = this.transform;
                chunks.TryAdd(c.chunk.name, c);
            }
        }
        public static Block GetWorldBlock(Vector3 pos) // getting the block in thw world when clicked on
        {
            int cx, cy, cz;
            //figuring out the chunk to get the block from
            if (pos.x < 0) // if an negitive numbers for each dimension to push the numbers into the next chunk into the negative direction
            {
                cx = (int)(Mathf.Round(pos.x - chunkSize) / (float)chunkSize) * chunkSize;
            }
            else // positive numbers
            {
                cx = (int)(Mathf.Round(pos.x) / (float)chunkSize) * chunkSize;
            }
            if (pos.y < 0) // if an negitive numbers for each dimension to push the numbers into the next chunk into the negative direction
            {
                cy = (int)(Mathf.Round(pos.y - chunkSize) / (float)chunkSize) * chunkSize;
            }
            else // positive numbers
            {
                cy = (int)(Mathf.Round(pos.y) / (float)chunkSize) * chunkSize;
            }
            if (pos.z < 0) // if an negitive numbers for each dimension to push the numbers into the next chunk into the negative direction
            {
                cz = (int)(Mathf.Round(pos.z - chunkSize) / (float)chunkSize) * chunkSize;
            }
            else // positive numbers
            {
                cz = (int)(Mathf.Round(pos.z) / (float)chunkSize) * chunkSize;
            }
            //turning the negative numbers into a positive number because their is no blocks in the negative number range
            int blx = (int)Mathf.Abs((float)Mathf.Round(pos.x) - cx);
            int bly = (int)Mathf.Abs((float)Mathf.Round(pos.y) - cy);
            int blz = (int)Mathf.Abs((float)Mathf.Round(pos.z) - cz);
            // turning the chunk position into a name to get the block position
            string cn = BuildChunkName(new Vector3(cx, cy, cz));
            Chunk c;
            if (chunks.TryGetValue(cn, out c))
            {
                return c.dirtBlockChunkData[blx, bly, blz]; // accessing the block position
            }
            else
            {
                return null;
            }
        }
        IEnumerator BuildRecursiveWorld(int x, int y, int z, int startradius, int radius)// builds chunks around the player
        {
            int nextradius = radius - 1;
            if (radius <= 0 || y < 0 || y > columnHeight)
            {
                yield break;
            }
            //builds chunk forward
            BuildChunkAt(x, y, z + 1);
            queue.Run(BuildRecursiveWorld(x, y, z + 1, radius, nextradius));
            yield return null;
            //builds chunk back
            BuildChunkAt(x, y, z - 1);
            queue.Run(BuildRecursiveWorld(x, y, z - 1, radius, nextradius));
            yield return null;
            //builds chunk left
            BuildChunkAt(x - 1, y, z );
            queue.Run(BuildRecursiveWorld(x - 1, y, z , radius, nextradius));
            yield return null;
            //builds chunk right
            BuildChunkAt(x + 1, y, z - 1);
            queue.Run(BuildRecursiveWorld(x + 1, y, z, radius, nextradius));
            yield return null;
            //builds chunk up
            BuildChunkAt(x, y + 1, z );
            queue.Run(BuildRecursiveWorld(x, y + 1, z, radius, nextradius));
            yield return null;
            //builds chunk down
            BuildChunkAt(x, y - 1, z - 1);
            queue.Run(BuildRecursiveWorld(x, y - 1, z, radius, nextradius));
            yield return null;
        }
        IEnumerator DrawChunks() // looping through the dictionary and drawing the chunks that needed to be drawn
        {
            toRemove.Clear();
            foreach (KeyValuePair<string, Chunk> c in chunks)
            {
                if (c.Value.status == Chunk.ChunkStatus.DRAW)
                {
                    c.Value.DrawChunk();
                }
                if (c.Value.chunk && Vector3.Distance(player.transform.position, c.Value.chunk.transform.position) > radius*chunkSize) // finds the chunks outside the players radius and sends them to the ToRemove list
                {
                    toRemove.Add(c.Key);
                }
            }
            yield return null;
        }
        IEnumerator RemoveOldChunks()
        {
            for (int i = 0; i < toRemove.Count; i++)
            {
                string n = toRemove[i];
                Chunk c;
                if (chunks.TryGetValue(n, out c))
                {
                    Destroy(c.chunk);
                    c.Save(); // saves the old chunk to the file
                    chunks.TryRemove(n, out c);
                    yield return null;
                }
            }
        }
        public void BuildNearPlayer()
        {
            StopCoroutine("BuildRecursiveWorld");
            lastBuildTime = Time.time;
            queue.Run(BuildRecursiveWorld((int)(player.transform.position.x / chunkSize), (int)(player.transform.position.y / chunkSize), (int)(player.transform.position.z / chunkSize), radius, radius));
        }
        void Start() // Use this for initialization
        {
            Vector3 ppos = player.transform.position;
            player.transform.position = new Vector3(ppos.x, Utilities.GenerateDirtHeight(ppos.x, ppos.z) + 1, ppos.z); // setting the player height to the chunk height = 1
            lastBuildPos = player.transform.position;
            player.SetActive(false);
            firstbuild = true;
            chunks = new ConcurrentDictionary<string, Chunk>();
            this.transform.position = Vector3.zero;
            this.transform.rotation = Quaternion.identity;
            queue = new CoroutineQueue(MaxCorourtines, StartCoroutine);
            startTime = Time.time;
            lastBuildTime = Time.time;
            Debug.Log("Start build");
            // build starting chunk
            BuildChunkAt((int)(player.transform.position.x / chunkSize), (int)(player.transform.position.y / chunkSize), (int)(player.transform.position.z / chunkSize));
            // draw it
            queue.Run(DrawChunks());
            // creates a bigger world
            queue.Run(BuildRecursiveWorld((int)(player.transform.position.x / chunkSize), (int)(player.transform.position.y / chunkSize), (int)(player.transform.position.z / chunkSize), radius, radius));
        }
        void Update()// Update is called once per frame
        {
            Vector3 movement = lastBuildPos - player.transform.position; 
            if (movement.magnitude > chunkSize) // checks if the player has moved over a chunk
            {
                lastBuildPos = player.transform.position;
                BuildNearPlayer();
            }
            if (!player.activeSelf) //checks if the player is active in the scene
            {
                player.SetActive(true);
                Debug.Log("Built it " + (Time.time - startTime));
                firstbuild = false;
            }
            queue.Run(DrawChunks());
            queue.Run(RemoveOldChunks());
        }
    }
}
