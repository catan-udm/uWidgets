using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
    private static readonly StreamGeometry SpeakerGeometry = StreamGeometry.Parse("M11.536 14.01A8.47 8.47 0 0 0 14.026 8a8.47 8.47 0 0 0-2.49-6.01l-.708.707A7.48 7.48 0 0 1 13.025 8c0 2.071-.84 3.946-2.197 5.303zM10.121 12.6A6.47 6.47 0 0 0 12.025 8a6.47 6.47 0 0 0-1.904-4.596l-.707.707A5.48 5.48 0 0 1 11.025 8a5.48 5.48 0 0 1-1.61 3.89zM8.707 11.182A4.5 4.5 0 0 0 10.025 8a4.5 4.5 0 0 0-1.318-3.182L8 5.525A3.5 3.5 0 0 1 9.025 8 3.5 3.5 0 0 1 8 10.475zM6.717 3.55A.5.5 0 0 1 7 4v8a.5.5 0 0 1-.812.39L3.825 10.5H1.5A.5.5 0 0 1 1 10V6a.5.5 0 0 1 .5-.5h2.325l2.363-1.89a.5.5 0 0 1 .529-.06");
    private static readonly StreamGeometry SpeakerMuteGeometry = StreamGeometry.Parse("M6.717 3.55A.5.5 0 0 1 7 4v8a.5.5 0 0 1-.812.39L3.825 10.5H1.5A.5.5 0 0 1 1 10V6a.5.5 0 0 1 .5-.5h2.325l2.363-1.89a.5.5 0 0 1 .529-.06m7.137 2.096a.5.5 0 0 1 0 .708L12.207 8l1.647 1.646a.5.5 0 0 1-.708.708L11.5 8.707l-1.646 1.647a.5.5 0 0 1-.708-.708L10.793 8 9.146 6.354a.5.5 0 1 1 .708-.708L11.5 7.293l1.646-1.647a.5.5 0 0 1 .708 0");

    private readonly NowPlayingModel model;
    private readonly ILyricsService? lyricsService;
    private int activeLyricsFetchId;
    private string trackTitle = Locale.Media_NowPlaying_Title;
    private string artistOrApp = Locale.Media_SystemAudio;
    private string album = "";
    private string currentAppId = "";
    private bool isPlaying;
    private bool isAppMuted;
    private double peakPercentage;
    private double positionSeconds;
    private double durationSeconds;
    private bool canSeek;
    private bool isUserDragging;
    private Bitmap? albumArt;
    private byte[]? currentThumbnailBytes;
    private bool isUpdating;

    // Hysteresis / grace period (R3)
    private int fallbackGraceTicks;
    private const int FallbackGracePeriodTicks = 5;

    // Extensible lyrics fields (R4)
    private string? currentLyrics;
    private string? currentLyricLine;
    private bool isLyricsLoading;
    private CancellationTokenSource? lyricsCts;

    public NowPlayingViewModel(NowPlayingModel model, ILyricsService? lyricsService = null)
    {
        this.model = model;
        this.lyricsService = lyricsService;
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

    public bool IsAppMuted
    {
        get => isAppMuted;
        private set
        {
            this.RaiseAndSetIfChanged(ref isAppMuted, value);
            this.RaisePropertyChanged(nameof(MuteIcon));
        }
    }

    public StreamGeometry PlayPauseIcon => IsPlaying ? PauseGeometry : PlayGeometry;
    public StreamGeometry MuteIcon => IsAppMuted ? SpeakerMuteGeometry : SpeakerGeometry;

    public string TooltipText => string.IsNullOrWhiteSpace(Album)
        ? $"{TrackTitle} — {ArtistOrApp}"
        : $"{TrackTitle} — {ArtistOrApp} ({Album})";

    public string StatusText => IsPlaying && DurationSeconds > 0
        ? $"{PositionText} / {DurationText}"
        : "";

    public string? CurrentLyrics
    {
        get => currentLyrics;
        private set
        {
            this.RaiseAndSetIfChanged(ref currentLyrics, value);
            this.RaisePropertyChanged(nameof(HasLyrics));
        }
    }

    public string? CurrentLyricLine
    {
        get => currentLyricLine;
        private set => this.RaiseAndSetIfChanged(ref currentLyricLine, value);
    }

    public bool HasLyrics => !string.IsNullOrWhiteSpace(CurrentLyrics);

    public bool IsLyricsLoading
    {
        get => isLyricsLoading;
        private set => this.RaiseAndSetIfChanged(ref isLyricsLoading, value);
    }

    public async Task FetchLyricsAsync(string title, string artist, double duration, CancellationToken ct = default)
    {
        if (lyricsService == null || string.IsNullOrWhiteSpace(title))
        {
            CurrentLyrics = null;
            CurrentLyricLine = null;
            IsLyricsLoading = false;
            return;
        }

        int fetchId = Interlocked.Increment(ref activeLyricsFetchId);

        try
        {
            IsLyricsLoading = true;
            var lyrics = await lyricsService.GetLyricsAsync(title, artist, duration, ct);
            if (!ct.IsCancellationRequested && fetchId == activeLyricsFetchId)
            {
                CurrentLyrics = lyrics;
                CurrentLyricLine = null;
            }
        }
        catch
        {
            if (!ct.IsCancellationRequested && fetchId == activeLyricsFetchId)
            {
                CurrentLyrics = null;
                CurrentLyricLine = null;
            }
        }
        finally
        {
            if (fetchId == activeLyricsFetchId)
            {
                IsLyricsLoading = false;
            }
        }
    }

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

    public void ToggleMute() => IsAppMuted = MediaService.ToggleAppMute(currentAppId);

    private async void Update()
    {
        if (isUpdating)
        {
            // If an update is in progress, smoothly advance position locally if actively playing
            if (isPlaying && durationSeconds > 0 && !isUserDragging)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (positionSeconds < durationSeconds)
                    {
                        PositionSeconds = Math.Min(durationSeconds, positionSeconds + 1.0);
                    }
                });
            }
            return;
        }
        isUpdating = true;

        try
        {
            var snapshot = await MediaService.GetSnapshotAsync(model.PrefersMusicPlayer, model.AllowBrowserMedia);

            bool isFallbackSnapshot = string.Equals(snapshot.Title, Locale.Media_NowPlaying_Title, StringComparison.OrdinalIgnoreCase);

            // R3 Hysteresis: add grace period before falling back to system audio default text
            if (isFallbackSnapshot)
            {
                bool hasActiveTrack = !string.Equals(trackTitle, Locale.Media_NowPlaying_Title, StringComparison.OrdinalIgnoreCase);
                if (hasActiveTrack && fallbackGraceTicks < FallbackGracePeriodTicks)
                {
                    fallbackGraceTicks++;
                    // In grace period: update peak meter, mark paused, but do not clear track metadata or album art
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        IsPlaying = false;
                        PeakPercentage = Math.Clamp(snapshot.PeakLevel * 100.0, 0.0, 100.0);
                    });
                    return;
                }
            }
            else
            {
                fallbackGraceTicks = 0;
            }

            bool trackChanged = !string.Equals(snapshot.Title, trackTitle, StringComparison.Ordinal)
                || !string.Equals(snapshot.ArtistOrApp, artistOrApp, StringComparison.Ordinal)
                || !string.Equals(snapshot.Album, album, StringComparison.Ordinal);

            bool thumbnailChanged = !ByteArraysEqual(snapshot.ThumbnailBytes, currentThumbnailBytes);
            Bitmap? newBitmap = null;
            bool decodeFailed = false;

            if (thumbnailChanged)
            {
                if (snapshot.ThumbnailBytes != null && snapshot.ThumbnailBytes.Length > 0)
                {
                    try
                    {
                        using var ms = new MemoryStream(snapshot.ThumbnailBytes);
                        newBitmap = new Bitmap(ms);
                    }
                    catch
                    {
                        // Decode failed: flag so currentThumbnailBytes is not updated (prevents cache poisoning)
                        newBitmap = null;
                        decodeFailed = true;
                    }
                }
                else
                {
                    // R1: Do not wipe AlbumArt if snapshot.ThumbnailBytes is momentarily null but the track is still active
                    if (!trackChanged && !isFallbackSnapshot && albumArt != null)
                    {
                        thumbnailChanged = false;
                    }
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                currentAppId = snapshot.SourceAppUserModelId;
                IsAppMuted = MediaService.IsAppMuted(currentAppId);
                TrackTitle = snapshot.Title;
                ArtistOrApp = snapshot.ArtistOrApp;
                Album = snapshot.Album;
                IsPlaying = snapshot.IsPlaying;
                PeakPercentage = Math.Clamp(snapshot.PeakLevel * 100.0, 0.0, 100.0);

                // R2: Assign DurationSeconds and CanSeek BEFORE assigning PositionSeconds!
                // Prevents Avalonia Slider's TwoWay binding from coercing PositionSeconds to 1.0/0.0 when SliderMaximum is default 1.0
                if (snapshot.DurationSeconds > 0)
                {
                    DurationSeconds = snapshot.DurationSeconds;
                    CanSeek = snapshot.CanSeek;
                }
                else if (isFallbackSnapshot)
                {
                    DurationSeconds = 0;
                    CanSeek = false;
                }
                // Else retain previous duration/canSeek when paused or during transient 0

                // R2: Timeline position tracking and local smooth advancement
                if (!isUserDragging)
                {
                    if (snapshot.PositionSeconds > 0)
                    {
                        PositionSeconds = Math.Clamp(snapshot.PositionSeconds, 0.0, SliderMaximum);
                    }
                    else if (trackChanged || isFallbackSnapshot)
                    {
                        PositionSeconds = 0;
                    }
                    else if (snapshot.IsPlaying && DurationSeconds > 0 && PositionSeconds > 0 && PositionSeconds < DurationSeconds)
                    {
                        // Smoothly advance locally during active playback
                        PositionSeconds = Math.Min(DurationSeconds, PositionSeconds + 1.0);
                    }
                    else if (!snapshot.IsPlaying && PositionSeconds > 0)
                    {
                        // Retain position when paused
                    }
                    else
                    {
                        PositionSeconds = Math.Clamp(snapshot.PositionSeconds, 0.0, SliderMaximum);
                    }
                }

                // R1: Safely update AlbumArt and dispose old bitmaps without render-thread contention
                if (thumbnailChanged)
                {
                    if (decodeFailed)
                    {
                        if (trackChanged || isFallbackSnapshot)
                        {
                            var old = AlbumArt;
                            AlbumArt = null;
                            if (old != null)
                            {
                                Dispatcher.UIThread.Post(() => old.Dispose(), DispatcherPriority.Background);
                            }
                        }
                        currentThumbnailBytes = snapshot.ThumbnailBytes;
                    }
                    else
                    {
                        var old = AlbumArt;
                        AlbumArt = newBitmap;
                        currentThumbnailBytes = snapshot.ThumbnailBytes;
                        if (old != null)
                        {
                            Dispatcher.UIThread.Post(() => old.Dispose(), DispatcherPriority.Background);
                        }
                    }
                }
            });

            // R4: Trigger lyrics update on track change
            if (trackChanged && lyricsService != null && !isFallbackSnapshot)
            {
                lyricsCts?.Cancel();
                lyricsCts?.Dispose();
                lyricsCts = new CancellationTokenSource();
                _ = FetchLyricsAsync(snapshot.Title, snapshot.ArtistOrApp, snapshot.DurationSeconds, lyricsCts.Token);
            }
            else if (isFallbackSnapshot && lyricsService != null)
            {
                Interlocked.Increment(ref activeLyricsFetchId);
                lyricsCts?.Cancel();
                lyricsCts?.Dispose();
                lyricsCts = null;
                CurrentLyrics = null;
                CurrentLyricLine = null;
                IsLyricsLoading = false;
            }
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
        Interlocked.Increment(ref activeLyricsFetchId);
        lyricsCts?.Cancel();
        lyricsCts?.Dispose();
        lyricsCts = null;
        IsLyricsLoading = false;
        if (albumArt != null)
        {
            albumArt.Dispose();
            albumArt = null;
        }
        currentThumbnailBytes = null;
        GC.SuppressFinalize(this);
    }
}
