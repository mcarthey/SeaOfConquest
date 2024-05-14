using SeaOfConquest.Models;

namespace SeaOfConquest.Utilities;

/// <summary>
///     Reads hero data from a CSV file.
/// </summary>
public static class HeroReader
{
    /// <summary>
    ///     Reads heroes from a CSV file.
    /// </summary>
    /// <param name="filePath">The path to the CSV file.</param>
    /// <returns>A list of heroes.</returns>
    public static List<Hero> ReadHeroes(string filePath)
    {
        var heroes = new List<Hero>();
        using (var reader = new StreamReader(filePath))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                if (values.Length < 5) // Ensure there are enough columns
                {
                    Console.WriteLine("Warning: Line format incorrect, skipping line.");
                    continue;
                }

                var hero = new Hero(values[0])
                {
                    PreferredPositions = values[1].Split('|').ToList(),
                    PreferredPartners = values[2].Split('|').ToList(),
                    PreferredShips = values[3].Split('|').ToList(),
                    PreferredTrinkets = values[4].Split('|').ToList()
                };
                heroes.Add(hero);
            }
        }

        return heroes;
    }
}
