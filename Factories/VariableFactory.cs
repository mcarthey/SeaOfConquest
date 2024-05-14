using Google.OrTools.Sat;
using SeaOfConquest.Models;

namespace SeaOfConquest.Factories;

public static class VariableFactory
{
    public static Dictionary<string, IntVar> CreateAssignmentVariables(CpModel model, List<Hero> heroes, List<string> ships, List<string> positions)
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

    public static Dictionary<string, BoolVar> CreateShipActiveVariables(CpModel model, List<string> ships)
    {
        var shipActiveVars = new Dictionary<string, BoolVar>();
        foreach (var ship in ships)
        {
            shipActiveVars[ship] = model.NewBoolVar($"active_{ship}");
        }

        return shipActiveVars;
    }

    public static Dictionary<string, IntVar> CreateTrinketAssignmentVariables(CpModel model, List<Hero> heroes, List<Trinket> trinkets)
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
}
