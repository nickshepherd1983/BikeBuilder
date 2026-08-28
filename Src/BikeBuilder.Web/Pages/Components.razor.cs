using Microsoft.AspNetCore.Components.Forms;

namespace BikeBuilder.Web.Pages;

public partial class Components(
    ComponentService.ComponentServiceClient _componentClient,
    ComponentImageClient _imageClient,
    IDialogService _dialogService,
    ISnackbar _snackbar)
{
  static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif"];

  // Roughly double the dialog's natural content width: stretch it to MaxWidth.Small (600px).
  static readonly DialogOptions ComponentDialogOptions = new() { MaxWidth = MaxWidth.Small, FullWidth = true };

  MudTable<ComponentMessage> _table = null!;
  bool _isUploading;

  async Task<TableData<ComponentMessage>> LoadComponentsAsync(TableState state, CancellationToken cancellationToken)
  {
    // MudTable pages are 0-based; the RPC is 1-based, with Limit acting as the page size.
    var response = await _componentClient.ListComponentsAsync(new ListComponentsRequest
    {
      Page = state.Page + 1,
      Limit = state.PageSize
    }, cancellationToken: cancellationToken);

    return new TableData<ComponentMessage> { Items = response.Components, TotalItems = response.TotalCount };
  }

  async Task ReloadComponents() => await _table.ReloadServerData();

  static string FormatCost(string cost) =>
      decimal.TryParse(cost, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
          ? value.ToString("C2")
          : cost;

  async Task AddComponent()
  {
    var parameters = new DialogParameters<ComponentDialog>
    {
      { x => x.Title, "Add Component" }
    };

    var dialog = await _dialogService.ShowAsync<ComponentDialog>("Add Component", parameters, ComponentDialogOptions);
    var result = await dialog.Result;

    if (result is null || result.Canceled)
      return;

    var component = (ComponentDialogResult)result.Data!;

    try
    {
      await _componentClient.CreateComponentAsync(new CreateComponentRequest
      {
        Name = component.Name,
        Cost = component.Cost,
        Description = component.Description,
        Sku = component.Sku,
        Manufacturer = component.Manufacturer,
        ComponentInformationJson = ComponentInformationSerializer.Serialize(component.Information)
      });
      _snackbar.Add("Component added.", Severity.Success);
      await ReloadComponents();
    }
    catch (RpcException ex)
    {
      _snackbar.Add(ex.Status.Detail, Severity.Error);
    }
  }

  async Task EditComponent(ComponentMessage component)
  {
    var parameters = new DialogParameters<ComponentDialog>
    {
      { x => x.Title, "Edit Component" },
      { x => x.Name, component.Name },
      { x => x.Cost, component.Cost },
      { x => x.Description, component.Description },
      { x => x.Sku, component.Sku },
      { x => x.Manufacturer, component.Manufacturer },
      { x => x.ComponentInformationJson, component.ComponentInformationJson }
    };

    var dialog = await _dialogService.ShowAsync<ComponentDialog>("Edit Component", parameters, ComponentDialogOptions);
    var result = await dialog.Result;

    if (result is null || result.Canceled)
      return;

    var edited = (ComponentDialogResult)result.Data!;

    try
    {
      await _componentClient.UpdateComponentAsync(new UpdateComponentRequest
      {
        Id = component.Id,
        Name = edited.Name,
        Cost = edited.Cost,
        Description = edited.Description,
        Sku = edited.Sku,
        Manufacturer = edited.Manufacturer,
        ComponentInformationJson = ComponentInformationSerializer.Serialize(edited.Information)
      });
      _snackbar.Add("Component updated.", Severity.Success);
      await ReloadComponents();
    }
    catch (RpcException ex)
    {
      _snackbar.Add(ex.Status.Detail, Severity.Error);
    }
  }

  async Task DeleteComponent(ComponentMessage component)
  {
    var confirmed = await _dialogService.ShowMessageBoxAsync(
        "Delete Component",
        $"Delete \"{component.Name}\"? This cannot be undone.",
        yesText: "Delete", cancelText: "Cancel");

    if (confirmed != true)
      return;

    try
    {
      await _componentClient.DeleteComponentAsync(new DeleteComponentRequest { Id = component.Id });
      _snackbar.Add("Component deleted.", Severity.Success);
      await ReloadComponents();
    }
    catch (RpcException ex)
    {
      _snackbar.Add(ex.Status.Detail, Severity.Error);
    }
  }

  async Task UploadImage(ComponentMessage component, IBrowserFile file)
  {
    var extension = Path.GetExtension(file.Name).ToLowerInvariant();
    if (!AllowedImageExtensions.Contains(extension))
    {
      _snackbar.Add("Only .jpg, .png, and .gif files are supported.", Severity.Error);
      return;
    }

    _isUploading = true;
    try
    {
      var response = await _imageClient.UploadAsync(component.Id, file, maxFileSize: 5_000_000);
      if (!response.IsSuccessStatusCode)
      {
        _snackbar.Add("Failed to upload image.", Severity.Error);
        return;
      }

      _snackbar.Add("Image uploaded.", Severity.Success);
      await ReloadComponents();
    }
    catch (IOException)
    {
      _snackbar.Add("File is too large (max 5 MB).", Severity.Error);
    }
    finally
    {
      _isUploading = false;
    }
  }

  async Task DeleteImage(ComponentMessage component)
  {
    var confirmed = await _dialogService.ShowMessageBoxAsync(
        "Delete Image",
        $"Delete the image for \"{component.Name}\"?",
        yesText: "Delete", cancelText: "Cancel");

    if (confirmed != true)
      return;

    var response = await _imageClient.DeleteAsync(component.Id);
    if (!response.IsSuccessStatusCode)
    {
      _snackbar.Add("Failed to delete image.", Severity.Error);
      return;
    }

    _snackbar.Add("Image deleted.", Severity.Success);
    await ReloadComponents();
  }
}
