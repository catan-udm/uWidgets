using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media;
using Development.Models;
using Development.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Development.ViewModels;

public class DockerContainerItemViewModel : ReactiveObject
{
    private DockerContainerInfo info;

    public DockerContainerItemViewModel(DockerContainerInfo info, Func<string, string, Task> actionCallback)
    {
        this.info = info;

        StartCommand = ReactiveCommand.CreateFromTask(() => actionCallback(info.Id, "start"));
        StopCommand = ReactiveCommand.CreateFromTask(() => actionCallback(info.Id, "stop"));
        RestartCommand = ReactiveCommand.CreateFromTask(() => actionCallback(info.Id, "restart"));
    }

    public string Id => info.Id;
    public string Name => info.Name;
    public string Image => info.Image;
    public string State => info.State;
    public string Status => info.Status;
    public string PortSummary => info.PortSummary;
    public bool IsRunning => info.IsRunning;

    public IBrush StatusBrush => IsRunning
        ? new SolidColorBrush(Color.Parse("#10B981"))
        : new SolidColorBrush(Color.Parse("#9CA3AF"));

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand RestartCommand { get; }

    public void Update(DockerContainerInfo newInfo)
    {
        info = newInfo;
        this.RaisePropertyChanged(nameof(Name));
        this.RaisePropertyChanged(nameof(Image));
        this.RaisePropertyChanged(nameof(State));
        this.RaisePropertyChanged(nameof(Status));
        this.RaisePropertyChanged(nameof(PortSummary));
        this.RaisePropertyChanged(nameof(IsRunning));
        this.RaisePropertyChanged(nameof(StatusBrush));
    }
}

public class DockerViewModel : ReactiveObject, IDisposable
{
    private readonly DockerModel model;

    public DockerViewModel(DockerModel model)
    {
        this.model = model;
        Containers = [];

        Update();
        TimerService.Timer5Seconds.Subscribe(Update);
    }

    public ObservableCollection<DockerContainerItemViewModel> Containers { get; }

    private bool isDockerOnline = true;
    public bool IsDockerOnline
    {
        get => isDockerOnline;
        private set => this.RaiseAndSetIfChanged(ref isDockerOnline, value);
    }

    private string headerSummary = "Docker";
    public string HeaderSummary
    {
        get => headerSummary;
        private set => this.RaiseAndSetIfChanged(ref headerSummary, value);
    }

    private void Update() => _ = UpdateAsync();

    private async Task UpdateAsync()
    {
        var (online, list) = await DockerEngineService.GetContainersAsync(model.ShowAllContainers);
        IsDockerOnline = online;

        if (!online)
        {
            HeaderSummary = "Docker Offline";
            Containers.Clear();
            return;
        }

        int running = list.Count(c => c.IsRunning);
        HeaderSummary = $"{running}/{list.Count} Running";

        // Remove old
        var toRemove = Containers.Where(c => list.All(l => l.Id != c.Id)).ToList();
        foreach (var rem in toRemove) Containers.Remove(rem);

        // Add or update
        foreach (var item in list)
        {
            var existing = Containers.FirstOrDefault(c => c.Id == item.Id);
            if (existing != null)
            {
                existing.Update(item);
            }
            else
            {
                Containers.Add(new DockerContainerItemViewModel(item, HandleContainerActionAsync));
            }
        }
    }

    private async Task HandleContainerActionAsync(string id, string action)
    {
        await DockerEngineService.ExecuteActionAsync(id, action);
        await Task.Delay(500);
        await UpdateAsync();
    }

    public void Dispose()
    {
        TimerService.Timer5Seconds.Unsubscribe(Update);
        GC.SuppressFinalize(this);
    }
}
