using TMPro;
using UnityEngine;
#if MIRROR
using Mirror;
using Neo.Network;
#endif

namespace Neo.Samples
{
    /// <summary>
    ///     Interactive smoke demo for Neo.Network: Host / Client / Stop buttons drive the scene's
    ///     <c>NeoNetworkManager</c> (Mirror) and the readout shows the live server/client state.
    ///     Without a second instance the client button simply fails to connect — that is expected
    ///     and logged; hosting inside the editor is the one-click path.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Network Demo")]
    public sealed class NetworkDemoController : MonoBehaviour
    {
        private NeoDemoShell.Context _shell;
        private TMP_Text _modeValue;
        private TMP_Text _addressValue;
        private TMP_Text _connectionsValue;

        private void Start()
        {
            _shell = NeoDemoShell.Build("Neo.Network", new Color(0.30f, 0.78f, 0.95f));

            _modeValue = _shell.AddValueLabel("Mode");
            _addressValue = _shell.AddValueLabel("Address");
            _connectionsValue = _shell.AddValueLabel("Connections");

#if MIRROR
            _shell.AddButtonRow(
                ("Start Host", StartHost),
                ("Start Client", StartClient),
                ("Stop", StopAll));
            _shell.Log("NeoNetworkManager ready — Start Host to run server+client in one editor");
#else
            _shell.Log("Mirror is not installed — network demo is inactive");
#endif
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

#if MIRROR
        private static NetworkManager Manager => NetworkManager.singleton;

        private void StartHost()
        {
            if (Manager == null)
            {
                _shell.Log("No NetworkManager in scene");
                return;
            }

            Manager.StartHost();
            _shell.Log("NetworkManager.StartHost() — server + local client");
        }

        private void StartClient()
        {
            if (Manager == null)
            {
                return;
            }

            Manager.StartClient();
            _shell.Log($"NetworkManager.StartClient() → {Manager.networkAddress}");
        }

        private void StopAll()
        {
            if (Manager == null)
            {
                return;
            }

            if (NetworkServer.active && NetworkClient.isConnected)
            {
                Manager.StopHost();
                _shell.Log("NetworkManager.StopHost()");
            }
            else if (NetworkClient.active)
            {
                Manager.StopClient();
                _shell.Log("NetworkManager.StopClient()");
            }
            else if (NetworkServer.active)
            {
                Manager.StopServer();
                _shell.Log("NetworkManager.StopServer()");
            }
        }
#endif

        private void Refresh()
        {
#if MIRROR
            string mode = "Offline";
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                mode = "Host (server + client)";
            }
            else if (NetworkServer.active)
            {
                mode = "Server";
            }
            else if (NetworkClient.isConnected)
            {
                mode = "Client (connected)";
            }
            else if (NetworkClient.active)
            {
                mode = "Client (connecting…)";
            }

            _modeValue.text = mode;
            _addressValue.text = Manager != null ? Manager.networkAddress : "—";
            _connectionsValue.text = NetworkServer.active
                ? NetworkServer.connections.Count.ToString()
                : "—";
#else
            _modeValue.text = "Mirror missing";
            _addressValue.text = "—";
            _connectionsValue.text = "—";
#endif
        }
    }
}
