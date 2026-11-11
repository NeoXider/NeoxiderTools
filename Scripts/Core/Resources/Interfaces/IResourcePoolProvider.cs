namespace Neo.Core.Resources
{
    /// <summary>
    ///     Generic contract for resource pools (HP, Mana, etc.). Used by RPG combat and NoCode.
    /// </summary>
    public interface IResourcePoolProvider
    {
        /// <summary>Current value for the given resource id.</summary>
        float GetCurrent(string resourceId);

        /// <summary>Maximum value for the given resource id.</summary>
        float GetMax(string resourceId);

        /// <summary>Try to spend amount; returns false and reason if not enough.</summary>
        bool TrySpend(string resourceId, float amount, out string failReason);

        /// <summary>Decrease by amount (e.g. damage); returns actual amount decreased (after limits).</summary>
        float Decrease(string resourceId, float amount);

        /// <summary>Increase by amount (e.g. heal); returns actual amount increased (after limits).</summary>
        float Increase(string resourceId, float amount);

        /// <summary>True if current &lt;= 0 for the resource.</summary>
        bool IsDepleted(string resourceId);
    }
}
