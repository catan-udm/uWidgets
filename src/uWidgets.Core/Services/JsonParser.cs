using System.Diagnostics;
using System.Text.Json;
using uWidgets.Core.Interfaces;

namespace uWidgets.Core.Services;

/// <inheritdoc />
public class JsonParser<T>(Func<string> filePathResolver, Func<T>? defaultFactory = null) : IDataProvider<T>
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };
    private readonly object syncLock = new();
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
        lock (syncLock)
        {
            if (data != null) return data;

            var filePath = filePathResolver();

            if (TryDeserializeFile(filePath, out var loaded))
                return data = loaded!;

            if (File.Exists(filePath))
            {
                try
                {
                    File.Copy(filePath, $"{filePath}.corrupt", overwrite: true);
                }
                catch
                {
                    // Best effort preservation of unreadable primary file
                }
            }

            var backupFile = $"{filePath}.bak";
            if (TryDeserializeFile(backupFile, out var backupLoaded))
                return data = backupLoaded!;

            if (defaultFactory != null)
                return data = defaultFactory();

            throw new FormatException($"Can't deserialize {typeof(T).Name}");
        }
    }

    /// <inheritdoc />
    public virtual void Save(T newData)
    {
        T? oldDataSnapshot;
        lock (syncLock)
        {
            oldDataSnapshot = data;
        }

        DataChanging?.Invoke(this, oldDataSnapshot, newData);
        var json = JsonSerializer.Serialize(newData, WriteOptions);

        T? previousData;
        lock (syncLock)
        {
            var filePath = filePathResolver();
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var tempFile = $"{filePath}.tmp";
            var backupFile = $"{filePath}.bak";
            const int maxRetries = 3;

            try
            {
                for (var attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                        using (var writer = new StreamWriter(fs))
                        {
                            writer.Write(json);
                        }

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
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch
                {
                    // Ignore temp cleanup failures
                }
            }

            previousData = data;
            data = newData;
        }

        DataChanged?.Invoke(this, previousData, newData);
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