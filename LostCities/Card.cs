using System.Text.Json.Serialization;

namespace LostCities;

/// <summary>
/// Represents a card with a particular <see cref="Color"/> and <see cref="Value"/>.
/// </summary>
/// <param name="Color">Expedition color of the card.</param>
/// <param name="Value">Either <see cref="Value.Wager"/>, or a numerical value.</param>
public record struct Card( [property: JsonInclude] Color Color, [property: JsonInclude] Value Value ) : IComparable<Card>
{
    /// <inheritdoc/>
    public readonly int CompareTo( Card other )
    {
        var colorComparison = Color.CompareTo( other.Color );
        return colorComparison != 0 ? colorComparison : Value.CompareTo( other.Value );
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Color} {Value}";
    }
}
