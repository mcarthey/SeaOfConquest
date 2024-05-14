namespace SeaOfConquest.Models;

/// <summary>
///     Represents a hero with preferred positions, ships, and trinkets.
/// </summary>
public class Hero
{
    public string Name { get; set; }
    public List<string> PreferredPositions { get; set; }
    public List<string> PreferredPartners { get; set; }
    public List<string> PreferredShips { get; set; }
    public List<string> PreferredTrinkets { get; set; }

    public Hero(string name)
    {
        Name = name;
        PreferredPositions = new List<string>();
        PreferredPartners = new List<string>();
        PreferredShips = new List<string>();
        PreferredTrinkets = new List<string>();
    }
}
