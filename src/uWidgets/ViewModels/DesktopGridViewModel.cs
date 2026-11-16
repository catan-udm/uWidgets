using System;
using ReactiveUI;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models.Settings;

namespace uWidgets.ViewModels;

public class DesktopGridViewModel : ReactiveObject, IDisposable
{
    private readonly IAppSettingsProvider appSettingsProvider;
    private bool disposed;

    public DesktopGridViewModel(IAppSettingsProvider appSettingsProvider)
    {
        this.appSettingsProvider = appSettingsProvider;
        appSettingsProvider.DataChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object sender, AppSettings? oldData, AppSettings newData)
    {
        this.RaisePropertyChanged(nameof(Size));
        this.RaisePropertyChanged(nameof(Margin));
        this.RaisePropertyChanged(nameof(SnapPosition));
        this.RaisePropertyChanged(nameof(SnapSize));
        this.RaisePropertyChanged(nameof(LockPosition));
        this.RaisePropertyChanged(nameof(LockSize));
        this.RaisePropertyChanged(nameof(MasterLock));
    }
    public int Size
    {
        get => appSettingsProvider.Get().Dimensions.Size;
        set
        {
            var settings = appSettingsProvider.Get();
            var dimensions = settings.Dimensions with { Size = value };
            appSettingsProvider.Save(settings with { Dimensions = dimensions });
            this.RaisePropertyChanged(nameof(Size));
        }
    }

    public int Margin
    {
        get => appSettingsProvider.Get().Dimensions.Margin;
        set
        {
            var settings = appSettingsProvider.Get();
            var dimensions = settings.Dimensions with { Margin = value };
            appSettingsProvider.Save(settings with { Dimensions = dimensions });
            this.RaisePropertyChanged(nameof(Margin));
        }
    }

    public bool SnapPosition
    {
        get => appSettingsProvider.Get().Layout.SnapPosition;
        set
        {
            var settings = appSettingsProvider.Get();
            var newLayout = settings.Layout with { SnapPosition = value };
            appSettingsProvider.Save(settings with { Layout = newLayout });
        }
    }

    public bool SnapSize
    {
        get => appSettingsProvider.Get().Layout.SnapSize;
        set
        {
            var settings = appSettingsProvider.Get();
            var newLayout = settings.Layout with { SnapSize = value };
            appSettingsProvider.Save(settings with { Layout = newLayout });
        }
    }

    public bool LockPosition
    {
        get => appSettingsProvider.Get().Layout.LockPosition;
        set
        {
            var settings = appSettingsProvider.Get();
            var newLayout = settings.Layout with { LockPosition = value };
            appSettingsProvider.Save(settings with { Layout = newLayout });
            this.RaisePropertyChanged(nameof(LockPosition));
            this.RaisePropertyChanged(nameof(MasterLock));
        }
    }

    public bool LockSize
    {
        get => appSettingsProvider.Get().Layout.LockSize;
        set
        {
            var settings = appSettingsProvider.Get();
            var newLayout = settings.Layout with { LockSize = value };
            appSettingsProvider.Save(settings with { Layout = newLayout });
            this.RaisePropertyChanged(nameof(LockSize));
            this.RaisePropertyChanged(nameof(MasterLock));
        }
    }

    public bool MasterLock
    {
        get => LockPosition && LockSize;
        set
        {
            var settings = appSettingsProvider.Get();
            var newLayout = settings.Layout with { LockPosition = value, LockSize = value };
            appSettingsProvider.Save(settings with { Layout = newLayout });
            this.RaisePropertyChanged(nameof(LockPosition));
            this.RaisePropertyChanged(nameof(LockSize));
            this.RaisePropertyChanged(nameof(MasterLock));
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        appSettingsProvider.DataChanged -= OnSettingsChanged;
        GC.SuppressFinalize(this);
    }
}
