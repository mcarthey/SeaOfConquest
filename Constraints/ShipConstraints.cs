using Google.OrTools.Sat;

namespace SeaOfConquest.Constraints;

/// <summary>
///     Contains constraints related to ship assignments.
/// </summary>
public static class ShipConstraints
{
    /// <summary>
    ///     Adds constraints to ensure each position on each ship is filled by exactly one hero.
    /// </summary>
    public static void AddShipAndHeroConstraints(CpModel model, Dictionary<string, IntVar> assignments, Dictionary<string, BoolVar> shipActiveVars, List<string> ships, List<string> positions)
    {
        // Constraint to limit the number of active ships to 4
        model.Add(LinearExpr.Sum(shipActiveVars.Values) == 4);

        foreach (var ship in ships)
        {
            foreach (var position in positions)
            {
                var positionAssignments = new List<IntVar>();

                // Collect all assignment variables for this specific position on this ship
                foreach (var hero in assignments.Keys.Where(a => a.Contains($"{ship}_{position}")))
                {
                    positionAssignments.Add(assignments[hero]);
                }

                // Add a constraint that each position on each ship is assigned to exactly one hero
                model.Add(LinearExpr.Sum(positionAssignments) == shipActiveVars[ship]); // This line ensures the position is filled only if the ship is active

                // Ensuring the position is not filled more than once even if the ship is active
                if (positionAssignments.Any())
                {
                    model.Add(LinearExpr.Sum(positionAssignments) <= 1);
                }
            }

            // Ensure the ship, if active, has exactly 3 positions filled
            var allShipAssignments = assignments.Where(a => a.Key.Contains(ship)).Select(a => a.Value).ToList();
            model.Add(LinearExpr.Sum(allShipAssignments) == 3 * shipActiveVars[ship]);
        }
    }
}
