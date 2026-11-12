using System.Diagnostics;
using System.Text.Json;
using uWidgets.Core.Interfaces;

namespace uWidgets.Core.Services;

/// <inheritdoc />
public class JsonParser<T>(Func<string> filePathResolver, Func<T>? defaultFactory = null) : IDataProvider<T>
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };
    private T? data;

    public JsonParser(string filePath, Func<T>? defaultFactory = null)
        : this(() => filePath, defaultFactory)
    {
    }

    /// <inheritdoc />
    public event DataChangedEvent<T>? DataChanging;
    /// <inheritdoc />
    public event DataChangedEvent<T>? DataChanged;

    /// <inheritdoc />
    public virtual T Get()
    {
        if (data != null) return data;

        var filePath = filePathResolver();

        if (TryDeserializeFile(filePath, out var loaded))
            return data = loaded!;

        var backupFile = $"{filePath}.bak";
        if (TryDeserializeFile(backupFile, out var backupLoaded))
            return data = backupLoaded!;

        if (defaultFactory != null)
            return data = defaultFactory();

        throw new FormatException($"Can't deserialize {typeof(T).Name}");
    }

    /// <inheritdoc />
    public virtual void Save(T newData)
    {
        var filePath = filePathResolver();
        var oldData = data;
        DataChanging?.Invoke(this, data, newData);
        var json = JsonSerializer.Serialize(data = newData, WriteOptions);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempFile = $"{filePath}.tmp";
        var backupFile = $"{filePath}.bak";

        using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        using (var writer = new StreamWriter(fs))
        {
            writer.Write(json);
        }

        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Replace(tempFile, filePath, backupFile, ignoreMetadataErrors: true);
                else
                    File.Move(tempFile, filePath, overwrite: true);

                break;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                Thread.Sleep(30 * attempt);
            }
        }

        DataChanged?.Invoke(this, oldData, newData);
    }

    private static bool TryDeserializeFile(string path, out T? result)
    {
        result = default;
        if (!File.Exists(path)) return false;

        try
        {
            var json = File.ReadAllText(path);
            result = JsonSerializer.Deserialize<T>(json);
            return result != null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[JsonParser] Failed reading {path}: {ex.Message}");
            return false;
        }
    }
}