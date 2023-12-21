using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace LostCities.Game;

public class Program
{
    /// <summary>
    /// Run one or more games with the given pair of players.
    /// </summary>
    /// <param name="player1">Command to run for player 1.</param>
    /// <param name="player2">Command to run for player 2.</param>
    /// <param name="output">File path to write game results.</param>
    /// <param name="gameSeed">Seed to use when shuffling the deck.</param>
    /// <param name="player1Seed">Seed to pass to player 1.</param>
    /// <param name="player2Seed">Seed to pass to player 2.</param>
    /// <param name="games">How many games to play in a row.</param>
    /// <param name="parallel">If true, run many games simultaneously.</param>
    /// <param name="fullResults">If true, output full results to file.</param>
    public static async Task<int> Main( FileInfo player1, FileInfo player2, FileInfo? output = null,
        int? gameSeed = null, int? player1Seed = null, int? player2Seed = null,
        int games = 1, bool parallel = false, bool fullResults = false )
    {
        var configs = Enumerable.Range( 0, games )
            .Select( i => new GameConfig(
                gameSeed ?? RandomNumberGenerator.GetInt32( int.MinValue, int.MaxValue ),
                player1Seed ?? RandomNumberGenerator.GetInt32( int.MinValue, int.MaxValue ),
                player2Seed ?? RandomNumberGenerator.GetInt32( int.MinValue, int.MaxValue ) ) )
            .ToArray();

        var task = parallel
            ? RunGamesParallelAsync( player1.FullName, player2.FullName, configs )
            : RunGamesAsync( player1.FullName, player2.FullName, configs );

        var results = await task;

        if ( games == 1 )
        {
            PrintGameConfig( configs[0] );
            PrintGameResult( results[0] );
        }

        Console.WriteLine();
        Console.WriteLine( "=== Final Results ===" );
        Console.WriteLine();

        PrintPlayerStats( results, Player.Player1 );
        Console.WriteLine();
        PrintPlayerStats( results, Player.Player2 );
        Console.WriteLine();

        if ( output != null )
        {
            var resultData = new GameResults( configs, results, fullResults );
            await File.WriteAllTextAsync( output.FullName, resultData.ToJson() );
        }

        return 0;
    }

    private static async Task<IReadOnlyList<GameResult>> RunGamesAsync( string player1Path, string player2Path, IReadOnlyList<GameConfig> configs )
    {
        var results = new List<GameResult>( configs.Count );

        foreach ( var config in configs )
        {
            var result = await RunGameAsync( player1Path, player2Path, config );
            results.Add( result );
        }

        return results;
    }

    private static async Task<IReadOnlyList<GameResult>> RunGamesParallelAsync( string player1Path, string player2Path, IReadOnlyList<GameConfig> configs )
    {
        var results = new ConcurrentBag<(int Index, GameResult Result)>();

        var task = Parallel.ForEachAsync( configs.Select( (x, i) => (Index: i, Config: x) ), async ( x, _ ) =>
        {
            var result = await RunGameAsync( player1Path, player2Path, x.Config );
            results.Add( (x.Index, result) );
        } );

        var top = Console.CursorTop;

        Console.CursorVisible = false;

        while ( !task.IsCompleted )
        {
            Console.SetCursorPosition( 0, top );
            Console.WriteLine( $"Completed {results.Count} of {configs.Count} games..." );

            await Task.Delay( 1 );
        }

        Console.SetCursorPosition( 0, top );
        Console.WriteLine( $"Completed {results.Count} of {configs.Count} games   " );
        Console.WriteLine();

        Console.CursorVisible = true;

        await task;

        var sortedResults = results
            .OrderBy( x => x.Index )
            .Select( x => x.Result )
            .ToArray();

        return sortedResults;
    }

    private static async Task<GameResult> RunGameAsync( string player1Path, string player2Path, GameConfig config )
    {
        var initialState = GameState.New( config.GameSeed, config.Player1Seed, config.Player2Seed );
        var state = initialState;

        using var p1 = new PlayerHost( player1Path );
        using var p2 = new PlayerHost( player2Path );

        var actions = new List<PlayerAction>();

        while ( state.CurrentPlayer != Player.None )
        {
            var player = state.CurrentPlayer switch
            {
                Player.Player1 => p1,
                Player.Player2 => p2,
                _ => throw new Exception()
            };

            var action = await player.TakeTurnAsync( state.CurrentPlayerView );

            if ( action == null )
            {
                return new GameResult( initialState, state, actions, state.CurrentPlayer );
            }

            actions.Add( action );
            state = state.WithAction( action );
        }

        return new GameResult( initialState, state, actions, Player.None );
    }

    private static void PrintPlayerState( Player player, PlayerState state )
    {
        Console.WriteLine( $"{player}: {state.Score} Points" );

        foreach ( var (color, cards) in state.Expeditions )
        {
            switch ( color )
            {
                case Color.White:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case Color.Yellow:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case Color.Blue:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case Color.Red:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case Color.Green:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case Color.Purple:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
            }

            var score = cards.Count == 0
                ? 0
                : (cards.Sum( x => (int)x.Value ) - 20)
                  * (1 + cards.Count( x => x.Value == Value.Wager ));

            Console.WriteLine( $"  {color}: {string.Join( ", ", cards.Select( x => x.Value.ToString() ) )} ({score})" );
        }

        Console.ResetColor();
    }

    private static void PrintGameConfig( GameConfig config )
    {
        Console.WriteLine( $"Game Seed: {config.GameSeed}" );
        Console.WriteLine( $"{Player.Player1} Seed: {config.Player1Seed}" );
        Console.WriteLine( $"{Player.Player2} Seed: {config.Player2Seed}" );
        Console.WriteLine();
    }

    private static void PrintGameResult( GameResult result )
    {
        if ( result.Disqualified != Player.None )
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine( $"{result.Disqualified} Disqualified!" );
        }
        else
        {
            PrintPlayerState( Player.Player1, result.Player1FinalState );
            PrintPlayerState( Player.Player2, result.Player2FinalState );
        }

        Console.ResetColor();
        Console.WriteLine();
    }

    private static void PrintPlayerStats( IReadOnlyList<GameResult> results, Player player )
    {
        Console.WriteLine( $"{player}:" );
        Console.WriteLine( $"  Went First: {results.Count( x => x.InitialState.CurrentPlayer == player )}" );
        Console.WriteLine( $"  Wins: {results.Count( x => x.Winner == player )}" );
        Console.WriteLine( $"  Ties: {results.Count( x => x.Winner == Player.None )}" );
        Console.WriteLine( $"  Losses: {results.Count( x => x.Winner != Player.None && x.Winner != player )}" );
        Console.WriteLine( $"  Best Score: {results.Max( x => x.GetFinalState( player ).Score )}" );
        Console.WriteLine( $"  Worst Score: {results.Min( x => x.GetFinalState( player ).Score )}" );
        Console.WriteLine( $"  Mean Score: {results.Average( x => x.GetFinalState( player ).Score )}" );
    }
}
