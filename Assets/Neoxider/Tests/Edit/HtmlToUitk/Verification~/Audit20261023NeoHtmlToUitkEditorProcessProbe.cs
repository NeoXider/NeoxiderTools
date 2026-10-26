using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

internal static class Audit20261023NeoHtmlToUitkEditorProcessProbe
{
    private static int Main(string[] args)
    {
        if (args.Length < 2 || args[0] != "run") return 3;
        Console.OutputEncoding = new UTF8Encoding(false);
        Console.Error.WriteLine("owned process probe stderr");
        using (Process self = Process.GetCurrentProcess())
            File.WriteAllText(args[1] + ".pid", self.Id.ToString(CultureInfo.InvariantCulture) + "|" + self.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture));
        string mode = File.ReadAllText(args[1]).Trim();
        if (mode == "wait") Thread.Sleep(Timeout.Infinite);
        Console.WriteLine("{\"verdict\":\"PASS\",\"counts\":{\"error\":0},\"findings\":[]}");
        return 0;
    }
}
