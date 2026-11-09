using Avalonia.Controls;
using Avalonia.Interactivity;
using Notes.ViewModels;

namespace Notes.Views.Controls;

public partial class NoteLarge : UserControl
{
    public NoteLarge()
    {
        InitializeComponent();
    }

    public NoteLarge(NoteViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void UpdateTitle(object? sender, RoutedEventArgs e) =>
        (Parent as Note)?.UpdateTitle(sender, e);

    private void UpdateContent(object? sender, RoutedEventArgs e) =>
        (Parent as Note)?.UpdateContent(sender, e);
}
