using System.Text.Json.Serialization;

namespace BikeBuilder.Contracts.Components;

public abstract class ComponentInformation
{
  [JsonIgnore]
  public abstract string DisplayName { get; }
}
