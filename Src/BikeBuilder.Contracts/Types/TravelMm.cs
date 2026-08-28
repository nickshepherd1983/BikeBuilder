using System.Globalization;
using System.Text.Json.Serialization;

namespace BikeBuilder.Contracts.Types;

/// <summary>Suspension or seatpost travel; shared by forks, shocks, and dropper posts.</summary>
[JsonConverter(typeof(Int32ValueJsonConverter<TravelMm>))]
public readonly record struct TravelMm : IInt32Value<TravelMm>
{
  public const int Min = 0;
  public const int Max = 300;

  public int Value { get; }

  public TravelMm(int value)
  {
    if (value is < Min or > Max)
      throw new ArgumentOutOfRangeException(nameof(value), value, $"Travel must be between {Min} and {Max}mm.");

    Value = value;
  }

  public static TravelMm From(int value) => new(value);

  public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
