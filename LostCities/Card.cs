using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LostCities;

public record struct Card( [property: JsonInclude] Color Color, [property: JsonInclude] Value Value ) : IComparable<Card>
{
    public readonly int CompareTo( Card other )
    {
        var colorComparison = Color.CompareTo( other.Color );
        return colorComparison != 0 ? colorComparison : Value.CompareTo( other.Value );
    }

    public override string ToString()
    {
        return $"{Color} {Value}";
    }
}
