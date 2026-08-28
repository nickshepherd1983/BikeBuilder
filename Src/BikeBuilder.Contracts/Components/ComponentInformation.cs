using System.Text.Json.Serialization;

namespace BikeBuilder.Contracts.Components;

public abstract class ComponentInformation
{
  [JsonIgnore]
  public abstract string DisplayName { get; }

  // A method rather than a property so STJ never serializes it - the JSON shape stays
  // exactly the persisted/wire contract.
  public abstract IEnumerable<KeyValuePair<string, string>> GetDisplayValues();
}
