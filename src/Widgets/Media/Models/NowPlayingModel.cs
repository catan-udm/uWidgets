namespace Media.Models;

public record NowPlayingModel(
    bool ShowTimeline = true,
    bool ShowMuteButton = true,
    bool PrefersMusicPlayer = true,
    bool AllowBrowserMedia = true,
    bool ShowLyrics = false
)
{
    public bool ShowVisualizer => ShowTimeline;
}
