using UnityEngine;

[CreateAssetMenu(menuName = "Tanks/Tank Database")]
public class TankDatabase : ScriptableObject
{
    public TankDefinition[] tanks;
    public TankDefinition GetById(int id)
    {
        foreach (var tank in tanks)
        {
            if (tank != null && tank.id == id)
                return tank;
        }
        return null;
    }
}
