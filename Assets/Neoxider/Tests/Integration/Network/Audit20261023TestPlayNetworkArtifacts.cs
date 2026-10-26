#if MIRROR && UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Neo.Tests.Network.Integration
{
    [Serializable]
    internal sealed class MatrixRunMarker
    {
        public string runId;
        public string snapshotSha;
        public string company;
    }

    internal sealed class MatrixOptions
    {
        public string RunId;
        public string SnapshotSha;
        public string PeerId;
        public string Root;
        public string PeerRoot;
        public string SaveRoot;
        public string Company;
        public string Product;
        public string PersistentDataPath;
        public ushort Port;
        public double PhaseTimeout;
        public double OverallTimeout;

        public bool IsServer => PeerId == "server";

        public static bool Requested(string[] arguments) => Array.IndexOf(arguments, "-neoNetworkMatrix") >= 0;

        public static MatrixOptions Parse(string[] arguments)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < arguments.Length; i++)
            {
                string key = arguments[i];
                if (key == "-neoNetworkMatrix" || !key.StartsWith("-neoNetworkMatrix", StringComparison.Ordinal))
                    continue;
                if (i + 1 >= arguments.Length || values.ContainsKey(key))
                    throw new ArgumentException("Missing or duplicate matrix argument: " + key);
                values.Add(key, arguments[++i]);
            }
            MatrixOptions result = new MatrixOptions
            {
                RunId = Required(values, "RunId"),
                SnapshotSha = Required(values, "Snapshot"),
                PeerId = Required(values, "Peer"),
                Root = Path.GetFullPath(Required(values, "Root")),
                Company = Required(values, "Company"),
                Product = Required(values, "Product"),
                PersistentDataPath = Path.GetFullPath(Required(values, "PersistentDataPath")),
                Port = ushort.Parse(Required(values, "Port"), CultureInfo.InvariantCulture),
                PhaseTimeout = double.Parse(Required(values, "PhaseTimeout"), CultureInfo.InvariantCulture),
                OverallTimeout = double.Parse(Required(values, "OverallTimeout"), CultureInfo.InvariantCulture)
            };
            if (!Regex.IsMatch(result.RunId, "^[a-f0-9]{32}$")
                || !Regex.IsMatch(result.SnapshotSha, "^(?:[a-fA-F0-9]{40}|[a-fA-F0-9]{64})$"))
                throw new ArgumentException("A unique 32-hex run ID and a frozen 40/64-hex snapshot SHA are required.");
            if (result.PeerId != "server" && result.PeerId != "client-a" && result.PeerId != "client-b")
                throw new ArgumentException("Peer must be server, client-a or client-b.");
            if (result.Port == 0 || !Finite(result.PhaseTimeout) || result.PhaseTimeout < 2
                || result.PhaseTimeout > 120 || !Finite(result.OverallTimeout)
                || result.OverallTimeout < result.PhaseTimeout || result.OverallTimeout > 3600)
                throw new ArgumentException("Invalid port or realtime deadline.");
            if (!Regex.IsMatch(result.Company, "^NeoQA_[A-Za-z0-9_.-]+$")
                || result.Company.IndexOf(result.RunId, StringComparison.Ordinal) < 0
                || !Regex.IsMatch(result.Product, "^[A-Za-z0-9_.-]+$")
                || result.Product.IndexOf(result.PeerId, StringComparison.Ordinal) < 0)
                throw new ArgumentException("Build identities must be task-owned and distinct for each peer.");
            MatrixRunMarker marker = JsonUtility.FromJson<MatrixRunMarker>(
                File.ReadAllText(Path.Combine(result.Root, "owned-run.json"), Encoding.UTF8));
            if (marker == null || marker.runId != result.RunId || marker.snapshotSha != result.SnapshotSha
                || marker.company != result.Company)
                throw new ArgumentException("The launcher-owned run marker does not match these arguments.");
            result.PeerRoot = result.UnderRoot(result.PeerId);
            result.SaveRoot = result.UnderRoot(Path.Combine(result.PeerId, "save"));
            return result;
        }

        public void VerifyPlayerIdentity()
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer)
                throw new InvalidOperationException("This entrypoint requires a standalone Windows player, not Editor/host emulation.");
            if (Application.companyName != Company || Application.productName != Product
                || !SamePath(Application.persistentDataPath, PersistentDataPath))
                throw new InvalidOperationException("Player build/save identity differs from its task-owned expected identity.");
            DirectoryInfo directory = new DirectoryInfo(PersistentDataPath);
            if (directory.Name != Product || directory.Parent == null || directory.Parent.Name != Company)
                throw new InvalidOperationException("The effective persistent path is outside its company/product namespace.");
        }

        public string UnderRoot(string relative)
        {
            string target = Path.GetFullPath(Path.Combine(Root, relative));
            string prefix = Root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Artifact path escaped the task-owned root.");
            return target;
        }

        public static bool SamePath(string first, string second) => string.Equals(
            Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static string Required(Dictionary<string, string> values, string suffix)
        {
            string key = "-neoNetworkMatrix" + suffix;
            if (!values.TryGetValue(key, out string value) || string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Required argument: " + key);
            return value;
        }
    }

    internal sealed class MatrixArtifacts : IDisposable
    {
        private readonly MatrixOptions _options;
        private readonly StreamWriter _writer;
        private readonly int _pid;

        public MatrixArtifacts(MatrixOptions options)
        {
            _options = options;
            Directory.CreateDirectory(options.PeerRoot);
            Directory.CreateDirectory(options.SaveRoot);
            _pid = Process.GetCurrentProcess().Id;
            FileStream stream = new FileStream(Path.Combine(options.PeerRoot, "events.jsonl"),
                FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(stream, new UTF8Encoding(false, true)) { AutoFlush = true };
        }

        public void Write(string kind, string detail = null, bool passed = true,
            int step = 0, string caseId = null, MatrixState state = null,
            MatrixEnvironment environment = null, string fromPeer = null, int connectionId = -1, int epoch = 0,
            MatrixSpendRequest[] spendRequests = null)
        {
            MatrixRecord record = new MatrixRecord
            {
                kind = kind,
                runId = _options.RunId,
                snapshotSha = _options.SnapshotSha,
                peerId = _options.PeerId,
                fromPeer = fromPeer,
                processId = _pid,
                connectionId = connectionId,
                epoch = epoch,
                step = step,
                caseId = caseId,
                passed = passed,
                realtime = Time.realtimeSinceStartupAsDouble,
                detail = detail,
                state = state,
                environment = environment,
                spendRequests = spendRequests ?? Array.Empty<MatrixSpendRequest>()
            };
            _writer.WriteLine(JsonUtility.ToJson(record));
        }

        public void Dispose() => _writer.Dispose();
    }
}
#endif
