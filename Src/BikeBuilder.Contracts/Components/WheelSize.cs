using System.Text.Json;
using System.Text.Json.Serialization;

namespace BikeBuilder.Contracts.Components;

/// <summary>Wheel/rim/tire diameter designation; the industry's closed set, not a number.</summary>
[JsonConverter(typeof(WheelSizeJsonConverter))]
public readonly record struct WheelSize
{
  public static readonly WheelSize TwentySix = new("26");
  public static readonly WheelSize TwentySevenFive = new("27.5");
  public static readonly WheelSize TwentyNine = new("29");
  public static readonly WheelSize[] All = [TwentySix, TwentySevenFive, TwentyNine];

  public string Value { get; }

  public WheelSize(string value)
  {
    if (value is not ("26" or "27.5" or "29"))
      throw new ArgumentOutOfRangeException(nameof(value), value, "Wheel size must be 26, 27.5, or 29.");

    Value = value;
  }

  public override string ToString() => Value;
}

public sealed class WheelSizeJsonConverter : JsonConverter<WheelSize>
{
  public override WheelSize Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    var value = reader.GetString();
    try
    {
      return new WheelSize(value!);
    }
    catch (ArgumentOutOfRangeException ex)
    {
      throw new JsonException(ex.Message, ex);
    }
  }

  public override void Write(Utf8JsonWriter writer, WheelSize value, JsonSerializerOptions options) =>
      writer.WriteStringValue(value.Value);
}
