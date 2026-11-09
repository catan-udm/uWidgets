using System;
using Avalonia.Media;
using Media.Locales;
using Media.Models;
using Media.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Media.ViewModels;

public class NowPlayingViewModel : ReactiveObject, IDisposable
{
    private static readonly StreamGeometry PlayGeometry = StreamGeometry.Parse("M4.5 3.5A.5.5 0 0 1 5.27.08L12.77 4.58a.5.5 0 0 1 0 .84L5.27 9.92A.5.5 0 0 1 4.5 9.5V3.5z");
    private static readonly StreamGeometry PauseGeometry = StreamGeometry.Parse("M5.5 3.5A1.5 1.5 0 0 1 7 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5zm5 0A1.5 1.5 0 0 1 12 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5z");

    private readonly NowPlayingModel model;
    private string trackTitle = Locale.Media_NowPlaying_Title;
    private string artistOrApp = Locale.Media_SystemAudio;
    private bool isPlaying;
    private double peakPercentage;

    public NowPlayingViewModel(NowPlayingModel model)
    {
        this.model = model;
        Update();
        TimerService.Timer1Second.Subscribe(Update);
    }

    public bool ShowVisualizer => model.ShowVisualizer;
    public bool ShowMuteButton => model.ShowMuteButton;

    public string TrackTitle
    {
        get => trackTitle;
        private set
        {
            this.RaiseAndSetIfChanged(ref trackTitle, value);
            this.RaisePropertyChanged(nameof(TooltipText));
        }
    }

    public string ArtistOrApp
    {
        get => artistOrApp;
        private set
        {
            this.RaiseAndSetIfChanged(ref artistOrApp, value);
            this.RaisePropertyChanged(nameof(TooltipText));
        }
    }

    public bool IsPlaying
    {
        get => isPlaying;
        private set
        {
            this.RaiseAndSetIfChanged(ref isPlaying, value);
            this.RaisePropertyChanged(nameof(PlayPauseIcon));
            this.RaisePropertyChanged(nameof(StatusText));
        }
    }

    public double PeakPercentage
    {
        get => peakPercentage;
        private set
        {
            this.RaiseAndSetIfChanged(ref peakPercentage, value);
            this.RaisePropertyChanged(nameof(StatusText));
        }
    }

    public StreamGeometry PlayPauseIcon => IsPlaying ? PauseGeometry : PlayGeometry;
    public string TooltipText => $"{TrackTitle} — {ArtistOrApp}";
    public string StatusText => IsPlaying ? $"{PeakPercentage:0}%" : Locale.Media_NoTrack;

    public void PlayPause()
    {
        MediaService.PlayPause();
        IsPlaying = !IsPlaying;
    }

    public void NextTrack() => MediaService.NextTrack();

    public void PreviousTrack() => MediaService.PreviousTrack();

    public void ToggleMute() => MediaService.ToggleMute();

    private void Update()
    {
        var snapshot = MediaService.GetSnapshot();
        TrackTitle = snapshot.Title;
        ArtistOrApp = snapshot.ArtistOrApp;
        IsPlaying = snapshot.IsPlaying;
        PeakPercentage = Math.Clamp(snapshot.PeakLevel * 100.0, 0.0, 100.0);
    }

    public void Dispose()
    {
        TimerService.Timer1Second.Unsubscribe(Update);
        GC.SuppressFinalize(this);
    }
}
