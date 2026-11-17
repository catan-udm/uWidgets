using Avalonia;
using Xunit;

namespace uWidgets.Tests;

/// <summary>
/// Initializes Avalonia once per test run for test collections that instantiate Avalonia controls or geometries.
/// </summary>
public sealed class AvaloniaUiFixture
{
    private sealed class TestAvaloniaApp : Application;

    public AvaloniaUiFixture()
    {
        if (Application.Current is null)
        {
            AppBuilder.Configure<TestAvaloniaApp>()
                .UsePlatformDetect()
                .SetupWithoutStarting();
        }
    }
}

/// <summary>
/// Serializes tests that rely on global Avalonia platform state.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class AvaloniaUiCollection : ICollectionFixture<AvaloniaUiFixture>
{
    public const string Name = "AvaloniaUi";
}
