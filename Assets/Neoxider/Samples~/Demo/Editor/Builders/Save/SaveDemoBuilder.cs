using System;
using Neo.Samples;
using Neo.Save;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Save demo into SaveDemo.unity. The hand-authored 'Save Demo' object keeps SaveManager and
    ///     gets a SaveProviderSettings asset (File provider, neo_save_demo.json) on its SaveProviderSettingsComponent,
    ///     so the demo saves into its own file. The card shows the two save layers side by side: [SaveField] values
    ///     through SaveManager and a 64-bit wallet through SaveProvider.SetLong, plus live FileSaveProvider status
    ///     (unsaved changes, OnDataSaved events versus real file writes). Save, Load and Reset call
    ///     <see cref="SaveDemoController" />; "FlushNow (NoCode)" calls <see cref="SaveManager.FlushNow()" /> directly.
    /// </summary>
    internal static class SaveDemoBuilder
    {
        /// <summary>Provider settings asset of the demo, relative to the Demo sample root.</summary>
        public const string ProviderSettingsAsset = "Data/Save/SaveDemoProviderSettings.asset";

        /// <summary>Save file the demo writes under Application.persistentDataPath.</summary>
        public const string SaveFileName = "neo_save_demo.json";

        private const string Scene = "SaveDemo.unity";

        private static readonly Color Accent = new(0.26f, 0.85f, 0.64f);

        [DemoSceneBuilder("Scenes/Save/SaveDemo.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "services";
            try
            {
                // WHY: the scene's hand-authored 'Save Demo' (SaveManager + SaveProviderSettingsComponent + info) and
                // 'Demo Controller' win; FindOrCreate only fills in what a stripped scene lacks.
                SaveManager manager = ctx.FindOrCreate<SaveManager>("Services", "Save Manager");
                SaveProviderSettingsComponent providerComponent =
                    ctx.FindOrCreate<SaveProviderSettingsComponent>("Services", "Save Provider Settings");
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
                if (info.GetComponent<SaveManager>() != null)
                {
                    // WHY: SaveManager keeps its GameObject across scene loads, which took the info card's owner out of
                    // the scene (and its card with it); the info gets an object of its own.
                    string owner = DemoSceneBuildContext.PathOf(info.transform);
                    UnityEngine.Object.DestroyImmediate(info);
                    info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
                    ctx.Note("Moved ModuleDemoSceneInfo off the persistent '" + owner + "' (SaveManager) object.");
                }

                SaveDemoController controller = ctx.FindOrCreate<SaveDemoController>("Services", "Demo Controller");

                step = "file provider settings asset";
                SaveProviderSettings settings =
                    DemoSceneAssets.EnsureAsset<SaveProviderSettings>(ProviderSettingsAsset, ConfigureFileProvider);
                ctx.Set(providerComponent, "_settings", settings);

                step = "frame";
                DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("Neo.Save", Accent) { ContentWidth = 920f });
                DemoPanel card = frame.Content;
                card.Note("Change a value -> 'Unsaved changes' -> Save -> re-enter Play Mode: the values return.");
                DemoPanel[] columns = card.Columns("SaveField | SaveProvider", 2, 20f);

                step = "[SaveField] column";
                DemoPanel fields = columns[0];
                fields.Section("[SaveField] · SaveManager");
                TMP_Text coins = fields.ValueLabel("Coins", "0");
                TMP_Text playerName = fields.ValueLabel("Name", "Rookie");
                TMP_Text bestTime = fields.ValueLabel("Best time", "-");
                fields.Note("keys Demo_Coins · Demo_Name · Demo_BestTime");
                Button[] counters = fields.ButtonRow("+10 coins", "+100 coins", "New run time");
                DemoWiring.AddInt(counters[0].onClick, controller.AddCoins, 10);
                DemoWiring.AddInt(counters[1].onClick, controller.AddCoins, 100);
                DemoWiring.AddVoid(counters[2].onClick, controller.NewRunTime);
                Button[] names = fields.ButtonRow("Name: Aurora", "Nova", "Zephyr");
                for (int i = 0; i < names.Length; i++)
                {
                    DemoWiring.AddInt(names[i].onClick, controller.SetName, i);
                }

                step = "SaveProvider column";
                DemoPanel store = columns[1];
                store.Section("64-bit · SaveProvider.SetLong");
                TMP_Text wallet = store.ValueLabel("Wallet", "$0.00");
                TMP_Text walletNote = store.Note("0 cents · key " + SaveDemoController.WalletKey);
                Button[] money = store.ButtonRow("+$1M", "+$10M");
                DemoWiring.AddInt(money[0].onClick, controller.AddWalletDollars, 1000000);
                DemoWiring.AddInt(money[1].onClick, controller.AddWalletDollars, 10000000);
                store.Section("FileSaveProvider · status");
                TMP_Text provider = store.ValueLabel("Provider", "File · " + SaveFileName);
                TMP_Text unsaved = store.ValueLabel("Unsaved changes", "no");
                TMP_Text saves = store.ValueLabel("OnDataSaved", "0 events · 0 writes");
                // WHY: these labels change at runtime; one line each keeps the 30 px rows from spilling into the next.
                NoWrap(wallet, walletNote, provider, unsaved, saves);

                step = "persistence buttons";
                card.Section("Persist · press Save twice");
                Button[] persist = card.ButtonRow("Save", "FlushNow (NoCode)", "Load", "Reset + Clear");
                DemoWiring.AddVoid(persist[0].onClick, controller.SaveToDisk);
                // WHY: NoCode path - the button calls the scene's SaveManager directly; the log line goes first so the
                // OnDataSaved result lands on top of the feed.
                DemoWiring.AddString(persist[1].onClick, frame.Log.Log,
                    "SaveManager.FlushNow() -> every registered [SaveField] + SaveProvider.Save()");
                DemoWiring.AddVoid(persist[1].onClick, manager.FlushNow);
                DemoWiring.AddVoid(persist[2].onClick, controller.LoadFromDisk);
                DemoWiring.AddVoid(persist[3].onClick, controller.ResetAndClear);
                card.Note("Second Save with nothing new: OnDataSaved fires again, the file is not rewritten.");

                step = "info card";
                frame.InfoCard(info, InfoContent());

                step = "controller references";
                ctx.Set(controller, "_coinsValue", coins);
                ctx.Set(controller, "_nameValue", playerName);
                ctx.Set(controller, "_timeValue", bestTime);
                ctx.Set(controller, "_walletValue", wallet);
                ctx.Set(controller, "_walletNote", walletNote);
                ctx.Set(controller, "_providerValue", provider);
                ctx.Set(controller, "_unsavedValue", unsaved);
                ctx.Set(controller, "_savedCountValue", saves);
                ctx.Set(controller, "_log", frame.Log);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    Scene + " build failed at step '" + step + "': " + exception.Message, exception);
            }
        }

        private static void ConfigureFileProvider(SaveProviderSettings settings)
        {
            SerializedObject serialized = new(settings);
            Require(serialized, "_providerType").intValue = (int)SaveProviderType.File;
            Require(serialized, "_fileName").stringValue = SaveFileName;
            // WHY: plain JSON so the reader can open the file and find the demo keys.
            Require(serialized, "_encryptFileSave").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void NoWrap(params TMP_Text[] labels)
        {
            foreach (TMP_Text label in labels)
            {
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
            }
        }

        private static SerializedProperty Require(SerializedObject serialized, string field)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException(serialized.targetObject.GetType().Name + " has no serialized field '" +
                                                    field + "'.");
            }

            return property;
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Save · SaveManager + SaveProvider",
                Purpose = "Two save layers. SaveDemoController is a SaveableBehaviour: fields with [SaveField(key)] " +
                          "persist through SaveManager. The wallet is a plain 64-bit key (SaveProvider.SetLong, above " +
                          "int.MaxValue). The scene uses a FileSaveProvider, so the demo never touches your PlayerPrefs.",
                RuntimeApi = "SaveManager.Save(component) / Load(component) / FlushNow(); SaveProvider.SetLong / " +
                             "GetLong / Save / Load / OnDataSaved; FileSaveProvider.HasUnsavedChanges / FilePath.",
                SceneWorkflow = "'Save Demo' holds SaveManager and SaveProviderSettingsComponent with " +
                                "Data/Save/SaveDemoProviderSettings (File provider, " + SaveFileName + "). Buttons call " +
                                "SaveDemoController or SaveManager.FlushNow through persistent UnityEvents.",
                Verification = "Change values (Unsaved changes: YES), Save, stop and re-enter Play Mode: the values " +
                               "return. Save twice: two OnDataSaved events, one file write.",
                Hints = new[]
                {
                    "'Save Demo' -> SaveProviderSettingsComponent -> SaveDemoProviderSettings (File provider)",
                    "Btn_FlushNow (NoCode) -> SaveManager.FlushNow; other buttons -> SaveDemoController",
                    "Save twice: OnDataSaved ×2, file written ×1 (FileSaveProvider write batching)"
                }
            };
        }
    }
}
