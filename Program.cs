using Google.OrTools.Sat;
using SeaOfConquest.Constraints;
using SeaOfConquest.Models;
using SeaOfConquest.Solvers;
using SeaOfConquest.Utilities;
using Spectre.Console;

namespace SeaOfConquest;

internal class Program
{
    public static void Main(string[] args)
    {
        var heroes = HeroReader.ReadHeroes("Files/heroes.csv");
        var trinkets = TrinketReader.ReadTrinkets("Files/trinkets.csv");

        var ships = GetDistinctShips(heroes);
        var positions = new List<string> {"Captain", "First Mate", "Gunner"};

        // Let the user select heroes and ships to exclude and filter them out
        var excludedHeroes = SelectExclusionsForHeroes(heroes);
        var excludedShips = SelectExclusionsForShips(ships);
        heroes = heroes.Except(excludedHeroes).ToList();
        ships = ships.Except(excludedShips).ToList();

        var model = new CpModel();
        var assignments = CreateAssignmentVariables(model, heroes, ships, positions);
        var trinketAssignments = CreateTrinketAssignmentVariables(model, heroes, trinkets);
        var shipActiveVars = CreateShipActiveVariables(model, ships);

        // Add constraints to limit the number of active ships and manage hero assignments per ship
        ShipConstraints.AddShipAndHeroConstraints(model, assignments, shipActiveVars, ships, positions);
        HeroConstraints.AddHeroAssignmentConstraints(model, assignments, heroes, ships, positions);
        TrinketConstraints.AddTrinketConstraints(model, trinketAssignments, heroes, trinkets);

        var totalScoreForHeroes = InitializeTotalScoreForHeroes(model, assignments, heroes, ships, positions);
        var totalScoreForTrinkets = InitializeTotalScoreForTrinkets(model, trinketAssignments, heroes);

        // Combine the total scores for heroes and trinkets
        var combinedMaxScore = heroes.Count * 20; // 10 for heroes and 10 for trinkets
        var overallTotalScore = model.NewIntVar(0, combinedMaxScore, "overallTotalScore");
        model.Add(overallTotalScore == totalScoreForHeroes + totalScoreForTrinkets);

        model.Maximize(overallTotalScore);

        HeroSolver.SolveHeroAssignments(model, assignments);
        TrinketSolver.SolveTrinketAssignments(model, trinketAssignments);
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

    private static Dictionary<string, BoolVar> CreateShipActiveVariables(CpModel model, List<string> ships)
    {
        var shipActiveVars = new Dictionary<string, BoolVar>();
        foreach (var ship in ships)
        {
            shipActiveVars[ship] = model.NewBoolVar($"active_{ship}");
        }

        return shipActiveVars;
    }

    private static Dictionary<string, IntVar> CreateTrinketAssignmentVariables(CpModel model, List<Hero> heroes, List<Trinket> trinkets)
    {
        var trinketAssignments = new Dictionary<string, IntVar>();

        foreach (var hero in heroes)
        {
            foreach (var trinket in trinkets)
            {
                if (trinket.Amount > 0) // Check inventory is available
                {
                    var varName = $"{hero.Name}_{trinket.Name}";
                    trinketAssignments[varName] = model.NewBoolVar(varName);
                }
            }
        }

        return trinketAssignments;
    }

    private static List<string> GetDistinctShips(List<Hero> heroes)
    {
        return heroes.SelectMany(hero => hero.PreferredShips).Distinct().ToList();
    }

    private static IntVar InitializeTotalScoreForHeroes(CpModel model, Dictionary<string, IntVar> assignments, List<Hero> heroes, List<string> ships, List<string> positions)
    {
        // Define the range of the total score based on potential maximum points
        var maxScore = heroes.Count * 10; // Assuming a max score of 10 per hero for their preferred assignment
        var totalScore = model.NewIntVar(0, maxScore, "totalScoreForHeroes");

        // Use a more straightforward approach to calculate the total score
        var scoredComponents = new List<IntVar>();
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
                            var preferenceScore = HeroConstraints.CalculatePreferenceScore(hero, ship, position);

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

    private static IntVar InitializeTotalScoreForTrinkets(CpModel model, Dictionary<string, IntVar> trinketAssignments, List<Hero> heroes)
    {
        // Define the range of the total score based on potential maximum points
        var maxScore = heroes.Count * 10; // Assuming a max score of 10 per hero for their preferred trinket assignment
        var totalScore = model.NewIntVar(0, maxScore, "totalScoreForTrinkets");

        // Use a more straightforward approach to calculate the total score
        var scoredComponents = new List<IntVar>();
        foreach (var hero in heroes)
        {
            foreach (var trinket in hero.PreferredTrinkets)
            {
                var varName = $"{hero.Name}_{trinket}";
                // Check if the varName key exists in the dictionary before using it
                if (trinketAssignments.ContainsKey(varName))
                {
                    // Assign a preference score for trinket assignment (assuming 10 for preferred trinket)
                    var preferenceScore = 10; // Adjust this value based on your scoring criteria

                    // Create an intermediate variable for trinket score contribution
                    var scoreContribution = model.NewIntVar(0, preferenceScore, $"trinketContrib_{varName}");
                    model.Add(scoreContribution == trinketAssignments[varName] * preferenceScore);
                    scoredComponents.Add(scoreContribution);
                }
            }
        }

        // Sum all contributions to get the total score
        model.Add(LinearExpr.Sum(scoredComponents) == totalScore);

        return totalScore;
    }
}
