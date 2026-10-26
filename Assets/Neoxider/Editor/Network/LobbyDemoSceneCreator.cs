#if MIRROR && UNITY_EDITOR
using System;
using System.IO;
using Mirror;
using Mirror.Discovery;
using Neo.Network;
using Neo.Tools;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

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
        private const string SceneTemplateGuid = "6ba1121cb747442992b1c286f8a7d3fa";

        [MenuItem(MenuPath, false, 200)]
        public static void CreateLobbyDemoScene()
        {
            TryCreateLobbyDemoScene(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo, ChooseScenePath);
        }

        private static string ChooseScenePath()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Lobby Demo Scene", "LobbyDemo", "unity",
                "Choose an unused path for the lobby demo scene and its player prefabs.");
            if (!string.IsNullOrEmpty(path) && DestinationExists(path))
            {
                EditorUtility.DisplayDialog("Lobby scene already exists",
                    "Choose an unused scene path. Existing assets are not overwritten.", "OK");
                return "";
            }

            return path;
        }

        internal static bool TryCreateLobbyDemoScene(Func<bool> saveCurrentScenes, Func<string> chooseScenePath,
            NewSceneMode sceneMode = NewSceneMode.Single)
        {
            return TryCreateLobbyDemoScene(saveCurrentScenes, chooseScenePath, sceneMode, null);
        }

        internal static bool TryCreateLobbyDemoScene(Func<bool> saveCurrentScenes, Func<string> chooseScenePath,
            NewSceneMode sceneMode, Action<Scene> beforePopulation)
        {
            if (!saveCurrentScenes())
            {
                return false;
            }

            string path = chooseScenePath();
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            path = path.Replace('\\', '/');
            if (sceneMode != NewSceneMode.Single && sceneMode != NewSceneMode.Additive)
            {
                throw new ArgumentOutOfRangeException(nameof(sceneMode));
            }

            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("/../") ||
                path.Contains("/./") || path.Contains("//") ||
                !string.Equals(Path.GetExtension(path), ".unity", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Choose a new .unity asset inside Assets.", nameof(chooseScenePath));
            }

            if (DestinationExists(path))
            {
                throw new IOException("Refusing to overwrite an existing lobby destination: " + path);
            }

            string parentFolder = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parentFolder))
            {
                throw new DirectoryNotFoundException(parentFolder);
            }

            string templatePath = AssetDatabase.GUIDToAssetPath(SceneTemplateGuid);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(templatePath) == null)
            {
                throw new IOException("The package lobby scene template is unavailable.");
            }

            Scene previousActive = SceneManager.GetActiveScene();
            Object[] previousSelection = Selection.objects;
            string assetFolder = AssetDatabase.GenerateUniqueAssetPath(
                parentFolder + "/" + Path.GetFileNameWithoutExtension(path) + "_Assets");
            string folderGuid = "";
            string lobbyGuid = "";
            Scene scene = default;
            bool copiedLobby = false;
            bool completed = false;
            try
            {
                folderGuid = AssetDatabase.CreateFolder(parentFolder, Path.GetFileName(assetFolder));
                if (string.IsNullOrEmpty(folderGuid))
                {
                    throw new IOException("Could not create the lobby demo asset folder.");
                }

                CopySceneTemplate(templatePath, path);
                copiedLobby = true;
                lobbyGuid = AssetDatabase.AssetPathToGUID(path);
                scene = EditorSceneManager.OpenScene(path,
                    sceneMode == NewSceneMode.Single ? OpenSceneMode.Single : OpenSceneMode.Additive);
                if (SceneManager.GetActiveScene() != scene && !SceneManager.SetActiveScene(scene))
                {
                    throw new InvalidOperationException("Could not activate the owned lobby scene.");
                }

                CreateDefaultSceneObjects();
                beforePopulation?.Invoke(scene);
                completed = PopulateLobbyScene(scene, path, assetFolder, templatePath);
                return completed;
            }
            finally
            {
                if (!completed)
                {
                    try
                    {
                        if (copiedLobby) CloseOwnedScene(scene, path, lobbyGuid);
                    }
                    finally
                    {
                        try
                        {
                            if (copiedLobby && AssetDatabase.AssetPathToGUID(path) == lobbyGuid)
                                AssetDatabase.DeleteAsset(path);
                        }
                        finally
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(folderGuid) &&
                                    AssetDatabase.AssetPathToGUID(assetFolder) == folderGuid)
                                    AssetDatabase.DeleteAsset(assetFolder);
                            }
                            finally
                            {
                                if (previousActive.IsValid() && previousActive.isLoaded)
                                    SceneManager.SetActiveScene(previousActive);
                                Selection.objects = previousSelection;
                            }
                        }
                    }
                }
            }
        }

        private static bool PopulateLobbyScene(Scene scene, string path, string assetFolder, string templatePath)
        {
            GameObject managerObj = new("--- LOBBY MANAGER ---");
            NeoLobbyManager lobby = managerObj.AddComponent<NeoLobbyManager>();
            // WHY: Add any available transport (KcpTransport is in Mirror.Transports assembly)
            Type transportType = Type.GetType("kcp2k.KcpTransport, Mirror.Transports");
            if (transportType != null && typeof(Transport).IsAssignableFrom(transportType))
            {
                lobby.transport = (Transport)managerObj.AddComponent(transportType);
            }
            else
            {
                Debug.LogWarning("[Neo] KcpTransport not found. Please add a Transport manually.");
            }

            NetworkDiscovery discovery = managerObj.AddComponent<NetworkDiscovery>();
            NeoNetworkDiscovery neoDiscovery = managerObj.AddComponent<NeoNetworkDiscovery>();

            GameObject roomPlayerPrefab = CreatePlayerPrefab(assetFolder, true);
            lobby.roomPlayerPrefab = roomPlayerPrefab.GetComponent<NeoLobbyPlayer>();
            lobby.playerPrefab = CreatePlayerPrefab(assetFolder, false);
            lobby.RoomScene = path;
            lobby.offlineScene = path;
            lobby.onlineScene = path;
            lobby.GameplayScene = CreateGameplayScene(assetFolder, templatePath);

            GameObject canvasObj = CreateCanvas();
            NeoLobbyPresenter presenter = canvasObj.AddComponent<NeoLobbyPresenter>();
            presenter.Manager = lobby;

            GameObject panelObj = CreatePanel(canvasObj.transform, "ConnectionPanel", new Vector2(0, 0));

            CreateText(panelObj.transform, "Title", "🎮 Neo Lobby Demo", 36,
                new Vector2(0, 200), new Vector2(500, 60));

            GameObject statusText = CreateText(panelObj.transform, "StatusText", "Disconnected", 20,
                new Vector2(0, 140), new Vector2(400, 40));

            GameObject ipFieldObj = new("IPField");
            ipFieldObj.transform.SetParent(panelObj.transform, false);
            RectTransform ipRect = ipFieldObj.AddComponent<RectTransform>();
            ipRect.anchoredPosition = new Vector2(0, 60);
            ipRect.sizeDelta = new Vector2(300, 40);
            Image ipImage = ipFieldObj.AddComponent<Image>();
            ipImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            TMP_InputField ipField = ipFieldObj.AddComponent<TMP_InputField>();

            GameObject textArea = new("Text Area");
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
            UnityEventTools.AddPersistentListener(hostBtn.GetComponent<Button>().onClick, presenter.HostLobby);

            GameObject joinBtn = CreateButton(panelObj.transform, "JoinButton", "🔗 Join Lobby",
                new Vector2(110, -10), new Vector2(200, 50), new Color(0.2f, 0.4f, 0.8f));
            UnityEventTools.AddPersistentListener(joinBtn.GetComponent<Button>().onClick, presenter.JoinLobby);

            GameObject readyBtn = CreateButton(panelObj.transform, "ReadyButton", "✅ Toggle Ready",
                new Vector2(0, -80), new Vector2(300, 50), new Color(0.7f, 0.5f, 0.1f));
            UnityEventTools.AddPersistentListener(readyBtn.GetComponent<Button>().onClick, presenter.ToggleReady);

            GameObject leaveBtn = CreateButton(panelObj.transform, "LeaveButton", "🚪 Leave",
                new Vector2(0, -150), new Vector2(200, 50), new Color(0.7f, 0.15f, 0.15f));
            UnityEventTools.AddPersistentListener(leaveBtn.GetComponent<Button>().onClick, presenter.LeaveLobby);
            UnityEventTools.AddPersistentListener<string>(ipField.onValueChanged, presenter.SetAddress);
            UnityEventTools.AddPersistentListener<string>(neoDiscovery.OnServerFound, presenter.SetAddress);
            UnityEventTools.AddPersistentListener<string>(neoDiscovery.OnServerFound, ipField.SetTextWithoutNotify);

            GameObject playerCountText = CreateText(panelObj.transform, "PlayerCountText", "Players: 0", 22,
                new Vector2(0, -220), new Vector2(300, 40));

            SetText countView = playerCountText.AddComponent<SetText>();
            countView.text = playerCountText.GetComponent<TMP_Text>();
            SetText statusView = statusText.AddComponent<SetText>();
            statusView.text = statusText.GetComponent<TMP_Text>();
            UnityEventTools.AddPersistentListener<string>(presenter.OnPlayerCountTextChanged, countView.Set);
            UnityEventTools.AddPersistentListener<string>(presenter.OnStatusChanged, statusView.Set);

            CreateText(panelObj.transform, "InfoLabel",
                "Player prefabs and presenter callbacks are saved.\n" +
                "Add lobby and generated gameplay scenes to Build Profiles\n" +
                "before Ready starts the game. Set a Transport if none was found.",
                14, new Vector2(0, -300), new Vector2(500, 80));

            CreateEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);

            bool saved = EditorSceneManager.SaveScene(scene, path);
            if (saved)
            {
                Debug.Log($"[Neo] Lobby demo scene saved to: {path}");
            }

            Selection.activeGameObject = managerObj;
            Debug.Log("[Neo] Lobby Demo Scene created. Add its lobby/gameplay scenes to Build Profiles before playing.");
            return saved;
        }

        private static string CreateGameplayScene(string folder, string templatePath)
        {
            Scene previousActive = SceneManager.GetActiveScene();
            string path = folder + "/LobbyGame.unity";
            CopySceneTemplate(templatePath, path);
            string guid = AssetDatabase.AssetPathToGUID(path);
            Scene gameplay = default;
            try
            {
                gameplay = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (SceneManager.GetActiveScene() != gameplay && !SceneManager.SetActiveScene(gameplay))
                {
                    throw new InvalidOperationException("Could not activate the owned gameplay scene.");
                }

                CreateDefaultSceneObjects();
                GameObject canvas = CreateCanvas();
                // No cross-scene serialized manager reference: the runtime presenter resolves the surviving singleton.
                NeoLobbyPresenter presenter = canvas.AddComponent<NeoLobbyPresenter>();
                GameObject panel = CreatePanel(canvas.transform, "GamePanel", Vector2.zero);
                CreateText(panel.transform, "GameInfo", "Gameplay scene — extend the saved GamePlayer prefab", 22,
                    new Vector2(0, 80), new Vector2(550, 80));
                GameObject leave = CreateButton(panel.transform, "LeaveButton", "Leave game",
                    new Vector2(0, -40), new Vector2(220, 50), new Color(0.7f, 0.15f, 0.15f));
                UnityEventTools.AddPersistentListener(leave.GetComponent<Button>().onClick, presenter.LeaveLobby);
                CreateEventSystem();
                if (!EditorSceneManager.SaveScene(gameplay, path))
                {
                    throw new IOException("Could not save gameplay scene: " + path);
                }

                return path;
            }
            finally
            {
                try
                {
                    CloseOwnedScene(gameplay, path, guid);
                }
                finally
                {
                    if (previousActive.IsValid() && previousActive.isLoaded)
                        SceneManager.SetActiveScene(previousActive);
                }
            }
        }

        private static bool DestinationExists(string path)
        {
            string absolutePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
            return File.Exists(absolutePath) || File.Exists(absolutePath + ".meta") || Directory.Exists(absolutePath) ||
                   !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path));
        }

        private static void CopySceneTemplate(string templatePath, string path)
        {
            if (DestinationExists(path))
                throw new IOException("Refusing to overwrite an existing scene destination: " + path);
            if (!AssetDatabase.CopyAsset(templatePath, path))
                throw new IOException("Could not copy the lobby scene template to: " + path);
            try
            {
                AssetDatabase.ImportAsset(path,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new IOException("The copied scene template is not a SceneAsset: " + path);
            }
            catch
            {
                AssetDatabase.DeleteAsset(path);
                throw;
            }
        }

        private static void CloseOwnedScene(Scene scene, string path, string guid)
        {
            if (!scene.IsValid() && !string.IsNullOrEmpty(guid) && AssetDatabase.AssetPathToGUID(path) == guid)
                scene = SceneManager.GetSceneByPath(path);
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }

        private static void CreateDefaultSceneObjects()
        {
            GameObject camera = new("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 1f, -10f);
            GameObject light = new("Directional Light", typeof(Light));
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light source = light.GetComponent<Light>();
            source.type = LightType.Directional;
            source.shadows = LightShadows.Soft;
        }

        private static GameObject CreateCanvas()
        {
            GameObject root = new("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            return root;
        }

        private static void CreateEventSystem()
        {
            GameObject root = new("EventSystem");
            root.AddComponent<UnityEngine.EventSystems.EventSystem>();
            root.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static GameObject CreatePlayerPrefab(string folder, bool roomPlayer)
        {
            GameObject source = new(roomPlayer ? "RoomPlayer" : "GamePlayer");
            try
            {
                source.AddComponent<NetworkIdentity>();
                if (roomPlayer)
                {
                    source.AddComponent<NeoLobbyPlayer>();
                }

                string path = folder + "/" + source.name + ".prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
                if (prefab == null)
                {
                    throw new IOException("Could not save player prefab: " + path);
                }

                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 pos)
        {
            GameObject obj = new(name);
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
            GameObject obj = new(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = color;

            obj.AddComponent<Button>();

            GameObject textObj = new("Label");
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
            GameObject obj = new(name);
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
