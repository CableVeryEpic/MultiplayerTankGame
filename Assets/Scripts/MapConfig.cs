using UnityEngine;

[System.Serializable]
public struct MapConfig
{
    public int seed;

    public int xSize, zSize;

    public float frequency, amplitude;
    public int octaves;
    public float frequencyModifier, amplitudeModifier;

    public float ringRadius, ringWidth, ringHeight, ringPower;
    public Vector2 ringCenterOffset;

    public int border;

    public int waterPOICount;
    public float waterDist;
    public float waterHeight;
    public float maxWaterLevel;
    public float waterSink;

    public int obstacleCount;
    public float obstacleDist;

    public int spawnCount;
    public float spawnDist;

    public float coverage;
    public float density;
}
