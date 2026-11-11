using Avalonia.Controls;
using Development.Models;
using Development.ViewModels;

namespace Development.Views;

public partial class GitWorkingTree : UserControl
{
    public GitWorkingTree() : this(new GitWorkingTreeModel())
    {
    }

    public GitWorkingTree(GitWorkingTreeModel model)
    {
        DataContext = new GitWorkingTreeViewModel(model);
        InitializeComponent();
    }
}
