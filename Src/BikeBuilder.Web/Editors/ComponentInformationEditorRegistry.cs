namespace BikeBuilder.Web.Editors;

// Reflection is safe under the default WASM partial trim (this assembly and Contracts are
// rooted whole); if full trimming is ever enabled the editors and subtypes need
// DynamicDependency roots or an STJ source-gen context.
public static class ComponentInformationEditorRegistry
{
  public static IReadOnlyList<(Type Type, string DisplayName)> KnownTypes { get; } =
      [.. ComponentInformationSerializer.KnownTypes
          .Select(t => (Type: t, ((ComponentInformation)Activator.CreateInstance(t)!).DisplayName))];

  static readonly Dictionary<Type, Type> _editors = BuildEditorMap();

  public static string DisplayNameFor(Type type) =>
      KnownTypes.First(entry => entry.Type == type).DisplayName;

  public static Type EditorFor(Type informationType) =>
      _editors.TryGetValue(informationType, out var editor) ? editor : typeof(DefaultComponentInformationEditor);

  static Dictionary<Type, Type> BuildEditorMap()
  {
    var map = new Dictionary<Type, Type>();

    foreach (var editorType in typeof(ComponentInformationEditorRegistry).Assembly.GetTypes())
    {
      if (editorType.IsAbstract || !editorType.IsAssignableTo(typeof(ComponentInformationEditorBase)))
        continue;

      var informationType = GetInformationType(editorType);
      if (informationType is not null)
        map[informationType] = editorType;
    }

    return map;
  }

  // Walks the base chain to the closed ComponentInformationEditor<T>; editors deriving only
  // from the non-generic base (the default editor) have no T and are skipped.
  static Type? GetInformationType(Type editorType)
  {
    for (var type = editorType.BaseType; type is not null; type = type.BaseType)
      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ComponentInformationEditor<>))
        return type.GetGenericArguments()[0];

    return null;
  }
}
