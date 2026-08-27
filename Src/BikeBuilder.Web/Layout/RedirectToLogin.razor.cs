namespace BikeBuilder.Web.Layout;

public partial class RedirectToLogin(NavigationManager _navigation)
{
  // NavigateToLogin captures the current URL as the post-login returnUrl automatically.
  protected override void OnInitialized() => _navigation.NavigateToLogin("authentication/login");
}
