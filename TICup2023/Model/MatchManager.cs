namespace TICup2023.Model;

public class MatchManager
{
    private static readonly MatchManager Instance = new();
    
    public int[] MapSizeList { get; } = {8, 10};
    public int MapSize { get; set; } = 8;
    
    public static MatchManager GetInstance() => Instance;
}