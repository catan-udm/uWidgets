namespace uWidgets.Core.Models;

/// <summary>
/// Metadata describing a widget sub-type exposed by a plugin assembly.
/// </summary>
/// <param name="SubType">The widget view class name.</param>
/// <param name="Title">The localized display title.</param>
/// <param name="Subtitle">The localized subtitle.</param>
public record WidgetDescriptor(string SubType, string Title, string Subtitle);

/// <summary>
/// Represents the information of an assembly.
/// </summary>
/// <param name="FilePath">The path to the file that contains the assembly.</param>
/// <param name="AssemblyName">The assembly name.</param>
/// <param name="DisplayName">Display name of the assembly.</param>
/// <param name="Author">Author of the assembly.</param>
/// <param name="Version">Version of the assembly.</param>
/// <param name="IconData">Icon data of the assembly.</param>
public record AssemblyInfo(string FilePath, string AssemblyName, string DisplayName, string Author, Version Version, string IconData)
{
    /// <summary>
    /// Precomputed widget descriptors from this plugin assembly.
    /// </summary>
    public IReadOnlyList<WidgetDescriptor> Widgets { get; init; } = [];

    /// <summary>
    /// Precomputed space-delimited search keywords for Settings filtering.
    /// </summary>
    public string SearchKeywords { get; init; } = string.Empty;
}