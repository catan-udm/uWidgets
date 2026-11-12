using System;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using uWidgets.Core.Interfaces;
using uWidgets.Services;
using uWidgets.ViewModels;

namespace uWidgets.Views.Pages;

public partial class General : UserControl
{
    private readonly GeneralViewModel viewModel;

    public General(IAppSettingsProvider appSettingsProvider, HotkeyService? hotkeyService = null)
    {
        viewModel = new GeneralViewModel(appSettingsProvider, hotkeyService);
        DataContext = viewModel;
        InitializeComponent();
    }

    private void ResetHotkeys_OnClick(object? sender, RoutedEventArgs e) =>
        viewModel.ResetHotkeysToDefault();

    private void RestartNow_OnClick(object? sender, RoutedEventArgs e)
    {
        var executablePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(executablePath))
        {
            var process = Process.Start(executablePath, "--settings");
            var tryCount = 0;
            while (process != null && process.MainWindowHandle == IntPtr.Zero && !process.HasExited && tryCount++ < 10)
            {
                Thread.Sleep(100);
                process.Refresh();
            }
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
            desktopApp.Shutdown();
    }
}