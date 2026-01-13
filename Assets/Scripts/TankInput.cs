using Unity.Netcode;

public struct TankInput : INetworkSerializable
{
    public sbyte throttle;
    public sbyte turn;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref throttle);
        serializer.SerializeValue(ref turn);
    }

    public bool Equals(TankInput other)
    {
        return throttle == other.throttle && turn == other.turn;
    }

    public override bool Equals(object obj)
    {
        return obj is TankInput other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + throttle.GetHashCode();
            hash = hash * 31 + turn.GetHashCode();
            return hash;
        }
    }

    public static bool operator == (TankInput left, TankInput right) => left.Equals(right);
    public static bool operator != (TankInput left, TankInput right) => !left.Equals(right);

}
