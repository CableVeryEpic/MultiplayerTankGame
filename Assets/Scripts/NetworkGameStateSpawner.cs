using Unity.Netcode;
using UnityEngine;

public class NetworkGameStateSpawner : MonoBehaviour
{
    [SerializeField] private NetworkGameState gameStatePrefab;

    private NetworkGameState spawned;

    void Awake()
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    private void OnServerStarted()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (spawned != null) return;

        spawned = Instantiate(gameStatePrefab);
        spawned.NetworkObject.Spawn();
    }
}
