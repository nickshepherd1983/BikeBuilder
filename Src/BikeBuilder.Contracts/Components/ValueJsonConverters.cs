using System.Text.Json;
using System.Text.Json.Serialization;

namespace BikeBuilder.Contracts.Components;

// The measurement value objects serialize as their raw primitive so the stored/wire JSON
// keeps its pre-value-object shape ({"TravelMm":140}) - existing rows and clients keep
// working. Invariant violations surface as JsonException so the gRPC service's existing
// guard maps them to InvalidArgument.
public interface IInt32Value<TSelf> where TSelf : struct, IInt32Value<TSelf>
{
  int Value { get; }
  static abstract TSelf From(int value);
}

public interface IDoubleValue<TSelf> where TSelf : struct, IDoubleValue<TSelf>
{
  double Value { get; }
  static abstract TSelf From(double value);
}

public sealed class Int32ValueJsonConverter<T> : JsonConverter<T> where T : struct, IInt32Value<T>
{
  public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    var value = reader.GetInt32();
    try
    {
      return T.From(value);
    }
    catch (ArgumentOutOfRangeException ex)
    {
      throw new JsonException(ex.Message, ex);
    }
  }

  public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
      writer.WriteNumberValue(value.Value);
}

public sealed class DoubleValueJsonConverter<T> : JsonConverter<T> where T : struct, IDoubleValue<T>
{
  public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    var value = reader.GetDouble();
    try
    {
      return T.From(value);
    }
    catch (ArgumentOutOfRangeException ex)
    {
      throw new JsonException(ex.Message, ex);
    }
  }

  public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
      writer.WriteNumberValue(value.Value);
}
