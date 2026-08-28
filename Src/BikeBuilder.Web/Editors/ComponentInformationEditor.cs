namespace BikeBuilder.Web.Editors;

// DynamicComponent passes parameters as IDictionary<string, object>, so the dialog talks to
// editors through the non-generic Model parameter; the generic class gives sub-forms a typed
// view of the same instance. Editors mutate the instance in place - the dialog owns it, so
// no change callback is needed.
public abstract class ComponentInformationEditorBase : ComponentBase
{
  [Parameter] public ComponentInformation Model { get; set; } = null!;
}

public abstract class ComponentInformationEditor<T> : ComponentInformationEditorBase where T : ComponentInformation
{
  protected T Value => (T)Model;
}
