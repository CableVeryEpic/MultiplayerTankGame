using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMenuUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField displayNameInput;
    [SerializeField] private TMP_InputField lobbyCodeInput;

    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button startMatchButton;

    [SerializeField] private Toggle readyToggle;

    [SerializeField] private TMP_Dropdown tankSelection;

    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text lobbyCodeText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text playerListText;

    [SerializeField] private string lobbyName = "TankLobby";
    [SerializeField] private int maxPlayers = 16;

    private void Awake()
    {
        createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        joinLobbyButton.onClick.AddListener(OnJoinLobbyClicked);

        readyToggle.onValueChanged.AddListener(OnReadyToggle);
        tankSelection.onValueChanged.AddListener(OnTankChanged);

        if (startMatchButton != null)
            startMatchButton.onClick.AddListener(OnStartMatchClicked);

        readyToggle.gameObject.SetActive(false);
        startMatchButton.gameObject.SetActive(false);
        tankSelection.gameObject.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(BindLobbyEvents());
    }

    private System.Collections.IEnumerator BindLobbyEvents()
    {
        while (LobbyManager.Instance == null)
            yield return null;

        LobbyManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
        LobbyManager.Instance.OnLobbyError += OnLobbyError;

        if (LobbyManager.Instance.CurrentLobby != null)
            OnLobbyUpdated(LobbyManager.Instance.CurrentLobby);
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
            LobbyManager.Instance.OnLobbyError -= OnLobbyError;
        }
    }

    private string GetNameOrDefault()
    {
        string name = (displayNameInput?.text ?? "").Trim();
        return string.IsNullOrWhiteSpace(name) ? "Player" : name;
    }

    private async void OnCreateLobbyClicked()
    {
        statusText.text = "Creating lobby...";
        lobbyCodeText.text = "";

        try
        {
            string code = await LobbyManager.Instance.CreateLobbyAsync(lobbyName, maxPlayers, GetNameOrDefault());
            lobbyCodeText.text = $"Lobby Code: {code}";
            statusText.text = "Lobby created.";
            tankSelection.gameObject.SetActive(true);
            startMatchButton.gameObject.SetActive(LobbyManager.Instance.IsHost);
            readyToggle.gameObject.SetActive(true);
        }
        catch { }
    }

    private async void OnJoinLobbyClicked()
    {
        string code = (lobbyCodeInput?.text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            statusText.text = "Enter a lobby code.";
            return;
        }

        statusText.text = "Joining lobby...";
        lobbyCodeText.text = "";

        try
        {
            await LobbyManager.Instance.JoinLobbyByCodeAsync(code, GetNameOrDefault());
            statusText.text = "Joined lobby.";
            tankSelection.gameObject.SetActive(true);
            readyToggle.gameObject.SetActive(true);
        }
        catch { }
    }

    private async void OnReadyToggle(bool toggle)
    {
        await LobbyManager.Instance.SetReadyAsync(toggle);
    }

    private async void OnTankChanged(int tankId)
    {
        await LobbyManager.Instance.SetTankAsync(tankId);
    }

    private void OnStartMatchClicked()
    {
        if (LobbyManager.Instance == null || !LobbyManager.Instance.IsHost)
        {
            statusText.text = "Only host can start.";
            return;
        }

        statusText.text = "Starting match...";
        MatchStartCoordinator.Instance.HostStartMatch();
    }

    private void OnLobbyUpdated(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        if (lobby == null) return;

        if (!string.IsNullOrWhiteSpace(lobby.LobbyCode))
        {
            lobbyCodeText.text = $"Lobby Code: {lobby.LobbyCode}";
        }

        if (startMatchButton != null)
            startMatchButton.interactable = LobbyManager.Instance.IsHost && LobbyManager.Instance.AreAllPlayersReady();

        var sb = new System.Text.StringBuilder();
        foreach (var p in lobby.Players)
        {
            string name = LobbyManager.Instance.GetPlayerDataString(p, LobbyManager.KEY_NAME, "Player");
            bool ready = LobbyManager.Instance.IsPlayerReady(p);

            sb.Append(ready ? "<color=#00FF00>" : "<color=#FF5555>");
            sb.Append(name);
            sb.Append(" " + (ready ? "[READY]" : "[NOT READY]"));
            sb.Append("</color>");
            sb.AppendLine();
        }
        playerListText.text =sb.ToString();
        playerCountText.text = $"Players: {lobby.Players?.Count ?? 0}/{lobby.MaxPlayers}";
    }

    private void OnLobbyError(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }
}
