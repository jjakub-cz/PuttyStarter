using System;
using System.IO;

namespace PuttyStarter;

public sealed class Logger
{
    private readonly string _dir;
    private readonly string _file;
    private readonly object _lock = new();

    private const string Name = "PuttyStarter.log";

    public Logger(string exeDir)
    {
        _dir = exeDir;
        _file = Path.Combine(_dir, Name);
        RotateIfNeeded();
    }

    public void Info(string msg) => Write("INFO", msg);
    public void Warn(string msg) => Write("WARN", msg);
    public void Error(string msg) => Write("ERROR", msg);

    private void Write(string level, string msg)
    {
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_file, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {msg}{Environment.NewLine}");
                RotateIfNeeded();
            }
            catch { /* ignore */ }
        }
    }

    private void RotateIfNeeded()
    {
        try
        {
            const int maxSizeKb = 256;
            if (File.Exists(_file) && new FileInfo(_file).Length > maxSizeKb * 1024)
            {
                var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.Move(_file, Path.Combine(_dir, $"PuttyStarter_{ts}.log"), overwrite: true);

                // Keep last 3
                var files = Directory.GetFiles(_dir, "PuttyStarter_*.log");
                Array.Sort(files, StringComparer.Ordinal);
                for (int i = 0; i < files.Length - 3; i++)
                    File.Delete(files[i]);
            }
        }
        catch { /* ignore */ }
    }
}
