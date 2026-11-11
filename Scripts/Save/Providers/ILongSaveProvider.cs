namespace Neo.Save
{
    /// <summary>
    ///     Optional capability of an <see cref="ISaveProvider" /> that stores 64-bit integers natively.
    ///     Callers never need to test for it: <see cref="SaveProviderExtensions.GetLong" /> and
    ///     <see cref="SaveProviderExtensions.SetLong" /> use it when the provider implements it and otherwise store the
    ///     value as an invariant-culture string through <see cref="ISaveProvider.SetString" />.
    /// </summary>
    /// <remarks>
    ///     Implement it on a custom provider when the backend has a native 64-bit type. Reads must stay backward
    ///     compatible: a key written earlier with <see cref="ISaveProvider.SetInt" /> or as an invariant-culture
    ///     integer string must still read back as the same number.
    /// </remarks>
    public interface ILongSaveProvider : ISaveProvider
    {
        /// <summary>
        ///     Gets a 64-bit integer by key. Values stored by <see cref="ISaveProvider.SetInt" /> or as an
        ///     invariant-culture integer string are widened and returned as well.
        /// </summary>
        /// <param name="key">Key to read.</param>
        /// <param name="defaultValue">Default if the key is missing or holds a non-integer value.</param>
        /// <returns>Stored value or default.</returns>
        long GetLong(string key, long defaultValue = 0L);

        /// <summary>
        ///     Sets a 64-bit integer by key.
        /// </summary>
        /// <param name="key">Key to write.</param>
        /// <param name="value">Value to store.</param>
        void SetLong(string key, long value);
    }
}
