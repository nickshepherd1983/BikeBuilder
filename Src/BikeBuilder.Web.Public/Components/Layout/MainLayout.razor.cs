namespace BikeBuilder.Web.Public.Components.Layout;

public partial class MainLayout
{
  readonly MudTheme _theme = new()
  {
    PaletteLight = new PaletteLight
    {
      Primary = "#594AE2",
      Secondary = "#2EC4B6",
      AppbarBackground = "#594AE2"
    },
    PaletteDark = new PaletteDark
    {
      Primary = "#8B7FF0",
      Secondary = "#2EC4B6"
    }
  };
}
