using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LostCities;

[JsonConverter( typeof(GameSeedConverter) )]
public record struct GameSeed( byte[] Bytes )
{
    private static Regex BytesRegex { get; } = new Regex( @"^\s*(?:(?<byte>[0-9a-fA-F]{2})\s*)+$" );

    public static GameSeed Parse( string value )
    {
        var match = BytesRegex.Match( value );

        if ( !match.Success || match.Groups["byte"].Captures.Count != 32 )
        {
            throw new Exception( "Expected 32 hex bytes." );
        }

        return new GameSeed( match.Groups["byte"].Captures
            .Select( x => byte.Parse( x.Value, NumberStyles.HexNumber ) )
            .ToArray() );
    }

    public static GameSeed Generate()
    {
        return new GameSeed( RandomNumberGenerator.GetBytes( 32 ) );
    }

    public override string ToString()
    {
        return string.Join( "", Bytes.Select( x => x.ToString( "x2" ) ) );
    }
}

public class DeckGenerator
{
    private readonly Random[] _randoms;
    private int _index;

    public DeckGenerator( GameSeed seed )
    {
        _randoms = Enumerable
            .Range( 0, 8 )
            .Select( x => new Random( BitConverter.ToInt32( seed.Bytes, x * 4 ) ) )
            .ToArray();
        _index = 0;
    }

    public int Next( int min, int max )
    {
        return _randoms[_index++ & 7].Next( min, max );
    }
}

public class GameSeedConverter : JsonConverter<GameSeed>
{

    public override GameSeed Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        var value = JsonSerializer.Deserialize<string>( ref reader, options );
        return GameSeed.Parse( value! );
    }

    public override void Write( Utf8JsonWriter writer, GameSeed value, JsonSerializerOptions options )
    {
        writer.WriteStringValue( value.ToString() );
    }
}

public record struct GameConfig( GameSeed GameSeed, int Player1Seed, int Player2Seed, Player StartPlayer );

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
    int? Player2Score, string Replay )
{
    public static GameSummary FromResult( GameConfig config, GameResult result )
    {
        return new GameSummary( config, result.InitialState.CurrentPlayer, result.Winner, result.Disqualified,
            result.Disqualified == Player.None ? result.FinalState.Player1.Score : null,
            result.Disqualified == Player.None ? result.FinalState.Player2.Score : null,
            GameString.Encode( result ) );
    }

    public static GameSummary FromJson( string value )
    {
        return JsonSerializer.Deserialize<GameSummary>( value, Helpers.JsonOptions )!;
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
            .Select( ( x, i ) => GameSummary.FromResult( configs[i], x ) )
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
        var initialState = GameState.New( config.GameSeed, config.Player1Seed, config.Player2Seed, config.StartPlayer );

        return ContinueGameAsync( initialState, player1, player2 );
    }
}
