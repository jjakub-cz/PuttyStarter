using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PuttyStarter;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        const string mutexName = "Global\\PuttyStarter_Mutex";
        using var mutex = new Mutex(initiallyOwned: true, name: mutexName, out bool isNew);
        if (!isNew)
        {
            // Už běží – pošleme jí „Open“ (pro jednoduchost jen ukončíme).
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        var exeDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;
        var confPath = Path.Combine(exeDir, "PuttyStarter.conf");
        //var exeDir = AppContext.BaseDirectory;
        //var confPath = Path.Combine(exeDir, "PuttyStarter.conf");
        var logger = new Logger(exeDir);

        var config = AppConfig.LoadOrCreate(confPath, logger);

        using var app = new AppContextEx(config, confPath, logger);
        Application.Run(app);
    }
}
