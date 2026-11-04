using System;
using TMPro;

namespace Neo.Tools
{
    /// <summary>Formats a count into a TMP label and skips the write when the label already shows that value.</summary>
    internal sealed class InventoryCountLabel
    {
        private string _renderedFormat;
        private string _renderedText;
        private int _renderedValue;

        internal void Write(TMP_Text target, string format, int value)
        {
            if (target == null)
            {
                return;
            }

            // WHY: Inventory events fire on every mutation and string.Format boxes the value and allocates a string;
            // an unchanged number on a label nobody else rewrote is not rendered again.
            if (_renderedText != null && value == _renderedValue && ReferenceEquals(format, _renderedFormat) &&
                string.Equals(target.text, _renderedText, StringComparison.Ordinal))
            {
                return;
            }

            string text = string.Format(format, value);
            target.text = text;
            _renderedFormat = format;
            _renderedText = text;
            _renderedValue = value;
        }
    }
}
