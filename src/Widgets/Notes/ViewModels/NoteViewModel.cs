using System.Threading;
using Notes.Models;
using ReactiveUI;

namespace Notes.ViewModels;

public class NoteViewModel(NoteModel noteModel) : ReactiveObject
{
    private NoteModel model = noteModel;
    private bool copiedRecently;

    public string? Title => model.Title;
    public string? Content => model.Content;
    public string? Updated => model.ShowTimestamp ? model.Updated?.ToString("g", Thread.CurrentThread.CurrentUICulture) : null;
    public bool ShowTimestamp => model.ShowTimestamp && model.Updated.HasValue;
    public int CharacterCount => (model.Content ?? string.Empty).Length;
    public double ContentFontSize => Math.Clamp(model.FontSize <= 0 ? 13.0 : model.FontSize, 10.0, 24.0);
    public string CopyButtonTooltip => copiedRecently ? "Copied!" : "Copy note";

    public void ApplyModel(NoteModel updated)
    {
        model = updated;
        this.RaisePropertyChanged(nameof(Title));
        this.RaisePropertyChanged(nameof(Content));
        this.RaisePropertyChanged(nameof(Updated));
        this.RaisePropertyChanged(nameof(ShowTimestamp));
        this.RaisePropertyChanged(nameof(CharacterCount));
        this.RaisePropertyChanged(nameof(ContentFontSize));
    }

    public void SetCopiedState(bool copied)
    {
        copiedRecently = copied;
        this.RaisePropertyChanged(nameof(CopyButtonTooltip));
    }
}
