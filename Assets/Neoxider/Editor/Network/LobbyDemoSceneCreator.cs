#if MIRROR && UNITY_EDITOR
using Mirror;
using Mirror.Discovery;
using Neo.Network;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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

        [MenuItem(MenuPath)]
        public static void CreateLobbyDemoScene()
        {
            // 1. Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // ──────────────── Network Manager ────────────────
            var managerObj = new GameObject("--- LOBBY MANAGER ---");
            var lobby = managerObj.AddComponent<NeoLobbyManager>();
            // Add any available transport (KcpTransport is in Mirror.Transports assembly)
            var transportType = System.Type.GetType("kcp2k.KcpTransport, Mirror.Transports");
            if (transportType != null)
                managerObj.AddComponent(transportType);
            else
                Debug.LogWarning("[Neo] KcpTransport not found. Please add a Transport manually.");
            var discovery = managerObj.AddComponent<NetworkDiscovery>();
            var neoDiscovery = managerObj.AddComponent<NeoNetworkDiscovery>();

            // Create Room Player prefab
            var roomPlayerObj = CreateRoomPlayerPrefab();
            lobby.playerPrefab = roomPlayerObj;

            // Configure lobby
            SerializedObject lobbySO = new SerializedObject(lobby);
            lobbySO.FindProperty("_minPlayersToStart").intValue = 1;
            lobbySO.ApplyModifiedPropertiesWithoutUndo();

            // ──────────────── Canvas ────────────────
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // ──── Panel: Connection ────
            var panelObj = CreatePanel(canvasObj.transform, "ConnectionPanel", new Vector2(0, 0));

            // Title
            CreateText(panelObj.transform, "Title", "🎮 Neo Lobby Demo", 36,
                new Vector2(0, 200), new Vector2(500, 60));

            // Status text
            var statusText = CreateText(panelObj.transform, "StatusText", "Disconnected", 20,
                new Vector2(0, 140), new Vector2(400, 40));

            // IP input field
            var ipFieldObj = new GameObject("IPField");
            ipFieldObj.transform.SetParent(panelObj.transform, false);
            var ipRect = ipFieldObj.AddComponent<RectTransform>();
            ipRect.anchoredPosition = new Vector2(0, 60);
            ipRect.sizeDelta = new Vector2(300, 40);
            var ipImage = ipFieldObj.AddComponent<Image>();
            ipImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            var ipField = ipFieldObj.AddComponent<TMP_InputField>();

            // IP field text area
            var textArea = new GameObject("Text Area");
            textArea.transform.SetParent(ipFieldObj.transform, false);
            var taRect = textArea.AddComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.offsetMin = new Vector2(10, 0);
            taRect.offsetMax = new Vector2(-10, 0);

            var placeholder = CreateText(textArea.transform, "Placeholder", "Enter IP (localhost)", 16,
                Vector2.zero, Vector2.zero);
            var phRect = placeholder.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            placeholder.GetComponent<TMP_Text>().color = new Color(1, 1, 1, 0.3f);
            placeholder.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;

            var inputText = CreateText(textArea.transform, "Text", "", 16,
                Vector2.zero, Vector2.zero);
            var itRect = inputText.GetComponent<RectTransform>();
            itRect.anchorMin = Vector2.zero;
            itRect.anchorMax = Vector2.one;
            itRect.offsetMin = Vector2.zero;
            itRect.offsetMax = Vector2.zero;
            inputText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;

            ipField.textViewport = taRect;
            ipField.textComponent = inputText.GetComponent<TMP_Text>();
            ipField.placeholder = placeholder.GetComponent<TMP_Text>();
            ipField.text = "localhost";

            // Host button
            var hostBtn = CreateButton(panelObj.transform, "HostButton", "🖥️ Host Lobby",
                new Vector2(-110, -10), new Vector2(200, 50), new Color(0.1f, 0.6f, 0.3f));
            hostBtn.GetComponent<Button>().onClick.AddListener(() => lobby.HostLobby());

            // Join button
            var joinBtn = CreateButton(panelObj.transform, "JoinButton", "🔗 Join Lobby",
                new Vector2(110, -10), new Vector2(200, 50), new Color(0.2f, 0.4f, 0.8f));

            // Ready button
            var readyBtn = CreateButton(panelObj.transform, "ReadyButton", "✅ Toggle Ready",
                new Vector2(0, -80), new Vector2(300, 50), new Color(0.7f, 0.5f, 0.1f));

            // Leave button
            var leaveBtn = CreateButton(panelObj.transform, "LeaveButton", "🚪 Leave",
                new Vector2(0, -150), new Vector2(200, 50), new Color(0.7f, 0.15f, 0.15f));
            leaveBtn.GetComponent<Button>().onClick.AddListener(() => lobby.LeaveLobby());

            // Player count text
            var playerCountText = CreateText(panelObj.transform, "PlayerCountText", "Players: 0", 22,
                new Vector2(0, -220), new Vector2(300, 40));

            // ──── Wire Events ────
            // OnPlayerCountChanged → update player count text
            var pcTMP = playerCountText.GetComponent<TMP_Text>();
            lobby.OnPlayerCountChanged.AddListener(count =>
            {
                if (pcTMP != null) pcTMP.text = $"Players: {count}";
            });

            // OnAllPlayersReady → update status
            var stTMP = statusText.GetComponent<TMP_Text>();
            lobby.OnAllPlayersReady.AddListener(() =>
            {
                if (stTMP != null) stTMP.text = "All players ready! Starting...";
            });

            // ──── Quick-Join via Discovery ────
            neoDiscovery.OnServerFound.AddListener(address =>
            {
                if (stTMP != null) stTMP.text = $"Found server: {address}";
            });

            // ──── Info Label ────
            CreateText(panelObj.transform, "InfoLabel",
                "Tip: Add NeoLobbyManager to your scene.\n" +
                "Set Room Player Prefab to the prefab with NeoLobbyPlayer.\n" +
                "Wire buttons via Inspector UnityEvents.",
                14, new Vector2(0, -300), new Vector2(500, 80));

            // ──────────────── EventSystem ────────────────
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Mark scene dirty for save
            EditorSceneManager.MarkSceneDirty(scene);

            // Prompt save
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

        // ──────────────── Helpers ────────────────

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
            var rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(600, 700);
            var img = obj.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            return obj;
        }

        private static GameObject CreateButton(Transform parent, string name, string label,
            Vector2 pos, Vector2 size, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = obj.AddComponent<Image>();
            img.color = color;

            obj.AddComponent<Button>();

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            var tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
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
            var rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.9f, 0.9f, 0.95f);

            return obj;
        }
    }
}
#endif
