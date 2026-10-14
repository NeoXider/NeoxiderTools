using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace HtmlToUitk.Capabilities.Reader;

public sealed class TypeMatch
{
    public string Namespace;
    public string Name;
    public string FullName => string.IsNullOrEmpty(Namespace) ? Name : Namespace + "." + Name;
    public IReadOnlyList<string> Members;
}

public sealed class MetadataAssembly : IDisposable
{
    private readonly PEReader _pe;
    private readonly MetadataReader _md;
    private Dictionary<string, List<TypeDefinitionHandle>> _byName;
    private HashSet<string> _userStrings;

    public string FileName { get; }
    public string Sha256 { get; }

    private MetadataAssembly(string fileName, byte[] bytes)
    {
        FileName = fileName;
        Sha256 = System.Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        _pe = new PEReader(new MemoryStream(bytes, false));
        if (!_pe.HasMetadata)
        {
            throw new InvalidDataException(fileName + " has no CLI metadata");
        }

        _md = _pe.GetMetadataReader();
    }

    public static MetadataAssembly Open(string path)
    {
        return new MetadataAssembly(Path.GetFileName(path), File.ReadAllBytes(path));
    }

    public static MetadataAssembly FromBytes(string fileName, byte[] bytes)
    {
        return new MetadataAssembly(fileName, bytes);
    }

    public void Dispose()
    {
        _pe.Dispose();
    }

    public IReadOnlyCollection<string> UserStrings
    {
        get
        {
            if (_userStrings == null)
            {
                var set = new HashSet<string>(StringComparer.Ordinal);
                var handle = MetadataTokens.UserStringHandle(0);
                while (true)
                {
                    handle = _md.GetNextHandle(handle);
                    if (handle.IsNil)
                    {
                        break;
                    }

                    set.Add(_md.GetUserString(handle));
                }

                _userStrings = set;
            }

            return _userStrings;
        }
    }

    public bool HasUserString(string value)
    {
        return ((HashSet<string>)UserStrings).Contains(value);
    }

    public IReadOnlyList<TypeMatch> FindTypes(string typeName)
    {
        EnsureIndex();
        var ns = "";
        var simple = typeName;
        var dot = typeName.LastIndexOf('.');
        if (dot > 0)
        {
            ns = typeName.Substring(0, dot);
            simple = typeName.Substring(dot + 1);
        }

        var result = new List<TypeMatch>();
        if (!_byName.TryGetValue(simple, out var handles))
        {
            return result;
        }

        foreach (var h in handles)
        {
            var t = _md.GetTypeDefinition(h);
            var tns = _md.GetString(t.Namespace);
            if (ns.Length > 0 && !string.Equals(ns, tns, StringComparison.Ordinal))
            {
                continue;
            }

            result.Add(new TypeMatch { Namespace = tns, Name = simple, Members = EnumMembers(t) });
        }

        return result;
    }

    private IReadOnlyList<string> EnumMembers(TypeDefinition type)
    {
        var members = new List<string>();
        foreach (var fh in type.GetFields())
        {
            var f = _md.GetFieldDefinition(fh);
            if ((f.Attributes & FieldAttributes.Static) == 0 || (f.Attributes & FieldAttributes.Literal) == 0)
            {
                continue;
            }

            members.Add(_md.GetString(f.Name));
        }

        return members;
    }

    private void EnsureIndex()
    {
        if (_byName != null)
        {
            return;
        }

        var map = new Dictionary<string, List<TypeDefinitionHandle>>(StringComparer.Ordinal);
        foreach (var h in _md.TypeDefinitions)
        {
            var t = _md.GetTypeDefinition(h);
            if (!t.GetDeclaringType().IsNil)
            {
                continue;
            }

            var name = _md.GetString(t.Name);
            if (!map.TryGetValue(name, out var list))
            {
                list = new List<TypeDefinitionHandle>();
                map[name] = list;
            }

            list.Add(h);
        }

        _byName = map;
    }
}
