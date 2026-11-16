using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
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

        StartCommand = ReactiveCommand.CreateFromTask(() => actionCallback(info.Id, "start"), outputScheduler: RxApp.MainThreadScheduler);
        StopCommand = ReactiveCommand.CreateFromTask(() => actionCallback(info.Id, "stop"), outputScheduler: RxApp.MainThreadScheduler);
        RestartCommand = ReactiveCommand.CreateFromTask(() => actionCallback(info.Id, "restart"), outputScheduler: RxApp.MainThreadScheduler);
    }

    public string Id => info.Id;
    public string Name => info.Name;
    public string Image => info.Image;
    public string State => info.State;
    public string Status => info.Status;
    public string PortSummary => info.PortSummary;
    public bool IsRunning => info.IsRunning;

    public string SummaryTooltip => string.IsNullOrWhiteSpace(PortSummary)
        ? $"{Name}\n{Image}\n{Status}"
        : $"{Name}\n{Image}\n{Status} ({PortSummary})";

    private static readonly IBrush RunningLightBrush = new SolidColorBrush(Color.Parse("#047857"));
    private static readonly IBrush RunningDarkBrush = new SolidColorBrush(Color.Parse("#34D399"));
    private static readonly IBrush StoppedLightBrush = new SolidColorBrush(Color.Parse("#6B7280"));
    private static readonly IBrush StoppedDarkBrush = new SolidColorBrush(Color.Parse("#9CA3AF"));

    public IBrush StatusBrush
    {
        get
        {
            var isDark = Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
            if (IsRunning)
                return isDark ? RunningDarkBrush : RunningLightBrush;
            return isDark ? StoppedDarkBrush : StoppedLightBrush;
        }
    }

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
        this.RaisePropertyChanged(nameof(SummaryTooltip));
        this.RaisePropertyChanged(nameof(StatusBrush));
    }
}

public class DockerViewModel : ReactiveObject, IDisposable
{
    private readonly DockerModel model;
    private readonly int refreshSeconds;
    private DateTimeOffset nextPollUtc = DateTimeOffset.MinValue;
    private int isUpdating;

    public DockerViewModel(DockerModel model)
    {
        this.model = model;
        refreshSeconds = Math.Clamp(model.RefreshIntervalSeconds, 2, 120);
        Containers = [];

        DismissStatusCommand = ReactiveCommand.Create(() => StatusMessage = null);

        OnTimerTick();
        TimerService.Timer1Second.Subscribe(OnTimerTick);
    }

    public ObservableCollection<DockerContainerItemViewModel> Containers { get; }
    public ICommand DismissStatusCommand { get; }

    private bool isDockerOnline = true;
    public bool IsDockerOnline
    {
        get => isDockerOnline;
        private set
        {
            this.RaiseAndSetIfChanged(ref isDockerOnline, value);
            this.RaisePropertyChanged(nameof(IsEmptyState));
        }
    }

    public bool HasContainers => Containers.Count > 0;
    public bool IsEmptyState => IsDockerOnline && Containers.Count == 0;

    private bool isBusy;
    public bool IsBusy
    {
        get => isBusy;
        private set => this.RaiseAndSetIfChanged(ref isBusy, value);
    }

    private string? statusMessage;
    public string? StatusMessage
    {
        get => statusMessage;
        private set => this.RaiseAndSetIfChanged(ref statusMessage, value);
    }

    private string headerSummary = "Docker";
    public string HeaderSummary
    {
        get => headerSummary;
        private set => this.RaiseAndSetIfChanged(ref headerSummary, value);
    }

    private void OnTimerTick()
    {
        var now = DateTimeOffset.UtcNow;
        if (now < nextPollUtc) return;
        nextPollUtc = now.AddSeconds(refreshSeconds);
        _ = UpdateAsync();
    }

    private async Task UpdateAsync()
    {
        if (Interlocked.CompareExchange(ref isUpdating, 1, 0) != 0)
            return;

        try
        {
            var (online, list) = await DockerEngineService.GetContainersAsync(model.ShowAllContainers);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsDockerOnline = online;

                if (!online)
                {
                    HeaderSummary = "Docker Offline";
                    Containers.Clear();
                    this.RaisePropertyChanged(nameof(HasContainers));
                    this.RaisePropertyChanged(nameof(IsEmptyState));
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

                this.RaisePropertyChanged(nameof(HasContainers));
                this.RaisePropertyChanged(nameof(IsEmptyState));
            });
        }
        catch (Exception)
        {
        }
        finally
        {
            Volatile.Write(ref isUpdating, 0);
        }
    }

    private async Task HandleContainerActionAsync(string id, string action)
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var ok = await DockerEngineService.ExecuteActionAsync(id, action);
            if (!ok)
            {
                StatusMessage = $"Docker failed to {action} container.";
            }
            await Task.Delay(500);
            nextPollUtc = DateTimeOffset.MinValue;
            await UpdateAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Dispose()
    {
        TimerService.Timer1Second.Unsubscribe(OnTimerTick);
        GC.SuppressFinalize(this);
    }
}
