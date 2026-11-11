using System;
using Neo.Samples.Survivor;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored InventoryUseDemo scene: three ordinary slot-grid inventories (temporary catch,
    ///     nine-slot hotbar, one-slot podium), an <see cref="InventoryHand" /> and an <see cref="InventoryUseAction" />.
    ///     The whole UI is saved in the scene: slot rows are
    ///     <see cref="InventorySlotGridView" /> cells, slot buttons call <see cref="InventoryHand.SetSlotIndex" />
    ///     directly, count labels are <see cref="InventoryTotalCountText" /> / <see cref="InventoryItemCountText" />,
    ///     and the module events reach this component through persistent listeners. This component only keeps the
    ///     sample's game rules (specimen payloads, podium placement, HP) and mirrors them into authored labels.
    ///     It builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Inventory Demo")]
    public sealed class InventoryDemoController : MonoBehaviour
    {
        /// <summary>Item id of the unique specimen instances.</summary>
        public const int SpecimenId = 101;

        /// <summary>Item id of the stackable energy cells.</summary>
        public const int CellId = 202;

        /// <summary>Number of hotbar slots the sample UI shows.</summary>
        public const int HotbarSlots = 9;

        private const int MaxHealth = 100;
        private const int StartHealth = 45;
        private const int HealPerCell = 25;
        private const int DamagePerHit = 20;
        private const int StartCells = 3;

        private const string Instructions = "1 Deposit catch.  2 Select PET, then Use to place it.\n" +
                                            "3 Select CELL, then Use: gain 25 HP and spend one CELL.";

        private const string DoneText = "Done: specimen placed and CELL used to restore HP.\n" +
                                        "Recall keeps UID/state. Collect more to test full bags.";

        [Header("Inventories")]
        [SerializeField] private InventoryComponent _catchBag;
        [SerializeField] private InventoryComponent _hotbar;
        [SerializeField] private InventoryComponent _podium;
        [SerializeField] private InventoryHand _hand;
        [SerializeField] private InventoryUseAction _cellUse;

        [Header("Authored UI")]
        [SerializeField] private TMP_Text _goalLabel;
        [SerializeField] private TMP_Text _selectedLabel;
        [SerializeField] private TMP_Text _podiumLabel;
        [SerializeField] private TMP_Text _healthLabel;
        [SerializeField] private Image _healthBar;
        [Tooltip("Highlight object per hotbar slot; the one of the hand's slot is shown.")]
        [SerializeField] private GameObject[] _slotHighlights = Array.Empty<GameObject>();

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        [Header("Keyboard")]
        [Tooltip("Keys 1-9 select a hotbar slot, E runs Use selected.")]
        [SerializeField] private bool _keyboardShortcuts = true;

        [Serializable]
        private sealed class SpecimenState
        {
            public int Level;
            public string Mutation;
        }

        private int _health;
        private int _nextSpecimen;
        private bool _placed;
        private bool _healed;
        private bool _started;
        private bool _warnedUnbuilt;

        /// <summary>Current sample character HP; inventory runtime does not own this gameplay rule.</summary>
        public int Health => _health;

        /// <summary>Whether placement and consumable use have both been demonstrated in this run.</summary>
        public bool IsGoalComplete => _placed && _healed;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => HasInventories && _goalLabel != null && _selectedLabel != null &&
                                    _podiumLabel != null && _healthBar != null && _log != null;

        private bool HasInventories => _catchBag != null && _hotbar != null && _podium != null && _hand != null;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            if (!HasInventories)
            {
                enabled = false;
                return;
            }

            _started = true;
            ResetDemo();
        }

        private void Update()
        {
            if (!_keyboardShortcuts || !_started)
            {
                return;
            }

            for (int i = 0; i < HotbarSlots; i++)
            {
                if (KeyInputCompat.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                {
                    SelectSlot(i);
                }
            }

            if (KeyInputCompat.GetKeyDown(KeyCode.E))
            {
                UseSelected();
            }
        }

        /// <summary>Button target: adds one unique specimen (level + mutation in its JSON state) to the catch.</summary>
        public void Collect()
        {
            if (!_started)
            {
                return;
            }

            SpecimenState state = new()
            {
                Level = 1 + _nextSpecimen % 3,
                Mutation = _nextSpecimen % 2 == 0 ? "Golden" : "Diamond"
            };
            InventoryItemInstance instance = new(SpecimenId);
            instance.ComponentStates.Add(new InventoryItemComponentState("specimen", JsonUtility.ToJson(state)));
            if (_catchBag.AddItemInstance(instance) > 0)
            {
                _nextSpecimen++;
                Log("Collected " + Describe(new InventoryItemRecord(instance)) + " into the temporary catch.");
            }
            else
            {
                Log("Catch bag full. Deposit it first; no specimen was removed from another container.");
            }

            Refresh();
        }

        /// <summary>Button target: moves every catch slot into the first free hotbar slot (exact instances).</summary>
        public void Deposit()
        {
            if (!_started)
            {
                return;
            }

            int moved = 0;
            for (int i = 0; i < _catchBag.SlotCapacity; i++)
            {
                int target = FirstEmpty(_hotbar);
                if (target < 0)
                {
                    break;
                }

                if (_catchBag.TryCaptureSelection(i, out InventorySelection selection))
                {
                    moved += InventoryTransferService.TransferSelection(_catchBag, selection, _hotbar, target);
                }
            }

            Log(moved > 0
                ? "Deposited " + moved + ". UID, level and mutation travel with each specimen."
                : "Nothing deposited. Empty catch or full hotbar; unaccepted items stay in the catch.");
            Refresh();
        }

        /// <summary>
        ///     Button / E target: a PET is moved to the podium, a CELL goes through
        ///     <see cref="InventoryHand.UseEquippedItem" /> (and the InventoryUseAction wired to it).
        /// </summary>
        public void UseSelected()
        {
            if (!_started)
            {
                return;
            }

            if (!_hand.TryGetEquippedSelection(out InventorySelection selection))
            {
                Log("This slot is empty. Click a PET or CELL slot first.");
                return;
            }

            if (selection.ItemId == SpecimenId)
            {
                if (!_podium.IsEmpty)
                {
                    Log("Podium occupied. Recall that specimen before placing another.");
                    return;
                }

                if (InventoryTransferService.TransferSelection(_hotbar, selection, _podium, 0) > 0)
                {
                    _placed = true;
                    Log("Placed " + Describe(selection.Record) + ". It left its exact hotbar slot.");
                }
            }
            else if (selection.ItemId == CellId)
            {
                if (_health >= MaxHealth)
                {
                    Log("HP is full. Take damage first; the CELL was kept.");
                }
                else
                {
                    _hand.UseEquippedItem();
                }
            }

            Refresh();
        }

        /// <summary>Button target: returns the podium specimen to the first free hotbar slot and selects it.</summary>
        public void Recall()
        {
            if (!_started)
            {
                return;
            }

            int target = FirstEmpty(_hotbar);
            if (target < 0)
            {
                Log("Hotbar full. The specimen stays safely on the podium.");
                return;
            }

            if (_podium.TryCaptureSelection(0, out InventorySelection selection) &&
                InventoryTransferService.TransferSelection(_podium, selection, _hotbar, target) > 0)
            {
                _hand.SetSlotIndex(target);
                Log("Recalled " + Describe(selection.Record) + ". Compare the same UID and state.");
            }
            else
            {
                Log("The podium is empty. Select a PET and Use to place one.");
            }

            Refresh();
        }

        /// <summary>Button target: the sample character takes damage so a CELL can heal.</summary>
        public void Damage()
        {
            if (!_started)
            {
                return;
            }

            _health = Mathf.Max(0, _health - DamagePerHit);
            Log("Took " + DamagePerHit + " damage. Select CELL and Use to restore health.");
            Refresh();
        }

        /// <summary>Button target: clears all containers and restores the starting scenario.</summary>
        public void ResetDemo()
        {
            if (!_started)
            {
                return;
            }

            _catchBag.ClearInventory();
            _hotbar.ClearInventory();
            _podium.ClearInventory();
            _placed = false;
            _healed = false;
            _health = StartHealth;
            _nextSpecimen = 0;
            _hotbar.AddItemByIdAmount(CellId, StartCells);
            Collect();
            Collect();
            _hand.SetSlotIndex(0);
            Log("Fresh run: 2 different PET instances in catch, 3 CELL in slot 1. Start with Deposit catch.");
            Refresh();
        }

        /// <summary>C# / keyboard path of the slot buttons (they call InventoryHand.SetSlotIndex directly).</summary>
        public void SelectSlot(int slot)
        {
            if (!_started)
            {
                return;
            }

            _hand.SetSlotIndex(slot);
            Refresh();
        }

        /// <summary>Persistent listener of InventoryUseAction.OnUsed: the committed CELL restores HP.</summary>
        public void OnCellUsed(InventoryItemRecord record)
        {
            if (!_started || record == null || record.EffectiveItemId != CellId)
            {
                return;
            }

            _health = Mathf.Min(MaxHealth, _health + HealPerCell);
            _healed = true;
            Log("CELL used: +" + HealPerCell + " HP. Exactly one unit was consumed from the selected slot.");
            Refresh();
        }

        /// <summary>Persistent listener of InventoryHand.OnEquippedChanged.</summary>
        public void OnEquippedChanged(int itemId)
        {
            Refresh();
        }

        /// <summary>Persistent listener of the inventories' OnInventoryChanged: mirrors state into the labels.</summary>
        public void Refresh()
        {
            if (!_started)
            {
                return;
            }

            int selectedSlot = _hand.SlotIndex;
            if (_selectedLabel != null)
            {
                InventoryItemRecord selected = selectedSlot >= 0 ? _hotbar.GetPhysicalSlotRecord(selectedSlot) : null;
                _selectedLabel.text = "Slot " + (selectedSlot + 1) + " · " + Describe(selected);
            }

            if (_podiumLabel != null)
            {
                _podiumLabel.text = _podium.IsEmpty
                    ? "Empty · select a PET and Use"
                    : Describe(_podium.GetPhysicalSlotRecord(0));
            }

            if (_healthLabel != null)
            {
                _healthLabel.text = "HP " + _health + " / " + MaxHealth + " · a CELL restores " + HealPerCell;
            }

            SurvivorUI.SetFill(_healthBar, _health / (float)MaxHealth);
            for (int i = 0; i < _slotHighlights.Length; i++)
            {
                GameObject highlight = _slotHighlights[i];
                if (highlight != null && highlight.activeSelf != (i == selectedSlot))
                {
                    highlight.SetActive(i == selectedSlot);
                }
            }

            if (_goalLabel != null)
            {
                _goalLabel.text = IsGoalComplete ? DoneText : Instructions;
            }
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[InventoryDemoController] The scene UI or inventories are not authored. " +
                "Re-import the scene from the Demo download.", this);
        }

        private static int FirstEmpty(InventoryComponent inventory)
        {
            for (int i = 0; i < inventory.SlotCapacity; i++)
            {
                if (inventory.GetSlot(i).IsEmpty)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string Describe(InventoryItemRecord record)
        {
            if (record == null || record.EffectiveItemId < 0 || record.EffectiveCount <= 0)
            {
                return "Empty";
            }

            if (!record.IsInstance)
            {
                return "CELL ×" + record.EffectiveCount;
            }

            SpecimenState state = record.Instance.ComponentStates.Count > 0
                ? JsonUtility.FromJson<SpecimenState>(record.Instance.ComponentStates[0].Json)
                : null;
            string uid = record.Instance.InstanceId;
            string shortUid = string.IsNullOrEmpty(uid) ? "missing" : uid.Substring(0, Mathf.Min(6, uid.Length));
            return (state != null ? state.Mutation + " Lv" + state.Level : "PET") + " · UID " + shortUid;
        }
    }
}
