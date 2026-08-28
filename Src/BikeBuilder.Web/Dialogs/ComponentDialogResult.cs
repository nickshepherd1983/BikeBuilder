namespace BikeBuilder.Web.Dialogs;

public sealed record ComponentDialogResult(
    string Name,
    string Cost,
    string Description,
    string Sku,
    Manufacturer Manufacturer,
    ComponentInformation? Information);
