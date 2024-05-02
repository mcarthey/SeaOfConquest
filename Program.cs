using Google.OrTools.Sat;
using SeaOfConquest;

internal class Program
{
    public static void Main(string[] args)
    {
        var heroes = ReadHeroes("heroes.csv");
        var ships = GetDistinctShips(heroes);
        var positions = new List<string> { "Captain", "First Mate", "Gunner" };

        var model = new CpModel();
        var assignments = CreateAssignmentVariables(model, heroes, ships, positions);

        // Use this for simplified testing
        //var totalScore = InitializeTotalScoreSimplified(model, assignments);

        var totalScore = InitializeTotalScore(model, assignments, heroes, ships, positions);

        // Add constraints
        AddConstraints(model, heroes, ships, positions, assignments);

        model.Maximize(totalScore);

        SolveModel(model, assignments);
    }

    private static void AddConstraints(CpModel model, List<Hero> heroes, List<string> ships, List<string> positions, Dictionary<string, IntVar> assignments)
    {
        // Each hero is assigned to at most one position on at most one ship
        foreach (var hero in heroes)
        {
            var heroVars = assignments
                .Where(kv => kv.Key.StartsWith(hero.Name))
                .Select(kv => kv.Value).ToList();
            model.Add(LinearExpr.Sum(heroVars) <= 1);
        }

        // Each position on each ship must be filled by at most one hero
        foreach (var ship in ships)
        {
            foreach (var position in positions)
            {
                var positionVars = assignments
                    .Where(kv => kv.Key.Contains($"_{ship}_{position}"))
                    .Select(kv => kv.Value).ToList();
                model.Add(LinearExpr.Sum(positionVars) <= 1);
            }
        }
    }


    // Adjust this to give more significant weights to preferred assignments
    private static int CalculatePreferenceScore(Hero hero, string ship, string position)
    {
        // Verify and adjust the logic here as necessary
        if (hero.PreferredShips.Contains(ship) && hero.PreferredPositions.Contains(position))
            return 10;  // Assign a significant positive score for preferred combinations
        return 1;  // Ensure there is a baseline positive score for all valid assignments
    }



    private static Dictionary<string, IntVar> CreateAssignmentVariables(CpModel model, List<Hero> heroes, List<string> ships, List<string> positions)
    {
        var assignments = new Dictionary<string, IntVar>();
        foreach (var hero in heroes)
        {
            foreach (var ship in ships)
            {
                foreach (var position in positions)
                {
                    var varName = $"{hero.Name}_{ship}_{position}";
                    assignments[varName] = model.NewBoolVar(varName);
                }
            }
        }
        return assignments;
    }

    private static List<string> GetDistinctShips(List<Hero> heroes)
    {
        return heroes.SelectMany(hero => hero.PreferredShips).Distinct().ToList();
    }

    private static IntVar InitializeTotalScore(CpModel model, Dictionary<string, IntVar> assignments, List<Hero> heroes, List<string> ships, List<string> positions)
    {
        var totalScore = model.NewIntVar(0, 100000, "totalScore");
        foreach (var hero in heroes)
        {
            foreach (var ship in hero.PreferredShips)
            {
                foreach (var position in positions)
                {
                    if (hero.PreferredPositions.Contains(position))
                    {
                        var varName = $"{hero.Name}_{ship}_{position}";
                        var preferenceScore = CalculatePreferenceScore(hero, ship, position);

                        Console.WriteLine($"Adding score constraint for {varName} with score {preferenceScore}");

                        // Consider using soft constraints or adjusting how these are applied
                        model.Add(totalScore >= assignments[varName] * preferenceScore);
                    }
                }
            }
        }

        return totalScore;
    }

    // Simplified total score initialization for debugging
    private static IntVar InitializeTotalScoreSimplified(CpModel model, Dictionary<string, IntVar> assignments)
    {
        IntVar totalScore = model.NewIntVar(0, 1000000, "totalScore"); // Ensure the upper bound is high enough.
        model.Add(totalScore == LinearExpr.Sum(assignments.Values));
        return totalScore;
    }



    /// <summary>
    ///     Reads heroes from a CSV file.
    /// </summary>
    /// <param name="filePath">The path to the CSV file.</param>
    /// <returns>A list of heroes.</returns>
    private static List<Hero> ReadHeroes(string filePath)
    {
        var heroes = new List<Hero>();
        using (var reader = new StreamReader(filePath))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                var hero = new Hero(values[0])
                {
                    PreferredPositions = values[1].Split('|').ToList(),
                    PreferredShips = values[2].Split('|').ToList()
                };
                heroes.Add(hero);
            }
        }

        return heroes;
    }

    private static void SolveModel(CpModel model, Dictionary<string, IntVar> assignments)
    {
        CpSolver solver = new CpSolver();
        solver.StringParameters = "num_search_workers:4, log_search_progress:true";  // Enable logging
        CpSolverStatus status = solver.Solve(model);
        Console.WriteLine("Solver status: " + status);


        if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
        {
            Console.WriteLine("Solution Found:");
            bool anyAssigned = false;
            foreach (var kvp in assignments)
            {
                if (solver.Value(kvp.Value) == 1)
                {
                    Console.WriteLine($"{kvp.Key} assigned.");
                    anyAssigned = true;
                }
            }
            if (!anyAssigned)
            {
                Console.WriteLine("No variables were assigned true.");
            }
        }
        else
        {
            Console.WriteLine("No solution found.");
        }
    }


}
