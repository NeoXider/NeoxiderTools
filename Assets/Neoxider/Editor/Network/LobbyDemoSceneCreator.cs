#if MIRROR && UNITY_EDITOR
using Mirror;
using Mirror.Discovery;
using Neo.Network;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neo.Editor
{
    /// <summary>
    ///     Editor utility that creates a fully configured Lobby demo scene
    ///     with NeoLobbyManager, NeoLobbyPlayer prefab, NeoNetworkDiscovery,
    ///     and ready-to-use UI (Host/Join/Ready buttons).
    ///     Menu: Neoxider → Network → Create Lobby Demo Scene
    /// </summary>
    public static class LobbyDemoSceneCreator
    {
        private const string MenuPath = "Neoxider/Network/Create Lobby Demo Scene";

        [MenuItem(MenuPath, false, 200)]
        public static void CreateLobbyDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var managerObj = new GameObject("--- LOBBY MANAGER ---");
            NeoLobbyManager lobby = managerObj.AddComponent<NeoLobbyManager>();
            // WHY: Add any available transport (KcpTransport is in Mirror.Transports assembly)
            var transportType = System.Type.GetType("kcp2k.KcpTransport, Mirror.Transports");
            if (transportType != null)
            {
                managerObj.AddComponent(transportType);
            }
            else
            {
                Debug.LogWarning("[Neo] KcpTransport not found. Please add a Transport manually.");
            }

            NetworkDiscovery discovery = managerObj.AddComponent<NetworkDiscovery>();
            NeoNetworkDiscovery neoDiscovery = managerObj.AddComponent<NeoNetworkDiscovery>();

            GameObject roomPlayerObj = CreateRoomPlayerPrefab();
            lobby.playerPrefab = roomPlayerObj;

            var lobbySO = new SerializedObject(lobby);
            lobbySO.FindProperty("_minPlayersToStart").intValue = 1;
            lobbySO.ApplyModifiedPropertiesWithoutUndo();

            var canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject panelObj = CreatePanel(canvasObj.transform, "ConnectionPanel", new Vector2(0, 0));

            CreateText(panelObj.transform, "Title", "🎮 Neo Lobby Demo", 36,
                new Vector2(0, 200), new Vector2(500, 60));

            GameObject statusText = CreateText(panelObj.transform, "StatusText", "Disconnected", 20,
                new Vector2(0, 140), new Vector2(400, 40));

            var ipFieldObj = new GameObject("IPField");
            ipFieldObj.transform.SetParent(panelObj.transform, false);
            RectTransform ipRect = ipFieldObj.AddComponent<RectTransform>();
            ipRect.anchoredPosition = new Vector2(0, 60);
            ipRect.sizeDelta = new Vector2(300, 40);
            Image ipImage = ipFieldObj.AddComponent<Image>();
            ipImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            TMP_InputField ipField = ipFieldObj.AddComponent<TMP_InputField>();

            var textArea = new GameObject("Text Area");
            textArea.transform.SetParent(ipFieldObj.transform, false);
            RectTransform taRect = textArea.AddComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.offsetMin = new Vector2(10, 0);
            taRect.offsetMax = new Vector2(-10, 0);

            GameObject placeholder = CreateText(textArea.transform, "Placeholder", "Enter IP (localhost)", 16,
                Vector2.zero, Vector2.zero);
            RectTransform phRect = placeholder.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            placeholder.GetComponent<TMP_Text>().color = new Color(1, 1, 1, 0.3f);
            placeholder.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;

            GameObject inputText = CreateText(textArea.transform, "Text", "", 16,
                Vector2.zero, Vector2.zero);
            RectTransform itRect = inputText.GetComponent<RectTransform>();
            itRect.anchorMin = Vector2.zero;
            itRect.anchorMax = Vector2.one;
            itRect.offsetMin = Vector2.zero;
            itRect.offsetMax = Vector2.zero;
            inputText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;

            ipField.textViewport = taRect;
            ipField.textComponent = inputText.GetComponent<TMP_Text>();
            ipField.placeholder = placeholder.GetComponent<TMP_Text>();
            ipField.text = "localhost";

            GameObject hostBtn = CreateButton(panelObj.transform, "HostButton", "🖥️ Host Lobby",
                new Vector2(-110, -10), new Vector2(200, 50), new Color(0.1f, 0.6f, 0.3f));
            hostBtn.GetComponent<Button>().onClick.AddListener(() => lobby.HostLobby());

            GameObject joinBtn = CreateButton(panelObj.transform, "JoinButton", "🔗 Join Lobby",
                new Vector2(110, -10), new Vector2(200, 50), new Color(0.2f, 0.4f, 0.8f));

            GameObject readyBtn = CreateButton(panelObj.transform, "ReadyButton", "✅ Toggle Ready",
                new Vector2(0, -80), new Vector2(300, 50), new Color(0.7f, 0.5f, 0.1f));

            GameObject leaveBtn = CreateButton(panelObj.transform, "LeaveButton", "🚪 Leave",
                new Vector2(0, -150), new Vector2(200, 50), new Color(0.7f, 0.15f, 0.15f));
            leaveBtn.GetComponent<Button>().onClick.AddListener(() => lobby.LeaveLobby());

            GameObject playerCountText = CreateText(panelObj.transform, "PlayerCountText", "Players: 0", 22,
                new Vector2(0, -220), new Vector2(300, 40));

            TMP_Text pcTMP = playerCountText.GetComponent<TMP_Text>();
            lobby.OnPlayerCountChanged.AddListener(count =>
            {
                if (pcTMP != null)
                {
                    pcTMP.text = $"Players: {count}";
                }
            });

            TMP_Text stTMP = statusText.GetComponent<TMP_Text>();
            lobby.OnAllPlayersReady.AddListener(() =>
            {
                if (stTMP != null)
                {
                    stTMP.text = "All players ready! Starting...";
                }
            });

            neoDiscovery.OnServerFound.AddListener(address =>
            {
                if (stTMP != null)
                {
                    stTMP.text = $"Found server: {address}";
                }
            });

            CreateText(panelObj.transform, "InfoLabel",
                "Tip: Add NeoLobbyManager to your scene.\n" +
                "Set Room Player Prefab to the prefab with NeoLobbyPlayer.\n" +
                "Wire buttons via Inspector UnityEvents.",
                14, new Vector2(0, -300), new Vector2(500, 80));

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            EditorSceneManager.MarkSceneDirty(scene);

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Lobby Demo Scene", "LobbyDemo", "unity",
                "Choose where to save the lobby demo scene.");

            if (!string.IsNullOrEmpty(path))
            {
                EditorSceneManager.SaveScene(scene, path);
                Debug.Log($"[Neo] Lobby demo scene saved to: {path}");
            }

            Selection.activeGameObject = managerObj;
            Debug.Log("[Neo] Lobby Demo Scene created! Configure Room/Game scenes in NeoLobbyManager inspector.");
        }

        private static GameObject CreateRoomPlayerPrefab()
        {
            var obj = new GameObject("RoomPlayer");
            obj.AddComponent<NetworkIdentity>();
            obj.AddComponent<NeoLobbyPlayer>();
            return obj;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 pos)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(600, 700);
            Image img = obj.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            return obj;
        }

        private static GameObject CreateButton(Transform parent, string name, string label,
            Vector2 pos, Vector2 size, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = color;

            obj.AddComponent<Button>();

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return obj;
        }

        private static GameObject CreateText(Transform parent, string name, string content,
            int fontSize, Vector2 pos, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.9f, 0.9f, 0.95f);

            return obj;
        }
    }
}
#endif
