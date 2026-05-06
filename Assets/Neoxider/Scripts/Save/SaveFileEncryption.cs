using System;
using System.Security.Cryptography;
using System.Text;

namespace Neo.Save
{
    /// <summary>
    ///     AES-CBC file payload helpers for <see cref="FileSaveProvider"/>.
    ///     Base64 ciphertext line layout (obfuscation, not DRM).
    ///     Built-in <see cref="DefaultEncryptionKey"/> / <see cref="DefaultEncryptionIv"/> apply when encryption is on but custom strings are left empty — replace for shipping builds.
    /// </summary>
    public static class SaveFileEncryption
    {
        /// <summary>
        ///     Default AES key (UTF-8, 16 bytes) used when file encryption is enabled and no custom key is set in settings.
        ///     Override via Save Provider Settings or replace at runtime before creating the provider.
        /// </summary>
        public const string DefaultEncryptionKey = "NeoXiderSaveK16.";

        /// <summary>
        ///     Default AES IV (UTF-8, 16 bytes) used when file encryption is enabled and no custom IV is set.
        /// </summary>
        public const string DefaultEncryptionIv = "NeoXiderInitV16!";
        /// <summary>
        ///     Encrypts UTF-8 text to Base64 AES ciphertext (no BOM).
        /// </summary>
        public static bool TryEncrypt(string plainText, byte[] key, byte[] iv, out string base64Cipher)
        {
            base64Cipher = null;
            if (string.IsNullOrEmpty(plainText))
            {
                base64Cipher = string.Empty;
                return true;
            }

            if (!ValidateKeyIv(key, iv, out _))
            {
                return false;
            }

            try
            {
                using Aes aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                base64Cipher = Convert.ToBase64String(encryptedBytes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Decrypts Base64 AES ciphertext to UTF-8 text.
        /// </summary>
        public static bool TryDecrypt(string base64Cipher, byte[] key, byte[] iv, out string plainText)
        {
            plainText = null;
            if (string.IsNullOrEmpty(base64Cipher))
            {
                plainText = string.Empty;
                return true;
            }

            if (!ValidateKeyIv(key, iv, out _))
            {
                return false;
            }

            try
            {
                using Aes aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] inputBytes = Convert.FromBase64String(base64Cipher);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                plainText = Encoding.UTF8.GetString(decryptedBytes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Heuristic: non-empty string that decodes as Base64 (does not guarantee ciphertext validity).
        /// </summary>
        public static bool LooksLikeBase64Payload(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            try
            {
                Convert.FromBase64String(value.Trim());
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Validates AES key (128/192/256-bit) and IV (128-bit).
        /// </summary>
        public static bool ValidateKeyIv(byte[] key, byte[] iv, out string error)
        {
            error = null;
            if (key == null || key.Length is not (16 or 24 or 32))
            {
                error = "AES key must be 16, 24, or 32 bytes (use UTF-8 string of that length in ASCII).";
                return false;
            }

            if (iv == null || iv.Length != 16)
            {
                error = "AES IV must be 16 bytes (use a 16-character UTF-8 string in ASCII).";
                return false;
            }

            return true;
        }
    }

    /// <summary>
    ///     Key material for file encryption. Keep key/iv out of source control in production.
    /// </summary>
    public sealed class FileSaveEncryptionConfig
    {
        public bool Enabled { get; }
        public byte[] Key { get; }
        public byte[] Iv { get; }

        private FileSaveEncryptionConfig(bool enabled, byte[] key, byte[] iv)
        {
            Enabled = enabled;
            Key = key;
            Iv = iv;
        }

        /// <summary>
        ///     Builds config from UTF-8 passphrases. Key: 16, 24, or 32 bytes; IV: 16 bytes.
        ///     When <paramref name="enabled"/> is false, returns success with a disabled config.
        ///     When enabled and both <paramref name="keyUtf8"/> and <paramref name="ivUtf8"/> are empty/whitespace,
        ///     uses <see cref="SaveFileEncryption.DefaultEncryptionKey"/> and <see cref="SaveFileEncryption.DefaultEncryptionIv"/>.
        ///     When only one of the pair is set, returns false (must set both custom values or neither).
        /// </summary>
        public static bool TryCreate(bool enabled, string keyUtf8, string ivUtf8, out FileSaveEncryptionConfig config,
            out string error)
        {
            config = null;
            error = null;
            if (!enabled)
            {
                config = new FileSaveEncryptionConfig(false, null, null);
                return true;
            }

            string k = keyUtf8?.Trim() ?? string.Empty;
            string v = ivUtf8?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(k) && string.IsNullOrEmpty(v))
            {
                k = SaveFileEncryption.DefaultEncryptionKey;
                v = SaveFileEncryption.DefaultEncryptionIv;
            }
            else if (string.IsNullOrEmpty(k) || string.IsNullOrEmpty(v))
            {
                error =
                    "When file encryption is enabled, either leave both key and IV empty (built-in defaults) or set both to custom values.";
                return false;
            }

            byte[] kb = Encoding.UTF8.GetBytes(k);
            byte[] vb = Encoding.UTF8.GetBytes(v);
            if (!SaveFileEncryption.ValidateKeyIv(kb, vb, out error))
            {
                return false;
            }

            config = new FileSaveEncryptionConfig(true, kb, vb);
            return true;
        }
    }

    /// <summary>
    ///     Optional root directory and encryption for <see cref="FileSaveProvider"/>.
    /// </summary>
    public sealed class FileSaveProviderOptions
    {
        /// <summary>
        ///     If null or empty, <see cref="UnityEngine.Application.persistentDataPath"/> is used.
        /// </summary>
        public string PersistenceRoot;

        public FileSaveEncryptionConfig Encryption;
    }
}
