using UnityEngine;
using Utils;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    private Mesh mesh;

    private Vector3[] verts;
    private int[] tris;

    private MeshFilter mf;
    private MeshRenderer mr;
    private MeshCollider mc;

    [Header("Terrain Parameters")]
    public int xSize = 20;
    public int zSize = 20;

    public float frequency = 0.3f;
    public float amplitude = 2f;
    public int octaves = 2;

    public float frequencyModifier = 2;
    public float amplitudeModifier = 1;
    public float maskSize = 50;

    private Vector3 center;

    [Header("Hill Parameters")]
    public float ringRadius = 25f;
    public float ringWidth = 10f;
    public float ringHeight = 2f;
    public float ringPower = 2f;
    public Vector2 ringCenterOffset = Vector2.zero;

    private float globalMinY;

    [Header("Water POI Parameters")]
    public int waterPOICount = 5;
    public int border = 5;
    public float waterDist = 5;
    public float waterHeight = 0.5f;
    public float maxWaterLevel = 2.5f;
    public float waterSink = 0.03f;
    public Material waterMat;

    private List<GameObject> waterSurfaces;
    private List<Vector3> waterCenters;
    private PriorityQueue<Vector3, float> minHeights;

    [Header("Obstacle POI Parameters")]
    public int obstacleCount = 10;
    public float obstacleDist = 3;

    private List<GameObject> obstacles;

    [Header("Forest Parameters")]
    public float coverage = 0.2f;
    public float density = 0.5f;

    public GameObject[] treePrefabs;

    private List<GameObject> trees;

    [Header("Spawn Parameters")]
    public int spawnCount = 16;
    public float spawnDistance = 5;



    private List<GameObject> spawnPoints;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        mf = GetComponent<MeshFilter>();
        mc = gameObject.AddComponent<MeshCollider>();

        mesh = new Mesh();
        mf.mesh = mesh;

        center = new Vector3(xSize / 2, 0, zSize / 2);

        globalMinY = float.MaxValue;

        CreateMap();
    }

    public void CreateMap()
    {
        CreateMesh();
        UpdateMesh();
        PopulateMap();
        CreateSpawnPoints();
    }

    private void CreateMesh()
    {

        /*
         * Create the vertices for each point in the grid, using Perlin noise for height values
         * Also populate the min-heap with height values for water POI selection
         */
        verts = new Vector3[(xSize + 1) * (zSize + 1)];
        minHeights = new PriorityQueue<Vector3, float>();
        float seed = Random.Range(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f)); // Generate a random seed for noise generation. Will change this for something else later
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = Mathf.PerlinNoise((x + seed) * frequency, (z + seed) * frequency) * amplitude; // Set the first octave of perlin noise
                // Add additional octaves
                for (int o = 1; o < octaves; o++)
                {
                    float freq = frequency * (o + frequencyModifier);
                    float amp = amplitude / (o + amplitudeModifier);
                    y += Mathf.PerlinNoise((x + seed) * freq, (z + seed) * freq) * amp;
                }
                y += HeightMask(x, z); // Apply height mask for hill/ring formation

                bool inBounds = x > border && x < xSize - border && z > border && z < zSize - border; // Checking if POI is inside the bounds of the map
                if (inBounds && y < maxWaterLevel)
                {
                    minHeights.Enqueue(new Vector3(x, y, z), y); // Add to min-heap for water POI selection
                }

                verts[i] = new Vector3(x, y, z);
                i++;
            }
        }

        /*
         * Create triangles for the mesh based on the vertices
         */
        tris = new int[xSize * zSize * 6];
        for (int t = 0, v = 0, z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                tris[t + 0] = v + 0;
                tris[t + 1] = v + xSize + 1;
                tris[t + 2] = v + 1;

                tris[t + 3] = v + 1;
                tris[t + 4] = v + xSize + 1;
                tris[t + 5] = v + xSize + 2;
                t += 6;
                v++;
            }
            v++;
        }

       
    }

    private void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mc.sharedMesh = mesh;
    }

    private void PopulateMap()
    { 
        /*
         * Find water POIs by selecting the lowest height values from the min-heap while ensuring they are not too close to each other and in bounds of the map.
         * Then marking pool areas around those POIs.
         */
        if (waterSurfaces != null)
        {
            for (int i = 0; i < waterSurfaces.Count; i++)
            {
                Destroy(waterSurfaces[i].gameObject);
            }
        }
        waterSurfaces = new List<GameObject>();
        waterCenters = new List<Vector3>();

        int attempts = 0;
        while (waterSurfaces.Count < waterPOICount && minHeights.Count > 0 && attempts++ < 10000)
        {
            Vector3 nextMin = minHeights.Dequeue();
            if (CheckProximity(nextMin)) continue;
            CreateWaterMeshSurface(nextMin);
        }

        /*
         * Find obstacle POIs by selecting random locations on the map that are not too close to water POIs or each other.
         */
        if (obstacles != null)
        {
            for (int i = 0; i < obstacles.Count; i++)
            {
                Destroy(obstacles[i].gameObject);
            }
        }
        attempts = 0;
        obstacles = new List<GameObject>();
        while (obstacles.Count < obstacleCount && attempts++ < 10000)
        {
            GenerateObstaclePOI();
        }

        /*
         * Create forest patches by generating a noise map and placing trees based on a percentage of coverage and density.
         */
        if (trees != null)
        {
            for (int i = 0; i < trees.Count; i++)
            {
                Destroy(trees[i].gameObject);
            }
        }
        trees = new List<GameObject>();
        CreateForests();
    }

    private void GenerateObstaclePOI()
    {
        int ox = Random.Range(border, xSize - border);
        int oz = Random.Range(border, zSize - border);

        Vector3 obstaclePos = new Vector3(ox, verts[GetIndexAtLocation(ox, oz)].y, oz);

        if (CheckProximity(obstaclePos) == false)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Rock_" + obstacles.Count;
            obstacle.transform.position = obstaclePos + new Vector3(0, 0.5f, 0);
            obstacle.transform.rotation = mesh.normals[GetIndexAtLocation(ox, oz)] != Vector3.up ? Quaternion.FromToRotation(Vector3.up, mesh.normals[GetIndexAtLocation(ox, oz)]) : Quaternion.identity;
            obstacles.Add(obstacle);
        }
    }

    private void CreateSpawnPoints()
    {
        spawnPoints = new List<GameObject>();
        while (spawnPoints.Count < spawnCount)
        {
            int randomX = Random.Range(border, xSize - border);
            int randomZ = Random.Range(border, zSize - border);
            Vector3 spawnPos = new Vector3(randomX, verts[GetIndexAtLocation(randomX, randomZ)].y, randomZ);

            if (CheckProximity(spawnPos) == false)
            {
                GameObject sp = new GameObject();
                sp.name = "SpawnPoint_" + spawnPoints.Count;
                sp.transform.position = spawnPos;
                sp.transform.position += Vector3.up * 0.2f;

                Vector3 centerDir = center - sp.transform.position;
                centerDir.y = 0;

                sp.transform.rotation = Quaternion.LookRotation(centerDir.normalized, Vector3.up);
                sp.transform.rotation *= Quaternion.Euler(0, Random.Range(-15f, 15f), 0);
                spawnPoints.Add(sp);
            }
        }
    }

    private void CreateWaterMeshSurface(Vector3 lowPoint)
    {
        int sx = Mathf.RoundToInt(lowPoint.x);
        int sz = Mathf.RoundToInt(lowPoint.z);

        float waterLevel = lowPoint.y + waterHeight;

        bool[,] basin = FloodFillBasinVertices(sx, sz, waterLevel);

        Mesh waterMesh = WaterMarchingSquares.BuildWaterMesh(xSize, zSize, basin, (x, z) => verts[GetIndexAtLocation(x, z)].y, waterLevel - waterSink);

        if (waterMesh == null)
            return;

        GameObject water = new GameObject($"Water_{waterSurfaces.Count}");
        water.transform.position = Vector3.zero;
        water.transform.rotation = Quaternion.identity;

        MeshFilter mfWater = water.AddComponent<MeshFilter>();
        MeshRenderer mrWater = water.AddComponent<MeshRenderer>();
        MeshCollider mcWater = water.AddComponent<MeshCollider>();

        mfWater.sharedMesh = waterMesh;
        mrWater.sharedMaterial = waterMat;

        mcWater.sharedMesh = waterMesh;
        mcWater.convex = true;
        mcWater.isTrigger = true;

        waterSurfaces.Add(water);
        waterCenters.Add(new Vector3(lowPoint.x, 0f, lowPoint.z));
    }

    private void CreateForests()
    {
        for (int x = border; x < xSize - border; x++)
        {
            for (int z = border; z < zSize - border; z++)
            {
                float noiseValue = Mathf.PerlinNoise(x * frequency, z * frequency);
                Debug.Log(noiseValue);
                if (noiseValue < 1 - coverage) continue;
                if (Random.Range(0f, 1f) < density)
                {
                    Vector3 treePos = new Vector3(x + Random.Range(-0.5f, 0.5f), verts[GetIndexAtLocation(x, z)].y + Random.Range(-0.3f, 0f), z + Random.Range(-0.5f, 0.5f));
                    GameObject treePrefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                    GameObject tree = Instantiate(treePrefab, treePos, Quaternion.identity);
                    tree.name = "Tree_" + trees.Count;
                    trees.Add(tree);
                }
            }
        }
    }

    private bool[,] FloodFillBasinVertices(int sx, int sz, float waterLevel)
    {
        bool[,] basin = new bool[xSize + 1, zSize + 1];
        bool[,] visited = new bool[xSize + 1, zSize + 1];

        Queue<(int x, int z)> q = new Queue<(int x, int z)>();
        q.Enqueue((sx, sz));
        visited[sx, sz] = true;

        while (q.Count > 0)
        {
            var (x, z) = q.Dequeue();

            float h = verts[GetIndexAtLocation(x, z)].y;
            if (h >= waterLevel) continue;

            basin[x, z] = true;

            TryEnqueue(x + 1, z);
            TryEnqueue(x - 1, z);
            TryEnqueue(x, z + 1);
            TryEnqueue(x, z - 1);

        }

        int borderHits = 0;
        for (int z = 0; z <= zSize; z++) if (basin[0, z] || basin[xSize, z]) borderHits++;
        for (int x = 0; x <= xSize; x++) if (basin[x, 0] || basin[x, zSize]) borderHits++;


        return basin;

        void TryEnqueue(int x, int z)
        {
            if (x < 0 || x > xSize || z < 0 || z > zSize) return;
            if (visited[x, z]) return;
            visited[x, z] = true;
            q.Enqueue((x, z));
        }
    }



    private bool CheckProximity(Vector3 point)
    {
        bool tooClose = false;
        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (Vector3.Distance(point, spawnPoints[i].transform.position) < spawnDistance || Vector3.Distance(point, center) < 5) // Ensure spawn point is not too close to other spawn points
                {
                    tooClose = true;
                    break;
                }
            }
        }
        if (waterCenters != null && waterCenters.Count > 0)
        {
            for (int i = 0; i < waterCenters.Count; i++)
            {
                Vector3 c = waterCenters[i];
                c.y = point.y;
                if (Vector3.Distance(point, c) < waterDist) // Ensure spawn point is not too close to water POIs
                {
                    tooClose = true;
                    break;
                }
            }
        }
        if (obstacles != null && obstacles.Count > 0)
        {
            for (int i = 0; i < obstacles.Count; i++)
            {
                if (Vector3.Distance(point, obstacles[i].transform.position) < obstacleDist) // Ensure spawn point is not too close to obstacles
                {
                    tooClose = true;
                    break;
                }
            }
        }
        return tooClose;
    }

    private int GetIndexAtLocation(int x, int z)
    {
        int index = z * (xSize + 1) + x;
        return index;
    }

    private float HeightMask(int x, int z)
    {
        float nx = (x / (float)xSize) * 2f - 1f;
        float nz = (z / (float)zSize) * 2f - 1f;

        nx -= ringCenterOffset.x;
        nz -= ringCenterOffset.y;

        float r = Mathf.Sqrt(nx * nx + nz * nz);
        float halfWidth = Mathf.Max(0.0001f, ringWidth * 0.5f);
        float t = Mathf.Abs(r - ringRadius) / halfWidth;

        float ridge = 1f - SmoothStep01(t);
        ridge = Mathf.Pow(Mathf.Clamp01(ridge), ringPower);

        return ridge * ringHeight;
    }

    private float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
