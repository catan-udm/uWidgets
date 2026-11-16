using Clock.Models;
using ReactiveUI;

namespace Clock.ViewModels;

public class WorldClockViewModel : ReactiveObject, IDisposable
{
    private readonly List<AnalogClockViewModel> viewModels;

    public WorldClockViewModel(WorldClockModel worldClockModel)
    {
        viewModels = Enumerable
            .Range(0, 4)
            .Select(i =>
                new AnalogClockViewModel(new ClockModel(
                    false,
                    false,
                    false,
                    worldClockModel.TimeZoneIds.ElementAtOrDefault(i))))
            .ToList();
    }

    public AnalogClockViewModel First => viewModels[0];
    public AnalogClockViewModel Second => viewModels[1];
    public AnalogClockViewModel Third => viewModels[2];
    public AnalogClockViewModel Fourth => viewModels[3];

    public void Dispose()
    {
        foreach (var vm in viewModels)
            vm.Dispose();
        GC.SuppressFinalize(this);
    }
}