using System;
using UnityEngine;

namespace Neo.Rpg.Runtime
{
    /// <summary>
    ///     Reusable serializer and validation boundary for RPG character profiles.
    ///     Keeps persistence and network adapters independent from the scene component lifecycle.
    /// </summary>
    public sealed class RpgCharacterProfileService
    {
        public string Serialize(RpgCharacterProfileData profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            RpgCharacterProfileData sanitized = profile.Clone();
            sanitized.Sanitize();
            return JsonUtility.ToJson(sanitized);
        }

        public RpgCharacterProfileData Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            RpgCharacterProfileData profile = JsonUtility.FromJson<RpgCharacterProfileData>(json);
            if (profile == null)
            {
                return null;
            }

            profile.Sanitize();
            return profile;
        }
    }
}
