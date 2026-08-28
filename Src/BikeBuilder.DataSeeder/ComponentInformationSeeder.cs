using System.Globalization;
using System.Text.RegularExpressions;

namespace BikeBuilder.DataSeeder;

/// <summary>
/// Derives ComponentInformation from the specs ComponentCatalog already embeds in the seed
/// names (tire "29 x 2.4\"", fork "160mm", shock "210x50mm"), randomizing only what the
/// name doesn't carry. Categories without a matching subtype get null.
/// </summary>
public static class ComponentInformationSeeder
{
  static readonly Regex TireSpec = new("(26|27\\.5|29) x (\\d+(?:\\.\\d+)?)\"", RegexOptions.Compiled);
  static readonly Regex ShockSpec = new(@"(\d+)x(\d+)mm", RegexOptions.Compiled);
  static readonly Regex MillimetreSpec = new(@"(\d+)mm", RegexOptions.Compiled);

  public static ComponentInformation? Create(ComponentSeed seed, Random random) => seed.Category switch
  {
    "Tire" => CreateTire(seed),
    "Rim" => new RimComponentInformation { Size = seed.Name.Contains("27.5") ? "27.5" : "29" },
    "Handlebar" => new HandlebarComponentInformation
    {
      WidthMm = ParseMillimetres(seed.Name) ?? 780,
      RiseMm = random.NextDouble() < 0.5 ? 20 : 35
    },
    "Stem" => new StemComponentInformation { LengthMm = ParseMillimetres(seed.Name) ?? 50 },
    "Dropper Post" => new DropperPostComponentInformation
    {
      TravelMm = ParseMillimetres(seed.Name) ?? 150,
      DiameterMm = DropperPostComponentInformation.Diameters[random.Next(DropperPostComponentInformation.Diameters.Length)]
    },
    "Suspension Fork" => new ForkComponentInformation { TravelMm = ParseMillimetres(seed.Name) ?? 150 },
    "Rear Shock" => CreateShock(seed),
    _ => null
  };

  static TireComponentInformation? CreateTire(ComponentSeed seed)
  {
    var match = TireSpec.Match(seed.Name);
    if (!match.Success)
      return null;

    return new TireComponentInformation
    {
      Size = match.Groups[1].Value,
      WidthInches = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
    };
  }

  static ShockComponentInformation? CreateShock(ComponentSeed seed)
  {
    // The first number in e.g. "210x50mm" is really eye-to-eye length, but it's close
    // enough for seed data.
    var match = ShockSpec.Match(seed.Name);
    if (!match.Success)
      return null;

    return new ShockComponentInformation
    {
      TravelMm = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
      StrokeMm = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
    };
  }

  static int? ParseMillimetres(string name)
  {
    var match = MillimetreSpec.Match(name);
    return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : null;
  }
}
