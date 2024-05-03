﻿using Google.OrTools.Sat;
using SeaOfConquest;

internal class Program
{
    public static void Main(string[] args)
    {
        var heroes = ReadHeroes("heroes.csv");
        var ships = GetDistinctShips(heroes);
        //ships = SelectSubsetOfShips(ships);

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

    private static List<string> SelectSubsetOfShips(List<string> allShips)
    {
        // Define the subset of ships you want to focus on
        var selectedShips = new List<string> { "Flagship", "Fearless Princess", "Warhammer", "Stormbringer" };  // Example subset

        // Filter the list of ships to include only the selected ones
        var shipsToConsider = allShips.Where(ship => selectedShips.Contains(ship)).ToList();

        return shipsToConsider;
    }

    /// <summary>
    /// Adds constraints to the model to ensure each hero is assigned to exactly one position on one ship
    /// and each position on each ship is filled by exactly one hero.
    /// </summary>
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


    /// <summary>
    /// Creates binary variables for each hero, ship, and position combination.
    /// </summary>
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

    /// <summary>
    /// Extracts a distinct list of ships from the heroes' preferences.
    /// </summary>
    private static List<string> GetDistinctShips(List<Hero> heroes)
    {
        return heroes.SelectMany(hero => hero.PreferredShips).Distinct().ToList();
    }

    /// <summary>
    /// Initializes and returns the total score variable based on the assignments.
    /// The total score is calculated by assigning scores to preferred assignments.
    /// </summary>
    private static IntVar InitializeTotalScore(CpModel model, Dictionary<string, IntVar> assignments, List<Hero> heroes, List<string> ships, List<string> positions)
    {
        // Define the range of the total score based on potential maximum points
        int maxScore = heroes.Count * 10;  // Assuming a max score of 10 per hero for their preferred assignment
        var totalScore = model.NewIntVar(0, maxScore, "totalScore");

        // Use a more straightforward approach to calculate the total score
        List<IntVar> scoredComponents = new List<IntVar>();
        foreach (var hero in heroes)
        {
            foreach (var ship in hero.PreferredShips)
            {
                foreach (var position in positions)
                {
                    if (hero.PreferredPositions.Contains(position))
                    {
                        var varName = $"{hero.Name}_{ship}_{position}";
                        // Calculate the preference score for each assignment
                        int preferenceScore = CalculatePreferenceScore(hero, ship, position);

                        // Create an intermediate variable for score contribution, scaled by preference
                        var scoreContribution = model.NewIntVar(0, preferenceScore, $"scoreContrib_{varName}");
                        model.Add(scoreContribution == assignments[varName] * preferenceScore);
                        scoredComponents.Add(scoreContribution);
                    }
                }
            }
        }

        // Sum all contributions to get the total score
        model.Add(LinearExpr.Sum(scoredComponents) == totalScore);

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
    private static void SolveModel(CpModel model, Dictionary<string, IntVar> assignments)
    {
        var solver = new CpSolver();
        var status = solver.Solve(model);

        if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
        {
            Console.WriteLine("Solution Found:");
            bool anyAssigned = false;

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
                    string assignmentDescription = $"Hero {hero} is assigned to {position}.";

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
