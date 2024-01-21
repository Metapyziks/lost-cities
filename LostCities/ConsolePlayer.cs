
namespace LostCities;

public class ConsolePlayer : IPlayer
{
    public Player Player { get; }

    public ConsolePlayer( Player player )
    {
        Player = player;
    }

    public virtual async Task<PlayerAction?> TakeTurnAsync( PlayerView view )
    {
        Console.WriteLine( $"Turn: {Player}" );
        Console.WriteLine( $"View: {view.ToJson()}" );

        var response = await Task.Run( Console.ReadLine );

        return response == null ? null : PlayerAction.FromJson( response );
    }

    public void Dispose()
    {

    }
}
