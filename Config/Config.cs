namespace SeaOfConquest.Config;

public static class Config
{
    public static string HeroesFilePath { get; set; } = "Files/heroes.csv";
    public static int MaxActiveShips { get; set; } = 4;
    public static int MaxScorePerHero { get; set; } = 10;
    public static int MaxScorePerTrinket { get; set; } = 10;
    public static string TrinketsFilePath { get; set; } = "Files/trinkets.csv";
}
