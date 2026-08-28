using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace BikeBuilder.Contracts.Components;

// Shared by the API's EF value converter, the gRPC service, and the Blazor client so all
// three agree on the wire/storage format. Subtypes are discovered by reflection, so adding
// one needs no registration here - but the discriminator is the CLR type name, meaning a
// rename of an existing subtype orphans previously persisted rows (that needs a data
// migration).
public static class ComponentInformationSerializer
{
  public static IReadOnlyList<Type> KnownTypes { get; } =
      [.. typeof(ComponentInformation).Assembly
          .GetTypes()
          .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(ComponentInformation)))
          .OrderBy(t => t.Name, StringComparer.Ordinal)];

  public static readonly JsonSerializerOptions Options = new()
  {
    TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { AddPolymorphism, HideDisplayName } }
  };

  public static string Serialize(ComponentInformation? information) =>
      information is null ? string.Empty : JsonSerializer.Serialize(information, Options);

  public static ComponentInformation? Deserialize(string? json) =>
      string.IsNullOrWhiteSpace(json)
          ? null
          : JsonSerializer.Deserialize<ComponentInformation>(json, Options);

  static void AddPolymorphism(JsonTypeInfo typeInfo)
  {
    if (typeInfo.Type != typeof(ComponentInformation))
      return;

    typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
    {
      TypeDiscriminatorPropertyName = "$type",
      UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization
    };

    foreach (var type in KnownTypes)
      typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(type, type.Name));
  }

  // [JsonIgnore] on the abstract DisplayName isn't honored for the derived overrides, so
  // strip the property here instead - it's UI metadata, not data.
  static void HideDisplayName(JsonTypeInfo typeInfo)
  {
    if (!typeInfo.Type.IsAssignableTo(typeof(ComponentInformation)))
      return;

    for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
      if (typeInfo.Properties[i].Name == nameof(ComponentInformation.DisplayName))
        typeInfo.Properties.RemoveAt(i);
  }
}
