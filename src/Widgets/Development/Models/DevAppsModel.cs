using Avalonia.Media;
using Development.ViewModels;

namespace Development.Models;

public record DevAppsModel(
    int RefreshIntervalSeconds = 3);

public record DevAppInstanceInfo(
    int ProcessId,
    string AppName,
    string Framework,
    int Port,
    long MemoryWorkingSetMb,
    string Url)
{
    public StreamGeometry Icon => Framework switch
    {
        "Next.js" => DevelopmentIcon.NextGeometry,
        "Vite" => DevelopmentIcon.ViteGeometry,
        _ => DevelopmentIcon.NodeGeometry
    };
}
