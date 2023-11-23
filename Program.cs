using System.Reflection;
using DumpSymbols;
using static System.Console;
using static DumpSymbols.Dumper;

var settings = new Settings();
var dir = args.Length >= 1 ? args[0] : ".";
var files = Directory.GetFiles(dir, "*.dll");
var symbols = new List<string>();
foreach (var file in files) {
    var prefix = Path.GetFileNameWithoutExtension(file) + ": ";
    try {
        var fullPath = Path.GetFullPath(file);
        var assembly = Assembly.LoadFile(fullPath);
        symbols.AddRange(ListSymbols(assembly, settings, prefix));
    }
    catch (Exception e) {
        var message = $"{prefix}{e.GetType().Name}, {e.Message}";
        symbols.Add(message);
    }
}

symbols.Sort(StringComparer.InvariantCulture);
foreach (var s in symbols)
    WriteLine(s);
