using Google.OrTools.Sat;
using SeaOfConquest;
using Spectre.Console;

internal class Program
{
    public static void Main(string[] args)
    {
        var heroes = ReadHeroes("Files/heroes.csv");
        var trinkets = ReadTrinkets("Files/trinkets.csv");

        var ships = GetDistinctShips(heroes);
        var positions = new List<string> { "Captain", "First Mate", "Gunner" };

        //heroes = SelectSubsetOfHeroes(heroes);
        //ships = SelectSubsetOfShips(ships);

        // Let the user select heroes and ships to exclude and filter them out
        var excludedHeroes = SelectExclusionsForHeroes(heroes);
        var excludedShips = SelectExclusionsForShips(ships);
        heroes = heroes.Except(excludedHeroes).ToList();
        ships = ships.Except(excludedShips).ToList();

        var model = new CpModel();
        var assignments = CreateAssignmentVariables(model, heroes, ships, positions);

        // Use this for simplified testing
        //var totalScore = InitializeTotalScoreSimplified(model, assignments);

        var shipActiveVars = CreateShipActiveVariables(model, ships);
        var trinketAssignments = CreateTrinketAssignmentVariables(model, heroes, trinkets);

        // Add constraints to limit the number of active ships and manage hero assignments per ship
        AddShipAndHeroConstraints(model, assignments, shipActiveVars, ships, positions);

        // Ensure each hero is used at most once across all ships and positions
        AddHeroAssignmentConstraints(model, assignments, heroes, ships, positions);

        AddTrinketConstraints(model, trinketAssignments, trinkets);

        var totalScore = InitializeTotalScore(model, assignments, heroes, ships, positions);

        // Add constraints
        //AddConstraints(model, heroes, ships, positions, assignments);

        model.Maximize(totalScore);

        SolveHeroAssigments(model, assignments);
    }

    // iterate over the list of Trinket objects and create variables based on the trinket name and availability.
    private static Dictionary<string, IntVar> CreateTrinketAssignmentVariables(CpModel model, List<Hero> heroes, List<Trinket> trinkets)
    {
        var trinketAssignments = new Dictionary<string, IntVar>();

        foreach (var hero in heroes)
        {
            foreach (var trinket in trinkets)
            {
                if (trinket.Amount > 0) // Check inventory is available
                {
                    string varName = $"{hero.Name}_{trinket.Name}";
                    trinketAssignments[varName] = model.NewBoolVar(varName);
                }
            }
        }

        return trinketAssignments;
    }

    // add constraints to ensure that the total number of trinkets assigned does not exceed the available inventory for each trinket
    private static void AddTrinketConstraints(CpModel model, Dictionary<string, IntVar> trinketAssignments, List<Trinket> trinkets)
    {
        foreach (var trinket in trinkets)
        {
            var allAssignmentsForTrinket = trinketAssignments.Where(kv => kv.Key.EndsWith($"_{trinket.Name}")).Select(kv => kv.Value).ToList();
            if (allAssignmentsForTrinket.Any())
            {
                model.Add(LinearExpr.Sum(allAssignmentsForTrinket) <= trinket.Amount);
            }
        }
    }



    public static List<Hero> SelectExclusionsForHeroes(List<Hero> heroes)
    {
        // Use Spectre.Console to create a checklist for heroes
        var exclusions = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select the heroes you want to [red]exclude[/] from the list:")
                .NotRequired()
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more heroes)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle a hero, [green]<enter>[/] to accept)[/]")
                .AddChoices(heroes.Select(h => h.Name)));

        // Return the list of Hero objects that are selected for exclusion
        return heroes.Where(hero => exclusions.Contains(hero.Name)).ToList();
    }

    public static List<string> SelectExclusionsForShips(List<string> ships)
    {
        // Use Spectre.Console to create a checklist for ships
        var exclusions = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select the ships you want to [red]exclude[/] from the list:")
                .NotRequired()
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more ships)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle a ship, [green]<enter>[/] to accept)[/]")
                .AddChoices(ships));

        // Return the list of strings that are selected for exclusion
        return exclusions;
    }

    private static Dictionary<string, BoolVar> CreateShipActiveVariables(CpModel model, List<string> ships)
    {
        var shipActiveVars = new Dictionary<string, BoolVar>();
        foreach (var ship in ships)
        {
            shipActiveVars[ship] = model.NewBoolVar($"active_{ship}");
        }
        return shipActiveVars;
    }

    private static void AddShipAndHeroConstraints(CpModel model, Dictionary<string, IntVar> assignments, Dictionary<string, BoolVar> shipActiveVars, List<string> ships, List<string> positions)
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

    private static void AddHeroAssignmentConstraints(CpModel model, Dictionary<string, IntVar> assignments, List<Hero> heroes, List<string> ships, List<string> positions)
    {
        foreach (var hero in heroes)
        {
            List<IntVar> heroAssignments = new List<IntVar>();
            foreach (var ship in ships)
            {
                foreach (var position in positions)
                {
                    string varName = $"{hero.Name}_{ship}_{position}";
                    if (assignments.ContainsKey(varName))  // Ensure the variable exists before adding it to the list
                    {
                        heroAssignments.Add(assignments[varName]);
                    }
                }
            }
            // Add a constraint that the total number of positions across all ships for each hero is at most 1
            model.Add(LinearExpr.Sum(heroAssignments) <= 1);
        }
    }

    private static List<Hero> SelectSubsetOfHeroes(List<Hero> allHeroes)
    {
        // Define the subset of heroes you want to focus on
        var selectedHeroNames = new List<string> { "Boa", "Griffin", "Molly", "Cursed Ed", "Magnus", "Old Ahab", "Cordelia", "Armstrong", "Bones", "Luna", "Lord Kojo", "Barnacle", "Tanaka", "Lester", "Ophelia", "Qi Lanting", "Sharky" };  

        // Filter the list of heroes to include only the selected ones
        var heroesToConsider = allHeroes.Where(hero => selectedHeroNames.Contains(hero.Name)).ToList();

        return heroesToConsider;
    }

    private static List<string> SelectSubsetOfShips(List<string> allShips)
    {
        // Define the subset of ships you want to focus on
        var selectedShips = new List<string> { "Flagship", "Fearless Princess", "Warhammer", "Stormbringer", "Black Raven", "Dragon Lance", "Crimson Sentinel" };  

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
                    if (!assignments.ContainsKey(varName))
                    {
                        assignments[varName] = model.NewBoolVar(varName);
                    }
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
                        // Check if the varName key exists in the dictionary before using it
                        if (assignments.ContainsKey(varName))
                        {
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
    ///     Reads trinkets from a CSV file.
    /// </summary>
    /// <param name="filePath">The path to the CSV file.</param>
    /// <returns>A list of trinkets.</returns>
    private static List<Trinket> ReadTrinkets(string filePath)
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

    private static void SolveHeroAssigments(CpModel model, Dictionary<string, IntVar> assignments)
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
