# PlayerPrefsUtils

**Purpose:** Utility class extending `PlayerPrefs` with primitive array helpers.

## API

- `SetIntArray(string key, int[] array)` / `GetIntArray(string key, int[] defaultValue = null)`
- `SetFloatArray(string key, float[] array)` / `GetFloatArray(string key, float[] defaultValue = null)`
- `SetStringArray(string key, string[] array)` / `GetStringArray(string key, string[] defaultValue = null)`
- `SetBoolArray(string key, bool[] array)` / `GetBoolArray(string key, bool[] defaultValue = null)`

## Format and validation

- Arrays are stored as comma-separated strings in `PlayerPrefs`.
- Numeric arrays use invariant culture, so floats are saved with a dot decimal separator (`1.25`).
- `SetStringArray` writes the legacy CSV format and `GetStringArray` reads both legacy CSV and the older JSON wrapper format (`{"Value":[...]}`).
- `string[]` values cannot contain commas. `SetStringArray` throws `ArgumentException` if any element contains `,`, because that value cannot be restored safely from CSV.
- `bool[]` values are stored as `0` / `1`; any other stored number is treated as invalid data and returns `defaultValue`.
- Invalid stored data returns `defaultValue` or an empty array and logs a warning.

Call `PlayerPrefs.Save()` manually after Set methods when you need the data flushed to disk immediately.

## See Also
- ← [Extensions](README.md)
