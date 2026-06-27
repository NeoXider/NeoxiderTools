using System;
#if MIRROR
using Mirror;
#endif
using Neo.Network;
using UnityEngine;
using NeoNetworkDiagnostics = Neo.Network.NetworkDiagnostics;

/// <summary>
/// Demo helper: a UI button calls <see cref="StartGame"/>, the client sends
/// <see cref="CmdStartGame"/> to the server, and the server calls
/// <see cref="RpcShowStartPanel"/> on every client.
/// Without Mirror installed, it runs in local solo mode so imported samples
/// do not require the optional networking dependency.
/// </summary>
public class TestStart :
#if MIRROR
    NetworkBehaviour
#else
    MonoBehaviour
#endif
{
    public GameObject startPanel;
    public float time = 3;
    [SerializeField] private bool _logConnectionWarnings;

    /// <summary>Raised on each client after <see cref="RpcShowStartPanel"/> enables the panel.</summary>
    public static event Action OnStartPanelShownClients;

    /// <summary>Raised on each client after the panel auto-hides by timer.</summary>
    public static event Action OnStartPanelHiddenClients;

    /// <summary>Call from a UI button. The client must be connected and ready.</summary>
    public void StartGame()
    {
#if MIRROR
        if (!NetworkClient.active)
        {
            NeoNetworkDiagnostics.LogWarning(
                "[TestStart] StartGame: no active client. Is this running on a dedicated server UI?",
                this,
                _logConnectionWarnings);
            return;
        }

        if (!NetworkClient.ready)
        {
            NeoNetworkDiagnostics.LogWarning(
                "[TestStart] StartGame: client is not ready yet.",
                this,
                _logConnectionWarnings);
            return;
        }

        CmdStartGame();
#else
        ShowStartPanelLocal();
#endif
    }

#if MIRROR
    /// <summary>
    /// Allows a scene object without client authority to relay the demo button action.
    /// For production, prefer an owned player command or server-side validation.
    /// </summary>
    [Command(requiresAuthority = false)]
    private void CmdStartGame()
    {
        RpcShowStartPanel();
    }

    [ClientRpc]
    private void RpcShowStartPanel()
    {
        ShowStartPanelLocal();
    }
#endif

    private void ShowStartPanelLocal()
    {
        if (startPanel != null)
        {
            startPanel.SetActive(true);
            Invoke(nameof(Off), time);
        }

        OnStartPanelShownClients?.Invoke();
    }

    private void Off()
    {
        if (startPanel != null)
        {
            startPanel.SetActive(false);
        }

        OnStartPanelHiddenClients?.Invoke();
    }
}
