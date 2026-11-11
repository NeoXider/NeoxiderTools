using System.Collections.Generic;
using Neo.Level;
using Neo.Samples;
using Neo.Shop;
using Neo.Tools;
using UnityEngine;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Guide of the owner's hand-authored arcade shooter (Scenes/_ExampleGame/Shooter2D.unity), a NoCode reference
    ///     game. Player, the spawners, the disabled Enemy template, Circle and the SceneFlowController HUD stay as
    ///     authored; this builder only adds the info card, upgrades the legacy input module and gives the persisting
    ///     managers of the --System-- prefab (Money, ScoreManager, LevelManager) demo-only save keys: the library
    ///     defaults "Money" / "BestScore" / "LevelManager" would touch the user's own balance, best score and level
    ///     progress.
    /// </summary>
    internal static class Shooter2DBuilder
    {
        internal const string ScenePath = "Scenes/_ExampleGame/Shooter2D.unity";
        internal const string MoneySaveKey = "NeoDemo.Shooter2D.Money";
        internal const string BestScoreSaveKey = "NeoDemo.Shooter2D.BestScore";
        internal const string LevelSaveKey = "NeoDemo.Shooter2D.Level";

        private const string SceneName = "Shooter2D.unity";
        private const string EnemySpawnerName = "SpawnerEnemys";
        private const string EnemyTemplateName = "Enemy";

        private static readonly Color Accent = DemoPalette.Danger;

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            MouseMover2D mover = DemoSceneKit.Step(SceneName, "find the hand-authored player",
                () => ExampleGameGuide.Require<MouseMover2D>(ctx, SceneName, "mouse-steered player ship"));
            DemoSceneKit.Step(SceneName, "give the --System-- managers demo-only save keys", () =>
            {
                DemoSaveKeys.UseForAll<Money>(ctx, "_moneySave", MoneySaveKey);
                DemoSaveKeys.UseForAll<ScoreManager>(ctx, "_keySave", BestScoreSaveKey);
                DemoSaveKeys.UseForAll<LevelManager>(ctx, "_saveKey", LevelSaveKey);
            });

            ExampleGameGuide.AuthorInfoCard(ctx, SceneName, Content(ctx, mover), Accent);
        }

        private static DemoInfoContent Content(DemoSceneBuildContext ctx, MouseMover2D mover)
        {
            string player = ExampleGameGuide.NameOf(mover);
            Health playerHealth = mover.GetComponent<Health>();
            Spawner gun = mover.GetComponentInChildren<Spawner>(true);
            Spawner enemies = ExampleGameGuide.Named<Spawner>(ctx, EnemySpawnerName);
            Health enemy = ExampleGameGuide.Named<Health>(ctx, EnemyTemplateName);
            AdvancedAttackCollider blast = ExampleGameGuide.First<AdvancedAttackCollider>(ctx);
            SceneFlowController flow = ExampleGameGuide.First<SceneFlowController>(ctx);

            List<string> hints = new();
            ExampleGameGuide.Hint(ctx, hints, playerHealth, "Health on " + player,
                () => player + " Health.OnChange -> hp Text SetText.Set, OnChangePercent -> barfill Image.fillAmount");
            ExampleGameGuide.Hint(ctx, hints, playerHealth, "Health on " + player,
                () => player + " Health.OnDeath -> Destroy Loot.DropLoot, Circle on, " + player + " off, GameOver on");
            ExampleGameGuide.Hint(ctx, hints, enemy, "the disabled '" + EnemyTemplateName + "' template",
                () => EnemyTemplateName + " (disabled template): Follow -> " + player + ", ConstantMover, " +
                      "Health.OnDeath -> explosion Loot + SetActive(false)");
            ExampleGameGuide.Hint(ctx, hints, blast, "the AdvancedAttackCollider blast",
                () => ExampleGameGuide.NameOf(blast) + ": AdvancedAttackCollider destroys every hit target; " +
                      "TimerObject.OnTimerCompleted -> SetActive(false)");
            ExampleGameGuide.Hint(ctx, hints, flow, "the SceneFlowController HUD",
                () => "GameOver Button -> " + ExampleGameGuide.NameOf(flow) + ".Restart()");

            return new DemoInfoContent
            {
                Title = "Example game · Shooter 2D",
                Purpose = "A complete arcade shooter built only from library components and UnityEvents, without " +
                          "game scripts: fly, shoot automatically and survive the enemy waves.",
                RuntimeApi = "Spawner.StartSpawn / StopSpawn, Health.TakeDamage / Heal, Loot.DropLoot(), " +
                             "SceneFlowController.Restart().",
                SceneWorkflow = player + " = MouseMover2D + Health + Loot" +
                                (gun != null ? ", its " + ExampleGameGuide.NameOf(gun) + " (Spawner) fires lasers" : "") +
                                (enemies != null
                                    ? "; " + EnemySpawnerName + " clones the disabled " + EnemyTemplateName +
                                      " inside its trigger area"
                                    : "") +
                                "; the SceneFlowController canvas holds the HP bar and Game Over.",
                Verification = "Hold the left mouse button: the ship flies to the pointer and fires on its own. " +
                               "Dodge the enemy lasers; at 0 HP the Circle blast clears the screen and the Game " +
                               "Over button restarts the scene.",
                Hints = hints.ToArray(),
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = ExampleGameGuide.CardWidth,
                StartCollapsed = false
            };
        }
    }
}
