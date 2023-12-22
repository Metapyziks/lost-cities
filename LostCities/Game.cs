using System.Text.Json.Serialization;
using System.Text.Json;

namespace LostCities;

public record struct GameConfig( int GameSeed, int Player1Seed, int Player2Seed );

public record GameResult( GameState InitialState, GameState FinalState, IReadOnlyList<PlayerAction> Actions, Player Disqualified )
{
    [JsonIgnore]
    public PlayerState Player1FinalState => FinalState.Player1;

    [JsonIgnore]
    public PlayerState Player2FinalState => FinalState.Player2;

    [JsonIgnore]
    public Player Winner =>
        Disqualified switch
        {
            Player.Player1 => Player.Player2,
            Player.Player2 => Player.Player1,
            _ => Player1FinalState.Score.CompareTo( Player2FinalState.Score ) switch
            {
                > 0 => Player.Player1,
                < 0 => Player.Player2,
                _ => Player.None
            }
        };

    public PlayerState GetFinalState( Player player )
    {
        return player switch
        {
            Player.Player1 => Player1FinalState,
            Player.Player2 => Player2FinalState,
            _ => throw new ArgumentException()
        };
    }
}

public record GameSummary( GameConfig Config, Player FirstTurn, Player Winner, Player Disqualified, int? Player1Score, int? Player2Score );

public record GameResults( IReadOnlyList<GameSummary> Summaries, IReadOnlyList<GameResult>? Results )
{
    private static IReadOnlyList<GameSummary> GenerateSummaries( IReadOnlyList<GameConfig> configs, IReadOnlyList<GameResult> results )
    {
        return results
            .Select( ( x, i ) => new GameSummary( configs[i],
                x.InitialState.CurrentPlayer, x.Winner, x.Disqualified,
                x.Disqualified == Player.None ? x.FinalState.Player1.Score : null,
                x.Disqualified == Player.None ? x.FinalState.Player2.Score : null ) )
            .ToArray();
    }

    public GameResults( IReadOnlyList<GameConfig> Configs, IReadOnlyList<GameResult> Results, bool fullResults )
        : this( GenerateSummaries( Configs, Results ), fullResults ? Results : null )
    {

    }

    public static GameResults FromJson( string value )
    {
        return JsonSerializer.Deserialize<GameResults>( value, Helpers.JsonOptions )!;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize( this, Helpers.JsonOptions );
    }
}
