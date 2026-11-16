using System.Globalization;
using Calendar.Models;
using ReactiveUI;
using uWidgets.Services;

namespace Calendar.ViewModels;

public class MonthCalendarViewModel : ReactiveObject, IDisposable
{
    private readonly MonthCalendarModel monthCalendarModel;
    private DateTime currentDate;
    private int monthOffset;
    
    public MonthCalendarViewModel(MonthCalendarModel monthCalendarModel)
    {
        this.monthCalendarModel = monthCalendarModel;
        TimerService.Timer1Minute.Subscribe(UpdateTime);
        UpdateTime();
    }

    public void Dispose()
    {
        TimerService.Timer1Minute.Unsubscribe(UpdateTime);
        GC.SuppressFinalize(this);
    }

    public bool IsViewingOtherMonth => monthOffset != 0;

    public void PreviousMonth()
    {
        monthOffset--;
        RebuildMonth(DateTime.Now);
    }

    public void NextMonth()
    {
        monthOffset++;
        RebuildMonth(DateTime.Now);
    }

    public void ResetToToday()
    {
        if (monthOffset == 0) return;
        monthOffset = 0;
        RebuildMonth(DateTime.Now);
    }

    private void UpdateTime()
    {
        var now = DateTime.Now;
        if (currentDate.Date == now.Date) return;

        currentDate = now;
        RebuildMonth(now);
    }

    private void RebuildMonth(DateTime now)
    {
        var displayDate = new DateTime(now.Year, now.Month, 1).AddMonths(monthOffset);
        var format = Thread.CurrentThread.CurrentUICulture.DateTimeFormat;
        var monthName = format.GetMonthName(displayDate.Month).ToUpper(Thread.CurrentThread.CurrentUICulture);

        Month = displayDate.Year == now.Year ? monthName : $"{monthName} {displayDate.Year}";
        this.RaisePropertyChanged(nameof(IsViewingOtherMonth));
        Days = GetWeekDays(format)
            .Concat(GetEmptyDays(displayDate))
            .Concat(GetDaysOfMonth(displayDate, now))
            .ToList();
    }
    
    private string? month;
    public string? Month 
    {
        get => month;
        private set => this.RaiseAndSetIfChanged(ref month, value);
    }

    private List<DayViewModel>? days;
    public List<DayViewModel>? Days
    {
        get => days;
        private set => this.RaiseAndSetIfChanged(ref days, value);
    }
    
    private static IEnumerable<DayViewModel> GetDaysOfMonth(DateTime displayDate, DateTime today)
    {
        var isCurrentMonth = displayDate.Year == today.Year && displayDate.Month == today.Month;
        return Enumerable
            .Range(1, DateTime.DaysInMonth(displayDate.Year, displayDate.Month))
            .Select(day => new DayViewModel(
                day.ToString(),
                IsWeekend(new DateTime(displayDate.Year, displayDate.Month, day).DayOfWeek),
                isCurrentMonth && day == today.Day));
    }

    public static int ComputeLeadingEmptyDays(int year, int month, DayOfWeek firstDayOfWeek)
    {
        var startOfMonthDayOfWeek = (int)new DateTime(year, month, 1).DayOfWeek;
        return (startOfMonthDayOfWeek - (int)firstDayOfWeek + 7) % 7;
    }

    private IEnumerable<DayViewModel> GetEmptyDays(DateTime displayDate)
    {
        var count = ComputeLeadingEmptyDays(displayDate.Year, displayDate.Month, monthCalendarModel.FirstDayOfWeek);
        return Enumerable.Range(0, count).Select(_ => new DayViewModel());
    }

    private IEnumerable<DayViewModel> GetWeekDays(DateTimeFormatInfo format)
    {
        var offset = (int)monthCalendarModel.FirstDayOfWeek;
        
        return Enumerable.Range(0, 7)
            .Select(i => (DayOfWeek)((i + offset) % 7))
            .Select(day => new DayViewModel(
                format.GetShortestDayName(day),
                IsWeekend(day)));
    }
    
    private static bool IsWeekend(DayOfWeek dayOfWeek) => dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}