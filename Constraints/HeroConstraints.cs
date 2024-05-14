using Google.OrTools.Sat;
using SeaOfConquest.Models;

namespace SeaOfConquest.Constraints;

/// <summary>
///     Contains constraints related to hero assignments.
/// </summary>
public static class HeroConstraints
{
    /// <summary>
    ///     Ensures each hero is assigned to at most one position on at most one ship.
    /// </summary>
    public static void AddHeroAssignmentConstraints(CpModel model, Dictionary<string, IntVar> assignments, List<Hero> heroes, List<string> ships, List<string> positions)
    {
        foreach (var hero in heroes)
        {
            var heroAssignments = new List<IntVar>();
            foreach (var ship in ships)
            {
                foreach (var position in positions)
                {
                    var varName = $"{hero.Name}_{ship}_{position}";
                    if (assignments.ContainsKey(varName)) // Ensure the variable exists before adding it to the list
                    {
                        heroAssignments.Add(assignments[varName]);
                    }
                }
            }

            // Add a constraint that the total number of positions across all ships for each hero is at most 1
            model.Add(LinearExpr.Sum(heroAssignments) <= 1);
        }
    }

    /// <summary>
    ///     Calculates the preference score for a hero assignment.
    /// </summary>
    public static int CalculatePreferenceScore(Hero hero, string ship, string position)
    {
        // Adjust the logic here as necessary
        if (hero.PreferredShips.Contains(ship) && hero.PreferredPositions.Contains(position))
        {
            return 10; // Assign a significant positive score for preferred combinations
        }

        return 1; // Ensure there is a baseline positive score for all valid assignments
    }
}
