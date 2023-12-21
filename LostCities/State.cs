using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LostCities;

public record PlayerState(
    int Seed,
    IReadOnlyList<Card> Hand,
    IReadOnlyDictionary<Color, IReadOnlyList<Card>> Expeditions )
{
    [JsonIgnore]
    public int Score => Expeditions.Values.Sum( x => x.Count == 0
        ? 0
        : (x.Sum( y => (int)y.Value ) - 20)
          * (1 + x.Count( y => y.Value == Value.Wager )) );
}

public enum Player
{
    None = 0,
    Player1 = 1,
    Player2 = 2
}

public record GameState(
    Player CurrentPlayer,
    PlayerState Player1,
    PlayerState Player2,
    IReadOnlyList<Card> Deck,
    IReadOnlyDictionary<Color, IReadOnlyList<Card>> Discarded,
    PlayerAction? LastAction = null )
{
    public static GameState New( int deckSeed, int player1Seed, int player2Seed, bool sixthColor = false )
    {
        var random = new Random( deckSeed );
        var deck = new List<Card>();

        var colors = sixthColor
            ? new[] { Color.Red, Color.Green, Color.Blue, Color.White, Color.Yellow, Color.Purple }
            : new[] { Color.Red, Color.Green, Color.Blue, Color.White, Color.Yellow };

        foreach ( var color in colors )
        {
            deck.Add( new Card( color, Value.Wager ) );
            deck.Add( new Card( color, Value.Wager ) );
            deck.Add( new Card( color, Value.Wager ) );

            for ( var i = Value.Two; i <= Value.Ten; ++i )
            {
                deck.Add( new Card( color, i ) );
            }
        }

        deck.ShuffleInPlace( random );

        var player1 = new PlayerState( player1Seed,
            deck.GetRange( 0, 8 ),
            colors.ToImmutableDictionary(
                x => x,
                x => (IReadOnlyList<Card>) Array.Empty<Card>() ) );
        var player2 = new PlayerState( player2Seed,
            deck.GetRange( 8, 8 ),
            colors.ToImmutableDictionary(
                x => x,
                x => (IReadOnlyList<Card>) Array.Empty<Card>() ) );

        deck.RemoveRange( 0, 16 );

        return new GameState( random.NextDouble() < 0.5 ? Player.Player1 : Player.Player2,
            player1, player2, deck,
            colors.ToImmutableDictionary(
                x => x,
                x => (IReadOnlyList<Card>)Array.Empty<Card>() ) );
    }

    [JsonIgnore]
    public PlayerState CurrentPlayerState => CurrentPlayer == Player.Player1 ? Player1 : Player2;

    [JsonIgnore]
    public PlayerState OtherPlayerState => CurrentPlayer == Player.Player1 ? Player2 : Player1;

    [JsonIgnore]
    public PlayerView CurrentPlayerView =>
        new( CurrentPlayerState.Seed, Deck.Count,
            CurrentPlayerState.Hand, CurrentPlayerState.Expeditions,
            OtherPlayerState.Expeditions, Discarded,
            LastAction );

    public GameState WithAction( PlayerAction action )
    {
        if ( !CurrentPlayerState.Hand.Contains( action.PlayedCard ) )
        {
            throw new ArgumentException( "Played card is not in hand." );
        }

        if ( !action.Discarded )
        {
            var expedition = CurrentPlayerState.Expeditions[action.PlayedCard.Color];

            if ( expedition.Count > 0 && expedition[^1].Value > action.PlayedCard.Value )
            {
                throw new ArgumentException( "Played card can't be added to an expedition." );
            }
        }

        var expeditions = CurrentPlayerState.Expeditions.ToDictionary( x => x.Key, x => x.Value );
        var discarded = Discarded.ToDictionary( x => x.Key, x => x.Value );

        if ( action.Discarded )
        {
            discarded[action.PlayedCard.Color] = discarded[action.PlayedCard.Color].Append( action.PlayedCard );
        }
        else
        {
            expeditions[action.PlayedCard.Color] = expeditions[action.PlayedCard.Color].Append( action.PlayedCard );
        }

        var hand = CurrentPlayerState.Hand.ExceptFirst( action.PlayedCard );
        var deck = Deck;

        if ( action.DrawnCard is { } drawnCard )
        {
            if ( action.Discarded && drawnCard.Color == action.PlayedCard.Color )
            {
                throw new ArgumentException( "Can't draw the card that was just discarded." );
            }

            if ( discarded[drawnCard.Color].Count == 0 || discarded[drawnCard.Color][^1] != drawnCard )
            {
                throw new ArgumentException( "Unable to draw the given card." );
            }

            hand = hand.Append( drawnCard );
            discarded[drawnCard.Color] = discarded[drawnCard.Color].ExceptLast();
        }
        else
        {
            hand = hand.Append( deck[^1] );
            deck = deck.ExceptLast();
        }

        var playerState = new PlayerState( CurrentPlayerState.Seed, hand, expeditions );

        return new GameState(
            deck.Count == 0 ? Player.None : CurrentPlayer == Player.Player1 ? Player.Player2 : Player.Player1,
            CurrentPlayer == Player.Player1 ? playerState : Player1,
            CurrentPlayer == Player.Player2 ? playerState : Player2,
            deck, discarded, action);
    }
}

internal static class Global
{
    public static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions
    {
        Converters =
        {
            new JsonStringEnumConverter()
        },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };
}

public record PlayerAction( Card PlayedCard, bool Discarded, Card? DrawnCard )
{
    public static PlayerAction FromJson( string value )
    {
        return JsonSerializer.Deserialize<PlayerAction>( value, Global.JsonOptions )!;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize( this, Global.JsonOptions );
    }

    public void WriteToConsole()
    {
        Console.WriteLine( ToJson() );
    }

    public override string ToString()
    {
        return $"{(Discarded ? "Discard" : "Play")} {PlayedCard}, Draw {DrawnCard?.ToString() ?? "from Deck"}";
    }
}

public record PlayerView( int Seed, int DeckCount,
    IReadOnlyList<Card> Hand,
    IReadOnlyDictionary<Color, IReadOnlyList<Card>> PlayerExpeditions,
    IReadOnlyDictionary<Color, IReadOnlyList<Card>> OpponentExpeditions,
    IReadOnlyDictionary<Color, IReadOnlyList<Card>> Discarded,
    PlayerAction? LastAction )
{
    public static PlayerView? ReadFromConsole()
    {
        var line = Console.ReadLine();

        if ( string.IsNullOrWhiteSpace( line ) )
        {
            return null;
        }

        try
        {
            return FromJson( line );
        }
        catch
        {
            return null;
        }
    }

    public static PlayerView FromJson( string value )
    {
        return JsonSerializer.Deserialize<PlayerView>( value, Global.JsonOptions )!;
    }

    [JsonIgnore]
    public IReadOnlySet<Card> PlayableCards
    {
        get
        {
            var cards = new HashSet<Card>();

            foreach ( var card in Hand )
            {
                if ( PlayerExpeditions[card.Color].Count == 0 || PlayerExpeditions[card.Color][^1].Value <= card.Value )
                {
                    cards.Add( card );
                }
            }

            return cards;
        }
    }

    [JsonIgnore]
    public IReadOnlySet<PlayerAction> ValidActions
    {
        get
        {
            var actions = new HashSet<PlayerAction>();

            foreach ( var card in Hand )
            {
                var canPlay = PlayerExpeditions[card.Color].Count == 0
                    || PlayerExpeditions[card.Color][^1].Value <= card.Value;

                actions.Add( new PlayerAction( card, true, null ) );

                if ( canPlay )
                {
                    actions.Add( new PlayerAction( card, false, null ) );
                }

                foreach ( var (color, discarded) in Discarded )
                {
                    if ( discarded.Count <= 0 ) continue;

                    if ( card.Color != color )
                    {
                        actions.Add( new PlayerAction( card, true, discarded[^1] ) );
                    }

                    if ( canPlay )
                    {
                        actions.Add( new PlayerAction( card, false, discarded[^1] ) );
                    }
                }
            }

            return actions;
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize( this, Global.JsonOptions );
    }
}

public static class ListExtensions
{
    public static IReadOnlyList<T> ExceptFirst<T>( this IReadOnlyList<T> list, T item )
        where T : IEquatable<T>
    {
        var subList = new List<T>( list );

        subList.Remove( item );

        return subList;
    }

    public static IReadOnlyList<T> ExceptLast<T>( this IReadOnlyList<T> list )
    {
        return list.SkipLast( 1 ).ToImmutableList();
    }

    public static IReadOnlyList<T> Append<T>( this IReadOnlyList<T> list, T item )
    {
        return new List<T>( list ) { item };
    }

    public static void ShuffleInPlace<T>( this IList<T> list, Random random )
    {
        for ( var i = 0; i < list.Count - 1; ++i )
        {
            var j = random.Next( i, list.Count );
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public static IReadOnlyList<T> Shuffle<T>( this IEnumerable<T> list, Random random )
    {
        var copy = list.ToArray();

        ShuffleInPlace( copy, random );

        return copy;
    }
}

public record struct GameConfig( int GameSeed, int Player1Seed, int Player2Seed );

public record GameResult( GameState InitialState, GameState FinalState, IReadOnlyList<PlayerAction> Actions, Player Disqualified )
{
    [JsonIgnore]
    public PlayerState Player1FinalState => FinalState.Player1;

    [JsonIgnore]
    public PlayerState Player2FinalState => FinalState.Player2;

    [JsonIgnore]
    public Player Winner =>
        Disqualified switch
        {
            Player.Player1 => Player.Player2,
            Player.Player2 => Player.Player1,
            _ => Player1FinalState.Score.CompareTo( Player2FinalState.Score ) switch
            {
                > 0 => Player.Player1,
                < 0 => Player.Player2,
                _ => Player.None
            }
        };

    public PlayerState GetFinalState( Player player )
    {
        return player switch
        {
            Player.Player1 => Player1FinalState,
            Player.Player2 => Player2FinalState,
            _ => throw new ArgumentException()
        };
    }
}

public record GameSummary( GameConfig Config, Player FirstTurn, Player Winner, Player Disqualified, int? Player1Score, int? Player2Score );

public record GameResults( IReadOnlyList<GameSummary> Summaries, IReadOnlyList<GameResult>? Results )
{
    private static IReadOnlyList<GameSummary> GenerateSummaries( IReadOnlyList<GameConfig> configs, IReadOnlyList<GameResult> results )
    {
        return results
            .Select( (x, i) => new GameSummary( configs[i],
                x.InitialState.CurrentPlayer, x.Winner, x.Disqualified,
                x.Disqualified == Player.None ? x.FinalState.Player1.Score : null,
                x.Disqualified == Player.None ? x.FinalState.Player2.Score : null ) )
            .ToArray();
    }

    public GameResults( IReadOnlyList<GameConfig> Configs, IReadOnlyList<GameResult> Results, bool fullResults )
        : this( GenerateSummaries( Configs, Results ), fullResults ? Results : null )
    {

    }

    public static GameResults FromJson( string value )
    {
        return JsonSerializer.Deserialize<GameResults>( value, Global.JsonOptions )!;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize( this, Global.JsonOptions );
    }
}
