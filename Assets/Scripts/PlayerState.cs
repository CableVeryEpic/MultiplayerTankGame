using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PlayerState : NetworkBehaviour
{
    [SerializeField] private NetworkObject tankPrefab;

    private NetworkObject spawnedTank;
    private bool spawnPending;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;

            if (SceneManager.GetActiveScene().name == "Game")
            {
                TrySpawnTank();
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        }
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;
        if (sceneName != "Game") return;
        Debug.Log("LoadEventCompleted");
        TrySpawnTank();
    }

    private void TrySpawnTank()
    {
        if (!IsServer) return;
        if (spawnPending) return;
        if (spawnedTank != null && spawnedTank.IsSpawned) return;
        Debug.Log("Spawning Tank");
        spawnPending = true;
        StartCoroutine(SpawnTankRoutine());

    }

    private System.Collections.IEnumerator SpawnTankRoutine()
    {
        while (spawnedTank == null || !spawnedTank.IsSpawned)
        {
            if (SpawnService.Instance != null && SpawnService.Instance.TryGetSpawn(out SpawnPoint sp))
            {
                Vector3 pos = sp.transform.position;
                Quaternion rot = sp.transform.rotation;

                spawnedTank = Instantiate(tankPrefab, pos, rot);
                spawnedTank.SpawnWithOwnership(OwnerClientId);

                break;
            }

            yield return new WaitForSeconds(2);
        }
        spawnPending = false;
    }
}
