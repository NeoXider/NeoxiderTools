namespace Neo.Progression
{
    /// <summary>
    ///     Action type for progression NoCode bridge. Values are serialized; append new members only.
    /// </summary>
    public enum ProgressionNoCodeActionType
    {
        /// <summary>ProgressionManager.TryAddXp(XP Amount).</summary>
        AddXp,

        /// <summary>ProgressionManager.AddPerkPoints(Perk Points Amount).</summary>
        GrantPerkPoints,

        /// <summary>ProgressionManager.TryUnlockNode(Node Id).</summary>
        UnlockNode,

        /// <summary>ProgressionManager.TryBuyPerk(Perk Id).</summary>
        BuyPerk,

        /// <summary>ProgressionManager.ResetProgression().</summary>
        ResetProgression,

        /// <summary>ProgressionManager.SaveProfile().</summary>
        SaveProfile,

        /// <summary>ProgressionManager.LoadProfile().</summary>
        LoadProfile,

        /// <summary>ProgressionManager.ActivatePremium(); fails when premium is already active.</summary>
        ActivatePremium
    }
}
