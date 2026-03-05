using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies.Models;

public class MatchStartCoordinator : MonoBehaviour
{
    public static MatchStartCoordinator Instance { get; private set; }

    [SerializeField] private RelayConnectionManager relay;
    [SerializeField] private MapConfig matchMapConfig;
    [SerializeField] private int maxPlayers = 16;
    [SerializeField] private float waitForClientsTime = 20f;

    private const string KEY_PHASE = "phase";
    private const string KEY_RELAY = "relayCode";

    private const string PHASE_LOBBY = "lobby";
    private const string PHASE_STARTING = "starting";
    private const string PHASE_INGAME = "inGame";

    private bool startedClientFromRelay;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(BindLobby());
    }

    private System.Collections.IEnumerator BindLobby()
    {
        while (LobbyManager.Instance == null)
            yield return null;

        LobbyManager.Instance.OnLobbyUpdated += HandleLobbyUpdated;
    }

    private void OnDisable()
    {
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.OnLobbyUpdated -= HandleLobbyUpdated;
    }

    public async void HostStartMatch()
    {
        if (LobbyManager.Instance == null || relay == null) return;
        if (!LobbyManager.Instance.IsHost) return;

        try
        {
            string relayCode = await relay.HostAsync(maxPlayers);

            Dictionary<string, DataObject> data = new Dictionary<string, DataObject>
            {
                [KEY_RELAY] = new DataObject(DataObject.VisibilityOptions.Member, relayCode),
                [KEY_PHASE] = new DataObject(DataObject.VisibilityOptions.Member, PHASE_LOBBY)
            };
            await LobbyManager.Instance.SetLobbyDataAsync(data);

            int expectedPlayers = LobbyManager.Instance.CurrentLobby?.Players?.Count ?? 1;
            bool ok = await WaitForNgoClients(expectedPlayers, waitForClientsTime);

            if (!ok)
            {
                Debug.LogWarning($"Timed out waiting for clients. Expected {expectedPlayers}, connected {NetworkManager.Singleton.ConnectedClientsIds.Count}");
            }

            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogError($"HostStartMatch failed: {e.Message}");
        }
    }

    private async Task<bool> WaitForNgoClients(int expectedPlayers, float timeoutSeconds)
    {
        if (!NetworkManager.Singleton.IsServer) return false;

        float start = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - start < timeoutSeconds)
        {
            int connected = NetworkManager.Singleton.ConnectedClientsIds.Count;
            if (connected >= expectedPlayers)
                return true;

            await Task.Delay(500);
        }

        return false;
    }

    private void HandleLobbyUpdated(Lobby lobby)
    {
        if (lobby == null) return;

        if (!LobbyManager.Instance.IsHost && !NetworkManager.Singleton.IsClient && !startedClientFromRelay)
        {
            if (lobby.Data != null && lobby.Data.TryGetValue(KEY_RELAY, out var relayObj) && !string.IsNullOrWhiteSpace(relayObj.Value))
            {
                startedClientFromRelay = true;
                _ = JoinRelayAndStartClient(relayObj.Value);
            }
        }
    }

    private async Task JoinRelayAndStartClient(string relayCode)
    {
        try
        {
            await relay.JoinAsync(relayCode);
        }
        catch (Exception e)
        {
            Debug.LogError($"JoinRelayAndStartClient failed: {e.Message}");
            startedClientFromRelay = false;
        }
    }

    private void TryLoadGameWhenReady()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
}
