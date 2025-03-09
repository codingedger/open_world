using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrainV2 : MonoBehaviour
{
    public int chunkSize = 16;
    public int worldHeight = 10;
    public float noiseScale = 0.1f;
    public int viewDistance = 3;
    public GameObject blockPrefab;

    private Dictionary<Vector2, GameObject> chunks = new Dictionary<Vector2, GameObject>();
    private Transform player;

    void Start()
    {
        player = Camera.main.transform;
        StartCoroutine(UpdateChunks());
    }

    IEnumerator UpdateChunks()
    {
        while (true)
        {
            Vector2 playerChunk = new Vector2(Mathf.Floor(player.position.x / chunkSize), Mathf.Floor(player.position.z / chunkSize));
            List<Vector2> newChunks = new List<Vector2>();

            for (int x = -viewDistance; x <= viewDistance; x++)
            {
                for (int z = -viewDistance; z <= viewDistance; z++)
                {
                    Vector2 chunkCoord = new Vector2(playerChunk.x + x, playerChunk.y + z);
                    if (!chunks.ContainsKey(chunkCoord))
                    {
                        newChunks.Add(chunkCoord);
                    }
                }
            }

            foreach (Vector2 chunk in newChunks)
            {
                GenerateChunk(chunk);
            }

            yield return new WaitForSeconds(1f);
        }
    }

    void GenerateChunk(Vector2 coord)
    {
        GameObject chunk = new GameObject("Chunk " + coord);
        chunk.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        chunks[coord] = chunk;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float biomeFactor = Mathf.PerlinNoise(coord.x * 0.1f, coord.y * 0.1f);
                float heightMultiplier = Mathf.Lerp(5, 20, biomeFactor);
                float height = Mathf.PerlinNoise((coord.x * chunkSize + x) * noiseScale, (coord.y * chunkSize + z) * noiseScale) * heightMultiplier;
                height = Mathf.Floor(height);

                for (int y = 0; y < height; y++)
                {
                    Vector3 position = new Vector3(coord.x * chunkSize + x, y, coord.y * chunkSize + z);
                    GameObject block = Instantiate(blockPrefab, position, Quaternion.identity, chunk.transform);
                    block.GetComponent<Renderer>().material.color = GetBiomeColor(biomeFactor);
                }
            }
        }
    }

    Color GetBiomeColor(float biomeFactor)
    {
        if (biomeFactor < 0.3f) return Color.green; // Grassland
        else if (biomeFactor < 0.6f) return Color.yellow; // Desert
        else return Color.gray; // Mountains
    }
}
