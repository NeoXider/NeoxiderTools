using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace Neo.Tools
{
    /// <summary>
    ///     Explicit reader for raw legacy JSON shaped as { player: { id, name, score, num }, users: [...] }.
    ///     Converts writable legacy id tokens without relying on rename metadata or changing a live board.
    /// </summary>
    public static class LeaderboardLegacyJson
    {
        /// <summary>
        ///     Reads one legacy aggregate. Rejects multiple source rows matching the player's nonempty key.
        ///     Requires one explicit root users array, including an empty array; nested names do not qualify.
        ///     Standard JSON escapes are decoded in recognized member names and string payloads.
        ///     Keyless rows are retained without guessing identity; derived sortUsers is ignored.
        ///     Outputs remain null on rejected input. This method performs no save I/O or event delivery.
        /// </summary>
        public static bool TryDeserialize(string json, out LeaderboardUser player, out List<LeaderboardUser> users)
        {
            player = null;
            users = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            LegacyAggregate data;
            try
            {
                // WHY: Use the same decoded tokens for presence checks and DTO fields. JsonUtility can
                // default an absent list to empty and does not bind every escaped JSON member name.
                if (!TryReadAggregate(json, out data))
                {
                    return false;
                }
            }
            catch (XmlException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (data == null || data.player == null || data.users == null)
            {
                return false;
            }

            int matchIndex = -1;
            if (!string.IsNullOrEmpty(data.player.id))
            {
                for (int i = 0; i < data.users.Count; i++)
                {
                    LegacyUser candidate = data.users[i];
                    if (candidate == null || candidate.id != data.player.id)
                    {
                        continue;
                    }

                    if (matchIndex >= 0)
                    {
                        return false;
                    }

                    matchIndex = i;
                }
            }

            LeaderboardUser canonicalPlayer = Convert(data.player);
            List<LeaderboardUser> source = new List<LeaderboardUser>(data.users.Count);
            for (int i = 0; i < data.users.Count; i++)
            {
                source.Add(i == matchIndex ? canonicalPlayer : Convert(data.users[i]));
            }

            player = canonicalPlayer;
            users = source;
            return true;
        }

        private static bool TryReadAggregate(string json, out LegacyAggregate data)
        {
            data = null;
            byte[] utf8 = Encoding.UTF8.GetBytes(json);
            using (XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(utf8,
                       XmlDictionaryReaderQuotas.Max))
            {
                if (reader.MoveToContent() != XmlNodeType.Element || reader.GetAttribute("type") != "object")
                {
                    return false;
                }

                reader.ReadStartElement();
                LegacyAggregate aggregate = new LegacyAggregate();
                bool hasUsers = false;
                while (reader.MoveToContent() == XmlNodeType.Element)
                {
                    string member = GetJsonMemberName(reader);
                    if (member == "users")
                    {
                        if (hasUsers || !TryReadUsers(reader, out aggregate.users))
                        {
                            return false;
                        }

                        hasUsers = true;
                    }
                    else if (member == "player")
                    {
                        if (!TryReadUser(reader, out aggregate.player))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // Skip complete unknown values; nested keys and quoted text are never root fields.
                        reader.Skip();
                    }
                }

                reader.ReadEndElement();
                if (reader.MoveToContent() != XmlNodeType.None || !hasUsers)
                {
                    return false;
                }

                data = aggregate;
                return true;
            }
        }

        private static bool TryReadUsers(XmlDictionaryReader reader, out List<LegacyUser> users)
        {
            users = null;
            if (reader.GetAttribute("type") != "array")
            {
                return false;
            }

            List<LegacyUser> source = new List<LegacyUser>();
            bool isEmpty = reader.IsEmptyElement;
            reader.ReadStartElement();
            if (!isEmpty)
            {
                while (reader.MoveToContent() == XmlNodeType.Element)
                {
                    if (!TryReadUser(reader, out LegacyUser user))
                    {
                        return false;
                    }

                    source.Add(user);
                }

                reader.ReadEndElement();
            }

            users = source;
            return true;
        }

        private static bool TryReadUser(XmlDictionaryReader reader, out LegacyUser user)
        {
            user = null;
            string type = reader.GetAttribute("type");
            if (type == "null")
            {
                reader.Skip();
                return true;
            }

            if (type != "object")
            {
                return false;
            }

            LegacyUser data = new LegacyUser();
            bool isEmpty = reader.IsEmptyElement;
            reader.ReadStartElement();
            if (!isEmpty)
            {
                while (reader.MoveToContent() == XmlNodeType.Element)
                {
                    string member = GetJsonMemberName(reader);
                    switch (member)
                    {
                        case "id":
                            if (!TryReadString(reader, out data.id))
                            {
                                return false;
                            }
                            break;
                        case "name":
                            if (!TryReadString(reader, out data.name))
                            {
                                return false;
                            }
                            break;
                        case "score":
                            if (!TryReadInt32(reader, out data.score))
                            {
                                return false;
                            }
                            break;
                        case "num":
                            if (!TryReadInt32(reader, out data.num))
                            {
                                return false;
                            }
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                reader.ReadEndElement();
            }

            user = data;
            return true;
        }

        private static string GetJsonMemberName(XmlDictionaryReader reader)
        {
            if (reader.NamespaceURI.Length == 0)
            {
                return reader.LocalName;
            }

            // Mono maps escaped JSON names to item/item nodes. The attribute is already JSON-decoded;
            // compare it exactly at the caller, without XML-name decoding or a second JSON unescape.
            return reader.LocalName == "item" && reader.NamespaceURI == "item"
                ? reader.GetAttribute("item", string.Empty) ?? string.Empty
                : string.Empty;
        }

        private static bool TryReadString(XmlDictionaryReader reader, out string value)
        {
            value = null;
            if (reader.GetAttribute("type") == "null")
            {
                reader.Skip();
                return true;
            }

            if (reader.GetAttribute("type") != "string")
            {
                return false;
            }

            value = reader.ReadElementContentAsString();
            return true;
        }

        private static bool TryReadInt32(XmlDictionaryReader reader, out int value)
        {
            value = 0;
            return reader.GetAttribute("type") == "number" &&
                   int.TryParse(reader.ReadElementContentAsString(), NumberStyles.Integer,
                       CultureInfo.InvariantCulture, out value);
        }

        private static LeaderboardUser Convert(LegacyUser data)
        {
            if (data == null)
            {
                return null;
            }

            LeaderboardUser user = new LeaderboardUser(data.name, data.score, data.num);
            user.SetPersistentId(data.id);
            return user;
        }

        [Serializable]
        private sealed class LegacyAggregate
        {
            public LegacyUser player;
            public List<LegacyUser> users;
        }

        [Serializable]
        private sealed class LegacyUser
        {
            public string id;
            public string name;
            public int score;
            public int num;
        }
    }
}
