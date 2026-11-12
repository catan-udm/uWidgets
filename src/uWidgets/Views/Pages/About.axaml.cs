using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using uWidgets.Core;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models.Settings;
using uWidgets.Locales;
using uWidgets.Services;

namespace uWidgets.Views.Pages;

public partial class About : UserControl
{
    private readonly IAppSettingsProvider appSettingsProvider;
    public string? Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    public string DataFolderPath => Const.CurrentFolder;
    
    public About(IAppSettingsProvider appSettingsProvider)
    {
        this.appSettingsProvider = appSettingsProvider;
        DataContext = this;
        _ = CheckForUpdates();
        InitializeComponent();
    }
    
    private async Task CheckForUpdates()
    {
        var version = await new UpdateService(appSettingsProvider).GetUpdateVersionAsync();
        if (version == null) return;
        
        Update.IsVisible = true;
        Update.Subtitle = string.Format(Locale.Update_UpdateFormat, version);
    }
    
    private void GoToUpdate(object? sender, RoutedEventArgs e)
    {
        Process.Start(
            new ProcessStartInfo("https://www.github.com/creewick/uWidgets/releases/latest") { UseShellExecute = true });
    }

    private void GoToRepository(object? sender, RoutedEventArgs e)
    {
        Process.Start(
            new ProcessStartInfo("https://www.github.com/creewick/uWidgets") { UseShellExecute = true });
    }

    private void GoToIssues(object? sender, RoutedEventArgs e)
    {
        Process.Start(
            new ProcessStartInfo("https://github.com/creewick/uWidgets/issues") { UseShellExecute = true });
    }

    private void OpenDataFolder(object? sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Const.CurrentFolder);
        Process.Start(new ProcessStartInfo(Const.CurrentFolder) { UseShellExecute = true });
    }

    private void OpenLogsFolder(object? sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Const.LogsFolder);
        Process.Start(new ProcessStartInfo(Const.LogsFolder) { UseShellExecute = true });
    }

    private void ResetDefaults(object? sender, RoutedEventArgs e)
    {
        var current = appSettingsProvider.Get();
        var defaults = AppSettings.CreateDefault() with { Onboarding = current.EffectiveOnboarding };
        appSettingsProvider.Save(defaults);
    }
}