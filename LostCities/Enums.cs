
namespace LostCities;

/// <summary>
/// Identifies a player, or <see cref="None"/>.
/// </summary>
public enum Player
{
    None = 0,
    Player1 = 1,
    Player2 = 2
}

/// <summary>
/// Identifies an expedition color.
/// </summary>
public enum Color
{
    Red = 1,
    Green,
    Blue,
    White,
    Yellow,
    Purple
}

/// <summary>
/// Either a numerical value for a card, or <see cref="Wager"/>.
/// </summary>
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
