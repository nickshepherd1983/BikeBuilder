using System.Globalization;
using System.Text.Json.Serialization;

namespace BikeBuilder.Contracts.Types;

[JsonConverter(typeof(Int32ValueJsonConverter<StemLengthMm>))]
public readonly record struct StemLengthMm : IInt32Value<StemLengthMm>
{
  public const int Min = 0;
  public const int Max = 200;

  public int Value { get; }

  public StemLengthMm(int value)
  {
    if (value is < Min or > Max)
      throw new ArgumentOutOfRangeException(nameof(value), value, $"Stem length must be between {Min} and {Max}mm.");

    Value = value;
  }

  public static StemLengthMm From(int value) => new(value);

  public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
