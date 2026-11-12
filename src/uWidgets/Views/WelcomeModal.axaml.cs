using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models.Settings;
using uWidgets.Services;

namespace uWidgets.Views;

public partial class WelcomeModal : Window, INotifyPropertyChanged
{
    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly ILayoutProvider layoutProvider;
    private readonly WidgetPlacementService placementService;
    private readonly Action<bool> onCompleted;

    private int currentStep = 1;
    private string selectedPack = "Essential";
    private bool finishedNormally;

    public new event PropertyChangedEventHandler? PropertyChanged;

    public WelcomeModal(
        IAppSettingsProvider appSettingsProvider,
        ILayoutProvider layoutProvider,
        WidgetPlacementService placementService,
        Action<bool> onCompleted)
    {
        this.appSettingsProvider = appSettingsProvider;
        this.layoutProvider = layoutProvider;
        this.placementService = placementService;
        this.onCompleted = onCompleted;

        DataContext = this;
        InitializeComponent();
    }

    public bool IsStep1 => currentStep == 1;
    public bool IsStep2 => currentStep == 2;
    public bool IsStep3 => currentStep == 3;
    public bool CanGoBack => currentStep > 1;
    public string StepBadge => $"Step {currentStep} of 3";
    public string NextButtonText => currentStep < 3 ? "Next" : "Get Started";

    public string StepTitle => currentStep switch
    {
        1 => "Welcome to uWidgets",
        2 => "Choose a Starter Pack",
        _ => "Quick Gesture & Shortcut Tour"
    };

    public string StepSubtitle => currentStep switch
    {
        1 => "Lightweight, customizable widgets pinned seamlessly to your Windows desktop.",
        2 => "Start with a curated desktop layout in one click, or pick Clean Slate to build your own.",
        _ => "Five quick habits to help you get the most out of your desktop widgets."
    };

    public bool IsEssentialSelected => selectedPack == "Essential";
    public bool IsDeveloperSelected => selectedPack == "Developer";
    public bool IsMinimalistSelected => selectedPack == "Minimalist";
    public bool IsCleanSlateSelected => selectedPack == "CleanSlate";

    private void SelectEssentialPack(object? sender, RoutedEventArgs e) => SetPack("Essential");
    private void SelectDeveloperPack(object? sender, RoutedEventArgs e) => SetPack("Developer");
    private void SelectMinimalistPack(object? sender, RoutedEventArgs e) => SetPack("Minimalist");
    private void SelectCleanSlate(object? sender, RoutedEventArgs e) => SetPack("CleanSlate");

    private void SetPack(string pack)
    {
        selectedPack = pack;
        NotifyPackProps();
    }

    private void Back_OnClick(object? sender, RoutedEventArgs e)
    {
        if (currentStep > 1)
        {
            currentStep--;
            NotifyStepProps();
        }
    }

    private void Next_OnClick(object? sender, RoutedEventArgs e)
    {
        if (currentStep < 3)
        {
            currentStep++;
            NotifyStepProps();
            return;
        }

        CompleteWizard(spawnSelectedPack: true);
    }

    private void Skip_OnClick(object? sender, RoutedEventArgs e)
    {
        CompleteWizard(spawnSelectedPack: false);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!finishedNormally)
        {
            PersistOnboardingCompleted(null);
            onCompleted(layoutProvider.Get().Count == 0);
        }
        base.OnClosing(e);
    }

    private void CompleteWizard(bool spawnSelectedPack)
    {
        finishedNormally = true;
        var openSettings = false;

        if (spawnSelectedPack && selectedPack != "CleanSlate")
        {
            var items = GetPackItems(selectedPack);
            var spawned = placementService.SpawnStarterPack(this, items);
            if (spawned == 0 && layoutProvider.Get().Count == 0)
                openSettings = true;
        }
        else
        {
            openSettings = layoutProvider.Get().Count == 0 || selectedPack == "CleanSlate";
        }

        PersistOnboardingCompleted(spawnSelectedPack ? selectedPack : "Skipped");
        Close();
        onCompleted(openSettings);
    }

    private void PersistOnboardingCompleted(string? pack)
    {
        var current = appSettingsProvider.Get();
        var updatedOnboarding = current.EffectiveOnboarding with
        {
            IsCompleted = true,
            SelectedStarterPack = pack ?? current.EffectiveOnboarding.SelectedStarterPack
        };
        appSettingsProvider.Save(current with { Onboarding = updatedOnboarding });
    }

    private static IReadOnlyList<(string Type, string SubType, int Cols, int Rows)> GetPackItems(string pack) => pack switch
    {
        "Developer" =>
        [
            ("Clock", "Digital", 2, 2),
            ("Development", "GitWorkingTree", 4, 2),
            ("Development", "Docker", 2, 2),
            ("Monitor", "Dashboard", 2, 2)
        ],
        "Minimalist" =>
        [
            ("Media", "NowPlaying", 4, 2),
            ("Clock", "Pomodoro", 2, 2),
            ("Reminders", "List", 2, 2)
        ],
        _ =>
        [
            ("Clock", "AnalogI", 2, 2),
            ("Calendar", "Month", 2, 2),
            ("Weather", "Forecast", 4, 2),
            ("Notes", "Note", 2, 2)
        ]
    };

    private void NotifyStepProps()
    {
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(StepBadge));
        OnPropertyChanged(nameof(NextButtonText));
        OnPropertyChanged(nameof(StepTitle));
        OnPropertyChanged(nameof(StepSubtitle));
    }

    private void NotifyPackProps()
    {
        OnPropertyChanged(nameof(IsEssentialSelected));
        OnPropertyChanged(nameof(IsDeveloperSelected));
        OnPropertyChanged(nameof(IsMinimalistSelected));
        OnPropertyChanged(nameof(IsCleanSlateSelected));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
