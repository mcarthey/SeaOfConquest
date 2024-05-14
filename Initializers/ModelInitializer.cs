using Google.OrTools.Sat;
using SeaOfConquest.Constraints;
using SeaOfConquest.Models;

namespace SeaOfConquest.Initializers;

public static class ModelInitializer
{
    public static IntVar InitializeTotalScoreForHeroes(CpModel model, Dictionary<string, IntVar> assignments, List<Hero> heroes, List<string> ships, List<string> positions)
    {
        // Define the range of the total score based on potential maximum points
        var maxScore = heroes.Count * Config.Config.MaxScorePerHero; // Assuming a max score of 10 per hero for their preferred assignment
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

    public static IntVar InitializeTotalScoreForTrinkets(CpModel model, Dictionary<string, IntVar> trinketAssignments, List<Hero> heroes)
    {
        // Define the range of the total score based on potential maximum points
        var maxScore = heroes.Count * Config.Config.MaxScorePerTrinket; // Assuming a max score of 10 per hero for their preferred trinket assignment
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
                    var preferenceScore = Config.Config.MaxScorePerTrinket; // Adjust this value based on your scoring criteria

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
