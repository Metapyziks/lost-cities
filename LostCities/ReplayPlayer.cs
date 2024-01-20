
namespace LostCities;

public class ReplayPlayer : IPlayer
{
    private readonly Queue<PlayerAction> _actions;

    public ReplayPlayer( GameResult result, Player player )
    {
        _actions = new Queue<PlayerAction>( player == result.InitialState.CurrentPlayer
            ? result.Actions.Where( ( _, i ) => (i & 1) == 0 )
            : result.Actions.Where( ( _, i ) => (i & 1) == 1 ) );
    }

    public Task<PlayerAction?> TakeTurnAsync( PlayerView view )
    {
        return Task.FromResult( _actions.Dequeue() )!;
    }

    public void Dispose()
    {

    }
}
