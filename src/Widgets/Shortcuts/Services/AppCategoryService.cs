using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Shortcuts.Services;

public record DiscoveredApp(string Name, string Target, string IconKey);

public record AppCategoryInfo(string CategoryName, string IconKey, IReadOnlyList<DiscoveredApp> Apps);

public static class AppCategoryService
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint AssocQueryString(
        uint flags,
        uint str,
        string pszAssoc,
        string? pszExtra,
        [Out] StringBuilder? pszOut,
        ref uint pcchOut);

    private const uint ASSOCSTR_EXECUTABLE = 2;
    private const uint ASSOCSTR_FRIENDLYAPPNAME = 4;

    private static string? GetExecutableForAssoc(string assoc)
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            uint length = 0;
            AssocQueryString(0, ASSOCSTR_EXECUTABLE, assoc, null, null, ref length);
            if (length == 0) return null;
            var sb = new StringBuilder((int)length);
            AssocQueryString(0, ASSOCSTR_EXECUTABLE, assoc, null, sb, ref length);
            var path = sb.ToString();
            return File.Exists(path) ? path : null;
        }
        catch { return null; }
    }

    public static List<AppCategoryInfo> GetCategories()
    {
        var result = new List<AppCategoryInfo>();

        // 1. Development & Code
        var devApps = new List<DiscoveredApp>();
        AddIfFound(devApps, "Visual Studio Code", "Code", GetExecutableForAssoc(".code-workspace") ?? GetExecutableForAssoc(".cs") ?? FindPath(@"%LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe", @"%ProgramFiles%\Microsoft VS Code\Code.exe"));
        AddIfFound(devApps, "Visual Studio", "Code", FindPath(@"%ProgramFiles%\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe", @"%ProgramFiles%\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe", @"%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\devenv.exe"));
        AddIfFound(devApps, "Cursor", "Sparkles", FindPath(@"%LOCALAPPDATA%\Programs\cursor\Cursor.exe"));
        AddIfFound(devApps, "Windsurf", "Sparkles", FindPath(@"%LOCALAPPDATA%\Programs\Windsurf\Windsurf.exe"));
        AddIfFound(devApps, "Antigravity IDE", "Atom", FindPath(@"%LOCALAPPDATA%\Programs\Antigravity\Antigravity.exe"));
        AddIfFound(devApps, "JetBrains Rider", "Code", FindPath(@"%LOCALAPPDATA%\Programs\Rider\bin\rider64.exe"));
        AddIfFound(devApps, "Sublime Text", "Code", FindPath(@"%ProgramFiles%\Sublime Text\sublime_text.exe", @"%ProgramFiles%\Sublime Text 3\sublime_text.exe"));
        AddIfFound(devApps, "Notepad++", "FileCode", FindPath(@"%ProgramFiles%\Notepad++\notepad++.exe", @"%ProgramFiles(x86)%\Notepad++\notepad++.exe"));
        AddIfFound(devApps, "Git Bash", "Terminal", FindPath(@"%ProgramFiles%\Git\git-bash.exe"));
        ScanStartMenu(devApps, "Code", "Visual Studio", "Code", "Rider", "IntelliJ", "PyCharm", "WebStorm", "Cursor", "Windsurf", "Antigravity", "Git Bash");
        result.Add(new AppCategoryInfo("Development & Code", "Code", Deduplicate(devApps)));

        // 2. Web Browsers
        var browserApps = new List<DiscoveredApp>();
        AddIfFound(browserApps, "Microsoft Edge", "Globe", FindPath(@"%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe", @"%ProgramFiles%\Microsoft\Edge\Application\msedge.exe") ?? GetExecutableForAssoc("https"));
        AddIfFound(browserApps, "Google Chrome", "Globe", FindPath(@"%ProgramFiles%\Google\Chrome\Application\chrome.exe", @"%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe", @"%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe"));
        AddIfFound(browserApps, "Mozilla Firefox", "Globe", FindPath(@"%ProgramFiles%\Mozilla Firefox\firefox.exe", @"%ProgramFiles(x86)%\Mozilla Firefox\firefox.exe"));
        AddIfFound(browserApps, "Brave Browser", "Globe", FindPath(@"%ProgramFiles%\BraveSoftware\Brave-Browser\Application\brave.exe"));
        AddIfFound(browserApps, "Opera", "Globe", FindPath(@"%LOCALAPPDATA%\Programs\Opera\launcher.exe", @"%ProgramFiles%\Opera\launcher.exe"));
        AddIfFound(browserApps, "Arc Browser", "Globe", FindPath(@"%LOCALAPPDATA%\Programs\Arc\Arc.exe"));
        ScanStartMenu(browserApps, "Globe", "Chrome", "Firefox", "Edge", "Brave", "Opera", "Arc", "Vivaldi");
        result.Add(new AppCategoryInfo("Web Browsers", "Globe", Deduplicate(browserApps)));

        // 3. Media & Audio
        var mediaApps = new List<DiscoveredApp>();
        AddIfFound(mediaApps, "Spotify", "Music", FindPath(@"%APPDATA%\Spotify\Spotify.exe"));
        AddIfFound(mediaApps, "VLC Media Player", "Play", FindPath(@"%ProgramFiles%\VideoLAN\VLC\vlc.exe", @"%ProgramFiles(x86)%\VideoLAN\VLC\vlc.exe"));
        AddIfFound(mediaApps, "Windows Media Player", "Music", FindPath(@"%ProgramFiles%\Windows Media Player\wmplayer.exe") ?? GetExecutableForAssoc(".mp3"));
        AddIfFound(mediaApps, "Apple Music / iTunes", "Music", FindPath(@"%ProgramFiles%\iTunes\iTunes.exe"));
        ScanStartMenu(mediaApps, "Music", "Spotify", "VLC", "Media Player", "AIMP", "foobar2000", "MusicBee");
        result.Add(new AppCategoryInfo("Media & Audio", "Music", Deduplicate(mediaApps)));

        // 4. Graphics & Design
        var graphicsApps = new List<DiscoveredApp>();
        AddIfFound(graphicsApps, "Adobe Photoshop", "Image", FindPath(@"%ProgramFiles%\Adobe\Adobe Photoshop 2024\Photoshop.exe", @"%ProgramFiles%\Adobe\Adobe Photoshop 2023\Photoshop.exe"));
        AddIfFound(graphicsApps, "Paint", "Image", FindPath(@"%windir%\system32\mspaint.exe"));
        AddIfFound(graphicsApps, "Paint.NET", "Image", FindPath(@"%ProgramFiles%\paint.net\paintdotnet.exe"));
        AddIfFound(graphicsApps, "GIMP", "Image", FindPath(@"%ProgramFiles%\GIMP 2\bin\gimp-2.10.exe"));
        AddIfFound(graphicsApps, "Blender", "Box", FindPath(@"%ProgramFiles%\Blender Foundation\Blender\blender.exe"));
        ScanStartMenu(graphicsApps, "Image", "Photoshop", "Illustrator", "Paint", "GIMP", "Blender", "Figma", "Canva", "Inkscape");
        result.Add(new AppCategoryInfo("Graphics & Design", "Image", Deduplicate(graphicsApps)));

        // 5. Office & Documents
        var officeApps = new List<DiscoveredApp>();
        AddIfFound(officeApps, "Microsoft Word", "FileText", FindPath(@"%ProgramFiles%\Microsoft Office\root\Office16\WINWORD.EXE"));
        AddIfFound(officeApps, "Microsoft Excel", "Grid", FindPath(@"%ProgramFiles%\Microsoft Office\root\Office16\EXCEL.EXE"));
        AddIfFound(officeApps, "Microsoft PowerPoint", "Display", FindPath(@"%ProgramFiles%\Microsoft Office\root\Office16\POWERPNT.EXE"));
        AddIfFound(officeApps, "OneNote", "Book", FindPath(@"%ProgramFiles%\Microsoft Office\root\Office16\ONENOTE.EXE"));
        AddIfFound(officeApps, "Notepad", "FileText", FindPath(@"%windir%\notepad.exe", @"%windir%\system32\notepad.exe"));
        AddIfFound(officeApps, "Adobe Acrobat", "Document", GetExecutableForAssoc(".pdf") ?? FindPath(@"%ProgramFiles%\Adobe\Acrobat DC\Acrobat\Acrobat.exe"));
        AddIfFound(officeApps, "Obsidian", "Book", FindPath(@"%LOCALAPPDATA%\Obsidian\Obsidian.exe"));
        ScanStartMenu(officeApps, "FileText", "Word", "Excel", "PowerPoint", "OneNote", "Notepad", "Acrobat", "Obsidian", "Notion");
        result.Add(new AppCategoryInfo("Office & Documents", "FileText", Deduplicate(officeApps)));

        // 6. System & Tools
        var sysApps = new List<DiscoveredApp>();
        AddIfFound(sysApps, "Windows Terminal", "Terminal", FindPath(@"%LOCALAPPDATA%\Microsoft\WindowsApps\wt.exe"));
        AddIfFound(sysApps, "PowerShell", "Terminal", FindPath(@"%windir%\system32\WindowsPowerShell\v1.0\powershell.exe", @"%ProgramFiles%\PowerShell\7\pwsh.exe"));
        AddIfFound(sysApps, "Command Prompt", "Terminal", FindPath(@"%windir%\system32\cmd.exe"));
        AddIfFound(sysApps, "Task Manager", "Speedometer", FindPath(@"%windir%\system32\taskmgr.exe"));
        AddIfFound(sysApps, "File Explorer", "Folder", FindPath(@"%windir%\explorer.exe"));
        AddIfFound(sysApps, "Calculator", "Calculator", FindPath(@"%windir%\system32\calc.exe"));
        AddIfFound(sysApps, "Registry Editor", "Gear", FindPath(@"%windir%\regedit.exe"));
        result.Add(new AppCategoryInfo("System & Utilities", "Terminal", Deduplicate(sysApps)));

        return result;
    }

    private static string? FindPath(params string[] candidates)
    {
        foreach (var c in candidates)
        {
            var expanded = Environment.ExpandEnvironmentVariables(c);
            if (File.Exists(expanded)) return expanded;
        }
        return null;
    }

    private static void AddIfFound(List<DiscoveredApp> list, string name, string iconKey, string? path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            list.Add(new DiscoveredApp(name, path, iconKey));
        }
    }

    private static void ScanStartMenu(List<DiscoveredApp> list, string iconKey, params string[] keywords)
    {
        try
        {
            var dirs = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                Environment.GetFolderPath(Environment.SpecialFolder.Programs)
            };

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;

                var files = Directory.GetFiles(dir, "*.lnk", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var baseName = Path.GetFileNameWithoutExtension(file);
                    foreach (var kw in keywords)
                    {
                        if (baseName.Contains(kw, StringComparison.OrdinalIgnoreCase) &&
                            !list.Any(x => x.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase)))
                        {
                            list.Add(new DiscoveredApp(baseName, file, iconKey));
                            break;
                        }
                    }
                }
            }
        }
        catch { }
    }

    private static List<DiscoveredApp> Deduplicate(List<DiscoveredApp> list)
    {
        var result = new List<DiscoveredApp>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var app in list)
        {
            if (seen.Add(app.Name))
            {
                result.Add(app);
            }
        }
        return result;
    }
}
