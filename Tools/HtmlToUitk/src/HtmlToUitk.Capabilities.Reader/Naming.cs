using System.Text;

namespace HtmlToUitk.Capabilities.Reader;

public static class Naming
{
    public static string Kebab(string member)
    {
        var sb = new StringBuilder(member.Length + 6);
        for (var i = 0; i < member.Length; i++)
        {
            var c = member[i];
            if (char.IsUpper(c))
            {
                var prevLowerOrDigit = i > 0 && (char.IsLower(member[i - 1]) || char.IsDigit(member[i - 1]));
                var acronymEnd = i > 0 && char.IsUpper(member[i - 1]) && i + 1 < member.Length && char.IsLower(member[i + 1]);
                if (prevLowerOrDigit || acronymEnd)
                {
                    sb.Append('-');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    public static string PascalCase(string name)
    {
        var sb = new StringBuilder(name.Length);
        var upper = true;
        foreach (var c in name)
        {
            if (c == '-' || c == ' ' || c == '_')
            {
                upper = true;
                continue;
            }

            sb.Append(upper ? char.ToUpperInvariant(c) : c);
            upper = false;
        }

        return sb.ToString();
    }

    public static string PropertyName(string member)
    {
        if (member.Length > 5 && member.StartsWith("Unity", System.StringComparison.Ordinal) && char.IsUpper(member[5]))
        {
            return "-" + Kebab(member);
        }

        return Kebab(member);
    }
}
