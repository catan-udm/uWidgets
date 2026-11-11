using System;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Media.Locales;
using Media.Models;
using Media.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Media.ViewModels;

public class NowPlayingViewModel : ReactiveObject, IDisposable
{
    private static readonly StreamGeometry PlayGeometry = StreamGeometry.Parse("m11.596 8.697-6.363 3.692c-.54.313-1.233-.066-1.233-.697V4.308c0-.63.692-1.01 1.233-.696l6.363 3.692a.802.802 0 0 1 0 1.393z");
    private static readonly StreamGeometry PauseGeometry = StreamGeometry.Parse("M5.5 3.5A1.5 1.5 0 0 1 7 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5zm5 0A1.5 1.5 0 0 1 12 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5z");

    private readonly NowPlayingModel model;
    private string trackTitle = Locale.Media_NowPlaying_Title;
    private string artistOrApp = Locale.Media_SystemAudio;
    private string album = "";
    private bool isPlaying;
    private double peakPercentage;
    private double positionSeconds;
    private double durationSeconds;
    private bool canSeek;
    private bool isUserDragging;
    private Bitmap? albumArt;
    private byte[]? currentThumbnailBytes;
    private bool isUpdating;

    public NowPlayingViewModel(NowPlayingModel model)
    {
        this.model = model;
        Update();
        TimerService.Timer1Second.Subscribe(Update);
    }

    public bool ShowTimeline => model.ShowTimeline;
    public bool ShowVisualizer => model.ShowTimeline;
    public bool ShowMuteButton => model.ShowMuteButton;

    public double PositionSeconds
    {
        get => positionSeconds;
        set
        {
            this.RaiseAndSetIfChanged(ref positionSeconds, value);
            this.RaisePropertyChanged(nameof(PositionText));
            this.RaisePropertyChanged(nameof(TimelineTooltip));
            this.RaisePropertyChanged(nameof(StatusText));
        }
    }

    public double DurationSeconds
    {
        get => durationSeconds;
        private set
        {
            this.RaiseAndSetIfChanged(ref durationSeconds, Math.Max(0.0, value));
            this.RaisePropertyChanged(nameof(SliderMaximum));
            this.RaisePropertyChanged(nameof(DurationText));
            this.RaisePropertyChanged(nameof(TimelineTooltip));
            this.RaisePropertyChanged(nameof(StatusText));
        }
    }

    public double SliderMaximum => DurationSeconds > 0 ? DurationSeconds : 1.0;

    public bool CanSeek
    {
        get => canSeek;
        private set => this.RaiseAndSetIfChanged(ref canSeek, value);
    }

    public string PositionText => FormatTime(PositionSeconds);
    public string DurationText => FormatTime(DurationSeconds);
    public string TimelineTooltip => $"{PositionText} / {DurationText}";

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

    public string Album
    {
        get => album;
        private set
        {
            this.RaiseAndSetIfChanged(ref album, value);
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

    public Bitmap? AlbumArt
    {
        get => albumArt;
        private set
        {
            this.RaiseAndSetIfChanged(ref albumArt, value);
            this.RaisePropertyChanged(nameof(HasAlbumArt));
        }
    }

    public bool HasAlbumArt => AlbumArt != null;

    public StreamGeometry PlayPauseIcon => IsPlaying ? PauseGeometry : PlayGeometry;

    public string TooltipText => string.IsNullOrWhiteSpace(Album)
        ? $"{TrackTitle} — {ArtistOrApp}"
        : $"{TrackTitle} — {ArtistOrApp} ({Album})";

    public string StatusText => IsPlaying
        ? (DurationSeconds > 0 ? $"{PositionText} / {DurationText}" : "Playing")
        : Locale.Media_NoTrack;

    public void OnSeekStarted()
    {
        isUserDragging = true;
    }

    public async void OnSeekCompleted(double targetPositionSeconds)
    {
        if (!isUserDragging) return;
        isUserDragging = false;

        var clamped = Math.Clamp(targetPositionSeconds, 0.0, Math.Max(0.0, DurationSeconds));
        PositionSeconds = clamped;
        await MediaService.SeekAsync(clamped, model.PrefersMusicPlayer, model.AllowBrowserMedia);
    }

    public async void PlayPause()
    {
        await MediaService.PlayPauseAsync(model.PrefersMusicPlayer, model.AllowBrowserMedia);
        IsPlaying = !IsPlaying;
    }

    public async void NextTrack() =>
        await MediaService.NextTrackAsync(model.PrefersMusicPlayer, model.AllowBrowserMedia);

    public async void PreviousTrack() =>
        await MediaService.PreviousTrackAsync(model.PrefersMusicPlayer, model.AllowBrowserMedia);

    public void ToggleMute() => MediaService.ToggleMute();

    private async void Update()
    {
        if (isUpdating) return;
        isUpdating = true;

        try
        {
            var snapshot = await MediaService.GetSnapshotAsync(model.PrefersMusicPlayer, model.AllowBrowserMedia);

            bool thumbnailChanged = !ByteArraysEqual(snapshot.ThumbnailBytes, currentThumbnailBytes);
            Bitmap? newBitmap = null;

            if (thumbnailChanged && snapshot.ThumbnailBytes != null)
            {
                try
                {
                    using var ms = new MemoryStream(snapshot.ThumbnailBytes);
                    newBitmap = new Bitmap(ms);
                }
                catch
                {
                    newBitmap = null;
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TrackTitle = snapshot.Title;
                ArtistOrApp = snapshot.ArtistOrApp;
                Album = snapshot.Album;
                IsPlaying = snapshot.IsPlaying;
                PeakPercentage = Math.Clamp(snapshot.PeakLevel * 100.0, 0.0, 100.0);

                if (!isUserDragging)
                {
                    PositionSeconds = Math.Clamp(snapshot.PositionSeconds, 0.0, Math.Max(0.0, snapshot.DurationSeconds));
                }
                DurationSeconds = snapshot.DurationSeconds;
                CanSeek = snapshot.CanSeek;

                if (thumbnailChanged)
                {
                    var old = AlbumArt;
                    AlbumArt = newBitmap;
                    currentThumbnailBytes = snapshot.ThumbnailBytes;
                    old?.Dispose();
                }
            });
        }
        catch
        {
            // Ignore background tick exceptions
        }
        finally
        {
            isUpdating = false;
        }
    }

    private static string FormatTime(double totalSeconds)
    {
        if (double.IsNaN(totalSeconds) || double.IsInfinity(totalSeconds) || totalSeconds <= 0)
            return "00:00";

        var ts = TimeSpan.FromSeconds(totalSeconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private static bool ByteArraysEqual(byte[]? a, byte[]? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null || a.Length != b.Length) return false;
        return a.AsSpan().SequenceEqual(b.AsSpan());
    }

    public void Dispose()
    {
        TimerService.Timer1Second.Unsubscribe(Update);
        if (albumArt != null)
        {
            albumArt.Dispose();
            albumArt = null;
        }
        currentThumbnailBytes = null;
        GC.SuppressFinalize(this);
    }
}
