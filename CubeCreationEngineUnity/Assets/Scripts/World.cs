using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CubeCreationEngine.Core
{
    public class World : MonoBehaviour
    {
        public GameObject player;
        public Material textureAtlas; // the texture that is going to be aplided to the chunks
        public Slider loadingAmount;
        public Camera cam;
        public Button playButton;
        public static int columnHeight = 16; // the height of the world
        public static int chunkSize = 16; // the size of the chunk
        public static int worldSize = 2; // size of the world
        public static int radius = 3;
        public static Dictionary<string, Chunk> chunks; // a disctionary of all of the chunks
        bool firstbuild = true;
        bool building = false;
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
            building = true;
            int posx = (int)Mathf.Floor(player.transform.position.x / chunkSize);
            int posz = (int)Mathf.Floor(player.transform.position.z / chunkSize);
            float totalChunks = (Mathf.Pow(radius * 2 + 1, 2) * columnHeight) * 2;
            int processCount = 0;
            for (int x = -radius; x < radius; x++)
            {
                for (int y = 0; y < columnHeight; y++)
                {
                    for (int z = -radius; z < radius; z++)
                    {
                        Vector3 chunkPosition = new Vector3((x+posx) * chunkSize, y * chunkSize, (z + posz) * chunkSize);
                        Chunk c = new Chunk(chunkPosition, textureAtlas);
                        string n = BuildChunkName(chunkPosition);
                        if (chunks.TryGetValue(n, out c)) // checks if the chunks has already been generated
                        {
                            c.status = Chunk.ChunkStatus.KEEP;
                            break;
                        }
                        else // generates a new chunk
                        {
                            c = new Chunk(chunkPosition, textureAtlas);
                            c.chunk.transform.parent = this.transform;
                            chunks.Add(c.chunk.name, c);
                        }
                        if (firstbuild) // checks if its the first build of the game
                        {
                            processCount++;
                            loadingAmount.value = processCount / totalChunks * 100;
                        }
                        yield return null;
                    }
                }
            }
            foreach (KeyValuePair<string, Chunk> c in chunks)
            {
                if (c.Value.status == Chunk.ChunkStatus.DRAW)
                {
                    c.Value.DrawChunk();
                    c.Value.status = Chunk.ChunkStatus.KEEP;
                }
                // deletes old chunks
                c.Value.status = Chunk.ChunkStatus.DONE;
                if (firstbuild)
                {
                    processCount++;
                    loadingAmount.value = processCount / totalChunks * 100;
                }
                yield return null;

            }
            if (firstbuild) // when the first build is finally done
            {
                player.SetActive(true);
                loadingAmount.gameObject.SetActive(false);
                cam.gameObject.SetActive(false);
                playButton.gameObject.SetActive(false);
                firstbuild = false;
            }
            building = false;
        }
        public void StartBuild() // the function that is called when the player presses the play button
        {
            StartCoroutine(BuildWorld());
        }
        void Start() // Use this for initialization
        {
            player.SetActive(false);
            chunks = new Dictionary<string, Chunk>();
            this.transform.position = Vector3.zero;
            this.transform.rotation = Quaternion.identity;
        }
        void Update()// Update is called once per frame
        {
            if (!building && !firstbuild) // checks if it is not building and it is not the first build it starts again
            {
                StartCoroutine(BuildWorld());
            }
        }
    }
}
