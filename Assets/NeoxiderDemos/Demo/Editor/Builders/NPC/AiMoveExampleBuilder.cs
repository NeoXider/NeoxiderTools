using System;
using Neo.NPC;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.NPC demo into Ai/AiMoveExample.unity: four NavMesh NPCs driven by <see cref="NpcNavigation" />.
    ///     The hand-authored world (baked NavMesh, walls, orbiting target, the two capsule agents) stays; the two agents'
    ///     legacy AiNavigation is migrated to NpcNavigation once (settings copied) and they become the blue Chaser
    ///     (FollowTarget) and the white Guard (mode switched from the card). The builder adds the green Patroller with
    ///     its route, the orange Sentry (Combined) with its zone and alert light, billboard labels, the control card with
    ///     an inline log and the info card, and wires the NPC events to <see cref="NpcDemoController" />.
    /// </summary>
    internal static class AiMoveExampleBuilder
    {
        private const string Scene = "AiMoveExample.unity";
        private const string LegacyNavigationType = "Neo.Tools.AiNavigation";
        private const string ChaserName = "NPC Chaser";
        private const string GuardName = "NPC Guard";
        private const float LabelHeight = 1.9f;

        private static readonly Color Accent = new(0.35f, 0.85f, 0.55f);
        private static readonly Color ChaserColor = new(0.45f, 0.66f, 1f);
        private static readonly Color GuardColor = new(0.95f, 0.95f, 0.98f);
        private static readonly Color PatrollerColor = new(0.55f, 1f, 0.40f);
        private static readonly Color SentryColor = new(1f, 0.62f, 0.25f);

        private static readonly Vector3 CameraPosition = new(0f, 24f, -18f);
        private static readonly Vector3 CameraEuler = new(55f, 0f, 0f);

        private static readonly Vector3[] RoutePoints =
        {
            new(-6f, 0.6f, 6f),
            new(10f, 0.6f, 6f),
            new(10f, 0.6f, 12f),
            new(-6f, 0.6f, 12f)
        };

        private static readonly Vector3 SentryZoneCenter = new(-10f, 0.6f, -6f);
        private static readonly Vector3 SentryZoneSize = new(7f, 2f, 7f);

        [DemoSceneBuilder("Scenes/Ai/AiMoveExample.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            Transform target = Step("orbiting target", () => ShopNpcBuildSupport.RequireNamed<Transform>(ctx, "target", "Target"));
            ConstantRotator orbit = Step("orbiting target",
                () => ShopNpcBuildSupport.RequireHandAuthored<ConstantRotator>(ctx, "the pivot that moves the target"));
            NavMeshAgent chaserAgent = Step("hand-authored agents",
                () => ShopNpcBuildSupport.RequireNamed<NavMeshAgent>(ctx, ChaserName, "AI agent"));
            NavMeshAgent guardAgent = Step("hand-authored agents",
                () => ShopNpcBuildSupport.RequireNamed<NavMeshAgent>(ctx, GuardName, "Capsule"));

            NpcNavigation chaser = Step("migrate the chaser", () => MigrateLegacy(ctx, chaserAgent.gameObject, ChaserName));
            NpcNavigation guard = Step("migrate the guard", () => MigrateLegacy(ctx, guardAgent.gameObject, GuardName));
            Camera camera = Step("overview camera", () => FixCamera(ctx));

            ModuleDemoSceneInfo info = Step("services", () => FindInfo(ctx));
            NpcDemoController controller = Step("services",
                () => ctx.FindOrCreate<NpcDemoController>("Services", "Demo Controller"));

            Materials materials = Step("materials", () => LoadMaterials());
            Transform world = ctx.Root("World").transform;
            Transform[] route = Step("patrol route", () => BuildRoute(ctx, world, materials.Patroller));
            BoxCollider zone = Step("sentry zone", () => BuildSentryZone(ctx, world, materials.Pad));
            NpcNavigation patroller = Step("patroller",
                () => BuildNpc(ctx, world, "NPC Patroller", RoutePoints[0], materials.Patroller));
            NpcNavigation sentry = Step("sentry",
                () => BuildNpc(ctx, world, "NPC Sentry", SentryZoneCenter, materials.Sentry));
            GameObject alert = Step("sentry alert", () => BuildAlert(ctx, sentry.transform, materials.Alert));

            Step("navigation settings", () =>
            {
                ConfigureChaser(ctx, chaser, target);
                ConfigureGuard(ctx, guard, target, route);
                ConfigurePatroller(ctx, patroller, route);
                ConfigureSentry(ctx, sentry, target, zone);
            });

            Step("labels", () =>
            {
                Label(ctx, chaser.transform, false, "Chaser · Follow", ChaserColor, camera);
                Label(ctx, guard.transform, false, "Guard · SetMode", GuardColor, camera);
                Label(ctx, patroller.transform, true, "Patroller · Patrol", PatrollerColor, camera);
                Label(ctx, sentry.transform, true, "Sentry · Combined", SentryColor, camera);
            });

            DemoFrame frame = Step("frame", () => DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Neo.NPC · NpcNavigation", Accent)
                {
                    Backdrop = DemoBackdrop.None,
                    Placement = DemoContentPlacement.Right,
                    ContentWidth = 450f,
                    ContentOffsetY = 0f,
                    LogFeed = false
                }));

            Controls controls = Step("controls", () => BuildControls(ctx, frame.Content, controller, chaser, orbit));
            Step("npc events", () => WireEvents(ctx, controller, chaser, guard, patroller, sentry, alert));

            Step("info card", () => frame.InfoCard(info, new DemoInfoContent
            {
                Title = "Neo.NPC · NpcNavigation",
                Purpose = "Four NavMesh NPCs, one NpcNavigation each: the blue Chaser follows the orbiting red target, " +
                          "the white Guard switches Follow / Patrol / Combined from the card, the green Patroller walks " +
                          "its route and the orange Sentry patrols its zone until the target comes within 6 m.",
                RuntimeApi = "npc.SetMode(NavigationMode) / SetFollowTarget / SetCombinedTarget / SetDestination / " +
                             "RefreshPath / SetRunning / Stop / Resume; IsChasing, IsRunning, Mode; events onModeChanged, " +
                             "onStartFollowing / onStopFollowing, onPatrolPointReached, onMovementStarted / Stopped.",
                SceneWorkflow = "Select 'NPC Chaser', 'NPC Guard' or '[Demo] World/NPC Patroller | NPC Sentry' to see " +
                                "NpcNavigation + NavMeshAgent. Their events call '[Demo] Services/Demo Controller'; the " +
                                "Sentry's onStartFollowing also turns on its red 'Alert' (GameObject.SetActive, NoCode).",
                Verification = "Play: Chaser and Guard path around the yellow walls; Run raises the Chaser's speed and " +
                               "Stop freezes it; the Guard changes mode from the card; the Sentry's alert lights while " +
                               "it chases. Stagger Path Updates spreads the NPCs' re-paths over the 0.5 s interval.",
                Hints = new[]
                {
                    "Walk / Run, Stop / Resume -> NpcNavigation directly (NoCode)",
                    "Follow / Patrol / Combined -> NpcDemoController.SetGuardMode -> guard.SetMode",
                    "Sentry onStartFollowing -> Alert.SetActive(true) + log",
                    "Orbit speed slider -> ConstantRotator.SetDegreesPerSecond"
                },
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = 400f,
                StartCollapsed = false
            }));

            Step("controller references", () =>
            {
                ctx.Set(controller, "_chaser", chaser);
                ctx.Set(controller, "_guard", guard);
                ctx.Set(controller, "_patroller", patroller);
                ctx.Set(controller, "_sentry", sentry);
                ctx.Set(controller, "_chaserValue", controls.Chaser);
                ctx.Set(controller, "_guardValue", controls.Guard);
                ctx.Set(controller, "_sentryValue", controls.Sentry);
                ctx.Set(controller, "_log", controls.Log);
            });
        }

        private static NpcNavigation MigrateLegacy(DemoSceneBuildContext ctx, GameObject host, string demoName)
        {
            MonoBehaviour legacy = null;
            foreach (MonoBehaviour behaviour in host.GetComponents<MonoBehaviour>())
            {
                if (behaviour != null && behaviour.GetType().FullName == LegacyNavigationType)
                {
                    legacy = behaviour;
                    break;
                }
            }

            NpcNavigation navigation = ctx.Ensure<NpcNavigation>(host);
            if (legacy != null)
            {
                CopyLegacySettings(new SerializedObject(legacy), navigation);
                Object.DestroyImmediate(legacy);
                ctx.Note("Migrated the deprecated AiNavigation on '" + host.name + "' to NpcNavigation (settings copied).");
            }

            Rigidbody body = host.GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                // WHY: the NavMeshAgent owns the movement; a dynamic body with gravity fought it and could be knocked
                // over by the orbiting target.
                body.isKinematic = true;
                body.useGravity = false;
                ctx.Note("Made the Rigidbody on '" + host.name + "' kinematic (NavMeshAgent drives the position).");
                ctx.MarkModified(body);
            }

            if (host.name != demoName)
            {
                ctx.Note("Renamed hand-authored agent '" + host.name + "' to '" + demoName + "'.");
                host.name = demoName;
            }

            ctx.MarkModified(host);
            return navigation;
        }

        private static void CopyLegacySettings(SerializedObject from, NpcNavigation navigation)
        {
            SerializedObject to = new(navigation);
            string[,] pairs =
            {
                { "movementMode", "mode" },
                { "target", "followTarget" },
                { "triggerDistance", "triggerDistance" },
                { "stoppingDistance", "stoppingDistance" },
                { "patrolPoints", "patrolPoints" },
                { "patrolZone", "patrolZone" },
                { "patrolWaitTime", "patrolWaitTime" },
                { "loopPatrol", "loopPatrol" },
                { "aggroDistance", "aggroDistance" },
                { "maxFollowDistance", "maxFollowDistance" },
                { "updateRotation", "updateRotation" },
                { "walkSpeed", "walkSpeed" },
                { "runSpeed", "runSpeed" },
                { "acceleration", "acceleration" },
                { "turnSpeed", "turnSpeed" },
                { "maxPathLength", "maxSampleDistance" },
                { "autoUpdatePath", "autoUpdatePath" },
                { "pathUpdateInterval", "pathUpdateInterval" },
                { "pathCheckRadius", "agentRadius" },
                { "animator", "animator" },
                { "debugMode", "debugMode" }
            };

            for (int i = 0; i < pairs.GetLength(0); i++)
            {
                CopyProperty(from.FindProperty(pairs[i, 0]), to.FindProperty(pairs[i, 1]));
            }

            // WHY: AiNavigation's Combined mode chased its single target; NpcNavigation keeps a separate Combined target.
            CopyProperty(from.FindProperty("target"), to.FindProperty("combinedTarget"));
            to.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CopyProperty(SerializedProperty source, SerializedProperty destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            switch (source.propertyType)
            {
                case SerializedPropertyType.Float:
                    destination.floatValue = source.floatValue;
                    return;
                case SerializedPropertyType.Boolean:
                    destination.boolValue = source.boolValue;
                    return;
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                    destination.intValue = source.intValue;
                    return;
                case SerializedPropertyType.ObjectReference:
                    destination.objectReferenceValue = source.objectReferenceValue;
                    return;
                case SerializedPropertyType.String:
                    destination.stringValue = source.stringValue;
                    return;
            }

            if (source.isArray && destination.isArray)
            {
                destination.arraySize = source.arraySize;
                for (int i = 0; i < source.arraySize; i++)
                {
                    CopyProperty(source.GetArrayElementAtIndex(i), destination.GetArrayElementAtIndex(i));
                }
            }
        }

        private static Camera FixCamera(DemoSceneBuildContext ctx)
        {
            Camera camera = ShopNpcBuildSupport.RequireHandAuthored<Camera>(ctx, "the scene camera");
            Transform cameraTransform = camera.transform;
            // WHY: the camera used to ride on the blue agent; a fixed overview keeps all four NPCs in view. The pose is
            // set only while detaching, so a later hand adjustment of the camera survives rebuilds.
            if (cameraTransform.parent != null && cameraTransform.parent.GetComponent<NavMeshAgent>() != null)
            {
                ctx.Note("Detached '" + camera.name + "' from '" + cameraTransform.parent.name + "' into a fixed overview.");
                cameraTransform.SetParent(null, false);
                cameraTransform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(CameraEuler));
                cameraTransform.localScale = Vector3.one;
                ctx.MarkModified(cameraTransform);
                ctx.MarkModified(camera.gameObject);
            }

            return camera;
        }

        private static ModuleDemoSceneInfo FindInfo(DemoSceneBuildContext ctx)
        {
            ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
            if (!DemoSceneBuildContext.IsGenerated(info.gameObject) && info.gameObject.name == "AiNavigation Demo")
            {
                info.gameObject.name = "Demo Info";
                ctx.MarkModified(info.gameObject);
            }

            return info;
        }

        private static Materials LoadMaterials()
        {
            return new Materials(
                DemoSceneAssets.LoadDemoAsset<Material>("Materials/green.mat"),
                DemoSceneAssets.LoadDemoAsset<Material>("Scenes/Ai/AiSentryOrange.mat"),
                DemoSceneAssets.LoadDemoAsset<Material>("Materials/red.mat"),
                DemoSceneAssets.LoadDemoAsset<Material>("Scenes/Ai/AiZonePad.mat"));
        }

        private static Transform[] BuildRoute(DemoSceneBuildContext ctx, Transform world, Material material)
        {
            Transform route = ctx.Child(world, "Patrol Route").transform;
            route.localPosition = Vector3.zero;
            Transform[] points = new Transform[RoutePoints.Length];
            for (int i = 0; i < RoutePoints.Length; i++)
            {
                Transform point = ctx.Child(route, "Point " + (i + 1)).transform;
                point.localPosition = RoutePoints[i];
                point.localRotation = Quaternion.identity;
                GameObject marker = ctx.Primitive(point, "Marker", PrimitiveType.Cylinder);
                ShopNpcBuildSupport.RemoveCollider(marker);
                marker.transform.localPosition = new Vector3(0f, -1.08f, 0f);
                marker.transform.localRotation = Quaternion.identity;
                marker.transform.localScale = new Vector3(0.9f, 0.02f, 0.9f);
                marker.GetComponent<MeshRenderer>().sharedMaterial = material;
                points[i] = point;
            }

            return points;
        }

        private static BoxCollider BuildSentryZone(DemoSceneBuildContext ctx, Transform world, Material material)
        {
            GameObject zone = ctx.Child(world, "Sentry Zone");
            zone.transform.localPosition = SentryZoneCenter;
            zone.transform.localRotation = Quaternion.identity;
            zone.transform.localScale = Vector3.one;
            BoxCollider box = ctx.Ensure<BoxCollider>(zone);
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = SentryZoneSize;

            GameObject pad = ctx.Primitive(zone.transform, "Zone Pad", PrimitiveType.Cube);
            ShopNpcBuildSupport.RemoveCollider(pad);
            pad.transform.localPosition = new Vector3(0f, -1.08f, 0f);
            pad.transform.localRotation = Quaternion.identity;
            pad.transform.localScale = new Vector3(SentryZoneSize.x, 0.02f, SentryZoneSize.z);
            pad.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        private static NpcNavigation BuildNpc(DemoSceneBuildContext ctx, Transform world, string name, Vector3 position,
            Material material)
        {
            // WHY: the NPC root is a plain child so its label and alert can be matched on rebuilds; a primitive reused
            // by the build context keeps its whole subtree, so the capsule mesh lives in its own "Body" child.
            GameObject npc = ctx.Child(world, name);
            npc.transform.localPosition = position;
            npc.transform.localRotation = Quaternion.identity;
            npc.transform.localScale = Vector3.one;

            GameObject body = ctx.Primitive(npc.transform, "Body", PrimitiveType.Capsule);
            // WHY: agents avoid each other through the NavMesh; a collider would let the Chaser's rigidbody shove them.
            ShopNpcBuildSupport.RemoveCollider(body);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = Vector3.one;
            body.GetComponent<MeshRenderer>().sharedMaterial = material;

            NavMeshAgent agent = ctx.Ensure<NavMeshAgent>(npc);
            agent.baseOffset = 1f;
            agent.height = 2f;
            agent.radius = 0.5f;
            agent.speed = 3.5f;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            return ctx.Ensure<NpcNavigation>(npc);
        }

        private static GameObject BuildAlert(DemoSceneBuildContext ctx, Transform sentry, Material material)
        {
            GameObject alert = ctx.Primitive(sentry, "Alert", PrimitiveType.Sphere);
            ShopNpcBuildSupport.RemoveCollider(alert);
            alert.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            alert.transform.localRotation = Quaternion.identity;
            alert.transform.localScale = Vector3.one * 0.45f;
            alert.GetComponent<MeshRenderer>().sharedMaterial = material;
            alert.SetActive(false);
            return alert;
        }

        private static void ConfigureChaser(DemoSceneBuildContext ctx, NpcNavigation chaser, Transform target)
        {
            ctx.Set(chaser, "isActive", true);
            ctx.Set(chaser, "mode", NpcNavigation.NavigationMode.FollowTarget);
            ctx.Set(chaser, "followTarget", target);
            ctx.Set(chaser, "triggerDistance", 0f);
            ctx.Set(chaser, "stoppingDistance", 1.2f);
            ctx.Set(chaser, "walkSpeed", 3f);
            ctx.Set(chaser, "runSpeed", 6.5f);
            ApplyPathDefaults(ctx, chaser);
        }

        private static void ConfigureGuard(DemoSceneBuildContext ctx, NpcNavigation guard, Transform target,
            Transform[] route)
        {
            ctx.Set(guard, "isActive", true);
            ctx.Set(guard, "mode", NpcNavigation.NavigationMode.FollowTarget);
            ctx.Set(guard, "followTarget", target);
            ctx.Set(guard, "triggerDistance", 10f);
            ctx.Set(guard, "stoppingDistance", 1.5f);
            ctx.Set(guard, "patrolPoints", route);
            ctx.Set(guard, "patrolZone", null);
            ctx.Set(guard, "patrolWaitTime", 0.5f);
            ctx.Set(guard, "loopPatrol", true);
            ctx.Set(guard, "combinedTarget", target);
            ctx.Set(guard, "aggroDistance", 7f);
            ctx.Set(guard, "maxFollowDistance", 12f);
            ApplyPathDefaults(ctx, guard);
        }

        private static void ConfigurePatroller(DemoSceneBuildContext ctx, NpcNavigation patroller, Transform[] route)
        {
            ctx.Set(patroller, "isActive", true);
            ctx.Set(patroller, "mode", NpcNavigation.NavigationMode.Patrol);
            ctx.Set(patroller, "followTarget", null);
            ctx.Set(patroller, "patrolPoints", route);
            ctx.Set(patroller, "patrolZone", null);
            ctx.Set(patroller, "patrolWaitTime", 0.6f);
            ctx.Set(patroller, "loopPatrol", true);
            ctx.Set(patroller, "stoppingDistance", 0.3f);
            ctx.Set(patroller, "walkSpeed", 3.2f);
            ctx.Set(patroller, "runSpeed", 6f);
            ctx.Set(patroller, "turnSpeed", 360f);
            ctx.Set(patroller, "agentRadius", 0.5f);
            ApplyPathDefaults(ctx, patroller);
        }

        private static void ConfigureSentry(DemoSceneBuildContext ctx, NpcNavigation sentry, Transform target,
            BoxCollider zone)
        {
            ctx.Set(sentry, "isActive", true);
            ctx.Set(sentry, "mode", NpcNavigation.NavigationMode.Combined);
            ctx.Set(sentry, "followTarget", null);
            ctx.Set(sentry, "combinedTarget", target);
            ctx.Set(sentry, "patrolPoints", Array.Empty<Transform>());
            ctx.Set(sentry, "patrolZone", zone);
            ctx.Set(sentry, "patrolWaitTime", 1f);
            ctx.Set(sentry, "loopPatrol", true);
            ctx.Set(sentry, "aggroDistance", 6f);
            ctx.Set(sentry, "maxFollowDistance", 10f);
            ctx.Set(sentry, "stoppingDistance", 1.2f);
            ctx.Set(sentry, "walkSpeed", 2.6f);
            ctx.Set(sentry, "runSpeed", 5f);
            ctx.Set(sentry, "turnSpeed", 360f);
            ctx.Set(sentry, "agentRadius", 0.5f);
            ApplyPathDefaults(ctx, sentry);
        }

        private static void ApplyPathDefaults(DemoSceneBuildContext ctx, NpcNavigation navigation)
        {
            ctx.Set(navigation, "autoUpdatePath", true);
            ctx.Set(navigation, "pathUpdateInterval", 0.5f);
            ctx.Set(navigation, "staggerPathUpdates", true);
            ctx.Set(navigation, "drawGizmos", true);
        }

        private static void Label(DemoSceneBuildContext ctx, Transform npc, bool generatedHost, string text, Color color,
            Camera camera)
        {
            // WHY: a hand-authored NPC can only host generated content inside its own generated root.
            GameObject host = generatedHost ? ctx.UiChild(npc, "Label").gameObject : ctx.RootUnder(npc, "Label", true);
            host.layer = 0;
            RectTransform rect = (RectTransform)host.transform;
            rect.localPosition = new Vector3(0f, LabelHeight, 0f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(8f, 1.2f);
            rect.rotation = camera.transform.rotation;

            TextMeshPro label = ctx.Ensure<TextMeshPro>(host);
            DemoSceneKit.StyleText(label, text, 8f, color, TextAlignmentOptions.Center, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;

            BillboardUniversal billboard = ctx.Ensure<BillboardUniversal>(host);
            ctx.Set(billboard, "targetCamera", camera);
            ctx.Set(billboard, "billboardMode", BillboardUniversal.BillboardMode.AwayFromCamera);
            ctx.Set(billboard, "ignoreY", false);
            ctx.Set(billboard, "useMainCameraFallback", true);
        }

        private static Controls BuildControls(DemoSceneBuildContext ctx, DemoPanel card, NpcDemoController controller,
            NpcNavigation chaser, ConstantRotator orbit)
        {
            card.Section("Chaser · NpcNavigation API");
            TMP_Text chaserValue = ShopNpcBuildSupport.CompactValue(card, "Chaser",
                "waiting · " + ReadFloat(chaser, "walkSpeed", 3f).ToString("0.0") + " m/s");
            Button[] speed = card.ButtonRow("Walk", "Run");
            Button[] flow = card.ButtonRow("Stop", "Resume");

            card.Section("Guard · SetMode(NavigationMode)");
            Button[] modes = card.ButtonRow("Follow", "Patrol", "Combined");
            TMP_Text guardValue = ShopNpcBuildSupport.CompactValue(card, "Guard mode", "FollowTarget");

            card.Section("Sentry · Combined events");
            TMP_Text sentryValue = ShopNpcBuildSupport.CompactValue(card, "Sentry", "patrolling");

            card.Section("Target · ConstantRotator");
            DemoSliderRow orbitSpeed = card.SliderRow("Orbit speed (deg per s)", 0f, 120f,
                Mathf.Clamp(orbit.degreesPerSecond, 0f, 120f), true, "0");

            DemoLogFeed log = card.LogRow(4, "NPCs ready");

            DemoWiring.AddBool(speed[0].onClick, chaser.SetRunning, false);
            DemoWiring.AddBool(speed[1].onClick, chaser.SetRunning, true);
            DemoWiring.AddVoid(flow[0].onClick, chaser.Stop);
            DemoWiring.AddString(flow[0].onClick, log.Log, "chaser.Stop() - IsActive = false");
            DemoWiring.AddVoid(flow[1].onClick, chaser.Resume);
            DemoWiring.AddString(flow[1].onClick, log.Log, "chaser.Resume()");
            for (int i = 0; i < modes.Length; i++)
            {
                DemoWiring.AddInt(modes[i].onClick, controller.SetGuardMode, i);
            }

            DemoWiring.AddDynamic(orbitSpeed.Slider.onValueChanged, orbit.SetDegreesPerSecond);
            ctx.MarkModified(chaser);
            return new Controls(chaserValue, guardValue, sentryValue, log);
        }

        private static void WireEvents(DemoSceneBuildContext ctx, NpcDemoController controller, NpcNavigation chaser,
            NpcNavigation guard, NpcNavigation patroller, NpcNavigation sentry, GameObject alert)
        {
            foreach (NpcNavigation navigation in new[] { chaser, guard, patroller, sentry })
            {
                EnsureEvents(navigation);
            }

            // WHY: the chaser and guard are hand-authored, so only the listeners this builder owns are replaced.
            ctx.OwnListeners(chaser, chaser.onMovementStarted, controller);
            ctx.OwnListeners(chaser, chaser.onMovementStopped, controller);
            ctx.OwnListeners(chaser, chaser.onSpeedChanged, controller);
            ctx.OwnListeners(guard, guard.onModeChanged, controller);
            DemoWiring.AddVoid(chaser.onMovementStarted, controller.OnChaserStarted);
            DemoWiring.AddVoid(chaser.onMovementStopped, controller.OnChaserStopped);
            DemoWiring.AddDynamic(chaser.onSpeedChanged, controller.OnChaserSpeedChanged);
            DemoWiring.AddDynamic(guard.onModeChanged, controller.OnGuardModeChanged);

            DemoWiring.AddDynamic(patroller.onPatrolPointReached, controller.OnPatrolPointReached);
            DemoWiring.AddBool(sentry.onStartFollowing, alert.SetActive, true);
            DemoWiring.AddVoid(sentry.onStartFollowing, controller.OnSentryChaseStarted);
            DemoWiring.AddBool(sentry.onStopFollowing, alert.SetActive, false);
            DemoWiring.AddVoid(sentry.onStopFollowing, controller.OnSentryChaseStopped);

            ctx.MarkModified(chaser);
            ctx.MarkModified(guard);
            ctx.MarkModified(patroller);
            ctx.MarkModified(sentry);
        }

        private static void EnsureEvents(NpcNavigation navigation)
        {
            // WHY: a component added from code in the editor may not have its UnityEvent fields deserialized yet.
            navigation.onMovementStarted ??= new UnityEvent();
            navigation.onMovementStopped ??= new UnityEvent();
            navigation.onSpeedChanged ??= new UnityEvent<float>();
            navigation.onModeChanged ??= new UnityEvent<NpcNavigation.NavigationMode>();
            navigation.onPatrolPointReached ??= new UnityEvent<int>();
            navigation.onStartFollowing ??= new UnityEvent();
            navigation.onStopFollowing ??= new UnityEvent();
        }

        private static float ReadFloat(Object owner, string path, float fallback)
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(path);
            return property != null && property.propertyType == SerializedPropertyType.Float ? property.floatValue : fallback;
        }

        private static void Step(string step, Action action)
        {
            DemoSceneKit.Step(Scene, step, action);
        }

        private static T Step<T>(string step, Func<T> action)
        {
            return DemoSceneKit.Step(Scene, step, action);
        }

        private sealed class Materials
        {
            public Materials(Material patroller, Material sentry, Material alert, Material pad)
            {
                Patroller = patroller;
                Sentry = sentry;
                Alert = alert;
                Pad = pad;
            }

            public Material Patroller { get; }
            public Material Sentry { get; }
            public Material Alert { get; }
            public Material Pad { get; }
        }

        private sealed class Controls
        {
            public Controls(TMP_Text chaser, TMP_Text guard, TMP_Text sentry, DemoLogFeed log)
            {
                Chaser = chaser;
                Guard = guard;
                Sentry = sentry;
                Log = log;
            }

            public TMP_Text Chaser { get; }
            public TMP_Text Guard { get; }
            public TMP_Text Sentry { get; }
            public DemoLogFeed Log { get; }
        }
    }
}
