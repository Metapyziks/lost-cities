
using System.Collections.Concurrent;

namespace LostCities.Client;

public class RunningGame : IDisposable
{
    public IPlayer Player { get; }
    public TimeSpan MaxTurnTime { get; }
    public DateTime LastTurn { get; private set; }

    public bool HasExpired => (DateTime.UtcNow - LastTurn) >= MaxTurnTime * 2;

    public RunningGame( IPlayer player, TimeSpan maxTurnTime )
    {
        Player = player;
        MaxTurnTime = maxTurnTime;
        LastTurn = DateTime.UtcNow;
    }

    public void TakeTurn( PlayerView view, Action<PlayerAction?> response )
    {
        LastTurn = DateTime.UtcNow;
        Player.TakeTurnAsync( view ).ContinueWith( async x =>
        {
            response( await x );
            LastTurn = DateTime.UtcNow;
        } );
    }

    public void Dispose()
    {
        Player.Dispose();
    }
}
