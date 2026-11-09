using System;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Notes.Locales;
using Notes.Models;
using Notes.ViewModels;
using Notes.Views.Controls;
using uWidgets.Core.Interfaces;

namespace Notes.Views;

public partial class Note : UserControl
{
    private NoteModel model;
    private NoteViewModel viewModel;
    private readonly IWidgetLayoutProvider widgetLayoutProvider;

    public Note(IWidgetLayoutProvider widgetLayoutProvider)
        : this(new NoteModel(Locale.Notes_Title), widgetLayoutProvider) { }

    public Note(NoteModel model, IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.model = model;
        this.widgetLayoutProvider = widgetLayoutProvider;
        viewModel = new NoteViewModel(model);
        DataContext = viewModel;
        Content = new NoteSmall(viewModel);
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;

        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Content = e.NewSize switch
        {
            { Width: <= 120 } or { Height: <= 120 } => new NoteTiny(viewModel),
            { Width: > 230, Height: > 230 } => new NoteLarge(viewModel),
            { Width: > 230 } => new NoteWide(viewModel),
            _ => new NoteSmall(viewModel)
        };
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
    }

    public void UpdateContent(object? sender, RoutedEventArgs e)
    {
        var newText = (sender as TextBox)!.Text;
        UpdateModel(model with { Content = newText, Updated = DateTime.Now });
    }

    public void UpdateTitle(object? sender, RoutedEventArgs e)
    {
        var newText = (sender as TextBox)!.Text;
        UpdateModel(model with { Title = newText, Updated = DateTime.Now });
    }

    private void UpdateModel(NoteModel newModel)
    {
        model = newModel;
        viewModel = new NoteViewModel(newModel);
        DataContext = viewModel;
        if (Content is Control control)
        {
            control.DataContext = viewModel;
        }

        var newSettings = JsonSerializer.SerializeToElement(newModel);
        var newLayout = widgetLayoutProvider.Get() with { Settings = newSettings };

        widgetLayoutProvider.Save(newLayout);
    }
}
