using Google.OrTools.Sat;
using SeaOfConquest.Constraints;
using SeaOfConquest.Factories;
using SeaOfConquest.Initializers;
using SeaOfConquest.Models;
using SeaOfConquest.Solvers;
using SeaOfConquest.Utilities;
using Spectre.Console;

namespace SeaOfConquest;

internal class Program
{
    public static void Main(string[] args)
    {
        var heroes = HeroReader.ReadHeroes(Config.Config.HeroesFilePath);
        var trinkets = TrinketReader.ReadTrinkets(Config.Config.TrinketsFilePath);

        var ships = GetDistinctShips(heroes);
        var positions = new List<string> {"Captain", "First Mate", "Gunner"};

        // Let the user select heroes and ships to exclude and filter them out
        var excludedHeroes = SelectExclusionsForHeroes(heroes);
        var excludedShips = SelectExclusionsForShips(ships);
        heroes = heroes.Except(excludedHeroes).ToList();
        ships = ships.Except(excludedShips).ToList();

        var model = new CpModel();
        var assignments = VariableFactory.CreateAssignmentVariables(model, heroes, ships, positions);
        var trinketAssignments = VariableFactory.CreateTrinketAssignmentVariables(model, heroes, trinkets);
        var shipActiveVars = VariableFactory.CreateShipActiveVariables(model, ships);

        // Add constraints to limit the number of active ships and manage hero assignments per ship
        ShipConstraints.AddShipAndHeroConstraints(model, assignments, shipActiveVars, ships, positions);
        HeroConstraints.AddHeroAssignmentConstraints(model, assignments, heroes, ships, positions);
        TrinketConstraints.AddTrinketConstraints(model, trinketAssignments, heroes, trinkets);

        var totalScoreForHeroes = ModelInitializer.InitializeTotalScoreForHeroes(model, assignments, heroes, ships, positions);
        var totalScoreForTrinkets = ModelInitializer.InitializeTotalScoreForTrinkets(model, trinketAssignments, heroes);

        // Combine the total scores for heroes and trinkets
        var combinedMaxScore = heroes.Count * (Config.Config.MaxScorePerHero + Config.Config.MaxScorePerTrinket); // 10 for heroes and 10 for trinkets
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

    private static List<string> GetDistinctShips(List<Hero> heroes)
    {
        return heroes.SelectMany(hero => hero.PreferredShips).Distinct().ToList();
    }
}
