using Google.OrTools.Sat;
using SeaOfConquest.Models;

namespace SeaOfConquest.Constraints;

/// <summary>
///     Contains constraints related to trinket assignments.
/// </summary>
public static class TrinketConstraints
{
    /// <summary>
    ///     Adds constraints to ensure trinket assignments are valid.
    /// </summary>
    public static void AddTrinketConstraints(CpModel model, Dictionary<string, IntVar> trinketAssignments, List<Hero> heroes, List<Trinket> trinkets)
    {
        // Ensure the number of trinkets assigned does not exceed the available inventory
        foreach (var trinket in trinkets)
        {
            var allAssignmentsForTrinket = trinketAssignments
                .Where(kv => kv.Key.EndsWith($"_{trinket.Name}"))
                .Select(kv => kv.Value)
                .ToList();

            if (allAssignmentsForTrinket.Any())
            {
                model.Add(LinearExpr.Sum(allAssignmentsForTrinket) <= trinket.Amount);
            }
        }

        // Ensure each hero can only be assigned one trinket
        foreach (var hero in heroes)
        {
            var heroTrinketAssignments = trinketAssignments
                .Where(kv => kv.Key.StartsWith(hero.Name))
                .Select(kv => kv.Value)
                .ToList();

            if (heroTrinketAssignments.Any())
            {
                model.Add(LinearExpr.Sum(heroTrinketAssignments) <= 1);
            }
        }
    }
}
