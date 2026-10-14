namespace HtmlToUitk.Capabilities.Reader;

public static class Program
{
    public static int Main(string[] args)
    {
        return ReaderCli.Run(args, System.Console.Out, System.Console.Error);
    }
}
