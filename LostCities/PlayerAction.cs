using System.Text.Json;

namespace LostCities;

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

    public override string ToString()
    {
        return $"{(Discarded ? "Discard" : "Play")} {PlayedCard}, Draw {DrawnCard?.ToString() ?? "from Deck"}";
    }
}
