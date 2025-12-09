using ConsoleAppFramework;
using System.Text;
using Uno.UI.Hosting;

namespace MobiDict.Uno.Platforms.Desktop;

internal class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        if (args.Length > 0)
        {
            await ConsoleApp.RunAsync(args, Cli.Run);
        }
        else
        {
            App.InitializeLogging();

            var host = UnoPlatformHostBuilder.Create()
                .App(() => new App())
                .UseX11()
                .UseLinuxFrameBuffer()
                .UseMacOS()
                .UseWin32()
                .Build();

            host.Run();
        }
    }
}
