using UnityEngine;
using System.Collections.Generic;

public class ProceduralTerrainManager : MonoBehaviour
{
    [Header("Terrain Generation")]
    [SerializeField] private float noiseScale = 50f;
    [SerializeField] private float heightScale = 1f;
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.1f, 0.1f);
    
    [Header("Chunk Management")]
    [SerializeField] private int chunkSize = 10;
    [SerializeField] private int viewDistance = 5;
    [SerializeField] private Transform player;
    [SerializeField] private Material terrainMaterial;
    
    private Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int lastPlayerChunk;
    
    private void Start()
    {
        if (player == null)
        {
            player = Camera.main.transform;
            Debug.LogWarning("No player assigned, using main camera");
        }
        
        // Generate initial chunks around player
        UpdateChunks();
    }
    
    private void Update()
    {
        Vector2Int playerChunk = GetPlayerChunk();
        if (playerChunk != lastPlayerChunk)
        {
            UpdateChunks();
            lastPlayerChunk = playerChunk;
        }
    }
    
    private void UpdateChunks()
    {
        Vector2Int playerChunk = GetPlayerChunk();
        HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();
        
        // Calculate needed chunks
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int offset = new Vector2Int(x, z);
                neededChunks.Add(playerChunk + offset);
            }
        }
        
        // Remove unneeded chunks
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var chunk in activeChunks)
        {
            if (!neededChunks.Contains(chunk.Key))
            {
                chunksToRemove.Add(chunk.Key);
            }
        }
        
        foreach (var chunk in chunksToRemove)
        {
            Destroy(activeChunks[chunk]);
            activeChunks.Remove(chunk);
        }
        
        // Create new chunks
        foreach (var chunk in neededChunks)
        {
            if (!activeChunks.ContainsKey(chunk))
            {
                CreateChunk(chunk);
            }
        }
    }
    
    private Vector2Int GetPlayerChunk()
    {
        Vector3 position = player.position;
        return new Vector2Int(
            Mathf.FloorToInt(position.x / chunkSize),
            Mathf.FloorToInt(position.z / chunkSize)
        );
    }
    
    private void CreateChunk(Vector2Int coord)
    {
        GameObject chunk = new GameObject($"Chunk_{coord.x}_{coord.y}");
        chunk.transform.parent = transform;
        chunk.layer = LayerMask.NameToLayer("Terrain");  // Set layer explicitly
        
        MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = chunk.AddComponent<MeshCollider>();
        
        // Generate mesh
        Mesh mesh = GenerateTerrainMesh(coord);
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshCollider.enabled = true;  // Ensure collider is enabled
        
        // Debug log collider setup
        // Debug.LogError($"Created chunk at {coord} with collider on layer {chunk.layer}");
        // Debug.LogError($"Collider bounds: {meshCollider.bounds}");
        
        meshRenderer.material = terrainMaterial;
        
        // Position chunk
        chunk.transform.position = new Vector3(
            coord.x * chunkSize,
            0,
            coord.y * chunkSize
        );
        
        activeChunks.Add(coord, chunk);
    }
    
    private Mesh GenerateTerrainMesh(Vector2Int coord)
    {
        Mesh mesh = new Mesh();
        
        int resolution = chunkSize + 1;
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        Vector2[] uvs = new Vector2[vertices.Length];
        
        // Generate vertices
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float worldX = x + (coord.x * chunkSize);
                float worldZ = z + (coord.y * chunkSize);
                
                // Generate height using multiple octaves of Perlin noise
                float height = 0f;
                float amplitude = 1f;
                float frequency = 1f;
                
                // First octave - larger features
                height += Mathf.PerlinNoise(
                    (worldX * frequency + Time.time * scrollSpeed.x) / noiseScale,
                    (worldZ * frequency + Time.time * scrollSpeed.y) / noiseScale
                ) * amplitude * heightScale;
                
                // Second octave - medium details
                frequency *= 2f;
                amplitude *= 0.5f;
                height += Mathf.PerlinNoise(
                    (worldX * frequency + Time.time * scrollSpeed.x) / noiseScale,
                    (worldZ * frequency + Time.time * scrollSpeed.y) / noiseScale
                ) * amplitude * heightScale;
                
                // Third octave - small details
                frequency *= 2f;
                amplitude *= 0.25f;
                height += Mathf.PerlinNoise(
                    (worldX * frequency + Time.time * scrollSpeed.x) / noiseScale,
                    (worldZ * frequency + Time.time * scrollSpeed.y) / noiseScale
                ) * amplitude * heightScale;
                
                int vertexIndex = z * resolution + x;
                vertices[vertexIndex] = new Vector3(x, height, z);
                // Create more detailed UV mapping for better visual reference
                float uvScale = 5f; // This will create more grid-like repetition
                uvs[vertexIndex] = new Vector2(
                    (x + coord.x * chunkSize) / (float)chunkSize * uvScale,
                    (z + coord.y * chunkSize) / (float)chunkSize * uvScale
                );
            }
        }
        
        // Generate triangles
        int triangleIndex = 0;
        for (int z = 0; z < resolution - 1; z++)
        {
            for (int x = 0; x < resolution - 1; x++)
            {
                int vertexIndex = z * resolution + x;
                
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + resolution;
                triangles[triangleIndex + 2] = vertexIndex + 1;
                triangles[triangleIndex + 3] = vertexIndex + 1;
                triangles[triangleIndex + 4] = vertexIndex + resolution;
                triangles[triangleIndex + 5] = vertexIndex + resolution + 1;
                
                triangleIndex += 6;
            }
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        
        return mesh;
    }
}