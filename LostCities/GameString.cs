namespace LostCities;

public static class GameString
{
    public static string Encode( GameResult result )
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter( stream );

        writer.Write( (byte) 1 ); // Version
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
        writer.Write( (byte)state.Deck.Count );

        foreach ( var card in state.Deck )
        {
            WriteCard( writer, card );
        }

        writer.Write( (byte)state.Discarded.Sum( x => x.Value.Count ) );

        foreach ( var card in state.Discarded.SelectMany( x => x.Value ) )
        {
            WriteCard( writer, card );
        }

        writer.Write( (byte)state.Player1.Expeditions.Sum( x => x.Value.Count ) );

        foreach ( var card in state.Player1.Expeditions.SelectMany( x => x.Value ) )
        {
            WriteCard( writer, card );
        }

        writer.Write( (byte)state.Player1.Hand.Count );

        foreach ( var card in state.Player1.Hand )
        {
            WriteCard( writer, card );
        }

        writer.Write( (byte)state.Player2.Expeditions.Sum( x => x.Value.Count ) );

        foreach ( var card in state.Player2.Expeditions.SelectMany( x => x.Value ) )
        {
            WriteCard( writer, card );
        }

        writer.Write( (byte)state.Player2.Hand.Count );

        foreach ( var card in state.Player2.Hand )
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
        throw new NotImplementedException();
    }
}
