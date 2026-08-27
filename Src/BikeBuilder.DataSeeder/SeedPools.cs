namespace BikeBuilder.DataSeeder;

public static class SeedPools
{
  static readonly string[] BuildThemes =
  [
    "Weekend", "Enduro", "XC", "Downcountry", "Bikepacking", "Steel", "Titanium", "Vintage",
    "Alpine", "Desert", "Coastal", "Midnight", "Rowdy", "Budget", "Superlight", "Race Day",
    "Backyard", "Winter", "Big Mountain", "Loam"
  ];

  static readonly string[] BuildRigs = ["Ripper", "Sled", "Whippet", "Mule", "Rocket"];

  // 20 themes x 5 rigs = 100 distinct build names ("Alpine Sled", "Midnight Whippet", ...).
  public static readonly string[] BuildNames =
      [.. BuildThemes.SelectMany(theme => BuildRigs.Select(rig => $"{theme} {rig}"))];

  public static readonly string[] BuildDescriptions =
  [
    "Built for long weekends in the hills with mates.",
    "Full send bike for lift-served laps and big hits.",
    "Every gram counted, built to hurt on the climbs.",
    "Short travel, big attitude - quick up, quicker down.",
    "Loaded-touring workhorse for multi-day routes.",
    "Timeless steel front triangle with modern parts.",
    "Built specifically for a summer trip to the Alps.",
    "Cheap, cheerful, and happy to be hosed down daily.",
    "The no-compromise downhill build I always wanted.",
    "Proof you don't need a fortune to have fun.",
    "Built around one goal: the local climbing KOM.",
    "Pop off every root and lip on the flow trails.",
    "Short, stout, and made for the backyard pump track.",
    "Fenders, tough tires, and zero care for the weather.",
    "Long-distance road comfort with a racy edge.",
    "Fast gravel rig for all-day mixed-surface rides.",
    "One bike to ride everything the region offers.",
    "Stripped-back race build, nothing that isn't needed.",
    "Sniffs out the deepest loam in the forest.",
    "Assembled entirely from the spares bin, rides great."
  ];

  public static readonly string[] RaterNames =
  [
    "Alex Mercer", "Sam Whitfield", "Jordan Blake", "Casey Nguyen", "Riley Thompson",
    "Morgan Ellis", "Jamie Sutton", "Taylor Brooks", "Drew Callahan", "Quinn Harper",
    "Avery Dawson", "Reese Molloy", "Hayden Frost", "Charlie Vance", "Rowan Pierce",
    "Skyler Nash", "Finley Marsh", "Emerson Cole", "Dakota Reeves", "Peyton Lang",
    "Kendall Ross", "Logan Tran", "Harper Quill", "Bailey Storm"
  ];

  public static readonly string[] Comments =
  [
    "Climbs like a dream and descends even better.",
    "Took it to the bike park and it never flinched.",
    "The brakes could use more bite, but overall solid.",
    "Perfect gearing for steep local climbs.",
    "A bit heavy on the climbs, plows downhill though.",
    "Six months in and everything still runs silent.",
    "Cornering grip is unreal in the dry.",
    "Set up tubeless first try, no drama.",
    "Great value build, nothing I'd swap.",
    "Front end feels planted at speed.",
    "Shifting stayed crisp even caked in mud.",
    "Would recommend to anyone getting into trail riding.",
    "The dropper is buttery smooth.",
    "Rattly on chunky descents until I re-torqued everything.",
    "Surprisingly capable for the price.",
    "My new favourite bike in the fleet.",
    "Handles switchbacks better than anything I've owned.",
    "Wish I'd built one of these years ago.",
    "Solid spec, though the saddle isn't for me.",
    "Rides quiet, pedals efficient, looks fantastic.",
    "Fast rolling but still hooks up in loose corners.",
    "Nailed the geometry on this one.",
    "Big hits disappear under the rear end.",
    "Needed a few rides to dial the suspension, now it's perfect.",
    "Confidence-inspiring on steep tech.",
    "Light enough to race, tough enough to crash.",
    "The cockpit setup is spot on out of the box.",
    "Chain slap is nonexistent, super quiet build.",
    "Braking power for days.",
    "Does everything well, masters nothing, love it."
  ];

  /// <summary>Realistic skew: mostly 4s and 5s, the odd stinker.</summary>
  public static int WeightedStars(Random random) => random.Next(100) switch
  {
    < 35 => 5,
    < 65 => 4,
    < 83 => 3,
    < 93 => 2,
    _ => 1
  };
}
