using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests.Tools
{
    public sealed class Audit20261023NeoToolsFakeLeaderboardLifecycleTests
    {
        private GameObject _root;
        private LeaderboardMove _move;
        private ISaveProvider _originalProvider;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_move != null)
            {
                _move.KillActiveTweens();
            }

            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }

            if (_originalProvider != null)
            {
                SaveProvider.SetProvider(_originalProvider);
                _originalProvider = null;
            }

            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }
        }

        [UnityTest]
        public IEnumerator Start_LoadsCanonicalPlayerAndBindsGeneratedRowsDespiteStaleSnapshot()
        {
            yield return new EnterPlayMode();
            Leaderboard board = CreateBoard();
            _originalProvider = SaveProvider.CurrentProvider;
            Audit20261023NeoToolsFakeLeaderboardSaveProvider provider =
                new Audit20261023NeoToolsFakeLeaderboardSaveProvider();
            SaveProvider.SetProvider(provider);
            board.playerSaveKey = "Audit20261023NeoToolsFakeLeaderboardLifecycle";
            provider.SetString(board.playerSaveKey + "_Name", "Loaded");
            provider.SetInt(board.playerSaveKey + "_Score", 20);
            LeaderboardUser bot = new LeaderboardUser("Bot", 100);
            board.users = new List<LeaderboardUser> { bot };
            board.sortUsers = new List<LeaderboardUser> { bot };
            board.generateLeaderboardOnAwake = true;
            board.onAwakeSort = false;
            board.prefab = CreateRow("Template");
            board.leaderboardItemPlayer = CreateRow("DedicatedPlayer");

            yield return null;

            Assert.That(board.leaderboardItems, Has.Count.EqualTo(2));
            Assert.That(board.leaderboardItems[1].user, Is.SameAs(board.player));
            Assert.That(board.player.name, Is.EqualTo("Loaded"));
            Assert.That(board.player.score, Is.EqualTo(20));
            Assert.That(board.leaderboardItemPlayer.gameObject.activeSelf, Is.True);
            Assert.That(board.leaderboardItemPlayer.user, Is.SameAs(board.player));
            Assert.That(board.leaderboardItemPlayer.id, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Start_RebindsAggregateAfterExistingSaveKeysOverlayPlayerValues()
        {
            yield return new EnterPlayMode();
            Leaderboard board = CreateBoard();
            LeaderboardUser bot = new LeaderboardUser("Shared", 100);
            board.player.name = "Shared";
            board.player.score = 5;
            Audit20261023NeoToolsFakeLeaderboardAggregate original =
                new Audit20261023NeoToolsFakeLeaderboardAggregate
                {
                    player = board.player,
                    users = new List<LeaderboardUser> { bot, board.player },
                    sortUsers = new List<LeaderboardUser> { board.player }
                };
            Audit20261023NeoToolsFakeLeaderboardAggregate restored =
                JsonUtility.FromJson<Audit20261023NeoToolsFakeLeaderboardAggregate>(JsonUtility.ToJson(original));
            board.player = restored.player;
            board.users = restored.users;
            board.sortUsers = restored.sortUsers;
            board.onAwakeSort = true;
            _originalProvider = SaveProvider.CurrentProvider;
            Audit20261023NeoToolsFakeLeaderboardSaveProvider provider =
                new Audit20261023NeoToolsFakeLeaderboardSaveProvider();
            SaveProvider.SetProvider(provider);
            board.playerSaveKey = "Audit20261023NeoToolsFakeLeaderboardPersistentIdentity";
            provider.SetString(board.playerSaveKey + "_Name", "SavedName");
            provider.SetInt(board.playerSaveKey + "_Score", 30);

            yield return null;

            Assert.That(board.users, Has.Count.EqualTo(2));
            Assert.That(board.users[1], Is.SameAs(board.player));
            Assert.That(board.player.name, Is.EqualTo("SavedName"));
            Assert.That(board.player.score, Is.EqualTo(30));
            Assert.That(board.users[0].name, Is.EqualTo("Shared"));
            Assert.That(board.users[0].score, Is.EqualTo(100));
            Assert.That(board.sortUsers[board.GetIdPlayer()], Is.SameAs(board.player));
            Assert.That(provider.HasKey(board.playerSaveKey + "_Id"), Is.False);
            Assert.That(provider.HasKey(board.playerSaveKey + "_Snapshot"), Is.False);
        }

        [UnityTest]
        public IEnumerator Start_EmptyRuntimeBoardGeneratesConfiguredUsers()
        {
            yield return new EnterPlayMode();
            Leaderboard board = CreateBoard();
            board.count = 3;
            board.names = new[] { "Bot" };
            board.users.Clear();
            board.generateLeaderboardOnAwake = true;
            board.prefab = CreateRow("Template");

            yield return null;

            Assert.That(board.users, Has.Count.EqualTo(3));
            Assert.That(board.leaderboardItems, Has.Count.EqualTo(3));
            Assert.That(board.leaderboardItems[board.GetIdPlayer()].user, Is.SameAs(board.player));
        }

        [UnityTest]
        public IEnumerator Disable_CancelsScheduledMoveBeforeItsDelayElapses()
        {
            yield return new EnterPlayMode();
            Leaderboard board = CreateBoard();
            LeaderboardItem row = CreateRow("Player");
            board.leaderboardItems.Add(row);
            board.Sort();
            CreateMove(board, true);
            Assert.That(_move.IsInvoking(nameof(LeaderboardMove.Move)), Is.True);
            Vector3 position = _move.transform.position;

            _move.enabled = false;

            Assert.That(_move.IsInvoking(nameof(LeaderboardMove.Move)), Is.False);
            yield return new WaitForSeconds(0.1f);
            Assert.That(_move.transform.position, Is.EqualTo(position));
            Assert.That(DOTween.IsTweening(_move.transform), Is.False);
        }

        [UnityTest]
        public IEnumerator Move_RestartAndDisableRestoreOriginalScaleWithoutAccumulation()
        {
            yield return new EnterPlayMode();
            Leaderboard board = CreateBoard();
            LeaderboardItem row = CreateRow("Player");
            Vector3 baseScale = new Vector3(2f, 3f, 4f);
            row.transform.localScale = baseScale;
            board.leaderboardItems.Add(row);
            board.Sort();
            CreateMove(board, false);
            _move.durationAnimPlayer = 1f;

            _move.Move();
            GetOnlyTween(_move.transform).Complete();
            Tween scaleTween = GetOnlyTween(row.transform);
            scaleTween.Goto(0.5f);
            Vector3 interruptedScale = row.transform.localScale;
            Assert.That(interruptedScale.x, Is.GreaterThan(baseScale.x));

            _move.Move();
            Assert.That(row.transform.localScale, Is.EqualTo(baseScale));
            GetOnlyTween(_move.transform).Complete();
            Tween restartedScaleTween = GetOnlyTween(row.transform);
            restartedScaleTween.Goto(0.5f);
            Assert.That(Vector3.Distance(row.transform.localScale, interruptedScale), Is.LessThan(0.0001f));

            _move.enabled = false;

            Assert.That(row.transform.localScale, Is.EqualTo(baseScale));
            Assert.That(restartedScaleTween.IsActive(), Is.False);
        }

        private Leaderboard CreateBoard()
        {
            _root = new GameObject(nameof(Audit20261023NeoToolsFakeLeaderboardLifecycleTests));
            Leaderboard board = _root.AddComponent<Leaderboard>();
            board.playerSaveKey = string.Empty;
            board.generateLeaderboardOnAwake = false;
            board.onAwakeSort = false;
            board.container = _root.transform;
            board.player = new LeaderboardUser("Player");
            board.users = new List<LeaderboardUser> { board.player };
            board.leaderboardItems = new List<LeaderboardItem>();
            return board;
        }

        private LeaderboardItem CreateRow(string name)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(_root.transform);
            return row.AddComponent<LeaderboardItem>();
        }

        private void CreateMove(Leaderboard board, bool scheduleMove)
        {
            GameObject mover = new GameObject("Mover");
            mover.transform.SetParent(_root.transform);
            mover.SetActive(false);
            _move = mover.AddComponent<LeaderboardMove>();
            _move.leaderboard = board;
            _move.useSortEnable = false;
            _move.useMove = scheduleMove;
            _move.delayTime = 0.05f;
            mover.SetActive(true);
        }

        private static Tween GetOnlyTween(Transform target)
        {
            List<Tween> tweens = DOTween.TweensByTarget(target);
            Assert.That(tweens, Is.Not.Null.And.Count.EqualTo(1));
            return tweens[0];
        }
    }
}
