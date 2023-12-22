using System.Text.Json;

namespace LostCities;

/// <summary>
/// An action taken by a player, including which card was played or discarded, and where a card was drawn from.
/// </summary>
/// <param name="PlayedCard">Which card was played or discarded.</param>
/// <param name="Discarded">Was <see cref="PlayedCard"/> discarded?</param>
/// <param name="DrawnCard">Either a card to draw from the top of a discard pile, or null to draw from the deck.</param>
public record PlayerAction( Card PlayedCard, bool Discarded, Card? DrawnCard )
{
    public static PlayerAction FromJson( string value )
    {
        return JsonSerializer.Deserialize<PlayerAction>( value, Helpers.JsonOptions )!;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize( this, Helpers.JsonOptions );
    }

    public void WriteToConsole()
    {
        Console.WriteLine( ToJson() );
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{(Discarded ? "Discard" : "Play")} {PlayedCard}, Draw {DrawnCard?.ToString() ?? "from Deck"}";
    }
}
