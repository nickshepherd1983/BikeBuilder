using System.Globalization;
using System.Text.Json.Serialization;

namespace BikeBuilder.Contracts.Components;

[JsonConverter(typeof(Int32ValueJsonConverter<RiseMm>))]
public readonly record struct RiseMm : IInt32Value<RiseMm>
{
  public const int Min = 0;
  public const int Max = 150;

  public int Value { get; }

  public RiseMm(int value)
  {
    if (value is < Min or > Max)
      throw new ArgumentOutOfRangeException(nameof(value), value, $"Rise must be between {Min} and {Max}mm.");

    Value = value;
  }

  public static RiseMm From(int value) => new(value);

  public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
