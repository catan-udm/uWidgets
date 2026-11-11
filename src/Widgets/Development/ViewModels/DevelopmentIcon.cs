using Avalonia.Media;

namespace Development.ViewModels;

public static class DevelopmentIcon
{
    public static Lazy<StreamGeometry> Category = new(() => StreamGeometry.Parse(
        "M10.478 1.647a.5.5 0 1 0-.956-.294l-4 13a.5.5 0 0 0 .956.294l4-13zM4.854 4.146a.5.5 0 0 1 0 .708L1.707 8l3.147 3.146a.5.5 0 0 1-.708.708l-3.5-3.5a.5.5 0 0 1 0-.708l3.5-3.5a.5.5 0 0 1 .708 0zm6.292 0a.5.5 0 0 0 0 .708L14.293 8l-3.147 3.146a.5.5 0 0 0 .708.708l3.5-3.5a.5.5 0 0 0 0-.708l-3.5-3.5a.5.5 0 0 0-.708 0z"));

    public static Lazy<StreamGeometry> Docker = new(() => StreamGeometry.Parse(
        "M10.5 4h1.5v1.5h-1.5zm-2 0h1.5v1.5h-1.5zm-2 0h1.5v1.5h-1.5zm-2 2h1.5v1.5h-1.5zm2 0h1.5v1.5h-1.5zm2 0h1.5v1.5h-1.5zm2 0h1.5v1.5h-1.5zm-8 2h1.5v1.5h-1.5zm2 0h1.5v1.5h-1.5zm2 0h1.5v1.5h-1.5zm2 0h1.5v1.5h-1.5zm2 0h1.5v1.5h-1.5zm3.8-1.5c-.3-.2-.8-.4-1.3-.4-.1 0-.2 0-.3.1-.3-.6-.8-1-1.4-1.2h-.3v3.5H0c.1 1.8.8 3.5 2.1 4.7 1.4 1.3 3.3 2 5.4 2 4.4 0 8.1-2.9 9.3-7.1.6 0 1.2-.2 1.6-.6.5-.5.7-1.1.7-1.7-.5.1-1.1.4-1.6.7z"));

    public static Lazy<StreamGeometry> NodeJs = new(() => StreamGeometry.Parse(
        "M8 1L1.5 4.75v7.5L8 16l6.5-3.75v-7.5L8 1zm0 1.5l5 2.9v5.8L8 14.1l-5-2.9v-5.8l5-2.9z"));

    public static Lazy<StreamGeometry> NextJs = new(() => StreamGeometry.Parse(
        "M8 0a8 8 0 1 0 8 8A8 8 0 0 0 8 0zm3.3 11.8L6.8 6.1H5.4v5.3h1.3V7.9l4 5.1c-.5.4-1 .6-1.6.7-.6.1-1.2.1-1.8 0a6.5 6.5 0 0 1-5.2-5.2c-.2-.7-.2-1.4 0-2.1a6.5 6.5 0 0 1 5.2-5.2c.7-.2 1.4-.2 2.1 0a6.5 6.5 0 0 1 5.2 5.2c.2.7.2 1.4 0 2.1-.3 1.4-1 2.5-2.1 3.3z"));

    public static Lazy<StreamGeometry> Vite = new(() => StreamGeometry.Parse(
        "M15.4 2.1L8.6 15.7a.5.5 0 0 1-.9 0L.6 2.1A.5.5 0 0 1 1 1.4l14 0a.5.5 0 0 1 .4.7zM9.5 2.5l-4.2 4.5 2.4.4-2.7 4.2 4.8-5.3-2.5-.4 2.2-3.4z"));

    public static Lazy<StreamGeometry> GitBranch = new(() => StreamGeometry.Parse(
        "M11.75 2.5a2.25 2.25 0 1 0-1.77 3.63v1.62c0 .97-.78 1.75-1.75 1.75H6.27a2.25 2.25 0 1 0-2.02 1.25v2.5a2.25 2.25 0 1 0 1.5 0v-2.5h2.5a3.25 3.25 0 0 0 3.25-3.25V6.13a2.25 2.25 0 0 0 .25-3.63zm-7.5 7.5a.75.75 0 1 1 0 1.5.75.75 0 0 1 0-1.5zm0 4.5a.75.75 0 1 1 0 1.5.75.75 0 0 1 0-1.5zm7.5-12a.75.75 0 1 1 0 1.5.75.75 0 0 1 0-1.5z"));

    public static Lazy<StreamGeometry> Play = new(() => StreamGeometry.Parse(
        "m11.596 8.697-6.363 3.692c-.54.313-1.233-.066-1.233-.697V4.308c0-.63.692-1.01 1.233-.696l6.363 3.692a.802.802 0 0 1 0 1.393z"));

    public static Lazy<StreamGeometry> Stop = new(() => StreamGeometry.Parse(
        "M3.5 3a1.5 1.5 0 0 0-1.5 1.5v7A1.5 1.5 0 0 0 3.5 13h7a1.5 1.5 0 0 0 1.5-1.5v-7A1.5 1.5 0 0 0 10.5 3h-7z"));

    public static Lazy<StreamGeometry> Restart = new(() => StreamGeometry.Parse(
        "M11.5 3.5a5.5 5.5 0 1 0 2.2 4.4h-1.5a4 4 0 1 1-1.6-3.2l-1.6 1.6H15V2l-1.5 1.5h-2z"));

    public static Lazy<StreamGeometry> Terminal = new(() => StreamGeometry.Parse(
        "M1.5 2A1.5 1.5 0 0 0 0 3.5v9A1.5 1.5 0 0 0 1.5 14h13a1.5 1.5 0 0 0 1.5-1.5v-9A1.5 1.5 0 0 0 14.5 2h-13zm2.15 3.15a.5.5 0 0 1 .7 0l2.5 2.5a.5.5 0 0 1 0 .7l-2.5 2.5a.5.5 0 0 1-.7-.7L5.79 8.5 3.65 6.35a.5.5 0 0 1 0-.7zM8 10.5a.5.5 0 0 1 .5-.5h4a.5.5 0 0 1 0 1h-4a.5.5 0 0 1-.5-.5z"));

    public static Lazy<StreamGeometry> Code = new(() => StreamGeometry.Parse(
        "M5.85 4.15a.5.5 0 0 1 0 .7L2.7 8l3.15 3.15a.5.5 0 0 1-.7.7l-3.5-3.5a.5.5 0 0 1 0-.7l3.5-3.5a.5.5 0 0 1 .7 0zm4.3 0a.5.5 0 0 0 0 .7L13.3 8l-3.15 3.15a.5.5 0 0 0 .7.7l3.5-3.5a.5.5 0 0 0 0-.7l-3.5-3.5a.5.5 0 0 0-.7 0z"));

    public static Lazy<StreamGeometry> Folder = new(() => StreamGeometry.Parse(
        "M1 3.5A1.5 1.5 0 0 1 2.5 2h3.1a1.5 1.5 0 0 1 1.06.44l1.34 1.34A.5.5 0 0 0 8.35 4H13.5A1.5 1.5 0 0 1 15 5.5v7a1.5 1.5 0 0 1-1.5 1.5h-11A1.5 1.5 0 0 1 1 12.5v-9z"));

    public static Lazy<StreamGeometry> Globe = new(() => StreamGeometry.Parse(
        "M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1zm0 1.2A5.8 5.8 0 0 1 13.5 7H10a8.7 8.7 0 0 0-1.7-4.4c-.2-.3-.3-.4-.3-.4zm-2.8 0c0 .1-.2.2-.3.4A8.7 8.7 0 0 0 3.2 7h-2.1A5.8 5.8 0 0 1 5.2 2.2zM2.5 8h3.3c.1.9.4 1.8.8 2.6H3.3A5.8 5.8 0 0 1 2.5 8zm3.9 3.6c-.3.4-.6.8-.9 1.2a5.8 5.8 0 0 1-1.9-2.2h2.2c.2.4.4.7.6 1zm1.6 1.2c-.3 0-.6-.3-.9-.9A8.7 8.7 0 0 1 6.3 9h3.4c-.2 1.3-.7 2.4-1.2 3.1-.3.5-.6.7-.5.7z"));

    public static StreamGeometry CategoryGeometry => Category.Value;
    public static StreamGeometry DockerGeometry => Docker.Value;
    public static StreamGeometry NodeGeometry => NodeJs.Value;
    public static StreamGeometry NextGeometry => NextJs.Value;
    public static StreamGeometry ViteGeometry => Vite.Value;
    public static StreamGeometry GitBranchGeometry => GitBranch.Value;
    public static StreamGeometry PlayGeometry => Play.Value;
    public static StreamGeometry StopGeometry => Stop.Value;
    public static StreamGeometry RestartGeometry => Restart.Value;
    public static StreamGeometry TerminalGeometry => Terminal.Value;
    public static StreamGeometry CodeGeometry => Code.Value;
    public static StreamGeometry FolderGeometry => Folder.Value;
    public static StreamGeometry GlobeGeometry => Globe.Value;
}
