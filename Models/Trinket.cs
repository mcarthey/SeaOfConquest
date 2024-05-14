namespace SeaOfConquest.Models;

/// <summary>
///     Represents a trinket with its name and available amount.
/// </summary>
public class Trinket
{
    public int Amount { get; set; }
    public string Name { get; set; }

    public Trinket(string name, int amount)
    {
        Name = name;
        Amount = amount;
    }
}
