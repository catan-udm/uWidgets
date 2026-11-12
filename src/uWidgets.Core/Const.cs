namespace uWidgets.Core;

/// <summary>
/// Constants and resolved storage paths for the application.
/// </summary>
public static class Const
{
    /// <summary>
    /// The name of the application.
    /// </summary>
    public const string AppName = "uWidgets";

    /// <summary>
    /// The installation directory containing the application executable and widget plugins.
    /// </summary>
    public static readonly string AppFolder = !string.IsNullOrEmpty(AppContext.BaseDirectory)
        ? AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        : (Path.GetDirectoryName(Environment.ProcessPath) ?? AppDomain.CurrentDomain.BaseDirectory);

    /// <summary>
    /// The active user-data directory (AppData by default, or <see cref="AppFolder"/> in portable mode).
    /// </summary>
    public static string CurrentFolder { get; internal set; } = AppFolder;

    /// <summary>
    /// The folder containing widget plugin assemblies.
    /// </summary>
    public static string WidgetsFolder => Path.Combine(AppFolder, WidgetsFolderName);

    /// <summary>
    /// The path to the application settings file.
    /// </summary>
    public static string AppSettingsFile => Path.Combine(CurrentFolder, AppSettingsFileName);

    /// <summary>
    /// The path to the layout file.
    /// </summary>
    public static string LayoutFile => Path.Combine(CurrentFolder, LayoutFileName);

    /// <summary>
    /// The folder for diagnostic and crash logs.
    /// </summary>
    public static string LogsFolder => Path.Combine(CurrentFolder, LogsFolderName);

    /// <summary>
    /// The folder for saved desktop layout profiles.
    /// </summary>
    public static string ProfilesFolder => Path.Combine(CurrentFolder, ProfilesFolderName);

    private static string WidgetsFolderName => "Widgets";
    private static string AppSettingsFileName => "appSettings.json";
    private static string LayoutFileName => "layout.json";
    private static string LogsFolderName => "Logs";
    private static string ProfilesFolderName => "Profiles";
}