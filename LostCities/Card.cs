using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostCities;

public enum Color
{
    Red = 1,
    Green,
    Blue,
    White,
    Yellow,
    Purple
}

public enum Value
{
    Wager = 0,
    Two = 2,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten
}

public record struct Card( Color Color, Value Value ) : IComparable<Card>
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
