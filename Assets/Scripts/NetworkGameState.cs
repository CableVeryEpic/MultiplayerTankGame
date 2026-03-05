using Unity.Netcode;
using UnityEngine;

public class NetworkGameState : NetworkBehaviour
{
    public static NetworkGameState Instance { get; private set; }

    public NetworkVariable<MapConfig> CurrentMap = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        Instance = this;
        CurrentMap.OnValueChanged += OnMapChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentMap.OnValueChanged -= OnMapChanged;

        if (Instance == this) Instance = null;
    }

    private void OnMapChanged(MapConfig oldCfg, MapConfig newCfg)
    {
        GenerateMapClient(newCfg);
    }

    private void GenerateMapClient(MapConfig cfg)
    {
        if (cfg.xSize <= 0 || cfg.zSize <= 0) return;

        var gen = FindAnyObjectByType<MapGeneratorDeterministic>();
        if (gen != null)
            gen.Generate(cfg);
    }

    public void SetMapServer(MapConfig cfg)
    {
        if (!IsServer) return;
        CurrentMap.Value = cfg;
    }
}
