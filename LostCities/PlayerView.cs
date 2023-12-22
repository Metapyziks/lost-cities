using System.Text.Json.Serialization;
using System.Text.Json;

namespace LostCities;

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
        return JsonSerializer.Deserialize<PlayerView>( value, Helpers.JsonOptions )!;
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
        return JsonSerializer.Serialize( this, Helpers.JsonOptions );
    }
}
