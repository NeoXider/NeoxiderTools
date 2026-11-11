using System.Collections.Generic;
using Neo.Samples;
using Neo.Tools;
using Neo.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Guide of the owner's hand-authored first-person sandbox (Scenes/_ExampleGame/adventure.unity), a NoCode
    ///     reference world. The player prefab, the Physics zones and interactables, the NPC prefabs, the pickups and the
    ///     inventory UI stay as authored; this builder only adds the info card, upgrades the legacy input module and
    ///     moves the player's autosaving inventory off the library default key "Inventory_Default", which made the
    ///     sample read and overwrite the user's own default inventory save.
    /// </summary>
    internal static class AdventureBuilder
    {
        internal const string ScenePath = "Scenes/_ExampleGame/adventure.unity";
        internal const string InventorySaveKey = "NeoDemo.Adventure.Inventory";

        private const string SceneName = "adventure.unity";

        private static readonly Color Accent = DemoPalette.Cyan;

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            PlayerController3DPhysics player = DemoSceneKit.Step(SceneName, "find the hand-authored player",
                () => ExampleGameGuide.Require<PlayerController3DPhysics>(ctx, SceneName, "first-person player"));
            DemoSceneKit.Step(SceneName, "give the inventory a demo-only save key",
                () => DemoSaveKeys.UseForAll<InventoryComponent>(ctx, "_saveKey", InventorySaveKey));
            ExampleGameGuide.AuthorInfoCard(ctx, SceneName, Content(ctx, player), Accent);
        }

        private static DemoInfoContent Content(DemoSceneBuildContext ctx, PlayerController3DPhysics player)
        {
            string playerName = ExampleGameGuide.NameOf(player);
            Spawner spawner = ExampleGameGuide.First<Spawner>(ctx);
            PhysicsEvents3D trigger = spawner != null ? TriggerOf(ctx, spawner) : null;
            MagneticField magnetic = ExampleGameGuide.First<MagneticField>(ctx);
            ImpulseZone impulse = ExampleGameGuide.First<ImpulseZone>(ctx);
            ExplosiveForce explosive = ExampleGameGuide.First<ExplosiveForce>(ctx);
            VisualToggle toggle = ExampleGameGuide.First<VisualToggle>(ctx);
            MultiKeyEventTrigger keys = ExampleGameGuide.First<MultiKeyEventTrigger>(ctx);
            PickableItem pickup = ExampleGameGuide.First<PickableItem>(ctx);
            InventoryView view = ExampleGameGuide.First<InventoryView>(ctx);

            List<string> hints = new();
            ExampleGameGuide.Hint(ctx, hints, trigger, "the PhysicsEvents3D trigger of the spawner",
                () => DemoSceneBuildContext.PathOf(trigger.transform) + ": PhysicsEvents3D onTriggerEnter / Exit -> " +
                      ExampleGameGuide.NameOf(spawner) + ".StartSpawn / StopSpawn + particles on / off");
            ExampleGameGuide.Hint(ctx, hints, magnetic != null ? magnetic : impulse, "MagneticField / ImpulseZone",
                () => PhysicsZones(magnetic, impulse));
            ExampleGameGuide.Hint(ctx, hints, explosive, "the ExplosiveForce sphere",
                () => ExampleGameGuide.NameOf(explosive) + ": InteractiveObject (E) -> ExplosiveForce.Explode()");
            ExampleGameGuide.Hint(ctx, hints, toggle, "the VisualToggle sphere",
                () => ExampleGameGuide.NameOf(toggle) + ": E -> VisualToggle.Toggle -> Terrain renderer on / off");
            ExampleGameGuide.Hint(ctx, hints, keys, "the MultiKeyEventTrigger hotkeys",
                () => ExampleGameGuide.NameOf(keys) + ": MultiKeyEventTrigger 1 / 2 -> Selector.Previous / Next = " +
                      "the InventoryHand slot");
            ExampleGameGuide.Hint(ctx, hints, pickup, "the PickableItem pickups",
                () => "Pickups: PickableItem + InventoryPickupBridge fill the inventory" +
                      (view != null ? ", " + ExampleGameGuide.NameOf(view) + " (InventoryView) lists it" : "") +
                      "; saved under " + InventorySaveKey);

            return new DemoInfoContent
            {
                Title = "Example game · Adventure",
                Purpose = "A first-person sandbox assembled only from library prefabs, components and UnityEvents, " +
                          "without game scripts: physics zones, interactables, AI NPCs, pickups and an inventory " +
                          "with a hand slot.",
                RuntimeApi = "InventoryComponent.AddItemById / DropSelected / Save, ExplosiveForce.Explode(), " +
                             "Spawner.StartSpawn / StopSpawn, Selector.Next / Previous, InteractiveObject events.",
                SceneWorkflow = playerName + " = PlayerController3DPhysics + InventoryComponent, InventoryHand, " +
                                "InventoryDropper, Selector; Physics = zones and interactables; NPC = AiNavigation / " +
                                "NpcNavigation prefabs on a NavMeshSurface.",
                Verification = "WASD + mouse to walk, E interacts, walk into the spheres to pick them up, 1 / 2 " +
                               "switch the hand slot, G drops the held item. Esc frees the cursor (to collapse " +
                               "this card).",
                Hints = hints.ToArray(),
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = ExampleGameGuide.CardWidth,
                StartCollapsed = false
            };
        }

        private static string PhysicsZones(MagneticField magnetic, ImpulseZone impulse)
        {
            List<string> parts = new();
            if (magnetic != null)
            {
                parts.Add(ExampleGameGuide.NameOf(magnetic) + " = MagneticField (attract / repel cycle)");
            }

            if (impulse != null)
            {
                parts.Add(ExampleGameGuide.NameOf(impulse) + " = ImpulseZone (pushes bodies away from its center)");
            }

            return string.Join(", ", parts);
        }

        /// <summary>The PhysicsEvents3D whose onTriggerEnter calls into <paramref name="spawner" />, or null.</summary>
        private static PhysicsEvents3D TriggerOf(DemoSceneBuildContext ctx, Spawner spawner)
        {
            foreach (PhysicsEvents3D events in ctx.FindAll<PhysicsEvents3D>())
            {
                UnityEventBase enter = events.onTriggerEnter;
                for (int i = 0; i < enter.GetPersistentEventCount(); i++)
                {
                    if (enter.GetPersistentTarget(i) == spawner)
                    {
                        return events;
                    }
                }
            }

            return null;
        }
    }
}
