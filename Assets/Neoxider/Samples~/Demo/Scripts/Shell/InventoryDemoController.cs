using System;
using Neo.Samples.Survivor;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>Sample view over three ordinary inventories; game-specific rules stay in this sample.</summary>
    public sealed class InventoryDemoController : MonoBehaviour
    {
        private const int SpecimenId = 101;
        private const int CellId = 202;
        private const string Instructions = "1 Deposit catch.  2 Select PET, then Use to place it.\n" +
            "3 Select CELL, then Use: gain 25 HP and spend one CELL.";
        [SerializeField] private InventoryComponent _catchBag;
        [SerializeField] private InventoryComponent _hotbar;
        [SerializeField] private InventoryComponent _podium;
        [SerializeField] private InventoryHand _hand;
        [SerializeField] private InventoryUseAction _cellUse;

        [Serializable]
        private sealed class SpecimenState
        {
            public int Level;
            public string Mutation;
        }

        private NeoDemoShell.Context _shell;
        private TMP_Text _catchLabel;
        private TMP_Text _podiumLabel;
        private TMP_Text _selectedLabel;
        private TMP_Text _goalLabel;
        private Image _healthBar;
        private Button[] _slots;
        private int _health;
        private int _nextSpecimen;
        private bool _placed;
        private bool _healed;

        /// <summary>Current sample character HP; inventory runtime does not own this gameplay rule.</summary>
        public int Health => _health;

        /// <summary>Whether placement and consumable use have both been demonstrated in this run.</summary>
        public bool IsGoalComplete => _placed && _healed;

        private void Start()
        {
            _shell = NeoDemoShell.Build("Inventory · collect, keep, use", new Color(0.3f, 0.85f, 0.7f), transform);
            _goalLabel = _shell.AddParagraph(Instructions);
            _catchLabel = _shell.AddValueLabel("Temporary catch (4 slots)");
            _podiumLabel = _shell.AddValueLabel("Display podium (1 slot)");
            _selectedLabel = _shell.AddParagraph("", 64f);
            _healthBar = _shell.AddBar("Your HP · a CELL restores 25 HP", new Color(0.35f, 0.85f, 0.5f));
            (string label, Action onClick)[] slotActions = new (string, Action)[9];
            for (int i = 0; i < slotActions.Length; i++)
            {
                int index = i;
                slotActions[i] = ((i + 1).ToString(), () => Select(index));
            }
            _slots = _shell.AddButtonRow(slotActions);
            _shell.AddButtonRow(("Collect specimen", Collect), ("Deposit catch", Deposit));
            _shell.AddButtonRow(("Use selected [E]", UseSelected), ("Recall from podium", Recall));
            _shell.AddButtonRow(("Take 20 damage", Damage), ("Reset demo", ResetDemo));
            _cellUse.OnUsed.AddListener(UseCell);
            _hotbar.OnInventoryChanged.AddListener(Refresh);
            _catchBag.OnInventoryChanged.AddListener(Refresh);
            _podium.OnInventoryChanged.AddListener(Refresh);
            _hand.OnEquippedChanged.AddListener(SelectionChanged);
            ResetDemo();
        }

        private void Update()
        {
            if (_shell == null) return;
            for (int i = 0; i < 9; i++)
            {
                if (KeyInputCompat.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) Select(i);
            }
            if (KeyInputCompat.GetKeyDown(KeyCode.E)) UseSelected();
        }

        public void Collect()
        {
            SpecimenState state = new() { Level = 1 + _nextSpecimen % 3,
                Mutation = _nextSpecimen % 2 == 0 ? "Golden" : "Diamond" };
            InventoryItemInstance instance = new(SpecimenId);
            instance.ComponentStates.Add(new InventoryItemComponentState("specimen", JsonUtility.ToJson(state)));
            if (_catchBag.AddItemInstance(instance) > 0)
            {
                _nextSpecimen++;
                _shell.Log("Collected " + Describe(new InventoryItemRecord(instance)) + " into the temporary catch.");
            }
            else _shell.Log("Catch bag full. Deposit it first; no specimen was removed from another container.");
            Refresh();
        }

        public void Deposit()
        {
            int moved = 0;
            for (int i = 0; i < _catchBag.SlotCapacity; i++)
            {
                int target = FirstEmpty(_hotbar);
                if (target < 0) break;
                if (_catchBag.TryCaptureSelection(i, out InventorySelection selection))
                    moved += InventoryTransferService.TransferSelection(_catchBag, selection, _hotbar, target);
            }
            _shell.Log(moved > 0 ? "Deposited " + moved + ". UID, level and mutation travel with each specimen."
                : "Nothing deposited. Empty catch or full hotbar; unaccepted items stay in the catch.");
            Refresh();
        }

        public void UseSelected()
        {
            if (!_hand.TryGetEquippedSelection(out InventorySelection selection))
            {
                _shell.Log("This slot is empty. Click a PET or CELL slot first.");
                return;
            }
            if (selection.ItemId == SpecimenId)
            {
                if (!_podium.IsEmpty)
                {
                    _shell.Log("Podium occupied. Recall that specimen before placing another.");
                    return;
                }
                if (InventoryTransferService.TransferSelection(_hotbar, selection, _podium, 0) > 0)
                {
                    _placed = true;
                    _shell.Log("Placed " + Describe(selection.Record) + ". It left its exact hotbar slot.");
                }
            }
            else if (selection.ItemId == CellId)
            {
                if (_health >= 100) _shell.Log("HP is full. Take damage first; the CELL was kept.");
                else _hand.UseEquippedItem();
            }
            Refresh();
        }

        public void Recall()
        {
            int target = FirstEmpty(_hotbar);
            if (target < 0) { _shell.Log("Hotbar full. The specimen stays safely on the podium."); return; }
            if (_podium.TryCaptureSelection(0, out InventorySelection selection) &&
                InventoryTransferService.TransferSelection(_podium, selection, _hotbar, target) > 0)
            {
                _hand.SetSlotIndex(target);
                _shell.Log("Recalled " + Describe(selection.Record) + ". Compare the same UID and state.");
            }
            else _shell.Log("The podium is empty. Select a PET and Use to place one.");
            Refresh();
        }

        private void UseCell(InventoryItemRecord record)
        {
            if (record.EffectiveItemId != CellId) return;
            _health = Mathf.Min(100, _health + 25);
            _healed = true;
            _shell.Log("CELL used: +25 HP. Exactly one unit was consumed from the selected slot.");
            Refresh();
        }

        public void Damage()
        {
            _health = Mathf.Max(0, _health - 20);
            _shell.Log("Took 20 damage. Select CELL and Use to restore health.");
            Refresh();
        }

        public void ResetDemo()
        {
            _catchBag.ClearInventory();
            _hotbar.ClearInventory();
            _podium.ClearInventory();
            _placed = false;
            _healed = false;
            _health = 45;
            _nextSpecimen = 0;
            _hotbar.AddItemByIdAmount(CellId, 3);
            Collect();
            Collect();
            _hand.SetSlotIndex(0);
            _shell.Log("Fresh run: 2 different PET instances in catch, 3 CELL in slot 1. Start with Deposit catch.");
            Refresh();
        }

        private void Select(int slot) { _hand.SetSlotIndex(slot); Refresh(); }
        private void SelectionChanged(int itemId) => Refresh();

        private void Refresh()
        {
            if (_shell == null || _slots == null) return;
            _catchLabel.text = _catchBag.TotalItemCount + " / " + _catchBag.SlotCapacity;
            _podiumLabel.text = _podium.IsEmpty ? "Empty · place a PET here" : Describe(_podium.GetPhysicalSlotRecord(0));
            InventoryItemRecord selected = _hotbar.GetPhysicalSlotRecord(_hand.SlotIndex);
            _selectedLabel.text = "Selected slot " + (_hand.SlotIndex + 1) + ": " + Describe(selected) +
                "\nHP " + _health + "/100 · select 1–9 or a slot; E / Use = action.";
            SurvivorUI.SetFill(_healthBar, _health / 100f);
            for (int i = 0; i < _slots.Length; i++)
            {
                InventorySlotState slot = _hotbar.GetSlot(i);
                TMP_Text caption = _slots[i].GetComponentInChildren<TMP_Text>();
                caption.fontSize = 20f;
                caption.text = (i + 1) + "\n" + (slot.IsEmpty ? "—" : slot.EffectiveItemId == CellId ? "C×" + slot.EffectiveCount : "PET");
                _slots[i].image.color = i == _hand.SlotIndex ? new Color(1f, 0.8f, 0.25f) : _shell.Accent;
            }
            if (_placed && _healed)
                _goalLabel.text = "Done: specimen placed and CELL used to restore HP.\nRecall keeps UID/state. Collect more to test full bags.";
            else
                _goalLabel.text = Instructions;
        }

        private static int FirstEmpty(InventoryComponent inventory)
        {
            for (int i = 0; i < inventory.SlotCapacity; i++) if (inventory.GetSlot(i).IsEmpty) return i;
            return -1;
        }

        private static string Describe(InventoryItemRecord record)
        {
            if (record == null || record.EffectiveItemId < 0 || record.EffectiveCount <= 0) return "Empty";
            if (!record.IsInstance) return "CELL ×" + record.EffectiveCount;
            SpecimenState state = record.Instance.ComponentStates.Count > 0
                ? JsonUtility.FromJson<SpecimenState>(record.Instance.ComponentStates[0].Json) : null;
            string uid = record.Instance.InstanceId;
            string shortUid = string.IsNullOrEmpty(uid) ? "missing" : uid.Substring(0, Mathf.Min(6, uid.Length));
            return (state != null ? state.Mutation + " Lv" + state.Level : "PET") + " · UID " + shortUid;
        }

        private void OnEnable() => _shell?.SetVisible(true);
        private void OnDisable() => _shell?.SetVisible(false);
        private void OnDestroy()
        {
            if (_cellUse != null) _cellUse.OnUsed.RemoveListener(UseCell);
            if (_hotbar != null) _hotbar.OnInventoryChanged.RemoveListener(Refresh);
            if (_catchBag != null) _catchBag.OnInventoryChanged.RemoveListener(Refresh);
            if (_podium != null) _podium.OnInventoryChanged.RemoveListener(Refresh);
            if (_hand != null) _hand.OnEquippedChanged.RemoveListener(SelectionChanged);
            _shell?.Dispose();
        }
    }
}
