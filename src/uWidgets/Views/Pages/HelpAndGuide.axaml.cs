using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using uWidgets.Core;
using uWidgets.Core.Interfaces;
using uWidgets.Services;

namespace uWidgets.Views.Pages;

public partial class HelpAndGuide : UserControl
{
    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly ILayoutProvider layoutProvider;
    private readonly WidgetPlacementService placementService;

    public HelpAndGuide(
        IAppSettingsProvider appSettingsProvider,
        ILayoutProvider layoutProvider,
        WidgetPlacementService placementService)
    {
        this.appSettingsProvider = appSettingsProvider;
        this.layoutProvider = layoutProvider;
        this.placementService = placementService;
        DataContext = this;
        InitializeComponent();
    }

    public string OpenSettingsHotkey => appSettingsProvider.Get().EffectiveHotkeys.OpenSettings;
    public string ToggleWidgetsHotkey => appSettingsProvider.Get().EffectiveHotkeys.ToggleWidgets;
    public string ToggleLockHotkey => appSettingsProvider.Get().EffectiveHotkeys.ToggleLock;
    public string StoragePathDescription => $"Active Data Directory: {Const.CurrentFolder}";

    private void ReplayWelcome_OnClick(object? sender, RoutedEventArgs e)
    {
        var wizard = new WelcomeModal(
            appSettingsProvider,
            layoutProvider,
            placementService,
            _ => { });

        if (VisualRoot is Window owner)
            _ = wizard.ShowDialog(owner);
        else
            wizard.Show();
    }

    private void OpenDataFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Const.CurrentFolder);
        Process.Start(new ProcessStartInfo(Const.CurrentFolder) { UseShellExecute = true });
    }

    private void OpenLogsFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Const.LogsFolder);
        Process.Start(new ProcessStartInfo(Const.LogsFolder) { UseShellExecute = true });
    }
}
