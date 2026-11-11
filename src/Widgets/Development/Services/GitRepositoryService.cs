using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Development.Models;

namespace Development.Services;

public static class GitRepositoryService
{
    public static async Task<GitStatusInfo> GetStatusAsync(string repoPath)
    {
        if (string.IsNullOrWhiteSpace(repoPath) || !Directory.Exists(repoPath))
        {
            return new GitStatusInfo(repoPath, "No Repository", "N/A", 0, 0, 0, 0, true, false);
        }

        var repoName = Path.GetFileName(repoPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(repoName)) repoName = repoPath;

        return await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo("git", "status --porcelain=v2 --branch")
                {
                    WorkingDirectory = repoPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                    return new GitStatusInfo(repoPath, repoName, "N/A", 0, 0, 0, 0, true, false);

                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(3000);

                if (proc.ExitCode != 0)
                {
                    return new GitStatusInfo(repoPath, repoName, "Not a Git repo", 0, 0, 0, 0, true, false);
                }

                string branch = "HEAD (detached)";
                int ahead = 0;
                int behind = 0;
                int modified = 0;
                int untracked = 0;

                using var reader = new StringReader(output);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("# branch.head "))
                    {
                        branch = line[14..].Trim();
                    }
                    else if (line.StartsWith("# branch.ab "))
                    {
                        var parts = line[12..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            if (parts[0].StartsWith("+") && int.TryParse(parts[0][1..], out var a)) ahead = a;
                            if (parts[1].StartsWith("-") && int.TryParse(parts[1][1..], out var b)) behind = b;
                        }
                    }
                    else if (line.StartsWith("1 ") || line.StartsWith("2 ") || line.StartsWith("u "))
                    {
                        modified++;
                    }
                    else if (line.StartsWith("? "))
                    {
                        untracked++;
                    }
                }

                bool isClean = modified == 0 && untracked == 0;
                return new GitStatusInfo(repoPath, repoName, branch, ahead, behind, modified, untracked, isClean, true);
            }
            catch
            {
                return new GitStatusInfo(repoPath, repoName, "Git error", 0, 0, 0, 0, true, false);
            }
        });
    }

    public static FileSystemWatcher? CreateWatcher(string repoPath, Action onChanged)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(repoPath) || !Directory.Exists(repoPath))
                return null;

            string gitDir = Path.Combine(repoPath, ".git");
            if (File.Exists(gitDir))
            {
                // Worktree pointer file: "gitdir: <path>"
                var content = File.ReadAllText(gitDir).Trim();
                if (content.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase))
                {
                    var resolved = content[7..].Trim();
                    if (!Path.IsPathRooted(resolved))
                    {
                        resolved = Path.GetFullPath(Path.Combine(repoPath, resolved));
                    }
                    gitDir = resolved;
                }
            }

            if (!Directory.Exists(gitDir)) return null;

            var watcher = new FileSystemWatcher(gitDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            var debounceTimer = new System.Timers.Timer(400) { AutoReset = false };
            debounceTimer.Elapsed += (_, _) => onChanged();

            void HandleEvent(object s, FileSystemEventArgs e)
            {
                debounceTimer.Stop();
                debounceTimer.Start();
            }

            watcher.Changed += HandleEvent;
            watcher.Created += HandleEvent;
            watcher.Deleted += HandleEvent;
            watcher.Renamed += (_, e) => HandleEvent(null!, e);
            watcher.Disposed += (_, _) => debounceTimer.Dispose();

            return watcher;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("shell32.dll", EntryPoint = "SHOpenWithDialog", CharSet = CharSet.Unicode)]
    private static extern int SHOpenWithDialog(IntPtr hWndParent, ref OPENASINFO poa);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OPENASINFO
    {
        public string pcszFile;
        public string pcszClass;
        public int oaifInFlags;
    }

    private const int OAIF_ALLOW_REGISTRATION = 0x00000001;
    private const int OAIF_EXEC = 0x00000004;

    public static void PromptOpenWith(string repoPath)
    {
        if (!OperatingSystem.IsWindows() || !Directory.Exists(repoPath)) return;
        try
        {
            var info = new OPENASINFO
            {
                pcszFile = repoPath,
                pcszClass = ".code-workspace",
                oaifInFlags = OAIF_ALLOW_REGISTRATION | OAIF_EXEC
            };
            int hr = SHOpenWithDialog(IntPtr.Zero, ref info);
            if (hr != 0)
            {
                Process.Start(new ProcessStartInfo("rundll32.exe", $"shell32.dll,OpenAs_RunDLL \"{repoPath}\"")
                {
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo("rundll32.exe", $"shell32.dll,OpenAs_RunDLL \"{repoPath}\"")
                {
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }

    public static void OpenInEditor(string repoPath, CodeEditorType editor, string? customCommand)
    {
        if (string.IsNullOrWhiteSpace(repoPath) || !Directory.Exists(repoPath)) return;

        try
        {
            switch (editor)
            {
                case CodeEditorType.VSCode:
                    RunGuiApp("code", repoPath);
                    break;
                case CodeEditorType.Cursor:
                    RunGuiApp("cursor", repoPath);
                    break;
                case CodeEditorType.Windsurf:
                    RunGuiApp("windsurf", repoPath);
                    break;
                case CodeEditorType.AntigravityIDE:
                    RunGuiApp("antigravity", repoPath);
                    break;
                case CodeEditorType.AntigravityCLI:
                    RunTerminalApp("agy", repoPath);
                    break;
                case CodeEditorType.ClaudeCode:
                    RunTerminalApp("claude", repoPath);
                    break;
                case CodeEditorType.Aider:
                    RunTerminalApp("aider", repoPath);
                    break;
                case CodeEditorType.Rider:
                    RunGuiApp("rider", repoPath);
                    break;
                case CodeEditorType.IntelliJ:
                    RunGuiApp("idea", repoPath);
                    break;
                case CodeEditorType.PyCharm:
                    RunGuiApp("pycharm", repoPath);
                    break;
                case CodeEditorType.WebStorm:
                    RunGuiApp("webstorm", repoPath);
                    break;
                case CodeEditorType.VisualStudio:
                    RunGuiApp("devenv", repoPath);
                    break;
                case CodeEditorType.Zed:
                    RunGuiApp("zed", repoPath);
                    break;
                case CodeEditorType.SublimeText:
                    RunGuiApp("subl", repoPath);
                    break;
                case CodeEditorType.Neovim:
                    RunTerminalApp("nvim .", repoPath);
                    break;
                case CodeEditorType.SystemOpenWith:
                    PromptOpenWith(repoPath);
                    break;
                case CodeEditorType.Custom:
                    if (!string.IsNullOrWhiteSpace(customCommand))
                    {
                        string cmd = customCommand.Contains("{path}")
                            ? customCommand.Replace("{path}", repoPath)
                            : $"{customCommand} \"{repoPath}\"";
                        Process.Start(new ProcessStartInfo("cmd.exe", $"/c {cmd}") { CreateNoWindow = true, UseShellExecute = false });
                    }
                    else
                    {
                        RunGuiApp("code", repoPath);
                    }
                    break;
                default:
                    PromptOpenWith(repoPath);
                    break;
            }
        }
        catch
        {
            PromptOpenWith(repoPath);
        }
    }

    private static void RunGuiApp(string app, string repoPath)
    {
        try
        {
            var psi = new ProcessStartInfo("cmd.exe", $"/c {app} \"{repoPath}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var p = Process.Start(psi);
            if (p == null) PromptOpenWith(repoPath);
        }
        catch
        {
            PromptOpenWith(repoPath);
        }
    }

    private static void RunTerminalApp(string app, string repoPath)
    {
        try
        {
            var psi = new ProcessStartInfo("powershell.exe", $"-NoExit -Command \"Set-Location '{repoPath}'; {app}\"")
            {
                UseShellExecute = true
            };
            var p = Process.Start(psi);
            if (p == null) PromptOpenWith(repoPath);
        }
        catch
        {
            PromptOpenWith(repoPath);
        }
    }

    public static void OpenInVSCode(string repoPath) => OpenInEditor(repoPath, CodeEditorType.VSCode, null);

    public static void OpenInTerminal(string repoPath)
    {
        if (Directory.Exists(repoPath))
        {
            try
            {
                Process.Start(new ProcessStartInfo("powershell.exe", $"-NoExit -Command \"Set-Location '{repoPath}'\"")
                {
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }

    public static void OpenInExplorer(string repoPath)
    {
        if (Directory.Exists(repoPath))
        {
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", repoPath) { UseShellExecute = true });
            }
            catch { }
        }
    }
}
