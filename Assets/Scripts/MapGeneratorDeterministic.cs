using UnityEngine;
using Utils;
using System.Collections.Generic;
using System;

public class MapGeneratorDeterministic : MonoBehaviour
{
    public Material waterMat;
    public PhysicsMaterial waterPhysicsMat;
    public GameObject[] treePrefabs;
    public MapConfig testConfig;

    [SerializeField] private bool generateOnStart = true;

    private MapConfig config;

    private Mesh mesh;
    private MeshFilter mf;
    private MeshRenderer mr;
    private MeshCollider mc;

    private Vector3[] verts;
    private int[] tris;

    private float noiseOffsetX, noiseOffsetZ;
    private Vector3 center;

    private List<Vector3> waterCenters;
    private List<GameObject> waterSurfaces;
    private List<GameObject> trees;
    private List<GameObject> obstacles;
    private List<GameObject> spawnPoints;

    private System.Random rng;

    private struct LowCandidate
    {
        public int x, z;
        public float y;
    }
    private List<LowCandidate> lowCandidates;

    private int NextInt(int minInclusive, int maxExclusive) => rng.Next(minInclusive, maxExclusive);
    private float nextFloat01() => (float)rng.NextDouble();
    private float NextRange(float min, float max) => min + (max - min) * nextFloat01();

    private void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        mf = GetComponent<MeshFilter>();
        mc = gameObject.AddComponent<MeshCollider>();
        if (mc == null) mc = gameObject.AddComponent<MeshCollider>();

        mesh = new Mesh();
        mf.sharedMesh = mesh;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (generateOnStart) Generate(testConfig);
    }

    public void Generate(MapConfig cfg)
    {
        config = cfg;
        rng = new System.Random(cfg.seed);

        center = new Vector3(config.xSize / 2, 0, config.zSize / 2);

        noiseOffsetX = NextRange(-1000f, 1000f);
        noiseOffsetZ = NextRange(-1000f, 1000f);

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
        verts = new Vector3[(config.xSize + 1) * (config.zSize + 1)];
        lowCandidates = new List<LowCandidate>();
        for (int i = 0, z = 0; z <= config.zSize; z++)
        {
            for (int x = 0; x <= config.xSize; x++)
            {
                float y = Mathf.PerlinNoise((x + noiseOffsetX) * config.frequency, (z + noiseOffsetZ) * config.frequency) * config.amplitude; // Set the first octave of perlin noise
                // Add additional octaves
                for (int o = 1; o < config.octaves; o++)
                {
                    float freq = config.frequency * Mathf.Pow(config.frequencyModifier, o);
                    float amp = config.amplitude * Mathf.Pow(config.amplitudeModifier, -o);
                    y += Mathf.PerlinNoise((x + noiseOffsetX) * freq, (z + noiseOffsetZ) * freq) * amp;
                }
                y += HeightMask(x, z); // Apply height mask for hill/ring formation

                bool inBounds = x > config.border && x < config.xSize - config.border && z > config.border && z < config.zSize - config.border; // Checking if POI is inside the bounds of the map
                if (inBounds && y < config.maxWaterLevel)
                {
                    lowCandidates.Add(new LowCandidate { x = x, z = z, y = y });
                }

                verts[i] = new Vector3(x, y, z);
                i++;
            }
        }

        /*
         * Create triangles for the mesh based on the vertices
         */
        tris = new int[config.xSize * config.zSize * 6];
        for (int t = 0, v = 0, z = 0; z < config.zSize; z++)
        {
            for (int x = 0; x < config.xSize; x++)
            {
                tris[t + 0] = v + 0;
                tris[t + 1] = v + config.xSize + 1;
                tris[t + 2] = v + 1;

                tris[t + 3] = v + 1;
                tris[t + 4] = v + config.xSize + 1;
                tris[t + 5] = v + config.xSize + 2;
                t += 6;
                v++;
            }
            v++;
        }

        lowCandidates.Sort((a, b) =>
        {
            int c = a.y.CompareTo(b.y);
            if (c != 0) return c;
            c = a.x.CompareTo(b.x);
            if (c != 0) return c;
            return a.z.CompareTo(b.z);
        });
       
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

        for (int i = 0; i < lowCandidates.Count && waterSurfaces.Count < config.waterPOICount; i++)
        {
            LowCandidate c = lowCandidates[i];
            Vector3 p = new Vector3(c.x, c.y, c.z);
            if (CheckProximity(p)) continue;
            CreateWaterMeshSurface(p);
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
        int attempts = 0;
        obstacles = new List<GameObject>();
        while (obstacles.Count < config.obstacleCount && attempts++ < 10000)
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
        int ox = NextInt(config.border, config.xSize - config.border);
        int oz = NextInt(config.border, config.zSize - config.border);

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
        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                Destroy(spawnPoints[i].gameObject);
            }
        }
        spawnPoints = new List<GameObject>();
        int attempts = 0;
        while (spawnPoints.Count < config.spawnCount && attempts++ < 10000)
        {
            int randomX = NextInt(config.border, config.xSize - config.border);
            int randomZ = NextInt(config.border, config.zSize - config.border);
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
                sp.transform.rotation *= Quaternion.Euler(0, NextRange(-15f, 15f), 0);
                spawnPoints.Add(sp);
            }
        }
    }

    private void CreateWaterMeshSurface(Vector3 lowPoint)
    {
        int sx = Mathf.RoundToInt(lowPoint.x);
        int sz = Mathf.RoundToInt(lowPoint.z);

        float waterLevel = lowPoint.y + config.waterHeight;

        bool[,] basin = FloodFillBasinVertices(sx, sz, waterLevel);

        Mesh waterMesh = WaterMarchingSquares.BuildWaterMesh(config.xSize, config.zSize, basin, (x, z) => verts[GetIndexAtLocation(x, z)].y, waterLevel - config.waterSink);

        if (waterMesh == null)
            return;

        GameObject water = new GameObject($"Water_{waterSurfaces.Count}");
        water.transform.position = Vector3.zero;
        water.transform.rotation = Quaternion.identity;

        MeshFilter mfWater = water.AddComponent<MeshFilter>();
        MeshRenderer mrWater = water.AddComponent<MeshRenderer>();
        MeshCollider mcWater = water.AddComponent<MeshCollider>();
        water.AddComponent<WaterZone>();

        mfWater.sharedMesh = waterMesh;
        mrWater.sharedMaterial = waterMat;

        mcWater.sharedMesh = waterMesh;
        mcWater.material = waterPhysicsMat;
        mcWater.convex = true;
        mcWater.isTrigger = true;

        waterSurfaces.Add(water);
        waterCenters.Add(new Vector3(lowPoint.x, 0f, lowPoint.z));
    }

    private void CreateForests()
    {
        for (int x = config.border; x < config.xSize - config.border; x++)
        {
            for (int z = config.border; z < config.zSize - config.border; z++)
            {
                float noiseValue = Mathf.PerlinNoise((x + noiseOffsetX) * config.frequency, (z + noiseOffsetZ) * config.frequency);
                if (noiseValue < 1 - config.coverage) continue;
                if (nextFloat01() < config.density)
                {
                    float jx = NextRange(-0.5f, 0.5f);
                    float jz = NextRange(-0.5f, 0.5f);
                    float jy = NextRange(-0.3f, 0.5f);

                    int prefabIndex = NextInt(0, treePrefabs.Length);

                    Vector3 treePos = new Vector3(x + jx, verts[GetIndexAtLocation(x, z)].y + jy, z + jz);
                    GameObject treePrefab = treePrefabs[prefabIndex];
                    GameObject tree = Instantiate(treePrefab, treePos, Quaternion.identity);
                    tree.name = "Tree_" + trees.Count;
                    trees.Add(tree);
                }
            }
        }
    }

    private bool[,] FloodFillBasinVertices(int sx, int sz, float waterLevel)
    {
        bool[,] basin = new bool[config.xSize + 1, config.zSize + 1];
        bool[,] visited = new bool[config.xSize + 1, config.zSize + 1];

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
        for (int z = 0; z <= config.zSize; z++) if (basin[0, z] || basin[config.xSize, z]) borderHits++;
        for (int x = 0; x <= config.xSize; x++) if (basin[x, 0] || basin[x, config.zSize]) borderHits++;


        return basin;

        void TryEnqueue(int x, int z)
        {
            if (x < 0 || x > config.xSize || z < 0 || z > config.zSize) return;
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
                if (Vector3.Distance(point, spawnPoints[i].transform.position) < config.spawnDist || Vector3.Distance(point, center) < 5) // Ensure spawn point is not too close to other spawn points
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
                if (Vector3.Distance(point, c) < config.waterDist) // Ensure spawn point is not too close to water POIs
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
                if (Vector3.Distance(point, obstacles[i].transform.position) < config.obstacleDist) // Ensure spawn point is not too close to obstacles
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
        int index = z * (config.xSize + 1) + x;
        return index;
    }

    private float HeightMask(int x, int z)
    {
        float nx = (x / (float)config.xSize) * 2f - 1f;
        float nz = (z / (float)config.zSize) * 2f - 1f;

        nx -= config.ringCenterOffset.x;
        nz -= config.ringCenterOffset.y;

        float r = Mathf.Sqrt(nx * nx + nz * nz);
        float halfWidth = Mathf.Max(0.0001f, config.ringWidth * 0.5f);
        float t = Mathf.Abs(r - config.ringRadius) / halfWidth;

        float ridge = 1f - SmoothStep01(t);
        ridge = Mathf.Pow(Mathf.Clamp01(ridge), config.ringPower);

        return ridge * config.ringHeight;
    }

    private float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
