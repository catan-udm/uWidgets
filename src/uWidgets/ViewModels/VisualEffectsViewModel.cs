using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace uWidgets.ViewModels;

public class VisualEffectsViewModel(IAppSettingsProvider appSettingsProvider) : ReactiveObject
{
    public bool LiquidGlass
    {
        get => appSettingsProvider.Get().Theme.LiquidGlass;
        set
        {
            var settings = appSettingsProvider.Get();
            var newTheme = settings.Theme with
            {
                LiquidGlass = value,
                UseNativeFrame = value ? false : settings.Theme.UseNativeFrame
            };
            appSettingsProvider.Save(settings with { Theme = newTheme });
            this.RaisePropertyChanged(nameof(LiquidGlass));
        }
    }

    public double RefThickness
    {
        get => appSettingsProvider.Get().Theme.RefThickness;
        set
        {
            var settings = appSettingsProvider.Get();
            appSettingsProvider.Save(settings with { Theme = settings.Theme with { RefThickness = value } });
            this.RaisePropertyChanged(nameof(RefThickness));
        }
    }

    public double RefFactor
    {
        get => appSettingsProvider.Get().Theme.RefFactor;
        set
        {
            var settings = appSettingsProvider.Get();
            appSettingsProvider.Save(settings with { Theme = settings.Theme with { RefFactor = value } });
            this.RaisePropertyChanged(nameof(RefFactor));
        }
    }

    public double RefDispersion
    {
        get => appSettingsProvider.Get().Theme.RefDispersion;
        set
        {
            var settings = appSettingsProvider.Get();
            appSettingsProvider.Save(settings with { Theme = settings.Theme with { RefDispersion = value } });
            this.RaisePropertyChanged(nameof(RefDispersion));
        }
    }

    public double ShapeRadius
    {
        get => appSettingsProvider.Get().Theme.ShapeRadius;
        set
        {
            var settings = appSettingsProvider.Get();
            appSettingsProvider.Save(settings with { Theme = settings.Theme with { ShapeRadius = value } });
            this.RaisePropertyChanged(nameof(ShapeRadius));
        }
    }

    public double ShapeRoundness
    {
        get => appSettingsProvider.Get().Theme.ShapeRoundness;
        set
        {
            var settings = appSettingsProvider.Get();
            appSettingsProvider.Save(settings with { Theme = settings.Theme with { ShapeRoundness = value } });
            this.RaisePropertyChanged(nameof(ShapeRoundness));
        }
    }

    public void ApplyPreset(double thickness, double factor, double dispersion, double radius = 0.85, double roundness = 4.0)
    {
        var settings = appSettingsProvider.Get();
        var updatedTheme = settings.Theme with
        {
            LiquidGlass = true,
            UseNativeFrame = false,
            RefThickness = thickness,
            RefFactor = factor,
            RefDispersion = dispersion,
            ShapeRadius = radius,
            ShapeRoundness = roundness
        };
        appSettingsProvider.Save(settings with { Theme = updatedTheme });
        NotifyAll();
    }

    private void NotifyAll()
    {
        this.RaisePropertyChanged(nameof(LiquidGlass));
        this.RaisePropertyChanged(nameof(RefThickness));
        this.RaisePropertyChanged(nameof(RefFactor));
        this.RaisePropertyChanged(nameof(RefDispersion));
        this.RaisePropertyChanged(nameof(ShapeRadius));
        this.RaisePropertyChanged(nameof(ShapeRoundness));
    }
}
