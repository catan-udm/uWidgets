using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Media.Locales;

namespace Media.Services;

public record MediaSnapshot(
    string Title,
    string ArtistOrApp,
    string Album,
    bool IsPlaying,
    double PeakLevel,
    byte[]? ThumbnailBytes,
    string SourceAppUserModelId,
    double PositionSeconds = 0,
    double DurationSeconds = 0,
    bool CanSeek = false
);

[SupportedOSPlatform("windows10.0.17763.0")]
public static class MediaService
{
    private const byte VK_VOLUME_MUTE = 0xAD;
    private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
    private const byte VK_MEDIA_PREV_TRACK = 0xB1;
    private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static GlobalSystemMediaTransportControlsSessionManager? sessionManager;
    private static bool sessionManagerFailed;

    private static readonly HashSet<string> BrowserKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome",
        "msedge",
        "edge",
        "firefox",
        "brave",
        "opera",
        "vivaldi",
        "arc",
        "zen",
        "waterfox",
        "tor",
        "librewolf",
        "safari"
    };

    private static readonly string[] MusicPlayers =
    [
        "Spotify",
        "MusicBee",
        "vlc",
        "foobar2000",
        "AIMP",
        "AppleMusic",
        "iTunes",
        "wmplayer",
        "TIDAL",
        "Deezer",
        "AmazonMusic"
    ];

    private static readonly string[] BrowserPlayers =
    [
        "msedge",
        "chrome",
        "firefox",
        "brave"
    ];

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public static bool IsBrowserApp(string? sourceAppId)
    {
        if (string.IsNullOrWhiteSpace(sourceAppId)) return false;
        foreach (var browser in BrowserKeywords)
        {
            if (sourceAppId.Contains(browser, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static async Task<GlobalSystemMediaTransportControlsSessionManager?> GetSessionManagerAsync()
    {
        if (sessionManager != null) return sessionManager;
        if (sessionManagerFailed || !OperatingSystem.IsWindows()) return null;

        try
        {
            sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            return sessionManager;
        }
        catch
        {
            sessionManagerFailed = true;
            return null;
        }
    }

    public static GlobalSystemMediaTransportControlsSession? ResolvePrioritizedSession(
        GlobalSystemMediaTransportControlsSessionManager manager,
        bool prefersMusicPlayer = true,
        bool allowBrowserMedia = true)
    {
        IReadOnlyList<GlobalSystemMediaTransportControlsSession> sessions;
        try
        {
            sessions = manager.GetSessions();
        }
        catch
        {
            return null;
        }

        if (sessions == null || sessions.Count == 0) return null;

        var musicSessions = sessions.Where(s => !IsBrowserApp(s.SourceAppUserModelId)).ToList();
        var browserSessions = allowBrowserMedia
            ? sessions.Where(s => IsBrowserApp(s.SourceAppUserModelId)).ToList()
            : [];

        // MODE 1: PrefersMusicPlayer = TRUE (Strict Music Dominance)
        // If a music player is playing OR paused, it stays displayed; browser audio will NOT override it!
        if (prefersMusicPlayer)
        {
            if (musicSessions.Count > 0)
            {
                // 1A. Any music player actively PLAYING
                var playingMusic = musicSessions.FirstOrDefault(s =>
                {
                    try { return s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing; }
                    catch { return false; }
                });
                if (playingMusic != null) return playingMusic;

                // 1B. Any music player PAUSED (holds priority over any active browser video)
                var pausedMusic = musicSessions.FirstOrDefault(s =>
                {
                    try { return s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused; }
                    catch { return false; }
                });
                if (pausedMusic != null) return pausedMusic;

                // 1C. Any other music session
                return musicSessions[0];
            }

            // Only evaluated when ZERO music players are open
            if (allowBrowserMedia && browserSessions.Count > 0)
            {
                var playingBrowser = browserSessions.FirstOrDefault(s =>
                {
                    try { return s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing; }
                    catch { return false; }
                });
                if (playingBrowser != null) return playingBrowser;

                var pausedBrowser = browserSessions.FirstOrDefault(s =>
                {
                    try { return s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused; }
                    catch { return false; }
                });
                if (pausedBrowser != null) return pausedBrowser;

                return browserSessions[0];
            }

            return null;
        }

        // MODE 2: PrefersMusicPlayer = FALSE (Active Playback Follower)
        // Follows whatever is actively playing across all sources
        GlobalSystemMediaTransportControlsSession? currentSession = null;
        try
        {
            currentSession = manager.GetCurrentSession();
        }
        catch { }

        if (currentSession != null)
        {
            bool isBrowser = IsBrowserApp(currentSession.SourceAppUserModelId);
            if (!isBrowser || allowBrowserMedia)
            {
                try
                {
                    if (currentSession.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                        return currentSession;
                }
                catch { }
            }
        }

        var anyPlaying = sessions.FirstOrDefault(s =>
        {
            bool isBrowser = IsBrowserApp(s.SourceAppUserModelId);
            if (isBrowser && !allowBrowserMedia) return false;
            try { return s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing; }
            catch { return false; }
        });
        if (anyPlaying != null) return anyPlaying;

        if (currentSession != null && (!IsBrowserApp(currentSession.SourceAppUserModelId) || allowBrowserMedia))
        {
            return currentSession;
        }

        return sessions.FirstOrDefault(s => !IsBrowserApp(s.SourceAppUserModelId) || allowBrowserMedia);
    }

    private static async Task<byte[]?> ReadThumbnailBytesAsync(IRandomAccessStreamReference? thumbnailRef)
    {
        if (thumbnailRef == null) return null;
        try
        {
            using var stream = await thumbnailRef.OpenReadAsync();
            if (stream == null || stream.Size == 0 || stream.Size > 15 * 1024 * 1024) return null;

            using var reader = new DataReader(stream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)stream.Size);
            var bytes = new byte[stream.Size];
            reader.ReadBytes(bytes);
            return bytes;
        }
        catch
        {
            return null;
        }
    }

    public static async Task PlayPauseAsync(bool prefersMusicPlayer = true, bool allowBrowserMedia = true)
    {
        var manager = await GetSessionManagerAsync();
        if (manager != null)
        {
            var session = ResolvePrioritizedSession(manager, prefersMusicPlayer, allowBrowserMedia);
            if (session != null)
            {
                try
                {
                    var success = await session.TryTogglePlayPauseAsync();
                    if (success) return;
                }
                catch { }
            }
        }

        SendMediaKey(VK_MEDIA_PLAY_PAUSE);
    }

    public static async Task NextTrackAsync(bool prefersMusicPlayer = true, bool allowBrowserMedia = true)
    {
        var manager = await GetSessionManagerAsync();
        if (manager != null)
        {
            var session = ResolvePrioritizedSession(manager, prefersMusicPlayer, allowBrowserMedia);
            if (session != null)
            {
                try
                {
                    var success = await session.TrySkipNextAsync();
                    if (success) return;
                }
                catch { }
            }
        }

        SendMediaKey(VK_MEDIA_NEXT_TRACK);
    }

    public static async Task PreviousTrackAsync(bool prefersMusicPlayer = true, bool allowBrowserMedia = true)
    {
        var manager = await GetSessionManagerAsync();
        if (manager != null)
        {
            var session = ResolvePrioritizedSession(manager, prefersMusicPlayer, allowBrowserMedia);
            if (session != null)
            {
                try
                {
                    var success = await session.TrySkipPreviousAsync();
                    if (success) return;
                }
                catch { }
            }
        }

        SendMediaKey(VK_MEDIA_PREV_TRACK);
    }

    public static void ToggleMute() => SendMediaKey(VK_VOLUME_MUTE);

    public static async Task<bool> SeekAsync(double positionSeconds, bool prefersMusicPlayer = true, bool allowBrowserMedia = true)
    {
        var manager = await GetSessionManagerAsync();
        if (manager != null)
        {
            var session = ResolvePrioritizedSession(manager, prefersMusicPlayer, allowBrowserMedia);
            if (session != null)
            {
                try
                {
                    long ticks = (long)(positionSeconds * TimeSpan.TicksPerSecond);
                    var success = await session.TryChangePlaybackPositionAsync(ticks);
                    return success;
                }
                catch
                {
                    return false;
                }
            }
        }
        return false;
    }

    private static void SendMediaKey(byte vk)
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            keybd_event(vk, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
            keybd_event(vk, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        catch
        {
            // Ignore interop errors
        }
    }

    public static async Task<MediaSnapshot> GetSnapshotAsync(bool prefersMusicPlayer = true, bool allowBrowserMedia = true)
    {
        var peak = GetSystemAudioPeak();

        var manager = await GetSessionManagerAsync();
        if (manager != null)
        {
            var session = ResolvePrioritizedSession(manager, prefersMusicPlayer, allowBrowserMedia);
            if (session != null)
            {
                try
                {
                    var mediaProperties = await session.TryGetMediaPropertiesAsync();
                    var playbackInfo = session.GetPlaybackInfo();
                    bool isPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                    string title = mediaProperties?.Title ?? "";
                    string artist = mediaProperties?.Artist ?? "";
                    if (string.IsNullOrWhiteSpace(artist))
                        artist = mediaProperties?.AlbumArtist ?? "";
                    string album = mediaProperties?.AlbumTitle ?? "";

                    if (string.IsNullOrWhiteSpace(artist))
                        artist = FormatAppId(session.SourceAppUserModelId);

                    byte[]? thumbnail = await ReadThumbnailBytesAsync(mediaProperties?.Thumbnail);

                    // Extract timeline properties & seek capability
                    double positionSeconds = 0;
                    double durationSeconds = 0;
                    bool canSeek = false;

                    try
                    {
                        var timeline = session.GetTimelineProperties();
                        var controls = playbackInfo?.Controls;
                        canSeek = controls?.IsPlaybackPositionEnabled == true && timeline != null && timeline.EndTime > timeline.StartTime;

                        if (timeline != null && timeline.EndTime > timeline.StartTime)
                        {
                            durationSeconds = (timeline.EndTime - timeline.StartTime).TotalSeconds;
                            var pos = timeline.Position - timeline.StartTime;
                            if (isPlaying && timeline.LastUpdatedTime > DateTimeOffset.MinValue)
                            {
                                var elapsed = DateTimeOffset.UtcNow - timeline.LastUpdatedTime;
                                if (elapsed.TotalSeconds > 0 && elapsed.TotalSeconds < 60)
                                {
                                    pos += elapsed;
                                }
                            }
                            positionSeconds = Math.Clamp(pos.TotalSeconds, 0, durationSeconds);
                        }
                    }
                    catch { }

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        return new MediaSnapshot(
                            Title: title,
                            ArtistOrApp: artist,
                            Album: album,
                            IsPlaying: isPlaying,
                            PeakLevel: peak,
                            ThumbnailBytes: thumbnail,
                            SourceAppUserModelId: session.SourceAppUserModelId ?? "",
                            PositionSeconds: positionSeconds,
                            DurationSeconds: durationSeconds,
                            CanSeek: canSeek
                        );
                    }
                }
                catch
                {
                    // Fall back on session querying error
                }
            }
        }

        // Win32 Process Scraper Fallback
        var fallback = FindActivePlayerMetadata(allowBrowserMedia);
        if (!string.IsNullOrWhiteSpace(fallback.Title))
        {
            return new MediaSnapshot(
                Title: fallback.Title,
                ArtistOrApp: fallback.Artist,
                Album: "",
                IsPlaying: peak > 0.005,
                PeakLevel: peak,
                ThumbnailBytes: null,
                SourceAppUserModelId: fallback.PlayerName
            );
        }

        return peak > 0.005
            ? new MediaSnapshot(Locale.Media_NowPlaying_Title, Locale.Media_SystemAudio, "", true, peak, null, "")
            : new MediaSnapshot(Locale.Media_NoTrack, Locale.Media_SystemAudio, "", false, 0.0, null, "");
    }

    private static (string Title, string Artist, string PlayerName) FindActivePlayerMetadata(bool allowBrowserMedia)
    {
        var musicMeta = SearchPlayerMetadata(MusicPlayers);
        if (!string.IsNullOrWhiteSpace(musicMeta.Title)) return musicMeta;

        if (allowBrowserMedia)
        {
            var browserMeta = SearchPlayerMetadata(BrowserPlayers);
            if (!string.IsNullOrWhiteSpace(browserMeta.Title)) return browserMeta;
        }

        return ("", "", "");
    }

    private static (string Title, string Artist, string PlayerName) SearchPlayerMetadata(string[] playerNames)
    {
        foreach (var playerName in playerNames)
        {
            try
            {
                var processes = Process.GetProcessesByName(playerName);
                foreach (var proc in processes)
                {
                    try
                    {
                        var windowTitle = proc.MainWindowTitle;
                        if (string.IsNullOrWhiteSpace(windowTitle)) continue;

                        if (IsIdlePlayerTitle(playerName, windowTitle)) continue;

                        var cleaned = StripSuffixes(windowTitle);
                        var parts = cleaned.Split(" - ", 2, StringSplitOptions.TrimEntries);
                        if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
                        {
                            return (parts[1], parts[0], playerName);
                        }

                        return (cleaned, FormatPlayerName(playerName), playerName);
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }
            }
            catch
            {
                // Ignore process access errors
            }
        }

        return ("", "", "");
    }

    private static bool IsIdlePlayerTitle(string playerName, string title)
    {
        if (playerName.Equals("Spotify", StringComparison.OrdinalIgnoreCase) &&
            (title.Equals("Spotify", StringComparison.OrdinalIgnoreCase) ||
             title.Equals("Spotify Free", StringComparison.OrdinalIgnoreCase) ||
             title.Equals("Spotify Premium", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if ((playerName.Equals("chrome", StringComparison.OrdinalIgnoreCase) ||
             playerName.Equals("msedge", StringComparison.OrdinalIgnoreCase) ||
             playerName.Equals("firefox", StringComparison.OrdinalIgnoreCase) ||
             playerName.Equals("brave", StringComparison.OrdinalIgnoreCase)) &&
            !title.Contains("YouTube", StringComparison.OrdinalIgnoreCase) &&
            !title.Contains("Spotify", StringComparison.OrdinalIgnoreCase) &&
            !title.Contains("SoundCloud", StringComparison.OrdinalIgnoreCase) &&
            !title.Contains("Music", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string StripSuffixes(string title)
    {
        string[] suffixes =
        [
            " - VLC media player",
            " - YouTube - Google Chrome",
            " - YouTube - Microsoft​ Edge",
            " - YouTube - Mozilla Firefox",
            " - Google Chrome",
            " - Microsoft​ Edge",
            " - Mozilla Firefox",
            " - Brave",
            " - MusicBee"
        ];

        foreach (var suffix in suffixes)
        {
            if (title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return title[..^suffix.Length].Trim();
            }
        }

        return title.Trim();
    }

    private static string FormatAppId(string? sourceAppId)
    {
        if (string.IsNullOrWhiteSpace(sourceAppId)) return Locale.Media_SystemAudio;

        string lower = sourceAppId.ToLowerInvariant();
        if (lower.Contains("spotify")) return "Spotify";
        if (lower.Contains("applemusic")) return "Apple Music";
        if (lower.Contains("zunemusic") || lower.Contains("mediaplayer")) return "Media Player";
        if (lower.Contains("tidal")) return "TIDAL";
        if (lower.Contains("deezer")) return "Deezer";
        if (lower.Contains("vlc")) return "VLC";
        if (lower.Contains("musicbee")) return "MusicBee";
        if (lower.Contains("chrome")) return "Chrome";
        if (lower.Contains("msedge") || lower.Contains("edge")) return "Edge";
        if (lower.Contains("firefox")) return "Firefox";
        if (lower.Contains("brave")) return "Brave";

        var name = Path.GetFileNameWithoutExtension(sourceAppId);
        return string.IsNullOrWhiteSpace(name) ? sourceAppId : name;
    }

    private static string FormatPlayerName(string raw) => raw.ToLowerInvariant() switch
    {
        "vlc" => "VLC",
        "msedge" => "Edge",
        "chrome" => "Chrome",
        "firefox" => "Firefox",
        "brave" => "Brave",
        "wmplayer" => "Windows Media Player",
        _ => raw
    };

    public static double GetSystemAudioPeak()
    {
        if (!OperatingSystem.IsWindows()) return 0.0;

        object? enumerator = null;
        IMMDevice? device = null;
        object? meterObj = null;

        try
        {
            var enumeratorType = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
            if (enumeratorType == null) return 0.0;

            enumerator = Activator.CreateInstance(enumeratorType);
            if (enumerator is not IMMDeviceEnumerator devEnum) return 0.0;

            devEnum.GetDefaultAudioEndpoint(0, 1, out device);
            if (device == null) return 0.0;

            var iidMeter = typeof(IAudioMeterInformation).GUID;
            device.Activate(ref iidMeter, 0, IntPtr.Zero, out meterObj);
            if (meterObj is IAudioMeterInformation meter)
            {
                meter.GetPeakValue(out var peak);
                return Math.Clamp(peak, 0f, 1f);
            }
        }
        catch
        {
            // Ignore COM audio query failures
        }
        finally
        {
            if (meterObj != null && Marshal.IsComObject(meterObj))
            {
                try { Marshal.ReleaseComObject(meterObj); } catch { }
            }
            if (device != null && Marshal.IsComObject(device))
            {
                try { Marshal.ReleaseComObject(device); } catch { }
            }
            if (enumerator != null && Marshal.IsComObject(enumerator))
            {
                try { Marshal.ReleaseComObject(enumerator); } catch { }
            }
        }

        return 0.0;
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IntPtr ppDevices);
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice? ppEndpoint);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object? ppInterface);
    }

    [ComImport]
    [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioMeterInformation
    {
        int GetPeakValue(out float pfPeak);
    }
}
