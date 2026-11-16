using System.Text.Json;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;
using uWidgets.Core.Services;
using Xunit;

namespace uWidgets.Tests;

public class CoreStorageAndLayoutTests : IDisposable
{
    private readonly string tempDir;

    public CoreStorageAndLayoutTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"uWidgetsTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
        catch { }
    }

    private sealed class TestPayload
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    private sealed class InMemoryLayoutProvider(List<WidgetLayout> initial) : ILayoutProvider
    {
        private List<WidgetLayout> items = initial.ToList();
        public int SaveCount { get; private set; }

        public event DataChangedEvent<List<WidgetLayout>>? DataChanging;
        public event DataChangedEvent<List<WidgetLayout>>? DataChanged;

        public List<WidgetLayout> Get() => items.ToList();

        public void Save(List<WidgetLayout> data)
        {
            var old = items;
            var next = data.ToList();
            DataChanging?.Invoke(this, old, next);
            items = next;
            SaveCount++;
            DataChanged?.Invoke(this, old, next);
        }
    }

    [Fact]
    public void JsonParser_CorruptPrimary_PreservesCorruptCopyAndFallsBackToBackup()
    {
        var primaryPath = Path.Combine(tempDir, "settings.json");
        var backupPath = $"{primaryPath}.bak";
        var corruptPath = $"{primaryPath}.corrupt";

        File.WriteAllText(backupPath, JsonSerializer.Serialize(new TestPayload { Name = "from-backup", Count = 42 }));
        File.WriteAllText(primaryPath, "{ invalid-json !!!");

        var parser = new JsonParser<TestPayload>(primaryPath, () => new TestPayload { Name = "factory", Count = -1 });
        var result = parser.Get();

        Assert.Equal("from-backup", result.Name);
        Assert.Equal(42, result.Count);
        Assert.True(File.Exists(corruptPath));
        Assert.Equal("{ invalid-json !!!", File.ReadAllText(corruptPath));
    }

    [Fact]
    public async Task JsonParser_Save_CreatesBackupCleansTempAndHandlesConcurrency()
    {
        var primaryPath = Path.Combine(tempDir, "concurrent.json");
        var parser = new JsonParser<TestPayload>(primaryPath, () => new TestPayload { Name = "init", Count = 0 });

        parser.Save(new TestPayload { Name = "v1", Count = 1 });
        parser.Save(new TestPayload { Name = "v2", Count = 2 });

        Assert.True(File.Exists(primaryPath));
        Assert.True(File.Exists($"{primaryPath}.bak"));
        Assert.False(File.Exists($"{primaryPath}.tmp"));

        var tasks = Enumerable.Range(0, 16).Select(i => Task.Run(() =>
        {
            parser.Save(new TestPayload { Name = $"worker-{i}", Count = i });
            var read = parser.Get();
            Assert.NotNull(read);
        })).ToArray();

        await Task.WhenAll(tasks);

        var reloaded = new JsonParser<TestPayload>(primaryPath).Get();
        Assert.StartsWith("worker-", reloaded.Name);
        Assert.False(File.Exists($"{primaryPath}.tmp"));
    }

    [Fact]
    public void WidgetLayoutProvider_StaleIdSave_DoesNotOverwriteOtherWidgetAtSameSlot()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var staleId = Guid.NewGuid();

        var existingA = new WidgetLayout(idA, "Weather", "Forecast", 1, 1, 2, 2, null);
        var existingB = new WidgetLayout(idB, "Weather", "Forecast", 1, 1, 2, 2, null);
        var layoutProvider = new InMemoryLayoutProvider([existingA, existingB]);

        var staleLayout = new WidgetLayout(staleId, "Weather", "Forecast", 1, 1, 2, 2, null);
        var widgetProvider = new WidgetLayoutProvider(layoutProvider, staleLayout);

        widgetProvider.Save(staleLayout with { Width = 4 });

        Assert.Equal(0, layoutProvider.SaveCount);
        var current = layoutProvider.Get().ToList();
        Assert.Equal(2, current[0].Width);
        Assert.Equal(2, current[1].Width);
    }

    [Fact]
    public void WidgetLayoutProvider_NoOpSave_ShortCircuitsEventsAndWrite()
    {
        var id = Guid.NewGuid();
        var initial = new WidgetLayout(id, "Notes", "Note", 0, 0, 2, 2, null);
        var layoutProvider = new InMemoryLayoutProvider([initial]);
        var widgetProvider = new WidgetLayoutProvider(layoutProvider, initial);

        int changedCount = 0;
        widgetProvider.DataChanged += (_, _, _) => changedCount++;

        widgetProvider.Save(initial with { });

        Assert.Equal(0, changedCount);
        Assert.Equal(0, layoutProvider.SaveCount);
    }

    [Fact]
    public void WidgetLayoutProvider_Remove_FiresEventsAndBlocksSubsequentSaves()
    {
        var id = Guid.NewGuid();
        var initial = new WidgetLayout(id, "Clock", "Digital", 2, 3, 2, 2, null);
        var layoutProvider = new InMemoryLayoutProvider([initial]);
        var widgetProvider = new WidgetLayoutProvider(layoutProvider, initial);

        int changedCount = 0;
        widgetProvider.DataChanged += (_, _, _) => changedCount++;

        widgetProvider.Remove();

        Assert.Equal(1, changedCount);
        Assert.Empty(layoutProvider.Get());

        widgetProvider.Save(initial with { X = 5 });
        Assert.Equal(1, changedCount);
        Assert.Empty(layoutProvider.Get());
    }
}
