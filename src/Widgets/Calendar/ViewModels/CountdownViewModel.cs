using System;
using Calendar.Locales;
using Calendar.Models;
using ReactiveUI;
using uWidgets.Services;

namespace Calendar.ViewModels;

public class CountdownViewModel : ReactiveObject, IDisposable
{
    private readonly CountdownModel model;
    private int daysValue;
    private string statusLabel = "";
    private string detailText = "";
    private double progressPercentage;

    public CountdownViewModel(CountdownModel model)
    {
        this.model = model;
        Update();
        TimerService.Timer1Minute.Subscribe(Update);
    }

    public string Title => string.IsNullOrWhiteSpace(model.Title)
        ? Locale.Calendar_Countdown_DefaultEvent
        : model.Title;

    public string TargetDateText => model.TargetDate.ToString("MMM d, yyyy");

    public bool ShowHours => model.ShowHours;

    public int DaysValue
    {
        get => daysValue;
        private set => this.RaiseAndSetIfChanged(ref daysValue, value);
    }

    public string StatusLabel
    {
        get => statusLabel;
        private set => this.RaiseAndSetIfChanged(ref statusLabel, value);
    }

    public string DetailText
    {
        get => detailText;
        private set => this.RaiseAndSetIfChanged(ref detailText, value);
    }

    public double ProgressPercentage
    {
        get => progressPercentage;
        private set => this.RaiseAndSetIfChanged(ref progressPercentage, value);
    }

    private void Update()
    {
        var now = DateTime.Now;
        var diff = model.TargetDate - now;

        if (diff >= TimeSpan.Zero)
        {
            DaysValue = (int)Math.Ceiling(diff.TotalDays);
            StatusLabel = Locale.Calendar_Countdown_DaysLeft;
            DetailText = model.ShowHours
                ? $"{(int)diff.TotalDays}d {diff.Hours}h {diff.Minutes}m"
                : TargetDateText;
        }
        else
        {
            var elapsed = now - model.TargetDate;
            DaysValue = (int)Math.Floor(elapsed.TotalDays);
            StatusLabel = Locale.Calendar_Countdown_DaysAgo;
            DetailText = TargetDateText;
        }

        var totalSpan = (model.TargetDate - model.StartDate).TotalSeconds;
        if (totalSpan > 0)
        {
            var elapsedSpan = (now - model.StartDate).TotalSeconds;
            ProgressPercentage = Math.Clamp(elapsedSpan / totalSpan * 100.0, 0.0, 100.0);
        }
        else
        {
            ProgressPercentage = diff <= TimeSpan.Zero ? 100.0 : 0.0;
        }
    }

    public void Dispose()
    {
        TimerService.Timer1Minute.Unsubscribe(Update);
        GC.SuppressFinalize(this);
    }
}
