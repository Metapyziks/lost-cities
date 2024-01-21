namespace LostCities;

public record struct GameMemory( IReadOnlyList<Card> Unseen, IReadOnlyList<Card> KnownOpponentHand )
{
    public static GameMemory Create( PlayerView view )
    {
        var allCards = new List<Card>();

        foreach ( var (color, _) in view.Discarded )
        {
            allCards.Add( new Card( color, Value.Wager ) );
            allCards.Add( new Card( color, Value.Wager ) );
            allCards.Add( new Card( color, Value.Wager ) );

            for ( var i = Value.Two; i <= Value.Ten; ++i )
            {
                allCards.Add( new Card( color, i ) );
            }
        }

        foreach ( var card in view.Discarded.Values.SelectMany( x => x ) )
        {
            allCards.Remove( card );
        }

        foreach ( var card in view.PlayerExpeditions.Values.SelectMany( x => x ) )
        {
            allCards.Remove( card );
        }

        foreach ( var card in view.OpponentExpeditions.Values.SelectMany( x => x ) )
        {
            allCards.Remove( card );
        }

        return new GameMemory( allCards, Array.Empty<Card>() );
    }

    public GameMemory WithOpponentAction( PlayerAction action )
    {
        var memory = this;

        if ( memory.KnownOpponentHand.Contains( action.PlayedCard ) )
        {
            memory = memory with { KnownOpponentHand = KnownOpponentHand.ExceptFirst( action.PlayedCard ) };
        }
        else
        {
            memory = memory with { Unseen = Unseen.ExceptFirst( action.PlayedCard ) };
        }

        if ( action.DrawnCard is { } drawnCard )
        {
            memory = memory with { KnownOpponentHand = KnownOpponentHand.Append( drawnCard ) };
        }

        return memory;
    }

    public GameMemory WithDrawnCard( Card card )
    {
        return this with { Unseen = Unseen.ExceptFirst( card ) };
    }
}
