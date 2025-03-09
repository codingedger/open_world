using UnityEngine;
using System.Collections.Generic;
using System.Linq;


[System.Serializable]
public class Biome{
    public string name;
    [Range(0, 5)]public float heightMultiplier = 1;
    [Range(0.1f , 0.5f)]public float noiseScaleModifier = 0.1f;
    public Color baseColor ;
    [Range(0,1)]public float minBiomeValue; // used for thresholding
}


public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int chunkSize = 16;           // Size of each terrain chunk
    public float noiseScale = 0.1f;      // Scale of the Perlin noise
    public float heightScale = 10f;      // Maximum height of terrain
    public int renderDistance = 2;       // How many chunks to render around player

    [Header("noise settings")]
    [Range(0,8)]public int octaves = 4;
    [Range(0,0.5f)]public float persistence = 0.5f;
    [Range(0,4)]public float lacunarity = 2;

    [Range(0,0.1f)]public float biomeNoiseScale = 0.1f;
    public Biome[] biomes;


    [Header("References")]
    public GameObject player;            // Reference to player object
    public Material terrainMaterial;     // Material for the terrain


    private Dictionary<Vector2Int, GameObject> terrainChunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int currentChunkCoord;
    public Vector2 noiseOffset;

    void Start(){

        if(biomes.Length <1) setUpExampleBiomes();
        noiseOffset = new Vector2(Random.value * 100, Random.value * 100);
        UpdateTerrainChunks();
    }

    void Update(){

        if(Input.GetKeyDown(KeyCode.Space))RegenerateTerrain();

        Vector2Int newChunkCoord = new Vector2Int(
            Mathf.FloorToInt(player.transform.position.x / chunkSize),
            Mathf.FloorToInt(player.transform.position.z / chunkSize)
        );

        if (newChunkCoord != currentChunkCoord){
            currentChunkCoord = newChunkCoord;
            UpdateTerrainChunks();
        }
    }

    void UpdateTerrainChunks()
    {
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var chunk in terrainChunks)
        {
            if (Vector2Int.Distance(chunk.Key, currentChunkCoord) > renderDistance)
            {
                chunksToRemove.Add(chunk.Key);
            }
        }

        foreach (var chunkCoord in chunksToRemove)
        {
            Destroy(terrainChunks[chunkCoord]);
            terrainChunks.Remove(chunkCoord);
        }

        for (int x = currentChunkCoord.x - renderDistance; x <= currentChunkCoord.x + renderDistance; x++)
        {
            for (int z = currentChunkCoord.y - renderDistance; z <= currentChunkCoord.y + renderDistance; z++)
            {
                Vector2Int chunkCoord = new Vector2Int(x, z);

                if (!terrainChunks.ContainsKey(chunkCoord))
                {
                    GenerateChunk(chunkCoord);
                }
            }
        }
    }

    void GenerateChunk(Vector2Int chunkCoord)
    {
        GameObject chunk = new GameObject("Terrain Chunk " + chunkCoord);
        chunk.transform.parent = transform;

        MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();

        Vector3[] vertices = new Vector3[(chunkSize + 1) * (chunkSize + 1)];
        int[] triangles = new int[chunkSize * chunkSize * 6];
        Vector2[] uv = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];

        int triIndex = 0;

        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                float worldX = (chunkCoord.x * chunkSize) + x;
                float worldZ = (chunkCoord.y * chunkSize) + z;

                //add noiseOffset for coordinates
                float biomeNoise = Mathf.PerlinNoise(
                    (worldX * biomeNoiseScale) + noiseOffset.x,
                    (worldZ * biomeNoiseScale) + noiseOffset.y
                );
                Biome currentBiome = getBiome(biomeNoise);

                float biomeHeight = GetLayeredNoise(
                    (worldX * noiseScale * currentBiome.noiseScaleModifier) + noiseOffset.x,
                    (worldZ * noiseScale * currentBiome.noiseScaleModifier) + noiseOffset.y
                )* heightScale * currentBiome.heightMultiplier;

                //float height = Mathf.PerlinNoise(worldX * noiseScale, worldZ * noiseScale) * heightScale;
                int vertexIndex = z * (chunkSize + 1) + x;

                vertices[vertexIndex] = new Vector3(x, biomeHeight, z);
                uv[vertexIndex] = new Vector2((float)x / chunkSize, (float)z / chunkSize); // Add this line
                colors[vertexIndex] = currentBiome.baseColor;
                //uv2[vertexIndex] = new Vector2(height, 0);
            }
        }

        for (int z = 0; z < chunkSize; z++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int vertexIndex = z * (chunkSize + 1) + x;

                triangles[triIndex] = vertexIndex;
                triangles[triIndex + 1] = vertexIndex + chunkSize + 1;
                triangles[triIndex + 2] = vertexIndex + 1;

                triangles[triIndex + 3] = vertexIndex + 1;
                triangles[triIndex + 4] = vertexIndex + chunkSize + 1;
                triangles[triIndex + 5] = vertexIndex + chunkSize + 2;


                triIndex += 6;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshRenderer.material = terrainMaterial;


        chunk.transform.position = new Vector3(chunkCoord.x * chunkSize, 0, chunkCoord.y * chunkSize);

        MeshCollider meshCollider = chunk.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        terrainChunks[chunkCoord] = chunk;
    }

    public Biome getBiome(float noise){
        foreach (var item in biomes){
            if(noise >= item.minBiomeValue) return item;
        }
        return biomes[biomes.Length - 1];
    }

    public float GetLayeredNoise(float x, float z){
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++){
            total += Mathf.PerlinNoise(
                (x * frequency) + noiseOffset.x,
                (z * frequency) + noiseOffset.y
            )* amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        return total / maxValue;

    }

    public void RegenerateTerrain()
    {
        print("Regenerating terrain");
        foreach (var chunk in terrainChunks)
        {
            Destroy(chunk.Value);
        }
        terrainChunks.Clear();
        
        // Add this line to change the terrain pattern
        noiseOffset = new Vector2(Random.value * 1000f, Random.value * 1000f);
        
        UpdateTerrainChunks();
    }

    public void setUpExampleBiomes(){
        biomes = new Biome[4];

        // 1. Plains Biome
        biomes[0] = new Biome
        {
            name = "Plains",
            heightMultiplier = 0.5f,         // Gentle, rolling hills
            noiseScaleModifier = 0.8f,       // Smoother noise pattern
            baseColor = new Color(0.45f, 0.7f, 0.3f), // Grassy green
            minBiomeValue = 0.5f           // Appears at higher noise values
        };

        // 2. Mountain Biome
        biomes[1] = new Biome
        {
            name = "Mountains",
            heightMultiplier = 2.5f,         // Tall, dramatic peaks
            noiseScaleModifier = 1.5f,       // Rougher, more detailed terrain
            baseColor = new Color(0.5f, 0.5f, 0.5f), // Rocky gray
            minBiomeValue = 0.7f            // Appears in mid-range noise
        };

        // 3. Desert Biome
        biomes[2] = new Biome
        {
            name = "Desert",
            heightMultiplier = 0.8f,         // Low dunes
            noiseScaleModifier = 1.2f,       // Slightly detailed noise
            baseColor = new Color(0.95f, 0.8f, 0.4f), // Sandy yellow
            minBiomeValue = 0.25f           // Appears in lower-mid noise
        };

        // 4. Ocean Biome
        biomes[3] = new Biome
        {
            name = "Ocean",
            heightMultiplier = 0.2f,         // Very flat
            noiseScaleModifier = 0.5f,       // Very smooth noise
            baseColor = new Color(0.2f, 0.4f, 0.8f), // Deep blue
            minBiomeValue = 0f              // Appears at lowest noise values
        };
    }

}
