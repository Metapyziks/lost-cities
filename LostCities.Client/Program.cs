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

    await using var stream = client.GetStream();
    await using var writer = new StreamWriter( stream );

    using var reader = new StreamReader( stream );

    var runningGames = new Dictionary<string, IPlayer>();
    var sendQueue = new ConcurrentQueue<ClientMessage>();

    sendQueue.Enqueue( new InitializeMessage( playerToken, version, maxParallelGames ) );

    try
    {
        while ( true )
        {
            while ( sendQueue.TryDequeue( out var clientMessage ) )
            {
                await writer.WriteLineAsync( JsonSerializer.Serialize( clientMessage, jsonOptions ) );
            }

            await writer.FlushAsync();

            var line = await reader.ReadLineAsync().WaitAsync( TimeSpan.FromMilliseconds( 10 ) );

            if ( line == null )
            {
                continue;
            }

            var serverMessage = JsonSerializer.Deserialize<ServerMessage>( line, jsonOptions );

            switch ( serverMessage )
            {
                case NewGameMessage newGameMessage:
                {
                    if ( runningGames.Count >= maxParallelGames )
                    {
                        sendQueue.Enqueue( new AcceptGameMessage( newGameMessage.Id, false ) );
                    }
                    else
                    {
                        Console.WriteLine( $"New Game: {newGameMessage.Id}" );
                        runningGames[newGameMessage.Id] =
                            new ChildProcessPlayer( playerCmd[0], playerCmd.Skip( 1 ).ToArray() );
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
                    _ = player.TakeTurnAsync( PlayerView.FromJson( turnMessage.ViewJson ) )
                        .ContinueWith( async action =>
                        {
                            sendQueue.Enqueue( new ActionMessage( turnMessage.Id, (await action)?.ToJson() ?? "" ) );
                        } );
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
