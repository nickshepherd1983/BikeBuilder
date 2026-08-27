namespace BikeBuilder.Web.Layout;

public partial class MainLayout
{
  bool _drawerOpen;

  // Palette and type lifted from jensonusa.com: navy header #00263A, accent red #CF0030, Lato.
  readonly MudTheme _theme = new()
  {
    PaletteLight = new PaletteLight
    {
      Primary = "#CF0030",
      Secondary = "#00263A",
      AppbarBackground = "#00263A",
      AppbarText = "#FFFFFF",
      Background = "#FFFFFF",
      DrawerBackground = "#FFFFFF",
      TextPrimary = "#333333"
    },
    PaletteDark = new PaletteDark
    {
      Primary = "#FF4D6D",
      Secondary = "#7FB6CC"
    },
    Typography = new Typography
    {
      Default = new DefaultTypography
      {
        FontFamily = ["Lato", "Helvetica Neue", "Helvetica", "Arial", "sans-serif"]
      }
    }
  };

  void ToggleDrawer() => _drawerOpen = !_drawerOpen;
}
