using LostCities;
using System.Linq;

static int GetValue( Card card, IReadOnlyDictionary<Color, IReadOnlyList<Card>> expeditions )
{
    var expedition = expeditions[card.Color];

    if ( expedition.Count > 0 && expedition[^1].Value > card.Value )
    {
        // If we can't play it, it's worthless
        return 0;
    }

    var multiplier = 1 + expedition.Count( x => x.Value == Value.Wager );

    return multiplier * (int)card.Value;
}

static IComparer<PlayerAction> CreateComparer( PlayerView view, IReadOnlyList<Card> unknownCards, Random random )
{
    var colorOrder = view.PlayerExpeditions.Keys
        .Shuffle( random )
        .ToArray();

    var colorScore = colorOrder
        .ToDictionary(
            x => x,
            x => Array.IndexOf( colorOrder, x ) );

    var avgUnknownCardScore = unknownCards.Average( x => GetValue( x, view.PlayerExpeditions ) );

    // Positive means a is worse than b

    return Comparer<PlayerAction>.Create( ( a, b ) =>
    {
        // Prefer to play rather than discard

        if ( a.Discarded != b.Discarded )
        {
            return a.Discarded ? 1 : -1;
        }

        // Compare played card

        if ( a.PlayedCard != b.PlayedCard )
        {
            if ( a.Discarded )
            {
                var aMyValue = GetValue( a.PlayedCard, view.PlayerExpeditions );
                var bMyValue = GetValue( b.PlayedCard, view.PlayerExpeditions );

                // Prefer to discard cards that have less value for me

                if ( aMyValue != bMyValue )
                {
                    return aMyValue - bMyValue;
                }

                var aOpponentValue = GetValue( a.PlayedCard, view.OpponentExpeditions );
                var bOpponentValue = GetValue( b.PlayedCard, view.OpponentExpeditions );

                // Prefer to discard cards that have less value for my opponent

                if ( aOpponentValue != bOpponentValue )
                {
                    return aOpponentValue - bOpponentValue;
                }
            }
            else
            {
                var aCommitted = view.PlayerExpeditions[a.PlayedCard.Color].Count > 0;
                var bCommitted = view.PlayerExpeditions[b.PlayedCard.Color].Count > 0;

                // Prefer to play cards we're already committed to

                if ( aCommitted != bCommitted )
                {
                    return aCommitted ? -1 : 1;
                }

                if ( aCommitted )
                {
                    // Prefer to play cards with small gaps from last played

                    var aGap = a.PlayedCard.Value - view.PlayerExpeditions[a.PlayedCard.Color][^1].Value;
                    var bGap = b.PlayedCard.Value - view.PlayerExpeditions[b.PlayedCard.Color][^1].Value;

                    if ( aGap != bGap )
                    {
                        return aGap - bGap;
                    }
                }
                else
                {
                    // Prefer to commit to colors with the highest sum in-hand

                    var aValue = view.Hand.Where( x => x.Color == a.PlayedCard.Color ).CalculateScore();
                    var bValue = view.Hand.Where( x => x.Color == b.PlayedCard.Color ).CalculateScore();

                    if ( aValue != bValue )
                    {
                        return bValue - aValue;
                    }
                }
            }

            return colorScore[a.PlayedCard.Color] - colorScore[b.PlayedCard.Color];
        }

        // Compare draw source

        if ( a.DrawnCard != b.DrawnCard )
        {
            var aMyValue = a.DrawnCard is null
                ? avgUnknownCardScore
                : GetValue( a.DrawnCard.Value, view.PlayerExpeditions );

            var bMyValue = b.DrawnCard is null
                ? avgUnknownCardScore
                : GetValue( b.DrawnCard.Value, view.PlayerExpeditions );

            return -aMyValue.CompareTo( bMyValue );
        }

        return 0;
    } );
}

Random? random = null;

var unknownCards = new List<Card>();
var knownOpponentHand = new List<Card>();

var drewFromDeck = false;

while ( PlayerView.ReadFromConsole() is { } view )
{
    if ( random == null )
    {
        random = new Random( view.Seed );

        // Set up known / unknown cards

        foreach ( var color in view.PlayerExpeditions.Keys )
        {
            unknownCards.Add( new Card( color, Value.Wager ) );
            unknownCards.Add( new Card( color, Value.Wager ) );
            unknownCards.Add( new Card( color, Value.Wager ) );

            for ( var i = Value.Two; i <= Value.Ten; ++i )
            {
                unknownCards.Add( new Card( color, i ) );
            }
        }

        foreach ( var card in view.Hand )
        {
            unknownCards.Remove( card );
        }

        foreach ( var card in view.Discarded.SelectMany( x => x.Value ) )
        {
            unknownCards.Remove( card );
        }

        foreach ( var card in view.PlayerExpeditions.SelectMany( x => x.Value ) )
        {
            unknownCards.Remove( card );
        }

        foreach ( var card in view.OpponentExpeditions.SelectMany( x => x.Value ) )
        {
            unknownCards.Remove( card );
        }
    }

    // Update known / unknown cards

    if ( view.LastAction is {} lastAction )
    {
        if ( knownOpponentHand.Contains( lastAction.PlayedCard ) )
        {
            knownOpponentHand.Remove( lastAction.PlayedCard );
        }
        else
        {
            unknownCards.Remove( lastAction.PlayedCard );
        }

        if ( lastAction.DrawnCard is { } card )
        {
            knownOpponentHand.Add( card );
        }
    }

    if ( drewFromDeck )
    {
        // Newest card will always be last in the hand

        unknownCards.Remove( view.Hand[^1] );
    }

    // Rank actions, pick the best

    var actions = view.ValidActions
        .ToArray();

    var best = actions
        .Order( CreateComparer( view, unknownCards, random ) )
        .First();

    // Remember if we drew from the deck so we can check which card we drew

    drewFromDeck = best.DrawnCard is null;

    best.WriteToConsole();
}
