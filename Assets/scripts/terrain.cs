using UnityEngine;
using System.Collections.Generic;

public class InfiniteTerrain : MonoBehaviour
{
    [Header("Generation Settings")]
    public int chunkSize = 16;
    public float noiseScale = 0.1f;
    public float heightMultiplier = 10f;
    public float viewDistance = 3; // Chunks around player in each direction

    [Header("Biome Settings")]
    public float biomeScale = 0.05f;
    public float biomeTransitionSmoothness = 2f;

    [Header("References")]
    public GameObject chunkPrefab;
    public Transform player;

    // Biome definitions
    private struct Biome
    {
        public float minHeight;
        public float maxHeight;
        public Color color;
        public float noiseScaleMultiplier;

        public Biome(float minH, float maxH, Color col, float scaleMult)
        {
            minHeight = minH;
            maxHeight = maxH;
            color = col;
            noiseScaleMultiplier = scaleMult;
        }
    }

    private Biome[] biomes = new Biome[]
    {
        new Biome(0f, 0.3f, new Color(0.2f, 0.8f, 0.2f), 1f),    // Grassland
        new Biome(0.3f, 0.6f, new Color(0.8f, 0.8f, 0.2f), 1.5f), // Desert
        new Biome(0.6f, 1f, new Color(0.5f, 0.5f, 0.5f), 0.8f)    // Mountains
    };

    private Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int lastPlayerChunk;

    void Start()
    {
        UpdateChunks();
    }

    void Update()
    {
        Vector2Int currentChunk = GetPlayerChunkCoord();
        if (currentChunk != lastPlayerChunk)
        {
            UpdateChunks();
            lastPlayerChunk = currentChunk;
        }
    }

    Vector2Int GetPlayerChunkCoord()
    {
        Vector3 playerPos = player.position;
        return new Vector2Int(
            Mathf.FloorToInt(playerPos.x / chunkSize),
            Mathf.FloorToInt(playerPos.z / chunkSize)
        );
    }

    void UpdateChunks()
    {
        Vector2Int playerChunk = GetPlayerChunkCoord();

        // Remove old chunks
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (Vector2Int coord in chunks.Keys)
        {
            if (Vector2Int.Distance(coord, playerChunk) > viewDistance)
            {
                chunksToRemove.Add(coord);
            }
        }

        foreach (Vector2Int coord in chunksToRemove)
        {
            Destroy(chunks[coord]);
            chunks.Remove(coord);
        }

        // Generate new chunks
        for (int x = playerChunk.x - (int)viewDistance; x <= playerChunk.x + (int)viewDistance; x++)
        {
            for (int z = playerChunk.y - (int)viewDistance; z <= playerChunk.y + (int)viewDistance; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                if (!chunks.ContainsKey(coord))
                {
                    GenerateChunk(coord);
                }
            }
        }
    }

    void GenerateChunk(Vector2Int coord)
    {
        GameObject chunk = Instantiate(chunkPrefab, 
            new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize), 
            Quaternion.identity);
        
        MeshFilter meshFilter = chunk.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = chunk.GetComponent<MeshRenderer>();
        
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[chunkSize * chunkSize];
        Color[] colors = new Color[chunkSize * chunkSize];
        int[] triangles = new int[(chunkSize - 1) * (chunkSize - 1) * 6];

        // Generate vertices
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float worldX = (coord.x * chunkSize) + x;
                float worldZ = (coord.y * chunkSize) + z;

                // Biome blending
                float biomeNoise = Mathf.PerlinNoise(worldX * biomeScale, worldZ * biomeScale);
                Biome biome1, biome2;
                float blendFactor;
                GetBiomeBlend(biomeNoise, out biome1, out biome2, out blendFactor);

                // Height calculation with biome influence
                float height1 = Mathf.PerlinNoise(worldX * noiseScale * biome1.noiseScaleMultiplier, 
                    worldZ * noiseScale * biome1.noiseScaleMultiplier);
                float height2 = Mathf.PerlinNoise(worldX * noiseScale * biome2.noiseScaleMultiplier, 
                    worldZ * noiseScale * biome2.noiseScaleMultiplier);
                float height = Mathf.Lerp(height1, height2, blendFactor) * heightMultiplier;

                int vertexIndex = x + z * chunkSize;
                vertices[vertexIndex] = new Vector3(x, height, z);
                colors[vertexIndex] = Color.Lerp(biome1.color, biome2.color, blendFactor);
            }
        }

        // Generate triangles
        int triIndex = 0;
        for (int x = 0; x < chunkSize - 1; x++)
        {
            for (int z = 0; z < chunkSize - 1; z++)
            {
                int a = x + z * chunkSize;
                int b = (x + 1) + z * chunkSize;
                int c = (x + 1) + (z + 1) * chunkSize;
                int d = x + (z + 1) * chunkSize;

                triangles[triIndex] = a;
                triangles[triIndex + 1] = b;
                triangles[triIndex + 2] = c;
                triangles[triIndex + 3] = a;
                triangles[triIndex + 4] = c;
                triangles[triIndex + 5] = d;
                triIndex += 6;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        chunks[coord] = chunk;
    }

    void GetBiomeBlend(float noise, out Biome biome1, out Biome biome2, out float blendFactor)
    {
        for (int i = 0; i < biomes.Length - 1; i++)
        {
            if (noise >= biomes[i].minHeight && noise <= biomes[i + 1].maxHeight)
            {
                biome1 = biomes[i];
                biome2 = biomes[i + 1];
                float range = biomes[i + 1].minHeight - biomes[i].maxHeight;
                float normalized = (noise - biomes[i].maxHeight) / range;
                blendFactor = Mathf.SmoothStep(0f, 1f, normalized * biomeTransitionSmoothness);
                return;
            }
        }

        // Edge cases
        if (noise <= biomes[0].maxHeight)
        {
            biome1 = biome2 = biomes[0];
            blendFactor = 0f;
        }
        else
        {
            biome1 = biome2 = biomes[biomes.Length - 1];
            blendFactor = 1f;
        }
    }
}
