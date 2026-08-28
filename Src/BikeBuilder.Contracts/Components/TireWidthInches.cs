using System.Globalization;
using System.Text.Json.Serialization;

namespace BikeBuilder.Contracts.Components;

[JsonConverter(typeof(DoubleValueJsonConverter<TireWidthInches>))]
public readonly record struct TireWidthInches : IDoubleValue<TireWidthInches>
{
  public const double Min = 0.5;
  public const double Max = 5.0;

  public static readonly TireWidthInches[] Common = [new(1.95), new(2.0), new(2.25), new(2.4), new(2.5), new(2.6)];

  public double Value { get; }

  public TireWidthInches(double value)
  {
    if (value is < Min or > Max)
      throw new ArgumentOutOfRangeException(nameof(value), value, $"Tire width must be between {Min} and {Max} inches.");

    Value = value;
  }

  public static TireWidthInches From(double value) => new(value);

  public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
