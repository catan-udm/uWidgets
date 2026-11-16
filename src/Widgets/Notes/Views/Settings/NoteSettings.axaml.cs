using Avalonia.Controls;
using Notes.ViewModels;
using uWidgets.Core.Interfaces;

namespace Notes.Views.Settings;

public partial class NoteSettings : UserControl
{
    public NoteSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        DataContext = new NoteSettingsViewModel(widgetLayoutProvider);
        InitializeComponent();
    }
}
