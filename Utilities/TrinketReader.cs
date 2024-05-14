using SeaOfConquest.Models;

namespace SeaOfConquest.Utilities;

/// <summary>
///     Reads trinket data from a CSV file.
/// </summary>
public static class TrinketReader
{
    /// <summary>
    ///     Reads trinkets from a CSV file.
    /// </summary>
    /// <param name="filePath">The path to the CSV file.</param>
    /// <returns>A list of trinkets.</returns>
    public static List<Trinket> ReadTrinkets(string filePath)
    {
        var trinkets = new List<Trinket>();
        using (var reader = new StreamReader(filePath))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                if (values.Length < 2) // Ensure there are enough columns
                {
                    Console.WriteLine("Warning: Line format incorrect, skipping line.");
                    continue;
                }

                var trinket = new Trinket(values[0], Convert.ToInt32(values[1]));
                trinkets.Add(trinket);
            }
        }

        return trinkets;
    }
}
