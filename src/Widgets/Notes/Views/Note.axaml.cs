using System;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Notes.Locales;
using Notes.Models;
using Notes.ViewModels;
using Notes.Views.Controls;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;

namespace Notes.Views;

public partial class Note : UserControl, ILiveSettingsWidget
{
    private NoteModel model;
    private readonly NoteViewModel viewModel;
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private readonly DispatcherTimer autoSaveTimer;
    private string? currentTier;
    private bool isSelfSaving;
    private string? pendingTitle;
    private string? pendingContent;
    private bool hasPendingEdits;

    public Note(IWidgetLayoutProvider widgetLayoutProvider)
        : this(new NoteModel(Locale.Notes_Title), widgetLayoutProvider) { }

    public Note(NoteModel model, IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.model = model;
        this.widgetLayoutProvider = widgetLayoutProvider;
        viewModel = new NoteViewModel(model);
        DataContext = viewModel;
        currentTier = "Small";
        Content = new NoteSmall(viewModel);

        autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(750) };
        autoSaveTimer.Tick += (_, _) => FlushPendingEdits();

        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;

        InitializeComponent();
    }

    public bool OnLayoutSettingsChanged(WidgetLayout newLayout)
    {
        if (isSelfSaving)
            return true;

        var incoming = newLayout.GetModel<NoteModel>();
        if (incoming == null)
            return false;

        model = incoming;
        hasPendingEdits = false;
        autoSaveTimer.Stop();
        viewModel.ApplyModel(incoming);
        return true;
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var nextTier = e.NewSize switch
        {
            { Width: <= 120 } or { Height: <= 120 } => "Tiny",
            { Width: > 230, Height: > 230 } => "Large",
            { Width: > 230 } => "Wide",
            _ => "Small"
        };

        if (nextTier == currentTier) return;
        currentTier = nextTier;

        Content = nextTier switch
        {
            "Tiny" => new NoteTiny(viewModel),
            "Large" => new NoteLarge(viewModel),
            "Wide" => new NoteWide(viewModel),
            _ => new NoteSmall(viewModel)
        };
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        autoSaveTimer.Stop();
        FlushPendingEdits();
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
    }

    public void OnTitleTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            pendingTitle = tb.Text;
            hasPendingEdits = true;
            autoSaveTimer.Stop();
            autoSaveTimer.Start();
        }
    }

    public void OnContentTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            pendingContent = tb.Text;
            hasPendingEdits = true;
            autoSaveTimer.Stop();
            autoSaveTimer.Start();
        }
    }

    public void UpdateContent(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        autoSaveTimer.Stop();
        var newText = tb.Text;
        if (string.Equals(newText ?? string.Empty, model.Content ?? string.Empty, StringComparison.Ordinal))
        {
            hasPendingEdits = false;
            return;
        }
        hasPendingEdits = false;
        UpdateModel(model with { Content = newText, Updated = DateTime.Now });
    }

    public void UpdateTitle(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        autoSaveTimer.Stop();
        var newText = tb.Text;
        if (string.Equals(newText ?? string.Empty, model.Title ?? string.Empty, StringComparison.Ordinal))
        {
            hasPendingEdits = false;
            return;
        }
        hasPendingEdits = false;
        UpdateModel(model with { Title = newText, Updated = DateTime.Now });
    }

    private void FlushPendingEdits()
    {
        autoSaveTimer.Stop();
        if (!hasPendingEdits) return;
        hasPendingEdits = false;

        var nextTitle = pendingTitle ?? model.Title;
        var nextContent = pendingContent ?? model.Content;
        if (string.Equals(nextTitle ?? string.Empty, model.Title ?? string.Empty, StringComparison.Ordinal) &&
            string.Equals(nextContent ?? string.Empty, model.Content ?? string.Empty, StringComparison.Ordinal))
        {
            return;
        }

        UpdateModel(model with { Title = nextTitle, Content = nextContent, Updated = DateTime.Now });
    }

    public async void CopyNote_OnClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;

        var text = string.IsNullOrWhiteSpace(model.Title)
            ? (model.Content ?? string.Empty)
            : string.IsNullOrWhiteSpace(model.Content)
                ? model.Title
                : $"{model.Title}{Environment.NewLine}{model.Content}";

        await clipboard.SetTextAsync(text);
        viewModel.SetCopiedState(true);
        await Task.Delay(1500);
        viewModel.SetCopiedState(false);
    }

    private void UpdateModel(NoteModel newModel)
    {
        model = newModel;
        viewModel.ApplyModel(newModel);

        var newSettings = JsonSerializer.SerializeToElement(newModel);
        var newLayout = widgetLayoutProvider.Get() with { Settings = newSettings };

        isSelfSaving = true;
        try
        {
            widgetLayoutProvider.Save(newLayout);
        }
        finally
        {
            isSelfSaving = false;
        }
    }
}
