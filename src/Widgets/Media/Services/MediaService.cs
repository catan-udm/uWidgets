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
    private static DateTime lastSessionManagerAttempt = DateTime.MinValue;

    // Track-key and thumbnail cache for R1
    private static string? cachedTrackKey;
    private static byte[]? cachedThumbnailBytes;

    // Paused session and snapshot persistence for R3
    private static string? lastActiveSessionAppId;
    private static MediaSnapshot? lastValidSnapshot;
    private static DateTime lastValidSnapshotTime = DateTime.MinValue;

    private static void HandleComException(COMException? ex)
    {
        sessionManager = null;
        lastSessionManagerAttempt = DateTime.MinValue;
    }

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
        if (!OperatingSystem.IsWindows()) return null;

        if ((DateTime.UtcNow - lastSessionManagerAttempt).TotalSeconds < 3)
            return null;

        lastSessionManagerAttempt = DateTime.UtcNow;

        try
        {
            sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            return sessionManager;
        }
        catch (COMException ex)
        {
            HandleComException(ex);
            return null;
        }
        catch
        {
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
        catch (COMException ex)
        {
            HandleComException(ex);
            return null;
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
            // 1A. Any non-browser player actively PLAYING
            var playingMusic = musicSessions.FirstOrDefault(s =>
            {
                try { return s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing; }
                catch (COMException ex) { HandleComException(ex); return false; }
                catch { return false; }
            });
            if (playingMusic != null) return playingMusic;

            // 1B. Any browser actively PLAYING (if browser media is allowed)
            if (allowBrowserMedia)
            {
                var playingBrowser = browserSessions.FirstOrDefault(s =>
                {
                    try { return s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing; }
                    catch (COMException ex) { HandleComException(ex); return false; }
                    catch { return false; }
                });
                if (playingBrowser != null) return playingBrowser;
            }

            // R3: If no session is currently playing, prioritize the previously active session (even when Paused)
            // over arbitrary idle background players or browsers.
            if (!string.IsNullOrEmpty(lastActiveSessionAppId))
            {
                var lastActive = sessions.FirstOrDefault(s =>
                    string.Equals(s.SourceAppUserModelId, lastActiveSessionAppId, StringComparison.OrdinalIgnoreCase));
                if (lastActive != null && (!IsBrowserApp(lastActive.SourceAppUserModelId) || allowBrowserMedia))
                {
                    try
                    {
                        var status = lastActive.GetPlaybackInfo()?.PlaybackStatus;
                        if (status != null && status != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed && status != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped)
                        {
                            return lastActive;
                        }
                    }
                    catch (COMException ex) { HandleComException(ex); }
                    catch { }
                }
            }

            // 1C. Any non-browser player PAUSED
            var pausedMusic = musicSessions.FirstOrDefault(s =>
            {
                try { return s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused; }
                catch (COMException ex) { HandleComException(ex); return false; }
                catch { return false; }
            });
            if (pausedMusic != null) return pausedMusic;

            // 1D. Any browser PAUSED
            if (allowBrowserMedia)
            {
                var pausedBrowser = browserSessions.FirstOrDefault(s =>
                {
                    try { return s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused; }
                    catch (COMException ex) { HandleComException(ex); return false; }
                    catch { return false; }
                });
                if (pausedBrowser != null) return pausedBrowser;
            }

            // 1E. Non-browser session that is not stopped or closed
            var activeMusic = musicSessions.FirstOrDefault(s =>
            {
                try
                {
                    var status = s.GetPlaybackInfo()?.PlaybackStatus;
                    return status != null && status != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed && status != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped;
                }
                catch (COMException ex) { HandleComException(ex); return false; }
                catch { return false; }
            });
            if (activeMusic != null) return activeMusic;

            if (allowBrowserMedia && browserSessions.Count > 0)
                return browserSessions[0];

            return musicSessions.FirstOrDefault();
        }

        // MODE 2: PrefersMusicPlayer = FALSE (Active Playback Follower)
        // Follows whatever is actively playing across all sources
        GlobalSystemMediaTransportControlsSession? currentSession = null;
        try
        {
            currentSession = manager.GetCurrentSession();
        }
        catch (COMException ex) { HandleComException(ex); }
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
                catch (COMException ex) { HandleComException(ex); }
                catch { }
            }
        }

        var anyPlaying = sessions.FirstOrDefault(s =>
        {
            bool isBrowser = IsBrowserApp(s.SourceAppUserModelId);
            if (isBrowser && !allowBrowserMedia) return false;
            try { return s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing; }
            catch (COMException ex) { HandleComException(ex); return false; }
            catch { return false; }
        });
        if (anyPlaying != null) return anyPlaying;

        // R3: If no session is playing, prioritize the previously active session (even when Paused)
        if (!string.IsNullOrEmpty(lastActiveSessionAppId))
        {
            var lastActive = sessions.FirstOrDefault(s =>
                string.Equals(s.SourceAppUserModelId, lastActiveSessionAppId, StringComparison.OrdinalIgnoreCase));
            if (lastActive != null && (!IsBrowserApp(lastActive.SourceAppUserModelId) || allowBrowserMedia))
            {
                try
                {
                    var status = lastActive.GetPlaybackInfo()?.PlaybackStatus;
                    if (status != null && status != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed && status != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped)
                    {
                        return lastActive;
                    }
                }
                catch (COMException ex) { HandleComException(ex); }
                catch { }
            }
        }

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

            using var netStream = stream.AsStreamForRead();
            using var ms = new MemoryStream();
            await netStream.CopyToAsync(ms);
            return ms.ToArray();
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
                catch (COMException ex) { HandleComException(ex); }
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
                catch (COMException ex) { HandleComException(ex); }
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
                catch (COMException ex) { HandleComException(ex); }
                catch { }
            }
        }

        SendMediaKey(VK_MEDIA_PREV_TRACK);
    }

    public static void ToggleMute(string? appOrProcess = null) => ToggleAppMute(appOrProcess);

    public static bool ToggleAppMute(string? appOrProcess)
    {
        if (!OperatingSystem.IsWindows()) return false;

        object? enumerator = null;
        IMMDevice? device = null;
        object? managerObj = null;
        IAudioSessionEnumerator? sessionEnum = null;

        try
        {
            var enumeratorType = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
            if (enumeratorType == null) return false;

            enumerator = Activator.CreateInstance(enumeratorType);
            if (enumerator is not IMMDeviceEnumerator devEnum) return false;

            devEnum.GetDefaultAudioEndpoint(0, 1, out device);
            if (device == null) return false;

            var iidManager = typeof(IAudioSessionManager2).GUID;
            device.Activate(ref iidManager, 0, IntPtr.Zero, out managerObj);
            if (managerObj is not IAudioSessionManager2 sessionManager) return false;

            if (sessionManager.GetSessionEnumerator(out sessionEnum) != 0 || sessionEnum == null) return false;
            sessionEnum.GetCount(out int count);

            string targetName = NormalizeProcessTarget(appOrProcess);
            bool toggled = false;
            bool newMuteState = false;

            for (int i = 0; i < count; i++)
            {
                if (sessionEnum.GetSession(i, out var ctl) == 0 && ctl != null)
                {
                    try
                    {
                        if (ctl is IAudioSessionControl2 ctl2)
                        {
                            ctl2.GetProcessId(out uint pid);
                            if (pid > 0)
                            {
                                string procName = "";
                                try { procName = Process.GetProcessById((int)pid).ProcessName; } catch { }

                                bool isMatch = false;
                                if (!string.IsNullOrWhiteSpace(targetName))
                                {
                                    isMatch = procName.Contains(targetName, StringComparison.OrdinalIgnoreCase) ||
                                              targetName.Contains(procName, StringComparison.OrdinalIgnoreCase);
                                }

                                if (isMatch && ctl is ISimpleAudioVolume vol)
                                {
                                    vol.GetMute(out bool currentMute);
                                    var g = Guid.Empty;
                                    vol.SetMute(!currentMute, ref g);
                                    newMuteState = !currentMute;
                                    toggled = true;
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(ctl);
                    }
                }
            }

            if (!toggled)
            {
                for (int i = 0; i < count; i++)
                {
                    if (sessionEnum.GetSession(i, out var ctl) == 0 && ctl != null)
                    {
                        try
                        {
                            ctl.GetState(out int state);
                            if (state == 1 && ctl is ISimpleAudioVolume vol)
                            {
                                vol.GetMute(out bool currentMute);
                                var g = Guid.Empty;
                                vol.SetMute(!currentMute, ref g);
                                newMuteState = !currentMute;
                                toggled = true;
                                break;
                            }
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(ctl);
                        }
                    }
                }
            }

            return newMuteState;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (sessionEnum != null && Marshal.IsComObject(sessionEnum))
            {
                try { Marshal.ReleaseComObject(sessionEnum); } catch { }
            }
            if (managerObj != null && Marshal.IsComObject(managerObj))
            {
                try { Marshal.ReleaseComObject(managerObj); } catch { }
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
    }

    public static bool IsAppMuted(string? appOrProcess)
    {
        if (!OperatingSystem.IsWindows()) return false;

        object? enumerator = null;
        IMMDevice? device = null;
        object? managerObj = null;
        IAudioSessionEnumerator? sessionEnum = null;

        try
        {
            var enumeratorType = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
            if (enumeratorType == null) return false;

            enumerator = Activator.CreateInstance(enumeratorType);
            if (enumerator is not IMMDeviceEnumerator devEnum) return false;

            devEnum.GetDefaultAudioEndpoint(0, 1, out device);
            if (device == null) return false;

            var iidManager = typeof(IAudioSessionManager2).GUID;
            device.Activate(ref iidManager, 0, IntPtr.Zero, out managerObj);
            if (managerObj is not IAudioSessionManager2 sessionManager) return false;

            if (sessionManager.GetSessionEnumerator(out sessionEnum) != 0 || sessionEnum == null) return false;
            sessionEnum.GetCount(out int count);

            string targetName = NormalizeProcessTarget(appOrProcess);

            for (int i = 0; i < count; i++)
            {
                if (sessionEnum.GetSession(i, out var ctl) == 0 && ctl != null)
                {
                    try
                    {
                        if (ctl is IAudioSessionControl2 ctl2)
                        {
                            ctl2.GetProcessId(out uint pid);
                            if (pid > 0)
                            {
                                string procName = "";
                                try { procName = Process.GetProcessById((int)pid).ProcessName; } catch { }

                                bool isMatch = false;
                                if (!string.IsNullOrWhiteSpace(targetName))
                                {
                                    isMatch = procName.Contains(targetName, StringComparison.OrdinalIgnoreCase) ||
                                              targetName.Contains(procName, StringComparison.OrdinalIgnoreCase);
                                }

                                if (isMatch && ctl is ISimpleAudioVolume vol)
                                {
                                    vol.GetMute(out bool isMuted);
                                    if (isMuted) return true;
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(ctl);
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (sessionEnum != null && Marshal.IsComObject(sessionEnum))
            {
                try { Marshal.ReleaseComObject(sessionEnum); } catch { }
            }
            if (managerObj != null && Marshal.IsComObject(managerObj))
            {
                try { Marshal.ReleaseComObject(managerObj); } catch { }
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
    }

    private static string NormalizeProcessTarget(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        string lower = raw.ToLowerInvariant();
        if (lower.Contains("spotify")) return "spotify";
        if (lower.Contains("chrome")) return "chrome";
        if (lower.Contains("edge")) return "msedge";
        if (lower.Contains("firefox")) return "firefox";
        if (lower.Contains("brave")) return "brave";
        if (lower.Contains("vlc")) return "vlc";
        if (lower.Contains("applemusic")) return "applemusic";
        if (lower.Contains("musicbee")) return "musicbee";
        if (lower.Contains("wmplayer")) return "wmplayer";

        var name = Path.GetFileNameWithoutExtension(raw);
        return string.IsNullOrWhiteSpace(name) ? raw : name;
    }

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
                catch (COMException ex)
                {
                    HandleComException(ex);
                    return false;
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

    private static bool ShouldRetainLastSnapshot(bool allowBrowserMedia)
    {
        if (lastValidSnapshot == null) return false;

        // If within 5 seconds of last valid snapshot, always retain to ride out transient glitches or pause transitions
        if ((DateTime.UtcNow - lastValidSnapshotTime).TotalSeconds < 5)
            return true;

        // Check if the session still exists in GSMTC
        if (sessionManager != null)
        {
            try
            {
                var sessions = sessionManager.GetSessions();
                if (sessions != null && sessions.Any(s =>
                    string.Equals(s.SourceAppUserModelId, lastActiveSessionAppId, StringComparison.OrdinalIgnoreCase) &&
                    (!IsBrowserApp(s.SourceAppUserModelId) || allowBrowserMedia)))
                {
                    return true;
                }
            }
            catch (COMException ex)
            {
                HandleComException(ex);
            }
            catch { }
        }

        // Check if the player process is still running
        if (!string.IsNullOrWhiteSpace(lastActiveSessionAppId))
        {
            try
            {
                string procTarget = NormalizeProcessTarget(lastActiveSessionAppId);
                if (!string.IsNullOrWhiteSpace(procTarget))
                {
                    var procs = Process.GetProcessesByName(procTarget);
                    try
                    {
                        if (procs.Length > 0)
                        {
                            return true;
                        }
                    }
                    finally
                    {
                        foreach (var p in procs)
                        {
                            try { p.Dispose(); } catch { }
                        }
                    }
                }
            }
            catch { }
        }

        return false;
    }

    public static async Task<MediaSnapshot> GetSnapshotAsync(bool prefersMusicPlayer = true, bool allowBrowserMedia = true)
    {
        var peak = GetSystemAudioPeak();

        GlobalSystemMediaTransportControlsSessionManager? manager = null;
        try
        {
            manager = await GetSessionManagerAsync();
        }
        catch (COMException ex)
        {
            HandleComException(ex);
        }
        catch { }

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

                    // Track-key caching for thumbnails (R1)
                    string trackKey = $"{session.SourceAppUserModelId}|{title}|{artist}|{album}";
                    byte[]? thumbnail = null;

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        if (string.Equals(trackKey, cachedTrackKey, StringComparison.Ordinal) && cachedThumbnailBytes != null)
                        {
                            // Cache hit: avoid expensive 1-second IPC stream reading
                            thumbnail = cachedThumbnailBytes;
                        }
                        else
                        {
                            // Track changed or previous thumbnail was null: read from stream
                            thumbnail = await ReadThumbnailBytesAsync(mediaProperties?.Thumbnail);
                            if (thumbnail != null)
                            {
                                cachedThumbnailBytes = thumbnail;
                                cachedTrackKey = trackKey;
                            }
                            else if (!string.Equals(trackKey, cachedTrackKey, StringComparison.Ordinal))
                            {
                                cachedTrackKey = trackKey;
                                cachedThumbnailBytes = null;
                            }
                        }
                    }

                    // Extract timeline properties & seek capability (R2)
                    double positionSeconds = 0;
                    double durationSeconds = 0;
                    bool canSeek = false;

                    try
                    {
                        var timeline = session.GetTimelineProperties();
                        var controls = playbackInfo?.Controls;
                        canSeek = (controls?.IsPlaybackPositionEnabled == true || (timeline != null && (timeline.EndTime - timeline.StartTime).TotalSeconds > 1))
                            && timeline != null && timeline.EndTime > timeline.StartTime;

                        if (timeline != null && timeline.EndTime > timeline.StartTime)
                        {
                            durationSeconds = (timeline.EndTime - timeline.StartTime).TotalSeconds;
                            var pos = timeline.Position - timeline.StartTime;
                            if (isPlaying && timeline.LastUpdatedTime > DateTimeOffset.MinValue)
                            {
                                var elapsed = DateTimeOffset.UtcNow - timeline.LastUpdatedTime;
                                // Removed the arbitrary && elapsed.TotalSeconds < 60 cap (R2)
                                if (elapsed.TotalSeconds > 0)
                                {
                                    pos += elapsed;
                                }
                            }
                            positionSeconds = Math.Clamp(pos.TotalSeconds, 0, durationSeconds);
                        }
                    }
                    catch (COMException ex)
                    {
                        HandleComException(ex);
                    }
                    catch { }

                    // If paused or timeline temporarily unavailable, retain duration/position from last valid snapshot
                    if (durationSeconds <= 0 && lastValidSnapshot != null && lastValidSnapshot.DurationSeconds > 0 &&
                        string.Equals(session.SourceAppUserModelId, lastValidSnapshot.SourceAppUserModelId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(title, lastValidSnapshot.Title, StringComparison.Ordinal))
                    {
                        durationSeconds = lastValidSnapshot.DurationSeconds;
                        positionSeconds = lastValidSnapshot.PositionSeconds;
                        canSeek = lastValidSnapshot.CanSeek;
                    }

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        var snapshot = new MediaSnapshot(
                            Title: title,
                            ArtistOrApp: artist,
                            Album: album,
                            IsPlaying: isPlaying,
                            PeakLevel: peak,
                            ThumbnailBytes: thumbnail ?? (string.Equals(trackKey, cachedTrackKey, StringComparison.Ordinal) ? cachedThumbnailBytes : null),
                            SourceAppUserModelId: session.SourceAppUserModelId ?? "",
                            PositionSeconds: positionSeconds,
                            DurationSeconds: durationSeconds,
                            CanSeek: canSeek
                        );

                        lastValidSnapshot = snapshot;
                        lastValidSnapshotTime = DateTime.UtcNow;
                        lastActiveSessionAppId = session.SourceAppUserModelId;

                        return snapshot;
                    }
                }
                catch (COMException ex)
                {
                    HandleComException(ex);
                }
                catch
                {
                    // Fall back on session querying error
                }
            }
        }

        // R3: If paused or session temporarily unavailable, retain last valid snapshot
        if (ShouldRetainLastSnapshot(allowBrowserMedia) && lastValidSnapshot != null)
        {
            return lastValidSnapshot with
            {
                IsPlaying = false,
                PeakLevel = peak
            };
        }

        // Win32 Process Scraper Fallback
        var fallback = FindActivePlayerMetadata(allowBrowserMedia);
        if (!string.IsNullOrWhiteSpace(fallback.Title))
        {
            var fallbackSnapshot = new MediaSnapshot(
                Title: fallback.Title,
                ArtistOrApp: fallback.Artist,
                Album: "",
                IsPlaying: peak > 0.005,
                PeakLevel: peak,
                ThumbnailBytes: null,
                SourceAppUserModelId: fallback.PlayerName
            );

            lastValidSnapshot = fallbackSnapshot;
            lastValidSnapshotTime = DateTime.UtcNow;
            lastActiveSessionAppId = fallback.PlayerName;

            return fallbackSnapshot;
        }

        return peak > 0.005
            ? new MediaSnapshot(Locale.Media_NowPlaying_Title, Locale.Media_SystemAudio, "", true, peak, null, "")
            : new MediaSnapshot(Locale.Media_NowPlaying_Title, Locale.Media_SystemAudio, "", false, 0.0, null, "");
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

    [ComImport]
    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionManager2
    {
        int GetAudioSessionControl(IntPtr sessionId, int streamFlags, out IntPtr sessionControl);
        int GetSimpleAudioVolume(IntPtr sessionId, int streamFlags, out IntPtr audioVolume);
        int GetSessionEnumerator(out IAudioSessionEnumerator sessionEnum);
        int RegisterSessionNotification(IntPtr client);
        int UnregisterSessionNotification(IntPtr client);
        int RegisterDuckNotification(IntPtr sessionID, IntPtr duckNotification);
        int UnregisterDuckNotification(IntPtr duckNotification);
    }

    [ComImport]
    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277dee8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionEnumerator
    {
        int GetCount(out int sessionCount);
        int GetSession(int sessionIndex, out IAudioSessionControl session);
    }

    [ComImport]
    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl
    {
        int GetState(out int state);
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string name);
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, ref Guid eventContext);
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string path);
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string path, ref Guid eventContext);
        int GetGroupingParam(out Guid groupingParam);
        int SetGroupingParam(ref Guid groupingParam, ref Guid eventContext);
        int RegisterAudioSessionNotification(IntPtr client);
        int UnregisterAudioSessionNotification(IntPtr client);
    }

    [ComImport]
    [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl2 : IAudioSessionControl
    {
        #region IAudioSessionControl methods
        new int GetState(out int state);
        new int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string name);
        new int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, ref Guid eventContext);
        new int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string path);
        new int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string path, ref Guid eventContext);
        new int GetGroupingParam(out Guid groupingParam);
        new int SetGroupingParam(ref Guid groupingParam, ref Guid eventContext);
        new int RegisterAudioSessionNotification(IntPtr client);
        new int UnregisterAudioSessionNotification(IntPtr client);
        #endregion

        int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        int GetProcessId(out uint pRetVal);
        int IsSystemSoundsSession();
        int SetDuckingPreference(bool optOut);
    }

    [ComImport]
    [Guid("87CE5498-686D-48E5-A449-04859073800F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISimpleAudioVolume
    {
        int SetMasterVolume(float fLevel, ref Guid eventContext);
        int GetMasterVolume(out float pfLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid eventContext);
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
    }
}
