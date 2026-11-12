using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using uWidgets.Core.Interfaces;
using uWidgets.ViewModels;

namespace uWidgets.Views;

public partial class Settings : Window
{
    private readonly SettingsViewModel viewModel;

    public Settings(IAppSettingsProvider appSettingsProvider, IAssemblyProvider assemblyProvider, 
        ILayoutProvider layoutProvider, IWidgetFactory<Window, UserControl> widgetFactory, IServiceProvider serviceProvider)
    {
        viewModel = new SettingsViewModel(appSettingsProvider, assemblyProvider, layoutProvider, widgetFactory, serviceProvider);
        DataContext = viewModel;
        Resized += OnResized;
        KeyDown += OnKeyDown;
        Unloaded += OnUnloaded;
        InitializeComponent();
        ListBox.SelectedItem = SettingsViewModel.MenuItems[0];
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (e.CloseReason == WindowCloseReason.WindowClosing &&
            Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.ShutdownMode == ShutdownMode.OnExplicitShutdown)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    public void ShowAndActivate()
    {
        Show();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Activate();
    }

    public void NavigateToPage(Type pageType)
    {
        var item = viewModel.AllItems.FirstOrDefault(p => p.Type == pageType);
        if (item != null)
            ListBox.SelectedItem = item;
    }

    private void Restart(object? sender, RoutedEventArgs e)
    {
        var executablePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (executablePath == null) return;
        
        var process = Process.Start(executablePath, "--settings");
        var tryCount = 0;
        var maxTryCount = 10;
        
        while (process.MainWindowHandle == IntPtr.Zero && !process.HasExited && tryCount++ < maxTryCount)
        {
            Thread.Sleep(100);
            process.Refresh();
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp) 
            desktopApp.Shutdown();
    }

    private void Exit(object? sender, RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp) 
            desktopApp.Shutdown();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            SplitView.IsPaneOpen = true;
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && SearchBox.IsFocused)
        {
            viewModel.SearchQuery = string.Empty;
            e.Handled = true;
        }
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Resized -= OnResized;
        KeyDown -= OnKeyDown;
    }

    private void OnResized(object? sender, WindowResizedEventArgs e) => 
        SplitView.IsPaneOpen = Width >= 800;

    private void OnMenuItemChanged(object? _, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0)
            viewModel.SetCurrentPage(e.AddedItems[0] as PageViewModel);
    }
}