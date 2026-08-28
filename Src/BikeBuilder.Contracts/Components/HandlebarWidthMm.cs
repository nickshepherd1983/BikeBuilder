using System.Globalization;
using System.Text.Json.Serialization;

namespace BikeBuilder.Contracts.Components;

[JsonConverter(typeof(Int32ValueJsonConverter<HandlebarWidthMm>))]
public readonly record struct HandlebarWidthMm : IInt32Value<HandlebarWidthMm>
{
  public const int Min = 400;
  public const int Max = 1000;

  public int Value { get; }

  public HandlebarWidthMm(int value)
  {
    if (value is < Min or > Max)
      throw new ArgumentOutOfRangeException(nameof(value), value, $"Handlebar width must be between {Min} and {Max}mm.");

    Value = value;
  }

  public static HandlebarWidthMm From(int value) => new(value);

  public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
