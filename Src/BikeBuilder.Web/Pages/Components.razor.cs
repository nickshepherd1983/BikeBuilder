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

  List<ComponentMessage>? _components;
  bool _isUploading;

  protected override async Task OnInitializedAsync()
  {
    await LoadComponents();
  }

  async Task LoadComponents()
  {
    var response = await _componentClient.ListComponentsAsync(new ListComponentsRequest());
    _components = response.Components.ToList();
  }

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

    var (name, cost, description, sku, manufacturer) = ((string, string, string, string, Manufacturer))result.Data!;

    try
    {
      await _componentClient.CreateComponentAsync(new CreateComponentRequest
      {
        Name = name,
        Cost = cost,
        Description = description,
        Sku = sku,
        Manufacturer = manufacturer
      });
      _snackbar.Add("Component added.", Severity.Success);
      await LoadComponents();
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
      { x => x.Manufacturer, component.Manufacturer }
    };

    var dialog = await _dialogService.ShowAsync<ComponentDialog>("Edit Component", parameters, ComponentDialogOptions);
    var result = await dialog.Result;

    if (result is null || result.Canceled)
      return;

    var (name, cost, description, sku, manufacturer) = ((string, string, string, string, Manufacturer))result.Data!;

    try
    {
      await _componentClient.UpdateComponentAsync(new UpdateComponentRequest
      {
        Id = component.Id,
        Name = name,
        Cost = cost,
        Description = description,
        Sku = sku,
        Manufacturer = manufacturer
      });
      _snackbar.Add("Component updated.", Severity.Success);
      await LoadComponents();
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
      await LoadComponents();
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
      await LoadComponents();
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
    await LoadComponents();
  }
}
