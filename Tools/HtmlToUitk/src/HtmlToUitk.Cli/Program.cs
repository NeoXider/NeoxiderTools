using System;
using System.IO;

namespace HtmlToUitk.Cli
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return CliApp.Run(args, Console.Out, Console.Error);
        }
    }
}
