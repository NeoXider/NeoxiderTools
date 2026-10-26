using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Neo.Cards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoCardsEditorTests
    {
        private readonly List<Sprite> _sprites = new();
        private DeckConfig _config;
        private UnityEditor.Editor _editor;
        private Texture2D _texture;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<DeckConfig>();
            _editor = UnityEditor.Editor.CreateEditor(_config);
            _texture = new Texture2D(2, 2);
            Assert.That(_editor.GetType().FullName, Is.EqualTo("Neo.Cards.Editor.DeckConfigEditor"));
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
            {
                Undo.ClearUndo(_config);
            }

            Object.DestroyImmediate(_editor);
            Object.DestroyImmediate(_config);
            foreach (Sprite sprite in _sprites)
            {
                Object.DestroyImmediate(sprite);
            }

            _sprites.Clear();
            Object.DestroyImmediate(_texture);
        }

        [TestCase("hearts_02", "2_of_hearts", CardSpriteKind.Card)]
        [TestCase("card_back", "back", CardSpriteKind.Back)]
        [TestCase("joker_red", "red_joker", CardSpriteKind.JokerRed)]
        [TestCase("joker_black", "black_joker", CardSpriteKind.JokerBlack)]
        public void AutoFill_DuplicateSlots_KeepFirstSpriteAndReportTheOther(
            string firstName, string duplicateName, CardSpriteKind kind)
        {
            Sprite previous = CreateSprite("previous");
            SerializedObject serialized = _editor.serializedObject;
            serialized.FindProperty("_backSprite").objectReferenceValue = previous;
            serialized.FindProperty("_redJoker").objectReferenceValue = previous;
            serialized.FindProperty("_blackJoker").objectReferenceValue = previous;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Sprite first = CreateSprite(firstName);
            Sprite duplicate = CreateSprite(duplicateName);

            ApplyAutoFill(new List<Sprite> { first, duplicate }, out int assigned,
                out List<string> unrecognized, out List<string> conflicts);

            Sprite actual = kind switch
            {
                CardSpriteKind.Card => _config.GetSprite(new CardData(Suit.Hearts, Rank.Two)),
                CardSpriteKind.Back => _config.BackSprite,
                CardSpriteKind.JokerRed => _config.RedJoker,
                CardSpriteKind.JokerBlack => _config.BlackJoker,
                _ => null
            };
            Assert.That(actual, Is.SameAs(first), "The first folder candidate replaces any previous assignment.");
            Assert.That(conflicts, Is.EqualTo(new[] { duplicateName }));
            Assert.That(unrecognized, Is.Empty);
            Assert.That(assigned, Is.EqualTo(kind == CardSpriteKind.Card ? 1 : 0));
        }

        [Test]
        public void AutoFill_RepeatedSpriteReference_CountsOneAssignedCard()
        {
            Sprite card = CreateSprite("hearts_02");

            ApplyAutoFill(new List<Sprite> { card, card }, out int assigned,
                out List<string> unrecognized, out List<string> conflicts);

            Assert.That(_config.GetSprite(new CardData(Suit.Hearts, Rank.Two)), Is.SameAs(card));
            Assert.That(assigned, Is.EqualTo(1));
            Assert.That(unrecognized, Is.Empty);
            Assert.That(conflicts, Is.Empty);
        }

        [Test]
        public void AutoFill_Standard36_MapsRanksAndPreservesCustomEntriesAndMissingSpecials()
        {
            Sprite previous = CreateSprite("previous");
            SerializedObject serialized = _editor.serializedObject;
            serialized.FindProperty("_deckType").intValue = (int)DeckType.Standard36;
            serialized.FindProperty("_useCustomDeck").boolValue = true;
            serialized.FindProperty("_backSprite").objectReferenceValue = previous;
            serialized.FindProperty("_redJoker").objectReferenceValue = previous;
            serialized.FindProperty("_blackJoker").objectReferenceValue = previous;
            SerializedProperty hearts = serialized.FindProperty("_hearts");
            hearts.arraySize = 13;
            hearts.GetArrayElementAtIndex(0).objectReferenceValue = previous;
            SerializedProperty customCards = serialized.FindProperty("_customCards");
            customCards.arraySize = 1;
            SerializedProperty customCard = customCards.GetArrayElementAtIndex(0);
            customCard.FindPropertyRelative("_id").stringValue = "seed-card";
            customCard.FindPropertyRelative("_sprite").objectReferenceValue = previous;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Sprite six = CreateSprite("hearts_06");
            Sprite ace = CreateSprite("AS");

            ApplyAutoFill(new List<Sprite>
                {
                    six, ace, CreateSprite("clubs_05"), CreateSprite("unsupported_art")
                }, out int assigned, out List<string> unrecognized, out List<string> conflicts);

            Assert.That(assigned, Is.EqualTo(2));
            Assert.That(_config.Hearts.Count, Is.EqualTo(9));
            Assert.That(_config.Diamonds.Count, Is.EqualTo(9));
            Assert.That(_config.Clubs.Count, Is.EqualTo(9));
            Assert.That(_config.Spades.Count, Is.EqualTo(9));
            Assert.That(_config.GetSprite(new CardData(Suit.Hearts, Rank.Six)), Is.SameAs(six));
            Assert.That(_config.GetSprite(new CardData(Suit.Spades, Rank.Ace)), Is.SameAs(ace));
            Assert.That(_config.GetSprite(new CardData(Suit.Hearts, Rank.Seven)), Is.Null);
            Assert.That(unrecognized, Is.EqualTo(new[] { "clubs_05 (rank outside Standard36)", "unsupported_art" }));
            Assert.That(conflicts, Is.Empty);
            Assert.That(_config.BackSprite, Is.SameAs(previous));
            Assert.That(_config.RedJoker, Is.SameAs(previous));
            Assert.That(_config.BlackJoker, Is.SameAs(previous));
            Assert.That(_config.UseCustomDeck, Is.True);
            Assert.That(_config.CustomCards.Count, Is.EqualTo(1));
            Assert.That(_config.CustomCards[0].Sprite, Is.SameAs(previous));
            List<CardData> generated = _config.GenerateDeck();
            Assert.That(generated.Count, Is.EqualTo(1));
            Assert.That(generated[0].CustomId, Is.EqualTo("seed-card"));
        }

        [Test]
        public void AutoFill_UndoAndRedo_RestoreSuitAndBackAssignments()
        {
            Sprite previous = CreateSprite("previous");
            SerializedObject serialized = _editor.serializedObject;
            SerializedProperty hearts = serialized.FindProperty("_hearts");
            hearts.arraySize = 1;
            hearts.GetArrayElementAtIndex(0).objectReferenceValue = previous;
            serialized.FindProperty("_backSprite").objectReferenceValue = previous;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Sprite card = CreateSprite("hearts_02");
            Sprite back = CreateSprite("card_back");
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                ApplyAutoFill(new List<Sprite> { card, back }, out _, out _, out _);
                Assert.That(_config.Hearts.Count, Is.EqualTo(13));
                Assert.That(_config.Hearts[0], Is.SameAs(card));
                Assert.That(_config.BackSprite, Is.SameAs(back));
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(undoGroup);

                Undo.PerformUndo();
                Assert.That(_config.Hearts.Count, Is.EqualTo(1));
                Assert.That(_config.Hearts[0], Is.SameAs(previous));
                Assert.That(_config.BackSprite, Is.SameAs(previous));

                Undo.PerformRedo();
                Assert.That(_config.Hearts.Count, Is.EqualTo(13));
                Assert.That(_config.Hearts[0], Is.SameAs(card));
                Assert.That(_config.BackSprite, Is.SameAs(back));
            }
            finally
            {
                Undo.IncrementCurrentGroup();
            }
        }

        [Test]
        public void AssetFolder_ProjectAssetsRoot_IsAcceptedAsAnAssetDatabasePath()
        {
            bool accepted = TryGetAssetFolder(Application.dataPath, out string folder);

            Assert.That(accepted, Is.True);
            Assert.That(folder, Is.EqualTo("Assets"));
        }

        [Test]
        public void AssetFolder_BackslashesAndTrailingSeparator_NormalizeToAssets()
        {
            bool accepted = TryGetAssetFolder(Application.dataPath.Replace('/', '\\') + "\\", out string folder);

            Assert.That(accepted, Is.True);
            Assert.That(folder, Is.EqualTo("Assets"));
        }

        [TestCase("Backup")]
        [TestCase("-Sprites")]
        public void AssetFolder_SiblingWithAssetsPrefix_IsRejected(string suffix)
        {
            bool accepted = TryGetAssetFolder(Application.dataPath + suffix, out _);

            Assert.That(accepted, Is.False);
        }

        [Test]
        public void AssetFolder_ProjectRootOutsideAssets_IsRejected()
        {
            bool accepted = TryGetAssetFolder(Path.GetDirectoryName(Application.dataPath), out _);

            Assert.That(accepted, Is.False);
        }

        private Sprite CreateSprite(string name)
        {
            Sprite sprite = Sprite.Create(_texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            sprite.name = name;
            _sprites.Add(sprite);
            return sprite;
        }

        private void ApplyAutoFill(List<Sprite> sprites, out int assigned,
            out List<string> unrecognized, out List<string> conflicts)
        {
            // Exercise the same operation as the folder button without opening a modal native dialog.
            MethodInfo method = _editor.GetType().GetMethod("ApplyAutoFill",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            object[] arguments = { sprites, 0, null, null };
            method.Invoke(_editor, arguments);
            assigned = (int)arguments[1];
            unrecognized = (List<string>)arguments[2];
            conflicts = (List<string>)arguments[3];
        }

        private bool TryGetAssetFolder(string path, out string assetFolder)
        {
            MethodInfo method = _editor.GetType().GetMethod("TryGetAssetFolder",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            object[] arguments = { path, null };
            bool accepted = (bool)method.Invoke(null, arguments);
            assetFolder = (string)arguments[1];
            return accepted;
        }
    }
}
