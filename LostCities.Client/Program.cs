using System.Collections.Concurrent;
using LostCities.Client;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using LostCities;

var hostNameOption = new Option<string>(
    name: "--hostname",
    description: "Address of the matchmaking server." );
var portOption = new Option<int>(
    name: "--port",
    description: "Master server TCP port.",
    getDefaultValue: () => 57910 );
var playerTokenOption = new Option<string>(
    name: "--token",
    description: "Secret token to identify your bot." );
var versionOption = new Option<string?>(
    name: "--version",
    description: "String to identify the current version of your bot.",
    getDefaultValue: () => null );
var playerOption = new Option<string[]>(
    name: "--player",
    description: "Command to run implementing a player." )
{
    AllowMultipleArgumentsPerToken = true
};
var maxParallelGamesOption = new Option<int>(
    name: "--max-parallel",
    description: "Maximum number of games to play in parallel.",
    getDefaultValue: () => 16 );
var outputOption = new Option<FileInfo>(
    name: "--output",
    description: "File to write game results to." );

var rootCommand = new RootCommand( "Connect to the game server and start playing matches." )
{
    hostNameOption,
    portOption,
    playerTokenOption,
    versionOption,
    playerOption,
    maxParallelGamesOption,
    outputOption
};

var jsonOptions = new JsonSerializerOptions
{
    Converters =
    {
        new JsonStringEnumConverter()
    }
};

rootCommand.SetHandler( async (hostName, port, playerToken, version, playerCmd, maxParallelGames, output ) =>
{
    var client = new TcpClient();

    await client.ConnectAsync( hostName, port );

    Console.WriteLine( "Connected!" );

    await using var stream = client.GetStream();
    await using var writer = new StreamWriter( stream );

    using var reader = new StreamReader( stream );

    var results = new List<(int Player, GameSummary Summary)>();

    var runningGames = new Dictionary<string, RunningGame>();
    var sendQueue = new ConcurrentQueue<ClientMessage>();

    var cancelPressed = false;

    sendQueue.Enqueue( new InitializeMessage( playerToken, version, maxParallelGames ) );

    void UpdateDisplay()
    {
        Console.Clear();

        if ( cancelPressed )
        {
            Console.WriteLine( $"Waiting for all active games to finish before exiting" );
            Console.WriteLine( $"Press Ctrl+C again to immediately exit, forfeiting active games" );
        }
        else
        {
            Console.WriteLine( "Press Ctrl+C to stop accepting games" );
        }

        var winCount = results.Count( x => (int)x.Summary.Winner == x.Player );

        Console.WriteLine();
        Console.WriteLine( $"Active game count: {runningGames.Count}" );
        Console.WriteLine( $"Completed games: {results.Count}" );
        Console.WriteLine( $"Win rate: {(results.Count == 0 ? "N/A" : winCount * 100d / results.Count):F1}%" );
        Console.WriteLine( $"Disqualified: {results.Count( x => (int)x.Summary.Disqualified == x.Player )}" );
        Console.WriteLine();
    }

    Console.CancelKeyPress += ( sender, ev ) =>
    {
        if ( cancelPressed || runningGames.Count == 0 )
        {
            Console.WriteLine( $"Exiting immediately" );
            System.Environment.Exit( 0 );
        }
        else
        {
            ev.Cancel = false;
            cancelPressed = true;

            UpdateDisplay();
        }
    };

    _ = Task.Run( async () =>
    {
        try
        {
            while ( client.Connected )
            {
                if ( sendQueue.Count == 0 )
                {
                    await Task.Delay( 10 );
                }

                while ( sendQueue.TryDequeue( out var clientMessage ) )
                {
                    await writer.WriteLineAsync( JsonSerializer.Serialize( clientMessage, jsonOptions ) );
                }

                await writer.FlushAsync();
            }
        }
        catch ( Exception e )
        {
            Console.WriteLine( e );
        }
    } );

    try
    {
        while ( await reader.ReadLineAsync() is { } line )
        {
            var serverMessage = JsonSerializer.Deserialize<ServerMessage>( line, jsonOptions );

            switch ( serverMessage )
            {
                case NewGameMessage newGameMessage:
                {
                    var expired = runningGames
                        .Where( x => x.Value.HasExpired )
                        .ToArray();

                    foreach ( var expiredGame in expired )
                    {
                        runningGames.Remove( expiredGame.Key );
                        expiredGame.Value.Dispose();
                    }

                    if ( runningGames.Count >= maxParallelGames || cancelPressed )
                    {
                        sendQueue.Enqueue( new AcceptGameMessage( newGameMessage.Id, false ) );
                    }
                    else
                    {
                        runningGames[newGameMessage.Id] =
                            new RunningGame( new ChildProcessPlayer( Player.None, playerCmd[0], playerCmd.Skip( 1 ).ToArray() ),
                                TimeSpan.FromSeconds( newGameMessage.MaxTurnTime ?? 60d ) );
                        sendQueue.Enqueue( new AcceptGameMessage( newGameMessage.Id, true ) );
                    }

                    UpdateDisplay();
                    break;
                }

                case TurnMessage turnMessage:
                {
                    if ( !runningGames.TryGetValue( turnMessage.Id, out var player ) )
                    {
                        break;
                    }

                    player.TakeTurn( PlayerView.FromJson( turnMessage.ViewJson ), action =>
                        sendQueue.Enqueue( new ActionMessage( turnMessage.Id, action?.ToJson() ?? "" ) ) );
                    break;
                }

                case EndGameMessage endGameMessage:
                {
                    if ( !runningGames.TryGetValue( endGameMessage.Id, out var player ) )
                    {
                        break;
                    }

                    runningGames.Remove( endGameMessage.Id );
                    player.Dispose();

                    if ( output != null )
                    {
                        await File.AppendAllTextAsync( output.FullName, $"{{ \"Player\": {endGameMessage.Player}, \"Result\": {endGameMessage.ResultJson} }}{Environment.NewLine}" );
                    }

                    var result = GameSummary.FromJson( endGameMessage.ResultJson );

                    results.Add( (endGameMessage.Player, result) );

                    if ( (int) result.Disqualified == endGameMessage.Player )
                    {
                        cancelPressed = true;
                    }

                    if ( cancelPressed && runningGames.Count == 0 )
                    {
                        return;
                    }

                    UpdateDisplay();
                    break;
                }

                case KickMessage kickMessage:
                {
                    Console.WriteLine( $"Kicked: {kickMessage.Reason}" );
                    return;
                }
            }
        }
    }
    finally
    {
        foreach ( var game in runningGames )
        {
            game.Value.Dispose();
        }
    }

}, hostNameOption, portOption, playerTokenOption, versionOption, playerOption, maxParallelGamesOption, outputOption );

await rootCommand.Parse( args  ).InvokeAsync();
