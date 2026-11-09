namespace uWidgets.Core.Models.Settings;

/// <summary>
/// Application theme settings.
/// </summary>
/// <param name="DarkMode">
/// Should the application use the dark mode
/// <para><c>null</c> to use system settings</para>
/// </param>
/// <param name="AccentColor">
/// Accent color in HEX format
/// <para><c>null</c> to use system accent color</para>
/// </param>
/// <param name="OpacityLevel">
/// Widget's background opacity level
/// </param>
/// <param name="Monochrome">
/// Should the application use monochrome theme
/// </param>
/// <param name="UseNativeFrame">
/// Should the application use native window frame
/// </param>
/// <param name="FontFamily">
/// Font family to use
/// </param>
/// <param name="LiquidGlass">
/// Should the application use the liquid glass (glass warping) effect
/// </param>
/// <param name="RefThickness">
/// Refraction bevel thickness in pixels (MyDockFinder default: 50.0)
/// </param>
/// <param name="RefFactor">
/// Index of refraction for Snell's law meniscus bending (MyDockFinder default: 1.5)
/// </param>
/// <param name="RefDispersion">
/// Spectral chromatic dispersion strength (MyDockFinder default: 160.0)
/// </param>
/// <param name="ShapeRadius">
/// Corner radius ratio (MyDockFinder default: 0.85)
/// </param>
/// <param name="ShapeRoundness">
/// Superellipse corner roundness exponent (MyDockFinder default: 4.0)
/// </param>
public record Theme(
    bool? DarkMode, 
    string? AccentColor, 
    double OpacityLevel, 
    bool Monochrome, 
    bool UseNativeFrame, 
    string FontFamily,
    bool LiquidGlass = false,
    double RefThickness = 50.0,
    double RefFactor = 1.5,
    double RefDispersion = 160.0,
    double ShapeRadius = 0.85,
    double ShapeRoundness = 4.0);