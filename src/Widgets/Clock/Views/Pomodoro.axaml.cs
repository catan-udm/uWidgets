using Avalonia.Controls;
using Avalonia.Interactivity;
using Clock.Models;
using Clock.ViewModels;

namespace Clock.Views;

public partial class Pomodoro : UserControl
{
    private readonly PomodoroViewModel viewModel;

    public Pomodoro() : this(new PomodoroModel()) { }

    public Pomodoro(PomodoroModel model)
    {
        viewModel = new PomodoroViewModel(model);
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
