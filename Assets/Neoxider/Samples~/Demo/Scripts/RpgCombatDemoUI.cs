using Neo.Rpg;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Demo UI for the RpgCombatant system using OnGUI.
    ///     Shows HP, Level, status, and buttons to interact with two combatants.
    /// </summary>
    public class RpgCombatDemoUI : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private RpgCombatant _player;

        [SerializeField] private RpgCombatant _enemy;

        [Header("Settings")] [SerializeField] private float _attackDamage = 25f;
        [SerializeField] private float _healAmount = 30f;
        [SerializeField] private float _hpUpgradeAmount = 50f;

        private string _log = "";
        private Vector2 _scrollPos;

        private void Start()
        {
            if (_player != null)
            {
                _player.OnDamaged.AddListener(dmg => Log($"[PLAYER] took {dmg:F0} damage!"));
                _player.OnHealed.AddListener(hp => Log($"[PLAYER] healed {hp:F0} HP."));
                _player.OnDeath.AddListener(() => Log("☠ [PLAYER] DIED!"));
            }

            if (_enemy != null)
            {
                _enemy.OnDamaged.AddListener(dmg => Log($"[ENEMY] took {dmg:F0} damage!"));
                _enemy.OnHealed.AddListener(hp => Log($"[ENEMY] healed {hp:F0} HP."));
                _enemy.OnDeath.AddListener(() => Log("☠ [ENEMY] DEFEATED!"));
            }

            Log("RPG Combat Demo started. Interact via the UI panel!");
        }

        private void OnGUI()
        {
            float w = Screen.width;
            float h = Screen.height;

            GUI.skin.label.richText = true;
            var titleStyle = new GUIStyle(GUI.skin.label)
                { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var headerStyle = new GUIStyle(GUI.skin.label)
                { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleLeft };
            var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 22 };
            var logStyle = new GUIStyle(GUI.skin.box)
                { fontSize = 20, alignment = TextAnchor.UpperLeft, richText = true };

            int padding = 40;
            var rect = new Rect(padding, padding, w - padding * 2, h - padding * 2);

            GUILayout.BeginArea(rect, "", GUI.skin.window);
            GUILayout.Space(20);
            GUILayout.Label("⚔ RPG Combat Demo ⚔", titleStyle);
            GUILayout.Space(30);

            GUILayout.BeginHorizontal();

            float colWidth = rect.width / 2f - 20f;

            // Player Column
            GUILayout.BeginVertical("box", GUILayout.Width(colWidth));
            DrawCombatantUI(_player, "<color=#2EA3FF>PLAYER</color>", headerStyle, labelStyle);
            GUILayout.Space(20);
            if (GUILayout.Button($"Attack Enemy ({_attackDamage:F0} DMG)", btnStyle, GUILayout.Height(50)))
            {
                PlayerAttackEnemy();
            }

            if (GUILayout.Button($"Heal ({_healAmount:F0} HP)", btnStyle, GUILayout.Height(50)))
            {
                HealPlayer();
            }

            if (GUILayout.Button($"Max HP +{_hpUpgradeAmount:F0}", btnStyle, GUILayout.Height(50)))
            {
                if (_player != null)
                {
                    _player.IncreaseMaxHp(_hpUpgradeAmount);
                }
            }

            if (GUILayout.Button("Toggle Invulnerable", btnStyle, GUILayout.Height(50)))
            {
                TogglePlayerInvulnerable();
            }

            if (GUILayout.Button($"Level Up (HP +{_hpUpgradeAmount:F0})", btnStyle, GUILayout.Height(50)))
            {
                PlayerLevelUp();
            }

            GUILayout.EndVertical();

            // Enemy Column
            GUILayout.BeginVertical("box", GUILayout.Width(colWidth));
            DrawCombatantUI(_enemy, "<color=#FF4444>ENEMY</color>", headerStyle, labelStyle);
            GUILayout.Space(20);
            if (GUILayout.Button($"Attack Player ({_attackDamage * 0.8f:F0} DMG)", btnStyle, GUILayout.Height(50)))
            {
                EnemyAttackPlayer();
            }

            if (GUILayout.Button($"Heal ({_healAmount:F0} HP)", btnStyle, GUILayout.Height(50)))
            {
                HealEnemy();
            }

            if (GUILayout.Button($"Max HP +{_hpUpgradeAmount:F0}", btnStyle, GUILayout.Height(50)))
            {
                if (_enemy != null)
                {
                    _enemy.IncreaseMaxHp(_hpUpgradeAmount);
                }
            }

            GUILayout.Space(50); // Empty space to match player column height for level up button alignment
            if (GUILayout.Button($"Level Up (HP +{_hpUpgradeAmount:F0})", btnStyle, GUILayout.Height(50)))
            {
                EnemyLevelUp();
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Restore Full HP for Both", btnStyle, GUILayout.Height(55), GUILayout.Width(500)))
            {
                RestoreAll();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(30);
            GUILayout.Label("<b>Combat Log:</b>", labelStyle);

            // Expand the scroll view to fill the rest of the window screen height
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
            GUILayout.Label(_log, logStyle, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawCombatantUI(RpgCombatant c, string name, GUIStyle headerStyle, GUIStyle labelStyle)
        {
            GUILayout.Space(10);
            GUILayout.Label(name, headerStyle);
            GUILayout.Space(15);

            if (c == null)
            {
                GUILayout.Label("Not assigned", labelStyle);
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>Level:</b> {c.Level}", labelStyle);
            string status = c.IsDead ? "<color=red>DEAD</color>" :
                c.IsInvulnerable ? "<color=cyan>INVULNERABLE</color>" : "<color=green>Alive</color>";
            GUILayout.Label($"<b>Status:</b> {status}", labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label($"<b>HP:</b> {c.CurrentHp:F0} / {c.MaxHp:F0}", labelStyle);

            float hpFill = c.MaxHp > 0 ? c.CurrentHp / c.MaxHp : 0;
            Rect barRect = GUILayoutUtility.GetRect(18, 30);
            GUI.Box(barRect, "");
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            GUI.Box(barRect, ""); // background
            GUI.color = c.IsInvulnerable ? Color.cyan :
                hpFill > 0.5f ? Color.green :
                hpFill > 0.2f ? Color.yellow : Color.red;
            GUI.Box(new Rect(barRect.x, barRect.y, barRect.width * hpFill, barRect.height), "");
            GUI.color = Color.white;

            GUILayout.Space(15);

            string buffEffects = $"<b>Buffs:</b> {c.ActiveBuffs.Count}   <b>Statuses:</b> {c.ActiveStatuses.Count}";
            GUILayout.Label(buffEffects, labelStyle);
        }

        private void PlayerAttackEnemy()
        {
            if (_enemy == null || _enemy.IsDead)
            {
                return;
            }

            float damage = _attackDamage * (_player != null ? _player.GetOutgoingDamageMultiplier() : 1f);
            _enemy.TakeDamage(damage);
            Log($"[PLAYER] attacks [ENEMY] for <color=orange>{damage:F0}</color> dmg!");
        }

        private void EnemyAttackPlayer()
        {
            if (_player == null || _player.IsDead)
            {
                return;
            }

            float damage = _attackDamage * 0.8f * (_enemy != null ? _enemy.GetOutgoingDamageMultiplier() : 1f);
            _player.TakeDamage(damage);
            Log($"[ENEMY] attacks [PLAYER] for <color=orange>{damage:F0}</color> dmg!");
        }

        private void HealPlayer()
        {
            if (_player != null && !_player.IsDead)
            {
                _player.Heal(_healAmount);
            }
        }

        private void HealEnemy()
        {
            if (_enemy != null && !_enemy.IsDead)
            {
                _enemy.Heal(_healAmount);
            }
        }

        private void RestoreAll()
        {
            _player?.Restore();
            _enemy?.Restore();
            Log("All combatants restored to full HP.");
        }

        private void TogglePlayerInvulnerable()
        {
            if (_player == null)
            {
                return;
            }

            if (_player.IsInvulnerable)
            {
                _player.RemoveInvulnerabilityLock();
                Log("[PLAYER] invulnerability OFF");
            }
            else
            {
                _player.AddInvulnerabilityLock();
                Log("[PLAYER] invulnerability <color=cyan>ON</color> ⚡");
            }
        }

        private void PlayerLevelUp()
        {
            if (_player == null)
            {
                return;
            }

            _player.SetLevel(_player.Level + 1);
            _player.IncreaseMaxHp(_hpUpgradeAmount);
            Log($"[PLAYER] Leveled up to {_player.Level}! Max HP increased to {_player.MaxHp:F0}.");
        }

        private void EnemyLevelUp()
        {
            if (_enemy == null)
            {
                return;
            }

            _enemy.SetLevel(_enemy.Level + 1);
            _enemy.IncreaseMaxHp(_hpUpgradeAmount);
            Log($"[ENEMY] Leveled up to {_enemy.Level}! Max HP increased to {_enemy.MaxHp:F0}.");
        }

        private void Log(string msg)
        {
            _log = $"[{Time.time:F1}] {msg}\n{_log}";
            if (_log.Length > 2000)
            {
                _log = _log.Substring(0, 2000);
            }
        }
    }
}
