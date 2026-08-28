using System.Text.Json.Serialization;

namespace BikeBuilder.Contracts.Components;

public abstract class ComponentInformation
{
  [JsonIgnore]
  public abstract string DisplayName { get; }

  // A method rather than a property so STJ never serializes it - the JSON shape stays
  // exactly the persisted/wire contract.
  public abstract IEnumerable<KeyValuePair<string, string>> GetDisplayValues();

  // How many of this kind of component a build sensibly carries (2 tires, 1 fork, ...).
  // A recommendation, not a rule - the UI warns politely but never blocks. Null = no
  // opinion. A method for the same serialization reason as above.
  public virtual int? GetRecommendedMaxPerBuild() => null;
}
