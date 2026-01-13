using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using UnityEngine;

public class RelayConnectionManager : MonoBehaviour
{
    [SerializeField] private UnityTransport transport;

    private async Task EnsureServicesReadyAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async Task<string> HostAsync(int maxPlayers)
    {
        await EnsureServicesReadyAsync();

        int maxPeers = Mathf.Clamp(maxPlayers - 1, 1, 15);
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPeers);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
        transport.SetRelayServerData(relayServerData);

        bool ok = NetworkManager.Singleton.StartHost();
        if (!ok) throw new Exception("StartHost failed.");

        return joinCode;
    }

    public async Task JoinAsync(string joinCode)
    {
        await EnsureServicesReadyAsync();

        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
        transport.SetRelayServerData(relayServerData);

        bool ok = NetworkManager.Singleton.StartClient();
        if (!ok) throw new Exception("StartClient failed.");
    }
}
