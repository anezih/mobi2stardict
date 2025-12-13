using ConsoleAppFramework;

namespace MobiDict.Cli;

internal class Program
{
    public static async Task Main(string[] args)
    {
        await ConsoleApp.RunAsync(args, Cli.Run);
    }
}
