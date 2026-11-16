using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Clock.Models;
using Clock.ViewModels;
using uWidgets.Core.Interfaces;

namespace Clock.Views;

public partial class Pomodoro : UserControl
{
    private readonly PomodoroViewModel viewModel;

    public Pomodoro() : this(null, null) { }

    public Pomodoro(PomodoroModel model) : this(null, model) { }

    public Pomodoro(IWidgetLayoutProvider? widgetLayoutProvider, PomodoroModel? model = null)
    {
        var id = widgetLayoutProvider?.Get()?.Id ?? Guid.Empty;
        viewModel = new PomodoroViewModel(model ?? new PomodoroModel(), id);
        DataContext = viewModel;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void StartPause_OnClick(object? sender, RoutedEventArgs e) => viewModel.ToggleStartPause();

    private void Reset_OnClick(object? sender, RoutedEventArgs e) => viewModel.Reset();

    private void Skip_OnClick(object? sender, RoutedEventArgs e) => viewModel.SkipPhase();

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        viewModel.Dispose();
    }
}
