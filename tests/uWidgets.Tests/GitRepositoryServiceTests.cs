using Avalonia.Controls;
using Avalonia.LogicalTree;
using Development.Models;
using Development.Services;
using Development.ViewModels;
using Development.Views;
using Xunit;

namespace uWidgets.Tests;

public class GitRepositoryServiceTests
{
    [Fact]
    public void BuildTerminalAdminStartInfo_ConfiguresElevatedPowerShellAtRepoPath()
    {
        const string repoPath = @"C:\Users\dev\My Repo";

        var psi = GitRepositoryService.BuildTerminalAdminStartInfo(repoPath);

        Assert.Equal("powershell.exe", psi.FileName);
        Assert.True(psi.UseShellExecute);
        Assert.Equal("runas", psi.Verb);
        Assert.Equal(repoPath, psi.WorkingDirectory);
        Assert.False(psi.CreateNoWindow);
        Assert.Equal(
            "-NoExit -Command \"Set-Location -LiteralPath 'C:\\Users\\dev\\My Repo'\"",
            psi.Arguments);
    }

    [Fact]
    public void BuildTerminalAdminStartInfo_EscapesSingleQuotesAndPreservesSpecialCharacters()
    {
        const string repoPath = @"C:\Repos\O'Brien's & (Test) $env";

        var psi = GitRepositoryService.BuildTerminalAdminStartInfo(repoPath);

        Assert.Equal(
            "-NoExit -Command \"Set-Location -LiteralPath 'C:\\Repos\\O''Brien''s & (Test) $env'\"",
            psi.Arguments);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildTerminalAdminStartInfo_ThrowsForBlankRepoPath(string repoPath)
    {
        Assert.ThrowsAny<ArgumentException>(() => GitRepositoryService.BuildTerminalAdminStartInfo(repoPath));
    }

    [Fact]
    public void OpenInTerminal_IgnoresMissingOrBlankDirectoryWithoutThrowing()
    {
        var nonexistent = Path.Combine(Path.GetTempPath(), $"uWidgets_Missing_{Guid.NewGuid():N}");

        var ex1 = Record.Exception(() => GitRepositoryService.OpenInTerminal(""));
        var ex2 = Record.Exception(() => GitRepositoryService.OpenInTerminal("   "));
        var ex3 = Record.Exception(() => GitRepositoryService.OpenInTerminal(nonexistent));

        Assert.Null(ex1);
        Assert.Null(ex2);
        Assert.Null(ex3);
    }
}

[Collection(AvaloniaUiCollection.Name)]
public class GitWorkingTreeTerminalUiTests
{
    [Fact]
    public void GitWorkingTreeViewModel_OpenTerminalCommand_NoOpsSafelyWithoutValidRepo()
    {
        var nonexistent = Path.Combine(Path.GetTempPath(), $"uWidgets_Missing_{Guid.NewGuid():N}");
        using var vm = new GitWorkingTreeViewModel(new GitWorkingTreeModel
        {
            Repositories = [nonexistent],
            SelectedIndex = 0
        });

        Assert.True(vm.OpenTerminalCommand.CanExecute(null));
        var ex = Record.Exception(() => vm.OpenTerminalCommand.Execute(null));
        Assert.Null(ex);
    }

    [Fact]
    public void GitWorkingTree_TerminalButtons_ExposeAdminPowerShellTooltipAndCommand()
    {
        using var widget = new GitWorkingTree(new GitWorkingTreeModel());
        var vm = Assert.IsType<GitWorkingTreeViewModel>(widget.DataContext);

        var terminalButtons = widget.GetLogicalDescendants()
            .OfType<Button>()
            .Where(b => ReferenceEquals(b.Command, vm.OpenTerminalCommand))
            .ToList();

        Assert.Equal(2, terminalButtons.Count);
        Assert.All(terminalButtons, button =>
            Assert.Equal("Open PowerShell (Administrator)", ToolTip.GetTip(button)));
    }
}
