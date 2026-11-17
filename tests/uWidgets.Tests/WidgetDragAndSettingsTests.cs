using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;
using uWidgets.Core.Models.Settings;
using uWidgets.Services;
using uWidgets.ViewModels;
using uWidgets.Views;
using Xunit;

namespace uWidgets.Tests;

/// <summary>
/// Regression tests for desktop grid drag snapping and fast Settings initialization.
/// </summary>
[Collection(AvaloniaUiCollection.Name)]
public class WidgetDragAndSettingsTests
{
    private const uint WmEnterSizeMove = 0x0231;
    private const uint WmExitSizeMove = 0x0232;

    private static void EnsureAvaloniaPlatform() { }

    private sealed class FakeAppSettingsProvider(AppSettings initial) : IAppSettingsProvider
    {
        private AppSettings current = initial;
        public event DataChangedEvent<AppSettings>? DataChanging;
        public event DataChangedEvent<AppSettings>? DataChanged;
        public AppSettings Get() => current;
        public void Save(AppSettings data)
        {
            var prev = current;
            DataChanging?.Invoke(this, prev, data);
            current = data;
            DataChanged?.Invoke(this, prev, data);
        }
    }

    private sealed class FakeWidgetLayoutProvider(WidgetLayout initial) : IWidgetLayoutProvider
    {
        private WidgetLayout current = initial;
        public int SaveCount { get; private set; }
        public event DataChangedEvent<WidgetLayout>? DataChanging;
        public event DataChangedEvent<WidgetLayout>? DataChanged;
        public WidgetLayout Get() => current;
        public void Save(WidgetLayout data)
        {
            var prev = current;
            DataChanging?.Invoke(this, prev, data);
            current = data;
            SaveCount++;
            DataChanged?.Invoke(this, prev, data);
        }
        public void Remove() { }
    }

    private sealed class FakeGridService : IGridService<Widget>
    {
        public int SnapPositionCalls { get; private set; }
        public PixelPoint SnappedPoint { get; set; } = new(16, 16);

        public void SetSize(Widget window, int columns, int rows) { }
        public void SnapSize(Widget window) { }
        public void SnapPosition(Widget window)
        {
            SnapPositionCalls++;
            window.Position = SnappedPoint;
        }
    }

    private sealed class MetadataOnlyAssemblyProvider(AssemblyInfo[] infos) : IAssemblyProvider
    {
        public ILookup<string, AssemblyInfo> GetAssemblyInfos(string directoryPath) =>
            infos.ToLookup(i => i.AssemblyName);

        public Assembly LoadAssembly(string name) =>
            throw new InvalidOperationException($"Settings initialization must not eagerly load plugin '{name}'.");

        public void UnloadAssembly(string name) { }
        public object Activate(Type type, params object[] args) => throw new NotSupportedException();
        public ResourceManager? GetLocaleResourceManager(Assembly assembly) => null;
    }

    private sealed class DummyLayoutProvider : ILayoutProvider
    {
        public event DataChangedEvent<List<WidgetLayout>>? DataChanging;
        public event DataChangedEvent<List<WidgetLayout>>? DataChanged;
        public List<WidgetLayout> Get() => [];
        public void Save(List<WidgetLayout> data)
        {
            DataChanging?.Invoke(this, [], data);
            DataChanged?.Invoke(this, [], data);
        }
    }

    private sealed class DummyWidgetFactory : IWidgetFactory<Window, UserControl>
    {
        public IEnumerable<Window> Create() => [];
        public Window Add(WidgetLayout widgetLayout) => throw new NotSupportedException();
        public UserControl CreateControl(Type type) => throw new NotSupportedException();
    }

    private static Widget CreateTestWidget(
        FakeAppSettingsProvider settingsProvider,
        FakeWidgetLayoutProvider layoutProvider,
        FakeGridService gridService)
    {
        return new Widget(
            settingsProvider,
            layoutProvider,
            gridService,
            () => new UserControl(),
            () => throw new NotSupportedException());
    }

    [Fact]
    public void BeginDragFromPointer_DoesNotSnapPrematurely_SnapsOnExitSizeMove()
    {
        EnsureAvaloniaPlatform();

        var defaults = AppSettings.CreateDefault();
        var settings = defaults with { Layout = defaults.Layout with { SnapPosition = true, LockPosition = false } };
        var settingsProvider = new FakeAppSettingsProvider(settings);
        var layoutProvider = new FakeWidgetLayoutProvider(new WidgetLayout(Guid.NewGuid(), "Clock", "AnalogI", 16, 16, 160, 160, null));
        var grid = new FakeGridService { SnappedPoint = new PixelPoint(192, 104) };
        var widget = CreateTestWidget(settingsProvider, layoutProvider, grid);
        widget.Position = new PixelPoint(16, 16);

        var dragStarted = false;
        widget.BeginDragFromPointer(isLeftButtonPressed: true, () => dragStarted = true);

        // BeginMoveDrag is asynchronous on Win32; pressing must NOT snap before dragging ends.
        Assert.True(dragStarted);
        Assert.True(widget.IsDragging);
        Assert.Equal(0, grid.SnapPositionCalls);
        Assert.Equal(0, layoutProvider.SaveCount);

        widget.ProcessSizeMoveMessage(WmEnterSizeMove);
        Assert.True(widget.IsInSizeMove);

        // Move to unaligned drop location while modal loop is active
        widget.Position = new PixelPoint(187, 111);
        widget.CompletePointerRelease();
        Assert.Equal(0, grid.SnapPositionCalls);

        // Exiting the OS modal move loop must snap and persist once
        widget.ProcessSizeMoveMessage(WmExitSizeMove);
        Assert.False(widget.IsInSizeMove);
        Assert.False(widget.IsDragging);
        Assert.Equal(1, grid.SnapPositionCalls);
        Assert.Equal(new PixelPoint(192, 104), widget.Position);
        Assert.Equal(1, layoutProvider.SaveCount);
        Assert.Equal(192, layoutProvider.Get().X);
        Assert.Equal(104, layoutProvider.Get().Y);

        // Trailing synthetic LBUTTONUP after SC_MOVE must not double-snap
        widget.CompletePointerRelease();
        Assert.Equal(1, grid.SnapPositionCalls);
        Assert.Equal(1, layoutProvider.SaveCount);
    }

    [Fact]
    public void CompletePointerRelease_QuickClickWithoutModalLoop_SnapsOnce()
    {
        EnsureAvaloniaPlatform();

        var settingsProvider = new FakeAppSettingsProvider(AppSettings.CreateDefault());
        var layoutProvider = new FakeWidgetLayoutProvider(new WidgetLayout(Guid.NewGuid(), "Clock", "AnalogI", 40, 50, 160, 160, null));
        var grid = new FakeGridService { SnappedPoint = new PixelPoint(16, 16) };
        var widget = CreateTestWidget(settingsProvider, layoutProvider, grid);
        widget.Position = new PixelPoint(40, 50);

        widget.BeginDragFromPointer(isLeftButtonPressed: true, () => { });
        Assert.Equal(0, grid.SnapPositionCalls);

        widget.CompletePointerRelease();
        Assert.False(widget.IsDragging);
        Assert.Equal(1, grid.SnapPositionCalls);
        Assert.Equal(new PixelPoint(16, 16), widget.Position);
        Assert.Equal(1, layoutProvider.SaveCount);
    }

    [Fact]
    public void SettingsViewModel_InitializesWithoutEagerPluginLoading_AndCulturesLoadFast()
    {
        EnsureAvaloniaPlatform();

        var sw = Stopwatch.StartNew();
        var cultures = GeneralViewModel.GetAvailableCultures().Select(c => c.Name).ToList();
        sw.Stop();

        Assert.Contains("en", cultures);
        Assert.Contains("de", cultures);
        Assert.Contains("ru", cultures);
        Assert.True(sw.ElapsedMilliseconds < 500, $"Expected fast culture lookup, took {sw.ElapsedMilliseconds} ms.");

        var infos = new[]
        {
            new AssemblyInfo("Fake.dll", "FakeWidget", "Fake Widget", "Author", new Version(1, 0), "")
            {
                Widgets = [new WidgetDescriptor("Demo", "Demo Title", "Demo Sub")],
                SearchKeywords = "FakeWidget Demo Demo Title Demo Sub"
            }
        };

        using var sp = new ServiceCollection()
            .AddSingleton<IAppSettingsProvider>(new FakeAppSettingsProvider(AppSettings.CreateDefault()))
            .BuildServiceProvider();

        using var settingsVm = new SettingsViewModel(
            sp.GetRequiredService<IAppSettingsProvider>(),
            new MetadataOnlyAssemblyProvider(infos),
            new DummyLayoutProvider(),
            new DummyWidgetFactory(),
            sp);

        var galleryItem = Assert.Single(settingsVm.AllItems, i => i.AssemblyInfo != null);
        Assert.Contains("Demo Title", galleryItem.SearchKeywords, StringComparison.Ordinal);
        Assert.Same(settingsVm.MenuItems[0], settingsVm.SelectedMenuItem);
    }
}
