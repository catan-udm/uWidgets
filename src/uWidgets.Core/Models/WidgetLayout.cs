using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace uWidgets.Core.Models;

/// <summary>
/// Layout of a single widget, stored in <c>layout.json</c>.
/// </summary>
public record WidgetLayout
{
    /// <summary>
    /// Initializes a widget layout record with an explicit instance identifier.
    /// </summary>
    [JsonConstructor]
    public WidgetLayout(
        Guid Id,
        string Type,
        string SubType,
        int X,
        int Y,
        int Width,
        int Height,
        JsonElement? Settings)
    {
        this.Id = Id;
        this.Type = Type;
        this.SubType = SubType;
        this.X = X;
        this.Y = Y;
        this.Width = Width;
        this.Height = Height;
        this.Settings = Settings;
    }

    /// <summary>
    /// Initializes a new widget layout record with a freshly generated <see cref="Guid"/>.
    /// </summary>
    public WidgetLayout(
        string Type,
        string SubType,
        int X,
        int Y,
        int Width,
        int Height,
        JsonElement? Settings)
        : this(Guid.NewGuid(), Type, SubType, X, Y, Width, Height, Settings)
    {
    }

    /// <summary>Unique widget instance identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Assembly name of the widget.</summary>
    public string Type { get; init; }
    /// <summary>UserControl name of the widget.</summary>
    public string SubType { get; init; }
    /// <summary>X coordinate of the widget's top-left corner.</summary>
    public int X { get; init; }
    /// <summary>Y coordinate of the widget's top-left corner.</summary>
    public int Y { get; init; }
    /// <summary>Width of the widget.</summary>
    public int Width { get; init; }
    /// <summary>Height of the widget.</summary>
    public int Height { get; init; }
    /// <summary>Widget's model as <see cref="JsonElement"/>.</summary>
    public JsonElement? Settings { get; init; }

    /// <summary>
    /// Get the widget's model.
    /// </summary>
    /// <typeparam name="T">Type of the widget's model.</typeparam>
    /// <returns>Widget's model.</returns>
    public T? GetModel<T>() => (T?) GetModel(typeof(T));
    
    /// <summary>
    /// Get the widget's model.
    /// </summary>
    /// <param name="type">Type of the widget's model.</param>
    /// <returns>Widget's model.</returns>
    public object? GetModel(Type? type)
    {
        if (type == null || !Settings.HasValue) 
            return null;

        try
        {
            return Settings.Value.Deserialize(type);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public virtual bool Equals(WidgetLayout? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id
            && Type == other.Type
            && SubType == other.SubType
            && X == other.X
            && Y == other.Y
            && Width == other.Width
            && Height == other.Height
            && JsonElementsEqual(Settings, other.Settings);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Type, SubType, X, Y, Width, Height);
    }

    private static bool JsonElementsEqual(JsonElement? a, JsonElement? b)
    {
        if (!a.HasValue && !b.HasValue) return true;
        if (!a.HasValue || !b.HasValue) return false;
        return a.Value.ValueKind == b.Value.ValueKind && a.Value.GetRawText() == b.Value.GetRawText();
    }
}
