using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.Immutable;

namespace LostCities;

public static class Helpers
{
    public static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions
    {
        Converters =
        {
            new JsonStringEnumConverter()
        },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static int CalculateScore( this IEnumerable<Card> expedition )
    {
        var wagers = 0;
        var value = 0;

        foreach ( var card in expedition )
        {
            if ( card.Value == Value.Wager )
            {
                ++wagers;
            }
            else
            {
                value += (int)card.Value;
            }
        }

        if ( wagers == 0 && value == 0 )
        {
            return 0;
        }

        return (value - 20) * (1 + wagers);
    }

    public static IReadOnlyList<T> ExceptFirst<T>( this IReadOnlyList<T> list, T item )
        where T : IEquatable<T>
    {
        var subList = new List<T>( list );

        subList.Remove( item );

        return subList;
    }

    public static IReadOnlyList<T> ExceptLast<T>( this IReadOnlyList<T> list )
    {
        return list.SkipLast( 1 ).ToImmutableList();
    }

    public static IReadOnlyList<T> Append<T>( this IReadOnlyList<T> list, T item )
    {
        return new List<T>( list ) { item };
    }

    public static void ShuffleInPlace<T>( this IList<T> list, Random random )
    {
        for ( var i = 0; i < list.Count - 1; ++i )
        {
            var j = random.Next( i, list.Count );
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public static IReadOnlyList<T> Shuffle<T>( this IEnumerable<T> list, Random random )
    {
        var copy = list.ToArray();

        ShuffleInPlace( copy, random );

        return copy;
    }
}
