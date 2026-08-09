namespace Neo.Rpg.Runtime
{
    public enum RpgCharacterNetworkCommandType
    {
        Damage = 0,
        Heal = 1,
        Spend = 2,
        Refill = 3,
        RestoreResource = 4,
        RestoreAll = 5,
        SetMaxResource = 6,
        AddMaxResource = 7,
        AddStatBase = 8,
        SetStatBase = 9,
        ApplyBuff = 10,
        ApplyInlineBuff = 11,
        RemoveBuff = 12,
        ClearBuffs = 13,
        ApplyStatus = 14,
        RemoveStatus = 15,
        ClearStatuses = 16,
        AddLevel = 17,
        SetLevel = 18,
        AddXp = 19,
        AddUpgradePoints = 20,
        UpgradeStat = 21,
        SetInvulnerable = 22
    }

    /// <summary>Transport-neutral mutation request for an RPG character.</summary>
    public readonly struct RpgCharacterNetworkCommand
    {
        public RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType type, string text = null,
            float number = 0f, int integer = 0, bool flag = false)
        {
            Type = type;
            Text = text ?? string.Empty;
            Number = number;
            Integer = integer;
            Flag = flag;
        }

        public RpgCharacterNetworkCommandType Type { get; }
        public string Text { get; }
        public float Number { get; }
        public int Integer { get; }
        public bool Flag { get; }
    }
}
