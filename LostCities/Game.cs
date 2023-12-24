using System.Text.Json.Serialization;
using System.Text.Json;
using System.Security.Cryptography;

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

public record GameSummary( GameConfig Config, Player FirstTurn, Player Winner, Player Disqualified, int? Player1Score,
    int? Player2Score )
{
    public GameSummary( GameConfig config, GameResult result )
    : this (config, result.InitialState.CurrentPlayer, result.Winner, result.Disqualified,
        result.Disqualified == Player.None ? result.FinalState.Player1.Score : null,
        result.Disqualified == Player.None ? result.FinalState.Player2.Score : null )
    {

    }

    public string ToJson()
    {
        return JsonSerializer.Serialize( this, Helpers.JsonOptions );
    }
}

public record GameResults( IReadOnlyList<GameSummary> Summaries, IReadOnlyList<GameResult>? Results )
{
    private static IReadOnlyList<GameSummary> GenerateSummaries( IReadOnlyList<GameConfig> configs, IReadOnlyList<GameResult> results )
    {
        return results
            .Select( ( x, i ) => new GameSummary( configs[i], x ) )
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

public static class LostCities
{
    public static async Task<GameResult> ContinueGameAsync( GameState state, IPlayer player1, IPlayer player2 )
    {
        var initialState = state;
        var actions = new List<PlayerAction>();

        while ( state.CurrentPlayer != Player.None )
        {
            var player = state.CurrentPlayer switch
            {
                Player.Player1 => player1,
                Player.Player2 => player2,
                _ => throw new Exception()
            };

            PlayerAction? action;

            try
            {
                action = await player.TakeTurnAsync( state.CurrentPlayerView );
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( e );
                action = null;
            }

            if ( action == null )
            {
                return new GameResult( initialState, state, actions, state.CurrentPlayer );
            }

            actions.Add( action );
            state = state.WithAction( action );
        }

        return new GameResult( initialState, state, actions, Player.None );
    }

    public static Task<GameResult> RunGameAsync( GameConfig config, IPlayer player1, IPlayer player2 )
    {
        var initialState = GameState.New( config.GameSeed, config.Player1Seed, config.Player2Seed );

        return ContinueGameAsync( initialState, player1, player2 );
    }
}
