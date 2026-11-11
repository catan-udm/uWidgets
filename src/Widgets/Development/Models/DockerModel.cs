namespace Development.Models;

public record DockerModel(
    bool ShowAllContainers = true,
    int RefreshIntervalSeconds = 5);

public record DockerContainerInfo(
    string Id,
    string Name,
    string Image,
    string State,
    string Status,
    string PortSummary)
{
    public bool IsRunning => string.Equals(State, "running", StringComparison.OrdinalIgnoreCase);
}
