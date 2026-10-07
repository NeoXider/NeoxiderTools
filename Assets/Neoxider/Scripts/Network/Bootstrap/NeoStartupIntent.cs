using System;
using System.Collections.Generic;
using System.Globalization;

namespace Neo.Network
{
    /// <summary>How this process was told to start. Decided once, from the command line, at boot.</summary>
    public enum NeoStartupMode
    {
        /// <summary>No networking switches: the single-player path, untouched.</summary>
        Solo = 0,

        /// <summary>Server that also plays: <c>NeoNetworkManager.StartAsHost()</c>.</summary>
        Host = 1,

        /// <summary>Authority only, no local player: <c>NeoNetworkManager.StartAsServer()</c>. Command line only.</summary>
        DedicatedServer = 2,

        /// <summary>Remote client: <c>NeoNetworkManager.StartAsClient()</c>.</summary>
        Client = 3
    }

    /// <summary>
    ///     Defaults the command-line parser falls back to for switches that were not given.
    ///     <see cref="Port"/> and <see cref="MaxPlayers"/> of <c>0</c> mean "leave the transport / Mirror setting alone".
    /// </summary>
    public readonly struct NeoStartupDefaults
    {
        /// <summary>Address used by <c>-client</c> when <c>-address</c> is absent.</summary>
        public readonly string Address;

        /// <summary>Port used when <c>-port</c> is absent; 0 keeps the transport's own port.</summary>
        public readonly int Port;

        /// <summary>Capacity used when <c>-maxplayers</c> is absent; 0 keeps Mirror's <c>maxConnections</c>.</summary>
        public readonly int MaxPlayers;

        /// <summary>Display name used when <c>-name</c> is absent.</summary>
        public readonly string DisplayName;

        public NeoStartupDefaults(string address = null, int port = 0, int maxPlayers = 0, string displayName = null)
        {
            Address = string.IsNullOrEmpty(address) ? NeoStartupIntent.DefaultAddress : address;
            Port = port;
            MaxPlayers = maxPlayers;
            DisplayName = displayName ?? string.Empty;
        }

        /// <summary>Address <c>localhost</c>, port and capacity unset.</summary>
        public static NeoStartupDefaults Default => new NeoStartupDefaults();
    }

    /// <summary>
    ///     The parsed startup decision of a process. Plain C# and immutable, so the whole resolution is unit-testable
    ///     without Unity, a scene or a Mirror session. Produced by <see cref="NeoStartupCommandLine.Parse"/>.
    /// </summary>
    public readonly struct NeoStartupIntent
    {
        /// <summary>Address a client connects to when <c>-address</c> is not given.</summary>
        public const string DefaultAddress = "localhost";

        /// <summary>Address a host listens on for local clients.</summary>
        public const string LoopbackAddress = "127.0.0.1";

        /// <summary>The role this process starts in.</summary>
        public readonly NeoStartupMode Mode;

        /// <summary>True when Unity itself was launched headless (<c>-batchmode</c> or <c>-nographics</c>).</summary>
        public readonly bool Headless;

        /// <summary>Server address for a client (<c>-address</c>).</summary>
        public readonly string Address;

        /// <summary>Port from <c>-port</c> clamped to 1..65535, or the default; 0 means "not specified".</summary>
        public readonly int Port;

        /// <summary>Capacity from <c>-maxplayers</c> (at least 1), or the default; 0 means "not specified".</summary>
        public readonly int MaxPlayers;

        /// <summary>Player display name from <c>-name</c>; empty when not given.</summary>
        public readonly string DisplayName;

        private readonly IReadOnlyDictionary<string, string> _custom;

        public NeoStartupIntent(
            NeoStartupMode mode,
            bool headless,
            string address,
            int port,
            int maxPlayers,
            string displayName,
            IReadOnlyDictionary<string, string> custom = null)
        {
            Mode = mode;
            Headless = headless;
            Address = string.IsNullOrEmpty(address) ? DefaultAddress : address;
            Port = port < 0 ? 0 : port;
            MaxPlayers = maxPlayers < 0 ? 0 : maxPlayers;
            DisplayName = displayName ?? string.Empty;
            _custom = custom;
        }

        /// <summary>Copy of this intent with another role (a UI button picking host or client after boot).</summary>
        public NeoStartupIntent WithMode(NeoStartupMode mode)
        {
            return new NeoStartupIntent(mode, Headless, Address, Port, MaxPlayers, DisplayName, _custom);
        }

        /// <summary>Copy of this intent with another server address.</summary>
        public NeoStartupIntent WithAddress(string address)
        {
            return new NeoStartupIntent(Mode, Headless, address, Port, MaxPlayers, DisplayName, _custom);
        }

        /// <summary>A solo intent with the given defaults.</summary>
        public static NeoStartupIntent Solo(NeoStartupDefaults defaults)
        {
            return new NeoStartupIntent(NeoStartupMode.Solo, false, defaults.Address, defaults.Port, defaults.MaxPlayers,
                defaults.DisplayName);
        }

        /// <summary>True for host, dedicated server and client.</summary>
        public bool IsNetworked => Mode != NeoStartupMode.Solo;

        /// <summary>Host or dedicated server: the peers allowed to author gameplay.</summary>
        public bool IsAuthorityPeer => Mode == NeoStartupMode.Host || Mode == NeoStartupMode.DedicatedServer;

        /// <summary>Solo or host: this process plays locally.</summary>
        public bool SimulatesLocally => Mode == NeoStartupMode.Solo || Mode == NeoStartupMode.Host;

        /// <summary>A dedicated server renders nothing, hears nothing and shows no UI.</summary>
        public bool SuppressesLocalPresentation => Mode == NeoStartupMode.DedicatedServer;

        /// <summary>True when <c>-port</c> (or a default) supplied a port.</summary>
        public bool HasPort => Port > 0;

        /// <summary>True when <c>-maxplayers</c> (or a default) supplied a capacity.</summary>
        public bool HasMaxPlayers => MaxPlayers > 0;

        /// <summary>Number of custom switches the parser collected.</summary>
        public int CustomCount => _custom != null ? _custom.Count : 0;

        /// <summary>True when a custom flag or value switch was present. <paramref name="name"/> is matched without dashes.</summary>
        public bool HasCustom(string name)
        {
            return _custom != null && _custom.ContainsKey(NeoStartupCommandLine.Normalize(name));
        }

        /// <summary>Reads a custom switch value (empty string for a flag). Returns false when it was not given.</summary>
        public bool TryGetCustom(string name, out string value)
        {
            if (_custom != null && _custom.TryGetValue(NeoStartupCommandLine.Normalize(name), out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>Reads a custom switch as an integer, <paramref name="fallback"/> when missing or not a number.</summary>
        public int GetCustomInt(string name, int fallback)
        {
            return TryGetCustom(name, out string raw)
                   && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : fallback;
        }

        /// <summary>Reads a custom switch as a float (invariant culture), <paramref name="fallback"/> when missing or invalid.</summary>
        public float GetCustomFloat(string name, float fallback)
        {
            return TryGetCustom(name, out string raw)
                   && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : fallback;
        }

        public override string ToString()
        {
            return $"NeoStartupIntent(Mode={Mode}, Address={Address}, Port={Port}, MaxPlayers={MaxPlayers}, " +
                   $"Name='{DisplayName}', Headless={Headless}, Custom={CustomCount})";
        }
    }

    /// <summary>
    ///     Pure-C# command-line resolver. Recognises Unity's own <c>-batchmode</c> / <c>-nographics</c> only to decide
    ///     whether the process is headless; every other switch comes from the table below plus the custom switches the
    ///     caller registers.
    /// </summary>
    /// <remarks>
    ///     <para><b>Switch table</b> (case-insensitive; <c>-name value</c>, <c>-name=value</c> and <c>--name value</c> all work):</para>
    ///     <list type="table">
    ///         <item><term><c>-host</c></term><description>Start as host (server + local client).</description></item>
    ///         <item><term><c>-server</c></term><description>Start as dedicated server (no local player).</description></item>
    ///         <item><term><c>-client</c></term><description>Start as a client.</description></item>
    ///         <item><term><c>-address &lt;host&gt;</c></term><description>Server address for a client.</description></item>
    ///         <item><term><c>-port &lt;n&gt;</c></term><description>Listen / connect port, clamped to 1..65535.</description></item>
    ///         <item><term><c>-maxplayers &lt;n&gt;</c></term><description>Capacity (Mirror <c>maxConnections</c>).</description></item>
    ///         <item><term><c>-name &lt;text&gt;</c></term><description>Player display name.</description></item>
    ///     </list>
    ///     Role precedence is host &gt; server &gt; client, so an explicit <c>-host</c> beats a stray <c>-server</c>.
    ///     Unknown switches are ignored: Unity adds many of its own and must never crash a game.
    /// </remarks>
    public static class NeoStartupCommandLine
    {
        public const string HostSwitch = "-host";
        public const string ServerSwitch = "-server";
        public const string ClientSwitch = "-client";
        public const string AddressSwitch = "-address";
        public const string PortSwitch = "-port";
        public const string MaxPlayersSwitch = "-maxplayers";
        public const string NameSwitch = "-name";
        public const string BatchModeSwitch = "-batchmode";
        public const string NoGraphicsSwitch = "-nographics";

        /// <summary>Lowest valid port.</summary>
        public const int MinPort = 1;

        /// <summary>Highest valid port.</summary>
        public const int MaxPort = 65535;

        /// <summary>Strips leading dashes and lower-cases a switch name: <c>--BotFill</c> becomes <c>botfill</c>.</summary>
        public static string Normalize(string name)
        {
            return string.IsNullOrEmpty(name) ? string.Empty : name.TrimStart('-').ToLowerInvariant();
        }

        /// <summary>
        ///     Parses <paramref name="args"/> (typically <c>Environment.GetCommandLineArgs()</c>) into an intent.
        /// </summary>
        /// <param name="args">Raw arguments; <see langword="null"/> or empty gives a solo intent.</param>
        /// <param name="defaults">Fallbacks for switches that were not given.</param>
        /// <param name="customValueSwitches">Game-specific switches that take a value, e.g. <c>-botfill</c>.</param>
        /// <param name="customFlagSwitches">Game-specific switches without a value, e.g. <c>-autoplay</c>.</param>
        public static NeoStartupIntent Parse(
            string[] args,
            NeoStartupDefaults defaults = default,
            IReadOnlyCollection<string> customValueSwitches = null,
            IReadOnlyCollection<string> customFlagSwitches = null)
        {
            if (defaults.Address == null)
            {
                defaults = NeoStartupDefaults.Default;
            }

            bool wantsHost = false;
            bool wantsServer = false;
            bool wantsClient = false;
            bool headless = false;
            string address = defaults.Address;
            string displayName = defaults.DisplayName;
            int port = defaults.Port;
            int maxPlayers = defaults.MaxPlayers;
            Dictionary<string, string> custom = null;

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string token = args[i];
                    if (string.IsNullOrEmpty(token))
                    {
                        continue;
                    }

                    if (IsSwitch(token, HostSwitch, out _))
                    {
                        wantsHost = true;
                    }
                    else if (IsSwitch(token, ServerSwitch, out _))
                    {
                        wantsServer = true;
                    }
                    else if (IsSwitch(token, ClientSwitch, out _))
                    {
                        wantsClient = true;
                    }
                    else if (IsSwitch(token, BatchModeSwitch, out _) || IsSwitch(token, NoGraphicsSwitch, out _))
                    {
                        headless = true;
                    }
                    else if (TryReadValue(args, ref i, AddressSwitch, out string addressValue))
                    {
                        address = addressValue;
                    }
                    else if (TryReadValue(args, ref i, PortSwitch, out string portValue))
                    {
                        if (TryParseInt(portValue, out int parsedPort))
                        {
                            port = parsedPort;
                        }
                    }
                    else if (TryReadValue(args, ref i, MaxPlayersSwitch, out string maxValue))
                    {
                        if (TryParseInt(maxValue, out int parsedMax))
                        {
                            maxPlayers = parsedMax;
                        }
                    }
                    else if (TryReadValue(args, ref i, NameSwitch, out string nameValue))
                    {
                        displayName = nameValue;
                    }
                    else
                    {
                        ReadCustom(args, ref i, customValueSwitches, customFlagSwitches, ref custom);
                    }
                }
            }

            NeoStartupMode mode = ResolveMode(wantsHost, wantsServer, wantsClient);
            if (port > 0)
            {
                port = port < MinPort ? MinPort : port > MaxPort ? MaxPort : port;
            }
            else if (port < 0)
            {
                port = MinPort;
            }

            if (maxPlayers < 0)
            {
                maxPlayers = 1;
            }

            return new NeoStartupIntent(mode, headless, address, port, maxPlayers, displayName, custom);
        }

        /// <summary>Host beats server beats client; nothing means solo.</summary>
        public static NeoStartupMode ResolveMode(bool wantsHost, bool wantsServer, bool wantsClient)
        {
            if (wantsHost)
            {
                return NeoStartupMode.Host;
            }

            if (wantsServer)
            {
                return NeoStartupMode.DedicatedServer;
            }

            return wantsClient ? NeoStartupMode.Client : NeoStartupMode.Solo;
        }

        /// <summary>
        ///     Turns a URL query string into command-line style tokens, for WebGL builds that have no command line:
        ///     <c>https://game/?client&amp;address=play.example.com&amp;port=7778&amp;name=Bob</c> becomes
        ///     <c>-client -address=play.example.com -port=7778 -name=Bob</c>. Values are percent-decoded.
        /// </summary>
        /// <param name="urlOrQuery">A full URL, or just the part after <c>?</c>. <see langword="null"/> gives no tokens.</param>
        public static string[] FromUrlQuery(string urlOrQuery)
        {
            if (string.IsNullOrEmpty(urlOrQuery))
            {
                return Array.Empty<string>();
            }

            string query = urlOrQuery;
            int hash = query.IndexOf('#');
            if (hash >= 0)
            {
                query = query.Substring(0, hash);
            }

            int question = query.IndexOf('?');
            if (question >= 0)
            {
                query = query.Substring(question + 1);
            }
            else if (urlOrQuery.IndexOf("://", StringComparison.Ordinal) >= 0)
            {
                return Array.Empty<string>();
            }

            string[] pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
            List<string> tokens = new List<string>(pairs.Length);
            for (int i = 0; i < pairs.Length; i++)
            {
                int equals = pairs[i].IndexOf('=');
                string key = equals >= 0 ? pairs[i].Substring(0, equals) : pairs[i];
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                string decodedKey = SafeUnescape(key).Trim();
                if (equals < 0)
                {
                    tokens.Add("-" + decodedKey);
                }
                else
                {
                    tokens.Add("-" + decodedKey + "=" + SafeUnescape(pairs[i].Substring(equals + 1)));
                }
            }

            return tokens.ToArray();
        }

        private static string SafeUnescape(string value)
        {
            try
            {
                return Uri.UnescapeDataString(value.Replace('+', ' '));
            }
            catch (UriFormatException)
            {
                return value;
            }
        }

        private static void ReadCustom(
            string[] args,
            ref int index,
            IReadOnlyCollection<string> valueSwitches,
            IReadOnlyCollection<string> flagSwitches,
            ref Dictionary<string, string> custom)
        {
            if (valueSwitches != null)
            {
                foreach (string name in valueSwitches)
                {
                    if (TryReadValue(args, ref index, name, out string value))
                    {
                        custom ??= new Dictionary<string, string>();
                        custom[Normalize(name)] = value;
                        return;
                    }
                }
            }

            if (flagSwitches != null)
            {
                foreach (string name in flagSwitches)
                {
                    if (IsSwitch(args[index], name, out string inline))
                    {
                        custom ??= new Dictionary<string, string>();
                        custom[Normalize(name)] = inline ?? string.Empty;
                        return;
                    }
                }
            }
        }

        private static bool TryReadValue(string[] args, ref int index, string name, out string value)
        {
            value = null;
            if (!IsSwitch(args[index], name, out string inline))
            {
                return false;
            }

            if (inline != null)
            {
                value = inline;
                return !string.IsNullOrEmpty(value);
            }

            if (index + 1 >= args.Length || string.IsNullOrEmpty(args[index + 1]) || LooksLikeSwitch(args[index + 1]))
            {
                return false;
            }

            value = args[++index];
            return true;
        }

        // WHY: "-address -host" must not swallow "-host" as the address. A bare negative number is still a value.
        private static bool LooksLikeSwitch(string token)
        {
            return token.Length > 1 && token[0] == '-' && !char.IsDigit(token[1]);
        }

        private static bool TryParseInt(string raw, out int value)
        {
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool IsSwitch(string token, string name, out string inlineValue)
        {
            inlineValue = null;
            string bare = Normalize(name);
            if (bare.Length == 0 || token.Length < 2 || token[0] != '-')
            {
                return false;
            }

            string body = token.StartsWith("--", StringComparison.Ordinal) ? token.Substring(2) : token.Substring(1);
            int equals = body.IndexOf('=');
            string key = equals >= 0 ? body.Substring(0, equals) : body;
            if (!string.Equals(key, bare, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (equals >= 0)
            {
                inlineValue = body.Substring(equals + 1);
            }

            return true;
        }
    }
}
