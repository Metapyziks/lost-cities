
using System.Diagnostics;
using System.Reflection;

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

        if ( max == 0 )
        {
            Console.WriteLine();
        }

        return dict;
    }

    private void PrintCard( Card card )
    {
        PrintCard( card.Color, card.Value == Value.Wager ? "Wg" : ((int) card.Value).ToString() );
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

        if ( view.LastAction is { } lastAction )
        {
            Console.Write( $"Your opponent {(lastAction.Discarded ? "discarded" : "played")} " );
            PrintCard( lastAction.PlayedCard );
            Console.Write( $"and drew " );

            if ( lastAction.DrawnCard is { } opponentDrawnCard )
            {
                PrintCard( opponentDrawnCard );
            }
            else
            {
                Console.Write( $"from the Deck" );
            }

            Console.WriteLine();
            Console.WriteLine();
        }

        Console.Write( $"Deck: {view.DeckCount} " );

        var deckPos = (Left: Console.CursorLeft, Top: Console.CursorTop);

        Console.WriteLine();
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
        var drawFromDeck = true;
        var drawIndex = 0;
        var discard = false;

        Card playedCard = default;
        Card? drawnCard = null;

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
                    Console.WriteLine( $"Select a card" );

                    var key = Console.ReadKey( true );

                    switch ( key.Key )
                    {
                        case ConsoleKey.LeftArrow:
                        case ConsoleKey.UpArrow:
                            selectedIndex = (selectedIndex + 7) & 7;
                            break;

                        case ConsoleKey.RightArrow:
                        case ConsoleKey.DownArrow:
                            selectedIndex = (selectedIndex + 1) & 7;
                            break;

                        case ConsoleKey.Enter:
                            state = State.PlayOrDiscard;
                            playedCard = sortedHand[selectedIndex];
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
                        case ConsoleKey.LeftArrow:
                        case ConsoleKey.RightArrow:
                            discard = !discard;
                            break;

                        case ConsoleKey.Enter:
                            state = State.DrawCard;
                            Console.CursorTop = top + 1;
                            Console.WriteLine( $"                      " );
                            Console.WriteLine( $"         " );
                            Console.WriteLine( $"         " );
                            break;

                        case ConsoleKey.Backspace:
                        case ConsoleKey.Escape:
                            state = State.SelectCard;
                            Console.CursorTop = top + 1;
                            Console.WriteLine( $"                      " );
                            Console.WriteLine( $"         " );
                            Console.WriteLine( $"         " );
                            break;
                    }

                    break;
                }

                case State.DrawCard:
                {
                    Color? discardedColor = discard ? playedCard.Color : null;

                    var drawableCards = view.Discarded
                        .Where( x => x.Value.Count > 0 )
                        .Where( x => x.Key != discardedColor )
                        .Select( x => x.Value[^1] )
                        .ToArray();

                    var index = 0;
                    foreach ( var card in drawableCards )
                    {
                        var pos = discardPositions[card.Color];

                        Console.CursorLeft = pos.Left;
                        Console.CursorTop = pos.Top + 1;

                        Console.Write( !drawFromDeck && drawIndex == index ? " ^^ " : "    " );
                        ++index;
                    }

                    Console.CursorLeft = deckPos.Left;
                    Console.CursorTop = deckPos.Top;

                    Console.Write(  drawFromDeck ? "<" : " " );
                    Console.CursorLeft = 0;

                    if ( drawableCards.Length == 0 )
                    {
                        state = State.Confirm;
                        break;
                    }

                    Console.CursorTop = top + 1;
                    Console.Write($"Draw ");

                    if ( !drawFromDeck )
                    {
                        PrintCard( drawableCards[drawIndex] );
                        Console.WriteLine( "?    " );
                    }
                    else
                    {
                        Console.WriteLine( $"from Deck?" );
                    }

                    var key = Console.ReadKey( true );

                    switch ( key.Key )
                    {
                        case ConsoleKey.UpArrow:
                        case ConsoleKey.DownArrow:
                            drawFromDeck = drawableCards.Length == 0 || !drawFromDeck;
                            break;

                        case ConsoleKey.LeftArrow:
                            if ( drawableCards.Length > 0 )
                            {
                                drawFromDeck = false;
                                drawIndex = (drawIndex + (drawableCards.Length - 1)) % drawableCards.Length;
                            }
                            break;

                        case ConsoleKey.RightArrow:
                            if ( drawableCards.Length > 0 )
                            {
                                drawFromDeck = false;
                                drawIndex = (drawIndex + 1) % drawableCards.Length;
                            }
                            break;

                        case ConsoleKey.Enter:
                            state = State.Confirm;
                            drawnCard = drawFromDeck ? null : drawableCards[drawIndex];
                            Console.CursorTop = top + 1;
                            Console.WriteLine( $"               " );
                            break;

                        case ConsoleKey.Backspace:
                        case ConsoleKey.Escape:
                            state = State.SelectCard;
                            Console.CursorTop = top + 1;
                            Console.WriteLine( $"               " );
                            break;
                    }

                    break;
                }

                case State.Confirm:
                {
                    Console.CursorTop = top + 1;
                    Console.Write($"{(discard ? "Discard" : "Play")} ");
                    PrintCard( playedCard );
                    Console.Write($"and draw ");

                    if ( drawnCard is { } card )
                    {
                        PrintCard( card );
                    }
                    else
                    {
                        Console.Write( "from Deck?" );
                    }

                    var key = Console.ReadKey( true );

                    switch ( key.Key )
                    {
                        case ConsoleKey.Enter:
                            state = State.Done;
                            Console.CursorTop = top + 1;
                            Console.WriteLine("                               ");
                            break;

                        case ConsoleKey.Backspace:
                        case ConsoleKey.Escape:
                            state = State.SelectCard;
                            Console.CursorTop = top + 1;
                            Console.WriteLine( "                               " );
                            break;
                    }
                    break;
                }
            }
        }

        Console.CursorVisible = true;

        return Task.FromResult( new PlayerAction( playedCard, discard, drawnCard ) )!;
    }
}

public class ChildProcessPlayer : IPlayer
{
    public Process Process { get; }

    public static IPlayer Create( Player player, string fileName, params string[] args )
    {
        try
        {
            var asm = Assembly.LoadFrom( fileName );
            var botType = asm.ExportedTypes
                .First( x => !x.IsAbstract && x.IsAssignableTo( typeof(IPlayer) ) );

            if ( botType.GetConstructor( new[] { typeof(string[]) } ) is { } ctor )
            {
                return (IPlayer)ctor.Invoke( new object[] { args } );
            }

            return (IPlayer)Activator.CreateInstance( botType )!;
        }
        catch
        {
            //
        }

        return new ChildProcessPlayer( player, fileName, args );
    }

    private ChildProcessPlayer( Player player, string fileName, params string[] args )
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

        Task.Run( async () =>
        {
            while ( await Process.StandardError.ReadLineAsync() is { } line )
            {
                Console.Error.WriteLine( $"{player}: {line}" );
            }
        } );
    }

    public async Task<PlayerAction?> TakeTurnAsync( PlayerView view )
    {
        await Process.StandardInput.WriteLineAsync( view.ToJson() );

        var response = await Process.StandardOutput.ReadLineAsync();
        return response == null ? null : PlayerAction.FromJson( response );
    }

    public void Dispose()
    {
        Process.Dispose();
    }
}
