
using System.Diagnostics;

namespace LostCities;

public interface IPlayer : IDisposable
{
    Task<PlayerAction?> TakeTurnAsync( PlayerView view );
}

public class ConsolePlayer : IPlayer
{
    public Player Player { get; }

    public ConsolePlayer( Player player )
    {
        Player = player;
    }

    public virtual async Task<PlayerAction?> TakeTurnAsync( PlayerView view )
    {
        Console.WriteLine( $"Turn: {Player}" );
        Console.WriteLine( $"View: {view.ToJson()}" );

        var response = await Task.Run( Console.ReadLine );

        return response == null ? null : PlayerAction.FromJson( response );
    }

    public void Dispose()
    {

    }
}

public class HumanConsolePlayer : ConsolePlayer
{
    private enum State
    {
        SelectCard,
        PlayOrDiscard,
        DrawCard,
        Confirm,
        Done
    }

    public HumanConsolePlayer( Player player )
        : base( player )
    {

    }

    private IReadOnlyDictionary<Color, (int Left, int Top)> PrintExpeditions( IReadOnlyDictionary<Color, IReadOnlyList<Card>> expeditions )
    {
        var left = Console.CursorLeft;
        var max = expeditions.Max( x => x.Value.Count );
        var dict = new Dictionary<Color, (int Left, int Top)>();

        for ( var i = 0; i < max; ++i )
        {
            Console.CursorLeft = left;

            foreach ( var (color, cards) in expeditions )
            {
                if ( cards.Count <= i )
                {
                    Console.Write( "     " );
                }
                else
                {
                    dict[color] = (Console.CursorLeft, Console.CursorTop);
                    PrintCard( cards[i] );
                }
            }

            Console.WriteLine();
        }

        return dict;
    }

    private void PrintCard( Card card )
    {
        PrintCard( card.Color, card.Value == Value.Wager ? "W" : ((int) card.Value).ToString() );
    }

    private void PrintCard( Color color, string value )
    {
        Console.BackgroundColor = color.ToConsoleColor();
        Console.ForegroundColor = ConsoleColor.Black;

        Console.Write( $" {value,2} " );
        Console.ResetColor();
        Console.Write( " " );
    }

    public override Task<PlayerAction?> TakeTurnAsync( PlayerView view )
    {
        Console.Clear();

        Console.WriteLine( $"====================" );
        Console.WriteLine( $"= {Player}'s Turn! =" );
        Console.WriteLine( $"====================" );
        Console.WriteLine();
        Console.WriteLine( $"Deck: {view.DeckCount}" );
        Console.WriteLine();

        Console.Write( $"          " );

        foreach ( var color in view.Discarded.Keys )
        {
            PrintCard( color, color.ToAbbreviation() );
        }

        Console.WriteLine();
        Console.WriteLine();
        Console.Write( $"Opponent: " );
        PrintExpeditions( view.OpponentExpeditions );
        Console.WriteLine();
        Console.Write( $" Discard: " );
        var discardPositions = PrintExpeditions( view.Discarded );
        Console.WriteLine();
        Console.Write( $"     You: " );
        PrintExpeditions( view.PlayerExpeditions );
        Console.WriteLine();
        Console.WriteLine();
        Console.Write( $"Hand: " );

        var sortedHand = view.Hand
            .OrderBy( x => x.Color )
            .ThenBy( x => x.Value )
            .ToArray();

        foreach ( var card in sortedHand )
        {
            PrintCard( card );
        }

        Console.WriteLine();

        var state = State.SelectCard;
        var top = Console.CursorTop;
        var selectedIndex = 0;
        var discard = false;

        Console.CursorVisible = false;

        while ( state != State.Done )
        {
            switch ( state )
            {
                case State.SelectCard:
                {
                    Console.CursorTop = top;
                    Console.Write( "      " );

                    for ( var i = 0; i < 8; ++i )
                    {
                        Console.Write( i == selectedIndex ? " ^^  " : "     " );
                    }

                    Console.WriteLine();
                    Console.WriteLine( $"Select a card       " );

                    var key = Console.ReadKey( true );

                    switch ( key.Key )
                    {
                        case ConsoleKey.LeftArrow:
                            selectedIndex = (selectedIndex + 7) & 7;
                            break;

                        case ConsoleKey.RightArrow:
                            selectedIndex = (selectedIndex + 1) & 7;
                            break;

                        case ConsoleKey.Enter:
                            state = State.PlayOrDiscard;
                            break;
                    }

                    break;
                }

                case State.PlayOrDiscard:
                {
                    Console.CursorTop = top + 1;
                    Console.Write( $"Play or discard " );
                    PrintCard( sortedHand[selectedIndex] );
                    Console.WriteLine( "?" );
                    Console.WriteLine( $"{(discard ? " " : ">")} Play" );
                    Console.WriteLine( $"{(discard ? ">" : " ")} Discard" );

                    var key = Console.ReadKey( true );

                    switch ( key.Key )
                    {
                        case ConsoleKey.UpArrow:
                        case ConsoleKey.DownArrow:
                            discard = !discard;
                            break;

                        case ConsoleKey.Enter:
                            state = State.DrawCard;
                            break;

                        case ConsoleKey.Backspace:
                            state = State.SelectCard;
                            Console.CursorTop = top + 1;
                            Console.WriteLine( $"         " );
                            Console.WriteLine( $"         " );
                            break;
                    }

                    break;
                }

                case State.DrawCard:
                {
                    break;
                }

                default:
                {
                    state = State.Done;
                    break;
                }
            }
        }

        Console.CursorVisible = true;
        throw new NotImplementedException();
    }
}

public class ChildProcessPlayer : IPlayer
{
    public Process Process { get; }

    public ChildProcessPlayer( string fileName, params string[] args )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach ( var arg in args )
        {
            startInfo.ArgumentList.Add( arg );
        }

        Process = Process.Start( startInfo )!;
    }

    public async Task<PlayerAction?> TakeTurnAsync( PlayerView view )
    {
        try
        {
            await Process.StandardInput.WriteLineAsync( view.ToJson() );

            var response = await Process.StandardOutput.ReadLineAsync();
            return response == null ? null : PlayerAction.FromJson( response );
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        Process.Dispose();
    }
}
