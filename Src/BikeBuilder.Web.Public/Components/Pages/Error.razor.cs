using System.Diagnostics;

namespace BikeBuilder.Web.Public.Components.Pages;

public partial class Error
{
  [CascadingParameter] HttpContext? HttpContext { get; set; }

  string? RequestId { get; set; }
  bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

  protected override void OnInitialized() =>
      RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}
