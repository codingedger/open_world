using UnityEngine;

public class ProceduralTerrain : MonoBehaviour
{
    public int width = 256;  // Width of the terrain
    public int depth = 256;  // Depth of the terrain
    public float heightScale = 20f;  // Max height of terrain
    public float scale = 10f;  // Noise scale (controls "zoom" of noise)

    void Start()
    {
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
    }

    TerrainData GenerateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, heightScale, depth);
        float[,] heights = GenerateHeights();
        terrainData.SetHeights(0, 0, heights);
        return terrainData;
    }

    float[,] GenerateHeights()
    {
        float[,] heights = new float[width, depth];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float xCoord = (float)x / width * scale;
                float zCoord = (float)z / depth * scale;
                heights[x, z] = Mathf.PerlinNoise(xCoord, zCoord);
            }
        }
        return heights;
    }
}