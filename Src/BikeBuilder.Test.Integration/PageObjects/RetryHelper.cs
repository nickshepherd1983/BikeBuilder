namespace BikeBuilder.Test.Integration.PageObjects;

static class RetryHelper
{
  /// <summary>
  /// Retries <paramref name="action"/> up to <paramref name="maxAttempts"/> times, as a defense
  /// against minor UI timing flakiness (dialog animations, etc.). <paramref name="action"/>
  /// should leave the UI in a state a subsequent attempt can start cleanly from (e.g. closing
  /// any dialog it opened) since it may run more than once.
  /// </summary>
  public static async Task RunAsync(Func<Task> action, int maxAttempts = 2)
  {
    Exception? lastError = null;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
      try
      {
        await action();
        return;
      }
      catch (Exception ex)
      {
        lastError = ex;

        if (attempt < maxAttempts)
          await Task.Delay(TimeSpan.FromSeconds(1));
      }
    }

    throw new InvalidOperationException($"Action failed after {maxAttempts} attempts.", lastError);
  }
}
