using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace LostCities.Game;

public class Program
{
    public static async Task<int> Main( string[] args )
    {
        var player1Option = new Option<string[]>(
            name: "--player1",
            description: "Command to run for player 1. If omitted, will use standard input / output.",
            getDefaultValue: Array.Empty<string> )
        {
            AllowMultipleArgumentsPerToken = true
        };

        var player2Option = new Option<string[]>(
            name: "--player2",
            description: "Command to run for player 2. If omitted, will use standard input / output.",
            getDefaultValue: Array.Empty<string> )
        {
            AllowMultipleArgumentsPerToken = true
        };

        var outputOption = new Option<FileInfo?>(
            name: "--output",
            description: "File path to write game results.",
            getDefaultValue: () => null );

        var gameSeedOption = new Option<int?>(
            name: "--game-seed",
            description: "Seed to use when shuffling the deck. Random by default.",
            getDefaultValue: () => null );

        var player1SeedOption = new Option<int?>(
            name: "--player1-seed",
            description: "Seed to pass to player 1. Random by default.",
            getDefaultValue: () => null );

        var player2SeedOption = new Option<int?>(
            name: "--player2-seed",
            description: "Seed to pass to player 2. Random by default.",
            getDefaultValue: () => null );

        var gameCountOption = new Option<int>(
            name: "--games",
            description: "How many games to play in a row.",
            getDefaultValue: () => 1 );

        var maxParallelCountOption = new Option<int>(
            name: "--max-parallel",
            description: "How many games to run simultaneously.",
            getDefaultValue: () => 1 );

        var fullResultsOption = new Option<bool>(
            name: "--full-results",
            description: "If true, output full results to file.",
            getDefaultValue: () => false );

        var humanPlayerOption = new Option<bool>(
            name: "--human",
            description: "If true, standard input / output players will have a human-readable interface.",
            getDefaultValue: () => false );

        var rootCommand = new RootCommand( "Run one or more games with the given pair of players." )
        {
            player1Option, player2Option, outputOption,
            gameSeedOption, player1SeedOption, player2SeedOption,
            gameCountOption, maxParallelCountOption,
            fullResultsOption, humanPlayerOption
        };

        rootCommand.SetHandler( async ( context ) =>
        {
            var player1Cmd = context.ParseResult.GetValueForOption( player1Option )!;
            var player2Cmd = context.ParseResult.GetValueForOption( player2Option )!;
            var outputFile = context.ParseResult.GetValueForOption( outputOption );
            var gameSeed = context.ParseResult.GetValueForOption( gameSeedOption );
            var player1Seed = context.ParseResult.GetValueForOption( player1SeedOption );
            var player2Seed = context.ParseResult.GetValueForOption( player2SeedOption );
            var gameCount = context.ParseResult.GetValueForOption( gameCountOption );
            var maxParallelCount = context.ParseResult.GetValueForOption( maxParallelCountOption );
            var fullResults = context.ParseResult.GetValueForOption( fullResultsOption );
            var humanPlayer = context.ParseResult.GetValueForOption( humanPlayerOption );

            var configs = Enumerable.Range( 0, gameCount )
                .Select( i => new GameConfig(
                    gameSeed ?? RandomNumberGenerator.GetInt32( int.MinValue, int.MaxValue ),
                    player1Seed ?? RandomNumberGenerator.GetInt32( int.MinValue, int.MaxValue ),
                    player2Seed ?? RandomNumberGenerator.GetInt32( int.MinValue, int.MaxValue ) ) )
            .ToArray();

            if ( maxParallelCount > 1 && (player1Cmd.Length == 0 || player2Cmd.Length == 0) )
            {
                throw new ArgumentException( "Can't run multiple games simultaneously over standard input." );
            }

            var task = maxParallelCount > 1
                ? RunGamesParallelAsync( player1Cmd, player2Cmd, configs, maxParallelCount )
                : RunGamesAsync( player1Cmd, player2Cmd, configs, humanPlayer );

            var results = await task;

            if ( gameCount == 1 )
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

            if ( outputFile != null )
            {
                var resultData = new GameResults( configs, results, fullResults );
                await File.WriteAllTextAsync( outputFile.FullName, resultData.ToJson() );
            }
        } );

        var result = rootCommand.Parse( args );

        await result.InvokeAsync();

        return 0;
    }

    private static async Task<IReadOnlyList<GameResult>> RunGamesAsync( string[] player1Cmd, string[] player2Cmd, IReadOnlyList<GameConfig> configs, bool human )
    {
        var results = new List<GameResult>( configs.Count );

        var top = Console.CursorTop;

        foreach ( var config in configs )
        {
            Console.WriteLine( $"Game: {results.Count + 1}" );

            var result = await RunGameAsync( player1Cmd, player2Cmd, config, human );
            results.Add( result );
        }

        Console.WriteLine( "End" );
        Console.WriteLine();

        return results;
    }

    private static async Task<IReadOnlyList<GameResult>> RunGamesParallelAsync( string[] player1Cmd, string[] player2Cmd, IReadOnlyList<GameConfig> configs, int maxParallel )
    {
        var results = new ConcurrentBag<(int Index, GameResult Result)>();
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxParallel
        };

        var task = Parallel.ForEachAsync( configs.Select( (x, i) => (Index: i, Config: x) ), options, async ( x, _ ) =>
        {
            var result = await RunGameAsync( player1Cmd, player2Cmd, x.Config, false );
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

    private static IPlayer CreatePlayer( string[] cmd, Player player, bool human )
    {
        return cmd.Length == 0
            ? human ? new HumanConsolePlayer( player ) : new ConsolePlayer( player )
            : new ChildProcessPlayer( cmd[0], cmd.Skip( 1 ).ToArray() );
    }

    private static async Task<GameResult> RunGameAsync( string[] player1Cmd, string[] player2Cmd, GameConfig config, bool human )
    {
        using var p1 = CreatePlayer( player1Cmd, Player.Player1, human );
        using var p2 = CreatePlayer( player2Cmd, Player.Player2, human );

        return await LostCities.RunGameAsync( config, p1, p2 );
    }

    private static void PrintPlayerState( Player player, PlayerState state )
    {
        Console.WriteLine( $"{player}: {state.Score} Points" );

        foreach ( var (color, cards) in state.Expeditions )
        {
            var score = cards.Count == 0
                ? 0
                : (cards.Sum( x => (int)x.Value ) - 20)
                  * (1 + cards.Count( x => x.Value == Value.Wager ));

            Console.ForegroundColor = color.ToConsoleColor();
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
        Console.WriteLine( $"  Disqualified: {results.Count( x => x.Disqualified == player )}" );
        Console.WriteLine( $"  Best Score: {results.Max( x => x.GetFinalState( player ).Score )}" );
        Console.WriteLine( $"  Worst Score: {results.Min( x => x.GetFinalState( player ).Score )}" );
        Console.WriteLine( $"  Mean Score: {results.Average( x => x.GetFinalState( player ).Score )}" );
    }
}
