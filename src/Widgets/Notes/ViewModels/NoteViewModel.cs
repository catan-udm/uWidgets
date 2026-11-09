using System.Threading;
using Notes.Models;
using ReactiveUI;

namespace Notes.ViewModels;

public class NoteViewModel(NoteModel noteModel) : ReactiveObject
{
    public string? Title => noteModel.Title;
    public string? Content => noteModel.Content;
    public string? Updated => noteModel.Updated?.ToString("g", Thread.CurrentThread.CurrentUICulture);
    public int CharacterCount => (noteModel.Content ?? string.Empty).Length;
}
