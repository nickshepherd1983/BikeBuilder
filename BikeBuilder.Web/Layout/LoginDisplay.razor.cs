namespace BikeBuilder.Web.Layout;

public partial class LoginDisplay(NavigationManager _navigation)
{
  void BeginLogIn() => _navigation.NavigateToLogin("authentication/login");

  void BeginLogOut() => _navigation.NavigateToLogout("authentication/logout");
}
