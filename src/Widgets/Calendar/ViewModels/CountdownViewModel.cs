using System;
using System.Threading;
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

    public string TargetDateText => model.TargetDate.ToString("MMM d, yyyy", Thread.CurrentThread.CurrentUICulture);
    public string TooltipText => $"{Title} • {TargetDateText}";

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
        var targetDate = model.TargetDate.Date;
        var diff = targetDate - now;

        if (targetDate == now.Date)
        {
            DaysValue = 0;
            StatusLabel = "TODAY";
            DetailText = TargetDateText;
            ProgressPercentage = 100.0;
            return;
        }

        if (diff > TimeSpan.Zero)
        {
            DaysValue = model.ShowHours
                ? diff.Days
                : (int)(targetDate - now.Date).TotalDays;
            StatusLabel = Locale.Calendar_Countdown_DaysLeft;
            DetailText = model.ShowHours
                ? $"{diff.Days}d {diff.Hours}h {diff.Minutes}m"
                : TargetDateText;
        }
        else
        {
            var elapsedDays = (int)(now.Date - targetDate).TotalDays;
            DaysValue = Math.Max(1, elapsedDays);
            StatusLabel = Locale.Calendar_Countdown_DaysAgo;
            DetailText = TargetDateText;
        }

        var totalSpan = (targetDate - model.StartDate.Date).TotalSeconds;
        if (totalSpan > 0)
        {
            var elapsedSpan = (now - model.StartDate.Date).TotalSeconds;
            ProgressPercentage = Math.Clamp(elapsedSpan / totalSpan * 100.0, 0.0, 100.0);
        }
        else
        {
            ProgressPercentage = targetDate <= now.Date ? 100.0 : 0.0;
        }
    }

    public void Dispose()
    {
        TimerService.Timer1Minute.Unsubscribe(Update);
        GC.SuppressFinalize(this);
    }
}
