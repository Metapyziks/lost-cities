using System.Xml;

namespace LostCities;

public static class GameString
{
    public static string Encode( GameResult result )
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter( stream );

        writer.Write( (byte) 3 ); // Version
        writer.Write( (byte) result.InitialState.Discarded.Count ); // Color count
        writer.Write( (byte) result.Winner );
        writer.Write( (byte) result.Disqualified );

        if ( result.Disqualified != Player.None )
        {
            writer.Write( result.Player1FinalState.Score );
            writer.Write( result.Player2FinalState.Score );
        }

        WriteState( writer, result.InitialState );

        writer.Write( result.Actions.Count );

        var state = result.InitialState;

        foreach ( var action in result.Actions )
        {
            WriteAction( writer, state, action );
            state = state.WithAction( action );
        }

        writer.Flush();

        return Convert.ToBase64String( stream.GetBuffer(), 0, (int)stream.Length );
    }

    private static void WriteState( BinaryWriter writer, GameState state )
    {
        writer.Write( (byte)state.CurrentPlayer );

        WriteCards( writer, state.Deck );
        WriteCards( writer, state.Discarded.SelectMany( x => x.Value ) );
        WriteCards( writer, state.Player1.Expeditions.SelectMany( x => x.Value ) );
        WriteCards( writer, state.Player1.Hand );
        WriteCards( writer, state.Player2.Expeditions.SelectMany( x => x.Value ) );
        WriteCards( writer, state.Player2.Hand );
    }

    private static void WriteCards( BinaryWriter writer, IEnumerable<Card> cards )
    {
        var array = cards.ToArray();

        writer.Write( (byte)array.Length );

        foreach ( var card in array )
        {
            WriteCard( writer, card );
        }
    }

    private static void WriteCard( BinaryWriter writer, Card card )
    {
        var encoded = (byte)(((uint)card.Color << 4) | (uint)card.Value);

        writer.Write( encoded );
    }

    private static void WriteAction( BinaryWriter writer, GameState state, PlayerAction action )
    {
        var player = state.CurrentPlayer switch
        {
            Player.Player1 => state.Player1,
            Player.Player2 => state.Player2,
            _ => throw new NotImplementedException()
        };

        var playedIndex = player.Hand.IndexOf( action.PlayedCard );

        if ( playedIndex is -1 or >= 8 )
        {
            throw new NotImplementedException();
        }

        var drawnColor = action.DrawnCard?.Color ?? 0;
        var encoded = (byte)((uint)playedIndex | (uint)(action.Discarded ? 8 : 0) | ((uint)drawnColor << 4));

        writer.Write( encoded );
    }

    public static GameResult Decode( string value )
    {
        var bytes = Convert.FromBase64String( value );

        using var stream = new MemoryStream( bytes );
        using var reader = new BinaryReader( stream );

        var version = reader.ReadByte();
        var colorCount = (int)reader.ReadByte();
        var winner = (Player)reader.ReadByte();
        var disqualified = (Player)reader.ReadByte();

        if ( disqualified != Player.None )
        {
            reader.ReadInt32(); // player 1 score
            reader.ReadInt32(); // player 2 score
        }

        var initialState = ReadState( reader, colorCount );
        var actionCount = reader.ReadInt32();

        var state = initialState;
        var actions = new List<PlayerAction>();

        for ( var i = 0; i < actionCount; i++ )
        {
            var action = ReadAction( reader, state );
            actions.Add( action );

            state = state.WithAction( action );
        }

        return new GameResult( initialState, state, actions, disqualified );
    }

    private static GameState ReadState( BinaryReader reader, int colorCount )
    {
        var currentPlayer = (Player)reader.ReadByte();
        var deck = ReadCards( reader );
        var discarded = ReadCards( reader );
        var player1Expeditions = ReadCards( reader );
        var player1Hand = ReadCards( reader );
        var player2Expeditions = ReadCards( reader );
        var player2Hand = ReadCards( reader );

        return new GameState( currentPlayer,
            new PlayerState( 0, player1Hand, player1Expeditions.DivideByColor( colorCount ) ),
            new PlayerState( 0, player2Hand, player2Expeditions.DivideByColor( colorCount ) ),
            deck, discarded.DivideByColor( colorCount ) );
    }

    private static PlayerAction ReadAction( BinaryReader reader, GameState state )
    {
        var encoded = reader.ReadByte();

        var player = state.CurrentPlayer == Player.Player1
            ? state.Player1
            : state.Player2;

        var playedIndex = encoded & 0x7;
        var discarded = (encoded & 8) == 8;
        var drawnColor = (Color)(encoded >> 4);

        return new PlayerAction( player.Hand[playedIndex], discarded,
            drawnColor == 0 ? null : state.Discarded[drawnColor][^1] );
    }

    private static IReadOnlyList<Card> ReadCards( BinaryReader reader )
    {
        var count = (int)reader.ReadByte();
        var cards = new Card[count];

        for ( var i = 0; i < count; ++i )
        {
            cards[i] = ReadCard( reader );
        }

        return cards;
    }

    private static Card ReadCard( BinaryReader reader )
    {
        var encoded = reader.ReadByte();

        var value = (Value)(encoded & 0xf);
        var color = (Color)(encoded >> 4);

        return new Card( color, value );
    }
}
