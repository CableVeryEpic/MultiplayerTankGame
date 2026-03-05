using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct MapConfig : INetworkSerializable, IEquatable<MapConfig>
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

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref seed);
        serializer.SerializeValue(ref xSize);
        serializer.SerializeValue(ref zSize);
        serializer.SerializeValue(ref border);

        serializer.SerializeValue(ref frequency);
        serializer.SerializeValue(ref amplitude);
        serializer.SerializeValue(ref octaves);
        serializer.SerializeValue(ref frequencyModifier);
        serializer.SerializeValue(ref amplitudeModifier);

        serializer.SerializeValue(ref ringRadius);
        serializer.SerializeValue(ref ringWidth);
        serializer.SerializeValue(ref ringHeight);
        serializer.SerializeValue(ref ringPower);
        serializer.SerializeValue(ref ringCenterOffset);

        serializer.SerializeValue(ref waterPOICount);
        serializer.SerializeValue(ref waterDist);
        serializer.SerializeValue(ref waterHeight);
        serializer.SerializeValue(ref maxWaterLevel);
        serializer.SerializeValue(ref waterSink);

        serializer.SerializeValue(ref obstacleCount);
        serializer.SerializeValue(ref obstacleDist);

        serializer.SerializeValue(ref spawnCount);
        serializer.SerializeValue(ref spawnDist);

        serializer.SerializeValue(ref coverage);
        serializer.SerializeValue(ref density);
    }
    public bool Equals(MapConfig other)
    {
        return seed == other.seed
            && xSize == other.xSize && zSize == other.zSize && border == other.border
            && frequency == other.frequency && amplitude == other.amplitude
            && octaves == other.octaves
            && frequencyModifier == other.frequencyModifier
            && amplitudeModifier == other.amplitudeModifier
            && ringRadius == other.ringRadius && ringWidth == other.ringWidth
            && ringHeight == other.ringHeight && ringPower == other.ringPower
            && ringCenterOffset == other.ringCenterOffset
            && waterPOICount == other.waterPOICount
            && waterDist == other.waterDist && waterHeight == other.waterHeight
            && maxWaterLevel == other.maxWaterLevel && waterSink == other.waterSink
            && obstacleCount == other.obstacleCount && obstacleDist == other.obstacleDist
            && spawnCount == other.spawnCount && spawnDist == other.spawnDist
            && coverage == other.coverage && density == other.density;
    }

    public override bool Equals(object obj) => obj is MapConfig other && Equals(other);
    public override int GetHashCode() => seed;
}
