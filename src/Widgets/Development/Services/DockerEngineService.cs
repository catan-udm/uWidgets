using System.IO.Pipes;
using System.Net.Http;
using System.Text.Json;
using Development.Models;

namespace Development.Services;

public static class DockerEngineService
{
    private static readonly Lazy<HttpClient> PipeHttpClient = new(() =>
    {
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var pipe = new NamedPipeClientStream(".", "docker_engine", PipeDirection.InOut, PipeOptions.Asynchronous);
                await pipe.ConnectAsync(1000, cancellationToken);
                return pipe;
            }
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(3)
        };
    });

    public static async Task<(bool IsOnline, List<DockerContainerInfo> Containers)> GetContainersAsync(bool all = true)
    {
        if (!OperatingSystem.IsWindows())
            return (false, []);

        try
        {
            var url = $"http://localhost/v1.43/containers/json?all={(all ? "1" : "0")}";
            using var response = await PipeHttpClient.Value.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (true, []);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var list = new List<DockerContainerInfo>();

            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                var id = elem.TryGetProperty("Id", out var idProp) ? idProp.GetString() ?? "" : "";
                var shortId = id.Length > 12 ? id[..12] : id;

                string name = shortId;
                if (elem.TryGetProperty("Names", out var namesProp) && namesProp.GetArrayLength() > 0)
                {
                    name = namesProp[0].GetString()?.TrimStart('/') ?? shortId;
                }

                var image = elem.TryGetProperty("Image", out var imgProp) ? imgProp.GetString() ?? "" : "";
                var state = elem.TryGetProperty("State", out var stProp) ? stProp.GetString() ?? "" : "";
                var status = elem.TryGetProperty("Status", out var statProp) ? statProp.GetString() ?? "" : "";

                var portList = new List<string>();
                if (elem.TryGetProperty("Ports", out var portsProp))
                {
                    foreach (var p in portsProp.EnumerateArray())
                    {
                        if (p.TryGetProperty("PublicPort", out var pub) && p.TryGetProperty("PrivatePort", out var priv))
                        {
                            portList.Add($":{pub.GetInt32()}->{priv.GetInt32()}");
                        }
                    }
                }
                var portSummary = portList.Count > 0 ? string.Join(", ", portList) : "";

                list.Add(new DockerContainerInfo(
                    Id: id,
                    Name: name,
                    Image: image,
                    State: state,
                    Status: status,
                    PortSummary: portSummary
                ));
            }

            return (true, list);
        }
        catch
        {
            // Named pipe offline or timeout
            return (false, []);
        }
    }

    public static async Task<bool> ExecuteActionAsync(string containerId, string action)
    {
        try
        {
            var url = $"http://localhost/v1.43/containers/{containerId}/{action}";
            using var response = await PipeHttpClient.Value.PostAsync(url, null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
