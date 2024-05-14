using Google.OrTools.Sat;

namespace SeaOfConquest.Solvers;

/// <summary>
///     Contains methods for solving hero assignments.
/// </summary>
public static class HeroSolver
{
    /// <summary>
    ///     Solves and outputs the hero assignments.
    /// </summary>
    public static void SolveHeroAssignments(CpModel model, Dictionary<string, IntVar> assignments)
    {
        var solver = new CpSolver();
        var status = solver.Solve(model);

        if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
        {
            Console.WriteLine("Solution Found:");
            var anyAssigned = false;

            // Dictionary to group assignments by ship for clearer output
            var assignmentsByShip = new Dictionary<string, List<string>>();

            foreach (var kvp in assignments)
            {
                if (solver.Value(kvp.Value) == 1)
                {
                    var parts = kvp.Key.Split('_');
                    if (parts.Length != 3)
                    {
                        Console.WriteLine("Error: Assignment variable name format is incorrect.");
                        continue;
                    }

                    var hero = parts[0];
                    var ship = parts[1];
                    var position = parts[2];

                    // Prepare the assignment description
                    var assignmentDescription = $"Hero {hero} is assigned to {position}.";

                    // Group by ship
                    if (!assignmentsByShip.ContainsKey(ship))
                    {
                        assignmentsByShip[ship] = new List<string>();
                    }

                    assignmentsByShip[ship].Add(assignmentDescription);

                    anyAssigned = true;
                }
            }

            // Print grouped by ships
            if (anyAssigned)
            {
                foreach (var ship in assignmentsByShip.Keys)
                {
                    Console.WriteLine($"\nAssignments for ship {ship}:");
                    foreach (var desc in assignmentsByShip[ship])
                    {
                        Console.WriteLine(desc);
                    }
                }
            }
            else
            {
                Console.WriteLine("No variables were assigned true.");
            }
        }
        else
        {
            Console.WriteLine("No solution found.");
            // Additional debug information
            Console.WriteLine("Model status: " + status);
        }
    }
}
