using System;
using System.Collections.Generic;
using HtmlToUitk.Core.Text;

namespace HtmlToUitk.Convert.Emulations
{
    public sealed class NameGenerator : INameGenerator
    {
        public string Generate(string role, string constructKey)
        {
            return "htu-" + role + "-" + Fnv.Hex8(constructKey ?? "");
        }
    }

    public sealed class AssetSink : IAssetSink
    {
        private readonly List<GeneratedAsset> _assets = new List<GeneratedAsset>();
        private readonly Dictionary<string, GeneratedAsset> _byPath = new Dictionary<string, GeneratedAsset>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<GeneratedAsset> Assets => _assets;

        public string Add(GeneratedAsset asset)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            string path = PathUtil.Normalize(asset.RelativePath);
            if (string.IsNullOrEmpty(path) || PathUtil.IsRooted(path) || path == ".." || path.StartsWith("../", StringComparison.Ordinal) ||
                string.Equals(path, "generated-assets.json", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(path, "diagnostics.json", StringComparison.OrdinalIgnoreCase) || asset.Content == null)
                throw new FormatException("Generated asset requires a relative non-reserved path and content: " + asset.RelativePath);
            GeneratedAsset existing;
            if (_byPath.TryGetValue(path, out existing))
            {
                if (!SameBytes(existing.Content, asset.Content))
                    throw new FormatException("Distinct generated assets target the same path: " + path);
                return existing.RelativePath;
            }
            asset.RelativePath = path;
            _assets.Add(asset);
            _byPath[path] = asset;
            return path;
        }

        private static bool SameBytes(byte[] first, byte[] second)
        {
            if (first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++)
                if (first[i] != second[i]) return false;
            return true;
        }
    }

    public sealed class SpecSink : ISpecSink
    {
        private readonly List<AnimDef> _animations = new List<AnimDef>();
        private readonly List<AnimBinding> _bindings = new List<AnimBinding>();
        private readonly List<MediaClass> _media = new List<MediaClass>();
        private string _mediaRoot;
        private NavModel _nav;

        public IReadOnlyList<AnimDef> Animations => _animations;

        public IReadOnlyList<AnimBinding> Bindings => _bindings;

        public MediaQuerySpec Media => _media.Count == 0 ? null : new MediaQuerySpec { Root = _mediaRoot, Classes = _media };

        public NavModel Nav => _nav;

        public void AddAnimation(AnimDef animation)
        {
            foreach (AnimDef existing in _animations)
            {
                if (existing.Id == animation.Id) return;
            }

            _animations.Add(animation);
        }

        public void AddAnimationBinding(AnimBinding binding)
        {
            _bindings.Add(binding);
        }

        public void AddMediaClass(string rootName, MediaClass mediaClass)
        {
            _mediaRoot = rootName;
            foreach (MediaClass existing in _media)
            {
                if (existing.Class == mediaClass.Class) return;
            }

            _media.Add(mediaClass);
        }

        public void SetNav(NavModel nav)
        {
            _nav = nav;
        }
    }
}
