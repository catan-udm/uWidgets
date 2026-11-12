using System.Diagnostics;

namespace uWidgets.Core.Services;

/// <summary>
/// Initializes and migrates user-writable application storage directories safely.
/// </summary>
public static class StorageService
{
    private static bool initialized;

    /// <summary>
    /// Resolves <see cref="Const.CurrentFolder"/> (portable mode or <c>%APPDATA%\uWidgets</c>),
    /// ensures required directories exist, and migrates legacy configuration files if needed.
    /// </summary>
    public static void Initialize()
    {
        if (initialized) return;

        try
        {
            var appDir = Const.AppFolder;
            var portableFlag = Path.Combine(appDir, "portable.dat");
            var portableEnv = string.Equals(
                Environment.GetEnvironmentVariable("UWIDGETS_PORTABLE"),
                "1",
                StringComparison.OrdinalIgnoreCase);

            if ((File.Exists(portableFlag) || portableEnv) && IsDirectoryWritable(appDir))
            {
                Const.CurrentFolder = appDir;
            }
            else
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                Const.CurrentFolder = string.IsNullOrWhiteSpace(appData)
                    ? Path.Combine(Path.GetTempPath(), Const.AppName)
                    : Path.Combine(appData, Const.AppName);

                Directory.CreateDirectory(Const.CurrentFolder);
                MigrateLegacyFiles(appDir, Const.CurrentFolder);
            }

            Directory.CreateDirectory(Const.LogsFolder);
            Directory.CreateDirectory(Const.ProfilesFolder);
            initialized = true;
        }
        catch (Exception ex)
        {
            try
            {
                Const.CurrentFolder = Path.Combine(Path.GetTempPath(), Const.AppName);
                Directory.CreateDirectory(Const.CurrentFolder);
                Directory.CreateDirectory(Const.LogsFolder);
                Directory.CreateDirectory(Const.ProfilesFolder);
            }
            catch
            {
                // Keep default AppFolder as last resort
            }

            Debug.WriteLine($"[StorageService] Initialization warning: {ex.Message}");
            initialized = true;
        }
    }

    private static bool IsDirectoryWritable(string directoryPath)
    {
        try
        {
            var probeFile = Path.Combine(directoryPath, $".write_probe_{Guid.NewGuid():N}.tmp");
            using (File.Create(probeFile, 1, FileOptions.DeleteOnClose))
            {
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void MigrateLegacyFiles(string sourceDir, string targetDir)
    {
        if (string.Equals(Path.GetFullPath(sourceDir), Path.GetFullPath(targetDir), StringComparison.OrdinalIgnoreCase))
            return;

        string[] legacyFiles =
        [
            "appSettings.json",
            "layout.json",
            "finance_crypto_cache.json",
            "finance_exchange_rates.json",
            "finance_stocks_cache.json"
        ];

        foreach (var fileName in legacyFiles)
        {
            try
            {
                var sourceFile = Path.Combine(sourceDir, fileName);
                var targetFile = Path.Combine(targetDir, fileName);

                if (File.Exists(sourceFile) && !File.Exists(targetFile))
                {
                    File.Copy(sourceFile, targetFile, overwrite: false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StorageService] Failed migrating {fileName}: {ex.Message}");
            }
        }

        try
        {
            var sourceIcons = Path.Combine(sourceDir, "CustomIcons");
            var targetIcons = Path.Combine(targetDir, "CustomIcons");
            if (Directory.Exists(sourceIcons) && !Directory.Exists(targetIcons))
            {
                Directory.CreateDirectory(targetIcons);
                foreach (var file in Directory.GetFiles(sourceIcons))
                {
                    var dest = Path.Combine(targetIcons, Path.GetFileName(file));
                    if (!File.Exists(dest))
                        File.Copy(file, dest, overwrite: false);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StorageService] Failed migrating CustomIcons: {ex.Message}");
        }
    }
}
