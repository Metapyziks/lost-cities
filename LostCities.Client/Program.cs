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

var rootCommand = new RootCommand( "Connect to the game server and start playing matches." )
{
    hostNameOption,
    portOption,
    playerTokenOption,
    versionOption,
    playerOption,
    maxParallelGamesOption
};

var jsonOptions = new JsonSerializerOptions
{
    Converters =
    {
        new JsonStringEnumConverter()
    }
};

rootCommand.SetHandler( async (hostName, port, playerToken, version, playerCmd, maxParallelGames) =>
{
    var client = new TcpClient();

    await client.ConnectAsync( hostName, port );

    Console.WriteLine( "Connected!" );
    Console.WriteLine( "Press Ctrl+C to stop accepting games" );

    await using var stream = client.GetStream();
    await using var writer = new StreamWriter( stream );

    using var reader = new StreamReader( stream );

    var runningGames = new Dictionary<string, RunningGame>();
    var sendQueue = new ConcurrentQueue<ClientMessage>();

    var cancelPressed = false;

    sendQueue.Enqueue( new InitializeMessage( playerToken, version, maxParallelGames ) );

    Console.CancelKeyPress += ( sender, ev ) =>
    {
        if ( cancelPressed || runningGames.Count == 0 )
        {
            Console.WriteLine( $"Exiting immediately" );
            System.Environment.Exit( 0 );
        }
        else
        {
            Console.WriteLine( $"Waiting for all active games to finish before exiting" );
            Console.WriteLine( $"Press Ctrl+C again to immediately exit, forfeiting {runningGames.Count} games" );

            ev.Cancel = false;
            cancelPressed = true;
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
                        Console.WriteLine( $"Game Expired: {expiredGame.Key}" );
                        runningGames.Remove( expiredGame.Key );
                        expiredGame.Value.Dispose();
                    }

                    if ( runningGames.Count >= maxParallelGames || cancelPressed )
                    {
                        sendQueue.Enqueue( new AcceptGameMessage( newGameMessage.Id, false ) );
                    }
                    else
                    {
                        Console.WriteLine( $"New Game: {newGameMessage.Id}" );
                        runningGames[newGameMessage.Id] =
                            new RunningGame( new ChildProcessPlayer( playerCmd[0], playerCmd.Skip( 1 ).ToArray() ),
                                TimeSpan.FromSeconds( newGameMessage.MaxTurnTime ?? 60d ) );
                        sendQueue.Enqueue( new AcceptGameMessage( newGameMessage.Id, true ) );
                    }

                    break;
                }

                case TurnMessage turnMessage:
                {
                    if ( !runningGames.TryGetValue( turnMessage.Id, out var player ) )
                    {
                        break;
                    }

                    Console.WriteLine( $"Turn: {turnMessage.Id}" );
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

                    Console.WriteLine( $"EndGame: {endGameMessage.Id}" );
                    Console.WriteLine( endGameMessage.ResultJson );
                    runningGames.Remove( endGameMessage.Id );
                    player.Dispose();

                    if ( cancelPressed && runningGames.Count == 0 )
                    {
                        return;
                    }

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

}, hostNameOption, portOption, playerTokenOption, versionOption, playerOption, maxParallelGamesOption );

await rootCommand.Parse( args  ).InvokeAsync();
