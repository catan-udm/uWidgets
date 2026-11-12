using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using uWidgets.Core;
using uWidgets.Core.Services;

namespace uWidgets;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        StorageService.Initialize();

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                var fileName = Path.Combine(Const.LogsFolder, "crash_log.txt");
                File.WriteAllText(fileName, $"[{DateTime.UtcNow:O}] UnhandledException: {ex?.Message}{Environment.NewLine}{ex?.StackTrace}");
            }
            catch
            {
            }
        };

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            try
            {
                var fileName = Path.Combine(Const.LogsFolder, "crash_log.txt");
                File.AppendAllText(fileName, $"[{DateTime.UtcNow:O}] UnobservedTaskException: {e.Exception.Message}{Environment.NewLine}{e.Exception.StackTrace}");
                e.SetObserved();
            }
            catch
            {
            }
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
                try
                {
                    var fileName = Path.Combine(Const.LogsFolder, "crash_log.txt");
                    File.AppendAllText(fileName, $"[{DateTime.UtcNow:O}] GlossyGlass StartEngine warning: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
                }
                catch
                {
                }
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
            try
            {
                var fileName = Path.Combine(Const.LogsFolder, "crash_log.txt");
                File.WriteAllText(fileName, $"[{DateTime.UtcNow:O}] Fatal: {e.Message}{Environment.NewLine}{e.StackTrace}");
            }
            catch
            {
            }
            throw;
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