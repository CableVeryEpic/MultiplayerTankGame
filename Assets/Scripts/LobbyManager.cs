using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance {  get; private set; }

    public Lobby CurrentLobby { get; private set; }

    public const string KEY_NAME = "name";
    public const string KEY_READY = "ready";
    public const string KEY_TANK = "tank";

    public string PlayerId => AuthenticationService.Instance.PlayerId;
    public bool IsHost => CurrentLobby != null && CurrentLobby.HostId == PlayerId;
    public int PlayerCount => CurrentLobby?.Players?.Count ?? 0;
    public int LobbyMaxPlayers => CurrentLobby?.MaxPlayers ?? 0;

    public event Action<Lobby> OnLobbyUpdated;
    public event Action<string> OnLobbyError;

    [Header("Polling/Heartbeat")]
    [SerializeField] private float pollIntervalSeconds = 1.5f;
    [SerializeField] private float heartbeatIntervalSeconds = 15f;

    private CancellationTokenSource pollCts;
    private CancellationTokenSource heartbeatCts;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            await EnsureUgsReadyAsync();
        }
        catch (Exception e)
        {
            OnLobbyError?.Invoke($"UGS init/auth failed: {e.Message}");
        }
    }

    private static async Task EnsureUgsReadyAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async Task<string> CreateLobbyAsync(string lobbyName, int maxPlayers, string displayName)
    {
        try
        {
            await EnsureUgsReadyAsync();

            var playerData = new Dictionary<string, PlayerDataObject>
            {
                [KEY_NAME] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, displayName),
                [KEY_READY] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0"),
                [KEY_TANK] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0")
            };

            var options = new CreateLobbyOptions
            {
                IsPrivate = true,
                Player = new Player(id: PlayerId, data: playerData),
                Data = new Dictionary<string, DataObject>()
            };

            CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            StartPolling();
            StartHeartbeat();

            OnLobbyUpdated?.Invoke(CurrentLobby);
            return CurrentLobby.LobbyCode; 
        }
        catch (LobbyServiceException e)
        {
            OnLobbyError?.Invoke($"CreateLobby failed: {e.Message}");
            throw;
        }
    }

    public async Task JoinLobbyByCodeAsync(string lobbyCode, string displayName)
    {
        try
        {
            await EnsureUgsReadyAsync();

            var playerData = new Dictionary<string, PlayerDataObject>
            {
                [KEY_NAME] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, displayName),
                [KEY_READY] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0"),
                [KEY_TANK] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0")
            };

            var options = new JoinLobbyByCodeOptions
            {
                Player = new Player(id: PlayerId, data: playerData)
            };

            CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);

            StartPolling();
            StopHeartbeat();

            OnLobbyUpdated?.Invoke(CurrentLobby);
        }
        catch (LobbyServiceException e)
        {
            OnLobbyError?.Invoke($"JoinLobby failed: {e.Message}");
            throw;
        }
    }

    public async Task SetLobbyDataAsync(Dictionary<string, DataObject> data)
    {
        if (CurrentLobby == null) return;

        try
        {
            var options = new UpdateLobbyOptions { Data = data };
            CurrentLobby = await LobbyService.Instance.UpdateLobbyAsync(CurrentLobby.Id, options);
            OnLobbyUpdated?.Invoke(CurrentLobby);
        }
        catch (LobbyServiceException e)
        {
            OnLobbyError?.Invoke($"UpdateLobby failed: {e.Message}");
            throw;
        }
    }

    public async Task SetPlayerDataAsync(Dictionary<string, PlayerDataObject> data)
    {
        if (CurrentLobby == null) return;

        try
        {
            var options = new UpdatePlayerOptions { Data = data };
            CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(CurrentLobby.Id, PlayerId, options);
            OnLobbyUpdated?.Invoke(CurrentLobby);
        }
        catch (LobbyServiceException e)
        {
            OnLobbyError?.Invoke($"UpdatePlayer failed: {e.Message}");
            throw;
        }
    }

    public Task SetReadyAsync(bool ready)
    {
        return SetPlayerDataAsync(new Dictionary<string, PlayerDataObject>
        {
            [KEY_READY] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ready ? "1" : "0")
        });
    }

    public Task SetTankAsync(int tankId)
    {
        return SetPlayerDataAsync(new Dictionary<string, PlayerDataObject>
        {
            [KEY_TANK] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, tankId.ToString())
        });
    }

    private void StartHeartbeat()
    {
        StopHeartbeat();
        heartbeatCts = new CancellationTokenSource();
        _ = HeartbeatLoopAsync(heartbeatCts.Token);
    }

    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && CurrentLobby != null)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                OnLobbyError?.Invoke($"Heartbeat failed: {e.Message}");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(heartbeatIntervalSeconds), ct); }
            catch { }
        }
    }

    private void StopHeartbeat()
    {
        heartbeatCts?.Cancel();
        heartbeatCts?.Dispose();
        heartbeatCts = null;
    }

    private void StartPolling()
    {
        StopPolling();
        pollCts = new CancellationTokenSource();
        _ = PollLoopAsync(pollCts.Token);
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && CurrentLobby != null)
        {
            try
            {
                CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);
                OnLobbyUpdated?.Invoke(CurrentLobby);
            }
            catch (LobbyServiceException e)
            {
                OnLobbyError?.Invoke($"Poll failed: {e.Message}");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), ct); }
            catch { }
        }
    }

    private void StopPolling()
    {
        pollCts?.Cancel();
        pollCts?.Dispose();
        pollCts = null;
    }

    private void OnDestroy()
    {
        StopHeartbeat();
        StopPolling();
    }

    public bool IsPlayerReady(Player p)
    {
        if (p?.Data == null) return false;
        return p.Data.TryGetValue(KEY_READY, out var v) && v.Value == "1";
    }

    public bool AreAllPlayersReady()
    {
        if (CurrentLobby?.Players == null) return false;
        if (CurrentLobby.Players.Count < 1) return false;

        foreach (var p in CurrentLobby.Players)
            if (!IsPlayerReady(p)) return false;

        return true;
    }

    public string GetPlayerDataString(Player p, string key, string fallback)
    {
        if (p?.Data == null) return fallback;
        return p.Data.TryGetValue(key, out var obj) ? obj.Value : fallback;
    }

    public string GetLobbyDataString(string key)
    {
        if (CurrentLobby?.Data == null) return null;
        return CurrentLobby.Data.TryGetValue(key, out var v) ? v.Value : null;
    }

}
