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

    [Header("Hill Parameters")]
    public float ringRadius = 25f;
    public float ringWidth = 10f;
    public float ringHeight = 2f;
    public float ringPower = 2f;
    public Vector2 ringCenterOffset = Vector2.zero;

    [Header("POI Parameters")]
    public int POICount = 5;
    public int border = 5;
    public float waterDist = 5;
    public float waterHeight = 0.5f;

    private List<List<Vector3>> waterPOIs;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        mf = GetComponent<MeshFilter>();
        mc = gameObject.AddComponent<MeshCollider>();

        mesh = new Mesh();
        mf.mesh = mesh;

        CreateMap();
    }

    public void CreateMap()
    {
        CreateMesh();
        UpdateMesh();
    }

    private void CreateMesh()
    {
        PriorityQueue<Vector3, float> minHeights = new PriorityQueue<Vector3, float>();
        verts = new Vector3[(xSize + 1) * (zSize + 1)];
        float seed = Random.Range(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f));
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = Mathf.PerlinNoise((x + seed) * frequency, (z + seed) * frequency) * amplitude;
                for (int o = 1; o < octaves; o++)
                {
                    float freq = frequency * (o + frequencyModifier);
                    float amp = amplitude / (o + amplitudeModifier);
                    y += Mathf.PerlinNoise((x + seed) * freq, (z + seed) * freq) * amp;
                }
                y += HeightMask(x, z);
                minHeights.Enqueue(new Vector3(x, y, z), y);
                verts[i] = new Vector3(x, y, z);
                i++;
            }
        }

        waterPOIs = new List<List<Vector3>>();
        while (waterPOIs.Count < POICount && minHeights.Count > 0)
        {
            Vector3 nextMin = minHeights.Dequeue();
            bool tooClose = false;
            for (int i = 0; i < waterPOIs.Count; i++)
            {
                bool inBounds = nextMin.x > border && nextMin.x < xSize - border && nextMin.z > border && nextMin.z < zSize - border;
                if (i + 1 > waterPOIs.Count || Vector3.Distance(nextMin, waterPOIs[i][0]) < waterDist || !inBounds)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose)
            {
                List<Vector3> newPOI = MarkPool(nextMin);
                waterPOIs.Add(newPOI);
            }
        }

        tris = new int[xSize * zSize * 6];
        for (int t = 0, v = 0, z = 0; z < xSize; z++)
        {
            for (int x = 0; x < zSize; x++)
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

    private List<Vector3> MarkPool(Vector3 startPoint) 
    {
        int sx = Mathf.RoundToInt(startPoint.x);
        int sz = Mathf.RoundToInt(startPoint.z);


        float startHeight = startPoint.y;
        float targetHeight = startHeight + waterHeight;

        List<Vector3> newPOI = new List<Vector3>();
        bool[,] visited = new bool[xSize + 1, zSize + 1];

        dfs(newPOI, visited, sx, sz, targetHeight);
        return newPOI;
    }

    private void dfs(List<Vector3> POI, bool[,] visited, int x, int z, float targetHeight)
    {
        if (x < 0 || x >= xSize ||  
            z < 0 || z >= zSize ||
            visited[x, z])
        {
            return;
        }

        float y = verts[GetIndexAtLocation(x, z)].y;
        if (y > targetHeight) return;

        visited[x, z] = true;

        POI.Add(new Vector3(x, y, z));

        dfs(POI, visited, x + 1, z, targetHeight);
        dfs(POI, visited, x - 1, z, targetHeight);
        dfs(POI, visited, x, z + 1, targetHeight);
        dfs(POI, visited, x, z - 1, targetHeight);
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

    private void OnDrawGizmos()
    {
        for (int n = 0; n < waterPOIs.Count; n++)
        {
            for (int k = 0; k < waterPOIs[n].Count; k++)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(waterPOIs[n][k], 0.3f);
            }
        }
    }
}
