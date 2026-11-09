using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Media.Locales;

namespace Media.Services;

public record MediaSnapshot(
    string Title,
    string ArtistOrApp,
    bool IsPlaying,
    double PeakLevel
);

public static class MediaService
{
    private const byte VK_VOLUME_MUTE = 0xAD;
    private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
    private const byte VK_MEDIA_PREV_TRACK = 0xB1;
    private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static readonly string[] KnownPlayers =
    [
        "Spotify",
        "MusicBee",
        "vlc",
        "foobar2000",
        "AIMP",
        "AppleMusic",
        "iTunes",
        "wmplayer",
        "msedge",
        "chrome",
        "firefox",
        "brave"
    ];

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public static void PlayPause() => SendMediaKey(VK_MEDIA_PLAY_PAUSE);
    public static void NextTrack() => SendMediaKey(VK_MEDIA_NEXT_TRACK);
    public static void PreviousTrack() => SendMediaKey(VK_MEDIA_PREV_TRACK);
    public static void ToggleMute() => SendMediaKey(VK_VOLUME_MUTE);

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

    public static MediaSnapshot GetSnapshot()
    {
        var peak = GetSystemAudioPeak();
        var isPlaying = peak > 0.005;

        var (title, artist) = FindActivePlayerMetadata();
        if (!string.IsNullOrWhiteSpace(title))
        {
            return new MediaSnapshot(title, artist, isPlaying, peak);
        }

        return isPlaying
            ? new MediaSnapshot(Locale.Media_NowPlaying_Title, Locale.Media_SystemAudio, true, peak)
            : new MediaSnapshot(Locale.Media_NoTrack, Locale.Media_SystemAudio, false, 0.0);
    }

    private static (string Title, string Artist) FindActivePlayerMetadata()
    {
        foreach (var playerName in KnownPlayers)
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
                            return (parts[1], parts[0]);
                        }

                        return (cleaned, FormatPlayerName(playerName));
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

        return ("", "");
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

    private static double GetSystemAudioPeak()
    {
        if (!OperatingSystem.IsWindows()) return 0.0;

        try
        {
            var enumeratorType = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
            if (enumeratorType == null) return 0.0;

            var enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(enumeratorType)!;
            enumerator.GetDefaultAudioEndpoint(0, 1, out var device);
            if (device == null) return 0.0;

            var iidMeter = typeof(IAudioMeterInformation).GUID;
            device.Activate(ref iidMeter, 0, IntPtr.Zero, out var meterObj);
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
