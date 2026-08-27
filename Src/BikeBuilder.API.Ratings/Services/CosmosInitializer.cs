namespace BikeBuilder.API.Ratings.Services;

public static class CosmosInitializer
{
  /// <summary>
  /// Provisions the database and container, retrying while the Cosmos emulator finishes
  /// starting - its data plane can refuse requests for a while after the container itself
  /// (and even its /ready probe) reports healthy.
  /// </summary>
  public static async Task EnsureCreatedAsync(CosmosClient client, string databaseId, string containerId,
      string partitionKeyPath, TimeSpan timeout)
  {
    var deadline = DateTime.UtcNow + timeout;
    Exception? lastError = null;

    while (DateTime.UtcNow < deadline)
    {
      try
      {
        var database = (await client.CreateDatabaseIfNotExistsAsync(databaseId)).Database;
        await database.CreateContainerIfNotExistsAsync(containerId, partitionKeyPath);
        return;
      }
      catch (Exception ex) when (ex is CosmosException or HttpRequestException)
      {
        lastError = ex;
        await Task.Delay(TimeSpan.FromSeconds(2));
      }
    }

    throw new InvalidOperationException($"Cosmos database '{databaseId}' was not provisionable within {timeout}.", lastError);
  }
}
