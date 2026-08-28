namespace BikeBuilder.DataSeeder;

public sealed record ComponentSeed(string Name, string Category, string Brand, string Series, decimal Cost, Manufacturer Manufacturer);

/// <summary>
/// Generates real-sounding catalog entries by crossing actual product lines with the
/// variants (ratios, lengths, sizes, colors) those parts genuinely ship in.
/// </summary>
public static class ComponentCatalog
{
  sealed record Line(string Brand, string Series);

  static readonly Line[] _drivetrainLines =
  [
    new("Shimano", "XTR M9100"), new("Shimano", "XT M8100"), new("Shimano", "SLX M7100"),
    new("Shimano", "Deore M6100"), new("Shimano", "Deore M5100"), new("Shimano", "CUES U8000"),
    new("Shimano", "GRX RX820"), new("Shimano", "Dura-Ace R9250"), new("Shimano", "Ultegra R8150"),
    new("Shimano", "105 R7150"),
    new("SRAM", "XX1 Eagle"), new("SRAM", "X01 Eagle"), new("SRAM", "GX Eagle"), new("SRAM", "NX Eagle"),
    new("SRAM", "SX Eagle"), new("SRAM", "XX Eagle Transmission"), new("SRAM", "X0 Eagle Transmission"),
    new("SRAM", "GX Eagle Transmission"), new("SRAM", "Red AXS"), new("SRAM", "Force AXS"),
    new("SRAM", "Rival AXS"), new("SRAM", "Apex Eagle")
  ];

  static readonly Line[] _brakeLines =
  [
    new("Shimano", "XTR M9120"), new("Shimano", "XT M8120"), new("Shimano", "SLX M7120"),
    new("Shimano", "Deore M6120"), new("Shimano", "Saint M820"), new("Shimano", "Zee M640"),
    new("SRAM", "Code RSC"), new("SRAM", "Code R"), new("SRAM", "Level Ultimate"),
    new("SRAM", "Level TLM"), new("SRAM", "Guide RE"), new("SRAM", "Maven Ultimate"),
    new("Hope", "Tech 4 V4"), new("Hope", "Tech 4 E4"), new("Hope", "Tech 3 X2"), new("Hope", "RX4+")
  ];

  static readonly Line[] _hubLines =
  [
    new("Hope", "Pro 5"), new("Hope", "Pro 4"), new("Hope", "Fortus 30SC"), new("Hope", "Fortus 26"),
    new("Hope", "Union TC"),
    new("DT Swiss", "240"), new("DT Swiss", "350"), new("DT Swiss", "XM 1700"), new("DT Swiss", "EX 511"),
    new("DT Swiss", "HX 531"),
    new("Race Face", "Turbine R"), new("Race Face", "Aeffect R"),
    new("Industry Nine", "Hydra"), new("Industry Nine", "1/1"), new("Chris King", "Boost")
  ];

  static readonly Line[] _forkLines =
  [
    new("RockShox", "Pike Ultimate"), new("RockShox", "Lyrik Ultimate"), new("RockShox", "Zeb Select+"),
    new("RockShox", "SID SL Ultimate"), new("RockShox", "Recon Silver"), new("RockShox", "Domain"),
    new("Fox", "32 Step-Cast Factory"), new("Fox", "34 Performance"), new("Fox", "36 Factory"),
    new("Fox", "38 Factory"), new("Fox", "40 Performance")
  ];

  static readonly Line[] _shockLines =
  [
    new("RockShox", "Super Deluxe Ultimate"), new("RockShox", "Deluxe Select+"), new("RockShox", "Vivid Air"),
    new("Fox", "Float X2 Factory"), new("Fox", "DHX2"), new("Fox", "Float SL")
  ];

  static readonly Line[] _tireModels =
  [
    new("Maxxis", "Minion DHF"), new("Maxxis", "Minion DHR II"), new("Maxxis", "Assegai"),
    new("Maxxis", "Dissector"), new("Maxxis", "Rekon"), new("Maxxis", "Ardent"), new("Maxxis", "Ikon"),
    new("Maxxis", "High Roller II"),
    new("Continental", "Kryptotal Fr"), new("Continental", "Kryptotal Re"), new("Continental", "Argotal"),
    new("Continental", "Xynotal"),
    new("Schwalbe", "Magic Mary"), new("Schwalbe", "Big Betty"), new("Schwalbe", "Nobby Nic"),
    new("Schwalbe", "Hans Dampf"),
    new("WTB", "Vigilante"), new("WTB", "Trail Boss"), new("WTB", "Judge"),
    new("Michelin", "Wild Enduro"), new("Michelin", "DH22")
  ];

  static readonly Line[] _barLines =
  [
    new("Race Face", "Next R 35"), new("Race Face", "Turbine R 35"), new("Renthal", "Fatbar 35"),
    new("Renthal", "Fatbar Lite"), new("Spank", "Oozy Trail"), new("Spank", "Spike 800"),
    new("OneUp", "Carbon Bar"), new("Deity", "Skywire"), new("Deity", "Highside"),
    new("Chromag", "Fubars OSX"), new("PNW", "Range")
  ];

  static readonly Line[] _stemLines =
  [
    new("Race Face", "Turbine R"), new("Renthal", "Apex 35"), new("Hope", "AM/Freeride"),
    new("Deity", "Copperhead"), new("PNW", "Loam"), new("Chromag", "Ranger V2"), new("Thomson", "Elite X4")
  ];

  static readonly Line[] _gripLines =
  [
    new("Ergon", "GE1 Evo"), new("Ergon", "GA3"), new("ODI", "Elite Pro"), new("ODI", "Ruffian"),
    new("Deity", "Knuckleduster"), new("Race Face", "Half Nelson"), new("PNW", "Loam Grip"),
    new("Chromag", "Format")
  ];

  static readonly Line[] _dropperLines =
  [
    new("OneUp", "Dropper V3"), new("RockShox", "Reverb AXS"), new("Fox", "Transfer Factory"),
    new("PNW", "Loam Dropper"), new("Bike Yoke", "Revive"), new("Crankbrothers", "Highline 7"),
    new("Thomson", "Elite Covert")
  ];

  static readonly Line[] _saddleLines =
  [
    new("WTB", "Volt"), new("WTB", "Silverado"), new("Ergon", "SM10 Enduro"), new("Fizik", "Terra Aidon"),
    new("Brooks", "Cambium C17"), new("Chromag", "Trailmaster"), new("Deity", "Speedtrap"),
    new("SDG", "Bel-Air V3")
  ];

  static readonly Line[] _pedalLines =
  [
    new("Crankbrothers", "Mallet E"), new("Crankbrothers", "Mallet DH"), new("Crankbrothers", "Stamp 7"),
    new("Crankbrothers", "Stamp 1"), new("Crankbrothers", "Candy 7"),
    new("Shimano", "XT PD-M8120"), new("Shimano", "XTR PD-M9120"), new("Shimano", "Saint PD-M828"),
    new("Hope", "F22"), new("Hope", "Union GC"),
    new("OneUp", "Composite"), new("OneUp", "Aluminum"),
    new("Deity", "TMAC"), new("Deity", "Deftrap"),
    new("Race Face", "Chester"), new("Race Face", "Atlas"), new("Wolf Tooth", "Waveform")
  ];

  static readonly Line[] _headsetLines =
  [
    new("Chris King", "NoThreadSet"), new("Chris King", "DropSet 3"), new("Cane Creek", "110"),
    new("Cane Creek", "40"), new("Cane Creek", "Hellbender 70"), new("Hope", "Pick N Mix"),
    new("FSA", "Orbit")
  ];

  static readonly Line[] _rimLines =
  [
    new("Stan's NoTubes", "Flow MK4"), new("Stan's NoTubes", "Flow EX3"), new("Stan's NoTubes", "Arch MK4"),
    new("WTB", "KOM i30"), new("Race Face", "ARC 30"), new("DT Swiss", "XM 481")
  ];

  static readonly Line[] _frameLines =
  [
    new("Santa Cruz", "Hightower CC"), new("Nukeproof", "Mega 290"), new("Cotic", "RocketMAX"),
    new("Privateer", "161"), new("Raaw", "Madonna V3"), new("Hope", "HB.916")
  ];

  static readonly string[] _cassetteRatios = ["10-45T", "10-51T", "10-52T"];
  static readonly string[] _crankLengths = ["165mm", "170mm", "175mm"];
  static readonly string[] _chainringTeeth = ["30T", "32T", "34T", "36T"];
  static readonly string[] _bottomBrackets = ["BSA 73mm", "PressFit 92"];
  static readonly string[] _rotorSizes = ["160mm", "180mm", "200mm", "220mm"];
  static readonly string[] _rotorMounts = ["Centerlock", "6-Bolt"];
  static readonly string[] _padCompounds = ["Metallic", "Resin"];
  static readonly string[] _hubDrillings = ["28h", "32h"];
  static readonly string[] _wheelSizes = ["27.5\"", "29\""];
  static readonly string[] _forkTravels = ["140mm", "160mm"];
  static readonly string[] _shockSizes = ["185x55mm", "210x50mm", "230x60mm"];
  static readonly string[] _tireSizes = ["27.5 x 2.4\"", "29 x 2.4\"", "29 x 2.5\""];
  static readonly string[] _tireCasings = ["Trail Casing", "Gravity Casing"];
  static readonly string[] _barWidths = ["760mm", "780mm", "800mm"];
  static readonly string[] _stemLengths = ["32mm", "40mm", "50mm", "60mm"];
  static readonly string[] _gripColors = ["Black", "Red", "Blue", "Orange"];
  static readonly string[] _dropperTravels = ["125mm", "150mm", "175mm", "200mm"];
  static readonly string[] _saddleWidths = ["135mm", "142mm"];
  static readonly string[] _pedalColors = ["Black", "Orange"];
  static readonly string[] _headsetColors = ["Black", "Silver", "Red"];
  static readonly string[] _frameSizes = ["Small", "Medium", "Large", "X-Large"];
  static readonly string[] _colorways = ["Stealth Black", "Raw Silver", "Team Orange", "Deep Blue", "Acid Green"];

  static readonly Dictionary<string, string> _categoryBlurbs = new()
  {
    ["Rear Derailleur"] = "Precise, clutch-equipped shifting even over rough ground",
    ["Cassette"] = "Wide-range gearing with crisp, consistent shifts",
    ["Crankset"] = "Stiff, dependable cranks that put every watt into the trail",
    ["Chain"] = "Durable plated chain with smooth-running rollers",
    ["Shifter"] = "Light, positive lever action with adjustable reach",
    ["Chainring"] = "Narrow-wide tooth profile keeps the chain planted",
    ["Bottom Bracket"] = "Sealed-bearing bottom bracket that shrugs off winter grit",
    ["Disc Brake"] = "Powerful, easy-to-modulate stopping in all conditions",
    ["Brake Rotor"] = "Consistent braking surface with excellent heat management",
    ["Brake Pads"] = "Fade-resistant pads with quiet, progressive bite",
    ["Front Hub"] = "Fast-engaging, easy-to-service hub internals",
    ["Rear Hub"] = "Fast-engaging, easy-to-service hub internals",
    ["Wheelset"] = "Tubeless-ready wheels built for daily abuse",
    ["Suspension Fork"] = "Supple small-bump feel with supportive mid-stroke",
    ["Rear Shock"] = "Coil-like traction with easy air-spring tuning",
    ["Tire"] = "Predictable cornering grip and strong braking traction",
    ["Handlebar"] = "Comfortable sweep with a damped, confident feel",
    ["Stem"] = "Stiff CNC-machined stem with a secure four-bolt clamp",
    ["Grips"] = "Tacky, vibration-damping compound for all-day comfort",
    ["Dropper Post"] = "Smooth, reliable drop with minimal service needs",
    ["Saddle"] = "All-day comfort with a pressure-relief channel",
    ["Pedals"] = "Grippy, low-profile platform with sealed bearings",
    ["Headset"] = "Buttery-smooth sealed bearings that last for seasons",
    ["Rim"] = "Impact-resistant profile with easy tubeless setup",
    ["Frame"] = "Modern geometry with room for a big bottle"
  };

  public static string Describe(ComponentSeed seed) =>
      $"{_categoryBlurbs[seed.Category]}. Part of the {seed.Brand} {seed.Series} range.";

  public static List<ComponentSeed> Generate(Random random, int minimum)
  {
    var seeds = new List<ComponentSeed>();

    void Add(Line line, string name, string category, decimal minCost, decimal maxCost) =>
        seeds.Add(new ComponentSeed(name, category, line.Brand, line.Series,
            RandomCost(random, minCost, maxCost), ToManufacturer(line.Brand)));

    foreach (var line in _drivetrainLines)
    {
      Add(line, $"{line.Brand} {line.Series} Rear Derailleur", "Rear Derailleur", 90, 560);
      Add(line, $"{line.Brand} {line.Series} Chain", "Chain", 25, 130);
      Add(line, $"{line.Brand} {line.Series} Shifter", "Shifter", 45, 260);
      foreach (var ratio in _cassetteRatios)
        Add(line, $"{line.Brand} {line.Series} Cassette {ratio}", "Cassette", 90, 480);
      foreach (var length in _crankLengths)
        Add(line, $"{line.Brand} {line.Series} Crankset {length}", "Crankset", 140, 650);
      foreach (var teeth in _chainringTeeth)
        Add(line, $"{line.Brand} {line.Series} Chainring {teeth}", "Chainring", 35, 120);
      foreach (var shell in _bottomBrackets)
        Add(line, $"{line.Brand} {line.Series} Bottom Bracket {shell}", "Bottom Bracket", 30, 110);
    }

    foreach (var line in _brakeLines)
    {
      Add(line, $"{line.Brand} {line.Series} Disc Brake Front", "Disc Brake", 110, 320);
      Add(line, $"{line.Brand} {line.Series} Disc Brake Rear", "Disc Brake", 110, 320);
      foreach (var size in _rotorSizes)
        foreach (var mount in _rotorMounts)
          Add(line, $"{line.Brand} {line.Series} Rotor {size} {mount}", "Brake Rotor", 30, 95);
      foreach (var compound in _padCompounds)
        Add(line, $"{line.Brand} {line.Series} Brake Pads {compound}", "Brake Pads", 15, 45);
    }

    foreach (var line in _hubLines)
    {
      foreach (var drilling in _hubDrillings)
      {
        Add(line, $"{line.Brand} {line.Series} Front Hub {drilling}", "Front Hub", 90, 320);
        Add(line, $"{line.Brand} {line.Series} Rear Hub {drilling}", "Rear Hub", 160, 620);
      }
      foreach (var size in _wheelSizes)
        Add(line, $"{line.Brand} {line.Series} Wheelset {size}", "Wheelset", 450, 2400);
    }

    foreach (var line in _forkLines)
      foreach (var size in _wheelSizes)
        foreach (var travel in _forkTravels)
          Add(line, $"{line.Brand} {line.Series} Fork {size} {travel}", "Suspension Fork", 380, 1250);

    foreach (var line in _shockLines)
      foreach (var size in _shockSizes)
        Add(line, $"{line.Brand} {line.Series} Rear Shock {size}", "Rear Shock", 320, 850);

    foreach (var model in _tireModels)
    {
      foreach (var size in _tireSizes)
        Add(model, $"{model.Brand} {model.Series} Tire {size}", "Tire", 55, 110);
      foreach (var casing in _tireCasings)
        Add(model, $"{model.Brand} {model.Series} Tire 29 x 2.4\" {casing}", "Tire", 60, 120);
    }

    foreach (var line in _barLines)
      foreach (var width in _barWidths)
        Add(line, $"{line.Brand} {line.Series} Handlebar {width}", "Handlebar", 45, 200);

    foreach (var line in _stemLines)
      foreach (var length in _stemLengths)
        Add(line, $"{line.Brand} {line.Series} Stem {length}", "Stem", 45, 180);

    foreach (var line in _gripLines)
      foreach (var color in _gripColors)
        Add(line, $"{line.Brand} {line.Series} Grips {color}", "Grips", 12, 40);

    foreach (var line in _dropperLines)
      foreach (var travel in _dropperTravels)
        Add(line, $"{line.Brand} {line.Series} Dropper Post {travel}", "Dropper Post", 180, 750);

    foreach (var line in _saddleLines)
      foreach (var width in _saddleWidths)
        Add(line, $"{line.Brand} {line.Series} Saddle {width}", "Saddle", 55, 220);

    foreach (var line in _pedalLines)
      foreach (var color in _pedalColors)
        Add(line, $"{line.Brand} {line.Series} Pedals {color}", "Pedals", 40, 200);

    foreach (var line in _headsetLines)
      foreach (var color in _headsetColors)
        Add(line, $"{line.Brand} {line.Series} Headset {color}", "Headset", 40, 190);

    foreach (var line in _rimLines)
      foreach (var size in _wheelSizes)
        Add(line, $"{line.Brand} {line.Series} Rim {size}", "Rim", 90, 650);

    foreach (var line in _frameLines)
      foreach (var size in _frameSizes)
        Add(line, $"{line.Brand} {line.Series} Frame {size}", "Frame", 1600, 4200);

    // Top up to the requested minimum with limited-run colorways of cockpit parts -
    // deterministic, still real-sounding, and guaranteed to terminate.
    var colorwayBase = seeds.Where(s => s.Category is "Handlebar" or "Stem" or "Dropper Post" or "Saddle").ToList();
    foreach (var color in _colorways)
    {
      if (seeds.Count >= minimum)
        break;

      foreach (var baseSeed in colorwayBase)
      {
        if (seeds.Count >= minimum)
          break;

        seeds.Add(baseSeed with { Name = $"{baseSeed.Name} - {color}", Cost = RandomCost(random, baseSeed.Cost, baseSeed.Cost + 30) });
      }
    }

    return seeds;
  }

  static Manufacturer ToManufacturer(string brand) => brand switch
  {
    "Shimano" => Manufacturer.Shimano,
    "SRAM" => Manufacturer.Sram,
    "Hope" => Manufacturer.Hope,
    _ => Manufacturer.Other
  };

  static decimal RandomCost(Random random, decimal min, decimal max) =>
      Math.Floor(min + (decimal)random.NextDouble() * (max - min)) + 0.99m;
}
