using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using uWidgets.Core;
using uWidgets.Core.Services;

namespace uWidgets;

class Program
{
    private const int MaxCrashLogBytes = 64 * 1024;
    private static readonly object crashLogLock = new();

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        StorageService.Initialize();

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            AppendCrashLog($"[{DateTime.UtcNow:O}] UnhandledException: {ex?.Message}{Environment.NewLine}{ex?.StackTrace}");
        };

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            AppendCrashLog($"[{DateTime.UtcNow:O}] UnobservedTaskException: {e.Exception.Message}{Environment.NewLine}{e.Exception.StackTrace}");
            e.SetObserved();
        };

        try
        {
            try
            {
                GlossyGlass.GlossyGlassApi.StartEngine();
            }
            catch (Exception ex)
            {
                // Non-fatal if MyDockFinder is not installed or locked
                AppendCrashLog($"[{DateTime.UtcNow:O}] GlossyGlass unavailable: {ex.Message}");
            }

            try
            {
                SkiaSharp.SKGraphics.SetResourceCacheTotalByteLimit(8 * 1024 * 1024);
            }
            catch
            {
            }

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            AppendCrashLog($"[{DateTime.UtcNow:O}] Fatal: {e.Message}{Environment.NewLine}{e.StackTrace}");
            throw;
        }
    }

    private static void AppendCrashLog(string entry)
    {
        try
        {
            lock (crashLogLock)
            {
                Directory.CreateDirectory(Const.LogsFolder);
                var fileName = Path.Combine(Const.LogsFolder, "crash_log.txt");
                if (File.Exists(fileName) && new FileInfo(fileName).Length > MaxCrashLogBytes)
                {
                    var text = File.ReadAllText(fileName);
                    var keepLength = Math.Min(text.Length, MaxCrashLogBytes / 2);
                    var tail = text.Substring(text.Length - keepLength);
                    var firstNewline = tail.IndexOf('\n');
                    if (firstNewline >= 0 && firstNewline + 1 < tail.Length)
                        tail = tail.Substring(firstNewline + 1);
                    File.WriteAllText(fileName, tail);
                }

                File.AppendAllText(fileName, entry + Environment.NewLine);
            }
        }
        catch
        {
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        StorageService.Initialize();
        var settings = new AppSettingsProvider().Get();
        
        return AppBuilder.Configure<App>()
            .UseWin32()
            .UsePlatformDetect()
            .UseReactiveUI()
            .WithInterFont()
            .With(new SkiaOptions { MaxGpuResourceSizeBytes = 16 * 1024 * 1024 })
            .With(new Win32PlatformOptions
            {
                CompositionMode = new[] { Win32CompositionMode.WinUIComposition },
                WinUICompositionBackdropCornerRadius = settings.Theme.UseNativeFrame ? 0 : settings.Dimensions.Radius
            })
            .LogToTrace();
    }
}