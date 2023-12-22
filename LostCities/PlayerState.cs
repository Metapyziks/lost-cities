using System.Text.Json.Serialization;

namespace LostCities;

public record PlayerState(
    int Seed,
    IReadOnlyList<Card> Hand,
    IReadOnlyDictionary<Color, IReadOnlyList<Card>> Expeditions )
{
    [JsonIgnore] public int Score => Expeditions.Values.Sum( x => x.CalculateScore() );
}
