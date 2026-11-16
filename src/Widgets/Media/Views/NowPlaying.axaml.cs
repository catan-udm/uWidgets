using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Media.Models;
using Media.ViewModels;
using Media.Views.Controls;

namespace Media.Views;

public partial class NowPlaying : UserControl, IDisposable
{
    private readonly NowPlayingViewModel viewModel;

    public NowPlaying() : this(new NowPlayingModel()) { }

    public NowPlaying(NowPlayingModel model)
    {
        InitializeComponent();
        viewModel = new NowPlayingViewModel(model);
        DataContext = viewModel;
        Content = new NowPlayingSmall(viewModel);
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        viewModel.IsUltraCompactTiny = e.NewSize.Height < 80;

        var newType = e.NewSize switch
        {
            { Width: <= 120 } or { Height: <= 120 } => typeof(NowPlayingTiny),
            { Width: > 450, Height: > 230 } => typeof(NowPlayingExtraLarge),
            { Width: > 230, Height: > 230 } => typeof(NowPlayingLarge),
            { Width: > 230 } => typeof(NowPlayingWide),
            _ => typeof(NowPlayingSmall)
        };

        if (Content?.GetType() == newType) return;

        Content = (Control?)Activator.CreateInstance(newType, viewModel);
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Dispose();
    }

    public void Dispose()
    {
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
        viewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
