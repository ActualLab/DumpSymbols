using System.Reflection;
using System.Security.Cryptography;
using Microsoft.Toolkit.HighPerformance;

namespace DumpSymbols;

public sealed record Settings
{
    public bool JustTypes { get; init; } = true;
    public bool IncludeFields { get; init; } = true;
    public bool IncludeProperties { get; init; } = false;
    public bool IncludeMethods { get; init; } = true;
}

public static class Dumper
{
    private static readonly BindingFlags BindingFlags =
        BindingFlags.Static
        | BindingFlags.Instance
        | BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.DeclaredOnly;

    public static IEnumerable<string> ListSymbols(Assembly assembly, Settings settings, string prefix)
    {
        foreach (var type in assembly.GetTypes()) {
            if (settings.JustTypes) {
                var text = string.Join(", ", ListSymbols(type, settings, ""));
                var hash = SHA256.HashData(text.AsSpan().Cast<char, byte>());
                var b64Hash = Convert.ToBase64String(hash)[..16];
                yield return $"{prefix}{type.FullName} -> {b64Hash}";
                continue;
            }
            foreach (var symbol in ListSymbols(type, settings, prefix))
                yield return symbol;
        }
    }

    static IEnumerable<string> ListSymbols(Type type, Settings settings, string prefix)
    {
        prefix = $"{prefix}{type.FullName} -> ";
        if (settings.IncludeFields)
            foreach (var symbol in ListFields(type, prefix))
                yield return symbol;
        if (settings.IncludeProperties)
            foreach (var symbol in ListProperties(type, prefix))
                yield return symbol;
        if (settings.IncludeMethods)
            foreach (var symbol in ListMethods(type, prefix))
                yield return symbol;
    }

    private static IEnumerable<string> ListFields(Type type, string prefix)
    {
        foreach (var f in type.GetFields(BindingFlags)) {
            yield return $"{prefix}{Describe(f.IsStatic, f.IsPrivate)}{f.Name}: {f.FieldType.Name}";
        }
    }

    private static IEnumerable<string> ListProperties(Type type, string prefix)
    {
        foreach (var p in type.GetProperties(BindingFlags)) {
            if (p.GetMethod is { } getter)
                yield return Describe(getter, prefix);
            if (p.SetMethod is { } setter)
                yield return Describe(setter, prefix);
        }
    }

    private static IEnumerable<string> ListMethods(Type type, string prefix)
    {
        foreach (var m in type.GetMethods(BindingFlags))
            yield return Describe(m, prefix);
    }

    private static string Describe(MethodInfo m, string prefix)
        => $"{prefix}{Describe(m.IsStatic, m.IsPrivate)}{m.Name}({m.GetParameters().Length}): {m.ReturnType.Name}";

    private static string Describe(bool isStatic, bool isPrivate)
        => (isStatic, isPrivate) switch {
            (true, true) => "[static, private] ",
            (true, false) => "[static] ",
            (false, true) => "[private] ",
            _ => "",
        };
}
