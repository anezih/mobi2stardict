using ConsoleAppFramework;
using System.Text;

namespace MobiDict.Cli;

internal class Program
{
    public static async Task Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        await ConsoleApp.RunAsync(args, Cli.Run);
    }
}
