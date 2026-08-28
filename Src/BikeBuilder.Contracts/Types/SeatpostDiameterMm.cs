using System.Globalization;
using System.Text.Json.Serialization;

namespace BikeBuilder.Contracts.Types;

[JsonConverter(typeof(DoubleValueJsonConverter<SeatpostDiameterMm>))]
public readonly record struct SeatpostDiameterMm : IDoubleValue<SeatpostDiameterMm>
{
  public const double Min = 20.0;
  public const double Max = 40.0;

  public static readonly SeatpostDiameterMm[] Common = [new(30.9), new(31.6), new(34.9)];

  public double Value { get; }

  public SeatpostDiameterMm(double value)
  {
    if (value is < Min or > Max)
      throw new ArgumentOutOfRangeException(nameof(value), value, $"Seatpost diameter must be between {Min} and {Max}mm.");

    Value = value;
  }

  public static SeatpostDiameterMm From(double value) => new(value);

  public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
