using Google.OrTools.Sat;

namespace SeaOfConquest.Solvers;

/// <summary>
///     Contains methods for solving trinket assignments.
/// </summary>
public static class TrinketSolver
{
    /// <summary>
    ///     Solves and outputs the trinket assignments.
    /// </summary>
    public static void SolveTrinketAssignments(CpModel model, Dictionary<string, IntVar> trinketAssignments)
    {
        var solver = new CpSolver();
        var status = solver.Solve(model);

        if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
        {
            Console.WriteLine("Trinket Assignments Found:");
            var anyAssigned = false;

            var trinketAssignmentsByHero = new Dictionary<string, List<string>>();

            foreach (var kvp in trinketAssignments)
            {
                if (solver.Value(kvp.Value) == 1)
                {
                    var parts = kvp.Key.Split('_');
                    if (parts.Length != 2)
                    {
                        Console.WriteLine("Error: Trinket assignment variable name format is incorrect.");
                        continue;
                    }

                    var hero = parts[0];
                    var trinket = parts[1];

                    // Prepare the trinket assignment description
                    var trinketDescription = $"Hero {hero} is assigned trinket {trinket}.";

                    if (!trinketAssignmentsByHero.ContainsKey(hero))
                    {
                        trinketAssignmentsByHero[hero] = new List<string>();
                    }

                    trinketAssignmentsByHero[hero].Add(trinketDescription);

                    anyAssigned = true;
                }
            }

            // Print trinket assignments by hero
            if (anyAssigned)
            {
                foreach (var hero in trinketAssignmentsByHero.Keys)
                {
                    Console.WriteLine($"\nTrinket assignments for hero {hero}:");
                    foreach (var desc in trinketAssignmentsByHero[hero])
                    {
                        Console.WriteLine(desc);
                    }
                }
            }
            else
            {
                Console.WriteLine("No trinket variables were assigned true.");
            }
        }
        else
        {
            Console.WriteLine("No trinket solution found.");
            // Additional debug information
            Console.WriteLine("Model status: " + status);
        }
    }
}
