using System.Globalization;
using System.Text.Json.Serialization;

namespace BikeBuilder.Contracts.Types;

[JsonConverter(typeof(Int32ValueJsonConverter<StrokeMm>))]
public readonly record struct StrokeMm : IInt32Value<StrokeMm>
{
  public const int Min = 0;
  public const int Max = 150;

  public int Value { get; }

  public StrokeMm(int value)
  {
    if (value is < Min or > Max)
      throw new ArgumentOutOfRangeException(nameof(value), value, $"Stroke must be between {Min} and {Max}mm.");

    Value = value;
  }

  public static StrokeMm From(int value) => new(value);

  public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
