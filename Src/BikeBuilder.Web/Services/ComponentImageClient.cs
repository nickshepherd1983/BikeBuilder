using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;

namespace BikeBuilder.Web.Services;

public class ComponentImageClient(HttpClient http)
{
  public Task<HttpResponseMessage> UploadAsync(int componentId, IBrowserFile file, long maxFileSize, CancellationToken ct = default)
  {
    var content = new MultipartFormDataContent();
    var streamContent = new StreamContent(file.OpenReadStream(maxFileSize, ct));
    streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
    content.Add(streamContent, "file", file.Name);
    return http.PostAsync($"/api/components/{componentId}/image", content, ct);
  }

  public Task<HttpResponseMessage> DeleteAsync(int componentId, CancellationToken ct = default) =>
      http.DeleteAsync($"/api/components/{componentId}/image", ct);

  public string GetImageUrl(int componentId, long imageVersion) =>
      $"{http.BaseAddress}api/components/{componentId}/image?v={imageVersion}";
}
