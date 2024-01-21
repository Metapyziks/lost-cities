using System.Text.Json.Serialization;
using System.Text.Json;

namespace LostCities;

/// <summary>
/// A view of a game from a player's perspective. This includes all public
/// information, and their private hand information.
/// </summary>
/// <param name="Seed">
/// A random number this player should use when initializing any PRNG, so they can act deterministically.
/// This will be the same for each turn.
/// </param>
/// <param name="DeckCount">How many cards remain in the deck.</param>
/// <param name="Hand">The values and colors of each card in the player's hand.</param>
/// <param name="PlayerExpeditions">Cards this player has previously played to an expedition.</param>
/// <param name="OpponentExpeditions">Cards the opponent has previously played to an expedition.</param>
/// <param name="Discarded">Cards that are currently in each discard pile.</param>
/// <param name="LastAction">The last action taken by the opposing player, or null on the very first turn.</param>
public record PlayerView( int Seed, int DeckCount,
    IReadOnlyList<Card> Hand,
    IReadOnlyDictionary<Color, IReadOnlyList<Card>> PlayerExpeditions,
    IReadOnlyDictionary<Color, IReadOnlyList<Card>> OpponentExpeditions,
    IReadOnlyDictionary<Color, IReadOnlyList<Card>> Discarded,
    PlayerAction? LastAction )
{
    public static PlayerView? Read( TextReader reader )
    {
        var line = reader.ReadLine();

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
        return JsonSerializer.Deserialize<PlayerView>( value, Helpers.JsonOptions )!;
    }

    /// <summary>
    /// All hand cards that can currently be played to an expedition.
    /// </summary>
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

    /// <summary>
    /// All possible legal actions from the current game state.
    /// </summary>
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
        return JsonSerializer.Serialize( this, Helpers.JsonOptions );
    }
}
