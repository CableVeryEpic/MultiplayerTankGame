using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RelayMenuUI : MonoBehaviour
{
    [SerializeField] private RelayConnectionManager relay;

    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text codeText;

    [SerializeField] private int maxPlayers = 16;

    void Awake()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    private async void OnHostClicked()
    {
        codeText.text = "Hosting...";
        try
        {
            string code = await relay.HostAsync(maxPlayers);
            codeText.text = $"Join Code: {code}";
        }
        catch (System.Exception e)
        {
            codeText.text = $"Host Failed: {e.Message}";
        }
    }

    private async void OnJoinClicked()
    {
        string code = (joinCodeInput.text ??  "").Trim();
        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "Enter a valid join code.";
            return;
        }

        statusText.text = "Joining...";

        try
        {
            await relay.JoinAsync(code);
            statusText.text = "Joined successfully.";
        }
        catch (System.Exception e)
        {
            statusText.text = $"Join Failed: {e.Message}";
        }
    }
}
