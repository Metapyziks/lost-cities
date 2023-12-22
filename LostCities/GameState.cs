using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace LostCities;

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
                x => (IReadOnlyList<Card>) Array.Empty<Card>() ) );
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
            deck, discarded, action );
    }
}